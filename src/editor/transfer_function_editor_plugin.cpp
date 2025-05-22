#include "transfer_function_editor_plugin.h"

#define RL ResourceLoader::get_singleton()

///////////////////////

TransferFunctionEditor::TransferFunctionEditor() {
    alpha_panel = memnew(Panel);
    colour_panel = memnew(Panel);
    alpha_panel->set_focus_mode(FOCUS_ALL);
    alpha_panel->set_custom_minimum_size(Size2(0, 200) * EDSCALE);
    add_child(alpha_panel);
    _histogram_texture_rect = memnew(HistogramTextureRect);
    ERR_FAIL_COND(!RL->exists("res://addons/godotvolumetricrendering/shaders/TransferFunctionEditor.gdshader"));
    Ref<ShaderMaterial> _histogram_material;
    _histogram_material.instantiate();
    _histogram_material->set_shader(RL->load("res://addons/godotvolumetricrendering/shaders/TransferFunctionEditor.gdshader"));
    _histogram_texture_rect->set_material(_histogram_material);
    _histogram_texture_rect->set_anchors_preset(PRESET_FULL_RECT);
    alpha_panel->add_child(_histogram_texture_rect);

    colour_panel->set_custom_minimum_size(Size2(0, 60) * EDSCALE);
    add_child(colour_panel);
    _palette_texture_rect = memnew(PaletteTextureRect);
    _palette_texture_rect->set_anchors_preset(PRESET_FULL_RECT);
    colour_panel->add_child(_palette_texture_rect);
}


void TransferFunctionEditor::set_transfer_function(Ref<TransferFunction> p_tf) {
    _transfer_function = p_tf;
    _histogram_texture_rect->set_transfer_function(p_tf);
    _palette_texture_rect->set_transfer_function(p_tf);
    transfer_function_changed();
    if (!_transfer_function->is_connected("changed", callable_mp(this, &TransferFunctionEditor::transfer_function_changed))) {
        _transfer_function->connect("changed", callable_mp(this, &TransferFunctionEditor::transfer_function_changed));
    }
}


void TransferFunctionEditor::transfer_function_changed() {
    ((Ref<ShaderMaterial>)_histogram_texture_rect->get_material())->set_shader_parameter("tf_tex_colour", _transfer_function->get_gradient_color());
    ((Ref<ShaderMaterial>)_histogram_texture_rect->get_material())->set_shader_parameter("tf_tex_alpha", _transfer_function->get_gradient_alpha());
    _histogram_texture_rect->set_texture(_transfer_function->get_histogram_texture());
    _palette_texture_rect->set_texture(_transfer_function->get_gradient_color());
    _histogram_texture_rect->queue_redraw();
}

