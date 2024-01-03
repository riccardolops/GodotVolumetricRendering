using System;
using Godot;
using VolumetricRendering;

public partial class Control : Godot.Control
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    private void OnButtonPressed()
    {
        ShowOpenNRRDFilePopup();
    }
    private void ShowOpenFilePopup(string filter, string windowTitle, string fileSelectedHandler)
    {
        FileDialog dialog = new()
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Title = windowTitle,
            Access = FileDialog.AccessEnum.Filesystem
        };
        dialog.AddFilter(filter);
        _ = dialog.Connect("file_selected", new Callable(this, fileSelectedHandler));
        AddChild(dialog);
        dialog.PopupCentered();
    }
    public void ShowOpenNRRDFilePopup()
    {
        ShowOpenFilePopup("*.nrrd", "Open NRRD File", nameof(OnOpenNRRDDatasetResultAsync));
    }
    public async void OnOpenNRRDDatasetResultAsync(string path)
    {
        using (ProgressHandler progressHandler = new ProgressHandler(new AppProgressView(), "NRRD import"))
        {
            progressHandler.ReportProgress(0.0f, "Importing NRRD dataset");
            IImageFileImporter importer = ImporterFactory.CreateImageFileImporter(ImageFileFormat.NRRD);
            VolumeDataset dataset = await importer.ImportAsync(path);
            if (dataset != null)
            {
                VolumeRenderedObject volObj = await VolumeObjectFactory.CreateObjectAsync(dataset, progressHandler);
                AddChild(volObj);
            }
            else
            {
                GD.PrintErr("Failed to import NRRD dataset");
            }
        }

    }
}