# nealib
Open source library for accessing, validating and processing archiveversions (information packages) and their data and metadata according to the SIARDDK 1.0 and SIARDDK 2.0 standards (regulations 1007 and 128).
# How to install
Nealib can be installed using nuget running this command: ``Install-Package NEALib -Version 2.1.0``

Or download it manually from https://www.nuget.org/packages/NEALib/
# Examples of how to use the library
## Get archiveversions in a folder
```
var avIdentifier = new ArchiveVersionIdentifier();

List<ArchiveVersion> archiveversions = avIdentifier.GetArchiveVersions(@"D:\MyArchiveVersions\");
```
## Get all files in archiveversion folders
```
var AvIdentifier = new ArchiveVersionIdentifier();

ArchiveVersion av = AvIdentifier.GetArchiveVersions(@"D:\MyArchiveVersions\")[0];

List<string> FilesInArchiveVersion = av.GetFiles();
```
    
## Get checksums from archiveversion files
```
var AvIdentifier = new ArchiveVersionIdentifier();

ArchiveVersion av = AvIdentifier.GetArchiveVersions(@"D:\MyArchiveVersions\")[0];

var FilesAndCheckSums = av.GetChecksumDict();
```
## Read data from a table
```
var AvIdentifier = new ArchiveVersionIdentifier();

ArchiveVersion av = AvIdentifier.GetArchiveVersions(@"D:\MyArchiveVersions\")[0];

TableReader tr = av.GetTableReader("table1.xml");

foreach(TableReader.Post[] posts in tr.Read())
{
    Console.WriteLine($"Row {posts[0].RowIndex} contains this data:");
    foreach(TableReader.Post post in posts)
    {
        Console.WriteLine(post.Data);
    }
}
```
