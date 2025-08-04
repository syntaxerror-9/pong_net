namespace shared.Messages;

public class EnemyMovePaddle(byte packetNumber, int positionY) : Message(packetNumber)
{
    private int _positionY = positionY;

    public int PositionY => _positionY;

    public override byte[] ToBytes()
    {
        byte[] r = new byte[2 + sizeof(int)];
        r[0] = Opcode;
        r[1] = packetNumber;
        for (int i = sizeof(int) - 1; i >= 0; i--)
        {
            // Pack int into 4 bytes
            r[2 + (sizeof(int) - 1 - i)] = (byte)((_positionY >> (i * 8)) & 0xFF);
        }

        return r;
    }

    public override bool RequiresAck(CallMode ackFrom) => false;
    public override byte GetOpcode => Opcode;
    public const byte Opcode = 0x05;
}