using Godot;

namespace VolumetricRendering
{
    public class ImporterUtilsInternal
    {
        public static void ConvertLPSToGodotCoordinateSpace(VolumeDataset volumeDataset)
        {
            volumeDataset.scale = new Vector3(
                -volumeDataset.scale.X,
                volumeDataset.scale.Y,
                -volumeDataset.scale.Z
            );
            volumeDataset.rotation = Quaternion.FromEuler(new Vector3(270.0f, 0.0f, 0.0f));
        }
    }
}
