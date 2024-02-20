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

        private List<MovableButton> alphaControls = new List<MovableButton>();
        private List<MovableButton> colourControls = new List<MovableButton>();
        private BaseButton addColourButton = null;
        private BaseButton addAlphaButton = null;

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

            if (addColourButton == null && colourPanel != null)
            {
                addColourButton = new TextureButton();
                addColourButton.LayoutMode = 1;
                addColourButton.SetAnchorsPreset(LayoutPreset.FullRect);
                addColourButton.Pressed += () => {
                    TFColourControlPoint colourPoint = new TFColourControlPoint();
                    colourPoint.dataValue = colourPanel.GetLocalMousePosition().X / colourPanel.Size.X;
                    colourPoint.colourValue = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                    transferFunction.AddControlPoint(colourPoint);
                    OpenColourPicker(transferFunction.GetNumColourControlPoints() - 1);
                };
                colourPanel.AddChild(addColourButton);
            }

            if (addAlphaButton == null && alphaPanel != null)
            {
                addAlphaButton = new TextureButton();
                addAlphaButton.LayoutMode = 1;
                addAlphaButton.SetAnchorsPreset(LayoutPreset.FullRect);
                addAlphaButton.Pressed += () => {
                    TFAlphaControlPoint alphaPoint = new TFAlphaControlPoint();
                    alphaPoint.dataValue = alphaPanel.GetLocalMousePosition().X / alphaPanel.Size.X;
                    alphaPoint.alphaValue = 1.0f - alphaPanel.GetLocalMousePosition().Y / alphaPanel.Size.Y;
                    transferFunction.AddControlPoint(alphaPoint);
                };
                alphaPanel.AddChild(addAlphaButton);
            }
        }

        private void UpdateAlphaPoints()
        {
            while (alphaControls.Count < transferFunction.GetNumAlphaControlPoints())
            {
                MovableButton alphaControl = GD.Load<PackedScene>("res://addons/volumetric_importer/gui/AlphaControl.tscn").Instantiate<MovableButton>();
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
                MovableButton alphaControl = alphaControls[i];
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

        private void UpdateColourPoints()
        {
            while (colourControls.Count < transferFunction.GetNumColourControlPoints())
            {
                MovableButton colourControl = GD.Load<PackedScene>("res://addons/volumetric_importer/gui/ColourControl.tscn").Instantiate<MovableButton>();
                colourControl.Pressed += () => {
                int colourIndex = colourControls.IndexOf(colourControl);
                   OpenColourPicker(colourIndex);
                };
                colourPanel.AddChild(colourControl);
                colourControls.Add(colourControl);
            }

            while (colourControls.Count > transferFunction.GetNumColourControlPoints())
            {
                colourPanel.RemoveChild(colourControls[colourControls.Count - 1]);
                colourControls.RemoveAt(colourControls.Count - 1);
            }

            for (int i = 0; i < colourControls.Count; i++)
            {
                MovableButton colourControl = colourControls[i];
                TFColourControlPoint colourPoint = transferFunction.GetColourControlPoint(i);
                if (colourControl.IsMoving())
                {
                    colourPoint.dataValue = (colourControl.Position.X + colourControl.Size.X * 0.5f) / colourPanel.Size.X;
                    transferFunction.SetColourControlPoint(i, colourPoint);
                }
                else
                {
                    Vector2 newPosition = colourPanel.Size * new Vector2(colourPoint.dataValue, 0.0f);
                    newPosition.Y = 0.0f;
                    newPosition.X -= colourControl.Size.X * 0.5f;
                    colourControl.Position = newPosition;
                    colourControl.Size = new Vector2(colourControl.Size.X, colourPanel.Size.Y);
                }
            }
        }

        private void UpdatePoints()
        {
            if (alphaPanel != null)
                UpdateAlphaPoints();

            if (colourPanel != null)
                UpdateColourPoints();
        }

        private void OpenColourPicker(int colourIndex)
        {
            if (colourIndex >= transferFunction.GetNumColourControlPoints())
                return;

            ColorPicker colourPicker = new ColorPicker();
            Popup popup = new Popup();
            popup.AddChild(colourPicker);
            colourPicker.Color = transferFunction.GetColourControlPoint(colourIndex).colourValue;
            colourPicker.ColorChanged += (Color colour) => {
                if (colourIndex < transferFunction.GetNumColourControlPoints())
                {
                    TFColourControlPoint colourPoint = transferFunction.GetColourControlPoint(colourIndex);
                    colourPoint.colourValue = colour;
                    transferFunction.SetColourControlPoint(colourIndex, colourPoint);
                }
            };
            AddChild(popup);
            popup.PopupCentered();
        }

        public override void _Input(InputEvent @event)
        {
            InputEventKey keyEvent = @event as InputEventKey;
            if (keyEvent != null && keyEvent.Keycode == Key.Delete)
            {
                for (int i = 0; i < colourControls.Count; i++)
                {
                    if (colourControls[i].HasFocus())
                    {
                        colourControls[i].ReleaseFocus();
                        transferFunction.RemoveColourControlPoint(i);
                        return;
                    }
                }
                for (int i = 0; i < alphaControls.Count; i++)
                {
                    if (alphaControls[i].HasFocus())
                    {
                        alphaControls[i].ReleaseFocus();
                        transferFunction.RemoveAlphaControlPoint(i);
                        return;
                    }
                }
            }
            base._Input(@event);
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
