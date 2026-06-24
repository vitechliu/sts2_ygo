shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix, depth_draw_always;

uniform sampler2D card_texture : source_color, filter_linear, repeat_disable;
uniform vec4 glow_color : source_color = vec4(0.44, 0.91, 1.0, 1.0);
uniform float outline_width : hint_range(0.0, 8.0, 0.1) = 3.0;
uniform float outline_strength : hint_range(0.0, 8.0, 0.05) = 1.8;
uniform float pulse_amount : hint_range(0.0, 1.0, 0.01) = 0.16;
uniform float pulse_speed : hint_range(0.0, 12.0, 0.1) = 4.5;
uniform float pulse_phase : hint_range(0.0, 6.283, 0.01) = 0.0;

void fragment() {
	vec4 card = texture(card_texture, UV);
	vec2 pixel = 1.0 / vec2(textureSize(card_texture, 0));
	vec2 x = vec2(pixel.x * outline_width, 0.0);
	vec2 y = vec2(0.0, pixel.y * outline_width);
	vec2 diagonal = vec2(pixel.x, pixel.y) * outline_width * 0.7071;

	float nearby_alpha = 0.0;
	nearby_alpha = max(nearby_alpha, texture(card_texture, UV + x).a);
	nearby_alpha = max(nearby_alpha, texture(card_texture, UV - x).a);
	nearby_alpha = max(nearby_alpha, texture(card_texture, UV + y).a);
	nearby_alpha = max(nearby_alpha, texture(card_texture, UV - y).a);
	nearby_alpha = max(nearby_alpha, texture(card_texture, UV + diagonal).a);
	nearby_alpha = max(nearby_alpha, texture(card_texture, UV - diagonal).a);
	nearby_alpha = max(nearby_alpha, texture(card_texture, UV + vec2(diagonal.x, -diagonal.y)).a);
	nearby_alpha = max(nearby_alpha, texture(card_texture, UV + vec2(-diagonal.x, diagonal.y)).a);

	float outline = max(nearby_alpha - card.a, 0.0);
	float pulse = 1.0 + sin(TIME * pulse_speed + pulse_phase) * pulse_amount;
	float energy = outline_strength * pulse;

	ALBEDO = card.rgb + glow_color.rgb * outline * energy * 0.28;
	EMISSION = glow_color.rgb * outline * energy;
	ALPHA = max(card.a, outline);
}
