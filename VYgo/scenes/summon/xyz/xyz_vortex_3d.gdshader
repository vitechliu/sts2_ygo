shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never;

uniform sampler2D vortex_texture : source_color;
uniform vec4 tint : source_color = vec4(0.48, 0.34, 1.0, 1.0);
uniform float opacity : hint_range(0.0, 1.0) = 0.0;

void fragment() {
	vec4 tex = texture(vortex_texture, UV);
	float radius = length(UV - vec2(0.5));
	float edge_fade = 1.0 - smoothstep(0.30, 0.49, radius);
	vec3 color = tex.rgb * tint.rgb;
	ALBEDO = color;
	EMISSION = color;
	ALPHA = tex.a * tint.a * opacity * edge_fade;
}