void HistogramTextureRect::_gui_input(const Ref<InputEvent> &p_event) {
    if (_transfer_function.is_null()) {
        return;
    } else if (_transfer_function->get_gradient_alpha().is_null()) {
        return;
    } else if (_transfer_function->get_gradient_alpha()->get_gradient().is_null()) {
        return;
    }
    Ref<Gradient> gradient = _transfer_function->get_gradient_alpha()->get_gradient();
    ERR_FAIL_COND(p_event.is_null());

    Ref<InputEventKey> k = p_event;

    if (k.is_valid() && k->is_pressed() && k->get_keycode() == Key::KEY_DELETE && selected_index != -1) {
        if (grabbing == GRAB_ADD) {
            gradient->remove_point(selected_index); // Point is temporary, so remove directly from gradient.
            set_selected_index(-1);
        } else {
            remove_point(selected_index);
        }
        grabbing = GRAB_NONE;
        hovered_index = -1;
        accept_event();
    }

    Ref<InputEventMouseButton> mb = p_event;

    if (mb.is_valid() && mb->is_pressed()) {
        float adjusted_mb_x = mb->get_position().x - handle_width / 2;
        float adjusted_mb_y = mb->get_position().y - handle_height / 2;

        // Delete point or move it to old position on middle or right click.
        if (mb->get_button_index() == MouseButton::MOUSE_BUTTON_RIGHT || mb->get_button_index() == MouseButton::MOUSE_BUTTON_MIDDLE) {
            if (grabbing == GRAB_MOVE && mb->get_button_index() == MouseButton::MOUSE_BUTTON_RIGHT) {
                gradient->set_offset(selected_index, pre_grab_offset);
                gradient->set_color(selected_index, Color(0, 0, 0, pre_grab_alpha));

                set_selected_index(pre_grab_index);
            } else {
                int point_to_remove = _get_point_at(adjusted_mb_x, adjusted_mb_y);
                if (point_to_remove == -1) {
                    set_selected_index(-1); // Nothing on the place of the click, just deselect any handle.
                } else {
                    if (grabbing == GRAB_ADD) {
                        gradient->remove_point(point_to_remove); // Point is temporary, so remove directly from gradient.
                        set_selected_index(-1);
                    } else {
                        remove_point(point_to_remove);
                    }
                    hovered_index = -1;
                }
            }
            grabbing = GRAB_NONE;
            accept_event();
        }

        // Select point.
        if (mb->get_button_index() == MouseButton::MOUSE_BUTTON_LEFT) {
            int total_w = get_size().x;
            int total_h = get_size().y;

            if (grabbing == GRAB_NONE) {
                set_selected_index(_get_point_at(adjusted_mb_x, adjusted_mb_y));
            }

            if (selected_index != -1 && !mb->is_alt_pressed()) {
                // An existing point was grabbed.
                grabbing = GRAB_MOVE;
                pre_grab_offset = gradient->get_offset(selected_index);
                pre_grab_alpha = gradient->get_color(selected_index).a;
                pre_grab_index = selected_index;
            } else if (grabbing == GRAB_NONE) {
                // Adding a new point. Insert a temporary point for the user to adjust, so it's not in the undo/redo.
                float new_offset = CLAMP(adjusted_mb_x / float(total_w), 0, 1);
                float alpha = 1 - CLAMP(adjusted_mb_y / float(total_h), 0, 1);

                for (int i = 0; i < gradient->get_point_count(); i++) {
                    if (gradient->get_offset(i) == new_offset) {
                        // If another point with the same offset is found, then
                        // tweak it if Alt was pressed, otherwise something has gone wrong, so stop the operation.
                        if (mb->is_alt_pressed()) {
                            new_offset = MIN(gradient->get_offset(i) + 0.00001, 1);
                        } else {
                            return;
                        }
                    }
                }
                
                Color new_color = Color(0, 0, 0, alpha);
                // Add a temporary point for the user to adjust before adding it permanently.
                gradient->add_point(new_offset, new_color);
                set_selected_index(_predict_insertion_index(new_offset));
                grabbing = GRAB_ADD;
            }
        }
    }

    if (mb.is_valid() && mb->get_button_index() == MouseButton::MOUSE_BUTTON_LEFT && !mb->is_pressed()) {
        if (grabbing == GRAB_MOVE) {
            // Finish moving a point.
            //set_offset(selected_index, gradient->get_offset(selected_index)); ???
            grabbing = GRAB_NONE;
        } else if (grabbing == GRAB_ADD) {
            // Finish inserting a new point. Remove the temporary point and insert the permanent one in its place.
            //float new_offset = gradient->get_offset(selected_index);
            //Color new_color = gradient->get_color(selected_index);
            //gradient->remove_point(selected_index);
            //add_point(new_offset, new_color);
            grabbing = GRAB_NONE;
        }
    }

    Ref<InputEventMouseMotion> mm = p_event;

    if (mm.is_valid()) {
        int total_w = get_size().x;
        float adjusted_mm_x = mm->get_position().x;
        float adjusted_mm_y = mm->get_position().y;

        // Hovering logic.
        if (grabbing == GRAB_NONE) {
            int nearest_point = _get_point_at(adjusted_mm_x, adjusted_mm_y);
            if (hovered_index != nearest_point) {
                hovered_index = nearest_point;
                queue_redraw();
            }
            return;
        } else {
            hovered_index = -1;
        }

        // Grabbing logic.
        float new_offset = CLAMP(adjusted_mm_x / float(total_w), 0, 1);
        float new_alpha = 1 - CLAMP(adjusted_mm_y / float(get_size().y), 0, 1);

        // Give the ability to snap right next to a point when using Shift.
        if (mm->is_shift_pressed()) {
            float smallest_offset = 0.01;
            int nearest_idx = -1;
            // Only check the two adjacent points to find which one is the nearest.
            if (selected_index > 0) {
                float temp_offset = ABS(gradient->get_offset(selected_index - 1) - new_offset);
                if (temp_offset < smallest_offset) {
                    smallest_offset = temp_offset;
                    nearest_idx = selected_index - 1;
                }
            }
            if (selected_index < gradient->get_point_count() - 1) {
                float temp_offset = ABS(gradient->get_offset(selected_index + 1) - new_offset);
                if (temp_offset < smallest_offset) {
                    smallest_offset = temp_offset;
                    nearest_idx = selected_index + 1;
                }
            }
            if (nearest_idx != -1) {
                // Snap to the point with a slight adjustment to the left or right.
                float adjustment = gradient->get_offset(nearest_idx) < new_offset ? 0.00001 : -0.00001;
                new_offset = CLAMP(gradient->get_offset(nearest_idx) + adjustment, 0, 1);
            }
        }

        // Don't move the point if its new offset and alpha would be the same as another point's.
        for (int i = 0; i < gradient->get_point_count(); i++) {
            if (gradient->get_offset(i) == new_offset && gradient->get_color(i).a == new_alpha && i != selected_index) {
                return;
            }
        }

        if (selected_index == -1) {
            return;
        }

        // We want to only save this action for undo/redo when released, so don't use set_offset() yet.
        gradient->set_offset(selected_index, new_offset);
        gradient->set_color(selected_index, Color(0, 0, 0, new_alpha));

        // Update selected_index after the gradient updates its indices, so you keep holding the same color.
        for (int i = 0; i < gradient->get_point_count(); i++) {
            if (gradient->get_offset(i) == new_offset) {
                set_selected_index(i);
                break;
            }
        }
    }
}

