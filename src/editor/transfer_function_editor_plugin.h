#ifndef TRANSFER_FUNCTION_EDITOR_PLUGIN_H
#define TRANSFER_FUNCTION_EDITOR_PLUGIN_H

#include "transfer_function.h"
#include "../util/editor_scale.h"
#include <godot_cpp/classes/editor_inspector.hpp>
#include <godot_cpp/classes/editor_inspector_plugin.hpp>
#include <godot_cpp/classes/v_box_container.hpp>
#include <godot_cpp/classes/button.hpp>
#include <godot_cpp/classes/editor_spin_slider.hpp>
#include <godot_cpp/classes/h_flow_container.hpp>
#include <godot_cpp/classes/v_separator.hpp>
#include <godot_cpp/classes/scene_state.hpp>
#include <godot_cpp/classes/popup_panel.hpp>
#include <godot_cpp/classes/color_picker.hpp>
#include <godot_cpp/classes/editor_settings.hpp>
#include <godot_cpp/classes/editor_plugin.hpp>
#include <godot_cpp/classes/gradient.hpp>
#include <godot_cpp/classes/editor_undo_redo_manager.hpp>
#include <godot_cpp/classes/input_event_key.hpp>
#include <godot_cpp/classes/input_event_mouse_button.hpp>
#include <godot_cpp/classes/input_event_mouse_motion.hpp>
#include <godot_cpp/classes/input.hpp>
#include <godot_cpp/classes/panel.hpp>
#include <godot_cpp/classes/texture_rect.hpp>
#include <godot_cpp/classes/label.hpp>
#include <godot_cpp/classes/image.hpp>
#include <godot_cpp/classes/shader_material.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
#include <godot_cpp/classes/texture_button.hpp>

using namespace godot;

class HistogramTextureRect : public TextureRect {
    GDCLASS(HistogramTextureRect, TextureRect);
public:
    HistogramTextureRect() {};
    void set_transfer_function(Ref<TransferFunction> p_tf) { _transfer_function = p_tf; };
    void _draw() override;
    void _gui_input(const Ref<InputEvent> &p_event) override;
private:
    Ref<TransferFunction> _transfer_function;

    int selected_index = -1;
    void set_selected_index(int p_index);
    int hovered_index = -1;

    enum GrabMode {
        GRAB_NONE,
        GRAB_ADD,
        GRAB_MOVE
    };

    GrabMode grabbing = GRAB_NONE;
    float pre_grab_offset = 0.5;
    float pre_grab_alpha = 0.5;
    int pre_grab_index = -1;

    const int BASE_HANDLE_DIM = 8;

    int handle_width = BASE_HANDLE_DIM;
    int handle_height = BASE_HANDLE_DIM;

    void remove_point(int p_index);

    int _get_point_at(int p_xpos, int p_ypos) const;
    int _predict_insertion_index(float p_offset);

    static void _bind_methods() {};
};

class PaletteTextureRect : public TextureRect {
    GDCLASS(PaletteTextureRect, TextureRect);
public:
    PaletteTextureRect();
    void set_transfer_function(Ref<TransferFunction> p_tf) { _transfer_function = p_tf; };
    void _draw() override;
    void _gui_input(const Ref<InputEvent> &p_event) override;
private:
    Ref<TransferFunction> _transfer_function;
    bool snap_enabled = false;
    int snap_count = 10;
    int selected_index = -1;
    void set_selected_index(int p_index);
    int hovered_index = -1;

    PopupPanel *popup = nullptr;
    ColorPicker *picker = nullptr;

    enum GrabMode {
        GRAB_NONE,
        GRAB_ADD,
        GRAB_MOVE
    };

    GrabMode grabbing = GRAB_NONE;
    float pre_grab_offset = 0.5;
    int pre_grab_index = -1;

    const int BASE_HANDLE_WIDTH = 8;

    int handle_width = BASE_HANDLE_WIDTH;

    void remove_point(int p_index);

    int _get_point_at(int p_xpos) const;
    int _predict_insertion_index(float p_offset);
    void _show_color_picker();

    void _color_changed(const Color &p_color);

    static void _bind_methods() {};
};

class TransferFunctionEditor : public VBoxContainer {
    GDCLASS(TransferFunctionEditor, VBoxContainer);
public:
    TransferFunctionEditor();
    void set_transfer_function(Ref<TransferFunction> p_tf);
    
private:
    Ref<TransferFunction> _transfer_function;
    HistogramTextureRect *_histogram_texture_rect;
    PaletteTextureRect *_palette_texture_rect;

    Panel *alpha_panel;
    Panel *colour_panel;

    static void _bind_methods() {};
    void transfer_function_changed();
    
};

class EditorInspectorPluginTransferFunction : public EditorInspectorPlugin {
    GDCLASS(EditorInspectorPluginTransferFunction, EditorInspectorPlugin);

public:
    virtual bool _can_handle(Object *p_object) const override;
    virtual void _parse_begin(Object *p_object) override;

protected:
    static void _bind_methods();
};

#endif // TRANSFER_FUNCTION_EDITOR_PLUGIN_H