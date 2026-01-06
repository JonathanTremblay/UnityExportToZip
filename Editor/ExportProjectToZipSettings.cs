#if UNITY_EDITOR
namespace ExportProjectToZip
{
    using System.Collections.Generic;

    /// <summary>
    /// Settings for the ExportProjectToZip package.
    /// </summary>
    [System.Serializable]
    public class ExportProjectToZipSettings
    {
        const bool shouldIncludeBuildsDefault = false;
        const bool shouldIncludeLibraryDefault = false;
        const bool shouldShowExperimentalFeaturesDefault = false;
        const bool shouldNameRootLevelFolderWithZipNameDefault = true;
        static readonly List<string> foldersToExcludeDefault = new() { ".git", ".vs", ".vscode", "Build", "Builds", "Library", "Logs", "obj", "Obj", "UserSettings", "Temp" };
        static readonly List<string> topLevelExtensionsToExcludeDefault = new() { ".gitignore", ".sln", ".csproj", ".zip" };
        public bool shouldIncludeBuilds = shouldIncludeBuildsDefault;
        public bool shouldIncludeLibrary = shouldIncludeLibraryDefault;
        public bool shouldShowExperimentalFeatures = shouldShowExperimentalFeaturesDefault;
        public bool shouldNameRootLevelFolderWithZipName = shouldNameRootLevelFolderWithZipNameDefault;
        public List<string> foldersToExclude = new(foldersToExcludeDefault);
        public List<string> topLevelExtensionsToExclude = new(topLevelExtensionsToExcludeDefault);

        /// <summary>
        /// Restore the default settings.
        /// </summary>
        public void RestoreDefaults()
        {
            shouldIncludeBuilds = shouldIncludeBuildsDefault;
            shouldIncludeLibrary = shouldIncludeLibraryDefault;
            shouldShowExperimentalFeatures = shouldShowExperimentalFeaturesDefault;
            shouldNameRootLevelFolderWithZipName = shouldNameRootLevelFolderWithZipNameDefault;
            foldersToExclude = new(foldersToExcludeDefault);
            topLevelExtensionsToExclude = new(topLevelExtensionsToExcludeDefault);
        }
    }
}
#endif