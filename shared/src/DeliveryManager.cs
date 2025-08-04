namespace shared;

public class DeliveryManager
{
    public HashSet<SenderDelivery> senderDeliveries { get; private set; } = new();
    public HashSet<ReceiverDelivery> receiverDeliveries { get; private set; } = new();

    private List<ReceiverDelivery> deleteDeliveries = new();


    public void Update()
    {
        deleteDeliveries.Clear();

        foreach (var receiverDelivery in receiverDeliveries)
            if (receiverDelivery.Expired())
                deleteDeliveries.Add(receiverDelivery);

        foreach (var deleteDelivery in deleteDeliveries) receiverDeliveries.Remove(deleteDelivery);

        foreach (var senderDelivery in senderDeliveries) senderDelivery.Tick();
    }

    public bool ContainsSender(byte ackOpcode, byte ackPacketNumber, out SenderDelivery senderDelivery)
    {
        try
        {
            senderDelivery = senderDeliveries.First(snd =>
                snd.Message.GetOpcode == ackOpcode && snd.Message.PacketNumber == ackPacketNumber);
            return true;
        }
        catch (InvalidOperationException)
        {
            senderDelivery = null;
            return false;
        }
    }

    public unsafe bool ContainsReceiver(byte opCode, byte* saData, byte packetNumber, out ReceiverDelivery delivery)
    {
        try
        {
            delivery = receiverDeliveries.First(rcv =>
                rcv.Message.GetOpcode == opCode &&
                Utils.SameByteSeq(rcv.TargetAddr->sa_data, saData, 14) &&
                rcv.Message.PacketNumber == packetNumber
            );
            return true;
        }
        catch (InvalidOperationException)
        {
            delivery = null;
            return false;
        }
    }

    public void RemoveSender(SenderDelivery senderDelivery)
    {
        senderDeliveries.Remove(senderDelivery);
    }

    // Adds to the HashMap and sends a message
    public void AddSender(SenderDelivery senderDelivery)
    {
        senderDelivery.SendMessage();
        senderDeliveries.Add(senderDelivery);
    }

    // Adds to the HashMap and sends a ack
    public void AddReceiver(ReceiverDelivery receiverDelivery)
    {
        receiverDelivery.SendAck();
        receiverDeliveries.Add(receiverDelivery);
    }
}