void HistogramTextureRect::_draw() {
    int w = get_size().x;
    int h = get_size().y;

    int handle_width = BASE_HANDLE_DIM * EDSCALE;
    int handle_height = BASE_HANDLE_DIM * EDSCALE;

    int half_handle_width = handle_width * 0.5;
    int half_handle_height = handle_height * 0.5;

    if (w == 0 || h == 0) {
        return;
    }

    if (_transfer_function.is_valid()) {
        Ref<GradientTexture1D> texture_gradient_alpha = _transfer_function->get_gradient_alpha();
        Ref<GradientTexture1D> texture_gradient_color = _transfer_function->get_gradient_color();
        if (texture_gradient_alpha.is_valid() && texture_gradient_color.is_valid()) {
            Ref<Gradient> gradient_alpha = texture_gradient_alpha->get_gradient();
            Ref<Gradient> gradient_color = texture_gradient_color->get_gradient();
            if (gradient_alpha.is_valid() && gradient_color.is_valid()) {
                for (int i = 0; i < gradient_alpha->get_point_count(); i++) {
                    if (gradient_alpha->get_offset(i) < 0.0) {
                        continue;
                    } else if (gradient_alpha->get_offset(i) > 1.0) {
                        break;
                    }
                    // White or black handle color, to contrast with the selected color's brightness.
                    Color inside_col = gradient_color->sample(gradient_alpha->get_offset(i));
                    Color border_col = Math::lerp(0.75f, inside_col.get_luminance(), inside_col.a) > 0.455 ? Color(0, 0, 0) : Color(1, 1, 1);
            
                    int handle_thickness = MAX(1, Math::round(EDSCALE));
                    float handle_x_pos = gradient_alpha->get_offset(i) * w;
                    float handle_y_pos = (1 - gradient_alpha->get_color(i).a) * h;
                    float handle_start_x = handle_x_pos - half_handle_width;
                    float handle_start_y = handle_y_pos - half_handle_height;
                    Rect2 rect = Rect2(handle_start_x, handle_start_y, handle_width, handle_height);
            
                    draw_rect(rect, inside_col, true);
            
                    if (selected_index == i) {
                        // Handle is selected.
                        draw_rect(rect, border_col, false, handle_thickness);
                        //draw_line(Vector2(handle_x_pos, 0), Vector2(handle_x_pos, h / 2 - handle_thickness), border_col, handle_thickness);
                        if (inside_col.a < 1) {
                            //draw_line(Vector2(handle_start_x + handle_thickness / 2.0, h * 0.9 - handle_thickness), Vector2(handle_start_x + handle_width - handle_thickness / 2.0, h * 0.9 - handle_thickness), border_col, handle_thickness);
                        }
                        rect = rect.grow(-handle_thickness);
                        const Color focus_col = get_theme_color("accent_color", "Editor");
                        draw_rect(rect, has_focus() ? focus_col : focus_col.darkened(0.4), false, handle_thickness);
                        rect = rect.grow(-handle_thickness);
                        draw_rect(rect, border_col, false, handle_thickness);
                    } else {
                        // Handle isn't selected.
                        border_col.a = 0.9;
                        draw_rect(rect, border_col, false, handle_thickness);
                        //draw_line(Vector2(handle_x_pos, 0), Vector2(handle_x_pos, h / 2 - handle_thickness), border_col, handle_thickness);
                        if (inside_col.a < 1) {
                            //draw_line(Vector2(handle_start_x + handle_thickness / 2.0, h * 0.9 - handle_thickness), Vector2(handle_start_x + handle_width - handle_thickness / 2.0, h * 0.9 - handle_thickness), border_col, handle_thickness);
                        }
                        if (hovered_index == i) {
                            // Draw a subtle translucent rect inside the handle if it's being hovered.
                            rect = rect.grow(-handle_thickness);
                            border_col.a = 0.54;
                            draw_rect(rect, border_col, false, handle_thickness);
                        }
                    }
                }
            } else {
                draw_line(Vector2(0, 0), Vector2(w, h), Color(0.8, 0.8, 0.8));
                draw_line(Vector2(0, h), Vector2(w, 0), Color(0.8, 0.8, 0.8));
            }
        } else {
            draw_line(Vector2(0, 0), Vector2(w, h), Color(0.8, 0.8, 0.8));
            draw_line(Vector2(0, h), Vector2(w, 0), Color(0.8, 0.8, 0.8));
        }
    } else {
        draw_line(Vector2(0, 0), Vector2(w, h), Color(0.8, 0.8, 0.8));
        draw_line(Vector2(0, h), Vector2(w, 0), Color(0.8, 0.8, 0.8));
    }
}

