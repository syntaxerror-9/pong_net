namespace shared.Messages;

public class Join : Message
{
    public override byte[] ToBytes()
    {
        byte[] r = new byte[2];
        r[0] = Opcode;
        r[1] = PacketNumber;
        return r;
    }

    public override bool RequiresAck(CallMode ackFrom)
    {
        return ackFrom == CallMode.Server;
    }

    public override byte GetOpcode => Opcode;
    public const byte Opcode = 0x00;
}