#if TOOLS
using Godot;

namespace VolumetricRendering
{
    public partial class TransferFunctionInspector : EditorInspectorPlugin
    {
        // This is a hack to make sure we can show the histogram in the TF editor.
        // TODO: Can we get the "parent" of the TransferFunctionInspector instead?
        public static VolumeRenderedObject volumeRenderedObject = null;

        public override bool _CanHandle(GodotObject @object)
        {
            return @object.GetType() == typeof(TransferFunction);
        }

        public override void _ParseBegin(GodotObject @object)
        {
            TransferFunction transferFunction = @object as TransferFunction;

            Button btShowTFEditor = new Button();
            btShowTFEditor.Text = "Edit transfer function";
            btShowTFEditor.Pressed += () => {
                TransferFunctionEditor tfEditor = GD.Load<PackedScene>("res://addons/volumetric_importer/gui/TransferFunctionEditor.tscn").Instantiate<TransferFunctionEditor>();
                tfEditor.transferFunction = transferFunction;
                if (volumeRenderedObject != null)
                    tfEditor.targetObject = volumeRenderedObject;
                Window tfEditorWindow = new Window();
                tfEditor.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                tfEditorWindow.AddChild(tfEditor);
                tfEditorWindow.CloseRequested += () => {
                    EditorInterface.Singleton.GetEditorViewport3D().RemoveChild(tfEditorWindow);
                };
                EditorInterface.Singleton.GetEditorViewport3D().AddChild(tfEditorWindow);
                tfEditorWindow.PopupCentered(new Vector2I(1024, 768));
            };
            AddCustomControl(btShowTFEditor);
            base._ParseBegin(@object);
        }
    }
}
#endif
