using System;

namespace MN7.Store
{
    [Serializable]
    public class MN7RewardOperationData
    {
        public string operationId;

        public string transactionId;

        public int rewardIndex;

        public bool completed;
    }
}