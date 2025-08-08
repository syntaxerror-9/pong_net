using System.Runtime.CompilerServices;

namespace shared;

public class Utils
{
    public static unsafe bool SameByteSeq(byte* a, byte* b, int length)
    {
        for (int i = 0; i < length; i++)
        {
            if (a[i] != b[i]) return false;
        }

        return true;
    }

    public static byte[] Pack<T>(T t) where T : unmanaged
    {
        var bytes = new byte[Unsafe.SizeOf<T>()];

        Unsafe.WriteUnaligned(ref bytes[0], t);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }

    public static unsafe T Unpack<T>(byte[] bytes) where T : unmanaged
    {
        // Pin the array to get a stable pointer to its first element.
        fixed (byte* pBytes = bytes)
        {
            return Unpack<T>(pBytes, bytes.Length);
        }
    }

    public static unsafe T Unpack<T>(byte* bytes, int len) where T : unmanaged
    {
        if (len != Unsafe.SizeOf<T>())
        {
            throw new ArgumentException($"Byte array length must be {Unsafe.SizeOf<T>()} for type {typeof(T).Name}.");
        }

        if (BitConverter.IsLittleEndian)
        {
            byte* temp = stackalloc byte[len];

            for (int i = 0; i < len; i++)
            {
                temp[i] = bytes[len - 1 - i];
            }

            return Unsafe.ReadUnaligned<T>(temp);
        }
        else
        {
            return Unsafe.ReadUnaligned<T>(bytes);
        }
    }
}