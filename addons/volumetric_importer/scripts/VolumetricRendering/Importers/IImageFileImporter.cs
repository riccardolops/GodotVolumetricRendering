using System;
using System.Threading.Tasks;

namespace VolumetricRendering
{
    public enum ImageFileFormat
    {
        NRRD,
        NIFTI
    }

    /// <summary>
    /// Interface for single file dataset importers (NRRD, NIFTI, etc.).
    /// These datasets contain only one single file.
    /// </summary>
    public interface IImageFileImporter
    {
        VolumeDataset Import(String filePath);
        Task<VolumeDataset> ImportAsync(String filePath);
    }
}