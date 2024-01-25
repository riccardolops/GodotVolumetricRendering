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
        public ImageTexture3D textureDataset;
        public ImageTexture3D textureGradient;
        public Vector3I sizeDataset;
        public NoiseTexture2D noiseTexture;
        private SemaphoreSlim updateMatLock = new(1, 1);
        private TFRenderMode tfRenderMode;
        private LightSource lightSource;
        private CrossSectionManager crossSectionManager;
        private RenderMode renderMode = RenderMode.MaximumIntensityProjection;
        [Export]
        public RenderMode RenderMode
        {
            get => renderMode;
            set
            {
                renderMode = value;
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("MODE", (int)renderMode);
            }
        }
        private Vector2 visibilityWindow = new(0.001f, 1.0f);
        [Export(PropertyHint.Range, "0, 1")]
        public Vector2 VisibilityWindow
        {
            get => visibilityWindow;
            set
            {
                visibilityWindow = new Vector2(
                    Mathf.Clamp(value.X, 0f, value.Y),
                    Mathf.Clamp(value.Y, value.X, 1f)
                );
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("_MinVal", visibilityWindow.X);
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("_MaxVal", visibilityWindow.Y);
            }
        }
        private Vector2 gradientLightingThreshold = new(0.02f, 0.15f);
        [Export(PropertyHint.Range, "0, 1")]
        public Vector2 GradientLightingThreshold
        {
            get => gradientLightingThreshold;
            set
            {
                gradientLightingThreshold = new Vector2(
                    Mathf.Clamp(value.X, 0f, value.Y),
                    Mathf.Clamp(value.Y, value.X, 1f)
                );
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("_LightingGradientThresholdStart", gradientLightingThreshold.X);
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("_LightingGradientThresholdEnd", gradientLightingThreshold.Y);
            }
        }
        private float minGradient = 0.01f;
        [Export(PropertyHint.Range, "0, 1")]
        public float MinGradient
        {
            get => minGradient;
            set
            {
                minGradient = Mathf.Clamp(value, 0f, 1f);
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("_MinGradient", minGradient);
            }
        }
        private bool rayTerminationEnabled = false;
        [Export]
        public bool RayTerminationEnabled
        {
            get => rayTerminationEnabled;
            set
            {
                rayTerminationEnabled = value;
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("earlyRayTermianation", rayTerminationEnabled);
            }
        }
        private bool cubicInterpolationEnabled = false;
        [Export]
        public bool CubicInterpolationEnabled
        {
            get => cubicInterpolationEnabled;
            set
            {
                cubicInterpolationEnabled = value;
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("cubicInterpolation", cubicInterpolationEnabled);
            }
        }
        private bool lightingEnabled;
        [Export]
        public bool LightingEnabled
        {
            get => lightingEnabled;
            set
            {
                lightingEnabled = value;
                (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("useLighting", lightingEnabled);
            }
        }
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
        public CrossSectionManager GetCrossSectionManager()
        {
            if (crossSectionManager == null)
            {
                crossSectionManager = new CrossSectionManager();
                AddChild(crossSectionManager);
            }
            return crossSectionManager;
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

            (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("useLighting", lightingEnabled);

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
                case RenderMode.MaximumIntensityProjection:
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

            (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("earlyRayTermianation", rayTerminationEnabled);

            (GetActiveMaterial(0) as ShaderMaterial).SetShaderParameter("cubicInterpolation", cubicInterpolationEnabled);
        }
    }
}
