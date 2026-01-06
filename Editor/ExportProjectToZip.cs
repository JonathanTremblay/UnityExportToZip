#if UNITY_EDITOR
namespace ExportProjectToZip
{
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// ExportProjectToZip allows you to easily export an entire Unity project to a Zip file.
    /// It will exclude unnecessary files from the Library folder, preserving only:
    ///   • LastSceneManagerSetup.txt (stores the last accessed scene)
    ///   • EditorUserBuildSettings.asset (stores the build settings)
    /// Note that other Library files can be recreated by Unity.
    ///
    /// It will also exclude the following folders: .git, .vs, .vscode, Build, Builds, Logs, obj, Obj, UserSettings, Temp.
    /// Additionally, it will exclude all .gitignore, .csproj, .sln and .zip files at the top level of the project.
    /// Exclusions can be changed in Project Settings.
    ///
    /// The scripts must be placed in an Editor folder (inside the Assets or Packages folder).
    /// To compress a project, simply select "Export Project to Zip..." from the File menu,
    /// then choose the name and location for the Zip file.
    ///
    /// Created by Jonathan Tremblay, teacher at Cegep de Saint-Jerome.
    /// This project is available for distribution and modification under the CC0 License.
    /// https://github.com/JonathanTremblay/UnityExportToZip
    /// </summary>
    public class ExportProjectToZip
    {
        static readonly string currentVersion = "Version 1.1.5 (2026-01)";

        static string projectName; //The Unity project name, based on the name of the root folder of the project. Will be used within the zip archive.
        static string projectPath; //The path to the root folder of the project.
        static string zipName; //The name of the zip file to create or replace (with the extension).
        static string zipNameWithoutExt; //The name of the zip file (without the extension).
        static string zipFullPath; //The full path of the zip file to create (with the filename and the extension).
        static string oldZipFullPath; //The temporary full path of the old zip file to replace.
        static List<string> filesToZip; //The list of all the files to zip in the project folder.
        static List<string> unzippableLibraryFiles; //The list of all the files that cannot be zipped (for example, if they are in use).
        static DateTime lastRefreshWorkaroundTime = DateTime.MinValue;

