#pragma once

#include <godot_cpp/classes/editor_import_plugin.hpp>

#include <godot_cpp/classes/editor_plugin.hpp>

#include <godot_cpp/classes/image_texture.hpp>

#include "transfer_function_editor_plugin.h"

namespace godot {
    // TODO: see ResourceFormatLoader

    class VDSImportPlugin : public EditorImportPlugin {
        GDCLASS(VDSImportPlugin, EditorImportPlugin)

    protected:
        static void _bind_methods();

    public:
        virtual String _get_importer_name() const override;
        virtual String _get_visible_name() const override;
        virtual PackedStringArray _get_recognized_extensions() const override;
        virtual String _get_save_extension() const override;
        virtual String _get_resource_type() const override;
        virtual int _get_preset_count() const override;
        virtual String _get_preset_name(int preset_index) const override;
        virtual float _get_priority() const override;
        virtual int _get_import_order() const override;
        virtual TypedArray<Dictionary> _get_import_options(const String &path, int p_idx) const override;
        virtual bool _get_option_visibility(const String &p_path, const StringName &p_option_name, const Dictionary &p_options) const override;
        virtual Error _import(const String &source_file, const String &save_path, const Dictionary &options, const TypedArray<String> &platform_variants, const TypedArray<String> &gen_files) const override;
    };

    class VDSEditorPlugin : public EditorPlugin {
        GDCLASS(VDSEditorPlugin, EditorPlugin);

        Ref<VDSImportPlugin> import_plugin;
        Ref<EditorInspectorPluginTransferFunction> inspector_plugin;

    protected:
        static void _bind_methods();
    public:
        virtual void _enter_tree() override;
        virtual void _exit_tree() override;
    };
} // namespace godot
