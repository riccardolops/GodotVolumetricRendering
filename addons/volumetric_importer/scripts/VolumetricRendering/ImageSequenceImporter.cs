using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Godot;

namespace VolumetricRendering
{
    /// <summary>
    /// Converts a directory of image slices into a VolumeDataset for volumetric rendering.
    /// </summary>
    public class ImageSequenceImporter : IImageSequenceImporter
    {
        public class ImageSequenceFile : IImageSequenceFile
        {
            public string filePath;

            public string GetFilePath()
            {
                return filePath;
            }
        }

        public class ImageSequenceSeries : IImageSequenceSeries
        {
            public List<ImageSequenceFile> files = new();

            public IEnumerable<IImageSequenceFile> GetFiles()
            {
                return files;
            }
        }

        public string directoryPath;
        private HashSet<string> supportedImageTypes = new()
        {
            ".png",
            ".jpg",
            ".jpeg"
        };

        public IEnumerable<IImageSequenceSeries> LoadSeries(IEnumerable<string> files, ImageSequenceImportSettings settings)
        {
            Dictionary<string, ImageSequenceSeries> sequenceByFiletype = new();

            LoadSeriesInternal(files, sequenceByFiletype, settings.progressHandler);

            if (sequenceByFiletype.Count == 0)
                GD.PrintErr("Found no image files of supported formats. Currently supported formats are: " + supportedImageTypes.ToString());

            return sequenceByFiletype.Select(f => f.Value).ToList();
        }
        public async Task<IEnumerable<IImageSequenceSeries>> LoadSeriesAsync(IEnumerable<string> files, ImageSequenceImportSettings settings)
        {
            Dictionary<string, ImageSequenceSeries> sequenceByFiletype = new();

            await Task.Run(() => LoadSeriesInternal(files, sequenceByFiletype, settings.progressHandler));

            if (sequenceByFiletype.Count == 0)
                GD.PrintErr("Found no image files of supported formats. Currently supported formats are: " + supportedImageTypes.ToString());

            return sequenceByFiletype.Select(f => f.Value).ToList();
        }
        private void LoadSeriesInternal(IEnumerable<string> files, Dictionary<string, ImageSequenceSeries> sequenceByFiletype, IProgressHandler progress)
        {
            int fileIndex = 0, numFiles = files.Count();
            foreach (string filePath in files)
            {
                progress.ReportProgress(fileIndex, numFiles, $"Loading DICOM file {fileIndex} of {numFiles}");
                string fileExt = Path.GetExtension(filePath).ToLower();
                if (supportedImageTypes.Contains(fileExt))
                {
                    if (!sequenceByFiletype.ContainsKey(fileExt))
                        sequenceByFiletype[fileExt] = new ImageSequenceSeries();

                    ImageSequenceFile imgSeqFile = new();
                    imgSeqFile.filePath = filePath;
                    sequenceByFiletype[fileExt].files.Add(imgSeqFile);
                }
            }
        }

        public VolumeDataset ImportSeries(IImageSequenceSeries series, ImageSequenceImportSettings settings)
        {
            List<string> imagePaths = series.GetFiles().Select(f => f.GetFilePath()).ToList();

            Vector3I dimensions = GetVolumeDimensions(imagePaths);
            int[] data = FillSequentialData(dimensions, imagePaths);
            VolumeDataset dataset = FillVolumeDataset(data, dimensions);

            dataset.FixDimensions();
            dataset.rotation = Quaternion.FromEuler(new Vector3(90.0f, 0.0f, 0.0f));

            return dataset;
        }
        public async Task<VolumeDataset> ImportSeriesAsync(IImageSequenceSeries series, ImageSequenceImportSettings settings)
        {
            List<string> imagePaths = null;
            VolumeDataset dataset = null;

            await Task.Run(() => { imagePaths = series.GetFiles().Select(f => f.GetFilePath()).ToList(); }); ;

            Vector3I dimensions = GetVolumeDimensions(imagePaths);
            int[] data = FillSequentialData(dimensions, imagePaths);
            dataset = await FillVolumeDatasetAsync(data, dimensions);
            dataset.FixDimensions();

            return dataset;
        }

