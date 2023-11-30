using System;
using Godot;

namespace VolumetricRendering
{
    public enum DensitySource
    {
        Unknown,
        Alpha,
        Grey
    }
    public static class DensityHelper
    {
        public static DensitySource IdentifyDensitySource(Color[] voxels)
        {
            DensitySource source = DensitySource.Unknown;

            for (int i = 0; i < voxels.Length - 1; i++)
            {
                if (!Mathf.IsEqualApprox(voxels[i].A, voxels[i + 1].A))
                {
                    source = DensitySource.Alpha;
                    break;
                }
                else if (!Mathf.IsEqualApprox(voxels[i].R, voxels[i + 1].R))
                {
                    source = DensitySource.Grey;
                    break;
                }
                else if (!Mathf.IsEqualApprox(voxels[i].G, voxels[i + 1].G))
                {
                    source = DensitySource.Grey;
                    break;
                }
                else if (!Mathf.IsEqualApprox(voxels[i].B, voxels[i + 1].B))
                {
                    source = DensitySource.Grey;
                    break;
                }
            }

            return source;
        }

        public static int[] ConvertColorsToDensities(Color[] colors)
        {
            DensitySource source = IdentifyDensitySource(colors);
            return ConvertColorsToDensities(colors, source);
        }

        public static int[] ConvertColorsToDensities(Color[] colors, DensitySource source)
        {
            int[] densities = new int[colors.Length];
            for (int i = 0; i < densities.Length; i++)
                densities[i] = ConvertColorToDensity(colors[i], source);
            return densities;
        }

        public static int ConvertColorToDensity(Color color, DensitySource source)
        {
            switch (source)
            {
                case DensitySource.Alpha:
                    return Mathf.RoundToInt(color.A * 255f);
                case DensitySource.Grey:
                    return Mathf.RoundToInt(color.R * 255f);
                default:
                    throw new ArgumentOutOfRangeException(source.ToString());
            }
        }

        public static Color ConvertDensityToColor(int density, DensitySource source)
        {
            float grey = source == DensitySource.Grey ? density / 255f : 0f;
            float alpha = source == DensitySource.Alpha ? density / 255f : 0f;

            return new Color()
            {
                R = grey,
                G = grey,
                B = grey,
                A = alpha
            };
        }
    }
}