#ifndef GDVDS_REGISTER_H
#define GDVDS_REGISTER_H

#include <godot_cpp/godot.hpp>
#include <godot_cpp/core/class_db.hpp>

using namespace godot;

void initialize_gdvds_module(ModuleInitializationLevel p_level);
void uninitialize_gdvds_module(ModuleInitializationLevel p_level);

#endif // GDVDS_REGISTER_H