using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
    int processStep = 1;
    bool shouldCheckForUnsavedFiles = true;
    bool shouldAddTime = true;
    GUIStyle styleForDetails;
    GUIStyle styleForVersion;
    string projectName; //The project name. 
    string projectPath; //The path to the root folder of the project.
    string projectFolderName; //The name of the root folder of the project (will be used within the zip archive).
    string dateTimeString; //A string with the date and time of the export.
    string zipFullPath; //The full path of the file to create or replace.
    string zipName; //The name of the zip file to create or replace (with the extension).
    List<string> filesToZip;
    string version = "CC0 - TIM CSTJ 0.82 (2023-01)"; //The version of the ExportToZip class.

    [MenuItem("File/Export to zip...", false, 199)] //Add an item in the file menu to call ShowWindow (will be under Save project )
    /// <summary>
    /// Creates a new window of type "ExportToZip" and sets its title and size.
    /// </summary>
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow<ExportToZip>(true, "Export project to zip");
        window.minSize = new Vector2(400, 200);
        window.maxSize = new Vector2(600, 300);
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    /// <summary>
    /// GUI event called whenever the GUI is rendered.
    /// The processStep counter is used to give feedback to the user
    /// (it allows OnGUI to be called more than once during the process).
    /// </summary>
    void OnGUI()
    {
        if (processStep == 1) DefineStyles();

        GUILayout.Label("This tool will export the entire project to a Zip file.", EditorStyles.boldLabel);
        GUILayout.Label("(Excluded files : *.zip, .git, Library, Logs, Temp.)", styleForDetails);

        string processName;
        switch (processStep)
        {
            case 1: processName = "Initialising"; break;
            case 2:
            case 3: processName = "Checking project state"; break;
            case 4:
            case 5: processName = "Waiting for zip filename"; break;
            case 6:
            case 7: processName = "Checking filename validity"; break;
            case 8:
            case 9: processName = "Listing files to add"; break;
            case 10:
            default: processName = "Adding files to the archive"; break;
        }

        GUILayout.Label($"{processName} ({processStep})", styleForVersion);

        GUILayout.Label(version, styleForVersion);

        if (processStep == 3) //checking and saving unsaved files
        {
            projectPath = System.IO.Directory.GetCurrentDirectory();
            projectFolderName = System.IO.Path.GetFileName(projectPath);
            projectName = projectFolderName;

            if (shouldCheckForUnsavedFiles)
            {
                bool shouldContinue = CheckForUnsavedFiles();
                if (!shouldContinue)
                {
                    ShowError("The project has not been exported.");
                    Close();
                    return;
                }
            }
        }


        if (processStep == 5) //choosing zip name and path
        {
            zipName = projectName + ".zip";
            // if (shouldAddTime)
            // {
            //     string sep = "_"; 
            //     CultureInfo cultureInfo = CultureInfo.CurrentCulture;
            //     dateTimeString = DateTime.Now.ToString("G", cultureInfo).Replace(" ", sep);
            //     char[] invalidChars = Path.GetInvalidFileNameChars();
            //     string validDateTimeString = string.Join(sep, dateTimeString.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            //     zipName = (projectName + sep + validDateTimeString) + ".zip";
            // }
            zipFullPath = EditorUtility.SaveFilePanel("Export project to zip", projectPath, zipName, "zip");
            if (zipFullPath == "")
            {
                ShowError("The project has not been exported.");
                Close();
                return;
            }
            zipName = Path.GetFileName(zipFullPath);
        }

        if (processStep == 7) //managing existing zip file
        {
            bool shouldContinue = DeleteExistingZip();
            if (!shouldContinue)
            {
                ShowError("The project has not been exported.");
                Close();
                return;
            }
        }

        if (processStep == 9) //finding files to add
        {
            List<string> exceptionList = new List<string>() { Path.Combine(projectPath, ".git"), Path.Combine(projectPath, "Library"), Path.Combine(projectPath, "Logs"), Path.Combine(projectPath, "Temp") };
            string[] topLevelFiles = Directory.GetFiles(projectPath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string file in topLevelFiles) { if (Path.GetExtension(file) == ".zip") { exceptionList.Add(file); } }
            filesToZip = Directory.EnumerateFiles(projectPath, "*.*", SearchOption.AllDirectories).Where(d => exceptionList.All(e => !d.StartsWith(e))).ToList();
            string lastSceneFullPath = Path.Combine(projectPath, "Library", "LastSceneManagerSetup.txt");
            if (File.Exists(lastSceneFullPath)) { filesToZip.Add(lastSceneFullPath); }
        }

        if (processStep == 11) //adding files to the archive
        {
            using (ZipArchive zip = ZipFile.Open(zipFullPath, ZipArchiveMode.Create))
            {
                AddFilesToZipFile(zip, filesToZip);
            }
        }

        processStep++;
    }

    private void DefineStyles()
    {
        //styles definitions:
        styleForDetails = new GUIStyle();
        styleForDetails.fontSize = 10;
        styleForDetails.normal.textColor = Color.gray;
        styleForDetails.margin = new RectOffset(5, 0, 0, 0);
        styleForDetails.normal.textColor = Color.yellow;
        styleForVersion = new GUIStyle(styleForDetails);
        styleForVersion.normal.textColor = Color.gray;
    }

    private bool CheckForUnsavedFiles()
    {
        if (CheckIfProjectNameIsEmpty()) { return false; }
        if (CheckIfProjectNeedsToBeSaved()) //at least one asset needs to be saved
        {
            bool shouldContinue = SaveProjectIfDesired();
            if (!shouldContinue) return false;
        }
        if (EditorSceneManager.GetActiveScene().isDirty) //the scene needs to be saved
        {
            bool shouldContinue = SaveSceneIfDesired();
            if (!shouldContinue) return false;
        }
        shouldCheckForUnsavedFiles = false; //this should be checked only once
        return true;
    }

    /// <summary>
    /// Add a list of files to a zip archive.
    /// </summary>
    /// <param name="zip">The zip archive to add the files to.</param>
    /// <param name="fileList">The list of files to add to the zip archive.</param>
    private void AddFilesToZipFile(ZipArchive zip, List<string> fileList)
    {
        int fileCount = fileList.Count();
        for (int i = 0; i < fileCount; i++)
        {
            string file = fileList[i];
            string fileRelativePath = Path.GetRelativePath(projectPath, file);
            EditorUtility.DisplayProgressBar("Compressing files", $"Zipping file {i + 1} of {fileCount}...\n{fileRelativePath}", (i + 1) / fileCount);
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
            Thread.Sleep(1);
        }
        EditorUtility.ClearProgressBar();

        // foreach (string file in fileList)
        // {
        //     string fileRelativePath = Path.GetRelativePath(projectPath, file);
        //     try
        //     {
        //         zip.CreateEntryFromFile(file, Path.Combine(projectFolderName, fileRelativePath));
        //     }
        //     catch (IOException exception)
        //     {
        //         ShowError($"An error occurred while adding the file to the zip archive: {exception.Message}\nThe project was not exported.");
        //         return;
        //     }
        //     catch (Exception exception)
        //     {
        //         ShowError($"An unknown error occurred: {exception.Message}\nThe project was not exported.");
        //         return;
        //     }
        // }

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
    /// <returns>True if the project name is empty, false otherwise.</returns>
    private bool CheckIfProjectNameIsEmpty()
    {
        if (projectName == "")
        {
            ShowError($"The project name cannot be empty.\nChange the project name and try again.");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Delete the zip file if it already exists.
    /// </summary>
    /// <returns>Returns true if the file was replaced or false if an error occurred while trying to delete the file.</returns>
    private bool DeleteExistingZip()
    {
        if (File.Exists(zipFullPath))
        {
            //note: if the file exists, the user has already given his ok to replace it
            try
            {
                File.Delete(zipFullPath); //Delete the file (maybe it should be temporarily renamed and deleted at the end)
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
        }
        return true;
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