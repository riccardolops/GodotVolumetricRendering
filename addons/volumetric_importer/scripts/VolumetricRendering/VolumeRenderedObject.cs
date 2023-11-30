using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace VolumetricRendering
{
    public enum LightSource
    {
        ActiveCamera,
        SceneMainLight
    }
    [Tool]
    public partial class VolumeRenderedObject : MeshInstance3D
    {
        public TransferFunction transferFunction;
        public TransferFunction2D transferFunction2D;
        [Export]
        public ImageTexture3D textureDataset;
        [Export]
        public ImageTexture3D textureGradient;
        [Export]
        public Vector3I sizeDataset;
        [Export]
        public NoiseTexture2D noiseTexture;
        private SemaphoreSlim updateMatLock = new SemaphoreSlim(1, 1);
        private RenderMode renderMode = RenderMode.MaximumIntensityProjectipon;
        private TFRenderMode tfRenderMode;
        private bool lightingEnabled;
        private LightSource lightSource;
        private Vector2 visibilityWindow = new(0.0f, 1000.0f);
        private bool rayTerminationEnabled = false;
        private Vector2 gradientLightingThreshold = new Vector2(0.02f, 0.15f);
        private float minGradient = 0.01f;
        private bool cubicInterpolationEnabled = false;
        public override void _EnterTree()
        {
        }
        /// <summary>
        /// Called when the node enters the scene tree for the first time.
        /// </summary>
        public override void _Ready()
        {
            UpdateMaterialProperties();
        }

        /// <summary>
        /// Called every frame. 'delta' is the elapsed time since the previous frame.
        /// </summary>
        /// <param name="delta"></param>
        public override void _Process(double delta)
        {
        }
        private void UpdateMaterialProperties(IProgressHandler progressHandler = null)
        {
            Task task = UpdateMaterialPropertiesAsync(progressHandler);
        }
        private async Task UpdateMaterialPropertiesAsync(IProgressHandler progressHandler = null)
        {
            await updateMatLock.WaitAsync();
            try
            {
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("volumeDataSampler", textureDataset);
                bool useGradientTexture = tfRenderMode == TFRenderMode.TF2D || renderMode == RenderMode.IsosurfaceRendering || lightingEnabled;
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("volumeGradientSampler", textureGradient);
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("noiseSampler", noiseTexture);
                UpdateMatInternal();
            }
            finally
            {
                updateMatLock.Release();
            }
        }
        private void UpdateMatInternal()
        {
            GD.Print("UpdateMatInternal");
            if (tfRenderMode == TFRenderMode.TF2D)
            {
                GD.Print("TF2D");
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("transferfunctionSampler", transferFunction2D.GetTexture());
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("useTransferFunction2D", true);
            }
            else
            {
                GD.Print("TF1D");
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("transferfunctionSamplerColor", transferFunction.GetTextureColor());
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("transferfunctionSamplerAlpha", transferFunction.GetTextureAlpha());
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("useTransferFunction2D", false);
            }

            if (lightingEnabled)
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("useLighting", true);
            else
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("useLighting", false);

            if (lightSource == LightSource.SceneMainLight)
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("useMainLight", true);
            else
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("useMainLight", false);

            switch (renderMode)
            {
                case RenderMode.DirectVolumeRendering:
                    {
                        (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("MODE", 1);
                        break;
                    }
                case RenderMode.MaximumIntensityProjectipon:
                    {
                        (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("MODE", 0);
                        break;
                    }
                case RenderMode.IsosurfaceRendering:
                    {
                        (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("MODE", 2);
                        break;
                    }
            }

            (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("_MinVal", visibilityWindow.X);
            (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("_MaxVal", visibilityWindow.Y);
            (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("_MinGradient", minGradient);
            (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("_TextureSize", sizeDataset);
            (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("_LightingGradientThresholdStart", gradientLightingThreshold.X);
            (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("_LightingGradientThresholdEnd", gradientLightingThreshold.Y);

            if (rayTerminationEnabled)
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("RAY_TERMINATE_ON", true);
            else
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("RAY_TERMINATE_ON", false);

            if (cubicInterpolationEnabled)
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("CUBIC_INTERPOLATION_ON", true);
            else
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("CUBIC_INTERPOLATION_ON", false);
        }
    }
}
