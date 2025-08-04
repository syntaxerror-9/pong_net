using System.Runtime.InteropServices;
using shared;

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


    OS.SockAddr* peer_addrs = stackalloc OS.SockAddr[2];
    uint[] peer_lenghts = new uint[2];
    int peer_addrs_counter = 0;
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

            // No packets arrived. Keep working.
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

            int client_id = -1;

            for (int i = 0; i < 2; i++)
            {
                if (Utils.SameByteSeq(peer_addrs[i].sa_data, peer_addr.sa_data, 14)) client_id = i;
            }

            bool isDuplicate = false;


            if (message.RequiresAck(CallMode.Server))
            {
                if (client_id != -1)
                {
                    Console.WriteLine($"Found client {client_id}");
                    // Retrieves an existing packet if it was a duplicate, otherwise it creates it and sends ack.
                    if (deliveryManager.ContainsReceiver(message.GetOpcode, peer_addrs[client_id].sa_data,
                            message.PacketNumber,
                            out var receiver))
                    {
                        Console.WriteLine("Was duplicate");
                        isDuplicate = true;
                    }
                    else
                    {
                        receiver = new ReceiverDelivery(message, &peer_addrs[client_id], peer_len, socketfd);
                    }

                    deliveryManager.AddReceiver(receiver);
                }
            }

            if (message is shared.Messages.Join && !isDuplicate)
            {
                Console.WriteLine("Join");
                if (peer_addrs_counter >= 2)
                {
                    throw new Exception("Too many joins. Panic.");
                }

                peer_addrs[peer_addrs_counter] = peer_addr;
                peer_lenghts[peer_addrs_counter] = peer_len;

                // Edge case
                var joinRcv = new ReceiverDelivery(message, &peer_addrs[peer_addrs_counter], peer_len, socketfd);
                deliveryManager.AddReceiver(joinRcv);


                var counter = peer_addrs_counter;
                SenderDelivery playerIdDelivery = new SenderDelivery(
                    new shared.Messages.PlayerID(packetCounter.Use(shared.Messages.PlayerID.Opcode),
                        (byte)peer_addrs_counter), &peer_addrs[peer_addrs_counter], peer_len, socketfd,
                    () => { Console.WriteLine($"Player {counter} received the id!"); });

                deliveryManager.AddSender(playerIdDelivery);
                peer_addrs_counter++;
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
                var enemyMovePaddleMessage =
                    new shared.Messages.EnemyMovePaddle(packetCounter.Use(shared.Messages.EnemyMovePaddle.Opcode),
                        movePaddle.PositionY);

                new Delivery(enemyMovePaddleMessage, &peer_addrs[(client_id + 1) % 2],
                    peer_lenghts[(client_id + 1) % 2], socketfd).Send();
            }
        }
    }

    void SendMessage(shared.Messages.Message message, int client)
    {
        Net.SendMessage(socketfd, message, &peer_addrs[client], peer_lenghts[client]);
    }
}

// if (OS.sendto(socket, buffer, (uint)packet_size, 0, &peer_addr, peer_len) == -1)
// {
//     Console.WriteLine($"Failed send {Marshal.GetLastPInvokeErrorMessage()}");
//     return;
// }
//
// Console.WriteLine($"Finished receiving - {packet_size}");
// for (int i = 0; i < packet_size; i++)
// {
//     Console.Write((char)buffer[i]);
// }
// }


// if (OS.close(socket) == -1)
// {
//     Console.WriteLine("Closing socket failed.");
// }