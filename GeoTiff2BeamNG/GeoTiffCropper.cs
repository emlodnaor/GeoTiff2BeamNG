using OSGeo.GDAL;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GeoTiff2BeamNG
{
    internal class GeoTiffCropper
    {
        private void SetOutputProjection(Dataset outputDataset, string inputProjection)
        {
            outputDataset.SetProjection(inputProjection);
        }

        public async Task GeoTiffOutputExtractor(string filenameRawImg, string filenameImg, BoundaryBox inputBoundaryBox)
        {
            if (File.Exists(filenameImg)) { File.Delete(filenameImg); }
            Gdal.AllRegister();

            // Open the input GeoTIFF file
            using (Dataset inputDataset = Gdal.Open(filenameRawImg, Access.GA_ReadOnly))
            {
                double[] geoTransform = new double[6];
                inputDataset.GetGeoTransform(geoTransform);

                double pixelSizeX = Math.Abs(geoTransform[1]);
                double pixelSizeY = Math.Abs(geoTransform[5]);

                int xOffset = CalculateXOffset(geoTransform, inputBoundaryBox);
                int yOffset = CalculateYOffset(geoTransform, inputBoundaryBox, pixelSizeY, inputDataset.RasterYSize);
                int width = CalculateWidth(inputBoundaryBox, pixelSizeX);
                int height = CalculateHeight(inputBoundaryBox, pixelSizeY);

                Dataset outputDataset = CreateOutputDataset(filenameImg, width, height, inputDataset);

                SetOutputGeoTransform(outputDataset, geoTransform, inputBoundaryBox);
                SetOutputProjection(outputDataset, inputDataset.GetProjection());

                CopyRasterData(inputDataset, outputDataset, xOffset, yOffset, width, height);

                outputDataset.FlushCache();
                outputDataset.Dispose();
            }
            
        }

        private int CalculateXOffset(double[] geoTransform, BoundaryBox inputBoundaryBox)
        {
            return (int)((((double)inputBoundaryBox.MinimumLongitude - geoTransform[0]) / geoTransform[1]) / Math.Abs(geoTransform[1]));
        }

        private int CalculateYOffset(double[] geoTransform, BoundaryBox inputBoundaryBox, double pixelSizeY, int rasterYSize)
        {
            return (int)(rasterYSize + ((geoTransform[3] - (double)inputBoundaryBox.MaximumLatitude) / Math.Abs(geoTransform[5])) / pixelSizeY);
        }

        private int CalculateWidth(BoundaryBox inputBoundaryBox, double pixelSizeX)
        {
            return (int)((double)inputBoundaryBox.Width / pixelSizeX);
        }

        private int CalculateHeight(BoundaryBox inputBoundaryBox, double pixelSizeY)
        {
            return (int)((double)inputBoundaryBox.Height / pixelSizeY);
        }

        private Dataset CreateOutputDataset(string filenameImg, int width, int height, Dataset inputDataset)
        {
            return Gdal.GetDriverByName("GTiff").Create(
                filenameImg,
                width,
                height,
                inputDataset.RasterCount,
                inputDataset.GetRasterBand(1).DataType,
                null);
        }

        private void SetOutputGeoTransform(Dataset outputDataset, double[] geoTransform, BoundaryBox inputBoundaryBox)
        {
            outputDataset.SetGeoTransform(new double[]
            {
                (double)inputBoundaryBox.MinimumLongitude,
                Math.Abs(geoTransform[1]), // Use adjusted pixel size
                geoTransform[2],
                (double)inputBoundaryBox.MaximumLatitude,
                geoTransform[4],
                -Math.Abs(geoTransform[5]) // Use adjusted pixel size with a negative sign
            });
        }

        private void CopyRasterData(Dataset inputDataset, Dataset outputDataset, int xOffset, int yOffset, int width, int height)
        {
            for (int bandIndex = 1; bandIndex <= inputDataset.RasterCount; bandIndex++)
            {
                Band inputBand = inputDataset.GetRasterBand(bandIndex);
                Band outputBand = outputDataset.GetRasterBand(bandIndex);

                double[] buffer = new double[width * height];

                inputBand.ReadRaster(xOffset, yOffset, width, height, buffer, width, height, 0, 0);

                outputBand.WriteRaster(0, 0, width, height, buffer, width, height, 0, 0);
            }
        }
    }
}
