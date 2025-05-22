#include "editor_scale.h"

float get_editor_scale() {
	using namespace godot;

	static float s_scale = 1.f;
	static bool s_initialized = false;

	if (!s_initialized) {
		EditorPlugin *dummy_plugin = memnew(EditorPlugin);
		EditorInterface *interface = dummy_plugin->get_editor_interface();
		s_scale = interface->get_editor_scale();
		s_initialized = true;
		memdelete(dummy_plugin);
	}

	return s_scale;
}
