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
    bool[] joinedUsers = new bool[2];

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


            int clientId = -1, userIndex = -1;

            for (int i = 0; i < 2; i++)
            {
                if (joinedUsers[i] &&
                    users[i].peer_addr != null &&
                    shared.Utils.SameByteSeq(users[i].peer_addr->sa_data, peer_addr.sa_data, 14))
                {
                    Console.WriteLine($"Found match for index {i}");
                    clientId = users[i].clientId;
                    userIndex = i;
                }
            }


            User? foundUser = Utils.FindById(users, clientId);

            peer_addr.Print();


            if (foundUser != null)
            {
                Console.WriteLine($"Received message from {foundUser.Value.clientId}");
            }

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
                if (joinedUsers[0] && joinedUsers[1])
                {
                    throw new Exception("Too many joins. Panic.");
                }

                int lastUserIndex = -1;
                for (int i = 0; i < 2; i++)
                    if (joinedUsers[i])
                        lastUserIndex = i;

                byte userId = (byte)(1 + (lastUserIndex == -1 ? 0 : users[lastUserIndex].clientId));
                User user = new User(userId, peer_len, peer_addr);
                // If lastUserIndex = -1, it will be 0, if lastUserIndex = 0, it will be 1; if lastUserIndex = 1, it will be 0;
                userIndex = (lastUserIndex + 1) % 2;
                users[userIndex] = user;
                joinedUsers[userIndex] = true;
                Console.WriteLine($"Last client id {userId} _ {user.clientId}");
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
                // Same as !joinedUsers[0] || !joinedUsers[1]
                if (!(joinedUsers[0] && joinedUsers[1]))
                {
                    Console.WriteLine("Both player have not joined. Nothing to do.");
                }
                else
                {
                    var enemyMovePaddleMessage =
                        new shared.Messages.EnemyMovePaddle(packetCounter.Use(shared.Messages.EnemyMovePaddle.Opcode),
                            movePaddle.PositionY);

                    if (foundUser == null) throw new Exception("Did not find an user who sent the move message");
                    var user = foundUser.Value;
                    User otherUser = users[0];

                    for (int i = 0; i < 2; i++)
                        if (users[i].clientId != user.clientId)
                            otherUser = users[i];


                    SendMessage(enemyMovePaddleMessage, otherUser.clientId);
                }
            }
            else if (message is shared.Messages.Exit)
            {
                if (foundUser == null || userIndex == -1) throw new Exception("Got exit request from invalid user");
                var user = foundUser.Value;
                joinedUsers[userIndex] = false;
                deliveryManager.DeleteUserRequests(user.peer_addr);

            }
        }
    }

    void SendMessage(shared.Messages.Message message, int client)
    {
        Console.WriteLine($"Sending message to {client}");
        var foundUser = Utils.FindById(users, client);
        if (foundUser == null) throw new Exception("SendMessage called with invalid id!");
        var user = foundUser.Value;


        Net.SendMessage(socketfd, message, user.peer_addr, user.peer_length);
    }
}