        /// <summary>
        /// Gets the XY dimensions of an image at the path.
        /// </summary>
        /// <param name="path">The image path to check.</param>
        /// <returns>The XY dimensions of the image.</returns>
        private Vector2I GetImageDimensions(string path)
        {
            Image image = Image.LoadFromFile(path);
            return new Vector2I()
            {
                X = image.GetWidth(),
                Y = image.GetHeight()
            };
        }

        /// <summary>
        /// Adds a depth value Z to the XY dimensions of the first image.
        /// </summary>
        /// <param name="paths">The set of image paths comprising the volume.</param>
        /// <returns>The dimensions of the volume.</returns>
        private Vector3I GetVolumeDimensions(List<string> paths)
        {
            Vector2I twoDimensional = GetImageDimensions(paths[0]);
            Vector3I threeDimensional = new()
            {
                X = twoDimensional.X,
                Y = twoDimensional.Y,
                Z = paths.Count
            };
            return threeDimensional;
        }

        /// <summary>
        /// Converts a volume set of images into a sequential series of values.
        /// </summary>
        /// <param name="dimensions">The XYZ dimensions of the volume.</param>
        /// <param name="paths">The set of image paths comprising the volume.</param>
        /// <returns>The set of sequential values for the volume.</returns>
        private int[] FillSequentialData(Vector3I dimensions, List<string> paths)
        {
            var data = new List<int>(dimensions.X * dimensions.Y * dimensions.Z);

            foreach (var path in paths)
            {
                Image image = Image.LoadFromFile(path);

                if (image.GetWidth() != dimensions.X || image.GetHeight() != dimensions.Y)
                {
                    throw new IndexOutOfRangeException("Image sequence has non-uniform dimensions");
                }
                Color[] pixels = new Color[dimensions.X * dimensions.Y];
                int index = 0;
                for (int i = 0; i < dimensions.X; i++)
                {
                    for (int j = 0; j < dimensions.Y; j++)
                    {
                        pixels[index] = image.GetPixel(i, j);
                        index++;
                    }
                }
                int[] imageData = DensityHelper.ConvertColorsToDensities(pixels);

                data.AddRange(imageData);
            }
            return data.ToArray();
        }

        /// <summary>
        /// Wraps volume data into a VolumeDataset.
        /// </summary>
        /// <param name="data">Sequential value data for a volume.</param>
        /// <param name="dimensions">The XYZ dimensions of the volume.</param>
        /// <returns>The wrapped volume data.</returns>
        private VolumeDataset FillVolumeDataset(int[] data, Vector3I dimensions)
        {
            string name = Path.GetFileName(directoryPath);

            VolumeDataset dataset = new();
            FillVolumeInternal(dataset, name, data, dimensions);

            return dataset;
        }
        private async Task<VolumeDataset> FillVolumeDatasetAsync(int[] data, Vector3I dimensions)
        {
            VolumeDataset dataset = new();
            string name = Path.GetFileName(directoryPath);

            await Task.Run(() => FillVolumeInternal(dataset, name, data, dimensions));

            return dataset;
        }
        private void FillVolumeInternal(VolumeDataset dataset, string name, int[] data, Vector3I dimensions)
        {
            dataset.datasetName = name;
            dataset.data = Array.ConvertAll(data, new Converter<int, float>((int val) => { return Convert.ToSingle(val); }));
            dataset.dimX = dimensions.X;
            dataset.dimY = dimensions.Y;
            dataset.dimZ = dimensions.Z;
            dataset.scale = new Vector3(
                1f, // Scale arbitrarily normalised around the x-axis 
                (float)dimensions.Y / (float)dimensions.X,
                (float)dimensions.Z / (float)dimensions.X
            );
        }


    }
}
