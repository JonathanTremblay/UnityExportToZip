#if UNITY_EDITOR
using UnityEngine;

namespace ExportProjectToZip
{
    /// <summary>
    /// Settings for the ExportProjectToZip package.
    /// </summary>
    [System.Serializable]
    public class ExportProjectToZipSettings
    {
        const bool shouldIncludeBuildsDefault = false;
        const bool shouldNameRootLevelFolderWithZipNameDefault = true;

        public bool shouldIncludeBuilds = shouldIncludeBuildsDefault;
        public bool shouldNameRootLevelFolderWithZipName = shouldNameRootLevelFolderWithZipNameDefault;

        /// <summary>
        /// Restore the default settings.
        /// </summary>
        public void RestoreDefaults()
        {
            shouldIncludeBuilds = shouldIncludeBuildsDefault;
            shouldNameRootLevelFolderWithZipName = shouldNameRootLevelFolderWithZipNameDefault;
        }
    }
}
#endif