#ifndef VOLUME_DATASET_H
#define VOLUME_DATASET_H

#include <godot_cpp/classes/resource.hpp>
#include <godot_cpp/classes/image_texture3d.hpp>
#include <godot_cpp/classes/texture2d.hpp>
#include <godot_cpp/variant/vector3.hpp>
#include <godot_cpp/variant/quaternion.hpp>

using namespace godot;

class VolumeDataset : public Resource {
    GDCLASS(VolumeDataset, Resource)

private:
    Vector3 scale = Vector3(1.0, 1.0, 1.0);
    Quaternion rotation = Quaternion(0.0, 0.0, 0.0, 1.0);
    TypedArray<float> data_array;
    Ref<ImageTexture3D> volume_texture;
    Ref<ImageTexture3D> gradient_texture;
    Ref<Texture2D> histogram_texture;

protected:
    static void _bind_methods();

public:
    void set_data(const TypedArray<float> &data) { data_array = data; }
    TypedArray<float> get_data() const { return data_array; }

    void set_volume_texture(const Ref<ImageTexture3D> &p_texture) { volume_texture = p_texture; }
    Ref<ImageTexture3D> get_volume_texture() const { return volume_texture; }

    void set_gradient_texture(const Ref<ImageTexture3D> &p_texture) { gradient_texture = p_texture; }
    Ref<ImageTexture3D> get_gradient_texture() const { return gradient_texture; }

    void set_scale(const Vector3 &p_scale) { scale = p_scale; }
    Vector3 get_scale() const { return scale; }

    void set_rotation(const Quaternion &p_rotation) { rotation = p_rotation; }
    Quaternion get_rotation() const { return rotation; }

    void set_histogram_texture(const Ref<Texture2D> &value) { histogram_texture = value; }
    Ref<Texture2D> get_histogram_texture() const { return histogram_texture; }
};

#endif // VOLUME_DATASET_H