        [MenuItem("File/Export Project to Zip...  %&s", false, 199)] //Add a menu item named "Export Project to Zip..." to the File menu, with the shortcut Ctrl+Alt+S
        /// <summary>
        /// Exports the entire project to a zip file when the menu item is selected.
        /// </summary>
        public static void ExportToZip()
        {
            bool shouldContinue;

            // Checking and saving unsaved files
            projectPath = FixLongPath(Directory.GetCurrentDirectory());
            projectName = Path.GetFileName(projectPath);

            shouldContinue = CheckForUnsavedFiles();
            if (!shouldContinue)
            {
                ShowError("The project has not been exported.");
                return;
            }

            //Clearing unzippableFiles list:
            unzippableLibraryFiles = new();

            // Choosing zip name and path
            zipName = projectName + ".zip";
            zipFullPath = FixLongPath(EditorUtility.SaveFilePanel("Export project to zip", projectPath, zipName, "zip"));
            if (string.IsNullOrEmpty(zipFullPath))
            {
                // User has pressed the cancel button in the SaveFilePanel
                return;
            }
            zipName = Path.GetFileName(zipFullPath);
            zipNameWithoutExt = Path.GetFileNameWithoutExtension(zipFullPath);

            // Temporarily renaming existing zip file
            shouldContinue = RenameExistingZip();
            if (!shouldContinue)
            {
                ShowError("The project has not been exported.");
                return;
            }

            // Force Build(s) folder to be included or excluded (for older versions of the package)
            ExportProjectToZipSettingsProvider.UpdateBuildFoldersExclusion(true);

            // Find files to add
            List<string> exceptionList = ExportProjectToZipSettingsProvider.Settings.foldersToExclude.Select(folder => FixLongPath(GetFolderFullPath(folder))).ToList();
            string[] topLevelFiles = Directory.GetFiles(projectPath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string file in topLevelFiles)
            {
                if (ExportProjectToZipSettingsProvider.Settings.topLevelExtensionsToExclude.Contains(Path.GetExtension(file)))
                {
                    exceptionList.Add(FixLongPath(file));
                }
            }
            filesToZip = Directory.EnumerateFiles(projectPath, "*.*", SearchOption.AllDirectories)
                .Where(d => exceptionList.All(e => !d.StartsWith(e)))
                .Select(FixLongPath)
                .ToList();

            string lastSceneFullPath = FixLongPath(Path.Combine(projectPath, "Library", "LastSceneManagerSetup.txt"));
            if (File.Exists(lastSceneFullPath) && !filesToZip.Contains(lastSceneFullPath)) // It exists but is not already in the list
            {
                filesToZip.Add(lastSceneFullPath);
            }

            string buildSettingsFullPath = FixLongPath(Path.Combine(projectPath, "Library", "EditorUserBuildSettings.asset"));
            if (File.Exists(buildSettingsFullPath) && !filesToZip.Contains(buildSettingsFullPath)) // It exists but is not already in the list
            {
                filesToZip.Add(buildSettingsFullPath);
            }

            // Adding files to the archive
            bool hasBeenCompleted;
            using (ZipArchive zip = ZipFile.Open(zipFullPath, ZipArchiveMode.Create))
            {
                hasBeenCompleted = AddFilesToZip(zip, filesToZip);
            }

            if (hasBeenCompleted)
            {
                string message = $"<b>SUCCESS!</b> The project was successfully exported (the Zip contains {filesToZip.Count} files). {FixPathForMac(zipFullPath)} \n** Export Project to Zip is free and open source. For updates and feedback, visit https://github.com/JonathanTremblay/UnityExportToZip. **\n** {currentVersion} **";
                int unzippableCount = unzippableLibraryFiles.Count;
                if (unzippableCount > 0) message += $"\n<b>EXPERIMENTAL!</b> The following {unzippableCount} Library files were skipped:\n" + string.Join("\n", unzippableLibraryFiles);
                Debug.Log(message);
            }
            else
            {
                ShowError("The compression has been cancelled before completion.");
                DeleteNewZip();
            }

            // Deleting old zip file
            DeleteOrRestoreOldZip();
        }

        /// <summary>
        /// Creates a full path to a folder within the project folder, including a trailing separator.
        /// </summary>
        /// <param name="folderName">The name of the folder.</param>
        /// <returns>The full path to the folder with a trailing separator.</returns>
        static string GetFolderFullPath(string folderName)
        {
            string folderFullPath = Path.Combine(projectPath, folderName, "X"); //add a separator at the end to avoid matching a file starting with the same name
            folderFullPath = folderFullPath.Substring(0, folderFullPath.Length - 1); //remove last X but keep the separator
            return folderFullPath;
        }

        /// <summary>
        /// Checks for unsaved files in the project or active scene and prompts the user to save these files.
        /// </summary>
        /// <returns>True if no files were saved or all files were saved successfully, 
        /// false if the user chose not to save the files or if an error occurred while saving.</returns>
        static bool CheckForUnsavedFiles()
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
            return true;
        }

