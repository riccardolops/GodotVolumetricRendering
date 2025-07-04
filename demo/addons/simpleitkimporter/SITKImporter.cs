using System;
using System.Runtime.InteropServices;
using Godot;
using Godot.Collections;
#if GVR_USE_SIMPLEITK
using itk.simple;
#endif

namespace VolumetricRendering;

public partial class SITKImporter : Node
{
#if GVR_USE_SIMPLEITK
    private itk.simple.Image image;
#endif

    public ImageTexture3D volume_texture;

    public ImageTexture3D gradient_texture;

    public ImageTexture histogram_texture;

    public Error Load(string filePath)
    {
#if GVR_USE_SIMPLEITK
        string imageIOType = DetermineImageIOType(filePath);
        ImageFileReader reader = new();
        reader.SetImageIO(imageIOType);
        reader.SetFileName(filePath);
        image = reader.Execute();
        image = SimpleITK.DICOMOrient(image, "LPS");
        return Error.Ok;
#else
        GD.PrintErr("SimpleITK not enabled");
        return Error.Failed;
#endif
    }

# if GVR_USE_SIMPLEITK
    private static string DetermineImageIOType(string filePath)
    {
        if (filePath.EndsWith(".nrrd") || filePath.EndsWith(".nhdr"))
        {
            return "NrrdImageIO";
        }
        else if (filePath.EndsWith(".nia") || filePath.EndsWith(".nii") || filePath.EndsWith(".img.gz") || filePath.EndsWith(".nii.gz") || filePath.EndsWith(".img") || filePath.EndsWith(".hdr"))
        {
            return "NiftiImageIO";
        }
        return string.Empty;
    }

    public void LoadDICOM(string path)
    {
        ImageSeriesReader reader = new();
        VectorString dicom_names = ImageSeriesReader.GetGDCMSeriesFileNames(path);
        reader.SetFileNames(dicom_names);
        image = reader.Execute();
        image = SimpleITK.DICOMOrient(image, "LPS");
    }

    public Vector3I getDimensions()
    {
        VectorUInt32 size = image.GetSize();
        return new Vector3I((int)size[0], (int)size[1], (int)size[2]);
    }

    public float[] getFloat32ByteArray()
    {
        itk.simple.Image image_f = SimpleITK.Cast(image, PixelIDValueEnum.sitkFloat32);
        VectorUInt32 size = image_f.GetSize();
        int length = (int)size[0] * (int)size[1] * (int)size[2];
        IntPtr imgBuffer = image_f.GetBufferAsFloat();
        float[] buffer = new float[length];
        Marshal.Copy(imgBuffer, buffer, 0, length);
        return buffer;
    }

    public byte[] getUInt8ByteArray()
    {
        itk.simple.Image image_i = SimpleITK.RescaleIntensity(image, 0, 255);
        image_i = SimpleITK.Cast(image_i, PixelIDValueEnum.sitkUInt8);
        VectorUInt32 size = image_i.GetSize();
        int length = (int)size[0] * (int)size[1] * (int)size[2];
        IntPtr imgBuffer = image_i.GetBufferAsUInt8();
        byte[] buffer = new byte[length];
        Marshal.Copy(imgBuffer, buffer, 0, length);
        return buffer;
    }

    public Vector3 getSpacing()
    {
        VectorDouble spacing = image.GetSpacing();
        return new Vector3((float)spacing[0], (float)spacing[1], (float)spacing[2]);
    }

    public void EvaluateTextures()
    {
        itk.simple.Image image_as_float = SimpleITK.Cast(image, PixelIDValueEnum.sitkFloat32);
        image_as_float = SimpleITK.RescaleIntensity(image_as_float, 0, 1);
        VectorUInt32 size = image_as_float.GetSize();
        int volumeLength = (int)size[0] * (int)size[1] * (int)size[2];
        int slice_byte_size = (int)size[0] * (int)size[1] * sizeof(float);
        IntPtr imgBuffer = image_as_float.GetConstBufferAsFloat();
        float[] imgData = new float[volumeLength];
        Marshal.Copy(imgBuffer, imgData, 0, volumeLength);

        itk.simple.Image gradientImage = SimpleITK.Gradient(image_as_float);
        IntPtr gradBuffer = gradientImage.GetBufferAsFloat();
        float[] gradData = new float[volumeLength * 3]; // 3 gradient vector components
        Marshal.Copy(gradBuffer, gradData, 0, gradData.Length);

        byte[] sliceBuffer = new byte[slice_byte_size];
        byte[] gradientBuffer = new byte[slice_byte_size * 3]; // 3 channels (RGB)

        Array<Godot.Image> slices = new Array<Godot.Image>();
        Array<Godot.Image> gradients = new Array<Godot.Image>();

        for (int z = 0; z < (int)size[2]; z++)
        {
            Buffer.BlockCopy(imgData, z * slice_byte_size, sliceBuffer, 0, slice_byte_size);
            Godot.Image slice = Godot.Image.CreateFromData((int)size[0], (int)size[1], false, Godot.Image.Format.Rf, sliceBuffer);
            slices.Add(slice);

            Buffer.BlockCopy(gradData, z * slice_byte_size * 3, gradientBuffer, 0, slice_byte_size * 3);
            Godot.Image gradient = Godot.Image.CreateFromData((int)size[0], (int)size[1], false, Godot.Image.Format.Rgbf, gradientBuffer);
            gradients.Add(gradient);
        }

        volume_texture = new ImageTexture3D();
        volume_texture.Create(Godot.Image.Format.Rf, (int)size[0], (int)size[1], (int)size[2], false, slices);
        gradient_texture = new ImageTexture3D();
        gradient_texture.Create(Godot.Image.Format.Rgbf, (int)size[0], (int)size[1], (int)size[2], false, gradients);
        ComputeHistogram();
    }

    private void ComputeHistogram()
    {
        itk.simple.Image image_as_double = SimpleITK.Cast(image, PixelIDValueEnum.sitkFloat64);
        MinimumMaximumImageFilter filter = new();
        filter.Execute(image_as_double);
        double min = filter.GetMinimum();
        double max = filter.GetMaximum();
        double range = max - min;
        filter.Dispose();
        int n_frequencies = (int)Math.Min(max - min, 1024);
        int[] frequencies = new int[n_frequencies];
        int size = (int)image_as_double.GetSize()[0] * (int)image_as_double.GetSize()[1] * (int)image_as_double.GetSize()[2];
        IntPtr imgBuffer = image_as_double.GetConstBufferAsDouble();
        double[] imgData = new double[size];
        Marshal.Copy(imgBuffer, imgData, 0, size);
        int maxFrequency = 0;
        for (int i = 0; i < size; i++)
        {
            double value = imgData[i];
            double tValue = (value - min) / range;
            int frequency = (int)(tValue * (n_frequencies - 1));
            frequencies[frequency]++;
            maxFrequency = Math.Max(maxFrequency, frequencies[frequency]);
        }

        float[] histogram = new float[n_frequencies];
        for (int i = 0; i < n_frequencies; i++)
        {
            histogram[i] = (float)(Math.Log(frequencies[i]) / Math.Log(10)) / (float)(Math.Log(maxFrequency) / Math.Log(10));
        }
        byte[] buffer = new byte[n_frequencies * sizeof(float)];
        Buffer.BlockCopy(histogram, 0, buffer, 0, buffer.Length);
        Godot.Image texture = Godot.Image.CreateFromData(n_frequencies, 1, false, Godot.Image.Format.Rf, buffer);
        histogram_texture = ImageTexture.CreateFromImage(texture);
    }
#endif
}