# GodotVolumetricRendering

Volume rendering, implemented in Godot Engine.

This repository builds upon the [work by Matias Lavik](https://github.com/mlavik1/UnityVolumeRendering) implementing the same volumetric rendering in Godot 4

<img src="Screenshots/example.gif" width="600x">

## Features

- [x] Direct volume rendering, using 1D transfer functions
- [x] Maximum intensity projection
- [x] Isosurface rendering, using 1D transfer functions
- [x] Support for SITK file formats:
  - [x] DICOM support
  - [x] NRRD support
  - [x] NIFTII support
- [x] Lighting*
- [x] Transfer function editor*
- [x] Cubic sampling
- [x] Loading of RAW datasets
- [x] Complete transfer function editor

## Features yet to be ported over

- [ ] Cutout/clipping tools
- [ ] Lighting camera dependent
- [ ] 2D Transfer function

## How to use

Download [SimpleITK binaries](https://github.com/SimpleITK/SimpleITK/releases) for your platform using the "Download SITK" button.

Import your dataset in the scene using one of the import buttons.

You can change the VolumeRenderedObject properties in the inspector.

For an example of how to use the plugin, see [SampleViewer.cs](addons/volumetric_importer/scripts/Samples/SampleViewer.cs), used in the [sample scene](main.tscn).
