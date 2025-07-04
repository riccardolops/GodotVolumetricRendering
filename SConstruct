#!/usr/bin/env python
import os
import sys

env = SConscript("godot-cpp/SConstruct")
lib_name = "libgvr"

# For reference:
# - CCFLAGS are compilation flags shared between C and C++
# - CFLAGS are for C-specific compilation flags
# - CXXFLAGS are for C++-specific compilation flags
# - CPPFLAGS are for pre-processor flags
# - CPPDEFINES are for pre-processor defines
# - LINKFLAGS are for linking flags

# tweak this if you want to use different folders, or more folders, to store your source code in.
env.Append(CPPPATH=["src/"])
sources = Glob("src/*.cpp")
sources += Glob("src/editor/*.cpp")

output_bin_folder = "./demo/addons/godotvolumetricrendering"

def add_sources_recursively(dir: str, glob_sources, exclude_folder: list = []):
    for f in os.listdir(dir):
        if f in exclude_folder:
            continue
        sub_dir = os.path.join(dir, f)
        if os.path.isdir(sub_dir):
            glob_sources += Glob(os.path.join(sub_dir, "*.cpp"))
            add_sources_recursively(sub_dir, glob_sources, exclude_folder)

if env.debug_features:
    env.Append(CPPDEFINES=["TOOLS_ENABLED"])
    sources += Glob("src/tools/*.cpp")

add_sources_recursively("src", sources, ["editor"])

if env["platform"] == "macos":
    library = env.SharedLibrary(
        f'{output_bin_folder}/{lib_name}.{env["platform"]}.{env["target"]}.framework/{lib_name}.{env["platform"]}.{env["target"]}',
        source=sources,
    )
elif env["platform"] == "ios":
    if env["ios_simulator"]:
        library = env.StaticLibrary(
            f'{output_bin_folder}/{lib_name}.{env["platform"]}.{env["target"]}.simulator.a',
            source=sources,
        )
    else:
        library = env.StaticLibrary(
            f'{output_bin_folder}/{lib_name}.{env["platform"]}.{env["target"]}.a',
            source=sources,
        )
else:
    library = env.SharedLibrary(
        f'{output_bin_folder}/{lib_name}{env["suffix"]}{env["SHLIBSUFFIX"]}',
        source=sources,
    )

if env["target"] in ["editor", "template_debug"]:
    try:
        doc_data = env.GodotCPPDocData("src/gen/doc_data.gen.cpp", source=Glob("doc_classes/*.xml"))
        sources.append(doc_data)
    except AttributeError:
        print("Not including class reference as we're targeting a pre-4.3 baseline.")

Default(library)
