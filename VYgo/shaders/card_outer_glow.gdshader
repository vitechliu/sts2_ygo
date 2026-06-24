shader_type canvas_item;
render_mode blend_add, unshaded;

uniform vec4 glow_color : source_color = vec4(0.44, 0.91, 1.0, 1.0);
uniform float glow_radius : hint_range(1.0, 32.0, 0.5) = 14.0;
uniform float glow_intensity : hint_range(0.0, 5.0, 0.05) = 1.2;
uniform float glow_opacity : hint_range(0.0, 1.0, 0.01) = 0.72;
uniform float pulse_amount : hint_range(0.0, 1.0, 0.01) = 0.14;
uniform float pulse_speed : hint_range(0.0, 12.0, 0.1) = 4.5;
uniform float pulse_phase : hint_range(0.0, 6.283, 0.01) = 0.0;
uniform float vertical_blur : hint_range(0.0, 1.0, 0.01) = 0.0;
uniform float vertical_blur_length : hint_range(0.0, 160.0, 1.0) = 90.0;

void fragment() {
	vec2 x = vec2(TEXTURE_PIXEL_SIZE.x * glow_radius * 0.65, 0.0);
	vec2 y = vec2(0.0, TEXTURE_PIXEL_SIZE.y * glow_radius * 0.65);
	vec2 diagonal = vec2(
		TEXTURE_PIXEL_SIZE.x * glow_radius,
		TEXTURE_PIXEL_SIZE.y * glow_radius
	) * 0.7071;

	float center = texture(TEXTURE, UV).a;
	float blur = 0.0;
	blur += texture(TEXTURE, UV + x).a;
	blur += texture(TEXTURE, UV - x).a;
	blur += texture(TEXTURE, UV + y).a;
	blur += texture(TEXTURE, UV - y).a;
	blur += texture(TEXTURE, UV + diagonal).a;
	blur += texture(TEXTURE, UV - diagonal).a;
	blur += texture(TEXTURE, UV + vec2(diagonal.x, -diagonal.y)).a;
	blur += texture(TEXTURE, UV + vec2(-diagonal.x, diagonal.y)).a;
	blur *= 0.125;

	float halo = max(blur * 1.25 - center * 0.7, 0.0);
	float vertical_streak = 0.0;
	if (vertical_blur > 0.001) {
		vec2 vertical_step = vec2(0.0, TEXTURE_PIXEL_SIZE.y * vertical_blur_length * 0.25);
		vertical_streak += texture(TEXTURE, UV - vertical_step).a * 0.38;
		vertical_streak += texture(TEXTURE, UV - vertical_step * 2.0).a * 0.28;
		vertical_streak += texture(TEXTURE, UV - vertical_step * 3.0).a * 0.20;
		vertical_streak += texture(TEXTURE, UV - vertical_step * 4.0).a * 0.12;
		vertical_streak *= vertical_blur;
	}

	float pulse = 1.0 + sin(TIME * pulse_speed + pulse_phase) * pulse_amount;
	float alpha = clamp((halo + vertical_streak) * glow_opacity * pulse, 0.0, 1.0);

	COLOR = vec4(glow_color.rgb * glow_intensity * pulse, alpha);
}
