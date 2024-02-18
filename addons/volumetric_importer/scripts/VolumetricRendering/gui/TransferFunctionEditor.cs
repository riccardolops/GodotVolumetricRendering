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

		private TransferFunction transferFunction;
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

			if (paletteTextureRect != null)
			{
				paletteTextureRect.Texture = transferFunction.GetTextureColor();
			}

			UpdatePoints();
		}

		private void UpdatePoints()
		{
			while (alphaControls.Count < transferFunction.alphaControlPoints.Count)
			{
				MovablePanel alphaControl = GD.Load<PackedScene>("res://addons/volumetric_importer/gui/AlphaControl.tscn").Instantiate<MovablePanel>();
				alphaPanel.AddChild(alphaControl);
				alphaControls.Add(alphaControl);
			}

			while (alphaControls.Count > transferFunction.alphaControlPoints.Count)
			{
				alphaPanel.RemoveChild(alphaControls[alphaControls.Count - 1]);
				alphaControls.RemoveAt(alphaControls.Count - 1);
			}

			for (int i = 0; i < alphaControls.Count; i++)
			{
				MovablePanel alphaControl = alphaControls[i];
				TFAlphaControlPoint alphaPoint = transferFunction.alphaControlPoints[i];
				if (alphaControl.IsMoving())
				{
					Vector2 value = (alphaControl.Position + alphaControl.Size * 0.5f) / alphaPanel.Size;
					alphaPoint.dataValue = value.X;
					alphaPoint.alphaValue = 1.0f - value.Y;
					transferFunction.alphaControlPoints[i] = alphaPoint;
				}
				else
				{
					Vector2 alphaPos = alphaPanel.Size * new Vector2(alphaPoint.dataValue, alphaPoint.alphaValue);
					alphaPos.Y = alphaPanel.Size.Y - alphaPos.Y;
					alphaPos -= alphaControl.Size * 0.5f;
					alphaControl.Position = alphaPos;
				}
			}
			transferFunction.GenerateTextureAlpha(); // TODO
			transferFunction.GenerateTextureColor(); // TODO
			targetObject.UpdateMaterialProperties();
		}

		public override void _Ready()
		{
			/*transferFunction = new TransferFunction();
			transferFunction.AddControlPoint(new TFColourControlPoint(0.0f, new Color(0.11f, 0.14f, 0.13f, 1.0f)));
			transferFunction.AddControlPoint(new TFColourControlPoint(0.2415f, new Color(0.469f, 0.354f, 0.223f, 1.0f)));
			transferFunction.AddControlPoint(new TFColourControlPoint(0.3253f, new Color(1.0f, 1.0f, 1.0f, 1.0f)));

			transferFunction.AddControlPoint(new TFAlphaControlPoint(0.0f, 0.0f));
			transferFunction.AddControlPoint(new TFAlphaControlPoint(0.1787f, 0.0f));
			transferFunction.AddControlPoint(new TFAlphaControlPoint(0.2f, 0.024f));
			transferFunction.AddControlPoint(new TFAlphaControlPoint(0.28f, 0.03f));
			transferFunction.AddControlPoint(new TFAlphaControlPoint(0.4f, 0.546f));
			transferFunction.AddControlPoint(new TFAlphaControlPoint(0.547f, 0.5266f));
			InitialiseContent();*/
		}

		public override void _Process(double delta)
		{
			if (transferFunction != null) // TODO
				UpdatePoints();

			if (targetObject == null || alphaPanel == null)
				return;

			if (transferFunction != targetObject.transferFunction)
			{
				transferFunction = targetObject.transferFunction;
				InitialiseContent();
			}

		}
	}
}
