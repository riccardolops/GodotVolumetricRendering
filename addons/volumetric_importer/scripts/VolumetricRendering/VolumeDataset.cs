using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using static Godot.Image;

namespace VolumetricRendering
{
    /// <summary>
	/// An imported dataset. Contains a 3D pixel array of density values.
	/// </summary>
    public class VolumeDataset
    {
        public string filePath;

        // Flattened 3D array of data sample values.
        [Export]
        public float[] data;

        [Export]
        public int dimX, dimY, dimZ;

        [Export]
        public Vector3 scale = Vector3.One;

        [Export]
        public Quaternion rotation;

        public float volumeScale;

        [Export]
        public string datasetName;

        private float minDataValue = float.MaxValue;
        private float maxDataValue = float.MinValue;

        private ImageTexture3D dataTexture = null;
        private ImageTexture3D gradientTexture = null;

        private SemaphoreSlim createDataTextureLock = new SemaphoreSlim(1, 1);
        private SemaphoreSlim createGradientTextureLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets the 3D data texture, containing the density values of the dataset.
        /// Will create the data texture if it does not exist. This may be slow (consider using <see cref="GetDataTextureAsync"/>).
        /// </summary>
        /// <returns>3D texture of dataset</returns>
        public ImageTexture3D GetDataTexture()
        {
            if (dataTexture == null)
            {
                dataTexture = AsyncHelper.RunSync<ImageTexture3D>(() => CreateTextureInternalAsync(NullProgressHandler.instance));
                return dataTexture;
            }
            else
            {
                return dataTexture;
            }
        }

        /// <summary>
        /// Gets the 3D data texture, containing the density values of the dataset.
        /// Will create the data texture if it does not exist, without blocking the main thread.
        /// </summary>
        /// <param name="progressHandler">Progress handler for tracking the progress of the texture creation (optional).</param>
        /// <returns>Async task returning a 3D texture of the dataset</returns>
        public async Task<ImageTexture3D> GetDataTextureAsync(IProgressHandler progressHandler = null)
        {
            if (dataTexture == null)
            {
                await createDataTextureLock.WaitAsync();
                try
                {
                    if (progressHandler == null)
                        progressHandler = new NullProgressHandler();
                    dataTexture = await CreateTextureInternalAsync(progressHandler != null ? progressHandler : NullProgressHandler.instance);
                }
                finally
                {
                    createDataTextureLock.Release();
                }
            }
            return dataTexture;
        }

        /// <summary>
        /// Gets the gradient texture, containing the gradient values (direction of change) of the dataset.
        /// Will create the gradient texture if it does not exist. This may be slow (consider using <see cref="GetGradientTextureAsync" />).
        /// </summary>
        /// <returns>Gradient texture</returns>
        public ImageTexture3D GetGradientTexture()
        {
            if (gradientTexture == null)
            {
                gradientTexture = AsyncHelper.RunSync<ImageTexture3D>(() => CreateGradientTextureInternalAsync(NullProgressHandler.instance));
                return gradientTexture;
            }
            else
            {
                return gradientTexture;
            }
        }

        /// <summary>
        /// Gets the gradient texture, containing the gradient values (direction of change) of the dataset.
        /// Will create the gradient texture if it does not exist, without blocking the main thread.
        /// </summary>
        /// <param name="progressHandler">Progress handler for tracking the progress of the texture creation (optional).</param>
        /// <returns>Async task returning a 3D gradient texture of the dataset</returns>
        public async Task<ImageTexture3D> GetGradientTextureAsync(IProgressHandler progressHandler = null)
        {
            if (gradientTexture == null)
            {
                await createGradientTextureLock.WaitAsync();
                try
                {
                    if (progressHandler == null)
                        progressHandler = new NullProgressHandler();
                    gradientTexture = await CreateGradientTextureInternalAsync(progressHandler != null ? progressHandler : NullProgressHandler.instance);
                }
                finally
                {
                    createGradientTextureLock.Release();
                }
            }
            return gradientTexture;
        }

        public float GetMinDataValue()
        {
            if (minDataValue == float.MaxValue)
                CalculateValueBounds(new NullProgressHandler());
            return minDataValue;
        }

        public float GetMaxDataValue()
        {
            if (maxDataValue == float.MinValue)
                CalculateValueBounds(new NullProgressHandler());
            return maxDataValue;
        }

