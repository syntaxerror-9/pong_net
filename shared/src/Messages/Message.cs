using shared.GameObjects;

namespace shared.Messages;

public abstract class Message
{
    public abstract byte[] ToBytes();

    public abstract bool RequiresAck(CallMode ackFrom);
    public abstract byte GetOpcode { get; }
    public byte PacketNumber { get; set; } // Note: Packet number will get set by the DeliveryManager

    public static unsafe Message FromBytes(byte* bytes, int packetSize)
    {
        int opcode = bytes[0];

        switch (opcode)
        {
            case Join.Opcode:
                return new Join { PacketNumber = bytes[1] };
            case Exit.Opcode:
                return new Exit { PacketNumber = bytes[1] };
            case MovePaddle.Opcode:
                int paddleY = Utils.Unpack<int>(&bytes[2], sizeof(int));
                return new MovePaddle(paddleY) { PacketNumber = bytes[1] };
            case BallState.Opcode:
                int offset = 2;
                float positionX = Utils.Unpack<float>(&bytes[offset], sizeof(float));
                offset += sizeof(float);
                float positionY = Utils.Unpack<float>(&bytes[offset], sizeof(float));
                offset += sizeof(float);
                float velocityX = Utils.Unpack<float>(&bytes[offset], sizeof(float));
                offset += sizeof(float);
                float velocityY = Utils.Unpack<float>(&bytes[offset], sizeof(float));
                offset += sizeof(float);
                long timeStamp = Utils.Unpack<long>(&bytes[offset], sizeof(long));
                return new BallState(new Ball
                {
                    PositionX = positionX, PositionY = positionY, VelocityX = velocityX, VelocityY = velocityY
                }, timeStamp) { PacketNumber = bytes[1] };
            case UpdateScore.Opcode:
                offset = 2;
                int player1Score = Utils.Unpack<int>(&bytes[offset], sizeof(int));
                offset += sizeof(int);
                int player2Score = Utils.Unpack<int>(&bytes[offset], sizeof(int));
                return new UpdateScore(player2Score, player1Score) { PacketNumber = bytes[1] };
            case EnemyMovePaddle.Opcode:
                paddleY = Utils.Unpack<int>(&bytes[2], sizeof(int));
                return new EnemyMovePaddle(paddleY) { PacketNumber = bytes[1] };
            case Echo.Opcode:
                return new Echo(bytes[2]) { PacketNumber = bytes[1] };
            case PlayerIndex.Opcode:
                return new PlayerIndex(bytes[2]) { PacketNumber = bytes[1] };
            case Acknowledgment.Opcode:
                return new Acknowledgment(bytes[2]) { PacketNumber = bytes[1] };

            default:
                throw new Exception("Unknown opcode");
        }
    }
}