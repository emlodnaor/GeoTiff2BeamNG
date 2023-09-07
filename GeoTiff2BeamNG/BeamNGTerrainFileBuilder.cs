using OSGeo.GDAL;

internal class BeamNGTerrainFileBuilder
{
    private string croppedOutputFile;
    private FileInfo OutputDirectory { get; }

    public BeamNGTerrainFileBuilder(string croppedOutputFile, FileInfo outputDirectory)
    {
        this.croppedOutputFile = croppedOutputFile;
        OutputDirectory = outputDirectory;
    }

    internal async Task Build()
    {
        
        List<string> materialNames = new() //This should be dynamic!!!
            {
                "Grass2",
                "Dirt",
                "Mud",
                "asphalt",
                "ROCK",
                "asphalt2"
            };

        var heightArray = GetHeightArray(croppedOutputFile);
        WriteTerrainFile(heightArray, materialNames);


    }
    private void WriteTerrainFile(double[,] heightArray, List<string> materialNames)
    {
        //data to the terrainfile is seemingly written to file startin lower left, to lower right, ending at upperright 
        byte version = 8; // unsure if beamng render/map version, or version of the map

        uint size = (uint)heightArray.GetLength(0);

        var binaryWriter = new BinaryWriter(File.Open($@"{OutputDirectory.FullName}\theTerrain.ter", FileMode.Create));
        binaryWriter.Write(version);
        binaryWriter.Write(size);

        WriteHeightMap(binaryWriter, heightArray);
        WriteLayerMap(binaryWriter, heightArray);
        WriteLayerTexture(binaryWriter, heightArray);

        binaryWriter.Write(materialNames.Count);
        WriteMaterialNames(binaryWriter, materialNames);

        binaryWriter.Close();
    }
    private static void WriteMaterialNames(BinaryWriter binaryWriter, List<string> materialNames)
    {
        foreach (var materialName in materialNames)
            binaryWriter.Write(materialName);
    }

    private static void WriteLayerTexture(BinaryWriter binaryWriter, double[,] heightArray)
    {
        foreach (var p in heightArray)
            binaryWriter.Write(0);
    }

    private static void WriteLayerMap(BinaryWriter binaryWriter, double[,] heightArray)
    {
        var longitudes = heightArray.GetLength(0);
        var latitudes = heightArray.GetLength(1);

        var longitudeCounter = 0;
        var latitudeCounter = 0;



        while (latitudeCounter < latitudes)
        {
            byte theByte = 0;
            binaryWriter.Write(theByte);

            longitudeCounter++;

            if (longitudeCounter > longitudes - 1) //unsure
            {
                longitudeCounter = 0;
                latitudeCounter++;
            }
        }
    }

    private static void WriteHeightMap(BinaryWriter binaryWriter, double[,] heightArray)
    {
        var minAltitude = double.MaxValue;
        var maxAltitude = double.MinValue;
        foreach ( var height in heightArray ) 
        { 
            minAltitude = Math.Min(minAltitude,height);
            maxAltitude= Math.Max(maxAltitude,height);
        }
        var heightDifference = maxAltitude - minAltitude;
        var steps = 65535d;
        var stepsPerMeter = steps / heightDifference;

        var longitudeCounter = 0;
        var latitudeCounter = 0;
        var latitudes = heightArray.GetLength(1);

        while (latitudeCounter < latitudes)
        {
            var localAltitude = heightArray[longitudeCounter, latitudeCounter] - minAltitude;
            var binaryAltitude = localAltitude * stepsPerMeter;
            ushort binaryInt = (ushort)Math.Round(binaryAltitude, 0);
            binaryWriter.Write(binaryInt);

            longitudeCounter++;

            if (longitudeCounter > heightArray.GetLength(0) - 1)
            {
                longitudeCounter = 0;
                latitudeCounter++;
            }
        }
        Console.WriteLine($"Done setting heigh map. diffHeight: {heightDifference}");

    }
    

    private double[,] GetHeightArray(string fileName)
    {
        // Open the GeoTIFF file
        Dataset dataSet = Gdal.Open(fileName, Access.GA_ReadOnly);

        // Get the raster band
        Band band = dataSet.GetRasterBand(1);
        band.AsMDArray();

        // Get the dimensions of the raster
        int width = dataSet.RasterXSize;
        int height = dataSet.RasterYSize;

        // Get the value of the pixels at a given x and y coordinate
        var pixelValues = new double[width * height];

        band.ReadRaster(0, 0, width, height, pixelValues, width, height, 0, 0); //

        var resultPixelValues = new double[width, height];

        //pictures are read from top left to bottom right, but we use lon and lat, so we read from bottom left to top right, this takes care of that... 

        var longitudeCounter = 0;
        var latitudeCounter = height - 1;

        foreach (var value in pixelValues)
        {
            resultPixelValues[longitudeCounter, latitudeCounter] = value;

            longitudeCounter++;
            if (longitudeCounter > width - 1)
            {
                longitudeCounter = 0;
                latitudeCounter--;
            }
        }

        return resultPixelValues;
    }
}