        /// <summary>
        /// Ensures that the dataset is not too large.
        /// This is automatically called during import,
        ///  so you should not need to call it yourself unless you're making your own importer of modify the dimensions.
        /// </summary>
        public void FixDimensions()
        {
            int MAX_DIM = 2048; // 3D texture max size.

            while (Mathf.Max(Mathf.Max(dimX, dimY), dimZ) > MAX_DIM)
            {
                GD.PrintErr("Dimension exceeds limits (maximum: " + MAX_DIM + "). Dataset is downscaled by 2 on each axis!");

                DownScaleData();
            }
        }

        /// <summary>
        /// Downscales the data by averaging 8 voxels per each new voxel,
        /// and replaces downscaled data with the original data
        /// </summary>
        public void DownScaleData()
        {
            int halfDimX = dimX / 2 + dimX % 2;
            int halfDimY = dimY / 2 + dimY % 2;
            int halfDimZ = dimZ / 2 + dimZ % 2;
            float[] downScaledData = new float[halfDimX * halfDimY * halfDimZ];

            for (int x = 0; x < halfDimX; x++)
            {
                for (int y = 0; y < halfDimY; y++)
                {
                    for (int z = 0; z < halfDimZ; z++)
                    {
                        downScaledData[x + y * halfDimX + z * (halfDimX * halfDimY)] = Mathf.Round(GetAvgerageVoxelValues(x * 2, y * 2, z * 2));
                    }
                }
            }

            //Update data & data dimensions
            data = downScaledData;
            dimX = halfDimX;
            dimY = halfDimY;
            dimZ = halfDimZ;
        }

        private void CalculateValueBounds(IProgressHandler progressHandler)
        {
            minDataValue = float.MaxValue;
            maxDataValue = float.MinValue;

            if (data != null)
            {
                int dimension = dimX * dimY * dimZ;
                int sliceDimension = dimX * dimY;
                for (int i = 0; i < dimension;)
                {
                    progressHandler.ReportProgress(i, dimension, "Calculating value bounds");
                    for (int j = 0; j < sliceDimension; j++, i++)
                    {
                        float val = data[i];
                        minDataValue = Mathf.Min(minDataValue, val);
                        maxDataValue = Mathf.Max(maxDataValue, val);
                    }
                }
            }
        }

        private async Task<ImageTexture3D> CreateTextureInternalAsync(IProgressHandler progressHandler)
        {
            GD.Print("Async texture generation. Hold on.");

            Format texformat = Format.Rf; //Format.Rh
            bool useMipmaps = false;
            bool isHalfFloat = texformat == Format.Rh;

            float minValue = 0;
            float maxValue = 0;
            float maxRange = 0;

            progressHandler.StartStage(0.1f, "Calculating value bounds");
            await Task.Run(() =>
            {
                minValue = GetMinDataValue();
                maxValue = GetMaxDataValue();
                maxRange = maxValue - minValue;
            });
            progressHandler.EndStage();

            ImageTexture3D texture = null;

            progressHandler.StartStage(0.2f, "Creating texture for slice 0/" + dimZ);
            Array<Image> imageArray = new Array<Image>();
            for (int z = 0; z < dimZ; z++)
            {
                progressHandler.ReportProgress(z, dimZ, "Creating texture for slice");
                Image image = Image.Create(dimX, dimY, useMipmaps, texformat);
                for (int y = 0; y < dimY; y++)
                {
                    for (int x = 0; x < dimX; x++)
                    {
                        float pixelValue = (float)(data[x + y * dimX + z * (dimX * dimY)] - minValue) / maxRange;
                        Color pixelColor = new Color(pixelValue, 0.0f, 0.0f, 0.0f);
                        image.SetPixel(x, y, pixelColor);
                    }
                }
                imageArray.Add(image);
            }
            texture = new ImageTexture3D();
            texture.Create(texformat, dimX, dimY, dimZ, useMipmaps, imageArray);
            progressHandler.EndStage();

            GD.Print("Texture generation done.");
            return texture;
        }

