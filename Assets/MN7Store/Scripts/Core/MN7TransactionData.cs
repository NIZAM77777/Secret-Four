using System;
using System.Collections.Generic;

namespace MN7.Store
{
    [Serializable]
    public class MN7TransactionData
    {
        public string transactionId;
        public string itemId;
        public string purchaseType;
        public long timestamp;
        public string status;
        public List<int> appliedRewardIndexes = new();
        public List<string> completedRewardOperations = new();
    }
}
