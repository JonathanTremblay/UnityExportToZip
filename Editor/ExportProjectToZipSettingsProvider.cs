#if UNITY_EDITOR
namespace ExportProjectToZip
{
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System.Collections.Generic;

    /// <summary>
    /// Settings provider for the ExportProjectToZip settings 
    /// (accessible from Edit > Project Settings > Export Project to Zip)
    /// Saves the settings in a json file in the ProjectSettings folder.
    /// </summary>
    public class ExportProjectToZipSettingsProvider : SettingsProvider
    {
        enum MessageKey { RepositoryLink, RenameZip, RenameZipTooltip, IncludeBuilds, IncludeBuildsTooltip, Experimental, RevealExperimental, IncludeLibrary, IncludeLibraryTooltip, IncludeLibraryWarning, FoldersToExclude, ExtensionsToExclude, RestoreDefaults, MoreInfo, InvalidFolderTitle, InvalidFolder, DuplicateTitle, DuplicateEntry, Mandatory }
        static readonly Dictionary<MessageKey, string> messages = new()
        {
            { MessageKey.RepositoryLink, "https://github.com/JonathanTremblay/UnityExportToZip" },
            { MessageKey.RenameZip, "Rename Root Folder Using Zip Filename" },
            { MessageKey.RenameZipTooltip, "Name the root level folder in the archive with the name of the zip file." },
            { MessageKey.IncludeBuilds, "Include Build(s) Folder(s)" },
            { MessageKey.IncludeBuildsTooltip, "Include Build and/or Builds folders in the zip archive." },
            { MessageKey.Experimental, "Show Experimental Features" },
            { MessageKey.RevealExperimental, "Show experimental features (not recommended!)" },
            { MessageKey.IncludeLibrary, "Include Library Folder (EXPERIMENTAL)" },
            { MessageKey.IncludeLibraryTooltip, "Include the Library folder in the zip archive." },
            { MessageKey.IncludeLibraryWarning, "Warning! Including the Library folder is a beta feature. It is not recommended except for a very small project. \n\nReopening a zipped project will be faster, but zipping and unzipping will be slower. Also, the resulting zip file will be significantly larger. \n\nOn Windows, some files from the Library will be skipped. It works best on macOS, which does not lock any files in the Library folder." },
            { MessageKey.FoldersToExclude, "Folders to Exclude (From Root Level)" },
            { MessageKey.ExtensionsToExclude, "File Extensions to Exclude (From Root Level)" },
            { MessageKey.RestoreDefaults, "Restore Defaults" },
            { MessageKey.MoreInfo, "More info: " },
            { MessageKey.InvalidFolderTitle, "Invalid Folder Exclusion" },
            { MessageKey.InvalidFolder, "The folder '{0}' cannot be added to the exclusions (it is part of a valid project)." },
            { MessageKey.DuplicateTitle, "Duplicate Entry" },
            { MessageKey.DuplicateEntry, "'{0}' is already in the exclusion list." },
            { MessageKey.Mandatory, "mandatory" }
        };

        const float VERTICAL_SPACING = 10f;
        const float SMALL_VERTICAL_SPACING = 4f;
        const float LABEL_WIDTH = 250f;
        const int LABEL_FIELD_HEIGHT = 15;
        const int TEXT_FIELD_HEIGHT = 20;
        const int BUTTON_WIDTH_REMOVE = 20;
        const int BUTTON_WIDTH_ADD = 50;
        const int BUTTON_HEIGHT = 20;
        const int SPACE_ADJUSTMENT_BETWEEN_LINES = -2;
        readonly GUIStyle smallLabelStyle = new(EditorStyles.label) { padding = new RectOffset(2, 2, 0, 0), fontSize = 11 };
        readonly GUIStyle smallLabelStyleDisabled = new(EditorStyles.label) { padding = new RectOffset(2, 2, 0, 0), fontSize = 11, normal = { textColor = Color.gray } };
        GUIStyle buttonStyle;
        GUIStyle textFieldStyle;
        const string PROJECT_SETTINGS_PATH = "ProjectSettings";
        const string SETTINGS_FILENAME = "ExportProjectToZipSettings.json";
        static readonly List<string> mandatoryFolders = new() { "Library" }; // List of folders that cannot be removed from the exclusions
        static readonly List<string> forbiddenFolders = new() { "Assets", "Packages", "ProjectSettings" }; // List of folders that cannot be excluded
        static ExportProjectToZipSettings settings;
        static string newFolderToExclude = string.Empty;
        static string newExtensionToExclude = string.Empty;
        static bool shouldSaveSettings = false;

