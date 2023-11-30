#if TOOLS
using System.Threading.Tasks;
using Godot;

namespace VolumetricRendering
{
    [Tool]
    public partial class VolumetricImporter : EditorPlugin
    {
        private Control dock;
        private EditorFileDialog dialog;

        public override void _EnterTree()
        {
            AddCustomType("VolumeRenderedObject", "MeshInstance3D", ResourceLoader.Load("res://addons/volumetric_importer/VolumetricRendering/VolumeRenderedObject.cs") as CSharpScript, ResourceLoader.Load("res://addons/volumetric_importer/icons/VolumeRenderedObject.svg") as Texture2D);
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
            vbox.AddChild(NRRDButton);
            vbox.AddChild(NiFTiButton);
            vbox.AddChild(DICOMButton);
            AddControlToDock(DockSlot.LeftUl, dock);
            _ = NRRDButton.Connect("pressed", new Callable(this, nameof(ShowOpenNRRDFilePopup)));
            _ = NiFTiButton.Connect("pressed", new Callable(this, nameof(ShowOpenNiFTiFilePopup)));
            _ = DICOMButton.Connect("pressed", new Callable(this, nameof(ShowOpenDICOMFolderPopup)));
        }

        public override void _ExitTree()
        {
            RemoveCustomType("VolumeRenderedObject");
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
            GetEditorInterface().GetBaseControl().AddChild(dialog);
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
            GetEditorInterface().GetBaseControl().AddChild(dialog);
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
            GetEditorInterface().GetBaseControl().AddChild(dialog);
            dialog.PopupCentered();
        }
        public void ShowOpenNRRDFilePopup()
        {
            ShowOpenFilePopup("*.nrrd", "Open NRRD File", nameof(OnOpenNRRDDatasetResultAsync));
        }
        public void ShowOpenNiFTiFilePopup()
        {
            ShowOpenFilePopup("*.nifti", "Open NiFTi File", nameof(OnNiFTiFileSelected));
        }
        public void ShowOpenDICOMFolderPopup()
        {
            ShowOpenFolderPopup("Open DICOM Folder", nameof(OnDICOMFolderSelected));
        }
        public async void OnOpenNRRDDatasetResultAsync(string path)
        {
            IImageFileImporter importer = ImporterFactory.CreateImageFileImporter(ImageFileFormat.NRRD);
            VolumeDataset dataset = await importer.ImportAsync(path);
            if (dataset != null)
            {
                Node root = GetTree().EditedSceneRoot;
                VolumeRenderedObject volObj = await VolumeObjectFactory.CreateObjectAsync(dataset);
                root.AddChild(volObj);
                volObj.Owner = root.GetTree().EditedSceneRoot;
            }
            else
            {
                GD.PrintErr("Failed to import NRRD dataset");
            }
        }
        public static void OnNiFTiFileSelected(string path)
        {
            GD.Print(path);
        }
        public static void OnDICOMFolderSelected(string path)
        {
            GD.Print(path);
        }
    }
}
#endif
