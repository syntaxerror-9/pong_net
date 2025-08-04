using shared.Messages;

namespace shared;

public unsafe class SenderDelivery(
    Message message,
    OS.SockAddr* targetAddr,
    uint targetAddrLen,
    int socketfd,
    Action _cb) : Delivery(message, targetAddr, targetAddrLen, socketfd)
{
    public Action cb => _cb;

    private const long TIMEOUT_MILLISECONDS = 100;

    public void Tick()
    {
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        if (now - timer >= TIMEOUT_MILLISECONDS)
        {
            timer = now;
            SendMessage();
        }
    }

    public SenderDelivery SendMessage()
    {
        Net.SendMessage(socketfd, message, targetAddr, targetAddrLen);
        return this;
    }
}