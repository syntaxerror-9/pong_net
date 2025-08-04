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
}