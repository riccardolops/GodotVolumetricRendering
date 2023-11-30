using Godot;

namespace VolumetricRendering
{
    public class NoiseTextureGenerator
    {
        public static NoiseTexture2D GenerateNoiseTexture(int noiseDimX, int noiseDimY)
        {
            NoiseTexture2D noiseTexture = new NoiseTexture2D();
            noiseTexture.Height = noiseDimY;
            noiseTexture.Width = noiseDimX;
            noiseTexture.Noise = new FastNoiseLite();
            return noiseTexture;
        }
    }
}
