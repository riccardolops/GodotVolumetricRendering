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
		public VolumeDataset dataset;
		[Export]
		public RenderMode RenderMode
		{
			get => renderMode;
			set
			{
				renderMode = value;
				volumeMaterial.SetShaderParameter("MODE", (int)renderMode);
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
				volumeMaterial.SetShaderParameter("_MinVal", visibilityWindow.X);
				volumeMaterial.SetShaderParameter("_MaxVal", visibilityWindow.Y);
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
				volumeMaterial.SetShaderParameter("_LightingGradientThresholdStart", gradientLightingThreshold.X);
				volumeMaterial.SetShaderParameter("_LightingGradientThresholdEnd", gradientLightingThreshold.Y);
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
				volumeMaterial.SetShaderParameter("_MinGradient", minGradient);
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
				volumeMaterial.SetShaderParameter("earlyRayTermianation", rayTerminationEnabled);
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
				volumeMaterial.SetShaderParameter("cubicInterpolation", cubicInterpolationEnabled);
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
				volumeMaterial.SetShaderParameter("useLighting", lightingEnabled);
			}
		}
		public ShaderMaterial volumeMaterial;
		public override void _EnterTree()
		{
			volumeMaterial ??= GetActiveMaterial(0) as ShaderMaterial;
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
			_ = UpdateMaterialPropertiesAsync(progressHandler);
		}
		private async Task UpdateMaterialPropertiesAsync(IProgressHandler progressHandler = null)
		{
			await updateMatLock.WaitAsync();
			try
			{
				volumeMaterial.SetShaderParameter("volumeDataSampler", textureDataset);
				bool useGradientTexture = tfRenderMode == TFRenderMode.TF2D || renderMode == RenderMode.IsosurfaceRendering || lightingEnabled;
				volumeMaterial.SetShaderParameter("volumeGradientSampler", textureGradient);
				volumeMaterial.SetShaderParameter("noiseSampler", noiseTexture);
				UpdateMatInternal();
			}
			finally
			{
				updateMatLock.Release();
			}
		}
		private void UpdateMatInternal()
		{
			if (tfRenderMode == TFRenderMode.TF2D)
			{
				volumeMaterial.SetShaderParameter("transferfunctionSampler", transferFunction2D.GetTexture());
				volumeMaterial.SetShaderParameter("useTransferFunction2D", true);
			}
			else
			{
				volumeMaterial.SetShaderParameter("transferfunctionSamplerColor", transferFunction.GetTextureColor());
				volumeMaterial.SetShaderParameter("transferfunctionSamplerAlpha", transferFunction.GetTextureAlpha());
				volumeMaterial.SetShaderParameter("useTransferFunction2D", false);
			}

			volumeMaterial.SetShaderParameter("useLighting", lightingEnabled);

			if (lightSource == LightSource.SceneMainLight)
				volumeMaterial.SetShaderParameter("useMainLight", true);
			else
				volumeMaterial.SetShaderParameter("useMainLight", false);

			switch (renderMode)
			{
				case RenderMode.DirectVolumeRendering:
					{
						volumeMaterial.SetShaderParameter("MODE", 1);
						break;
					}
				case RenderMode.MaximumIntensityProjection:
					{
						volumeMaterial.SetShaderParameter("MODE", 0);
						break;
					}
				case RenderMode.IsosurfaceRendering:
					{
						volumeMaterial.SetShaderParameter("MODE", 2);
						break;
					}
			}

			volumeMaterial.SetShaderParameter("_MinVal", visibilityWindow.X);
			volumeMaterial.SetShaderParameter("_MaxVal", visibilityWindow.Y);
			volumeMaterial.SetShaderParameter("_MinGradient", minGradient);
			volumeMaterial.SetShaderParameter("_TextureSize", sizeDataset);
			volumeMaterial.SetShaderParameter("_LightingGradientThresholdStart", gradientLightingThreshold.X);
			volumeMaterial.SetShaderParameter("_LightingGradientThresholdEnd", gradientLightingThreshold.Y);

			volumeMaterial.SetShaderParameter("earlyRayTermianation", rayTerminationEnabled);

			volumeMaterial.SetShaderParameter("cubicInterpolation", cubicInterpolationEnabled);
		}
	}
}
