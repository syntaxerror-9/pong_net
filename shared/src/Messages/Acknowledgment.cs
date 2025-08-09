namespace shared.Messages;

public class Acknowledgment(byte ackOpcode) : Message
{
    private byte ackOpcode = ackOpcode;

    public override byte[] ToBytes()
    {
        var r = new byte[3];
        r[0] = Opcode;
        r[1] = PacketNumber;
        r[2] = ackOpcode;
        return r;
    }

    public byte GetAckOpcode => ackOpcode;

    public override bool RequiresAck(CallMode ackFrom) => false;
    public override byte GetOpcode => Opcode;

    public const byte Opcode = 0xFF;
}