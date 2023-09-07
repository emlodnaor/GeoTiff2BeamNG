using GeoTiff2BeamNG;
using OSGeo.GDAL;
using OSGeo.OGR;
using System;
using System.IO;
using System.Runtime.InteropServices;

var InputFolder = new FileInfo("c:\\GeoTiff2BeamNG\\Input");
var OutputFolder = new FileInfo("c:\\GeoTiff2BeamNG\\Output");
var CombinedOutputFile = "Combined.tif";
var CroppedOutputFile = "Cropped.tif";

CheckArgs();
GdalSetup();

BoundaryBox inputBB = await new CombineGeoTiffs(InputFolder, OutputFolder).Combine();

await new GeoTiffCropper().GeoTiffOutputExtractor(CombinedOutputFile, CroppedOutputFile, inputBB);

await new BeamNGTerrainFileBuilder(CroppedOutputFile, OutputFolder).Build();

//
void CheckArgs()
{
    var exit = false;
    var count = 0;
    foreach (string arg in args)
    {
        if (arg == "-i") InputFolder = new(args[count + 1]);
        if (arg == "-o") OutputFolder = new(args[count + 1]);
        count++;
    }
    Console.WriteLine("Hello, BeamNG Worldcreator!");

    if (!InputFolder.Exists)
    {
        Console.WriteLine($"'{InputFolder.FullName}' is not a valid folder, use -i 'Path' to set correct folder, or create the default folders.");
        exit = true;
    }
    if (!OutputFolder.Exists)
    {
        Console.WriteLine($"'{OutputFolder.FullName}' is not a valid folder, use -o 'Path' to set correct folder, or create the default folders.");
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