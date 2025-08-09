using shared;

namespace server;

public class GameScore
{
    private User user1;
    private User user2;
    private DeliveryManager deliveryManager;
    public int[] Score { get; } = new int[2];
    private int socketfd;
    public bool PendingNextRound { get; private set; }
    private Action onResetReady;


    public GameScore(User user1, User user2, DeliveryManager deliveryManager, int socketfd, Action onResetReady)
    {
        this.user1 = user1;
        this.user2 = user2;
        this.deliveryManager = deliveryManager;
        this.socketfd = socketfd;
        this.onResetReady = onResetReady;
        UpdateScore();
    }


    public void OnUserScore(int userClientId)
    {
        if (user1.clientId == userClientId) Score[0]++;
        else if (user2.clientId == userClientId) Score[1]++;
        else throw new Exception($"Invalid user id supplied {userClientId}");

        UpdateScore();
    }

    private void CheckForReadyness()
    {
        foreach (var sender in deliveryManager.senderDeliveries)
        {
            if (sender.Message is shared.Messages.UpdateScore) return;
        }

        PendingNextRound = false;
        onResetReady.Invoke();
    }


    private unsafe void UpdateScore()
    {
        PendingNextRound = true;
        var message = new shared.Messages.UpdateScore(Score[0], Score[1]);
        var senderDelivery1 =
            new SenderNetMessage(message, user1.peer_addr, user1.peer_length, socketfd, CheckForReadyness);
        deliveryManager.AddSender(senderDelivery1);
        var senderDelivery2 =
            new SenderNetMessage(message, user2.peer_addr, user2.peer_length, socketfd, CheckForReadyness);
        deliveryManager.AddSender(senderDelivery2, updatePacketNumber: false);
    }
}