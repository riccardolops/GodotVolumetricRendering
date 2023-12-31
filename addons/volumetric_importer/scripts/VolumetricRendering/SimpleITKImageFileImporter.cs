using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Godot;
using itk.simple;

namespace VolumetricRendering
{
    /// <summary>
    /// SimpleITK-based image importer.
    /// </summary>
    public class SimpleITKImageFileImporter : IImageFileImporter
    {
        public VolumeDataset Import(string filePath)
        {
            float[] pixelData = null;
            VectorUInt32 size = null;
            VectorDouble spacing = null;

            VolumeDataset volumeDataset = new VolumeDataset();

            ImportInternal(volumeDataset, pixelData, size, spacing, filePath);

            return volumeDataset;
        }
        public async Task<VolumeDataset> ImportAsync(string filePath)
        {
            float[] pixelData = null;
            VectorUInt32 size = null;
            VectorDouble spacing = null;

            // Create dataset
            VolumeDataset volumeDataset = new VolumeDataset();

            await Task.Run(() => ImportInternal(volumeDataset, pixelData, size, spacing, filePath));

            return volumeDataset;
        }

        private void ImportInternal(VolumeDataset volumeDataset, float[] pixelData, VectorUInt32 size, VectorDouble spacing, string filePath)
        {
            ImageFileReader reader = new ImageFileReader();

            reader.SetFileName(filePath);

            itk.simple.Image image = reader.Execute();

            // Convert to LPS coordinate system (may be needed for NRRD and other datasets)
            SimpleITK.DICOMOrient(image, "LPS");

            // Cast to 32-bit float
            image = SimpleITK.Cast(image, PixelIDValueEnum.sitkFloat32);

            size = image.GetSize();

            int numPixels = 1;
            for (int dim = 0; dim < image.GetDimension(); dim++)
                numPixels *= (int)size[dim];

            // Read pixel data
            pixelData = new float[numPixels];
            IntPtr imgBuffer = image.GetBufferAsFloat();
            Marshal.Copy(imgBuffer, pixelData, 0, numPixels);

            spacing = image.GetSpacing();

            volumeDataset.data = pixelData;
            volumeDataset.dimX = (int)size[0];
            volumeDataset.dimY = (int)size[1];
            volumeDataset.dimZ = (int)size[2];
            volumeDataset.datasetName = Path.GetFileName(filePath);
            volumeDataset.filePath = filePath;
            volumeDataset.scale = new Vector3(
                (float)(spacing[0] * size[0]) / 1000.0f, // mm to m
                (float)(spacing[1] * size[1]) / 1000.0f, // mm to m
                (float)(spacing[2] * size[2]) / 1000.0f // mm to m
            );

            // Convert from LPS to Godot's coordinate system
            ImporterUtilsInternal.ConvertLPSToGodotCoordinateSpace(volumeDataset);

            volumeDataset.FixDimensions();
        }
    }
}
