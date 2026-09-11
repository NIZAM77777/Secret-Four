namespace MN7.Store
{
    public static class MN7StoreSaveVersion
    {
        // Version 2 introduces an explicit versioned save envelope for
        // currency, inventory and rewarded-ad progress. Transaction saves
        // already carried a version field and now use the same current version.
        public const int CurrentVersion = 2;
    }
}
