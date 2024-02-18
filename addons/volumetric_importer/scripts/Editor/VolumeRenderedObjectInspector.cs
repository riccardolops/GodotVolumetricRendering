#if TOOLS
using Godot;

namespace VolumetricRendering
{
    public partial class VolumeRenderedObjectInspector : EditorInspectorPlugin
    {
        private TransferFunctionInspector transferFunctionInspector = new TransferFunctionInspector();

        public override bool _CanHandle(GodotObject @object)
        {
            return @object.GetType() == typeof(VolumeRenderedObject);
        }

        public override void _ParseBegin(GodotObject @object)
        {
            // This is a hack to make sure we can show the histogram in the TF editor.
            // TODO: Can we get the "parent" of the TransferFunctionInspector instead?
            TransferFunctionInspector.volumeRenderedObject = @object as VolumeRenderedObject;
            base._ParseBegin(@object);
        }
    }
}
#endif