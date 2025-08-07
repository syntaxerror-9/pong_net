using System.Runtime.InteropServices;
using server;
using shared;
using Utils = server.Utils;

const int port = 8080;
const string address = "127.0.0.1";

int socketfd = OS.socket(OS.AF_INET, OS.SOCK_DGRAM, 0);
if (socketfd == -1)
{
    Console.WriteLine("Socket creation failed.");
    return;
}


unsafe
{
    int nonblock = 1;
    if (OS.ioctl(socketfd, OS.FIONBIO, &nonblock) == -1)
    {
        throw new Exception($"Ioctl failed. {Marshal.GetLastPInvokeErrorMessage()}");
    }


    OS.SockAddrIn socket_addr = new();
    socket_addr.sin_family = (ushort)OS.AF_INET;
    socket_addr.in_port_t = OS.htons(port);
    socket_addr.in_addr.s_addr = OS.inet_addr(address);
    if (OS.bind(socketfd, &socket_addr, (uint)Marshal.SizeOf<OS.SockAddrIn>()) == -1)
    {
        Console.WriteLine("Binding socket failed");
        return;
    }

    // Keeps track of the received packets from client. Sends ack if they send the same packet again.
    var deliveryManager = new DeliveryManager();

    User* users = stackalloc User[2];
    int userIndex = 0;

    nint size = 500;
    byte* buffer = stackalloc byte[(int)size];
    byte[] packetCounter = new byte[0xFF];
    bool isClient = false;

    while (true)
    {
        OS.SockAddr peer_addr = new();
        uint peer_len = (uint)sizeof(OS.SockAddr);
        int flags = 0;
        int bytesReceived = OS.recvfrom(socketfd, buffer, size, flags, &peer_addr, &peer_len);

        if (bytesReceived == -1)
        {
            int errno = Marshal.GetLastPInvokeError();

            // No packets arrived.
            if (errno == OS.EAGAIN)
            {
                deliveryManager.Update();
                System.Threading.Thread.Sleep(10); // Avoid busy looping
            }
            else
            {
                throw new Exception($"recvfrom failed: {Marshal.GetLastPInvokeErrorMessage()}");
            }
        }
        else
        {
            shared.Messages.Message message = shared.Messages.Message.FromBytes(buffer, bytesReceived);

            int clientId = -1;

            for (int i = 0; i < userIndex; i++)
            {
                if (users[i].peer_addr != null &&
                    shared.Utils.SameByteSeq(users[i].peer_addr->sa_data, peer_addr.sa_data, 14))
                {
                    clientId = users[i].clientId;
                }
            }


            User? foundUser = Utils.FindById(users, clientId);

            bool isDuplicate = false;


            if (message.RequiresAck(CallMode.Server))
            {
                if (clientId != -1 && foundUser != null)
                {
                    var user = foundUser.Value;
                    // Retrieves an existing packet if it was a duplicate, otherwise it creates it and sends ack.
                    if (deliveryManager.ContainsReceiver(message.GetOpcode,
                            user.peer_addr->sa_data,
                            message.PacketNumber,
                            out var receiver))
                    {
                        Console.WriteLine("Was duplicate");
                        isDuplicate = true;
                    }
                    else
                    {
                        receiver = new ReceiverNetMessage(message, user.peer_addr, user.peer_length, socketfd);
                    }

                    deliveryManager.AddReceiver(receiver);
                }
            }

            if (message is shared.Messages.Join && !isDuplicate && clientId == -1)
            {
                Console.WriteLine("Join");
                if (userIndex >= 2)
                {
                    throw new Exception("Too many joins. Panic.");
                }

                // NOTE: This will default to 0 since we stackalloc the struct
                var lastClientId = users[userIndex].clientId;
                // TOOD: is it ok to let it overflow?
                byte userId = (byte)(lastClientId + 1);
                User user = new User(userId, peer_len, peer_addr);
                Console.WriteLine($"Last client id {lastClientId} _ {userId} _ {user.clientId}");

                Console.WriteLine($"Join {userId}");

                var playerIdMessage =
                    new shared.Messages.PlayerIndex(packetCounter.Use(shared.Messages.PlayerIndex.Opcode),
                        (byte)userIndex);
                SenderNetMessage playerIdNetMessage = new SenderNetMessage(playerIdMessage, user.peer_addr,
                    user.peer_length,
                    socketfd,
                    () => { Console.WriteLine($"Player {user.clientId} received the id!"); });
                Console.WriteLine("Added sender");

                deliveryManager.AddSender(playerIdNetMessage);

                // Store the user and increment the index.
                users[userIndex++] = user;
            }
            else if (message is shared.Messages.Acknowledgment acknowledgment)
            {
                if (deliveryManager.ContainsSender(acknowledgment.GetAckOpcode, acknowledgment.PacketNumber,
                        out var senderDelivery))
                {
                    deliveryManager.RemoveSender(senderDelivery);
                    senderDelivery.cb();
                }
                else
                {
                    Console.WriteLine("Received an acknowledgment for an unknown packet. Investigate.");
                }
            }
            else if (message is shared.Messages.MovePaddle movePaddle)
            {
                if (userIndex != 2)
                {
                    Console.WriteLine("Both player have not joined. Nothing to do.");
                }
                else
                {
                    var enemyMovePaddleMessage =
                        new shared.Messages.EnemyMovePaddle(packetCounter.Use(shared.Messages.EnemyMovePaddle.Opcode),
                            movePaddle.PositionY);


                    var otherUser = users[(userIndex + 1) % 2];
                    SendMessage(enemyMovePaddleMessage, otherUser.clientId);
                }
            }
            else if (message is shared.Messages.Exit)
            {
                if (foundUser == null) throw new Exception("Got exit request from invalid user");
                var user = foundUser.Value;
                var currentUserIndex = -1;
                for (int i = 0; i < 2; i++)
                    if (users[userIndex].clientId == user.clientId)
                        currentUserIndex = i;

                // If its at the start of the array, copy the data of the second user to the start of the array
                if (currentUserIndex == 0)
                {
                    NativeMemory.Copy(&users[1], &users[0], (uint)sizeof(OS.SockAddr));
                }

                deliveryManager.DeleteUserRequests(user.peer_addr);


                userIndex = 1;
            }
        }
    }

    void SendMessage(shared.Messages.Message message, int client)
    {
        var foundUser = Utils.FindById(users, client);
        if (foundUser == null) throw new Exception("SendMessage called with invalid id!");
        var user = foundUser.Value;


        Net.SendMessage(socketfd, message, user.peer_addr, user.peer_length);
    }
}