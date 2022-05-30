[![Build Status](https://app.travis-ci.com/CopenhagenCityArchives/WolfPack.svg?token=3p1ukNPzEUWZc6C8RAv1&branch=master)](https://app.travis-ci.com/CopenhagenCityArchives/WolfPack)
# About
WolfPack is a simple tool for packaging any amount and any structure of files in an efficient, transparent and resumable way in packages (ZIP64 compliant files). No spanning or splitting is used when creating the ZIP files.

## How it works
WolfPack performs a planning and a packaging stage.
In the planning stage all source files are iterated and put into packages according to their location and the specified maximum size (given by the ``--size`` parameter) of the packages.

In the packaging stage all packages are created during the following flow:
* Pack all files in a package. This is done in memory, if the memory limit given by the ``--memory`` parameter it not met. Otherwise it is done using the given temporary work dir.
* When the package is done all files are extracted, either in memory or the working dir, and their checksums are calculated. If they match the expected checksums, the package is written to the destination directory.
* If IO errors occurs during packaging or validation, three retries are performed before the package fails.

The packaging stage are performed in threads according to the ``--threads`` parameter.

The output of the packaging is as follows:
* The ZIP files
* items.csv: A list of all files given by the IItemFeeder with their relative location, checksum and size. This file is used when resuming a packaging task
* filesPackagesMap.csv: A list of all files and their package. Used for file lookup.
* packagesChecksums.csv: A list of all packages and their checksums. Used for validation of the packages.
* packaging.log: A log with output from the task. Also has information about the task and software.

## SIARDDK
The files in SIARDDK are planned in packages acccording to this logic:
* All other files than those from the \\Documents folder and \\tables folder are put in the very first package
* All table files are put in the following package according to the ``--size`` parameter (this is a soft max, as table files larger than the wanted size still is put in a single package, thus breaking the size limit)
* All files from the \\Documents folders are put in the following packages

NOTICE: The WolfPack uses fileIndex.xml when retrieving files from the information package. It thus requires an updated fileIndex.xml with correct MD5 checksums! See [MD5Checker](https://github.com/CopenhagenCityArchives/MD5Checker) which validates SIARDDK information packages according to fileIndex.xml.


# Usage
Download the latest [release](https://github.com/CopenhagenCityArchives/WolfPack/releases) (both the exe file and the log4net.config files are required) or build the solution with ``dotnet build`` and run it.

Usage: ``WolfPack.CLI.exe pack <--source> <--work-dir> <--destination> <--passphrasefile> [options]``

Arguments:

  * <--source>:         The source location

  * <--work-dir>:       The temp directory where packaging and validation is done

  * <--destination>:    The destination folder

  * <--passphrasefile>: Location of the pass phrase file used to read pass phrases


Options:

  * -e, --encryption:  The encryption method. Only AES256 is supported at the moment [default: aes256]

  * -s, --size:             Maximum size of zip files in gigabytes. Prioritized files and files larger than this value will result in larger zip files than this value [default: 1]

  * -t, --threads:       Maximum number of threads used when packing [default: 8]

  * -m, --memory:          Maximum memory in gigabytes used for packing and validating [default: 8]

  * -?, -h, --help:                 Show help and usage information

To see the list above run ``WolfPack.CLI.exe pack --help``

# Important software parts
## Main process
An ItemFeeder returns a list of files to the PackagePlanner, which in turn orders items according to the planners internal logic.
Both ItemFeeder and PackagePlanner can be implemented as needed using IItemFeeder and IPackagePlanner interfaces to accomodate other needs such as plain file structures or other information package formats.

When packages are planned, they are packed into ZIP files.
They are either packed in temporary files on disk, or, if memory and total size allows it, in memory.
After they are packed, the files are extracted and validated, and the final ZIP file are written/copied to the destination folder.

## ItemFeeder1007
Right now only the ItemFeeder1007 is implemented. 
Why a speciel ItemFeeder for SIARDDK and not just a standard file/directory feeder? All files i SIARDDK are described in fileIndex.xml with relative path and checksums. This give some advantages:
* MD5Checksums are already calculated
* Files and directories doesn't have to be traversed (this is a big task for larger information packages)
* Files not described in fileIndex.xml are not included in the final packages

All of these points makes a specialized SIARDDK item feeder much faster than just traversing directories.

The ItemFeeder1007 iterates all files in a SIARDDK information package according to fileIndex.xml. this means that files in the structure that are not given in the fileIndex.xml are not included in the packages.

## PackagePlanner
The PackagePlanner is implemented as such:
* All index files and context documents are put in the first package
* All table files are put in the next packages
* All other files are put in later packages

The PackagePlanner returns a list of packages with serial numbers begining with 00000001.

The PackagePlanner has a soft maximum size of 1 gb.
It is soft because files larger than 1 gb will result in larger ZIP files.
If there are many files in the first package the file can also be larger than 1 gb.

# Development
To restore nuget packages run nuget restore.

To save packages for offline development run ``dotnet restore --packages R:\nuget\WolfPack``

To use offline packages when restoring run ``dotnet restore --source R:\nuget\WolfPack``

Then build using ``dotnet build --packages R:\nuget\WolfPack``

# Deployment
Deploy the application to a self contained single file using this command:

``dotnet publish --self-contained true --runtime win-x64 --output [OUTPUTPATH] -p:PublishSingleFile=true``

Deploy to local environment using this command (deploy location are set using build.props, see build.props-example for an example):

``dotnet publish WolfPackNEA.CLI -c PublishLocally``