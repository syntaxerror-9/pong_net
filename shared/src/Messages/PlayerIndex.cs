namespace shared.Messages;

public class PlayerIndex(byte index) : Message
{
    public byte Index { get; } = index;

    public override byte[] ToBytes()
    {
        var r = new byte[3];
        r[0] = Opcode;
        r[1] = PacketNumber;
        r[2] = Index;
        return r;
    }

    public override bool RequiresAck(CallMode ackFrom)
    {
        return ackFrom == CallMode.Client;
    }

    public override byte GetOpcode => Opcode;

    public const byte Opcode = 0x07;
}