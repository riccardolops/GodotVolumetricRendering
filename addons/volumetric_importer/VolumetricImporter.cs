#if TOOLS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

namespace VolumetricRendering
{
    [Tool]
    public partial class VolumetricImporter : EditorPlugin
    {
        private Control dock;
        private EditorFileDialog dialog;
        private ProgressBar progressView;
        private RichTextLabel progressText;
        private Callable _buildCallable;
        private VolumeRenderedObjectInspector volumeRenderedObjectInspector;

        public override void _EnterTree()
        {
            volumeRenderedObjectInspector = new VolumeRenderedObjectInspector();
            AddInspectorPlugin(volumeRenderedObjectInspector);

            dock = new()
            {
                Name = "Volumetric Importer"
            };
            MarginContainer margin = new()
            {
                AnchorTop = 0.0f,
                AnchorBottom = 1.0f,
                AnchorLeft = 0.0f,
                AnchorRight = 1.0f
            };
            dock.AddChild(margin);
            VBoxContainer vbox = new();
            margin.AddChild(vbox);
            Button SITKButton = new()
            {
                Text = "Download SITK"
            };
            Button NRRDButton = new()
            {
                Text = "Import NRRD dataset"
            };
            Button NiFTiButton = new()
            {
                Text = "Import NiFTi dataset"
            };
            Button DICOMButton = new()
            {
                Text = "Import DICOM dataset"
            };
            Button rawButton = new()
            {
                Text = "Import raw dataset"
            };
            progressText = new()
            {
                FitContent = true,
                Text = "Importing..."
            };
            progressView = new();
            vbox.AddChild(SITKButton);
            vbox.AddChild(NRRDButton);
            vbox.AddChild(NiFTiButton);
            vbox.AddChild(DICOMButton);
            vbox.AddChild(rawButton);
            vbox.AddChild(progressText);
            vbox.AddChild(progressView);
            AddControlToDock(DockSlot.LeftUl, dock);
            _ = SITKButton.Connect("pressed", new Callable(this, nameof(DownloadSITKBinaries)));
            _ = NRRDButton.Connect("pressed", new Callable(this, nameof(ShowOpenNRRDFilePopup)));
            _ = NiFTiButton.Connect("pressed", new Callable(this, nameof(ShowOpenNiFTiFilePopup)));
            _ = DICOMButton.Connect("pressed", new Callable(this, nameof(ShowOpenDICOMFolderPopup)));
            _ = rawButton.Connect("pressed", new Callable(this, nameof(ShowOpenRawFilePopup)));
            FindEditorBuildShortcut();
        }

