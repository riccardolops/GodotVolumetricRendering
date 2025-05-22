#ifndef TRANSFER_FUNCTION_H
#define TRANSFER_FUNCTION_H

#include <godot_cpp/classes/resource.hpp>
#include <godot_cpp/classes/gradient_texture1_d.hpp>
#include <godot_cpp/classes/gradient.hpp>

using namespace godot;

class TransferFunction : public Resource {
    GDCLASS(TransferFunction, Resource);

private:
    Ref<GradientTexture1D> gradient_color;
    Ref<GradientTexture1D> gradient_alpha;
    Ref<Texture2D> histogram_texture;

protected:
    static void _bind_methods();

public:
    TransferFunction();

    void set_gradient_color(const Ref<GradientTexture1D> &value);
    Ref<GradientTexture1D> get_gradient_color() const;

    void set_gradient_alpha(const Ref<GradientTexture1D> &value);
    Ref<GradientTexture1D> get_gradient_alpha() const;

    void set_histogram_texture(const Ref<Texture2D> &value);
    Ref<Texture2D> get_histogram_texture() const;
};

#endif // TRANSFER_FUNCTION_H
