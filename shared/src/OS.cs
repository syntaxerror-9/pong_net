using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace shared;

public static partial class OS
{
    public static int SOCK_DGRAM = 2;
    public static int AF_INET = 2;

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
    public static unsafe partial int bind(int sockfd, SockAddrIn* addr, uint addrlen);

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
    public static unsafe partial int recv(int sockfd, void* buffer, uint size, int flags);

    [LibraryImport("libc", SetLastError = true)]
    public static unsafe partial long write(int fd, void* buffer, uint size);


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
        public uint ai_addrlen;
        public char* ai_canonname;

        public SockAddr* ai_addr;
        public AddrInfo* ai_next;
    }
}