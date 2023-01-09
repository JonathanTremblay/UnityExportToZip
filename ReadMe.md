# Unity ExportToZip

This is a Unity script that allows to easily export an entire Unity project to a Zip file, directly in the project folder. The script will exclude all zip files at the top level of the project. It will also exclude the following folders: .git, Library, Logs, Temp.

## Getting Started

In your Unity project, import the package (or manually add the script to an Editor folder in the Assets folder).

You can then access the ExportToZip window by going to the File menu and selecting Export to zip... 

* In the window, you can change the name of the Zip file. 
* Click the Export button to start the export process. 
* The script will create the Zip file in the project folder.

## Description

I created this tool to help my students move their Unity projects between computers and to make it easier for them to hand in their assignments. As a teacher, I noticed that the huge size of the Library folder in a Unity project can be difficult to manage for new users. While the Library folder can be recreated if deleted, it is not an intuitive process for those unfamiliar with Unity. 

The ExportToZip script simplifies the process of transferring and handing in Unity projects, making it easier for my students to focus on learning and creating.

I hope you find this script useful and that it makes your workflow more efficient. Thank you for using ExportToZip!

## Compatibility

Tested on Mac and Windows with Unity version 2022.1.11

### Know issues

* If the project contains large files, processing them may make execution slower.


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

## License

 This project is available for distribution and modification under the CC0 License, which allows for free use and modification.  
 https://creativecommons.org/share-your-work/public-domain/cc0/

