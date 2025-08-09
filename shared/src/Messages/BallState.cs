using shared.GameObjects;

namespace shared.Messages;

public class BallState(Ball ball, long timeStamp) : Message
{
    public Ball Ball => ball;

    public override byte[] ToBytes()
    {
        byte[] r = new byte[2 + sizeof(float) * 4 + sizeof(long)];
        r[0] = Opcode;
        r[1] = PacketNumber;

        int offset = 2;
        var positionXPacked = Utils.Pack(ball.PositionX);
        var positionYPacked = Utils.Pack(ball.PositionY);
        var velocityXPacked = Utils.Pack(ball.VelocityX);
        var velocityYPacked = Utils.Pack(ball.VelocityY);
        var timeStampPacked = Utils.Pack(timeStamp);

        Array.Copy(positionXPacked, 0, r, offset, sizeof(float));
        offset += sizeof(float);
        Array.Copy(positionYPacked, 0, r, offset, sizeof(float));
        offset += sizeof(float);
        Array.Copy(velocityXPacked, 0, r, offset, sizeof(float));
        offset += sizeof(float);
        Array.Copy(velocityYPacked, 0, r, offset, sizeof(float));
        offset += sizeof(float);
        Array.Copy(timeStampPacked, 0, r, offset, sizeof(long));


        return r;
    }

    public override bool RequiresAck(CallMode ackFrom) => false;
    public override byte GetOpcode => Opcode;
    public const byte Opcode = 0x03;
}