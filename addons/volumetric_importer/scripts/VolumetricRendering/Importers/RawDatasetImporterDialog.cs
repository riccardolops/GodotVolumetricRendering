using Godot;
using System;
using System.Diagnostics;
using System.Linq;

namespace VolumetricRendering
{
    [Tool]
    public partial class RawDatasetImporterDialog : ConfirmationDialog
    {
        public event Action<RawDatasetImporter> onConfirmed;

        private string datasetPath;
        private int dimX, dimY, dimZ;
        private DataContentFormat dataFormat;
        private Endianness endianness;
        private int bytesToSkip;

        private RawDatasetImporterControl contentControl;

        public RawDatasetImporterDialog(string datasetPath, int dimX, int dimY, int dimZ, DataContentFormat dataFormat, Endianness endianness, int bytesToSkip)
        {
            this.datasetPath = datasetPath;
            this.dimX = dimX;
            this.dimY = dimY;
            this.dimZ = dimZ;
            this.dataFormat = dataFormat;
            this.endianness = endianness;
            this.bytesToSkip = bytesToSkip;

            this.Confirmed += () => {
                int dimX = (int)contentControl.ctrlDimX.Value;
                int dimY = (int)contentControl.ctrlDimY.Value;
                int dimZ = (int)contentControl.ctrlDimZ.Value;
                DataContentFormat dataFormat = (DataContentFormat)Enum.Parse(typeof(DataContentFormat), contentControl.ctrlDataFormat.Text);
                Endianness endianness = (Endianness)Enum.Parse(typeof(Endianness), contentControl.ctrlEndianness.Text);
                int bytesToSkip = (int)contentControl.ctrlBytesToSkip.Value;
                RawDatasetImporter datasetImporter = new RawDatasetImporter(ProjectSettings.GlobalizePath(datasetPath), dimX, dimY, dimZ, dataFormat, endianness, bytesToSkip);
                onConfirmed?.Invoke(datasetImporter);
            };
        }

        public override void _EnterTree()
        {
            contentControl = GD.Load<PackedScene>("res://addons/volumetric_importer/gui/RawDatasetImporterDialog.tscn").Instantiate<RawDatasetImporterControl>();
            AddChild(contentControl);
            contentControl.ctrlDimX.Value = dimX;
            contentControl.ctrlDimY.Value = dimY;
            contentControl.ctrlDimZ.Value = dimZ;
            contentControl.ctrlDataFormat.Selected = Array.IndexOf(Enum.GetValues(typeof(DataContentFormat)), dataFormat);
            contentControl.ctrlEndianness.Selected = Array.IndexOf(Enum.GetValues(typeof(Endianness)), endianness);
            contentControl.ctrlBytesToSkip.Value = bytesToSkip;
        }

        public override void _ExitTree()
        {
            RemoveChild(contentControl);
            contentControl.Free();
        }
    }
}
