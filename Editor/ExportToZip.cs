using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ExportToZip allows to easily export an entire Unity project to a Zip file, directly in the project folder. 
/// The script will exclude all zip files at the top level of the project. 
/// It will also exclude the following folders: .git, Library, Logs, Temp.
/// Created by Jonathan Tremblay, teacher at Cegep de Saint-Jerome.
/// This project is available for distribution and modification under the CC0 License.
/// To give feedback and find future versions: https://github.com/JonathanTremblay/UnityExportToZip
/// </summary>
public class ExportToZip : EditorWindow
{
    bool isExpertMode = false; //In expert mode, user can enter prefix, suffix, and separator for the project name.
    string prefix = "ID"; //The prefix for the project name.
    string projectName = "--"; //The project name. The default value "--" will be replaced with the folder name. 
    string suffix = "V1"; //The suffix for the project name.
    string sep = "_"; //The separator to be used between the prefix, project name, and suffix.
    string version = "CC0 - TIM CSTJ 0.81 (2022-12)"; //The version of the ExportToZip class.

    [MenuItem("File/Export to zip...", false, 199)] //Add an item in the file menu to call ShowWindow
    /// <summary>
    /// Creates a new window of type "ExportToZip" and sets its title and size.
    /// </summary>
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow<ExportToZip>(true, "Export to zip");
        window.minSize = new Vector2(400, 200);
        window.maxSize = new Vector2(600, 300);
    }

    /// <summary>
    /// GUI event called whenever the GUI is rendered.
    /// </summary>
    void OnGUI()
    {
        string projectPath = System.IO.Directory.GetCurrentDirectory();
        string projectFolderName = System.IO.Path.GetFileName(projectPath);
        if (projectName == "--") { projectName = projectFolderName; }

        //styles definitions:
        GUIStyle styleForDetails = new GUIStyle();
        styleForDetails.fontSize = 10;
        styleForDetails.normal.textColor = Color.gray;
        styleForDetails.margin = new RectOffset(5, 0, 0, 0);
        GUIStyle styleForButton = new GUIStyle(GUI.skin.button);
        styleForButton.fontStyle = FontStyle.Bold;
        styleForDetails.normal.textColor = Color.yellow;

        GUILayout.Label("This tool will export the entire project to a Zip file, \ndirectly in the project folder.", EditorStyles.boldLabel);
        GUILayout.Label("(Excluded files : *.zip, .git, Library, Logs, Temp.)", styleForDetails);
        isExpertMode = EditorGUILayout.Toggle("Use prefix and suffix?", isExpertMode);
        if (isExpertMode) prefix = EditorGUILayout.TextField("Project name prefix", prefix);
        projectName = EditorGUILayout.TextField("Project name", projectName);
        string zipName = projectName + ".zip";
        if (isExpertMode)
        {
            suffix = EditorGUILayout.TextField("Project name suffix", suffix);
            sep = EditorGUILayout.TextField("Separator to use", sep);
            zipName = ((prefix != "") ? prefix + sep : "") + projectName + ((suffix != "") ? sep + suffix : "") + ".zip";
        }

        string zipFullPath = Path.Combine(projectPath, zipName);
        bool zipAlreadyExists = File.Exists(zipFullPath);

        if (zipAlreadyExists)
        {
            GUILayout.Label($"Warning: A file named \"{zipName}\" already exist.", styleForDetails);
        }
        else if (projectName == "")
        {
            GUILayout.Label($"Warning: The project name cannot be empty.", styleForDetails);
        }
        else
        {
            styleForDetails.normal.textColor = Color.white;
            GUILayout.Label($"A file named \"{zipName}\" will be created.", styleForDetails);
        }

        if (GUILayout.Button("Export the project to zip", styleForButton))
        {
            if (CheckIfProjectNameIsEmpty(projectName)) { return; }
            if (CheckIfProjectNeedsToBeSaved()) //at least one asset needs to be saved
            {
                bool shouldContinue = SaveProjectIfDesired();
                if (!shouldContinue) return;
            }
            if (EditorSceneManager.GetActiveScene().isDirty) //the scene needs to be saved
            {
                bool shouldContinue = SaveSceneIfDesired();
                if (!shouldContinue) return;
            }
            if (zipAlreadyExists)
            {
                bool shouldContinue = ReplaceZipIfUserAccepts(zipName, zipFullPath);
                if (!shouldContinue) return;
            }
            List<string> exceptionList = new List<string>() { Path.Combine(projectPath, ".git"), Path.Combine(projectPath, "Library"), Path.Combine(projectPath, "Logs"), Path.Combine(projectPath, "Temp") };
            string[] topLevelFiles = Directory.GetFiles(projectPath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string file in topLevelFiles) { if (Path.GetExtension(file) == ".zip") { exceptionList.Add(file); } }
            List<string> fileList = Directory.EnumerateFiles(projectPath, "*.*", SearchOption.AllDirectories).Where(d => exceptionList.All(e => !d.StartsWith(e))).ToList();
            string lastSceneFullPath = Path.Combine(projectPath, "Library", "LastSceneManagerSetup.txt");
            if (File.Exists(lastSceneFullPath)) { fileList.Add(lastSceneFullPath); }

            using (ZipArchive zip = ZipFile.Open(zipName, ZipArchiveMode.Create))
            {
                AddFilesToZipFile(zip, fileList, projectPath, projectFolderName, zipName);
            }
        }
        styleForDetails.normal.textColor = Color.gray;
        GUILayout.Label(version, styleForDetails);
    }

    /// <summary>
    /// Add a list of files to a zip archive.
    /// </summary>
    /// <param name="zip">The zip archive to add the files to.</param>
    /// <param name="fileList">The list of files to add to the zip archive.</param>
    /// <param name="projectPath">The path to the root folder of the project.</param>
    /// <param name="projectFolderName">The name of the root folder of the project within the zip archive.</param>
    /// <param name="zipName">The name of the zip file (with the extension).</param>
    private void AddFilesToZipFile(ZipArchive zip, List<string> fileList, string projectPath, string projectFolderName, string zipName)
    {
        foreach (string file in fileList)
        {
            string fileRelativePath = Path.GetRelativePath(projectPath, file);
            try
            {
                zip.CreateEntryFromFile(file, Path.Combine(projectFolderName, fileRelativePath));
            }
            catch (IOException exception)
            {
                ShowError($"An error occurred while adding the file to the zip archive: {exception.Message}\nThe project was not exported.");
                return;
            }
            catch (Exception exception)
            {
                ShowError($"An unknown error occurred: {exception.Message}\nThe project was not exported.");
                return;
            }
        }

        EditorUtility.DisplayDialog("SUCCESS", $"EXCELLENT!\nThe project was exported correctly in the {zipName} file.\n(The archive contains {fileList.Count()} files.)", "Ok");
        Close(); //close the window
    }

    /// <summary>
    /// Save the current scene if desired by the user.
    /// </summary>
    /// <returns>True if the scene was saved or the user chose not to save it, false if an exception occurred while saving the scene.</returns>
    private bool SaveSceneIfDesired()
    {
        if (EditorUtility.DisplayDialog("Warning", "The current scene has not been saved. Would you like to save it before zipping the project?", "Yes", "No"))
        {
            try
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }
            catch (Exception exception)
            {
                ShowError($"{exception.Message}\nThe scene has not been saved.");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Check if the project name is empty.
    /// </summary>
    /// <param name="projectName">The project name to check.</param>
    /// <returns>True if the project name is empty, false otherwise.</returns>
    private bool CheckIfProjectNameIsEmpty(string projectName)
    {
        if (projectName == "")
        {
            ShowError($"The project name cannot be empty.\nChange the project name and try again.");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Prompts the user to replace the zip file if it already exists, and replaces it if desired.
    /// </summary>
    /// <param name="zipName">The name of the zip file (with the extension).</param>
    /// <param name="zipFullPath">The full path of the file to replace.</param>
    /// <returns>Returns true if the file was replaced or if the user chose not to replace it, or false if an error occurred while trying to delete the file.</returns>
    private bool ReplaceZipIfUserAccepts(string zipName, string zipFullPath)
    {
        if (EditorUtility.DisplayDialog("Warning", $"The file {zipName} already exists. Do you want to replace it?", "Yes", "No"))
        {
            try
            {
                File.Delete(zipFullPath);
            }
            catch (IOException exception)
            {
                ShowError($"{exception.Message}\nThe project was not exported.");
                return false;
            }
            catch (UnauthorizedAccessException exception)
            {
                ShowError($"{exception.Message}\nThe project was not exported.");
                return false;
            }
            catch (Exception exception)
            {
                ShowError($"An unknown error occurred: {exception.Message}\nThe project was not exported.");
                return false;
            }
            return true;
        }
        else
        {
            ShowError("The project was not exported.");
            return false;
        }
    }

    /// <summary>
    /// Prompts the user to save the project if it has unsaved changes, and saves it if desired.
    /// </summary>
    /// <returns>Returns true if the project was saved or if the user chose not to save it, or false if an error occurred while trying to save.</returns>
    private bool SaveProjectIfDesired()
    {
        if (EditorUtility.DisplayDialog("Warning", "The project has not been saved. Would you like to save it before zipping the project?", "Yes", "No"))
        {
            try
            {
                AssetDatabase.SaveAssets();
            }
            catch (Exception exception)
            {
                ShowError($"{exception.Message}\nThe project has not been saved.");
                return false;
            }
            AssetDatabase.Refresh();
        }
        return true;
    }

    /// <summary>
    /// Checks if any assets in the project have unsaved changes.
    /// </summary>
    /// <returns>Returns true if any assets in the project have unsaved changes, or false if all assets are saved.</returns>
    private bool CheckIfProjectNeedsToBeSaved()
    {
        string[] allAssetsPaths = AssetDatabase.GetAllAssetPaths();
        foreach (string assetPath in allAssetsPaths) //loop on each asset
        {
            if (assetPath.Contains("Assets")) //eliminates checks on Packages
            {
                if (EditorUtility.IsDirty(AssetDatabase.LoadAssetAtPath(assetPath, typeof(object))))
                {
                    return true; //this asset needs to be saved, no need to check other assets
                }
            }
        }
        return false; //no assets to save
    }

    /// <summary>
    /// Displays an error message to the user.
    /// </summary>
    /// <param name="message">The message to display to the user.</param>
    private void ShowError(string message)
    {
        EditorUtility.DisplayDialog("FAILURE", "ERROR!\n" + message, "Ok");
    }
}