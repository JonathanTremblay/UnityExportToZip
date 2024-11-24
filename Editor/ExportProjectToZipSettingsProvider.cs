#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace ExportProjectToZip
{
    /// <summary>
    /// Settings provider for the ExportProjectToZip settings 
    /// (accessible from Edit > Project Settings > Export Project to Zip)
    /// Saves the settings in a json file in the ProjectSettings folder.
    /// </summary>
    public class ExportProjectToZipSettingsProvider : SettingsProvider
    {
        static readonly Dictionary<string, string> messages = new Dictionary<string, string>
        {
            { "ABOUT", "ExportProjectToZip allows to easily export an entire Unity project to a Zip file." },
            { "REPOSITORY_LINK", "https://github.com/JonathanTremblay/UnityExportToZip" },
            { "RENAME_ZIP", "Rename Root Folder Using Zip Filename" },
            { "RENAME_ZIP_TOOLTIP", "Name the root level folder in the archive with the name of the zip file." },
            { "INCLUDE_BUILDS", "Include Build(s) Folder(s)" },
            { "INCLUDE_BUILDS_TOOLTIP", "Include Build and/or Builds folders in the zip archive." },
            { "FOLDERS_TO_EXCLUDE", "Folders to Exclude (From Root Level)" },
            { "EXTENSIONS_TO_EXCLUDE", "File Extensions to Exclude (From Root Level)" },
            { "RESTORE_DEFAULTS", "Restore Defaults" },
            { "MORE_INFO", "More info: " },
            { "INVALID_FOLDER_TITLE", "Invalid Folder Exclusion" },
            { "INVALID_FOLDER", "The folder '{0}' cannot be added to the exclusions (it is part of a valid project)." },
            { "MANDATORY", "mandatory" }
        };

        const float spacing = 10f;
        const float labelWidth = 250f;
        const string projectSettingsPath = "ProjectSettings";
        const string settingsFileName = "ExportProjectToZipSettings.json";
        static readonly List<string> mandatoryFolders = new List<string> { "Library" }; // List of folders that cannot be removed from the exclusions
        static readonly List<string> forbiddenFolders = new List<string> { "Assets", "Packages", "ProjectSettings" }; // List of folders that cannot be excluded
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
            LoadSettings();
        }

        /// <summary>
        /// GUI callback for drawing the settings provider GUI.
        /// </summary>
        /// <param name="searchContext">The search context for the GUI.</param>
        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            GUILayout.Space(spacing);
            GUILayout.BeginHorizontal();
            GUILayout.Space(spacing);
            GUILayout.BeginVertical();

            DrawNamingSection();
            GUILayout.Space(spacing);

            DrawInclusionsSection();
            GUILayout.Space(spacing);

            DrawFolderExclusionsSection();
            GUILayout.Space(spacing);

            DrawExtensionExclusionsSection();
            GUILayout.Space(spacing);

            DrawRestoreDefaultsButton();
            GUILayout.Space(spacing);

            if (shouldSaveSettings) SaveSettings();

            DrawAboutSection();

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
            EditorGUILayout.LabelField(new GUIContent(messages["RENAME_ZIP"], messages["RENAME_ZIP_TOOLTIP"]), GUILayout.Width(labelWidth));
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
            EditorGUILayout.LabelField(new GUIContent(messages["INCLUDE_BUILDS"], messages["INCLUDE_BUILDS_TOOLTIP"]), GUILayout.Width(labelWidth));
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
            EditorGUILayout.LabelField(messages["FOLDERS_TO_EXCLUDE"], EditorStyles.boldLabel);
            for (int i = 0; i < Settings.foldersToExclude.Count; i++)
            {
                GUILayout.BeginHorizontal();
                string folder = Settings.foldersToExclude[i];
                bool isMandatory = mandatoryFolders.Contains(folder);

                EditorGUI.BeginDisabledGroup(isMandatory);
                string displayFolder = isMandatory ? folder + $" [{messages["MANDATORY"]}]" : folder;
                string validatedFolder = ValidateFolder(EditorGUILayout.TextField(displayFolder));
                if (!string.IsNullOrEmpty(validatedFolder) && !isMandatory)
                {
                    Settings.foldersToExclude[i] = validatedFolder;
                    Settings.foldersToExclude.Sort();
                }
                EditorGUI.EndDisabledGroup();

                if (!isMandatory && GUILayout.Button("-", GUILayout.Width(20)))
                {
                    Settings.foldersToExclude.RemoveAt(i);
                    shouldSaveSettings = true;
                }
                GUILayout.EndHorizontal();
            }
            // Add new folder exclusion
            GUILayout.BeginHorizontal();
            newFolderToExclude = EditorGUILayout.TextField(newFolderToExclude);
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                string validatedFolder = ValidateFolder(newFolderToExclude);
                if (!string.IsNullOrEmpty(validatedFolder) && !Settings.foldersToExclude.Contains(validatedFolder))
                {
                    Settings.foldersToExclude.Add(validatedFolder);
                    Settings.foldersToExclude.Sort();
                    newFolderToExclude = string.Empty;
                    shouldSaveSettings = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Creates the extension exclusions section of the settings GUI.
        /// </summary>
        void DrawExtensionExclusionsSection()
        {
            EditorGUILayout.LabelField(messages["EXTENSIONS_TO_EXCLUDE"], EditorStyles.boldLabel);
            for (int i = 0; i < Settings.topLevelExtensionsToExclude.Count; i++)
            {
                GUILayout.BeginHorizontal();
                string validatedExtension = ValidateExtension(EditorGUILayout.TextField(Settings.topLevelExtensionsToExclude[i]));
                if (!string.IsNullOrEmpty(validatedExtension))
                {
                    Settings.topLevelExtensionsToExclude[i] = validatedExtension;
                    Settings.topLevelExtensionsToExclude.Sort();
                }
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    Settings.topLevelExtensionsToExclude.RemoveAt(i);
                    shouldSaveSettings = true;
                }
                GUILayout.EndHorizontal();
            }
            // Add new extension exclusion
            GUILayout.BeginHorizontal();
            newExtensionToExclude = EditorGUILayout.TextField(newExtensionToExclude);
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                string validatedExtension = ValidateExtension(newExtensionToExclude);
                if (!string.IsNullOrEmpty(validatedExtension) && !Settings.topLevelExtensionsToExclude.Contains(validatedExtension))
                {
                    Settings.topLevelExtensionsToExclude.Add(validatedExtension);
                    Settings.topLevelExtensionsToExclude.Sort();
                    newExtensionToExclude = string.Empty;
                    shouldSaveSettings = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Creates the restore defaults button of the settings GUI.
        /// </summary>
        void DrawRestoreDefaultsButton()
        {
            if (GUILayout.Button(messages["RESTORE_DEFAULTS"]))
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
            GUILayout.Label(messages["ABOUT"], EditorStyles.wordWrappedLabel);
            if (GUILayout.Button(messages["MORE_INFO"] + messages["REPOSITORY_LINK"], EditorStyles.linkLabel)) Application.OpenURL(messages["REPOSITORY_LINK"]);
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Loads the settings from the settings file or creates new default settings if the file doesn't exist.
        /// </summary>
        static void LoadSettings()
        {
            string filePath = Path.Combine(projectSettingsPath, settingsFileName);

            if (!Directory.Exists(projectSettingsPath)) Directory.CreateDirectory(projectSettingsPath);

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                settings = JsonUtility.FromJson<ExportProjectToZipSettings>(json);
            }
            else
            {
                settings = new ExportProjectToZipSettings();
                SaveSettings();
            }
        }

        /// <summary>
        /// Saves the settings to the settings file.
        /// </summary>
        static void SaveSettings()
        {
            string filePath = Path.Combine(projectSettingsPath, settingsFileName);
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
            var provider = new ExportProjectToZipSettingsProvider("Project/Export Project to Zip", SettingsScope.Project);
            return provider;
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
            if(shouldSaveNow) SaveSettings();
        }

        /// <summary>
        /// Validates an extension string.
        /// </summary>
        /// <param name="extension">The extension string to validate.</param>
        /// <returns>The validated extension string (empty if invalid).</returns>
        string ValidateExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return string.Empty; // Block empty extension

            extension = extension.Trim(); // Remove leading and trailing spaces

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

            if (forbiddenFolders.Contains(folder))
            {
                EditorUtility.DisplayDialog(messages["INVALID_FOLDER_TITLE"], string.Format(messages["INVALID_FOLDER"], folder), "OK");
                return string.Empty; // Forbidden folder
            }

            foreach (char c in folder)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-') return string.Empty; // Block invalid characters
            }

            return folder;
        }
    }
}
#endif