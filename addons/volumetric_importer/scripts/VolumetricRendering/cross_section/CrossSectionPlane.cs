using Godot;

namespace VolumetricRendering
{
    /// <summary>
    /// Cross section plane.
    /// Used for cutting a model (cross section view).
    /// </summary>
    [Tool]
    public partial class CrossSectionPlane : MeshInstance3D, ICrossSectionObject
    {
        /// <summary>
        /// Volume dataset to cross section.
        /// </summary>
        [Export]
        private VolumeRenderedObject targetObject;

        /// <summary>
        /// Called when the node enters the scene tree for the first time.
        /// </summary>
        public override void _Ready()
        {
            targetObject = (VolumeRenderedObject)(GetParent().GetParent());
        }
        public override void _EnterTree()
        {
            if (targetObject != null)
                targetObject.GetCrossSectionManager().AddCrossSectionObject(this);
        }
        public override void _ExitTree()
        {
            if (targetObject != null)
                targetObject.GetCrossSectionManager().RemoveCrossSectionObject(this);
        }

        public CrossSectionType GetCrossSectionType()
        {
            return CrossSectionType.Plane;
        }

        public Transform3D GetMatrix()
        {
            return Transform * targetObject.GlobalTransform;
        }
    }
}