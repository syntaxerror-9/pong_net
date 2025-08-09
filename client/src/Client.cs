using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using shared;
using shared.GameObjects;

namespace Client;

internal class Client
{
    private static int socketfd;
    private static unsafe OS.AddrInfo* serverAddr;
    private static int playerIndex = -1;
    private static float deltaTime = 0f;

    private static float paddleY = 300, enemyPaddleY = 300;

    // TODO: Make this resizable, and scale according to the simulation size (Constants.GAME_WIDTH,GAME_HEIGHT)
    private const int WINDOW_W = Constants.GAME_WIDTH, WINDOW_H = Constants.GAME_HEIGHT;
    private static Ball ball = new();
    private static List<long> fps = new(10);
    private static int[] playersScore = new int[2];

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


    public static void Main()
    {
        fps.Add(DateTimeOffset.Now.ToUnixTimeMilliseconds());
        NativeLibrary.SetDllImportResolver(typeof(Client).Assembly, Raylib.LoadRaylib);

        CreateConnection();
        shared.Messages.Join joinMessage = new();

        Raylib.InitWindow(WINDOW_W, WINDOW_H, "Hello Raylib!");


        unsafe
        {
            SenderNetMessage joinNetMessage =
                new SenderNetMessage(joinMessage, serverAddr->ai_addr, serverAddr->ai_addrlen, socketfd,
                    () => { Console.WriteLine("Join message completed."); });

            var deliveryManager = new DeliveryManager();
            deliveryManager.AddSender(joinNetMessage);
            const int size = 500;
            byte* buffer = stackalloc byte[size];
            long requestedClose = -1;
            bool forceQuit = false;

            OS.SockAddr peerAddr = new();
            uint peerLen = (uint)sizeof(OS.SockAddr);

            // Two cases this loop stops: it timed out (1s) or the client received the server's ack of exiting
            while (!forceQuit && (requestedClose == -1 ||
                                  DateTimeOffset.Now.ToUnixTimeMilliseconds() - requestedClose < 1000))
            {
                if (Raylib.WindowShouldClose() && requestedClose == -1)
                {
                    var exitPacket = new shared.Messages.Exit();
                    var senderMessage = new SenderNetMessage(exitPacket, serverAddr->ai_addr, serverAddr->ai_addrlen,
                        socketfd,
                        () => { });
                    deliveryManager.AddSender(senderMessage);
                    requestedClose = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }


                deliveryManager.Update();

                if (Raylib.IsKeyDown(Raylib.KEY_J) || Raylib.IsKeyDown(Raylib.KEY_DOWN))
                {
                    paddleY = Math.Clamp(paddleY + Constants.PADDLE_SPEED * deltaTime, 0,
                        Constants.GAME_HEIGHT - Constants.PADDLE_HEIGHT);
                    deliveryManager.SendOneshot(new NetMessage(
                        new shared.Messages.MovePaddle(
                            (int)paddleY), serverAddr->ai_addr, serverAddr->ai_addrlen, socketfd));
                }

                if (Raylib.IsKeyDown(Raylib.KEY_K) || Raylib.IsKeyDown(Raylib.KEY_UP))
                {
                    paddleY = Math.Clamp(paddleY - Constants.PADDLE_SPEED * deltaTime, 0,
                        Constants.GAME_HEIGHT - Constants.PADDLE_HEIGHT);
                    // paddleY -= Constants.PADDLE_SPEED * deltaTime;
                    deliveryManager.SendOneshot(new NetMessage(
                        new shared.Messages.MovePaddle(
                            (int)paddleY), serverAddr->ai_addr, serverAddr->ai_addrlen, socketfd).Send());
                }

                int bytesReceived = OS.recvfrom(socketfd, buffer, size, 0, &peerAddr, &peerLen);

                while (bytesReceived > 0)
                {
                    shared.Messages.Message message = shared.Messages.Message.FromBytes(buffer, bytesReceived);
                    ProcessMessage(message);
                    bytesReceived = OS.recvfrom(socketfd, buffer, size, 0, &peerAddr, &peerLen);
                }

                if (bytesReceived == -1)
                {
                    int errno = Marshal.GetLastPInvokeError();
                    // No packets arrived. Do some other work.
                    if (errno != OS.EAGAIN)
                    {
                        throw new Exception($"recvfrom failed: {Marshal.GetLastPInvokeErrorMessage()}");
                    }
                }

                Render();
            }


            void ProcessMessage(shared.Messages.Message message)
            {
                bool isDuplicate = false;

                if (message.RequiresAck(CallMode.Client))
                {
                    if (deliveryManager.ContainsReceiver(message.GetOpcode, serverAddr->ai_addr->sa_data,
                            message.PacketNumber, out var receiver))
                    {
                        receiver.SendAck();
                    }
                    else
                    {
                        receiver = new ReceiverNetMessage(message, serverAddr->ai_addr, serverAddr->ai_addrlen,
                            socketfd);
                        deliveryManager.AddReceiver(receiver);
                    }
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
                            forceQuit = true;
                        }
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Received an acknowledgment for an unknown packet. Investigate. {acknowledgment.GetAckOpcode} {acknowledgment.PacketNumber}");
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
                else if (message is shared.Messages.BallState ballState)
                {
                    ball = ballState.Ball;
                }
                else if (message is shared.Messages.UpdateScore updateScore)
                {
                    playersScore[0] = updateScore.Player1Score;
                    playersScore[1] = updateScore.Player2Score;
                }


                Console.WriteLine($"Received message: {message.GetType().Name}");
            }

            Shutdown();
        }
    }

