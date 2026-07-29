shader_type canvas_item;
render_mode blend_add, unshaded;

uniform float spin_speed = 0.025;
uniform float pulse_speed = 1.6;
uniform vec4 tint : source_color = vec4(0.35, 0.68, 1.0, 1.0);

void fragment() {
	vec2 centered = UV - vec2(0.5);
	float angle = TIME * spin_speed;
	mat2 rotation = mat2(vec2(cos(angle), -sin(angle)), vec2(sin(angle), cos(angle)));
	vec2 rotated_uv = rotation * centered + vec2(0.5);
	vec4 tex = texture(TEXTURE, rotated_uv);
	float pulse = 0.78 + 0.22 * sin(TIME * pulse_speed);
	COLOR = tex * tint * COLOR * pulse;
}