        public override void _ExitTree()
        {
            RemoveInspectorPlugin(volumeRenderedObjectInspector);
            RemoveControlFromDocks(dock);
            dock.Free();
        }
        private void ShowCreateFilePopup(string filter, string windowTitle, string fileSelectedHandler)
        {
            dialog = new();
            dialog.AddFilter(filter);
            dialog.FileMode = EditorFileDialog.FileModeEnum.SaveFile;
            dialog.Title = windowTitle;
            _ = dialog.Connect("file_selected", new Callable(this, fileSelectedHandler));
            EditorInterface.Singleton.GetBaseControl().AddChild(dialog);
            dialog.Popup(new Rect2I(50, 50, 700, 500));
        }
        private void ShowOpenFilePopup(string filter, string windowTitle, string fileSelectedHandler)
        {
            dialog = new()
            {
                FileMode = EditorFileDialog.FileModeEnum.OpenFile,
                Title = windowTitle,
                Access = EditorFileDialog.AccessEnum.Filesystem
            };
            dialog.AddFilter(filter);
            _ = dialog.Connect("file_selected", new Callable(this, fileSelectedHandler));
            EditorInterface.Singleton.GetBaseControl().AddChild(dialog);
            dialog.PopupCentered();
        }
        private void ShowOpenFolderPopup(string windowTitle, string folderSelectedHandler)
        {
            dialog = new()
            {
                FileMode = EditorFileDialog.FileModeEnum.OpenDir,
                Title = windowTitle,
                Access = EditorFileDialog.AccessEnum.Filesystem
            };
            _ = dialog.Connect("dir_selected", new Callable(this, folderSelectedHandler));
            EditorInterface.Singleton.GetBaseControl().AddChild(dialog);
            dialog.PopupCentered();
        }
        public void DownloadSITKBinaries()
        {
            SimpleITKManager.DownloadBinaries(_buildCallable);
        }
        public void ShowOpenNRRDFilePopup()
        {
            ShowOpenFilePopup("*.nrrd", "Open NRRD File", nameof(OnOpenNRRDDatasetResultAsync));
        }
        public void ShowOpenNiFTiFilePopup()
        {
            ShowOpenFilePopup("*.nii.gz", "Open NiFTi File", nameof(OnNiFTiFileSelected));
        }
        public void ShowOpenDICOMFolderPopup()
        {
            ShowOpenFolderPopup("Open DICOM Folder", nameof(OnDICOMFolderSelected));
        }
        public void ShowOpenRawFilePopup()
        {
            ShowOpenFilePopup("*.*", "Open raw File", nameof(OnRawFileSelected));
        }
        public async void OnOpenNRRDDatasetResultAsync(string path)
        {
            using ProgressHandler progressHandler = new(new EditorProgressView(progressView, progressText), "NRRD import");
            progressHandler.ReportProgress(0.0f, "Importing NRRD dataset");
            IImageFileImporter importer = ImporterFactory.CreateImageFileImporter(ImageFileFormat.NRRD);
            VolumeDataset dataset = await importer.ImportAsync(path);
            if (dataset != null)
            {
                Node root = GetTree().EditedSceneRoot;
                VolumeRenderedObject volObj = await VolumeObjectFactory.CreateObjectAsync(dataset, progressHandler);
                root.AddChild(volObj);
                volObj.Owner = root.GetTree().EditedSceneRoot;
            }
            else
            {
                GD.PrintErr("Failed to import NRRD dataset");
            }
        }
        public async void OnNiFTiFileSelected(string path)
        {
            using ProgressHandler progressHandler = new(new EditorProgressView(progressView, progressText), "NiFTi import");
            progressHandler.ReportProgress(0.0f, "Importing NiFTi dataset");
            IImageFileImporter importer = ImporterFactory.CreateImageFileImporter(ImageFileFormat.NIFTI);
            VolumeDataset dataset = await importer.ImportAsync(path);
            if (dataset != null)
            {
                Node root = GetTree().EditedSceneRoot;
                VolumeRenderedObject volObj = await VolumeObjectFactory.CreateObjectAsync(dataset, progressHandler);
                root.AddChild(volObj);
                volObj.Owner = root.GetTree().EditedSceneRoot;
            }
            else
            {
                GD.PrintErr("Failed to import NiFTi dataset");
            }
        }
        public async void OnDICOMFolderSelected(string path)
        {
            const bool recursive = true;
            // Read all files
            IEnumerable<string> fileCandidates = Directory.EnumerateFiles(path, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(p => p.EndsWith(".dcm", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicom", StringComparison.InvariantCultureIgnoreCase) || p.EndsWith(".dicm", StringComparison.InvariantCultureIgnoreCase));

            // Import the dataset
            IImageSequenceImporter importer = ImporterFactory.CreateImageSequenceImporter(ImageSequenceFormat.DICOM);
            IEnumerable<IImageSequenceSeries> seriesList = await importer.LoadSeriesAsync(fileCandidates);
            float numVolumesCreated = 0;
            Node root = GetTree().EditedSceneRoot;
            foreach (IImageSequenceSeries series in seriesList)
            {
                ProgressHandler progressHandler = new(new EditorProgressView(progressView, progressText), "NiFTi import");
                progressHandler.ReportProgress(0.0f, "Importing DICOM dataset");
                VolumeDataset dataset = await importer.ImportSeriesAsync(series);
                // Spawn the object
                if (dataset != null)
                {
                    VolumeRenderedObject volObj = await VolumeObjectFactory.CreateObjectAsync(dataset, progressHandler);
                    volObj.Position = new Vector3(numVolumesCreated, 0, 0);
                    root.AddChild(volObj);
                    volObj.Owner = root.GetTree().EditedSceneRoot;
                    numVolumesCreated++;
                }
            }
        }
        public void OnRawFileSelected(string path)
        {
            RawDatasetImporterDialog importerDialog = new RawDatasetImporterDialog(path, 128, 256, 256, DataContentFormat.Uint8, Endianness.LittleEndian, 0);
            importerDialog.Title = "Raw dataset import settings";
            importerDialog.onConfirmed += ImportRawDataset;
            EditorInterface.Singleton.GetEditorViewport3D().AddChild(importerDialog);
            importerDialog.PopupCentered(new Vector2I(400, 300));
        }
        public async void ImportRawDataset(RawDatasetImporter importer)
        {
            using ProgressHandler progressHandler = new(new EditorProgressView(progressView, progressText), "NiFTi import");
            progressHandler.ReportProgress(0.0f, "Importing raw dataset");

            VolumeDataset dataset = await importer.ImportAsync();
            if (dataset != null)
            {
                Node root = GetTree().EditedSceneRoot;
                VolumeRenderedObject volObj = await VolumeObjectFactory.CreateObjectAsync(dataset, progressHandler);
                root.AddChild(volObj);
                volObj.Owner = root.GetTree().EditedSceneRoot;
            }
            else
            {
                GD.PrintErr("Failed to import NiFTi dataset");
            }
        }
        void FindEditorBuildShortcut()
        {
            // See https://github.com/lewiji/RebuildCsOnFocus/blob/main/addons/rebuild_cs_on_focus/RebuildCsPlugin.cs
            var node = new Control();
            AddControlToBottomPanel(node, "");
            var bottomBar = node.GetParent();
            RemoveControlFromBottomPanel(node);
            node.QueueFree();
            var msBuildPanel = bottomBar.GetChildren()
               .FirstOrDefault(c => c is MarginContainer && c.HasMethod("RebuildProject"));
            if (msBuildPanel != null)
            {
                _buildCallable = new Callable(msBuildPanel, "RebuildProject");
            }
        }
    }
}
#endif