        private async Task<ImageTexture3D> CreateGradientTextureInternalAsync(IProgressHandler progressHandler)
        {
            GD.Print("Async gradient generation. Hold on.");

            Format texformat = Format.Rgbaf; //Format.Rh
            bool useMipmaps = false;

            float minValue = 0;
            float maxValue = 0;
            float maxRange = 0;
            ImageTexture3D texture = null;

            progressHandler.StartStage(0.2f, "Calculating value bounds");
            await Task.Run(() =>
            {
                if (minDataValue == float.MaxValue || maxDataValue == float.MinValue)
                    CalculateValueBounds(progressHandler);
                minValue = GetMinDataValue();
                maxValue = GetMaxDataValue();
                maxRange = maxValue - minValue;
            });
            progressHandler.EndStage();

            progressHandler.StartStage(0.4f, "Creating gradient texture");
            Array<Image> imageArray = new Array<Image>();
            await Task.Run(() =>
            {
                for (int z = 0; z < dimZ; z++)
                {
                    progressHandler.ReportProgress(z, dimZ, "Calculating gradients for slice");
                    Image image = Image.Create(dimX, dimY, useMipmaps, texformat);
                    for (int y = 0; y < dimY; y++)
                    {
                        for (int x = 0; x < dimX; x++)
                        {
                            int iData = x + y * dimX + z * (dimX * dimY);
                            Vector3 grad = GetGrad(x, y, z, minValue, maxRange);

                            Color pixelColor = new Color(grad.X, grad.Y, grad.Z, (float)(data[iData] - minValue) / maxRange);
                            image.SetPixel(x, y, pixelColor);
                        }
                    }
                    imageArray.Add(image);
                }
            });
            progressHandler.EndStage();

            progressHandler.StartStage(0.1f, "Uploading gradient texture");
            texture = new ImageTexture3D();
            texture.Create(texformat, dimX, dimY, dimZ, useMipmaps, imageArray);
            progressHandler.EndStage();

            GD.Print("Gradient gereneration done.");
            return texture;

        }
        public Vector3 GetGrad(int x, int y, int z, float minValue, float maxRange)
        {
            float x1 = data[Math.Min(x + 1, dimX - 1) + y * dimX + z * (dimX * dimY)] - minValue;
            float x2 = data[Math.Max(x - 1, 0) + y * dimX + z * (dimX * dimY)] - minValue;
            float y1 = data[x + Math.Min(y + 1, dimY - 1) * dimX + z * (dimX * dimY)] - minValue;
            float y2 = data[x + Math.Max(y - 1, 0) * dimX + z * (dimX * dimY)] - minValue;
            float z1 = data[x + y * dimX + Math.Min(z + 1, dimZ - 1) * (dimX * dimY)] - minValue;
            float z2 = data[x + y * dimX + Math.Max(z - 1, 0) * (dimX * dimY)] - minValue;

            return new Vector3((x2 - x1) / maxRange, (y2 - y1) / maxRange, (z2 - z1) / maxRange);
        }

        public float GetAvgerageVoxelValues(int x, int y, int z)
        {
            // if a dimension length is not an even number
            bool xC = x + 1 == dimX;
            bool yC = y + 1 == dimY;
            bool zC = z + 1 == dimZ;

            //if expression can only be true on the edges of the texture
            if (xC || yC || zC)
            {
                if (!xC && yC && zC) return (GetData(x, y, z) + GetData(x + 1, y, z)) / 2.0f;
                else if (xC && !yC && zC) return (GetData(x, y, z) + GetData(x, y + 1, z)) / 2.0f;
                else if (xC && yC && !zC) return (GetData(x, y, z) + GetData(x, y, z + 1)) / 2.0f;
                else if (!xC && !yC && zC) return (GetData(x, y, z) + GetData(x + 1, y, z) + GetData(x, y + 1, z) + GetData(x + 1, y + 1, z)) / 4.0f;
                else if (!xC && yC && !zC) return (GetData(x, y, z) + GetData(x + 1, y, z) + GetData(x, y, z + 1) + GetData(x + 1, y, z + 1)) / 4.0f;
                else if (xC && !yC && !zC) return (GetData(x, y, z) + GetData(x, y + 1, z) + GetData(x, y, z + 1) + GetData(x, y + 1, z + 1)) / 4.0f;
                else return GetData(x, y, z); // if xC && yC && zC
            }
            return (GetData(x, y, z) + GetData(x + 1, y, z) + GetData(x, y + 1, z) + GetData(x + 1, y + 1, z)
                    + GetData(x, y, z + 1) + GetData(x, y + 1, z + 1) + GetData(x + 1, y, z + 1) + GetData(x + 1, y + 1, z + 1)) / 8.0f;
        }

        public float GetData(int x, int y, int z)
        {
            return data[x + y * dimX + z * (dimX * dimY)];
        }
    }
}
