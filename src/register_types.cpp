#include "register_types.h"
#include "volume_dataset.h"
#include "transfer_function.h"
#include "volume_rendered_object.h"
#include "editor/vds_editor_plugin.h"
#include "resource_loader_raw.h"
//#include "resource_saver_raw.h"

//#ifdef TOOLS_ENABLED
//#include "tools/.h"
//#endif //TOOLS_ENABLED

#include <gdextension_interface.h>
#include <godot_cpp/core/defs.hpp>
#include <godot_cpp/godot.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
#include <godot_cpp/classes/resource_saver.hpp>
#include <godot_cpp/classes/editor_plugin_registration.hpp>

using namespace godot;

static Ref<ResourceFormatLoaderRAW> raw_loader;
//static Ref<ResourceFormatSaverVDS> raw_saver;

void initialize_gdvds_module(ModuleInitializationLevel p_level) {
    //#ifdef TOOLS_ENABLED
    //#endif //TOOLS_ENABLED
    if (p_level == MODULE_INITIALIZATION_LEVEL_EDITOR) {
        GDREGISTER_INTERNAL_CLASS(HistogramTextureRect);
        GDREGISTER_INTERNAL_CLASS(PaletteTextureRect);
        GDREGISTER_INTERNAL_CLASS(TransferFunctionEditor);
        GDREGISTER_INTERNAL_CLASS(EditorInspectorPluginTransferFunction);
        GDREGISTER_INTERNAL_CLASS(VDSImportPlugin);
        //GDREGISTER_INTERNAL_CLASS(VDSExportPlugin);
        GDREGISTER_INTERNAL_CLASS(VDSEditorPlugin);
        EditorPlugins::add_by_type<VDSEditorPlugin>();
    }

    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE) {
        return;
    }
    
    GDREGISTER_CLASS(VolumeDataset);
    GDREGISTER_CLASS(TransferFunction);
    GDREGISTER_CLASS(VolumeRenderedObject);
    GDREGISTER_CLASS(ResourceFormatLoaderRAW);
    raw_loader.instantiate();
    ResourceLoader::get_singleton()->add_resource_format_loader(raw_loader);
    //raw_saver.instantiate();
    //ResourceSaver::add_resource_format_saver(raw_saver);
}

void uninitialize_gdvds_module(ModuleInitializationLevel p_level) {
    //#ifdef TOOLS_ENABLED
    //#endif //TOOLS_ENABLED

    if (p_level == MODULE_INITIALIZATION_LEVEL_EDITOR) {
        EditorPlugins::remove_by_type<VDSEditorPlugin>();
    }
    
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE) {
        return;
    }

    ResourceLoader::get_singleton()->remove_resource_format_loader(raw_loader);
    raw_loader.unref();
    //ResourceSaver::remove_resource_format_saver(raw_saver);
    //raw_saver.unref();
}

extern "C" {
// Initialization.
GDExtensionBool GDE_EXPORT gdvds_library_init(GDExtensionInterfaceGetProcAddress p_get_proc_address, const GDExtensionClassLibraryPtr p_library, GDExtensionInitialization *r_initialization) {
    godot::GDExtensionBinding::InitObject init_obj(p_get_proc_address, p_library, r_initialization);

    init_obj.register_initializer(initialize_gdvds_module);
    init_obj.register_terminator(uninitialize_gdvds_module);
    init_obj.set_minimum_library_initialization_level(MODULE_INITIALIZATION_LEVEL_SCENE);

    return init_obj.init();
}
}