void PaletteTextureRect::_color_changed(const Color &p_color) {
    Ref<Gradient> gradient = _transfer_function->get_gradient_color()->get_gradient();
    ERR_FAIL_INDEX_MSG(selected_index, gradient->get_point_count(), "Gradient point is out of bounds.");
    gradient->set_color(selected_index, Color(p_color.r, p_color.g, p_color.b));
}

PaletteTextureRect::PaletteTextureRect() {
    picker = memnew(ColorPicker);
    picker->connect("color_changed", callable_mp(this, &PaletteTextureRect::_color_changed));
    popup = memnew(PopupPanel);
    add_child(popup, false, INTERNAL_MODE_FRONT);
    popup->add_child(picker);
};

void PaletteTextureRect::_draw() {
    int w = get_size().x;
    int h = get_size().y;

    int handle_width = BASE_HANDLE_WIDTH * EDSCALE;
    int half_handle_width = handle_width * 0.5;

    if (w == 0 || h == 0) {
        return;
    }

    if (_transfer_function.is_valid()) {
        Ref<GradientTexture1D> texture_gradient_color = _transfer_function->get_gradient_color();
        if (texture_gradient_color.is_valid()) {
            Ref<Gradient> gradient_color = texture_gradient_color->get_gradient();
            if (gradient_color.is_valid()) {
                for (int i = 0; i < gradient_color->get_point_count(); i++) {
                    if (gradient_color->get_offset(i) < 0.0) {
                        continue;
                    } else if (gradient_color->get_offset(i) > 1.0) {
                        break;
                    }
                    // White or black handle color, to contrast with the selected color's brightness.
                    // Also consider the fact that the color may be translucent.
                    // The checkerboard pattern in the background has an average luminance of 0.75.
                    Color inside_col = gradient_color->get_color(i);
                    Color border_col = Math::lerp(0.75f, inside_col.get_luminance(), inside_col.a) > 0.455 ? Color(0, 0, 0) : Color(1, 1, 1);
            
                    int handle_thickness = MAX(1, Math::round(EDSCALE));
                    float handle_x_pos = gradient_color->get_offset(i) * w;
                    float handle_start_x = handle_x_pos - half_handle_width;
                    Rect2 rect = Rect2(handle_start_x, h / 2, handle_width, h / 2);
            
                    if (inside_col.a < 1) {
                        // If the color is translucent, draw a little opaque rectangle at the bottom to more easily see it.
                        draw_rect(rect, inside_col, true);
                        Color inside_col_opaque = inside_col;
                        inside_col_opaque.a = 1.0;
                        draw_rect(Rect2(handle_start_x + handle_thickness / 2.0, h * 0.9 - handle_thickness / 2.0, handle_width - handle_thickness, h * 0.1), inside_col_opaque, true);
                    } else {
                        draw_rect(rect, inside_col, true);
                    }
            
                    if (selected_index == i) {
                        // Handle is selected.
                        draw_rect(rect, border_col, false, handle_thickness);
                        draw_line(Vector2(handle_x_pos, 0), Vector2(handle_x_pos, h / 2 - handle_thickness), border_col, handle_thickness);
                        if (inside_col.a < 1) {
                            draw_line(Vector2(handle_start_x + handle_thickness / 2.0, h * 0.9 - handle_thickness), Vector2(handle_start_x + handle_width - handle_thickness / 2.0, h * 0.9 - handle_thickness), border_col, handle_thickness);
                        }
                        rect = rect.grow(-handle_thickness);
                        const Color focus_col = get_theme_color("accent_color", "Editor");
                        draw_rect(rect, has_focus() ? focus_col : focus_col.darkened(0.4), false, handle_thickness);
                        rect = rect.grow(-handle_thickness);
                        draw_rect(rect, border_col, false, handle_thickness);
                    } else {
                        // Handle isn't selected.
                        border_col.a = 0.9;
                        draw_rect(rect, border_col, false, handle_thickness);
                        draw_line(Vector2(handle_x_pos, 0), Vector2(handle_x_pos, h / 2 - handle_thickness), border_col, handle_thickness);
                        if (inside_col.a < 1) {
                            draw_line(Vector2(handle_start_x + handle_thickness / 2.0, h * 0.9 - handle_thickness), Vector2(handle_start_x + handle_width - handle_thickness / 2.0, h * 0.9 - handle_thickness), border_col, handle_thickness);
                        }
                        if (hovered_index == i) {
                            // Draw a subtle translucent rect inside the handle if it's being hovered.
                            rect = rect.grow(-handle_thickness);
                            border_col.a = 0.54;
                            draw_rect(rect, border_col, false, handle_thickness);
                        }
                    }
                }
            }
        }
    }
}

