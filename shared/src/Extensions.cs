namespace shared;

public static class Extensions
{
    // Returns the current counter and increases it
    public static byte Use(this byte[] packetCounter, int index)
    {
        return packetCounter[index]++;
    }

    public static unsafe void Print(this OS.SockAddr addr)
    {
        Console.Write($"len:{addr.sa_len} fam:{addr.sa_family_t} b:");
        for (int i = 0; i < 14; i++) Console.Write($"{addr.sa_data[i]},");
        Console.WriteLine();
    }
}