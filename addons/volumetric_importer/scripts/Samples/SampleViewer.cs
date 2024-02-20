using Godot;
using System;
using VolumetricRendering;

public partial class SampleViewer : Node3D
{
    public override void _Ready()
    {
        VolumeRenderedObject volumeRenderedObject = ImportRawDataset("res://sample_datasets/VisMale.raw", 128, 256, 256, DataContentFormat.Uint8, Endianness.LittleEndian, 0);
        volumeRenderedObject.RenderMode = RenderMode.DirectVolumeRendering;
        volumeRenderedObject.GlobalRotation = new Vector3(Mathf.RadToDeg(-90.0f), Mathf.RadToDeg(180.0f), 0.0f);
        ShowTransferFunctionEditor(volumeRenderedObject);
    }

    private VolumeRenderedObject ImportRawDataset(string filePath, int dimX, int dimY, int dimZ, DataContentFormat contentFormat, Endianness endianness, int skipBytes)
    {
        RawDatasetImporter datasetImporter = new RawDatasetImporter(filePath, 128, 256, 256, DataContentFormat.Uint8, Endianness.LittleEndian, 0);
        VolumeDataset dataset = datasetImporter.Import();
        if (dataset != null)
        {
            VolumeRenderedObject volObj = VolumeObjectFactory.CreateObject(dataset);
            AddChild(volObj);
            return volObj;
        }
        else
        {
            GD.PrintErr("Failed to import RAW dataset");
        }
        return null;
    }

    private void ShowTransferFunctionEditor(VolumeRenderedObject volumeRenderedObject)
    {
        TransferFunctionEditor tfEditor = GD.Load<PackedScene>("res://addons/volumetric_importer/gui/TransferFunctionEditor.tscn").Instantiate<TransferFunctionEditor>();
        tfEditor.transferFunction = volumeRenderedObject.transferFunction;
        tfEditor.targetObject = volumeRenderedObject;
        Window tfEditorWindow = new Window();
        tfEditor.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        tfEditorWindow.AddChild(tfEditor);
        tfEditorWindow.CloseRequested += () => {
            RemoveChild(tfEditorWindow);
        };
        AddChild(tfEditorWindow);
        tfEditorWindow.PopupCentered(new Vector2I(800, 600));
    }
}