        /// <summary>
        /// Constructor for the ExportProjectToZipSettingsProvider class.
        /// </summary>
        /// <param name="path">The path of the settings provider.</param>
        /// <param name="scope">The scope of the settings provider.</param>
        public ExportProjectToZipSettingsProvider(string path, SettingsScope scope) : base(path, scope)
        {
            // LoadSettings(); // Settings are now loaded on demand to avoid file access issues
        }

        /// <summary>
        /// GUI callback for drawing the settings provider GUI.
        /// </summary>
        /// <param name="searchContext">The search context for the GUI.</param>
        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            buttonStyle = new(GUI.skin.button) { padding = new RectOffset(0, 0, 2, 2), margin = new RectOffset(2, 3, 5, 2) };
            textFieldStyle = new(GUI.skin.textField) { padding = new RectOffset(5, 6, 3, 2), margin = new RectOffset(4, 3, 5, 0), fontSize = 11 };

            GUILayout.Space(VERTICAL_SPACING);
            GUILayout.BeginHorizontal();
            GUILayout.Space(VERTICAL_SPACING);
            GUILayout.BeginVertical();

            DrawNamingSection();
            GUILayout.Space(SMALL_VERTICAL_SPACING);

            DrawInclusionsSection();
            GUILayout.Space(VERTICAL_SPACING);

            DrawFolderExclusionsSection();
            GUILayout.Space(VERTICAL_SPACING);

            DrawExtensionExclusionsSection();
            GUILayout.Space(VERTICAL_SPACING);

            DrawExperimentalSection();
            GUILayout.Space(VERTICAL_SPACING);

            DrawRestoreDefaultsButton();
            GUILayout.Space(VERTICAL_SPACING);

            DrawAboutSection();
            GUILayout.Space(VERTICAL_SPACING);


            if (shouldSaveSettings) SaveSettings();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Creates the naming section of the settings GUI.
        /// </summary>
        void DrawNamingSection()
        {
            bool isSelected;
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(messages[MessageKey.RenameZip], messages[MessageKey.RenameZipTooltip]), GUILayout.Width(LABEL_WIDTH));
            isSelected = EditorGUILayout.Toggle(Settings.shouldNameRootLevelFolderWithZipName, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            if (Settings.shouldNameRootLevelFolderWithZipName != isSelected)
            {
                Settings.shouldNameRootLevelFolderWithZipName = isSelected;
                shouldSaveSettings = true;
            }
        }

        /// <summary>
        /// Creates the inclusions section of the settings GUI.
        /// </summary>
        void DrawInclusionsSection()
        {
            bool isSelected;
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(messages[MessageKey.IncludeBuilds], messages[MessageKey.IncludeBuildsTooltip]), GUILayout.Width(LABEL_WIDTH));
            isSelected = EditorGUILayout.Toggle(Settings.shouldIncludeBuilds, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            if (Settings.shouldIncludeBuilds != isSelected)
            {
                Settings.shouldIncludeBuilds = isSelected;
                UpdateBuildFoldersExclusion();
                shouldSaveSettings = true;
            }
        }

