shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix, depth_draw_always;

uniform sampler2D card_texture : source_color, filter_linear, repeat_disable;
uniform vec4 glow_color : source_color = vec4(0.44, 0.91, 1.0, 1.0);
uniform float outline_width : hint_range(0.0, 8.0, 0.1) = 3.0;
uniform float outline_strength : hint_range(0.0, 8.0, 0.05) = 1.8;
uniform float pulse_amount : hint_range(0.0, 1.0, 0.01) = 0.16;
uniform float pulse_speed : hint_range(0.0, 12.0, 0.1) = 4.5;
uniform float pulse_phase : hint_range(0.0, 6.283, 0.01) = 0.0;
uniform float suction_progress : hint_range(0.0, 1.0, 0.01) = 0.0;
uniform vec2 suction_direction = vec2(0.0, -1.0);

vec2 rotate_2d(vec2 value, float angle) {
	float sine = sin(angle);
	float cosine = cos(angle);
	return mat2(vec2(cosine, sine), vec2(-sine, cosine)) * value;
}

vec2 get_suction_direction() {
	float direction_length = length(suction_direction);
	if (direction_length < 0.0001) {
		return vec2(0.0, -1.0);
	}
	return suction_direction / direction_length;
}

vec2 warp_card_uv(vec2 uv) {
	if (suction_progress <= 0.0001) {
		return uv;
	}

	vec2 texture_size = vec2(textureSize(card_texture, 0));
	float aspect = texture_size.x / max(texture_size.y, 1.0);
	vec2 aspect_scale = vec2(aspect, 1.0);
	vec2 point = (uv - vec2(0.5)) * aspect_scale;
	vec2 direction = get_suction_direction();
	vec2 perpendicular = vec2(-direction.y, direction.x);

	// Place the visual sink just beyond the edge facing the fusion vortex.
	float edge_distance = abs(direction.x) * aspect * 0.5 + abs(direction.y) * 0.5;
	vec2 sink = direction * (edge_distance + 0.08);
	vec2 relative = point - sink;
	float distance_to_sink = length(relative);
	float influence = 1.0 - smoothstep(0.12, 0.92, distance_to_sink);
	float strength = suction_progress * influence;

	// Reverse-map the output pixel through a tightening curl. Samples near the
	// sink are pulled lengthwise and pinched across the direction of travel.
	relative = rotate_2d(relative, -1.65 * strength * strength);
	float along = dot(relative, direction);
	float across = dot(relative, perpendicular);
	float taper = max(1.0 - 0.7 * strength, 0.28);
	across /= taper;
	along -= 0.13 * strength * strength;

	vec2 warped_point = sink + direction * along + perpendicular * across;
	return warped_point / aspect_scale + vec2(0.5);
}

float uv_bounds_mask(vec2 uv) {
	vec2 lower = step(vec2(0.0), uv);
	vec2 upper = step(uv, vec2(1.0));
	return lower.x * lower.y * upper.x * upper.y;
}

vec4 sample_warped_card(vec2 uv) {
	vec2 warped_uv = warp_card_uv(uv);
	float bounds = uv_bounds_mask(warped_uv);
	return texture(card_texture, clamp(warped_uv, vec2(0.0), vec2(1.0))) * bounds;
}

void vertex() {
	if (suction_progress > 0.0001) {
		vec2 direction = get_suction_direction();
		vec2 geometry_direction = vec2(direction.x, -direction.y);
		vec2 perpendicular = vec2(-geometry_direction.y, geometry_direction.x);
		float facing = smoothstep(
			-0.35,
			0.95,
			dot((UV - vec2(0.5)) * 2.0, direction)
		);
		float strength = suction_progress * suction_progress * facing;
		float across = dot(VERTEX.xy, perpendicular);

		// The quad only needs a restrained silhouette pull; the fragment warp
		// supplies the finer curl while the FX viewport provides room for it.
		VERTEX.xy += geometry_direction * 72.0 * strength;
		VERTEX.xy -= perpendicular * across * 0.58 * strength;
	}
}

void fragment() {
	vec4 card = sample_warped_card(UV);
	vec2 pixel = 1.0 / vec2(textureSize(card_texture, 0));
	vec2 x = vec2(pixel.x * outline_width, 0.0);
	vec2 y = vec2(0.0, pixel.y * outline_width);
	vec2 diagonal = vec2(pixel.x, pixel.y) * outline_width * 0.7071;

	float nearby_alpha = 0.0;
	nearby_alpha = max(nearby_alpha, sample_warped_card(UV + x).a);
	nearby_alpha = max(nearby_alpha, sample_warped_card(UV - x).a);
	nearby_alpha = max(nearby_alpha, sample_warped_card(UV + y).a);
	nearby_alpha = max(nearby_alpha, sample_warped_card(UV - y).a);
	nearby_alpha = max(nearby_alpha, sample_warped_card(UV + diagonal).a);
	nearby_alpha = max(nearby_alpha, sample_warped_card(UV - diagonal).a);
	nearby_alpha = max(nearby_alpha, sample_warped_card(UV + vec2(diagonal.x, -diagonal.y)).a);
	nearby_alpha = max(nearby_alpha, sample_warped_card(UV + vec2(-diagonal.x, diagonal.y)).a);

	float outline = max(nearby_alpha - card.a, 0.0);
	float pulse = 1.0 + sin(TIME * pulse_speed + pulse_phase) * pulse_amount;
	float energy = outline_strength * pulse;

	ALBEDO = card.rgb + glow_color.rgb * outline * energy * 0.28;
	EMISSION = glow_color.rgb * outline * energy;
	ALPHA = max(card.a, outline);
}
