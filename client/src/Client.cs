using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using shared;

namespace Client;

internal class Client
{
    private static int socketfd;
    private static unsafe OS.AddrInfo* serverAddr;


    private Paddle paddle = new();
    private Paddle enemyPaddle = new();


    public static void SendMessage(shared.Messages.Message msg)
    {
        unsafe
        {
            Net.SendMessage(socketfd, msg, serverAddr->ai_addr, serverAddr->ai_addrlen);
        }
    }

    public static void Shutdown()
    {
        Console.WriteLine("Shutting down");
        unsafe
        {
            OS.freeaddrinfo(serverAddr);
            OS.close(socketfd);
        }

        Raylib.CloseWindow();
        Environment.Exit(0);
    }

    public static unsafe void HandleCloseGame(DeliveryManager deliveryManager, ref byte[] packetCounter)
    {
        // if (Raylib.WindowShouldClose())
        // {
        //     Console.WriteLine("Hallo!");
        //     var exitPacket = new shared.Messages.Exit(packetCounter.Use(shared.Messages.Exit.Opcode));
        //     var senderMessage = new SenderNetMessage(exitPacket, serverAddr->ai_addr, serverAddr->ai_addrlen, socketfd,
        //         Shutdown);
        //     deliveryManager.AddSender(senderMessage);
        // }
    }

    public static unsafe void CreateConnection()
    {
        OS.AddrInfo hints = new();
        OS.AddrInfo* result = null;

        hints.ai_family = OS.AF_INET;
        hints.ai_socktype = OS.SOCK_DGRAM;
        hints.ai_flags = 0;
        hints.ai_protocol = 0;

        int gai_error = OS.getaddrinfo("127.0.0.1", "8080", &hints, &result);

        if (gai_error != 0)
        {
            throw new Exception($"Get addr info failed: {Marshal.GetLastPInvokeErrorMessage()}");
        }

        serverAddr = result;

        socketfd = OS.socket(result->ai_family, result->ai_socktype, result->ai_protocol);
        if (socketfd == -1)
        {
            OS.freeaddrinfo(result);
            throw new Exception($"Socket creation failed. {Marshal.GetLastPInvokeErrorMessage()}");
        }

        /*
         * NOTE: For some reason i cannot understand, it seems that the stack gets corrupted(?) or something goes
         * very wrong with the ffi and it sets the status flags with some random numbers
         */

        // if (OS.fcntl(socketfd, OS.F_SETFL, currentFlags | OS.O_NONBLOCK) == -1)
        // {
        //     OS.freeaddrinfo(result);
        //     throw new Exception($"fcntl failed. {Marshal.GetLastPInvokeErrorMessage()}");
        // }

        int nonblock = 1;
        if (OS.ioctl(socketfd, OS.FIONBIO, &nonblock) == -1)
        {
            OS.freeaddrinfo(result);
            throw new Exception($"Ioctl failed. {Marshal.GetLastPInvokeErrorMessage()}");
        }
    }

    private static int playerIndex = -1;
    private static int paddleY = 300, enemyPaddleY = 300;
    private const int WINDOW_W = 900, WINDOW_H = 600;

