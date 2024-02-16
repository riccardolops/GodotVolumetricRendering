shader_type canvas_item;

uniform sampler2D tf_tex_colour;
uniform sampler2D tf_tex_alpha;

void vertex()
{
}

void fragment()
{
	float density = UV.x;
	float histY = 1.0 - texture(TEXTURE, vec2(density, 0.0)).r;
	//vec4 histCol = histY < UV.y ? vec4(1.0, 1.0, 1.0, 1.0) : vec4(0.0, 0.0, 0.0, 0.0);
	vec3 tfCol = texture(tf_tex_colour, vec2(density, 0.0)).rgb;
	float alpha = 1.0-texture(tf_tex_alpha, vec2(density, 0.0)).a;
	vec3 histCol = histY < UV.y ? vec3(1.0, 1.0, 1.0) : vec3(0.0, 0.0, 0.0);
	
	vec3 col = histCol * 0.5 + tfCol * 0.7;
	COLOR = vec4(col, UV.y > alpha ? 1.0 : 0.8);
	//COLOR = vec4(alpha, 0.0, 0.0, 1.0);
	//COLOR = texture(TEXTURE, UV);
	//COLOR = vec4(UV, 0.0, 1.0);
}
