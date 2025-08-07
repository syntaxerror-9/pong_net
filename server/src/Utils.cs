namespace server;

public static class Utils
{
    public static unsafe User? FindById(User* users, int clientId)
    {
        for (int i = 0; i < 2; i++)
        {
            if (users[i].clientId == clientId) return users[i];
        }

        return null;
    }
}