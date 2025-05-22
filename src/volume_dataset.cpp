#include "volume_dataset.h"

void VolumeDataset::_bind_methods()
{
    ClassDB::bind_method(D_METHOD("set_data", "data"), &VolumeDataset::set_data);
    ClassDB::bind_method(D_METHOD("get_data"), &VolumeDataset::get_data);
    ADD_PROPERTY(PropertyInfo(Variant::ARRAY, "data", PROPERTY_HINT_ARRAY_TYPE, "float"), "set_data", "get_data");

    ClassDB::bind_method(D_METHOD("set_volume_texture", "volume"), &VolumeDataset::set_volume_texture);
    ClassDB::bind_method(D_METHOD("get_volume_texture"), &VolumeDataset::get_volume_texture);
    ADD_PROPERTY(PropertyInfo(Variant::OBJECT, "volume", PROPERTY_HINT_RESOURCE_TYPE, "ImageTexture3D"), "set_volume_texture", "get_volume_texture");

    ClassDB::bind_method(D_METHOD("set_gradient_texture", "gradient"), &VolumeDataset::set_gradient_texture);
    ClassDB::bind_method(D_METHOD("get_gradient_texture"), &VolumeDataset::get_gradient_texture);
    ADD_PROPERTY(PropertyInfo(Variant::OBJECT, "gradient", PROPERTY_HINT_RESOURCE_TYPE, "ImageTexture3D"), "set_gradient_texture", "get_gradient_texture");

    ClassDB::bind_method(D_METHOD("set_scale", "scale"), &VolumeDataset::set_scale);
    ClassDB::bind_method(D_METHOD("get_scale"), &VolumeDataset::get_scale);
    ADD_PROPERTY(PropertyInfo(Variant::VECTOR3, "scale"), "set_scale", "get_scale");

    ClassDB::bind_method(D_METHOD("set_rotation", "rotation"), &VolumeDataset::set_rotation);
    ClassDB::bind_method(D_METHOD("get_rotation"), &VolumeDataset::get_rotation);
    ADD_PROPERTY(PropertyInfo(Variant::QUATERNION, "rotation"), "set_rotation", "get_rotation");

    ClassDB::bind_method(D_METHOD("set_histogram_texture", "histogram"), &VolumeDataset::set_histogram_texture);
    ClassDB::bind_method(D_METHOD("get_histogram_texture"), &VolumeDataset::get_histogram_texture);
    ADD_PROPERTY(PropertyInfo(Variant::OBJECT, "histogram", PROPERTY_HINT_RESOURCE_TYPE, "Texture2D"), "set_histogram_texture", "get_histogram_texture");
}
