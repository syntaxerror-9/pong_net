using shared;
using shared.Messages;

namespace shared;

public unsafe class Delivery(
    Message message,
    OS.SockAddr* targetAddr,
    uint targetAddrLen,
    int socketfd)
{
    protected readonly int socketfd = socketfd;
    protected readonly Message message = message;
    protected readonly OS.SockAddr* targetAddr = targetAddr;
    protected readonly uint targetAddrLen = targetAddrLen;
    protected long timer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public Message Message => message;
    public OS.SockAddr* TargetAddr => targetAddr;

    public Delivery Send()
    {
        Net.SendMessage(socketfd, message, targetAddr, targetAddrLen);
        return this;
    }
}