        /// <summary>
        /// Check if the project name is empty.
        /// </summary>
        /// <returns>True if the project name is empty, false otherwise.</returns>
        static bool CheckIfProjectNameIsEmpty()
        {
            if (projectName == "")
            {
                ShowError($"The project name cannot be empty.\nChange the project name and try again.");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if any assets in the project have unsaved changes.
        /// Excludes shaders and custom render textures, because they are always marked as dirty.
        /// </summary>
        /// <returns>Returns true if any assets in the project have unsaved changes, or false if all assets are saved.</returns>
        static bool CheckIfProjectNeedsToBeSaved()
        {
            string[] allAssetsPaths = AssetDatabase.GetAllAssetPaths();
            foreach (string assetPath in allAssetsPaths) //loop on each asset
            {
                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                bool isValidAssetType = (assetType != null) && (assetType != typeof(Shader)) && (assetType != typeof(UnityEngine.CustomRenderTexture));
                if (assetPath.StartsWith("Assets") && isValidAssetType) //checks only in the Assets folder, excludes shaders and custom render textures
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
        /// Prompts the user to save the project if it has unsaved changes, and saves it if desired.
        /// </summary>
        /// <returns>Returns true if the project was saved or if the user chose not to save it, or false if an error occurred while trying to save.</returns>
        static bool SaveProjectIfDesired()
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
        /// Add a list of files to a zip archive.
        /// </summary>
        /// <param name="zip">The zip archive to add the files to.</param>
        /// <param name="fileList">The list of files to add to the zip archive.</param>
        /// <returns>True if all files were added to the zip file, false if an error occured or if the operation has been cancelled by the user.</returns>
        static bool AddFilesToZip(ZipArchive zip, List<string> fileList)
        {
            int fileCount = fileList.Count();
            string details = "";
            for (int i = 0; i < fileCount; i++)
            {
                string file = fileList[i];
                string fileRelativePath = Path.GetRelativePath(projectPath, file);
                float ratio = (i + 1f) / fileCount;
                details = $"Zipping file {i + 1} of {fileCount} ({(int)(ratio * 100)}%)... [{fileRelativePath}]";
                PauseForProgressBarRefresh(file);
                if (EditorUtility.DisplayCancelableProgressBar("Compressing files", details, ratio))
                {
                    //user has pressed the cancel button on the ProgressBar
                    EditorUtility.ClearProgressBar();
                    EditorUtility.FocusProjectWindow(); //prevents editor from losing focus when cancel button is used
                    return false;
                }
                try
                {
                    string folderName = (ExportProjectToZipSettingsProvider.Settings.shouldNameRootLevelFolderWithZipName) ? zipNameWithoutExt : projectName;
                    string combinedPath = Path.Combine(folderName, fileRelativePath);
                    combinedPath = FixPathForMac(combinedPath);
                    zip.CreateEntryFromFile(file, combinedPath);
                }
                catch (IOException exception)
                {
                    //if it is a file in use from the Library folder, it will be added to the unzippableFiles list:
                    if (file.StartsWith(Path.Combine(projectPath, "Library")))
                    {
                        unzippableLibraryFiles.Add(file);
                    }
                    else
                    {
                        ShowError($"An error occurred while adding the file to the zip archive: {exception.Message}\nThe project was not exported.");
                        return false;
                    }
                }
                catch (Exception exception)
                {
                    ShowError($"An unknown error occurred: {exception.Message}\nThe project was not exported.");
                    return false;
                }
            }
            EditorUtility.ClearProgressBar();
            return true;
        }

        /// <summary>
        /// Workaround for a bug in the Unity Editor that prevents the progress bar from updating.
        /// This method will add a delay in the execution to allow the progress bar to refresh.
        /// A small file gets a short delay, but a large file (25 MB or more) gets a long delay. 
        /// To speed up execution, short delays will be ignored if the last delay occurred recently.
        /// About the bug: https://forum.unity.com/threads/editorutility-displayprogressbar-not-showing-up-anymore.931875/
        /// </summary>
        /// <param name="file">The path to the file to be added to the zip archive.</param>
        static void PauseForProgressBarRefresh(string file)
        {
            int fileSizeInMb = (int)(new FileInfo(file).Length / 1000000);
            if (fileSizeInMb >= 25)
            {
                Thread.Sleep(100); //long pause (to be sure that the progress bar will show this specific filename)
                lastRefreshWorkaroundTime = DateTime.Now;
            }
            else if (lastRefreshWorkaroundTime.AddMilliseconds(200) < DateTime.Now)
            {
                Thread.Sleep(1); //short pause
                lastRefreshWorkaroundTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Save the current scene if desired by the user.
        /// </summary>
        /// <returns>True if the scene was saved or the user chose not to save it, false if an exception occurred while saving the scene.</returns>
        static bool SaveSceneIfDesired()
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
        /// Delete the zip file if it already exists.
        /// </summary>
        /// <returns>Returns true if the file was replaced or false if an error occurred while trying to delete the file.</returns>
        static bool RenameExistingZip()
        {
            oldZipFullPath = "";
            if (File.Exists(zipFullPath))
            {
                //note: if the file exists, the user has already given his ok to replace it, but it will be renamed first (deleted at the end)
                oldZipFullPath = zipFullPath + "-temp-old-delete.zip";
                DeleteOrRestoreOldZip(); //if another file with this temp name exist, it will be deleted first
                try
                {
                    File.Move(zipFullPath, oldZipFullPath);
                }
                catch (IOException exception)
                {
                    ShowError($"{exception.Message}\nThe existing zip file could not be accessed.");
                    return false;
                }
                catch (UnauthorizedAccessException exception)
                {
                    ShowError($"{exception.Message}\nThe existing zip file could not be accessed.");
                    return false;
                }
                catch (Exception exception)
                {
                    ShowError($"An unknown error occurred: {exception.Message}\nThe existing zip file could not be accessed.");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Delete the old zip file if it exists.
        /// </summary>
        static void DeleteOrRestoreOldZip()
        {
            if (File.Exists(oldZipFullPath) && oldZipFullPath != "")
            {
                if (File.Exists(zipFullPath)) //there is a new file, so the old zip should be deleted
                {
                    try
                    {
                        File.Delete(oldZipFullPath);
                    }
                    catch (IOException exception)
                    {
                        ShowError($"{exception.Message} \n(Please delete manually this old file.)");
                    }
                    catch (UnauthorizedAccessException exception)
                    {
                        ShowError($"{exception.Message} \n(Please delete manually this old file.)");
                    }
                    catch (Exception exception)
                    {
                        ShowError($"{exception.Message} \n(Please delete manually this old file.)");
                    }
                }
                else //there is no new file, so the old zip should be restored
                {
                    try
                    {
                        File.Move(oldZipFullPath, zipFullPath);
                    }
                    catch (IOException exception)
                    {
                        ShowError($"{exception.Message}\nThe name of the old zip file could not be restored. (Please rename it manually.)");
                    }
                    catch (UnauthorizedAccessException exception)
                    {
                        ShowError($"{exception.Message}\nThe name of the old zip file could not be restored. (Please rename it manually.)");
                    }
                    catch (Exception exception)
                    {
                        ShowError($"An unknown error occurred: {exception.Message}\nThe name of the old zip file could not be restored. (Please rename it manually.)");
                    }
                }
            }
        }

        /// <summary>
        /// Delete the new zip file if it exists. This method is called when the user clicks on cancel.
        /// </summary>
        static void DeleteNewZip()
        {
            if (File.Exists(zipFullPath)) //there is a new file to delete
            {
                try
                {
                    File.Delete(zipFullPath);
                }
                catch (IOException exception)
                {
                    ShowError($"{exception.Message} \n(Please delete manually this incomplete file.)");
                }
                catch (UnauthorizedAccessException exception)
                {
                    ShowError($"{exception.Message} \n(Please delete manually this incomplete file.)");
                }
                catch (Exception exception)
                {
                    ShowError($"{exception.Message} \n(Please delete manually this incomplete file.)");
                }
            }
        }

        /// <summary>
        /// Replaces the directory separator (\ on Windows) 
        /// with the alternate directory separator (/ on both platform),
        /// which works for Windows and Mac.
        /// </summary>
        /// <param name="path">The path to fix.</param>
        /// <returns>A string representing the fixed path with the alternate directory separator.</returns>
        static string FixPathForMac(string path)
        {
            if (path.StartsWith(@"\\?\")) path = path.Substring(4); //remove the "\\?\" prefix if it exists
            return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>
        /// Displays an error message to the user.
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        static void ShowError(string message)
        {
            EditorUtility.DisplayDialog("FAILURE", "ERROR!\n" + message, "Ok");
            Debug.Log("<b>ERROR!</b> \n" + message);
        }

        /// <summary>
        /// On Windows, adds the "\\?\" prefix if necessary.
        /// </summary>
        /// <param name="path">The path to check and fix.</param>
        public static string FixLongPath(string path)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor) //Windows only
            {
                if (!path.StartsWith(@"\\?\") && !string.IsNullOrEmpty(path)) //not already prefixed and not empty
                {
                    path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    return @"\\?\" + Path.GetFullPath(path);
                }
            }
            return path;
        }
    }
}
#endif