namespace shared;

public class DeliveryManager
{
    public HashSet<SenderNetMessage> senderDeliveries { get; private set; } = new();
    public HashSet<ReceiverNetMessage> receiverDeliveries { get; private set; } = new();

    private byte[] packetCounter = new byte[0xFF];


    private List<ReceiverNetMessage> deleteReceiverDeliveries = new();


    public void Update()
    {
        // Console.WriteLine("Delivery update");
        deleteReceiverDeliveries.Clear();

        foreach (var receiverDelivery in receiverDeliveries)
            if (receiverDelivery.Expired())
                deleteReceiverDeliveries.Add(receiverDelivery);

        foreach (var deleteDelivery in deleteReceiverDeliveries) receiverDeliveries.Remove(deleteDelivery);

        foreach (var senderDelivery in senderDeliveries) senderDelivery.Tick();
    }

    public bool ContainsSender(byte ackOpcode, byte ackPacketNumber, out SenderNetMessage senderDelivery)
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

    public unsafe bool ContainsReceiver(byte opCode, byte* saData, byte packetNumber, out ReceiverNetMessage delivery)
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

    public void RemoveSender(SenderNetMessage senderDelivery)
    {
        senderDeliveries.Remove(senderDelivery);
    }

    // Adds to the HashMap and sends a message
    public void AddSender(SenderNetMessage senderDelivery, bool updatePacketNumber = true)
    {
        if (updatePacketNumber)
        {
            senderDelivery.Message.PacketNumber = packetCounter.Use(senderDelivery.Message.GetOpcode);
        }

        senderDelivery.SendMessage();
        senderDeliveries.Add(senderDelivery);
    }

    public void SendOneshot(NetMessage netMessage)
    {
        netMessage.Message.PacketNumber = packetCounter.Use(netMessage.Message.GetOpcode);
        netMessage.Send();
    }

    // Adds to the HashSet and sends a ack
    public void AddReceiver(ReceiverNetMessage receiverDelivery)
    {
        receiverDeliveries.Add(receiverDelivery);
        receiverDelivery.SendAck();
    }

    public unsafe void DeleteUserRequests(OS.SockAddr* sockAddr)
    {
        List<SenderNetMessage> deleteSenders = new();
        foreach (var senderDelivery in senderDeliveries)
        {
            if (Utils.SameByteSeq(senderDelivery.TargetAddr->sa_data, sockAddr->sa_data, 14))
            {
                deleteSenders.Add(senderDelivery);
            }
        }

        foreach (var msg in deleteSenders) senderDeliveries.Remove(msg);

        foreach (var receiverDelivery in receiverDeliveries)
        {
            if (Utils.SameByteSeq(receiverDelivery.TargetAddr->sa_data, sockAddr->sa_data, 14))
            {
                deleteReceiverDeliveries.Add(receiverDelivery);
            }
        }

        foreach (var msg in deleteReceiverDeliveries) receiverDeliveries.Remove(msg);
    }
}