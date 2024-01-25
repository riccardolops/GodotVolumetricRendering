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
            VolumeRenderedObject volObj = new();
            volObj.Name = "VolumeRenderedObject_" + dataset.datasetName;
            volObj.Scale = dataset.scale;
            // volObj.RotationDegrees = dataset.rotation.GetEuler();
            volObj.Mesh = new BoxMesh
            {
                FlipFaces = true
            };
            volObj.Mesh.SurfaceSetMaterial(0, ResourceLoader.Load<Material>("res://addons/volumetric_importer/materials/raymarch_material.tres"));
            volObj.textureDataset = dataset.GetDataTexture();
            volObj.textureGradient = dataset.GetGradientTexture();
            volObj.sizeDataset = new Vector3I(dataset.dimX, dataset.dimY, dataset.dimZ);
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
                    volObj.Mesh.SurfaceSetMaterial(0, ResourceLoader.Load<Material>("res://addons/volumetric_importer/materials/raymarch_material.tres"));
                });
            volObj.textureDataset = await dataset.GetDataTextureAsync(progressHandler);
            volObj.textureGradient = await dataset.GetGradientTextureAsync(progressHandler);
            volObj.sizeDataset = new Vector3I(dataset.dimX, dataset.dimY, dataset.dimZ);
            TransferFunction tf = new();
            tf.AddControlPoint(new TFColourControlPoint(0.0f, new Color(0.11f, 0.14f, 0.13f, 1.0f)));
            tf.AddControlPoint(new TFColourControlPoint(0.2415f, new Color(0.469f, 0.354f, 0.223f, 1.0f)));
            tf.AddControlPoint(new TFColourControlPoint(0.3253f, new Color(1.0f, 1.0f, 1.0f, 1.0f)));

            tf.AddControlPoint(new TFAlphaControlPoint(0.0f, 0.0f));
            tf.AddControlPoint(new TFAlphaControlPoint(0.1787f, 0.0f));
            tf.AddControlPoint(new TFAlphaControlPoint(0.2f, 0.024f));
            tf.AddControlPoint(new TFAlphaControlPoint(0.28f, 0.03f));
            tf.AddControlPoint(new TFAlphaControlPoint(0.4f, 0.546f));
            tf.AddControlPoint(new TFAlphaControlPoint(0.547f, 0.5266f));

            volObj.transferFunction = tf;

            NoiseTexture2D noiseTexture = NoiseTextureGenerator.GenerateNoiseTexture(512, 512);
            volObj.noiseTexture = noiseTexture;

            return volObj;
        }
    }
}
