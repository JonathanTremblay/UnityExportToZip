# Unity ExportProjectToZip

This is a Unity script that allows to easily export an entire Unity project to a Zip file. The script will exclude all zip files at the top level of the project. It will also exclude the following folders: .git, Library, Logs, Temp.

## Getting Started

In your Unity project, import the package (or manually add the script to an Editor folder in the Assets folder).

To use it:
1. Simply select "Export Project to Zip..." from the file menu. 
2. If your project or your scene needs saving, you will be prompted to save (optional).
3. Then choose the name and location for the Zip file.

## Description

I created this tool to help my students move their Unity projects between computers and to make it easier for them to hand in their assignments. As a teacher, I noticed that the huge size of the Library folder in a Unity project can be difficult to manage for new users. While the Library folder can be recreated if deleted, it is not an intuitive process for those unfamiliar with Unity. 

The ExportProjectToZip script simplifies the process of transferring and handing in Unity projects, making it easier for my students to focus on learning and creating.

I hope you find this script useful and that it will make your workflow more efficient. Thank you for using ExportProjectToZip!

## Compatibility

Tested on Mac and Windows with Unity versions 2021.3.16 (LTS), 2022.1.11 and 2022.2.1.
Projects larger than 8 GB were compressed successfully.

### Know issues

* Progress bar could be more responsive when compressing (Unity editor bug).
* If the project contains large files, the export may take a long time.

## Contact

 **Jonathan Tremblay**  
 Teacher, Cegep de Saint-Jerome  
 jtrembla@cstj.qc.ca  
 https://jtremblay.tim-cstj.ca/index.html

Project Link: https://github.com/JonathanTremblay/UnityExportToZip

## Version History

* 0.82
    * First public version
* 0.83
    * Improved user feedback. Simplified user interface (filename is now chosen with a standard Save As dialog).
* 0.84
    * Major revision of the user interface (no more custom window). The process can now be cancelled.

## License

 This project is available for distribution and modification under the CC0 License, which allows for free use and modification.  
 https://creativecommons.org/share-your-work/public-domain/cc0/

