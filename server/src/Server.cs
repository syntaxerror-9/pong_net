using System.Runtime.InteropServices;
using server;
using shared;
using shared.GameObjects;
using Utils = server.Utils;


// How many times the server sends update about events (ball position, matchend etc.) per second
const int TPS = 60;
const long TPMS = 1000 / TPS;


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
    GameScore? gameScore = null;
    var ball = new Ball();

    User* users = stackalloc User[2];
    bool[] joinedUsers = new bool[2];


    nint size = 500;
    byte* buffer = stackalloc byte[(int)size];
    long lastTick = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    float deltaTime = 0f;

    while (true)
    {
        if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastTick < TPMS)
        {
            Thread.Sleep(1);
            continue;
        }

        deltaTime = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastTick) / 1000f;
        lastTick = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        OS.SockAddr peer_addr = new();
        uint peer_len = (uint)sizeof(OS.SockAddr);

        deliveryManager.Update();
        UpdateBallState();
        const int flags = 0;
        int bytesReceived = OS.recvfrom(socketfd, buffer, size, flags, &peer_addr, &peer_len);

        // Process all the requests in a tick
        while (bytesReceived > 0)
        {
            shared.Messages.Message message = shared.Messages.Message.FromBytes(buffer, bytesReceived);
            ProcessMessage(message, peer_addr, peer_len);
            bytesReceived = OS.recvfrom(socketfd, buffer, size, flags, &peer_addr, &peer_len);
        }

        if (bytesReceived == -1)
        {
            int errno = Marshal.GetLastPInvokeError();
            if (errno != OS.EAGAIN)
            {
                throw new Exception($"recvfrom failed: {Marshal.GetLastPInvokeErrorMessage()}");
            }
        }
    }

    void ProcessMessage(shared.Messages.Message message, OS.SockAddr peer_addr, uint peer_len)
    {
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
                    receiver.SendAck();
                    isDuplicate = true;
                }
                else
                {
                    receiver = new ReceiverNetMessage(message, user.peer_addr, user.peer_length, socketfd);
                    deliveryManager.AddReceiver(receiver);
                }
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
                new shared.Messages.PlayerIndex((byte)userIndex);
            SenderNetMessage playerIdNetMessage = new SenderNetMessage(playerIdMessage, user.peer_addr,
                user.peer_length,
                socketfd,
                () => { Console.WriteLine($"Player {user.clientId} received the id!"); });
            Console.WriteLine("Added sender");

            deliveryManager.AddSender(playerIdNetMessage);

            if (joinedUsers[0] && joinedUsers[1]) InitializeGameState();
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
                    new shared.Messages.EnemyMovePaddle(movePaddle.PositionY);

                if (foundUser == null) throw new Exception("Did not find an user who sent the move message");
                var user = foundUser.Value;
                User otherUser = users[0];

                for (int i = 0; i < 2; i++)
                    if (users[i].clientId != user.clientId)
                        otherUser = users[i];

                var netMessage = new NetMessage(enemyMovePaddleMessage, otherUser.peer_addr, otherUser.peer_length,
                    socketfd);
                deliveryManager.SendOneshot(netMessage);
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

    void InitializeGameState()
    {
        ResetBallState();
        gameScore = new GameScore(users[0], users[1], deliveryManager, socketfd, onResetReady: ResetBallState);
    }

    void ResetBallState()
    {
        ball.PositionX = Constants.GAME_WIDTH / 2f - Constants.BALL_RADIUS / 2f;
        ball.PositionY = Constants.GAME_HEIGHT / 2f - Constants.BALL_RADIUS / 2f;

        var randomAngle = new Random().NextSingle() * Math.PI * 2;
        ball.VelocityX = (float)Math.Cos(randomAngle) * Constants.BALL_SPEED;
        ball.VelocityY = (float)Math.Sin(randomAngle) * Constants.BALL_SPEED;
    }

    void UpdateBallState()
    {
        if (!(joinedUsers[0] && joinedUsers[1]) || gameScore == null || gameScore.PendingNextRound)
        {
            return;
        }

        ball.PositionX += ball.VelocityX * deltaTime;
        ball.PositionY += ball.VelocityY * deltaTime;


        // It hit the top wall
        if (ball.PositionY < Constants.BALL_RADIUS)
        {
            ball.VelocityY = -ball.VelocityY;
        }

        // It hit the bottom wall
        if (Constants.GAME_HEIGHT - ball.PositionY < Constants.BALL_RADIUS)
        {
            ball.VelocityY = -ball.VelocityY;
        }

        if (ball.PositionX < Constants.BALL_RADIUS)
        {
            gameScore.OnUserScore(users[1].clientId);
        }

        if (Constants.GAME_WIDTH - ball.PositionX < Constants.BALL_RADIUS)
        {
            gameScore.OnUserScore(users[0].clientId);
        }

        var ballMessage = new shared.Messages.BallState(ball, DateTimeOffset.Now.ToUnixTimeMilliseconds());

        for (int i = 0; i < 2; i++)
        {
            var user = users[i];
            var netMessage = new NetMessage(ballMessage, user.peer_addr, user.peer_length, socketfd);
            deliveryManager.SendOneshot(netMessage);
        }
    }
}