using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace VolumetricRendering
{
    [GlobalClass]
    [Tool]
    public partial class TransferFunction : Resource
    {
        [Export]
        private Gradient gradientColor = new Gradient();
        [Export]
        private Gradient gradientAlpha = new Gradient();

        private GradientTexture1D textureColor = null;
        private GradientTexture1D textureAlpha = null;

        private const int width = 512;
        private const bool use_hdr = false;

        public TransferFunction()
        {
            CreateTextureColor();
            CreateTextureAlpha();
        }

        public void AddControlPoint(TFColourControlPoint ctrlPoint)
        {
            gradientColor.AddPoint(ctrlPoint.dataValue, ctrlPoint.colourValue);
        }

        public void AddControlPoint(TFAlphaControlPoint ctrlPoint)
        {
            gradientAlpha.AddPoint(ctrlPoint.dataValue, new Color(0.0f, 0.0f, 0.0f, ctrlPoint.alphaValue));
        }

        public int GetNumColourControlPoints()
        {
            return gradientColor.GetPointCount();
        }

        public int GetNumAlphaControlPoints()
        {
            return gradientAlpha.GetPointCount();
        }

        public TFColourControlPoint GetColourControlPoint(int index)
        {
            TFColourControlPoint ctrlPoint = new TFColourControlPoint();
            ctrlPoint.dataValue = gradientColor.GetOffset(index);
            ctrlPoint.colourValue = gradientColor.GetColor(index);
            return ctrlPoint;
        }

        public TFAlphaControlPoint GetAlphaControlPoint(int index)
        {
            TFAlphaControlPoint ctrlPoint = new TFAlphaControlPoint();
            ctrlPoint.dataValue = gradientAlpha.GetOffset(index);
            ctrlPoint.alphaValue = gradientAlpha.GetColor(index).A;
            return ctrlPoint;
        }

        public void SetColourControlPoint(int index, TFColourControlPoint value)
        {
            gradientColor.SetOffset(index, value.dataValue);
            gradientColor.SetColor(index, value.colourValue);
        }

        public void SetAlphaControlPoint(int index, TFAlphaControlPoint value)
        {
            gradientAlpha.SetOffset(index, value.dataValue);
            gradientAlpha.SetColor(index, new Color(0.0f, 0.0f, 0.0f, value.alphaValue));
        }

        public GradientTexture1D GetTextureColor()
        {
            textureColor.Gradient = gradientColor;
            return textureColor;
        }

        public GradientTexture1D GetTextureAlpha()
        {
            textureAlpha.Gradient = gradientAlpha;
            return textureAlpha;
        }

        public Color GetColour(float x)
        {
            return new Color(gradientColor.Sample(x), gradientAlpha.Sample(x).A);
        }

        private void CreateTextureColor()
        {
            textureColor = new GradientTexture1D();
            textureColor.Width = width;
            textureColor.UseHdr = use_hdr;
            if (gradientColor == null)
                gradientColor = new Gradient();
            textureColor.Gradient = gradientColor;
        }
        private void CreateTextureAlpha()
        {
            textureAlpha = new GradientTexture1D();
            textureAlpha.Width = width;
            textureAlpha.UseHdr = use_hdr;
            if (gradientAlpha == null)
                gradientAlpha = new Gradient();
            textureAlpha.Gradient = gradientAlpha;
        }
    }
}