    public static void Main()
    {
        byte[] packetCounter = new byte[0xFF];
        NativeLibrary.SetDllImportResolver(typeof(Client).Assembly, Raylib.LoadRaylib);

        CreateConnection();
        shared.Messages.Join joinMessage = new(packetCounter.Use(shared.Messages.Join.Opcode));

        Raylib.InitWindow(WINDOW_W, WINDOW_H, "Hello Raylib!");


        unsafe
        {
            SenderNetMessage joinNetMessage =
                new SenderNetMessage(joinMessage, serverAddr->ai_addr, serverAddr->ai_addrlen, socketfd,
                    () => { Console.WriteLine("Sender delivery!"); });

            var deliveryManager = new DeliveryManager();
            deliveryManager.AddSender(joinNetMessage);
            const int size = 500;
            byte* buffer = stackalloc byte[size];
            long requestedClose = -1;


            // Two cases this loop stops: it timed out (1s) or the client received the server's ack of exiting
            while (requestedClose == -1 || DateTimeOffset.Now.ToUnixTimeMilliseconds() - requestedClose < 1000)
            {
                if (Raylib.WindowShouldClose() && requestedClose == -1)
                {
                    var exitPacket = new shared.Messages.Exit(packetCounter.Use(shared.Messages.Exit.Opcode));
                    var senderMessage = new SenderNetMessage(exitPacket, serverAddr->ai_addr, serverAddr->ai_addrlen,
                        socketfd,
                        () => { });
                    deliveryManager.AddSender(senderMessage);
                    requestedClose = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }

                OS.SockAddr peerAddr = new();
                uint peerLen = (uint)sizeof(OS.SockAddr);
                int bytesReceived = OS.recvfrom(socketfd, buffer, size, 0, &peerAddr, &peerLen);

                if (bytesReceived == -1)
                {
                    int errno = Marshal.GetLastPInvokeError();
                    // No packets arrived. Do some other work.
                    if (errno == OS.EAGAIN)
                    {
                        deliveryManager.Update();

                        if (Raylib.IsKeyDown(Raylib.KEY_J))
                        {
                            int d = 5;
                            paddleY -= d;
                            new NetMessage(
                                new shared.Messages.MovePaddle(packetCounter.Use(shared.Messages.MovePaddle.Opcode),
                                    paddleY), serverAddr->ai_addr, serverAddr->ai_addrlen, socketfd).Send();
                        }

                        if (Raylib.IsKeyDown(Raylib.KEY_K))
                        {
                            int d = 5;
                            paddleY += d;
                            new NetMessage(
                                new shared.Messages.MovePaddle(packetCounter.Use(shared.Messages.MovePaddle.Opcode),
                                    paddleY), serverAddr->ai_addr, serverAddr->ai_addrlen, socketfd).Send();
                        }
                    }
                    else
                    {
                        throw new Exception($"recvfrom failed: {Marshal.GetLastPInvokeErrorMessage()}");
                    }
                }
                else
                {
                    var message = shared.Messages.Message.FromBytes(buffer, bytesReceived);

                    bool isDuplicate = false;

                    if (message.RequiresAck(CallMode.Client))
                    {
                        if (deliveryManager.ContainsReceiver(message.GetOpcode, serverAddr->ai_addr->sa_data,
                                message.PacketNumber, out var receiver))
                        {
                            isDuplicate = true;
                        }
                        else
                        {
                            receiver = new ReceiverNetMessage(message, serverAddr->ai_addr, serverAddr->ai_addrlen,
                                socketfd);
                        }

                        deliveryManager.AddReceiver(receiver);
                    }


                    if (message is shared.Messages.Acknowledgment acknowledgment)
                    {
                        if (deliveryManager.ContainsSender(acknowledgment.GetAckOpcode, acknowledgment.PacketNumber,
                                out var senderDelivery))
                        {
                            deliveryManager.RemoveSender(senderDelivery);
                            senderDelivery.cb();
                            if (acknowledgment.GetAckOpcode == shared.Messages.Exit.Opcode)
                            {
                                Console.WriteLine("Server received exit request.");
                                break;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Received an acknowledgment for an unknown packet. Investigate.");
                        }
                    }
                    else if (message is shared.Messages.PlayerIndex playerIdMessage)
                    {
                        playerIndex = playerIdMessage.Index;
                        Console.WriteLine($"I'm player {playerIndex}");
                    }
                    else if (message is shared.Messages.EnemyMovePaddle enemyMovePaddle)
                    {
                        enemyPaddleY = enemyMovePaddle.PositionY;
                    }


                    Console.WriteLine($"Received message: {message.GetType().Name}");
                }

                Render();
            }

            Shutdown();
        }
    }

    public static void Render()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Raylib.Color { r = 0x18, g = 0x18, b = 0x18, a = 0xFF });

        Raylib.DrawRectangle(playerIndex * WINDOW_W, paddleY, 100, 300, new Raylib.Color { r = 0xFF, a = 0xFF });
        Raylib.DrawRectangle(((playerIndex + 1) % 2) * WINDOW_W, enemyPaddleY, 100, 300,
            new Raylib.Color { r = 0xFF, a = 0xFF });
        Thread.Sleep(50);
        Raylib.EndDrawing();
    }
}