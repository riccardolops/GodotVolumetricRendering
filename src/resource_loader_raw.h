#ifndef RESOURCE_LOADER_RAW_H
#define RESOURCE_LOADER_RAW_H

#include <godot_cpp/classes/ref.hpp>
#include <godot_cpp/classes/resource_format_loader.hpp>
#include "volume_dataset.h"

using namespace godot;

class ResourceFormatLoaderRAW : public ResourceFormatLoader
{
    GDCLASS(ResourceFormatLoaderRAW, ResourceFormatLoader)

protected:
    static void _bind_methods();

public:
    ResourceFormatLoaderRAW();
    ~ResourceFormatLoaderRAW();

    PackedStringArray _get_recognized_extensions() const override;
    bool _handles_type(const StringName &type) const override;
    String _get_resource_type(const String &path) const override;
    Variant _load(const String &path, const String &original_path, bool use_sub_threads, int32_t cache_mode) const override;
};

#endif
