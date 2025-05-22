#include "vds_editor_plugin.h"
#include "volume_dataset.h"
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/classes/file_access.hpp>
#include <godot_cpp/classes/resource_saver.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

#include <godot_cpp/classes/image.hpp>

using namespace godot;

enum DataFormat
{
    Uint8,
    Int8,
    Uint16,
    Int16,
    Uint32,
    Int32
};

enum Endianness
{
    LittleEndian,
    BigEndian
};

void VDSImportPlugin::_bind_methods() {}

String VDSImportPlugin::_get_importer_name() const { return "volumetric_importer.raw"; }
String VDSImportPlugin::_get_visible_name() const { return "VolumeDataset"; }
PackedStringArray VDSImportPlugin::_get_recognized_extensions() const
{
    PackedStringArray extensions = PackedStringArray();
    extensions.append("raw");
    return extensions;
}
String VDSImportPlugin::_get_save_extension() const { return "res"; }
String VDSImportPlugin::_get_resource_type() const { return "VolumeDataset"; }
int VDSImportPlugin::_get_preset_count() const { return 1; }
String VDSImportPlugin::_get_preset_name(int preset_index) const { return "Default"; }
float VDSImportPlugin::_get_priority() const { return 1.0; }
int VDSImportPlugin::_get_import_order() const { return ResourceImporter::IMPORT_ORDER_DEFAULT; }
TypedArray<Dictionary> VDSImportPlugin::_get_import_options(const String &path, int p_idx) const
{
    Dictionary optDimX = Dictionary();
    optDimX["name"] = "Dim x";
    optDimX["default_value"] = 128;
    
    Dictionary optDimY = Dictionary();
    optDimY["name"] = "Dim y";
    optDimY["default_value"] = 256;
    
    Dictionary optDimZ = Dictionary();
    optDimZ["name"] = "Dim z";
    optDimZ["default_value"] = 256;

    Dictionary optBytesToSkip = Dictionary();
    optBytesToSkip["name"] = "Bytes to skip";
    optBytesToSkip["default_value"] = 0;

    Dictionary optDataFormat = Dictionary();
    optDataFormat["name"] = "Data format";
    optDataFormat["default_value"] = DataFormat::Uint8;
    optDataFormat["property_hint"] = PropertyHint::PROPERTY_HINT_ENUM;
    optDataFormat["hint_string"] = String("Uint8,Int8,Uint16,Int16,Uint32,Int32");

    Dictionary optEndianness = Dictionary();
    optEndianness["name"] = "Endianness";
    optEndianness["default_value"] = Endianness::LittleEndian;
    optEndianness["property_hint"] = PropertyHint::PROPERTY_HINT_ENUM;
    optEndianness["hint_string"] = String("LittleEndian,BigEndian");

    TypedArray<Dictionary> options;
    options.append(optDimX);
    options.append(optDimY);
    options.append(optDimZ);
    options.append(optBytesToSkip);
    options.append(optDataFormat);
    options.append(optEndianness);
    return options;
}

bool VDSImportPlugin::_get_option_visibility(const String &p_path, const StringName &p_option_name, const Dictionary &p_options) const
{
    return true;
}