        /// <summary>
        /// Creates the folder exclusions section of the settings GUI.
        /// </summary>
        void DrawFolderExclusionsSection()
        {
            EditorGUILayout.LabelField(messages[MessageKey.FoldersToExclude], EditorStyles.boldLabel);
            for (int i = 0; i < Settings.foldersToExclude.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUI.skin.box);
                string folder = Settings.foldersToExclude[i];
                bool isMandatory = mandatoryFolders.Contains(folder);

                string displayFolder = isMandatory ? folder + $" [{messages[MessageKey.Mandatory]}]" : folder;
                GUIStyle labelStyle = isMandatory ? smallLabelStyleDisabled : smallLabelStyle;
                EditorGUILayout.LabelField(displayFolder, labelStyle, GUILayout.Height(LABEL_FIELD_HEIGHT));

                GUILayout.EndVertical();

                GUI.enabled = !isMandatory;
                if (GUILayout.Button("-", buttonStyle, GUILayout.Width(BUTTON_WIDTH_REMOVE), GUILayout.Height(BUTTON_HEIGHT)))
                {
                    if (!isMandatory)
                    {
                        Settings.foldersToExclude.RemoveAt(i);
                        shouldSaveSettings = true;
                    }
                }
                GUI.enabled = true; // Reset GUI state

                GUILayout.EndHorizontal();
                GUILayout.Space(SPACE_ADJUSTMENT_BETWEEN_LINES);
            }
            // Add new folder exclusion
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("FolderToExcludeField");
            newFolderToExclude = EditorGUILayout.TextField(newFolderToExclude, textFieldStyle, GUILayout.Height(TEXT_FIELD_HEIGHT));
            if (GUILayout.Button("Add", buttonStyle, GUILayout.Width(BUTTON_WIDTH_ADD), GUILayout.Height(BUTTON_HEIGHT)))
            {
                string validatedFolder = ValidateFolder(newFolderToExclude);
                if (!string.IsNullOrEmpty(validatedFolder))
                {
                    if (Settings.foldersToExclude.Exists(f => f.Equals(validatedFolder, System.StringComparison.OrdinalIgnoreCase)))
                    {
                        EditorUtility.DisplayDialog(messages[MessageKey.DuplicateTitle], string.Format(messages[MessageKey.DuplicateEntry], validatedFolder), "OK");
                    }
                    else
                    {
                        Settings.foldersToExclude.Add(validatedFolder);
                        Settings.foldersToExclude.Sort();
                        shouldSaveSettings = true;
                    }
                    newFolderToExclude = string.Empty;
                    GUI.FocusControl(null);
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Creates the extension exclusions section of the settings GUI.
        /// </summary>
        void DrawExtensionExclusionsSection()
        {
            EditorGUILayout.LabelField(messages[MessageKey.ExtensionsToExclude], EditorStyles.boldLabel);
            bool extensionsNeedSorting = false;

            for (int i = 0; i < Settings.topLevelExtensionsToExclude.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUI.skin.box);

                string extension = Settings.topLevelExtensionsToExclude[i];
                EditorGUILayout.LabelField(extension, smallLabelStyle, GUILayout.Height(LABEL_FIELD_HEIGHT));
                string validatedExtension = ValidateExtension(extension);

                // Only update the extension if a correction was actually made
                if (!string.IsNullOrEmpty(validatedExtension) && validatedExtension != extension)
                {
                    Settings.topLevelExtensionsToExclude[i] = validatedExtension;
                    extensionsNeedSorting = true;
                    shouldSaveSettings = true;
                }
                GUILayout.EndVertical();
                if (GUILayout.Button("-", buttonStyle, GUILayout.Width(BUTTON_WIDTH_REMOVE), GUILayout.Height(BUTTON_HEIGHT)))
                {
                    Settings.topLevelExtensionsToExclude.RemoveAt(i);
                    shouldSaveSettings = true;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(SPACE_ADJUSTMENT_BETWEEN_LINES);
            }

            if (extensionsNeedSorting) Settings.topLevelExtensionsToExclude.Sort();

            // Add new extension exclusion
            GUILayout.BeginHorizontal();
            newExtensionToExclude = EditorGUILayout.TextField(newExtensionToExclude, textFieldStyle, GUILayout.Height(TEXT_FIELD_HEIGHT));
            if (GUILayout.Button("Add", buttonStyle, GUILayout.Width(BUTTON_WIDTH_ADD), GUILayout.Height(BUTTON_HEIGHT)))
            {
                string validatedExtension = ValidateExtension(newExtensionToExclude);
                if (!string.IsNullOrEmpty(validatedExtension))
                {
                    if (Settings.topLevelExtensionsToExclude.Contains(validatedExtension))
                    {
                        EditorUtility.DisplayDialog(messages[MessageKey.DuplicateTitle], string.Format(messages[MessageKey.DuplicateEntry], validatedExtension), "OK");
                    }
                    else
                    {
                        Settings.topLevelExtensionsToExclude.Add(validatedExtension);
                        Settings.topLevelExtensionsToExclude.Sort();
                        shouldSaveSettings = true;
                    }
                    newExtensionToExclude = string.Empty;
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Creates the experimental section of the settings GUI.
        /// </summary>
        void DrawExperimentalSection()
        {
            bool isSelected;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            // Display the experimental features label in gray color
            GUIStyle experimentalLabelStyle = new(GUI.skin.label) { normal = { textColor = Color.gray } };
            GUIStyle labelStyle = Settings.shouldShowExperimentalFeatures ? GUI.skin.label : experimentalLabelStyle;
            EditorGUILayout.LabelField(new GUIContent(messages[MessageKey.Experimental], messages[MessageKey.RevealExperimental]), labelStyle, GUILayout.Width(LABEL_WIDTH));
            isSelected = EditorGUILayout.Toggle(Settings.shouldShowExperimentalFeatures, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            if (Settings.shouldShowExperimentalFeatures != isSelected)
            {
                Settings.shouldShowExperimentalFeatures = isSelected;
                if (!isSelected && Settings.shouldIncludeLibrary)
                {
                    Settings.shouldIncludeLibrary = false;
                    UpdateLibraryFolderExclusion();
                }
                shouldSaveSettings = true;
            }
            if (Settings.shouldShowExperimentalFeatures)
            {
                GUILayout.Space(SMALL_VERTICAL_SPACING);
                GUILayout.BeginHorizontal();
                GUIStyle experimentalStyle = new(GUI.skin.label) { fontStyle = FontStyle.Bold, normal = { textColor = Color.red } };
                EditorGUILayout.LabelField(new GUIContent(messages[MessageKey.IncludeLibrary], messages[MessageKey.IncludeLibraryTooltip]), experimentalStyle, GUILayout.Width(LABEL_WIDTH));
                isSelected = EditorGUILayout.Toggle(Settings.shouldIncludeLibrary, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
                if (Settings.shouldIncludeLibrary != isSelected)
                {
                    Settings.shouldIncludeLibrary = isSelected;
                    UpdateLibraryFolderExclusion();
                    shouldSaveSettings = true;
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Creates the restore defaults button of the settings GUI.
        /// </summary>
        void DrawRestoreDefaultsButton()
        {
            if (GUILayout.Button(messages[MessageKey.RestoreDefaults]))
            {
                Settings.RestoreDefaults();
                shouldSaveSettings = true;
            }
        }

        /// <summary>
        /// Creates the about section of the settings GUI.
        /// </summary>
        void DrawAboutSection()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUIStyle infoLabelStyle = new GUIStyle(EditorStyles.label) { padding = new RectOffset(4, 0, 2, 0) };
            GUILayout.Label(messages[MessageKey.MoreInfo], infoLabelStyle, GUILayout.ExpandWidth(false));
            if (GUILayout.Button(messages[MessageKey.RepositoryLink], EditorStyles.linkLabel, GUILayout.ExpandWidth(true)))
                Application.OpenURL(messages[MessageKey.RepositoryLink]);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Loads the settings from the settings file or creates new default settings if the file doesn't exist.
        /// </summary>
        static void LoadSettings()
        {
            string filePath = Path.Combine(PROJECT_SETTINGS_PATH, SETTINGS_FILENAME);

            if (!Directory.Exists(PROJECT_SETTINGS_PATH)) Directory.CreateDirectory(PROJECT_SETTINGS_PATH);

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                settings = JsonUtility.FromJson<ExportProjectToZipSettings>(json);

                // Security if the JSON file is corrupted or empty
                if (settings == null) settings = new ExportProjectToZipSettings();

                bool needsSave = false;

                // If lastSavedVersion is empty, settings were saved before version 1.1.6, so we add .slnx
                if (string.IsNullOrEmpty(settings.lastSavedVersion))
                {
                    if (!settings.topLevelExtensionsToExclude.Contains(".slnx"))
                    {
                        settings.topLevelExtensionsToExclude.Add(".slnx");
                        settings.topLevelExtensionsToExclude.Sort();
                    }
                }

                if (settings.lastSavedVersion != ExportProjectToZip.VERSION)
                {
                    settings.lastSavedVersion = ExportProjectToZip.VERSION;
                    needsSave = true;
                }

                if (needsSave) SaveSettings();
            }
            else
            {
                settings = new ExportProjectToZipSettings();
                settings.lastSavedVersion = ExportProjectToZip.VERSION;
                SaveSettings();
            }
        }

        /// <summary>
        /// Saves the settings to the settings file.
        /// </summary>
        static void SaveSettings()
        {
            string filePath = Path.Combine(PROJECT_SETTINGS_PATH, SETTINGS_FILENAME);
            string json = JsonUtility.ToJson(settings);
            File.WriteAllText(filePath, json);
            shouldSaveSettings = false;
        }

        /// <summary>
        /// Gets the ExportProjectToZip settings (loads them if they are not loaded yet).
        /// </summary>
        public static ExportProjectToZipSettings Settings
        {
            get
            {
                if (settings == null) LoadSettings();
                return settings;
            }
        }

        /// <summary>
        /// Creates the settings provider instance.
        /// </summary>
        /// <returns>The newly created SettingsProvider instance.</returns>
        [SettingsProvider]
        static public SettingsProvider CreateSettingsProvider()
        {
            try
            {
                var provider = new ExportProjectToZipSettingsProvider("Project/Export Project to Zip", SettingsScope.Project);
                return provider;
            }
            catch
            {
                // In case of error, do not create the settings provider (may be caused by file access issues)
                return null;
            }
        }

        /// <summary>
        /// Updates the exclusion of Build and Builds folders based on shouldIncludeBuilds.
        /// </summary>
        /// <param name="shouldSaveNow">Whether the settings should be saved immediately.</param>
        public static void UpdateBuildFoldersExclusion(bool shouldSaveNow = false)
        {
            if (Settings.shouldIncludeBuilds)
            {
                Settings.foldersToExclude.Remove("Build");
                Settings.foldersToExclude.Remove("Builds");
            }
            else
            {
                if (!Settings.foldersToExclude.Contains("Build")) Settings.foldersToExclude.Add("Build");
                if (!Settings.foldersToExclude.Contains("Builds")) Settings.foldersToExclude.Add("Builds");
                Settings.foldersToExclude.Sort();
            }
            if (shouldSaveNow) SaveSettings();
        }

        /// <summary>
        /// Updates the exclusion of the Library folder based on shouldIncludeLibrary.
        /// </summary>
        /// <param name="shouldSaveNow">Whether the settings should be saved immediately.</param>
        public static void UpdateLibraryFolderExclusion(bool shouldSaveNow = false)
        {
            if (Settings.shouldIncludeLibrary)
            {
                bool isOK = EditorUtility.DisplayDialog(messages[MessageKey.IncludeLibrary], messages[MessageKey.IncludeLibraryWarning], "OK", "Cancel");
                if (isOK) Settings.foldersToExclude.Remove("Library");
                else Settings.shouldIncludeLibrary = false; // Revert the change if the user cancels
            }
            else
            {
                if (!Settings.foldersToExclude.Contains("Library")) Settings.foldersToExclude.Add("Library");
                Settings.foldersToExclude.Sort();
            }
            if (shouldSaveNow) SaveSettings();
        }

        /// <summary>
        /// Validates an extension string.
        /// </summary>
        /// <param name="extension">The extension string to validate.</param>
        /// <returns>The validated extension string (empty if invalid).</returns>
        string ValidateExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return string.Empty; // Block empty extension

            extension = extension.Trim().ToLowerInvariant(); // Remove leading and trailing spaces and normalize to lowercase

            if (!extension.StartsWith(".")) extension = "." + extension; // Add the dot if it's missing

            foreach (char c in extension)
            {
                if (!char.IsLetterOrDigit(c) && c != '.') return string.Empty; // Block invalid characters
            }

            return extension;
        }

        /// <summary>
        /// Validates a folder string.
        /// </summary>
        /// <param name="folder">The folder string to validate.</param>
        /// <returns>The validated folder string (empty if invalid).</returns>
        string ValidateFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return string.Empty; // Block empty folder

            folder = folder.Trim(); // Remove leading and trailing spaces

            if (forbiddenFolders.Exists(f => f.Equals(folder, System.StringComparison.OrdinalIgnoreCase)))
            {
                EditorUtility.DisplayDialog(messages[MessageKey.InvalidFolderTitle], string.Format(messages[MessageKey.InvalidFolder], folder), "OK");
                return string.Empty; // Forbidden folder
            }

            foreach (char c in folder)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.' && c != ' ') return string.Empty; // Allowed characters: letters, digits, underscore, hyphen, dot, space.
            }

            if (folder == ".") return string.Empty; // Block if the folder is just a dot

            // Block if the folder name contains consecutive dots:
            bool isPreviousDot = false;
            foreach (char c in folder)
            {
                if (c == '.')
                {
                    if (isPreviousDot) return string.Empty; // Block consecutive dots
                    isPreviousDot = true;
                }
                else isPreviousDot = false; // Reset if a non-dot character is found
            }

            // Capitalize the first letter if applicable
            if (char.IsLetter(folder[0])) folder = char.ToUpper(folder[0]) + folder.Substring(1);

            return folder;
        }
    }
}
#endif