void PaletteTextureRect::set_selected_index(int p_index) {
    selected_index = p_index;
    queue_redraw();
}

void HistogramTextureRect::set_selected_index(int p_index) {
    selected_index = p_index;
    queue_redraw();
}

void HistogramTextureRect::remove_point(int p_index) {
    Ref<Gradient> gradient = _transfer_function->get_gradient_alpha()->get_gradient();
    ERR_FAIL_INDEX_MSG(p_index, gradient->get_point_count(), "Gradient point is out of bounds.");

    if (gradient->get_point_count() <= 1) {
        return;
    }

    gradient->remove_point(p_index);
    set_selected_index(-1);
}

void PaletteTextureRect::remove_point(int p_index) {
    Ref<Gradient> gradient = _transfer_function->get_gradient_color()->get_gradient();
    ERR_FAIL_INDEX_MSG(p_index, gradient->get_point_count(), "Gradient point is out of bounds.");

    if (gradient->get_point_count() <= 1) {
        return;
    }

    gradient->remove_point(p_index);
    set_selected_index(-1);
}

int HistogramTextureRect::_get_point_at(int p_xpos, int p_ypos) const {
    Ref<Gradient> gradient = _transfer_function->get_gradient_alpha()->get_gradient();
    int result = -1;
    int total_w = get_size().x;
    int total_h = get_size().y;
    float min_distance = handle_width * 1.8; // Allow the cursor to be more than half a handle width away for ease of use.
    for (int i = 0; i < gradient->get_point_count(); i++) {
        // Ignore points outside of [0, 1].
        if (gradient->get_offset(i) < 0) {
            continue;
        } else if (gradient->get_offset(i) > 1) {
            break;
        }
        // Check if we clicked at point.
        float point_x = gradient->get_offset(i) * total_w;
        float point_y = (1 - gradient->get_color(i).a) * total_h;
        float distance = sqrt(pow(p_xpos - point_x, 2) + pow(p_ypos - point_y, 2));
        if (distance < min_distance) {
            result = i;
            min_distance = distance;
        }
    }
    return result;
}

