namespace shared;

public static class Extensions
{
    // Returns the current counter and increases it
    public static byte Use(this byte[] packetCounter, int index)
    {
        return packetCounter[index]++;
    }
}