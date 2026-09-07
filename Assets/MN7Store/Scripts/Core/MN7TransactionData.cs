using System;

namespace MN7.Store
{
    [Serializable]
    public class MN7TransactionData
    {
        public string transactionId;
        public string itemId;
        public string purchaseType;
        public long timestamp;
        public bool completed;
    }
}