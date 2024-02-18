#if TOOLS
using Godot;

namespace VolumetricRendering
{
    public partial class VolumeRenderedObjectInspector : EditorInspectorPlugin
    {
        public override bool _CanHandle(GodotObject @object)
        {
            return @object.GetType() == typeof(VolumeRenderedObject);
        }

        public override void _ParseEnd(GodotObject @object)
        {
            VolumeRenderedObject volumeRenderedObject = @object as VolumeRenderedObject;

            Button btShowTFEditor = new Button();
            btShowTFEditor.Text = "Edit transfer function";
            btShowTFEditor.Pressed += () => {
                TransferFunctionEditor tfEditor = GD.Load<PackedScene>("res://addons/volumetric_importer/gui/TransferFunctionEditor.tscn").Instantiate<TransferFunctionEditor>();
                tfEditor.targetObject = volumeRenderedObject;
                Window tfEditorWindow = new Window();
                tfEditorWindow.AddChild(tfEditor);
                tfEditorWindow.CloseRequested += () => {
                    EditorInterface.Singleton.GetEditorViewport3D().RemoveChild(tfEditorWindow);
                };
                EditorInterface.Singleton.GetEditorViewport3D().AddChild(tfEditorWindow);
                tfEditorWindow.Size = new Vector2I(1100, 900);
                tfEditorWindow.PopupCentered(new Vector2I(400, 300));
            };
            AddCustomControl(btShowTFEditor);
            base._ParseEnd(@object);
        }
    }
}
#endif