// See https://aka.ms/new-console-template for more information
using GeoTiff2BeamNG;
using OSGeo.GDAL;
using OSGeo.OGR;
using System.Runtime.InteropServices;

GdalConfiguration.ConfigureGdal();
GdalConfiguration.ConfigureOgr();

Gdal.AllRegister();
Ogr.RegisterAll();

Console.WriteLine("Hello, Worldcreator!");

var inputFiles = Directory.GetFiles("C:\\Users\\emlodnaor\\Downloads\\Sticky", "*.tif");

// Initialize variables to store the overall bounding box of all input files
double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
double[] geoTransform = new double[6];

// Open the first GeoTIFF file to get its information
Dataset firstDataset = Gdal.Open(inputFiles[0], Access.GA_ReadOnly);


// Calculate the dimensions of each tile
int tileWidth = firstDataset.RasterXSize;
int tileHeight = firstDataset.RasterYSize;

foreach (var file in inputFiles)
{
    Dataset dataset = Gdal.Open(file, Access.GA_ReadOnly);
    if (tileWidth != dataset.RasterXSize || tileHeight != dataset.RasterYSize) throw new Exception("Tiles should be same size");


    dataset.GetGeoTransform(geoTransform);


    // Calculate the bounding box coordinates in geographic space
    double fileMinX = geoTransform[0];
    double fileMaxX = geoTransform[0] + geoTransform[1] * dataset.RasterXSize;
    double fileMinY = geoTransform[3] + geoTransform[5] * dataset.RasterYSize;
    double fileMaxY = geoTransform[3];

    // Update overall bounding box (this is not the complete bbox, rasterxysize should be added to one of the sides)
    minX = Math.Min(minX, fileMinX);
    minY = Math.Min(minY, fileMinY);
    maxX = Math.Max(maxX, fileMaxX);
    maxY = Math.Max(maxY, fileMaxY);

    dataset.Dispose();
}

var extentX = maxX- minX;
var extentY = maxY- minY;

maxX += firstDataset.RasterXSize * geoTransform[1];
minY -= firstDataset.RasterYSize * geoTransform[1];

var totalExtentMultiplyer = Math.Floor(extentX / 2048);
var totalExtent = (int)totalExtentMultiplyer * 2048;

var centerX = minX + (maxX- minX);
var centerY = minY + (maxY- minY);

var bbLon = centerX;
var bbLat = centerY;
var bbr = totalExtent/2;
var bbox = $"{(bbLon - bbr)},{(bbLat - bbr)},{(bbLon + bbr)},{(bbLat + bbr)}";

var grid = new decimal[totalExtent, totalExtent];


// Calculate the dimensions of the final output image
int totalWidth = (int)tileWidth * (int)Math.Sqrt(inputFiles.Count());
int totalHeight = totalWidth;

var outputFile = "output.tif";

// Create the output dataset with the calculated dimensions
Dataset outputDataset = Gdal.GetDriverByName("GTiff").Create(
    outputFile,
    totalWidth,
    totalHeight,
    firstDataset.RasterCount,
    firstDataset.GetRasterBand(1).DataType,
    null);

geoTransform[0] = minX;
geoTransform[3] = maxY;

outputDataset.SetGeoTransform(geoTransform);
var firstGeoProjection = firstDataset.GetProjection();
outputDataset.SetProjection(firstGeoProjection);

// Loop through input files and copy raster data to the output
foreach (var inputFile in inputFiles)
{
    // Calculate the current tile's offset within the output image based on geographic location

    var currentGeoTransform = new double[6];
    Dataset currentDataset = Gdal.Open(inputFile, Access.GA_ReadOnly);
    currentDataset.GetGeoTransform(currentGeoTransform);


    int xOffset = (int)(currentGeoTransform[0] - minX);
    int yOffset = (int)(maxY - currentGeoTransform[3]);

    // Open the current input GeoTIFF file
    Dataset inputDataset = Gdal.Open(inputFile, Access.GA_ReadOnly);

    // Loop through bands in the input file
    for (int bandIndex = 1; bandIndex <= inputDataset.RasterCount; bandIndex++)
    {
        Band inputBand = inputDataset.GetRasterBand(bandIndex);
        Band outputBand = outputDataset.GetRasterBand(bandIndex);

        int xSize = tileWidth;
        int ySize = tileHeight;

        // Read data from the current input tile's band
        double[] buffer = new double[xSize * ySize];
        inputBand.ReadRaster(0, 0, xSize, ySize, buffer, xSize, ySize, 0, 0);

        // Write data to the output image at the calculated offset
        outputBand.WriteRaster(xOffset, yOffset, xSize, ySize, buffer, xSize, ySize, 0, 0);
    }

    // Dispose of the input dataset
    inputDataset.Dispose();
}

// Dispose of the output dataset
outputDataset.Dispose();
firstDataset.Dispose();


var a = 0;
