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

        // Reward indexes that have already been granted.
        public List<int> appliedRewardIndexes =
            new();
    }
}