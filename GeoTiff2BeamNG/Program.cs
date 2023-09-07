using GeoTiff2BeamNG;
using OSGeo.GDAL;
using OSGeo.OGR;
using System;
using System.IO;
using System.Runtime.InteropServices;

GdalConfiguration.ConfigureGdal();
GdalConfiguration.ConfigureOgr();

Gdal.AllRegister();
Ogr.RegisterAll();

Console.WriteLine("Hello, Worldcreator!");

var rawOutputFile = "output.tif";
var croppedOutputFile = "Crop_output.tif";

BoundaryBox inputBB = await new CombineGeoTiffs().Combine();

await new GeoTiffCropper().GeoTiffOutputExtractor(rawOutputFile, croppedOutputFile, inputBB);


await new BeamNGTerrainFileBuilder(croppedOutputFile).Build();
var ca = 0;
