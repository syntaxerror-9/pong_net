namespace shared.Messages;

public class EnemyMovePaddle(int positionY) : Message
{
    private int _positionY = positionY;

    public int PositionY => _positionY;

    public override byte[] ToBytes()
    {
        byte[] r = new byte[2 + sizeof(int)];
        r[0] = Opcode;
        r[1] = PacketNumber;

        var intPacked = shared.Utils.Pack(PositionY);
        for (int i = 0; i < intPacked.Length; i++)
        {
            r[i + 2] = intPacked[i];
        }

        return r;
    }

    public override bool RequiresAck(CallMode ackFrom) => false;
    public override byte GetOpcode => Opcode;
    public const byte Opcode = 0x05;
}