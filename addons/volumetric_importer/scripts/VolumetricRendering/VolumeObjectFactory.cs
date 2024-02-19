using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace VolumetricRendering
{
    public static class VolumeObjectFactory
    {
        public static VolumeRenderedObject CreateObject(VolumeDataset dataset)
        {
            VolumeRenderedObject volObj = new()
            {
                Name = "VolumeRenderedObject_" + dataset.datasetName,
                Scale = dataset.scale,
                Mesh = new BoxMesh
                {
                    FlipFaces = true
                }
            };
            Material mat = ResourceLoader.Load<Material>("res://addons/volumetric_importer/materials/raymarch_material.tres");
            mat = mat.Duplicate() as Material;
            volObj.Mesh.SurfaceSetMaterial(0, mat);
            volObj.textureDataset = dataset.GetDataTexture();
            volObj.textureGradient = dataset.GetGradientTexture();
            volObj.sizeDataset = new Vector3I(dataset.dimX, dataset.dimY, dataset.dimZ);
            volObj.dataset = dataset;
            return volObj;
        }
        public static async Task<VolumeRenderedObject> CreateObjectAsync(VolumeDataset dataset, IProgressHandler progressHandler = null)
        {
            VolumeRenderedObject volObj = new();
            await Task.Run(() =>
                {
                    volObj.Name = "VolumeRenderedObject_" + dataset.datasetName;
                    volObj.Scale = dataset.scale;
                    volObj.Quaternion = dataset.rotation;
                    volObj.Mesh = new BoxMesh
                    {
                        FlipFaces = true
                    };
                    Material mat = ResourceLoader.Load<Material>("res://addons/volumetric_importer/materials/raymarch_material.tres");
                    mat = mat.Duplicate() as Material;
                    volObj.Mesh.SurfaceSetMaterial(0, mat);
                });
            volObj.textureDataset = await dataset.GetDataTextureAsync(progressHandler);
            volObj.textureGradient = await dataset.GetGradientTextureAsync(progressHandler);
            volObj.sizeDataset = new Vector3I(dataset.dimX, dataset.dimY, dataset.dimZ);
            volObj.dataset = dataset;

            NoiseTexture2D noiseTexture = NoiseTextureGenerator.GenerateNoiseTexture(512, 512);
            volObj.noiseTexture = noiseTexture;

            return volObj;
        }
    }
}
