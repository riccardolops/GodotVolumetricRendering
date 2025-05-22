#include "resource_loader_raw.h"

void ResourceFormatLoaderRAW::_bind_methods() {}

ResourceFormatLoaderRAW::ResourceFormatLoaderRAW() {}

ResourceFormatLoaderRAW::~ResourceFormatLoaderRAW() {}

PackedStringArray ResourceFormatLoaderRAW::_get_recognized_extensions() const
{
    PackedStringArray extensions = PackedStringArray();
    extensions.append("raw");
    return extensions;
}

bool ResourceFormatLoaderRAW::_handles_type(const StringName &type) const
{
    return type == String("VolumeDataset");
}

String ResourceFormatLoaderRAW::_get_resource_type(const String &path) const
{
    if (path.get_extension().to_lower() == "raw")
    {
        return "VolumeDataset";
    }
    return "";
}

Variant ResourceFormatLoaderRAW::_load(const String &path, const String &original_path, bool use_sub_threads, int32_t cache_mode) const
{
    Ref<VolumeDataset> dataset = Ref<VolumeDataset>(memnew(VolumeDataset));
    dataset->set_volume_texture(Ref<ImageTexture3D>(memnew(ImageTexture3D)));
    return dataset;
}