Error VDSImportPlugin::_import(const String &source_file, const String &save_path, const Dictionary &options, const TypedArray<String> &platform_variants, const TypedArray<String> &gen_files) const
{
    Ref<FileAccess> file = FileAccess::open(source_file, FileAccess::READ);
    if (!file.is_valid())
    {
        return ERR_FILE_CANT_OPEN;
    }
    file->set_big_endian(int(options.get("Endianness", Variant::INT)) == int(Endianness::BigEndian));

    int dim_x = int(options.get("Dim x", Variant::INT));
    int dim_y = int(options.get("Dim y", Variant::INT));
    int dim_z = int(options.get("Dim z", Variant::INT));
    int bytes_to_skip = int(options.get("Bytes to skip", Variant::INT));

    unsigned long size_per_element;
    switch (int(options.get("Data format", Variant::INT)))
    {
    case DataFormat::Uint8:
        size_per_element = sizeof(uint8_t);
        break;
    case DataFormat::Int8:
        size_per_element = sizeof(int8_t);
        break;
    case DataFormat::Uint16:
        size_per_element = sizeof(uint16_t);
        break;
    case DataFormat::Int16:
        size_per_element = sizeof(int16_t);
        break;
    case DataFormat::Uint32:
        size_per_element = sizeof(uint32_t);
        break;
    case DataFormat::Int32:
        size_per_element = sizeof(int32_t);
        break;
    default:
        break;
    }

    int expected_size = dim_x * dim_y * dim_z * size_per_element + bytes_to_skip;
    if (file->get_length() < expected_size)
    {
        return ERR_FILE_CORRUPT;
    }

    if (bytes_to_skip > 0)
    {
        file->seek(bytes_to_skip);
    }

    Ref<VolumeDataset> vds = memnew(VolumeDataset);
    int dimension = dim_x * dim_y * dim_z;
    TypedArray<float> data_array = TypedArray<float>();
    for (int i = 0; i < dimension; i++)
    {
        if (size_per_element == sizeof(uint8_t))
        {
            data_array.append(file->get_8());
        }
        else if (size_per_element == sizeof(uint16_t))
        {
            data_array.append(file->get_16());
        }
        else if (size_per_element == sizeof(uint32_t))
        {
            data_array.append(file->get_32());
        }
    }
    float min_value = data_array.min();
    float max_value = data_array.max();
    float range = max_value - min_value;

    TypedArray<Image> slices = TypedArray<Image>();
    TypedArray<Image> gradients = TypedArray<Image>();
    for (int z = 0; z < dim_z; z++)
    {
        Ref<Image> slice = Image::create_empty(dim_x, dim_y, false, Image::FORMAT_RF);
        Ref<Image> gradient = Image::create_empty(dim_x, dim_y, false, Image::FORMAT_RGBAF);
        for (int y = 0; y < dim_y; y++)
        {
            for (int x = 0; x < dim_x; x++)
            {
                int index = x + y * dim_x + z * dim_x * dim_y;
                float value = data_array[index];
                slice->set_pixel(x, y, Color((value - min_value) / range, 0, 0, 0));
                float x1 = float(data_array[MIN(x + 1, dim_x - 1) + y * dim_x + z * dim_x * dim_y]) - min_value;
                float x2 = float(data_array[MAX(x - 1, 0) + y * dim_x + z * dim_x * dim_y]) - min_value;
                float y1 = float(data_array[x + MIN(y + 1, dim_y - 1) * dim_x + z * dim_x * dim_y]) - min_value;
                float y2 = float(data_array[x + MAX(y - 1, 0) * dim_x + z * dim_x * dim_y]) - min_value;
                float z1 = float(data_array[x + y * dim_x + MIN(z + 1, dim_z - 1) * dim_x * dim_y]) - min_value;
                float z2 = float(data_array[x + y * dim_x + MAX(z - 1, 0) * dim_x * dim_y]) - min_value;
                gradient->set_pixel(x, y, Color((x2 - x1) / range, (y2 - y1) / range, (z2 - z1) / range, (value - min_value) / range));
            }
        }
        slices.push_back(slice);
        gradients.push_back(gradient);
    }

    int _n_frequencies = MIN((int)range, 1024);
    int *_frequencies = new int[_n_frequencies]();
    int _max_frequency = 0;
    for (int i = 0; i < data_array.size(); i++) {
        float data_value = data_array[i];
        int frequency_index = (int)(((data_value - min_value) / range) * (_n_frequencies - 1));
        _frequencies[frequency_index]++;
        _max_frequency = MAX(_max_frequency, _frequencies[frequency_index]);
    }

    float *samples = new float[_n_frequencies];
    for (int i = 0; i < _n_frequencies; i++) {
        samples[i] = (_max_frequency > 0) ? log10f((float)_frequencies[i]) / log10f((float)_max_frequency) : 0.0f;
    }
    
    PackedByteArray _byteArray;
    _byteArray.resize(_n_frequencies * sizeof(float));
    uint8_t *_byteArray_ptr = _byteArray.ptrw();
    memcpy(_byteArray_ptr, samples, _n_frequencies * sizeof(float));
    Ref<Image> histogram_image = Image::create_from_data(_n_frequencies, 1, false, Image::Format::FORMAT_RF, _byteArray);


    Ref<ImageTexture3D> data_texture = memnew(ImageTexture3D);
    Ref<ImageTexture3D> gradient_texture = memnew(ImageTexture3D);
    data_texture->create(Image::FORMAT_RF, dim_x, dim_y, dim_z, false, slices);
    gradient_texture->create(Image::FORMAT_RGBAF, dim_x, dim_y, dim_z, false, gradients);
    vds->set_data(data_array);
    vds->set_volume_texture(data_texture);
    vds->set_histogram_texture(ImageTexture::create_from_image(histogram_image));
    vds->set_gradient_texture(gradient_texture);
    vds->set_rotation(Quaternion::from_euler(Vector3(Math_PI / 2, 0, 0)));
    String filename = save_path + String(".") + _get_save_extension();
    return ResourceSaver::get_singleton()->save(vds, filename);
}

void VDSEditorPlugin::_bind_methods() {}

void VDSEditorPlugin::_enter_tree()
{
    import_plugin.instantiate();
    add_import_plugin(import_plugin);
    inspector_plugin.instantiate();
    add_inspector_plugin(inspector_plugin);
}

void VDSEditorPlugin::_exit_tree()
{
    remove_import_plugin(import_plugin);
    import_plugin.unref();
    remove_inspector_plugin(inspector_plugin);
    inspector_plugin.unref();
}