# Unity ExportProjectToZip

This is a Unity Editor script that allows easy export of an entire Unity project to a Zip file, directly from the Unity Editor file menu.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Technical Details](#technical-details)
3. [Compatibility](#compatibility)
4. [Known Issues](#known-issues)
5. [About the Project](#about-the-project)
6. [Contact](#contact)
7. [Version History](#version-history)
7. [License](#license)

## Getting Started

Import this lightweight package to your project (or manually add the script to an Editor folder in the Assets folder), and you’re ready to zip!

To use it:
1. Simply select "Export Project to Zip..." from the file menu. 
2. If your project or your scene needs saving, you will be prompted to save (optional).
3. Then choose the name and location for the Zip file. 
4. Sit back and watch the progress bar as the compression is done (can be cancelled).

That's it!

## Technical Details

* Integrates directly in the file menu
* Detects if scene or project needs saving
* Adds only the required project files to the archive\*
* Compression can be cancelled
* Compatible with both Mac and Windows
* No additional software needed

\* Excludes all zip files at the top level of the project, and also excludes the following folders: .git, Library, Logs, Obj, Temp. An exception to excluding the Library folder is the LastSceneManagerSetup.txt file, which is included to allow Unity to remember and load the last accessed scene when the project is reopened. Note that other Library files can be recreated by Unity.

## Compatibility

Tested on Mac and Windows with Unity versions 2021.3.16 (LTS), 2022.1.11 and 2022.2.1.
Projects larger than 8 GB were compressed successfully.

## Known Issues

* Progress bar could be more responsive when compressing (Unity editor bug).
* If the project contains large files, the export may take a long time.

## About the Project

I created this tool to help my students move their Unity projects between computers and to make it easier for them to hand in their assignments. As a teacher, I noticed that the huge size of the Library folder in a Unity project can be difficult to manage for new users. While the Library folder can be recreated if deleted, it is not an intuitive process for those unfamiliar with Unity. 

This tool simplifies Unity project transfer and submission, making it easier for my students to focus on learning and creating. It can also be useful for seasoned game developers!

I hope this script helps you to improve your workflow and make it more efficient.

## Contact

**Jonathan Tremblay**  
Teacher, Cegep de Saint-Jerome  
jtrembla@cstj.qc.ca

Project Link: https://github.com/JonathanTremblay/UnityExportToZip

## Version History

* 1.0.1
    * Minor changes to folder naming and exclusions.
    * Changed the default folder name inside the archive to use the archive name.
	* Added an option flag at the beginning the code to keep the original project name. 
	* Added an option flag to exclude Build/Builds folders. 
	* Added Obj to folder exclusions.
	* Added .sln to file exclusions.
	* Project reorganized to Packages folder standards.
	* Package can now be installed from git repository.
* 1.0.0
    * Revised readme. Added a namespace. Improved Mac compatibility.
* 0.84
    * Major revision of the user interface (no more custom window). The process can now be cancelled.
* 0.83
    * Improved user feedback. Simplified user interface (standard Save As dialog).
* 0.82
    * First public version

## License

This script is available for distribution and modification under the CC0 License, which allows for free use and modification. https://creativecommons.org/share-your-work/public-domain/cc0/