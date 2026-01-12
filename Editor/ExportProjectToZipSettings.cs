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
        const bool SHOULD_INCLUDE_BUILDS_DEFAULT = false;
        const bool SHOULD_INCLUDE_LIBRARY_DEFAULT = false;
        const bool SHOULD_SHOW_EXPERIMENTAL_FEATURES_DEFAULT = false;
        const bool SHOULD_NAME_ROOT_LEVEL_FOLDER_WITH_ZIP_NAME_DEFAULT = true;
        static readonly List<string> foldersToExcludeDefault = new() { ".git", ".vs", ".vscode", "Build", "Builds", "Library", "Logs", "Obj", "UserSettings", "Temp" };
        static readonly List<string> topLevelExtensionsToExcludeDefault = new() { ".gitignore", ".sln", ".slnx", ".csproj", ".zip" };
        public bool shouldIncludeBuilds = SHOULD_INCLUDE_BUILDS_DEFAULT;
        public bool shouldIncludeLibrary = SHOULD_INCLUDE_LIBRARY_DEFAULT;
        public bool shouldShowExperimentalFeatures = SHOULD_SHOW_EXPERIMENTAL_FEATURES_DEFAULT;
        public bool shouldNameRootLevelFolderWithZipName = SHOULD_NAME_ROOT_LEVEL_FOLDER_WITH_ZIP_NAME_DEFAULT;
        public List<string> foldersToExclude = new(foldersToExcludeDefault);
        public List<string> topLevelExtensionsToExclude = new(topLevelExtensionsToExcludeDefault);
        public string lastSavedVersion = "";

        /// <summary>
        /// Restore the default settings.
        /// </summary>
        public void RestoreDefaults()
        {
            shouldIncludeBuilds = SHOULD_INCLUDE_BUILDS_DEFAULT;
            shouldIncludeLibrary = SHOULD_INCLUDE_LIBRARY_DEFAULT;
            shouldShowExperimentalFeatures = SHOULD_SHOW_EXPERIMENTAL_FEATURES_DEFAULT;
            shouldNameRootLevelFolderWithZipName = SHOULD_NAME_ROOT_LEVEL_FOLDER_WITH_ZIP_NAME_DEFAULT;
            foldersToExclude = new(foldersToExcludeDefault);
            topLevelExtensionsToExclude = new(topLevelExtensionsToExcludeDefault);
            lastSavedVersion = ExportProjectToZip.VERSION;
        }
    }
}
#endif