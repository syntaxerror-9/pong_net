using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace shared;

using socklen_t = uint;
using size_t = nint;

public static partial class OS
{
    public const int SOCK_DGRAM = 2;
    public const int AF_INET = 2;
    public const int F_SETFD = 2; // Set file descriptor
    public const int F_GETFL = 3;
    public const int F_SETFL = 4; // Set file status flags
    public const ulong FIONBIO = 2147772030;
    public const int EAGAIN = 35;
    public const int EWOULDBLOCK = EAGAIN;

    public const int O_NONBLOCK = 0x00000004;

    [LibraryImport("libc")]
    public static partial void printf([MarshalAs(UnmanagedType.LPStr)] string str);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int socket(int domain, int type, int protocol);

    [LibraryImport("libc")]
    public static partial int close(int fd);

    [LibraryImport("libc")]
    public static partial uint inet_addr([MarshalAs(UnmanagedType.LPStr)] string ipString);

    [LibraryImport("libc")]
    public static partial ushort htons(ushort port);

    // https://man7.org/linux/man-pages/man3/bind.3p.html
    [LibraryImport("libc")]
    public static unsafe partial int bind(int sockfd, SockAddrIn* addr, socklen_t addrlen);

    [LibraryImport("libc", SetLastError = true)]
    // https://www.man7.org/linux/man-pages/man3/getaddrinfo.3.html example on server,client
    public static unsafe partial int getaddrinfo([MarshalAs(UnmanagedType.LPStr)] string node,
        [MarshalAs(UnmanagedType.LPStr)] string service, AddrInfo* hints, AddrInfo** res);

    [LibraryImport("libc")]
    public static unsafe partial void freeaddrinfo(AddrInfo* res);

    [LibraryImport("libc", SetLastError = true)]
    public static unsafe partial int connect(int socket, void* addr, uint addrlen);

    [LibraryImport("libc")]
    // https://man7.org/linux/man-pages/man3/recv.3p.html
    public static unsafe partial int recv(int sockfd, void* buffer, size_t size, int flags);

    [LibraryImport("libc", SetLastError = true)]
    // https://man7.org/linux/man-pages/man3/recv.3p.html
    public static unsafe partial int recvfrom(int sockfd, void* buffer, size_t size, int flags, SockAddr* addr,
        socklen_t* addrlen);

    [LibraryImport("libc", SetLastError = true)]
    public static unsafe partial long write(int fd, void* buffer, uint size);

    [LibraryImport("libc", SetLastError = true)]
    public static unsafe partial long sendto(int socket, void* message, nint length, int flags, SockAddr* dest_addr,
        uint dest_len);

    // [LibraryImport("libc", SetLastError = true)]
    [LibraryImport("libc", SetLastError = true)]
    public static unsafe partial int fcntl(int filedes, int cmd, int arg);

    [LibraryImport("libc", SetLastError = true)]
    public static unsafe partial int fcntl(int filedes, int cmd);

    [LibraryImport("libc", SetLastError = true)]
    public static unsafe partial int ioctl(int filedes, ulong cmd, int* value);

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SockAddrIn
    {
        public ushort sin_family;
        public ushort in_port_t;
        public InAddr in_addr;
        public fixed byte sin_zero[8];
    }

    public static unsafe string SockAddrInToString(SockAddrIn* addr)
    {
        return $"sin_family:{addr->sin_family},in_port_t:{addr->in_port_t},in_addr:{addr->in_addr.s_addr}";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InAddr
    {
        public uint s_addr; /* address in network byte order */
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SockAddr
    {
        public byte sa_len;
        public byte sa_family_t;
        public fixed byte sa_data[14];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AddrInfo
    {
        public int ai_flags;
        public int ai_family;
        public int ai_socktype;
        public int ai_protocol;
        public socklen_t ai_addrlen;
        public char* ai_canonname;

        public SockAddr* ai_addr;
        public AddrInfo* ai_next;
    }
}