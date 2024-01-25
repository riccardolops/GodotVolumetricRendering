using System;
using System.Collections.Generic;
using Godot;

namespace VolumetricRendering
{
    public class TransferFunction
    {
        public List<TFColourControlPoint> colourControlPoints = new();
        public List<TFAlphaControlPoint> alphaControlPoints = new();

        private GradientTexture1D textureColor = null;
        private GradientTexture1D textureAlpha = null;
        private Gradient gradientColor = null;
        private Gradient gradientAlpha = null;

        private const int width = 512;
        private const bool use_hdr = false;
        public void AddControlPoint(TFColourControlPoint ctrlPoint)
        {
            colourControlPoints.Add(ctrlPoint);
        }

        public void AddControlPoint(TFAlphaControlPoint ctrlPoint)
        {
            alphaControlPoints.Add(ctrlPoint);
        }

        public GradientTexture1D GetTextureColor()
        {
            GD.Print("GetTexture");
            if (textureColor == null)
                GenerateTextureColor();
            return textureColor;
        }

        public GradientTexture1D GetTextureAlpha()
        {
            GD.Print("GetTexture");
            if (textureAlpha == null)
                GenerateTextureAlpha();
            return textureAlpha;
        }
        public void GenerateTextureColor()
        {
            if (textureColor == null)
                CreateTextureColor();

            List<TFColourControlPoint> cols = new(colourControlPoints);

            // Sort lists of control points
            cols.Sort((a, b) => (a.dataValue.CompareTo(b.dataValue)));

            // Add colour points at beginning and end
            if (cols.Count == 0 || cols[cols.Count - 1].dataValue < 1.0f)
                cols.Add(new TFColourControlPoint(1.0f, Colors.White));
            if (cols[0].dataValue > 0.0f)
                cols.Insert(0, new TFColourControlPoint(0.0f, Colors.White));

            foreach (TFColourControlPoint col in cols)
            {
                gradientColor.AddPoint(col.dataValue, col.colourValue);
            }
            textureColor.Gradient = gradientColor;
        }
        public void GenerateTextureAlpha()
        {
            if (textureAlpha == null)
                CreateTextureAlpha();

            List<TFAlphaControlPoint> alphas = new(alphaControlPoints);

            // Sort lists of control points
            alphas.Sort((a, b) => (a.dataValue.CompareTo(b.dataValue)));

            // Add alpha points at beginning and end
            if (alphas.Count == 0 || alphas[alphas.Count - 1].dataValue < 1.0f)
                alphas.Add(new TFAlphaControlPoint(1.0f, 1.0f));
            if (alphas[0].dataValue > 0.0f)
                alphas.Insert(0, new TFAlphaControlPoint(0.0f, 0.0f));

            foreach (TFAlphaControlPoint alpha in alphas)
            {
                gradientAlpha.AddPoint(alpha.dataValue, new Color(0.0f, 0.0f, 0.0f, alpha.alphaValue));
            }
            textureAlpha.Gradient = gradientAlpha;
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
            gradientColor = new Gradient();
        }
        private void CreateTextureAlpha()
        {
            textureAlpha = new GradientTexture1D();
            textureAlpha.Width = width;
            textureAlpha.UseHdr = use_hdr;
            gradientAlpha = new Gradient();
        }
    }
}
