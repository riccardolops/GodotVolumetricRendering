using System.Collections.Generic;
using System.Numerics;
using Godot;
using Godot.NativeInterop;

namespace VolumetricRendering
{
    public enum CrossSectionType
    {
        Plane = 1,
        BoxInclusive = 2,
        BoxExclusive = 3,
        SphereInclusive = 4,
        SphereExclusive = 5
    }

    public struct CrossSectionData
    {
        public CrossSectionType type;
        public Transform3D matrix;
    }

    /// <summary>
    /// Manager for all cross section objects (planes and boxes).
    /// </summary>
    [Tool]
    public partial class CrossSectionManager : Node
    {
        private const int MAX_CROSS_SECTIONS = 8;

        /// <summary>
        /// Volume dataset to cross section.
        /// </summary>
        private VolumeRenderedObject targetObject;
        private List<ICrossSectionObject> crossSectionObjects = new List<ICrossSectionObject>();
        private Transform3D[] crossSectionMatrices = new Transform3D[MAX_CROSS_SECTIONS];
        private float[] crossSectionTypes = new float[MAX_CROSS_SECTIONS];
        private CrossSectionData[] crossSectionData = new CrossSectionData[MAX_CROSS_SECTIONS];
        public CrossSectionData[] GetCrossSectionData()
        {
            return crossSectionData;
        }

        public void AddCrossSectionObject(ICrossSectionObject crossSectionObject)
        {
            crossSectionObjects.Add(crossSectionObject);
        }

        public void RemoveCrossSectionObject(ICrossSectionObject crossSectionObject)
        {
            crossSectionObjects.Remove(crossSectionObject);
        }

        /// <summary>
        /// Called when the node enters the scene tree for the first time.
        /// </summary>
        public override void _Ready()
        {
            targetObject = (VolumeRenderedObject)GetParent();
        }

        /// <summary>
        /// Called every frame. 'delta' is the elapsed time since the previous frame.
        /// </summary>
        /// <param name="delta"></param>
        public override void _Process(double delta)
        {
            if (targetObject == null)
                return;

            ShaderMaterial mat = targetObject.GetActiveMaterial(0) as ShaderMaterial;

            if (crossSectionObjects.Count > 0)
            {
                int numCrossSections = System.Math.Min(crossSectionObjects.Count, MAX_CROSS_SECTIONS);

                for (int i = 0; i < numCrossSections; i++)
                {
                    ICrossSectionObject crossSectionObject = crossSectionObjects[i];
                    crossSectionMatrices[i] = crossSectionObject.GetMatrix();
                    crossSectionTypes[i] = (int)crossSectionObject.GetCrossSectionType();
                    crossSectionData[i] = new CrossSectionData() { type = crossSectionObjects[i].GetCrossSectionType(), matrix = crossSectionMatrices[i] };
                }
                Projection a = new Projection();
                mat.SetShaderParameter("cross_section_on", true);
                mat.SetShaderParameter("_CrossSectionMatrices", a);
                mat.SetShaderParameter("_CrossSectionTypes", crossSectionTypes);
                mat.SetShaderParameter("_NumCrossSections", numCrossSections);
            }
            else
            {
                mat.SetShaderParameter("cross_section_on", false);
            }
        }
        public void SpawnCrossSectionPlane()
        {
            CrossSectionPlane crossSectionPlane = new CrossSectionPlane();
            AddCrossSectionObject(crossSectionPlane);
            AddChild(crossSectionPlane);
        }
    }
}