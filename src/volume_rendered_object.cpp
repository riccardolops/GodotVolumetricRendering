#include "volume_rendered_object.h"

#define RL ResourceLoader::get_singleton()

void VolumeRenderedObject::_bind_methods() {
    ClassDB::bind_method(D_METHOD("set_dataset", "value"), &VolumeRenderedObject::set_dataset);
    ClassDB::bind_method(D_METHOD("get_dataset"), &VolumeRenderedObject::get_dataset);
    ADD_PROPERTY(PropertyInfo(Variant::OBJECT, "dataset", PROPERTY_HINT_RESOURCE_TYPE, "VolumeDataset"), "set_dataset", "get_dataset");

    ClassDB::bind_method(D_METHOD("set_transfer_function", "value"), &VolumeRenderedObject::set_transfer_function);
    ClassDB::bind_method(D_METHOD("get_transfer_function"), &VolumeRenderedObject::get_transfer_function);
    ADD_PROPERTY(PropertyInfo(Variant::OBJECT, "transfer_function", PROPERTY_HINT_RESOURCE_TYPE, "TransferFunction"), "set_transfer_function", "get_transfer_function");

    ClassDB::bind_method(D_METHOD("set_render_mode", "value"), &VolumeRenderedObject::set_render_mode);
    ClassDB::bind_method(D_METHOD("get_render_mode"), &VolumeRenderedObject::get_render_mode);
    ADD_PROPERTY(PropertyInfo(Variant::INT, "render_mode", PROPERTY_HINT_ENUM, "MaximumIntensityProjection,DirectVolume,Isosurface"), "set_render_mode", "get_render_mode");

    ClassDB::bind_method(D_METHOD("set_visibility_window", "value"), &VolumeRenderedObject::set_visibility_window);
    ClassDB::bind_method(D_METHOD("get_visibility_window"), &VolumeRenderedObject::get_visibility_window);
    ADD_PROPERTY(PropertyInfo(Variant::VECTOR2, "visibility_window"), "set_visibility_window", "get_visibility_window");

    ClassDB::bind_method(D_METHOD("set_gradient_lighting_threshold", "value"), &VolumeRenderedObject::set_gradient_lighting_threshold);
    ClassDB::bind_method(D_METHOD("get_gradient_lighting_threshold"), &VolumeRenderedObject::get_gradient_lighting_threshold);
    ADD_PROPERTY(PropertyInfo(Variant::VECTOR2, "gradient_lighting_threshold"), "set_gradient_lighting_threshold", "get_gradient_lighting_threshold");

    ClassDB::bind_method(D_METHOD("set_min_gradient", "value"), &VolumeRenderedObject::set_min_gradient);
    ClassDB::bind_method(D_METHOD("get_min_gradient"), &VolumeRenderedObject::get_min_gradient);
    ADD_PROPERTY(PropertyInfo(Variant::FLOAT, "min_gradient", PROPERTY_HINT_RANGE, "0.0,1.0"), "set_min_gradient", "get_min_gradient");

    ClassDB::bind_method(D_METHOD("set_ray_termination", "value"), &VolumeRenderedObject::set_ray_termination);
    ClassDB::bind_method(D_METHOD("get_ray_termination"), &VolumeRenderedObject::get_ray_termination);
    ADD_PROPERTY(PropertyInfo(Variant::BOOL, "ray_termination"), "set_ray_termination", "get_ray_termination");

    ClassDB::bind_method(D_METHOD("set_cubic_interpolation", "value"), &VolumeRenderedObject::set_cubic_interpolation);
    ClassDB::bind_method(D_METHOD("get_cubic_interpolation"), &VolumeRenderedObject::get_cubic_interpolation);
    ADD_PROPERTY(PropertyInfo(Variant::BOOL, "cubic_interpolation"), "set_cubic_interpolation", "get_cubic_interpolation");

    ClassDB::bind_method(D_METHOD("set_lighting_enabled", "value"), &VolumeRenderedObject::set_lighting_enabled);
    ClassDB::bind_method(D_METHOD("get_lighting_enabled"), &VolumeRenderedObject::get_lighting_enabled);
    ADD_PROPERTY(PropertyInfo(Variant::BOOL, "lighting_enabled"), "set_lighting_enabled", "get_lighting_enabled");

    ClassDB::bind_method(D_METHOD("set_light_source", "value"), &VolumeRenderedObject::set_light_source);
    ClassDB::bind_method(D_METHOD("get_light_source"), &VolumeRenderedObject::get_light_source);
    ADD_PROPERTY(PropertyInfo(Variant::INT, "light_source", PROPERTY_HINT_ENUM, "ActiveCamera,SceneMainLight"), "set_light_source", "get_light_source");
}

