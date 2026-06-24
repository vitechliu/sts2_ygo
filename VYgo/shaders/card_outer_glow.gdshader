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
	vec2 near_x = vec2(TEXTURE_PIXEL_SIZE.x * glow_radius * 0.45, 0.0);
	vec2 near_y = vec2(0.0, TEXTURE_PIXEL_SIZE.y * glow_radius * 0.45);
	vec2 far_x = vec2(TEXTURE_PIXEL_SIZE.x * glow_radius, 0.0);
	vec2 far_y = vec2(0.0, TEXTURE_PIXEL_SIZE.y * glow_radius);
	vec2 near_diagonal = vec2(near_x.x, near_y.y) * 0.7071;
	vec2 far_diagonal = vec2(far_x.x, far_y.y) * 0.7071;

	float center = texture(TEXTURE, UV).a;
	float near_blur = 0.0;
	near_blur += texture(TEXTURE, UV + near_x).a;
	near_blur += texture(TEXTURE, UV - near_x).a;
	near_blur += texture(TEXTURE, UV + near_y).a;
	near_blur += texture(TEXTURE, UV - near_y).a;
	near_blur += texture(TEXTURE, UV + near_diagonal).a;
	near_blur += texture(TEXTURE, UV - near_diagonal).a;
	near_blur += texture(TEXTURE, UV + vec2(near_diagonal.x, -near_diagonal.y)).a;
	near_blur += texture(TEXTURE, UV + vec2(-near_diagonal.x, near_diagonal.y)).a;
	near_blur *= 0.125;

	float far_blur = 0.0;
	far_blur += texture(TEXTURE, UV + far_x).a;
	far_blur += texture(TEXTURE, UV - far_x).a;
	far_blur += texture(TEXTURE, UV + far_y).a;
	far_blur += texture(TEXTURE, UV - far_y).a;
	far_blur += texture(TEXTURE, UV + far_diagonal).a;
	far_blur += texture(TEXTURE, UV - far_diagonal).a;
	far_blur += texture(TEXTURE, UV + vec2(far_diagonal.x, -far_diagonal.y)).a;
	far_blur += texture(TEXTURE, UV + vec2(-far_diagonal.x, far_diagonal.y)).a;
	far_blur *= 0.125;

	float halo = max(near_blur * 0.8 + far_blur * 0.65 - center * 0.7, 0.0);
	float vertical_streak = 0.0;
	vec2 vertical_step = vec2(0.0, TEXTURE_PIXEL_SIZE.y * vertical_blur_length / 6.0);
	vertical_streak += texture(TEXTURE, UV - vertical_step).a * 0.30;
	vertical_streak += texture(TEXTURE, UV - vertical_step * 2.0).a * 0.23;
	vertical_streak += texture(TEXTURE, UV - vertical_step * 3.0).a * 0.17;
	vertical_streak += texture(TEXTURE, UV - vertical_step * 4.0).a * 0.12;
	vertical_streak += texture(TEXTURE, UV - vertical_step * 5.0).a * 0.08;
	vertical_streak += texture(TEXTURE, UV - vertical_step * 6.0).a * 0.05;
	vertical_streak *= vertical_blur;

	float pulse = 1.0 + sin(TIME * pulse_speed + pulse_phase) * pulse_amount;
	float alpha = clamp((halo + vertical_streak) * glow_opacity * pulse, 0.0, 1.0);

	COLOR = vec4(glow_color.rgb * glow_intensity * pulse, alpha);
}