    public static int CalculateFps()
    {
        if (fps.Count != 10) return 0;
        List<double> dFps = new();
        for (int i = 1; i < fps.Count; i++)
        {
            long timeDiff = fps[i] - fps[i - 1];
            if (timeDiff > 0)
            {
                dFps.Add(1000.0 / timeDiff);
            }
        }

        if (dFps.Count == 0) return 0;


        return (int)dFps.Average();
    }


    public static void Render()
    {
        var avgFps = CalculateFps();
        if (fps.Count >= 10)
            fps.RemoveAt(0);

        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Raylib.Color { r = 0x18, g = 0x18, b = 0x18, a = 0xFF });

        var enemyPlayerIndex = (playerIndex + 1) % 2;
        var scoreString = $"{playersScore[0]}:{playersScore[1]}";
        const int scoreSize = 20;
        int textStringSize = Raylib.MeasureText(scoreString, scoreSize);

        Raylib.DrawText($"{playersScore[0]}:{playersScore[1]}", Constants.GAME_WIDTH / 2 - textStringSize / 2, 20,
            scoreSize,
            new Raylib.Color { r = 0xFF, g = 0xFF, b = 0xFF, a = 0xFF });
        Raylib.DrawRectangle((playerIndex * WINDOW_W) - Constants.PADDLE_WIDTH * playerIndex, (int)paddleY,
            Constants.PADDLE_WIDTH,
            Constants.PADDLE_HEIGHT,
            new Raylib.Color { r = 0xFF, a = 0xFF });
        Raylib.DrawRectangle(enemyPlayerIndex * WINDOW_W - Constants.PADDLE_WIDTH * enemyPlayerIndex, (int)enemyPaddleY,
            Constants.PADDLE_WIDTH, Constants.PADDLE_HEIGHT,
            new Raylib.Color { r = 0x88, a = 0xFF });
        Raylib.DrawCircle((int)ball.PositionX, (int)ball.PositionY, Constants.BALL_RADIUS,
            new Raylib.Color { g = 0xFF, a = 0xFF });

        Raylib.DrawText($"FPS:{avgFps}", 0, 0, 20,
            new Raylib.Color { r = 0xFF, g = 0xFF, b = 0xFF, a = 0xFF });

        Raylib.EndDrawing();
        fps.Add(DateTimeOffset.Now.ToUnixTimeMilliseconds());
        if (fps.Count >= 2) deltaTime = (fps[^1] - fps[^2]) / 1000f;
    }
}