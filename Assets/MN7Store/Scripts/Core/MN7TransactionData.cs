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

        // Populated for local currency purchases so recovery can verify whether
        // the debit actually happened without ever charging the player twice.
        public string paymentCurrencyId;
        public int paymentAmount;

        public List<int> appliedRewardIndexes =
            new();

        public List<string> completedRewardOperations =
            new();
    }
}