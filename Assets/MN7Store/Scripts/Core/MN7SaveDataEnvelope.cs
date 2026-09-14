using System;

namespace MN7.Store
{
    [Serializable]
    public class MN7SaveDataEnvelope<T>
    {
        public int version = MN7StoreSaveVersion.CurrentVersion;
        public T data;
    }
}
