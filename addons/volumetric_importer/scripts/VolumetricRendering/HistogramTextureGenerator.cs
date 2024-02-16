using System;
using Godot;

namespace VolumetricRendering
{
    /// <summary>
    /// Utility class for generating histograms for the dataset.
    /// </summary>
    public class HistogramTextureGenerator
    {
        /// <summary>
        /// Generates a histogram where:
        ///   X-axis = the data sample (density) value
        ///   Y-axis = the sample count (number of data samples with the specified density)
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        public static ImageTexture GenerateHistogramTexture(VolumeDataset dataset)
        {
            float minValue = dataset.GetMinDataValue();
            float maxValue = dataset.GetMaxDataValue();
            float valueRange = maxValue - minValue;

            int numFrequencies = Mathf.Min((int)valueRange, 1024);
            int[] frequencies = new int[numFrequencies];

            int maxFreq = 0;
            float valRangeRecip = 1.0f / (maxValue - minValue);
            for (int iData = 0; iData < dataset.data.Length; iData++)
            {
                float dataValue = dataset.data[iData];
                float tValue = (dataValue - minValue) * valRangeRecip;
                int freqIndex = (int)(tValue * (numFrequencies - 1));
                frequencies[freqIndex] += 1;
                maxFreq = System.Math.Max(frequencies[freqIndex], maxFreq);
            }

            float[] samples = new float[numFrequencies];

            for (int iSample = 0; iSample < numFrequencies; iSample++)
                samples[iSample] = Log10((float)frequencies[iSample]) / Log10((float)maxFreq);

            byte[] byteArray = new byte[samples.Length * sizeof(float)];
            Buffer.BlockCopy(samples, 0, byteArray, 0, byteArray.Length);
            Image image = Image.CreateFromData(numFrequencies, 1, false, Image.Format.Rf, byteArray);
            return ImageTexture.CreateFromImage(image);
        }

        private static float Log10(float x)
        {
            return Mathf.Log(x) / Mathf.Log(10.0f);
        }
    }
}