int PaletteTextureRect::_get_point_at(int p_xpos) const {
    Ref<Gradient> gradient = _transfer_function->get_gradient_color()->get_gradient();
    int result = -1;
    int total_w = get_size().x;
    float min_distance = handle_width * 1.8; // Allow the cursor to be more than half a handle width away for ease of use.
    for (int i = 0; i < gradient->get_point_count(); i++) {
        // Ignore points outside of [0, 1].
        if (gradient->get_offset(i) < 0) {
            continue;
        } else if (gradient->get_offset(i) > 1) {
            break;
        }
        // Check if we clicked at point.
        float distance = ABS(p_xpos - gradient->get_offset(i) * total_w);
        if (distance < min_distance) {
            result = i;
            min_distance = distance;
        }
    }
    return result;
}

int HistogramTextureRect::_predict_insertion_index(float p_offset) {
    Ref<Gradient> gradient = _transfer_function->get_gradient_alpha()->get_gradient();
    int result = 0;
    while (result < gradient->get_point_count() && gradient->get_offset(result) < p_offset) {
        result++;
    }
    return result;
}

int PaletteTextureRect::_predict_insertion_index(float p_offset) {
    Ref<Gradient> gradient = _transfer_function->get_gradient_color()->get_gradient();
    int result = 0;
    while (result < gradient->get_point_count() && gradient->get_offset(result) < p_offset) {
        result++;
    }
    return result;
}

void PaletteTextureRect::_show_color_picker() {
    Ref<Gradient> gradient = _transfer_function->get_gradient_color()->get_gradient();
    if (selected_index == -1) {
        return;
    }

    picker->set_pick_color(gradient->get_color(selected_index));
    Size2 minsize = popup->get_contents_minimum_size();
    float viewport_height = get_viewport_rect().size.y;

    // Determine in which direction to show the popup. By default popup below.
    // But if the popup doesn't fit below and the Gradient Editor is in the bottom half of the viewport, show above.
    bool show_above = get_global_position().y + get_size().y + minsize.y > viewport_height && get_global_position().y * 2 + get_size().y > viewport_height;

    float v_offset = show_above ? -minsize.y : get_size().y;
    popup->set_position(get_screen_position() + Vector2(0, v_offset));
    popup->popup();
}

