using GeoTiff2BeamNG;
using OSGeo.GDAL;
using OSGeo.OGR;
using System;
using System.IO;
using System.Runtime.InteropServices;

var InputDirectory = new DirectoryInfo(@"C:\GeoTiff2BeamNG\Input");
var OutputDirectory = new DirectoryInfo(@"c:\GeoTiff2BeamNG\Output");
var CombinedOutputFile = "Combined.tif";
var CroppedOutputFile = "Cropped.tif";

CheckArgs();
GdalSetup();

BoundaryBox inputBB = await new CombineGeoTiffs(InputDirectory, OutputDirectory).Combine();

await new GeoTiffCropper().GeoTiffOutputExtractor(CombinedOutputFile, CroppedOutputFile, inputBB);

await new BeamNGTerrainFileBuilder(CroppedOutputFile, OutputDirectory, InputDirectory).Build();

//
void CheckArgs()
{
    var exit = false;
    var count = 0;
    foreach (string arg in args)
    {
        if (arg == "-i") InputDirectory = new(args[count + 1]);
        if (arg == "-o") OutputDirectory = new(args[count + 1]);
        count++;
    }
    Console.WriteLine("Hello, BeamNG Worldcreator!");

    if (!InputDirectory.Exists)
    {
        Console.WriteLine($"'{InputDirectory.FullName}' is not a valid folder, use -i 'Path' to set correct folder, or create the default folders.");
        exit = true;
    }
    if (!OutputDirectory.Exists)
    {
        Console.WriteLine($"'{OutputDirectory.FullName}' is not a valid folder, use -o 'Path' to set correct folder, or create the default folders.");
        exit = true;
    }
    if (exit) Environment.Exit(0);

}
void GdalSetup()
{
    GdalConfiguration.ConfigureGdal();
    GdalConfiguration.ConfigureOgr();

    Gdal.AllRegister();
    Ogr.RegisterAll();
}