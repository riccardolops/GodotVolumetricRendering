using System;
using System.Diagnostics;
using Godot;

namespace VolumetricRendering
{
	[Tool]
	public partial class RawDatasetImporterControl : Control
	{
		[Export]
		public SpinBox ctrlDimX;

		[Export]
		public SpinBox ctrlDimY;

		[Export]
		public SpinBox ctrlDimZ;

		[Export]
		public SpinBox ctrlBytesToSkip;

		[Export]
		public OptionButton ctrlDataFormat;

		[Export]
		public OptionButton ctrlEndianness;

		public override void _EnterTree()
		{
			base._EnterTree();

			string[] dataFormatNames = Enum.GetNames(typeof(DataContentFormat));
			ctrlDataFormat.ItemCount = dataFormatNames.Length;
			for (int i = 0; i < dataFormatNames.Length; i++)
			{
				ctrlDataFormat.SetItemText(i, dataFormatNames[i]);
			}
			ctrlDataFormat.Selected = 0;

			string[] endiannessNames = Enum.GetNames(typeof(Endianness));
			ctrlEndianness.ItemCount = endiannessNames.Length;
			for (int i = 0; i < endiannessNames.Length; i++)
			{
				ctrlEndianness.SetItemText(i, endiannessNames[i]);
			}
			ctrlEndianness.Selected = 0;
		}

		public override void _ExitTree()
		{
			base._ExitTree();
		}
	}
}