void PaletteTextureRect::_gui_input(const Ref<InputEvent> &p_event) {
    if (_transfer_function.is_null()) {
        return;
    } else if (_transfer_function->get_gradient_color().is_null()) {
        return;
    } else if (_transfer_function->get_gradient_color()->get_gradient().is_null()) {
        return;
    }
    Ref<Gradient> gradient = _transfer_function->get_gradient_color()->get_gradient();
    ERR_FAIL_COND(p_event.is_null());

    Ref<InputEventKey> k = p_event;

    if (k.is_valid() && k->is_pressed() && k->get_keycode() == Key::KEY_DELETE && selected_index != -1) {
        if (grabbing == GRAB_ADD) {
            gradient->remove_point(selected_index); // Point is temporary, so remove directly from gradient.
            set_selected_index(-1);
        } else {
            remove_point(selected_index);
        }
        grabbing = GRAB_NONE;
        hovered_index = -1;
        accept_event();
    }

    Ref<InputEventMouseButton> mb = p_event;

    if (mb.is_valid() && mb->is_pressed()) {
        float adjusted_mb_x = mb->get_position().x - handle_width / 2;
        bool should_snap = snap_enabled || mb->is_ctrl_pressed();

        // Delete point or move it to old position on middle or right click.
        if (mb->get_button_index() == MouseButton::MOUSE_BUTTON_RIGHT || mb->get_button_index() == MouseButton::MOUSE_BUTTON_MIDDLE) {
            if (grabbing == GRAB_MOVE && mb->get_button_index() == MouseButton::MOUSE_BUTTON_RIGHT) {
                gradient->set_offset(selected_index, pre_grab_offset);
                set_selected_index(pre_grab_index);
            } else {
                int point_to_remove = _get_point_at(adjusted_mb_x);
                if (point_to_remove == -1) {
                    set_selected_index(-1); // Nothing on the place of the click, just deselect any handle.
                } else {
                    if (grabbing == GRAB_ADD) {
                        gradient->remove_point(point_to_remove); // Point is temporary, so remove directly from gradient.
                        set_selected_index(-1);
                    } else {
                        remove_point(point_to_remove);
                    }
                    hovered_index = -1;
                }
            }
            grabbing = GRAB_NONE;
            accept_event();
        }

        // Select point.
        if (mb->get_button_index() == MouseButton::MOUSE_BUTTON_LEFT) {
            int total_w = get_size().x;

            // Check if gradient was double-clicked.
            if (mb->is_double_click()) {
                set_selected_index(_get_point_at(adjusted_mb_x));
                _show_color_picker();
                accept_event();
                return;
            }

            if (grabbing == GRAB_NONE) {
                set_selected_index(_get_point_at(adjusted_mb_x));
            }

            if (selected_index != -1 && !mb->is_alt_pressed()) {
                // An existing point was grabbed.
                grabbing = GRAB_MOVE;
                pre_grab_offset = gradient->get_offset(selected_index);
                pre_grab_index = selected_index;
            } else if (grabbing == GRAB_NONE) {
                // Adding a new point. Insert a temporary point for the user to adjust, so it's not in the undo/redo.
                float new_offset = CLAMP(adjusted_mb_x / float(total_w), 0, 1);
                if (should_snap) {
                    new_offset = Math::snapped(new_offset, 1.0 / snap_count);
                }

                for (int i = 0; i < gradient->get_point_count(); i++) {
                    if (gradient->get_offset(i) == new_offset) {
                        // If another point with the same offset is found, then
                        // tweak it if Alt was pressed, otherwise something has gone wrong, so stop the operation.
                        if (mb->is_alt_pressed()) {
                            new_offset = MIN(gradient->get_offset(i) + 0.00001, 1);
                        } else {
                            return;
                        }
                    }
                }

                Color new_color = gradient->sample(new_offset);
                if (mb->is_alt_pressed()) {
                    // Alt + Click on a point duplicates it. So copy its color.
                    int point_to_copy = _get_point_at(adjusted_mb_x);
                    if (point_to_copy != -1) {
                        new_color = gradient->get_color(point_to_copy);
                    }
                }
                // Add a temporary point for the user to adjust before adding it permanently.
                gradient->add_point(new_offset, new_color);
                set_selected_index(_predict_insertion_index(new_offset));
                grabbing = GRAB_ADD;
            }
        }
    }

    if (mb.is_valid() && mb->get_button_index() == MouseButton::MOUSE_BUTTON_LEFT && !mb->is_pressed()) {
        if (grabbing == GRAB_MOVE) {
            // Finish moving a point.
            //set_offset(selected_index, gradient->get_offset(selected_index)); ???
            grabbing = GRAB_NONE;
        } else if (grabbing == GRAB_ADD) {
            // Finish inserting a new point. Remove the temporary point and insert the permanent one in its place.
            //float new_offset = gradient->get_offset(selected_index);
            //Color new_color = gradient->get_color(selected_index);
            //gradient->remove_point(selected_index);
            //add_point(new_offset, new_color);
            grabbing = GRAB_NONE;
        }
    }

    Ref<InputEventMouseMotion> mm = p_event;

    if (mm.is_valid()) {
        int total_w = get_size().x;
        float adjusted_mm_x = mm->get_position().x;
        bool should_snap = snap_enabled || mm->is_ctrl_pressed();

        // Hovering logic.
        if (grabbing == GRAB_NONE) {
            int nearest_point = _get_point_at(adjusted_mm_x);
            if (hovered_index != nearest_point) {
                hovered_index = nearest_point;
                queue_redraw();
            }
            return;
        } else {
            hovered_index = -1;
        }

        // Grabbing logic.
        float new_offset = CLAMP(adjusted_mm_x / float(total_w), 0, 1);

        // Give the ability to snap right next to a point when using Shift.
        if (mm->is_shift_pressed()) {
            float smallest_offset = should_snap ? (0.5 / snap_count) : 0.01;
            int nearest_idx = -1;
            // Only check the two adjacent points to find which one is the nearest.
            if (selected_index > 0) {
                float temp_offset = ABS(gradient->get_offset(selected_index - 1) - new_offset);
                if (temp_offset < smallest_offset) {
                    smallest_offset = temp_offset;
                    nearest_idx = selected_index - 1;
                }
            }
            if (selected_index < gradient->get_point_count() - 1) {
                float temp_offset = ABS(gradient->get_offset(selected_index + 1) - new_offset);
                if (temp_offset < smallest_offset) {
                    smallest_offset = temp_offset;
                    nearest_idx = selected_index + 1;
                }
            }
            if (nearest_idx != -1) {
                // Snap to the point with a slight adjustment to the left or right.
                float adjustment = gradient->get_offset(nearest_idx) < new_offset ? 0.00001 : -0.00001;
                new_offset = CLAMP(gradient->get_offset(nearest_idx) + adjustment, 0, 1);
            } else if (should_snap) {
                new_offset = Math::snapped(new_offset, 1.0 / snap_count);
            }
        } else if (should_snap) {
            // Shift is not pressed, so snap fully without adjustments.
            new_offset = Math::snapped(new_offset, 1.0 / snap_count);
        }

        // Don't move the point if its new offset would be the same as another point's.
        for (int i = 0; i < gradient->get_point_count(); i++) {
            if (gradient->get_offset(i) == new_offset && i != selected_index) {
                return;
            }
        }

        if (selected_index == -1) {
            return;
        }

        // We want to only save this action for undo/redo when released, so don't use set_offset() yet.
        gradient->set_offset(selected_index, new_offset);

        // Update selected_index after the gradient updates its indices, so you keep holding the same color.
        for (int i = 0; i < gradient->get_point_count(); i++) {
            if (gradient->get_offset(i) == new_offset) {
                set_selected_index(i);
                break;
            }
        }
    }
}

///////////////////////

void EditorInspectorPluginTransferFunction::_bind_methods() {}

bool EditorInspectorPluginTransferFunction::_can_handle(Object *p_object) const {
    return p_object != nullptr && Object::cast_to<TransferFunction>(p_object) != nullptr;
}

void EditorInspectorPluginTransferFunction::_parse_begin(Object *p_object) {
    TransferFunction *tf = static_cast<TransferFunction *>(p_object);
    ERR_FAIL_NULL(tf);
    Ref<TransferFunction> tf_ref(tf);

    TransferFunctionEditor *editor = memnew(TransferFunctionEditor);
    editor->set_transfer_function(tf_ref);
    add_custom_control(editor);
}