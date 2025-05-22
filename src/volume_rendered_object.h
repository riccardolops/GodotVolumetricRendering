#ifndef VOLUME_RENDERED_OBJECT_H
#define VOLUME_RENDERED_OBJECT_H

#include <godot_cpp/classes/mesh_instance3d.hpp>
#include <godot_cpp/classes/shader_material.hpp>
#include <godot_cpp/classes/box_mesh.hpp>
#include <godot_cpp/classes/noise_texture2d.hpp>
#include <godot_cpp/classes/fast_noise_lite.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
#include "volume_dataset.h"
#include "transfer_function.h"

using namespace godot;

class VolumeRenderedObject : public MeshInstance3D {
    GDCLASS(VolumeRenderedObject, MeshInstance3D)

public:
    enum RenderMode {
        MaximumIntensityProjection,
        DirectVolume,
        Isosurface
    };

    enum LightSource {
        ActiveCamera,
        SceneMainLight
    };

private:
    Ref<ShaderMaterial> volume_material;
    Ref<NoiseTexture2D> noise_texture;
    Ref<VolumeDataset> dataset;
    Ref<TransferFunction> transfer_function;

    RenderMode render_mode = MaximumIntensityProjection;
    LightSource light_source = ActiveCamera;
    Vector2 visibility_window = Vector2(0.001, 1.0);
    Vector2 gradient_lighting_threshold = Vector2(0.02, 0.15);
    float min_gradient = 0.01;
    bool ray_termination = false;
    bool cubic_interpolation = false;
    bool lighting_enabled = false;
    void transfer_function_changed();

protected:
    static void _bind_methods();

public:
    VolumeRenderedObject();

    void set_dataset(const Ref<VolumeDataset> &value);
    Ref<VolumeDataset> get_dataset() const;

    void set_transfer_function(const Ref<TransferFunction> &value);
    Ref<TransferFunction> get_transfer_function() const;

    void set_render_mode(RenderMode value);
    RenderMode get_render_mode() const;

    void set_visibility_window(Vector2 value);
    Vector2 get_visibility_window() const;

    void set_gradient_lighting_threshold(Vector2 value);
    Vector2 get_gradient_lighting_threshold() const;

    void set_min_gradient(float value);
    float get_min_gradient() const;

    void set_ray_termination(bool value);
    bool get_ray_termination() const;

    void set_cubic_interpolation(bool value);
    bool get_cubic_interpolation() const;

    void set_lighting_enabled(bool value);
    bool get_lighting_enabled() const;

    void set_light_source(LightSource value);
    LightSource get_light_source() const;

};

VARIANT_ENUM_CAST(VolumeRenderedObject::RenderMode);
VARIANT_ENUM_CAST(VolumeRenderedObject::LightSource);

#endif // VOLUME_RENDERED_OBJECT_H
