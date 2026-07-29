shader_type canvas_item;
render_mode blend_add, unshaded;

uniform float spin_speed = 0.35;
uniform float radial_power = 1.15;
uniform vec4 tint : source_color = vec4(0.35, 0.78, 1.0, 1.0);

void fragment() {
	vec2 centered = UV - vec2(0.5);
	float radius = length(centered);
	float angle = atan(centered.y, centered.x) + TIME * spin_speed + radius * 4.0;
	vec2 warped = vec2(cos(angle), sin(angle)) * pow(radius, radial_power) + vec2(0.5);
	vec4 tex = texture(TEXTURE, warped);
	float mask = smoothstep(0.52, 0.02, radius);
	COLOR = tex * tint * COLOR * mask;
}
