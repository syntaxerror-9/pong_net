using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace shared;

public static partial class OS
{
    public static int SOCK_DGRAM = 2;
    public static int AF_INET = 2;
    [LibraryImport("libc")]
    public static partial void printf([MarshalAs(UnmanagedType.LPStr)] string str);

    [LibraryImport("libc")]
    public static partial int socket(int domain, int type, int protocol);

    [LibraryImport("libc")]
    public static partial int close(int fd);

    [LibraryImport("libc")]
    public static partial uint inet_addr([MarshalAs(UnmanagedType.LPStr)] string ipString);
    [LibraryImport("libc")]
    public static partial ushort htons(ushort port);
    [LibraryImport("libc")]
    public static unsafe partial int bind(int sockfd, SockAddrIn* addr, uint addrlen);

    // https://www.man7.org/linux/man-pages/man3/getaddrinfo.3.html example on server,client
    [LibraryImport("libc")]
    public static unsafe partial int recv(int sockfd, void* buffer, uint size, int flags);

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SockAddrIn
    {
        public ushort sin_family;
        public ushort in_port_t;
        public uint in_addr;
        public fixed byte sin_zero[8];

    }



}