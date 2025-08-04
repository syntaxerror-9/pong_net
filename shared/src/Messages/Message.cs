namespace shared.Messages;

public abstract class Message(byte _packetNumber)
{
    public abstract byte[] ToBytes();

    public abstract bool RequiresAck(CallMode ackFrom);
    public abstract byte GetOpcode { get; }
    public byte PacketNumber { get; } = _packetNumber;

    public static unsafe Message FromBytes(byte* bytes, int packetSize)
    {
        int opcode = bytes[0];

        switch (opcode)
        {
            case Join.Opcode:
                return new Join(bytes[1]);
            case Exit.Opcode:
                return new Exit(bytes[1]);
            case MovePaddle.Opcode:
                int paddleY = 0;
                for (int i = 0; i < sizeof(int); i++)
                {
                    paddleY |= (bytes[2 + i] & 0xFF) << ((sizeof(int) - 1 - i) * 8);
                }

                return new MovePaddle(bytes[1], paddleY);
            case EnemyMovePaddle.Opcode:
                paddleY = 0;
                for (int i = 0; i < sizeof(int); i++)
                {
                    paddleY |= (bytes[2 + i] & 0xFF) << ((sizeof(int) - 1 - i) * 8);
                }

                return new EnemyMovePaddle(bytes[1], paddleY);
            case Echo.Opcode:
                return new Echo(bytes[1], bytes[2]);
            case PlayerID.Opcode:
                return new PlayerID(bytes[1], bytes[2]);
            case Acknowledgment.Opcode:
                return new Acknowledgment(bytes[1], bytes[2]);
            default:
                throw new Exception("Unknown opcode");
        }
    }
}