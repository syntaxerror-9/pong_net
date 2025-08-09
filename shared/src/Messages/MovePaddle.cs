namespace shared.Messages;

public class MovePaddle(int positionY) : Message
{
    private int _positionY = positionY;

    public int PositionY => _positionY;

    public override byte[] ToBytes()
    {
        byte[] r = new byte[2 + sizeof(int)];
        r[0] = Opcode;
        r[1] = PacketNumber;
        for (int i = sizeof(int) - 1; i >= 0; i--)
        {
            // Pack int into 4 bytes
            r[2 + (sizeof(int) - 1 - i)] = (byte)((_positionY >> (i * 8)) & 0xFF);
        }

        return r;
    }

    public override bool RequiresAck(CallMode ackFrom) => false;
    public override byte GetOpcode => Opcode;

    public const byte Opcode = 0x02;
}