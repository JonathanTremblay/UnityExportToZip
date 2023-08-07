#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ExportProjectToZip
{
    /// <summary>
    /// Settings provider for the ExportProjectToZip settings 
    /// (accessible from Edit > Project Settings > Export Project to Zip)
    /// Saves the settings in a json file in the ProjectSettings folder.
    /// </summary>
    public class ExportProjectToZipSettingsProvider : SettingsProvider
    {
        const string aboutMessage = "ExportProjectToZip allows to easily export an entire Unity project to a Zip file.";
        const string repositoryLink = "https://github.com/JonathanTremblay/UnityExportToZip";
        const float spacing = 10f;
        const string projectSettingsPath = "ProjectSettings";
        const string settingsFileName = "ExportProjectToZipSettings.json";

        static private ExportProjectToZipSettings settings;

        static private bool shouldSaveSettings = false;

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

            // INCLUSIONS
            bool isSelected;
            isSelected = EditorGUILayout.Toggle(new GUIContent("Include Build(s) Folder(s)", "Include Build and/or Builds folders in the zip archive."), Settings.shouldIncludeBuilds);
            //Debug.Log("Settings.shouldIncludeBuilds: " + Settings.shouldIncludeBuilds + ", isSelected: " + isSelected);
            if (Settings.shouldIncludeBuilds != isSelected)
            {
                Settings.shouldIncludeBuilds = isSelected;
                shouldSaveSettings = true;
            }
            GUILayout.Space(spacing);

            // NAMING
            isSelected = EditorGUILayout.Toggle(new GUIContent("Rename Root Folder", "Name the root level folder in the archive with the name of the zip file."), Settings.shouldNameRootLevelFolderWithZipName, GUILayout.ExpandWidth(true));
            if (Settings.shouldNameRootLevelFolderWithZipName != isSelected)
            {
                Settings.shouldNameRootLevelFolderWithZipName = isSelected;
                shouldSaveSettings = true;
            }
            GUILayout.Space(spacing);

            if (GUILayout.Button("Restore Defaults"))
            {
                Settings.RestoreDefaults();
                shouldSaveSettings = true;
            }
            GUILayout.Space(spacing);

            if(shouldSaveSettings) SaveSettings();

            // ABOUT
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(aboutMessage, EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("More info: " + repositoryLink, EditorStyles.linkLabel)) Application.OpenURL(repositoryLink);
            GUILayout.EndVertical();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Loads the settings from the settings file or creates new default settings if the file doesn't exist.
        /// </summary>
        static private void LoadSettings()
        {
            string filePath = Path.Combine(projectSettingsPath, settingsFileName);

            if (!Directory.Exists(projectSettingsPath))
            {
                Directory.CreateDirectory(projectSettingsPath);
            }

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
        static private void SaveSettings()
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
    }
}
#endif