VolumeRenderedObject::VolumeRenderedObject() {
    noise_texture.instantiate();
    noise_texture->set_height(512);
    noise_texture->set_width(512);
    noise_texture->set_noise(memnew(FastNoiseLite));

    ERR_FAIL_COND(!RL->exists("res://addons/godotvolumetricrendering/shaders/raymarch.gdshader"));
    volume_material.instantiate();
    volume_material->set_shader(RL->load("res://addons/godotvolumetricrendering/shaders/raymarch.gdshader"));
    volume_material->set_shader_parameter("noiseSampler", noise_texture);

    Ref<BoxMesh> mesh = memnew(BoxMesh);
    mesh->set_flip_faces(true);
    set_mesh(mesh);
    mesh->surface_set_material(0, volume_material);

    ERR_FAIL_COND(!RL->exists("res://addons/godotvolumetricrendering/materials/default_tf.tres"));
    set_transfer_function(RL->load("res://addons/godotvolumetricrendering/materials/default_tf.tres")->duplicate(true));
}

void VolumeRenderedObject::set_dataset(const Ref<VolumeDataset> &value) {
    dataset = value;
    if (dataset.is_valid()) {
        Ref<ImageTexture3D> data = dataset->get_volume_texture();
        if (data.is_valid()) {
            volume_material->set_shader_parameter("volumeDataSampler", data);
            volume_material->set_shader_parameter("_TextureSize", Vector3i(data->get_width(), data->get_height(), data->get_depth()));
        }
        volume_material->set_shader_parameter("volumeGradientSampler", dataset->get_gradient_texture());
        set_scale(dataset->get_scale());
        set_quaternion(dataset->get_rotation());
        if (transfer_function.is_valid()) {
            transfer_function->set_histogram_texture(dataset->get_histogram_texture());
        }
    }
}

Ref<VolumeDataset> VolumeRenderedObject::get_dataset() const {
    return dataset;
}

void VolumeRenderedObject::set_transfer_function(const Ref<TransferFunction> &value) {
    transfer_function = value;
    if (transfer_function.is_valid() && dataset.is_valid()) {
        transfer_function->set_histogram_texture(dataset->get_histogram_texture());
    }
    transfer_function_changed();
}

Ref<TransferFunction> VolumeRenderedObject::get_transfer_function() const {
    return transfer_function;
}

void VolumeRenderedObject::transfer_function_changed() {
    if (transfer_function.is_valid()) {
        if (!transfer_function->is_connected("changed", callable_mp(this, &VolumeRenderedObject::transfer_function_changed))) {
            transfer_function->connect("changed", callable_mp(this, &VolumeRenderedObject::transfer_function_changed));
        }
        volume_material->set_shader_parameter("transferfunctionSamplerColor", transfer_function->get_gradient_color());
        volume_material->set_shader_parameter("transferfunctionSamplerAlpha", transfer_function->get_gradient_alpha());
        volume_material->set_shader_parameter("useTransferFunction2D", false);
    }
}

void VolumeRenderedObject::set_render_mode(RenderMode value) {
    render_mode = value;
    volume_material->set_shader_parameter("MODE", render_mode);
}

VolumeRenderedObject::RenderMode VolumeRenderedObject::get_render_mode() const {
    return render_mode;
}

void VolumeRenderedObject::set_ray_termination(bool value) {
    ray_termination = value;
    volume_material->set_shader_parameter("earlyRayTermination", ray_termination);
}

bool VolumeRenderedObject::get_ray_termination() const {
    return ray_termination;
}

void VolumeRenderedObject::set_cubic_interpolation(bool value) {
    cubic_interpolation = value;
    volume_material->set_shader_parameter("cubicInterpolation", cubic_interpolation);
}

bool VolumeRenderedObject::get_cubic_interpolation() const {
    return cubic_interpolation;
}

void VolumeRenderedObject::set_lighting_enabled(bool value) {
    lighting_enabled = value;
    volume_material->set_shader_parameter("useLighting", lighting_enabled);
}

bool VolumeRenderedObject::get_lighting_enabled() const {
    return lighting_enabled;
}

void VolumeRenderedObject::set_light_source(LightSource value) {
    light_source = value;
    volume_material->set_shader_parameter("useMainLight", light_source);
}

VolumeRenderedObject::LightSource VolumeRenderedObject::get_light_source() const {
    return light_source;
}

void VolumeRenderedObject::set_visibility_window(Vector2 value) {
    visibility_window = Vector2(CLAMP(value.x, 0.0, value.y), CLAMP(value.y, value.x, 1.0));
    volume_material->set_shader_parameter("_MinVal", visibility_window.x);
    volume_material->set_shader_parameter("_MaxVal", visibility_window.y);
}

Vector2 VolumeRenderedObject::get_visibility_window() const {
    return visibility_window;
}

void VolumeRenderedObject::set_gradient_lighting_threshold(Vector2 value) {
    gradient_lighting_threshold = Vector2(CLAMP(value.x, 0.0, value.y), CLAMP(value.y, value.x, 1.0));
    volume_material->set_shader_parameter("_LightingGradientThresholdStart", gradient_lighting_threshold.x);
    volume_material->set_shader_parameter("_LightingGradientThresholdEnd", gradient_lighting_threshold.y);
}

Vector2 VolumeRenderedObject::get_gradient_lighting_threshold() const {
    return gradient_lighting_threshold;
}

void VolumeRenderedObject::set_min_gradient(float value) {
    min_gradient = value;
    volume_material->set_shader_parameter("_MinGradient", min_gradient);
}

float VolumeRenderedObject::get_min_gradient() const {
    return min_gradient;
}
