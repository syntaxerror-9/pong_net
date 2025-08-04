using shared.Messages;

namespace shared;

public unsafe class ReceiverDelivery(
    Message message,
    OS.SockAddr* targetAddr,
    uint targetAddrLen,
    int socketfd
) : Delivery(message, targetAddr, targetAddrLen, socketfd)
{
    private const long EXPIRE_MILLISECONDS = 1000;

    public bool Expired()
    {
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        if (now - timer >= EXPIRE_MILLISECONDS)
        {
            return true;
        }

        return false;
    }

    public ReceiverDelivery SendAck()
    {
        var ackMessage = new shared.Messages.Acknowledgment(message.PacketNumber, message.GetOpcode);
        Net.SendMessage(socketfd, ackMessage, targetAddr, targetAddrLen);
        return this;
    }
}