@tool
extends EditorImportPlugin

func _get_importer_name():
    return "volumetric_importer.sitk"

func _get_visible_name():
    return "VolumeDataset"

func _get_recognized_extensions():
    return ["nrrd", "nhdr", "nia", "nii", "gz", "hdr", "img"]

func _get_save_extension():
    return "res"

func _get_resource_type():
    return "VolumeDataset"

func _get_preset_count():
    return 1

func _get_import_order():
    return 0

func _get_option_visibility(path, option, options):
    if option == "import_folder":
        var file_extension = path.get_extension()
        if file_extension in ["dcm", "dicom"]:
            return true
    return false


func _get_preset_name(preset_index):
    return "Default"

func _get_import_options(path, preset_index):
    return [{"name": "import_folder", "default_value": false}]

func _get_priority():
    return 1

func _import(source_file, save_path, options, platform_variants, gen_files):
    var sitk_importer = load("res://addons/sitkvolumetricimporter/SITKImporter.cs").new()
    var result = sitk_importer.Load(ProjectSettings.globalize_path(source_file))
    if result == FAILED:
        return FAILED
    var dimensions := sitk_importer.getDimensions() as Vector3i
    var spacing := sitk_importer.getSpacing() as Vector3

    var volume = VolumeDataset.new()
    volume.scale = Vector3(-(spacing.x * dimensions.x / 1000), (spacing.y * dimensions.y / 1000), -(spacing.z * dimensions.z / 1000))
    volume.rotation = Quaternion.from_euler(Vector3(deg_to_rad(90), 0, 0))

    ######

    sitk_importer.EvaluateTextures()
    volume.volume = sitk_importer.volume_texture
    volume.gradient = sitk_importer.gradient_texture
    volume.histogram = sitk_importer.histogram_texture

    ######

    var filename = save_path + "." + _get_save_extension()
    return ResourceSaver.save(volume, filename)
