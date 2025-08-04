namespace shared.Messages;

public class Acknowledgment(byte packetNumber, byte ackOpcode) : Message(packetNumber)
{
    private byte ackOpcode = ackOpcode;

    public override byte[] ToBytes()
    {
        var r = new byte[3];
        r[0] = Opcode;
        r[1] = packetNumber;
        r[2] = ackOpcode;
        return r;
    }

    public byte GetAckOpcode => ackOpcode;

    public override bool RequiresAck(CallMode ackFrom) => false;
    public override byte GetOpcode => Opcode;

    public const byte Opcode = 0xFF;
}