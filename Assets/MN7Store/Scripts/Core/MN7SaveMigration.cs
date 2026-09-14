using System;
using UnityEngine;

namespace MN7.Store
{
    /// <summary>
    /// Shared save-schema compatibility helpers.
    /// Version 0 is the original unversioned payload. Version 1 is the
    /// previous transaction/versioned format. Version 2 is the current
    /// schema used by the store managers.
    /// </summary>
    public static class MN7SaveMigration
    {
        [Serializable]
        private class VersionProbe
        {
            public int version;
        }

        public static bool TryGetVersion(string json, out int version)
        {
            version = 0;

            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                VersionProbe probe = JsonUtility.FromJson<VersionProbe>(json);

                if (probe == null)
                    return false;

                version = Mathf.Max(0, probe.version);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[MN7 Store] Could not read save version. Treating save as legacy.\n{exception}"
                );
                return false;
            }
        }

        public static int GetCompatibleVersion(string json)
        {
            return TryGetVersion(json, out int version) ? version : 0;
        }

        public static bool IsLegacy(string json)
        {
            return !TryGetVersion(json, out _);
        }

        public static bool IsSupportedVersion(int version)
        {
            return version >= 0 && version <= MN7StoreSaveVersion.CurrentVersion;
        }

        public static void LogMigration(int fromVersion, int toVersion, string saveName)
        {
            if (fromVersion >= toVersion)
                return;

            Debug.Log(
                $"[MN7 Store] Migrating {saveName} save from version " +
                $"{fromVersion} to {toVersion}."
            );
        }
    }
}
