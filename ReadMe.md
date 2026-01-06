# Unity ExportProjectToZip

![Unity Version](https://img.shields.io/badge/unity-2021.3.16%2B-000000.svg?style=flat-square&logo=unity)
![License](https://img.shields.io/badge/license-CC0-green.svg?style=flat-square)

This Unity Editor tool lets you easily export an entire project to a zip file directly from the File menu.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Technical Details](#technical-details)
3. [Compatibility](#compatibility)
4. [Known Issues](#known-issues)
5. [About the Project](#about-the-project)
6. [Contact](#contact)
7. [Version History](#version-history)
8. [License](#license)

## Getting Started

Import this lightweight package to your project, and you’re ready to zip!

To use it:
1. Simply select "Export Project to Zip..." from the File menu (Ctrl+Alt+S). 
2. If your project or scene needs saving, you will be prompted to save (optional).
3. Then choose the name and location for the zip file. 
4. Sit back and watch the progress bar as the compression is done (can be cancelled).

That's it!

## Technical Details

* Integrates directly in the File menu.
* Detects if scene or project needs saving.
* Compression can be cancelled.
* Compatible with both Mac and Windows.
* No additional software needed.
* Adds only the required files to the archive:
  * Excludes unnecessary files from the Library folder. Only two files from the Library folder are preserved: `LastSceneManagerSetup.txt` (which stores the last accessed scene) and `EditorUserBuildSettings.asset` (which stores the build settings). Note that other Library files can be recreated by Unity.
  * Additionally, it excludes the following folders: `.git`, `.vs`, `.vscode`, `Build`, `Builds`, `Logs`, `obj`, `Obj`, `UserSettings`, `Temp`.
  * It also excludes all `.gitignore`, `.csproj`, `.sln`, and `.zip` files at the top level of the project.
  * Exclusions can be changed in Project Settings.

## Compatibility

* Tested with Unity versions ranging from 2021.3.16 to 6.3.
* Compatible with both Mac and Windows.
* Projects larger than 8 GB were compressed successfully.

## Known Issues

* The progress bar could be more responsive when compressing (Unity editor bug).
* If the project contains large files, the export may take a long time.

## About the Project

I created this tool to help my students move their Unity projects between computers and to make it easier for them to hand in their assignments. As a teacher, I noticed that the huge size of the Library folder in a project can be difficult to manage for new users. While the Library folder can be recreated if deleted, it is not an intuitive process for those unfamiliar with Unity. 

This tool simplifies the transfer and submission of Unity projects, making it easier for my students to focus on learning and creating. It can also be useful for seasoned game developers!

## Contact

**Jonathan Tremblay**  
Teacher, Cegep de Saint-Jerome  
jtrembla@cstj.qc.ca

Project Repository: [https://github.com/JonathanTremblay/UnityExportToZip](https://github.com/JonathanTremblay/UnityExportToZip)  
Unity Asset Store: [https://assetstore.unity.com/packages/tools/utilities/export-project-to-zip-243983](https://assetstore.unity.com/packages/tools/utilities/export-project-to-zip-243983)

## Version History

* 1.1.5
    * Fixed potential file access errors when loading Project Settings.
* 1.1.4
    * Improved execution speed.
    * Fixed an issue with Project Settings for folder names with a dot.
    * Added an experimental feature to include the Library folder.
* 1.1.3
    * Fixed an issue where cancelling the file name prompt caused an error.
    * Renamed asmdef file to match namespace.
* 1.1.2
    * Added support for paths longer than 260 characters.
* 1.1.1
    * Added exclusion customization settings.
* 1.1.0
    * Added settings to allow inclusion of Build or Builds folders, and control the renaming of the root folder.
    * Added an exception to keep Build Settings from the Library folder.
* 1.0.3
    * Added a shortcut (Ctrl+Alt+S) and fixed a compilation bug during build process.
* 1.0.2
    * Fixed an issue where certain top-level files (starting with excluded folder names) were not included (notably .gitignore).
* 1.0.1
    * Made minor changes to folder naming and exclusions.
    * Changed the default folder name inside the archive to use the archive name.
    * Added an option flag at the beginning the code to keep the original project name.
    * Added an option flag to exclude Build/Builds folders.
    * Added Obj to folder exclusions.
    * Added .sln to file exclusions.
    * Reorganized the project to follow the Packages folder standards.
    * Added support for installing the package from a Git repository.
* 1.0.0
    * Revised readme, added a namespace, and improved Mac compatibility.

## License

This tool is available for distribution and modification under the CC0 License, which allows for free use and modification.  
[https://creativecommons.org/share-your-work/public-domain/cc0/](https://creativecommons.org/share-your-work/public-domain/cc0/)