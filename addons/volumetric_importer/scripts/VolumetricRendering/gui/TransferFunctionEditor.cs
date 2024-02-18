using Godot;
using System;
using System.Collections.Generic;

namespace VolumetricRendering
{
	[GlobalClass]
	[Tool]
	public partial class TransferFunctionEditor : Panel
	{
		[Export]
		public Control alphaPanel;

		[Export]
		public Control colourPanel;

		[Export]
		public TextureRect histogramTextureRect;

		[Export]
		public TextureRect paletteTextureRect;

		[Export]
		public VolumeRenderedObject targetObject;

		[Export]
		public TransferFunction transferFunction;

		private List<MovablePanel> alphaControls = new List<MovablePanel>();
		private List<MovablePanel> colourControls = new List<MovablePanel>();

		private void InitialiseContent()
		{
			if (histogramTextureRect != null && targetObject != null)
			{
				histogramTextureRect.Texture = HistogramTextureGenerator.GenerateHistogramTexture(targetObject.dataset);
				ShaderMaterial shaderMaterial = histogramTextureRect.Material as ShaderMaterial;
				if (shaderMaterial != null)
				{
					shaderMaterial.SetShaderParameter("tf_tex_colour", transferFunction.GetTextureColor());
					shaderMaterial.SetShaderParameter("tf_tex_alpha", transferFunction.GetTextureAlpha());
				}
			}

			if (paletteTextureRect != null && transferFunction != null)
			{
				paletteTextureRect.Texture = transferFunction.GetTextureColor();
			}

			UpdatePoints();
		}

		private void UpdateAlphaPoints()
		{
			while (alphaControls.Count < transferFunction.GetNumAlphaControlPoints())
			{
				MovablePanel alphaControl = GD.Load<PackedScene>("res://addons/volumetric_importer/gui/AlphaControl.tscn").Instantiate<MovablePanel>();
				alphaPanel.AddChild(alphaControl);
				alphaControls.Add(alphaControl);
			}

			while (alphaControls.Count > transferFunction.GetNumAlphaControlPoints())
			{
				alphaPanel.RemoveChild(alphaControls[alphaControls.Count - 1]);
				alphaControls.RemoveAt(alphaControls.Count - 1);
			}

			for (int i = 0; i < alphaControls.Count; i++)
			{
				MovablePanel alphaControl = alphaControls[i];
				TFAlphaControlPoint alphaPoint = transferFunction.GetAlphaControlPoint(i);
				if (alphaControl.IsMoving())
				{
					Vector2 value = (alphaControl.Position + alphaControl.Size * 0.5f) / alphaPanel.Size;
					alphaPoint.dataValue = value.X;
					alphaPoint.alphaValue = 1.0f - value.Y;
					transferFunction.SetAlphaControlPoint(i, alphaPoint);
				}
				else
				{
					Vector2 alphaPos = alphaPanel.Size * new Vector2(alphaPoint.dataValue, alphaPoint.alphaValue);
					alphaPos.Y = alphaPanel.Size.Y - alphaPos.Y;
					alphaPos -= alphaControl.Size * 0.5f;
					alphaControl.Position = alphaPos;
				}
			}
		}

		private void UpdatePoints()
		{
			if (alphaPanel != null)
				UpdateAlphaPoints();
		}

		public override void _Ready()
		{
			InitialiseContent();
		}

		public override void _Process(double delta)
		{
			if (transferFunction == null && targetObject == null)
				return;

			if (transferFunction == null && targetObject != null)
			{
				transferFunction = targetObject.transferFunction;
				InitialiseContent();
			}

			UpdatePoints();
		}
	}
}
