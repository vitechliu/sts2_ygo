// NOTE: Shader automatically converted from MegaDot 4.5.1.m.8.mono's ParticleProcessMaterial.

shader_type particles;
render_mode disable_velocity;

const int EMISSION_SHAPE_POINT = 0;
const int EMISSION_SHAPE_SPHERE = 1;
const int EMISSION_SHAPE_SPHERE_SURFACE = 2;
const int EMISSION_SHAPE_BOX = 3;
const int EMISSION_SHAPE_POINTS = 4;
const int EMISSION_SHAPE_DIRECTED_POINTS = 5;
const int EMISSION_SHAPE_RING = 6;
const int SUB_EMITTER_DISABLED = 0;
const int SUB_EMITTER_CONSTANT = 1;
const int SUB_EMITTER_AT_END = 2;
const int SUB_EMITTER_AT_COLLISION = 3;
const int SUB_EMITTER_AT_START = 4;
const int COLLISION_DISABLED = 0;
const int COLLISION_RIGID = 1;
const int COLLISION_HIDE_ON_CONTACT = 2;
const int PARAM_INITIAL_LINEAR_VELOCITY = 0;
const int PARAM_ANGULAR_VELOCITY = 1;
const int PARAM_ORBIT_VELOCITY = 2;
const int PARAM_LINEAR_ACCEL = 3;
const int PARAM_RADIAL_ACCEL = 4;
const int PARAM_TANGENTIAL_ACCEL = 5;
const int PARAM_DAMPING = 6;
const int PARAM_ANGLE = 7;
const int PARAM_SCALE = 8;
const int PARAM_HUE_VARIATION = 9;
const int PARAM_ANIM_SPEED = 10;
const int PARAM_ANIM_OFFSET = 11;
const int PARAM_TURB_INFLUENCE_OVER_LIFE = 12;
const int PARAM_TURB_VEL_INFLUENCE = 13;
const int PARAM_TURB_INIT_DISPLACEMENT = 14;
const int PARAM_RADIAL_VELOCITY = 15;
const int PARAM_DIRECTIONAL_VELOCITY = 16;
const int PARAM_SCALE_OVER_VELOCITY = 17;
const int PARAM_MAX = 18;
const int PARTICLE_FLAG_ALIGN_Y_TO_VELOCITY = 0;
const int PARTICLE_FLAG_ROTATE_Y = 1;
const int PARTICLE_FLAG_DISABLE_Z = 2;
const int PARTICLE_FLAG_DAMPING_AS_FRICTION = 3;
const int PARTICLE_FLAG_MAX = 4;

uniform bool use_color_ramp;
uniform int emission_shape;
uniform bool has_emission_color;
uniform int sub_emitter_mode;
uniform int collision_mode;
uniform bool attractor_enabled;
uniform bool turbulence_enabled;
uniform bool use_limiter_curve;
uniform bool use_alpha_curve;
uniform bool use_emission_curve;
uniform bool has_initial_ramp;
uniform bool orbit_uses_curve_xyz;
uniform bool use_tex_param[PARAM_MAX];
uniform bool particle_flags[PARTICLE_FLAG_MAX];
uniform vec3 direction;
uniform float spread;
uniform float flatness;
uniform float inherit_emitter_velocity_ratio = 0.0;
uniform float initial_linear_velocity_min;
uniform float initial_linear_velocity_max;
uniform float directional_velocity_min;
uniform float directional_velocity_max;
uniform float angular_velocity_min;
uniform float angular_velocity_max;
uniform float orbit_velocity_min;
uniform float orbit_velocity_max;
uniform float radial_velocity_min;
uniform float radial_velocity_max;
uniform float linear_accel_min;
uniform float linear_accel_max;
uniform float radial_accel_min;
uniform float radial_accel_max;
uniform float tangent_accel_min;
uniform float tangent_accel_max;
uniform float damping_min;
uniform float damping_max;
uniform float initial_angle_min;
uniform float initial_angle_max;
uniform float scale_min;
uniform float scale_max;
uniform vec3 start_size_min = vec3(1.0, 1.0, 1.0);
uniform vec3 start_size_max = vec3(1.0, 1.0, 1.0);
uniform bool use_start_size_3d = false;
uniform float hue_variation_min;
uniform float hue_variation_max;
uniform float anim_speed_min;
uniform float anim_speed_max;
uniform float anim_offset_min;
uniform float anim_offset_max;
uniform float lifetime_randomness;
uniform vec3 emission_shape_offset = vec3(0.0);
uniform vec3 emission_shape_scale = vec3(1.0);
uniform vec3 velocity_pivot = vec3(0.0);
uniform float scale_over_velocity_min = 0.0;
uniform float scale_over_velocity_max = 5.0;
uniform float emission_sphere_radius;
uniform vec3 emission_box_extents;
uniform sampler2D emission_texture_normal : hint_default_black;
uniform sampler2D emission_texture_points : hint_default_black;
uniform int emission_texture_point_count;
uniform sampler2D emission_texture_color : hint_default_white;
uniform vec3 emission_ring_axis;
uniform float emission_ring_height;
uniform float emission_ring_radius;
uniform float emission_ring_inner_radius;
uniform float emission_ring_cone_angle;
uniform float sub_emitter_frequency;
uniform int sub_emitter_amount_at_end;
uniform int sub_emitter_amount_at_start;
uniform bool sub_emitter_keep_velocity;
uniform vec4 color_value : source_color;
uniform vec3 gravity;
uniform sampler2D color_ramp : repeat_disable;
uniform sampler2D color_initial_ramp : repeat_disable;
uniform sampler2D alpha_curve : repeat_disable;
uniform sampler2D emission_curve : repeat_disable;
uniform sampler2D linear_velocity_texture : repeat_disable;
uniform sampler2D orbit_velocity_curve : repeat_disable;
uniform sampler2D angular_velocity_texture : repeat_disable;
uniform sampler2D linear_accel_texture : repeat_disable;
uniform sampler2D radial_accel_texture : repeat_disable;
uniform sampler2D tangent_accel_texture : repeat_disable;
uniform sampler2D damping_texture : repeat_disable;
uniform sampler2D angle_texture : repeat_disable;
uniform sampler2D scale_curve : repeat_disable;
uniform sampler2D hue_rot_curve : repeat_disable;
uniform sampler2D animation_speed_curve : repeat_disable;
uniform sampler2D animation_offset_curve : repeat_disable;
uniform sampler2D radial_velocity_curve : repeat_disable;
uniform sampler2D scale_over_velocity_curve : repeat_disable;
uniform sampler2D directional_velocity_curve : repeat_disable;
uniform sampler2D velocity_limit_curve : repeat_disable;
uniform float turbulence_noise_strength;
uniform float turbulence_noise_scale;
uniform float turbulence_influence_min;
uniform float turbulence_influence_max;
uniform float turbulence_initial_displacement_min;
uniform float turbulence_initial_displacement_max;
uniform float turbulence_noise_speed_random;
uniform vec3 turbulence_noise_speed = vec3(1.0, 1.0, 1.0);
uniform sampler2D turbulence_influence_over_life;
uniform sampler2D turbulence_color_ramp;

// Functions for 3D noise / turbulence.
vec4 grad(vec4 p) {
	p = fract(vec4(
			dot(p, vec4(0.143081, 0.001724, 0.280166, 0.262771)),
			dot(p, vec4(0.645401, -0.047791, -0.146698, 0.595016)),
			dot(p, vec4(-0.499665, -0.095734, 0.425674, -0.207367)),
			dot(p, vec4(-0.013596, -0.848588, 0.423736, 0.17044))));
	return fract((p.xyzw * p.yzwx) * 2365.952041) * 2.0 - 1.0;
}

float noise(vec4 coord) {
	// Domain rotation to improve the look of XYZ slices + animation patterns.
	coord = vec4(
			coord.xyz + dot(coord, vec4(vec3(-0.1666667), -0.5)),
			dot(coord, vec4(0.5)));

	vec4 base = floor(coord), delta = coord - base;

	vec4 grad_0000 = grad(base + vec4(0.0, 0.0, 0.0, 0.0)), grad_1000 = grad(base + vec4(1.0, 0.0, 0.0, 0.0));
	vec4 grad_0100 = grad(base + vec4(0.0, 1.0, 0.0, 0.0)), grad_1100 = grad(base + vec4(1.0, 1.0, 0.0, 0.0));
	vec4 grad_0010 = grad(base + vec4(0.0, 0.0, 1.0, 0.0)), grad_1010 = grad(base + vec4(1.0, 0.0, 1.0, 0.0));
	vec4 grad_0110 = grad(base + vec4(0.0, 1.0, 1.0, 0.0)), grad_1110 = grad(base + vec4(1.0, 1.0, 1.0, 0.0));
	vec4 grad_0001 = grad(base + vec4(0.0, 0.0, 0.0, 1.0)), grad_1001 = grad(base + vec4(1.0, 0.0, 0.0, 1.0));
	vec4 grad_0101 = grad(base + vec4(0.0, 1.0, 0.0, 1.0)), grad_1101 = grad(base + vec4(1.0, 1.0, 0.0, 1.0));
	vec4 grad_0011 = grad(base + vec4(0.0, 0.0, 1.0, 1.0)), grad_1011 = grad(base + vec4(1.0, 0.0, 1.0, 1.0));
	vec4 grad_0111 = grad(base + vec4(0.0, 1.0, 1.0, 1.0)), grad_1111 = grad(base + vec4(1.0, 1.0, 1.0, 1.0));

	vec4 result_0123 = vec4(
			dot(delta - vec4(0.0, 0.0, 0.0, 0.0), grad_0000), dot(delta - vec4(1.0, 0.0, 0.0, 0.0), grad_1000),
			dot(delta - vec4(0.0, 1.0, 0.0, 0.0), grad_0100), dot(delta - vec4(1.0, 1.0, 0.0, 0.0), grad_1100));
	vec4 result_4567 = vec4(
			dot(delta - vec4(0.0, 0.0, 1.0, 0.0), grad_0010), dot(delta - vec4(1.0, 0.0, 1.0, 0.0), grad_1010),
			dot(delta - vec4(0.0, 1.0, 1.0, 0.0), grad_0110), dot(delta - vec4(1.0, 1.0, 1.0, 0.0), grad_1110));
	vec4 result_89AB = vec4(
			dot(delta - vec4(0.0, 0.0, 0.0, 1.0), grad_0001), dot(delta - vec4(1.0, 0.0, 0.0, 1.0), grad_1001),
			dot(delta - vec4(0.0, 1.0, 0.0, 1.0), grad_0101), dot(delta - vec4(1.0, 1.0, 0.0, 1.0), grad_1101));
	vec4 result_CDEF = vec4(
			dot(delta - vec4(0.0, 0.0, 1.0, 1.0), grad_0011), dot(delta - vec4(1.0, 0.0, 1.0, 1.0), grad_1011),
			dot(delta - vec4(0.0, 1.0, 1.0, 1.0), grad_0111), dot(delta - vec4(1.0, 1.0, 1.0, 1.0), grad_1111));

	vec4 fade = delta * delta * delta * (10.0 + delta * (-15.0 + delta * 6.0));
	vec4 result_W0 = mix(result_0123, result_89AB, fade.w), result_W1 = mix(result_4567, result_CDEF, fade.w);
	vec4 result_WZ = mix(result_W0, result_W1, fade.z);
	vec2 result_WZY = mix(result_WZ.xy, result_WZ.zw, fade.y);
	return mix(result_WZY.x, result_WZY.y, fade.x);
}

// Curl 3D and three-noise function with friendly permission by Isaac Cohen.
// Modified to accept 4D noise.
vec3 noise_3x(vec4 p) {
	float s = noise(p);
	float s1 = noise(p + vec4(vec3(0.0), 1.7320508 * 2048.333333));
	float s2 = noise(p - vec4(vec3(0.0), 1.7320508 * 2048.333333));
	vec3 c = vec3(s, s1, s2);
	return c;
}

vec3 curl_3d(vec4 p, float c) {
	float epsilon = 0.001 + c;
	vec4 dx = vec4(epsilon, 0.0, 0.0, 0.0);
	vec4 dy = vec4(0.0, epsilon, 0.0, 0.0);
	vec4 dz = vec4(0.0, 0.0, epsilon, 0.0);
	vec3 x0 = noise_3x(p - dx).xyz;
	vec3 x1 = noise_3x(p + dx).xyz;
	vec3 y0 = noise_3x(p - dy).xyz;
	vec3 y1 = noise_3x(p + dy).xyz;
	vec3 z0 = noise_3x(p - dz).xyz;
	vec3 z1 = noise_3x(p + dz).xyz;
	float x = (y1.z - y0.z) - (z1.y - z0.y);
	float y = (z1.x - z0.x) - (x1.z - x0.z);
	float z = (x1.y - x0.y) - (y1.x - y0.x);
	return normalize(vec3(x, y, z));
}

vec3 get_noise_direction(vec3 pos) {
	float adj_contrast = max((turbulence_noise_strength - 1.0), 0.0) * 70.0;
	vec4 noise_time = TIME * vec4(turbulence_noise_speed, turbulence_noise_speed_random);
	vec4 noise_pos = vec4(pos * turbulence_noise_scale, 0.0);
	vec3 noise_direction = curl_3d(noise_pos + noise_time, adj_contrast);
	noise_direction = mix(0.9 * noise_direction, noise_direction, turbulence_noise_strength - 9.0);
	return noise_direction;
}

vec4 rotate_hue(vec4 current_color, float hue_rot_angle) {
	float hue_rot_c = cos(hue_rot_angle);
	float hue_rot_s = sin(hue_rot_angle);
	mat4 hue_rot_mat =
			mat4(vec4(0.299, 0.587, 0.114, 0.0),
					vec4(0.299, 0.587, 0.114, 0.0),
					vec4(0.299, 0.587, 0.114, 0.0),
					vec4(0.000, 0.000, 0.000, 1.0)) +
			mat4(vec4(0.701, -0.587, -0.114, 0.0),
					vec4(-0.299, 0.413, -0.114, 0.0),
					vec4(-0.300, -0.588, 0.886, 0.0),
					vec4(0.000, 0.000, 0.000, 0.0)) *
					hue_rot_c +
			mat4(vec4(0.168, 0.330, -0.497, 0.0),
					vec4(-0.328, 0.035, 0.292, 0.0),
					vec4(1.250, -1.050, -0.203, 0.0),
					vec4(0.000, 0.000, 0.000, 0.0)) *
					hue_rot_s;
	return hue_rot_mat * current_color;
}

float rand_from_seed(inout uint seed) {
	int k;
	int s = int(seed);
	if (s == 0) {
		s = 305420679;
	}
	k = s / 127773;
	s = 16807 * (s - k * 127773) - 2836 * k;
	if (s < 0) {
		s += 2147483647;
	}
	seed = uint(s);
	return float(seed % uint(65536)) / 65535.0;
}

float rand_from_seed_m1_p1(inout uint seed) {
	return rand_from_seed(seed) * 2.0 - 1.0;
}

uint hash(uint x) {
	x = ((x >> uint(16)) ^ x) * uint(73244475);
	x = ((x >> uint(16)) ^ x) * uint(73244475);
	x = (x >> uint(16)) ^ x;
	return x;
}

struct DisplayParameters {
	vec3 scale;
	float hue_rotation;
	float animation_speed;
	float animation_offset;
	float lifetime;
	vec4 color;
	float emission_texture_position;
};

struct DynamicsParameters {
	float angle;
	float angular_velocity;
	float initial_velocity_multiplier;
	float directional_velocity;
	float radial_velocity;
	float orbit_velocity;
	float turb_influence;
};

struct PhysicalParameters {
	float linear_accel;
	float radial_accel;
	float tangent_accel;
	float damping;
};

void calculate_initial_physical_params(inout PhysicalParameters params, inout uint alt_seed) {
	params.linear_accel = mix(linear_accel_min, linear_accel_max, rand_from_seed(alt_seed));
	params.radial_accel = mix(radial_accel_min, radial_accel_max, rand_from_seed(alt_seed));
	params.tangent_accel = mix(tangent_accel_min, tangent_accel_max, rand_from_seed(alt_seed));
	params.damping = mix(damping_min, damping_max, rand_from_seed(alt_seed));
}

void calculate_initial_dynamics_params(inout DynamicsParameters params, inout uint alt_seed) {
	// -------------------- DO NOT REORDER OPERATIONS, IT BREAKS VISUAL COMPATIBILITY
	// -------------------- ADD NEW OPERATIONS AT THE BOTTOM
	params.angle = mix(initial_angle_min, initial_angle_max, rand_from_seed(alt_seed));
	params.angular_velocity = mix(angular_velocity_min, angular_velocity_max, rand_from_seed(alt_seed));
	params.initial_velocity_multiplier = mix(initial_linear_velocity_min, initial_linear_velocity_max, rand_from_seed(alt_seed));
	params.directional_velocity = mix(directional_velocity_min, directional_velocity_max, rand_from_seed(alt_seed));
	params.radial_velocity = mix(radial_velocity_min, radial_velocity_max, rand_from_seed(alt_seed));
	params.orbit_velocity = mix(orbit_velocity_min, orbit_velocity_max, rand_from_seed(alt_seed));
	params.turb_influence = mix(turbulence_influence_min, turbulence_influence_max, rand_from_seed(alt_seed));
}

void calculate_initial_display_params(inout DisplayParameters params, inout uint alt_seed) {
	// -------------------- DO NOT REORDER OPERATIONS, IT BREAKS VISUAL COMPATIBILITY
	// -------------------- ADD NEW OPERATIONS AT THE BOTTOM
	float pi = 3.14159;
	params.scale = vec3(mix(scale_min, scale_max, rand_from_seed(alt_seed)));
	if (use_start_size_3d) {
		params.scale = vec3(
			mix(start_size_min.x, start_size_max.x, rand_from_seed(alt_seed)),
			mix(start_size_min.y, start_size_max.y, rand_from_seed(alt_seed)),
			mix(start_size_min.z, start_size_max.z, rand_from_seed(alt_seed))
		);
	}
	params.scale = sign(params.scale) * max(abs(params.scale), 0.001);
	params.hue_rotation = pi * 2.0 * mix(hue_variation_min, hue_variation_max, rand_from_seed(alt_seed));
	params.animation_speed = mix(anim_speed_min, anim_speed_max, rand_from_seed(alt_seed));
	params.animation_offset = mix(anim_offset_min, anim_offset_max, rand_from_seed(alt_seed));
	params.lifetime = (1.0 - lifetime_randomness * rand_from_seed(alt_seed));
	params.color = color_value;
	if (has_initial_ramp) {
	params.color *= texture(color_initial_ramp, vec2(rand_from_seed(alt_seed)));
	}
	if (emission_shape == EMISSION_SHAPE_POINTS || emission_shape == EMISSION_SHAPE_DIRECTED_POINTS) {
	params.emission_texture_position = rand_from_seed(alt_seed);
	}
	if (has_emission_color && (emission_shape == EMISSION_SHAPE_POINTS || emission_shape == EMISSION_SHAPE_DIRECTED_POINTS)) {
	int point = min(emission_texture_point_count - 1, int(params.emission_texture_position * float(emission_texture_point_count)));
	ivec2 emission_tex_size = textureSize(emission_texture_points, 0);
	ivec2 emission_tex_ofs = ivec2(point % emission_tex_size.x, point / emission_tex_size.x);
	params.color *= texelFetch(emission_texture_color, emission_tex_ofs, 0);
	}
}

void process_display_param(inout DisplayParameters parameters, float lifetime) {
	// Compile-time add textures.
	if (use_tex_param[PARAM_SCALE]) {
		parameters.scale *= texture(scale_curve, vec2(lifetime)).rgb;
	}
	if (use_tex_param[PARAM_HUE_VARIATION]) {
		parameters.hue_rotation *= texture(hue_rot_curve, vec2(lifetime)).r;
	}
	if (use_tex_param[PARAM_ANIM_OFFSET]) {
		parameters.animation_offset += texture(animation_offset_curve, vec2(lifetime)).r;
	}
	if (use_tex_param[PARAM_ANIM_SPEED]) {
		parameters.animation_speed *= texture(animation_speed_curve, vec2(lifetime)).r;
	}
	if (use_color_ramp) {
		parameters.color *= texture(color_ramp, vec2(lifetime));
	}
	if (use_alpha_curve) {
		parameters.color.a *= texture(alpha_curve, vec2(lifetime)).r;
	}
	parameters.color = rotate_hue(parameters.color, parameters.hue_rotation);
	if (use_emission_curve) {
		parameters.color.rgb *= 1.0 + texture(emission_curve, vec2(lifetime)).r;
	}
}

vec3 calculate_initial_position(inout DisplayParameters params, inout uint alt_seed) {
	float pi = 3.14159;
	vec3 pos = vec3(0.0);
	{ // Emission shape.
	if (emission_shape == EMISSION_SHAPE_POINT) {
	 	pos = vec3(0.0);
	}
	if (emission_shape == EMISSION_SHAPE_SPHERE) {
		float s = rand_from_seed(alt_seed) * 2.0 - 1.0;
		float t = rand_from_seed(alt_seed) * 2.0 * pi;
		float p = rand_from_seed(alt_seed);
		float radius = emission_sphere_radius * sqrt(1.0 - s * s);
		pos = mix(vec3(0.0, 0.0, 0.0), vec3(radius * cos(t), radius * sin(t), emission_sphere_radius * s), p);
	}
	if (emission_shape == EMISSION_SHAPE_SPHERE_SURFACE) {
		float s = rand_from_seed(alt_seed) * 2.0 - 1.0;
		float t = rand_from_seed(alt_seed) * 2.0 * pi;
		float radius = emission_sphere_radius * sqrt(1.0 - s * s);
		pos = vec3(radius * cos(t), radius * sin(t), emission_sphere_radius * s);
	}
	if (emission_shape == EMISSION_SHAPE_BOX) {
		pos = vec3(rand_from_seed(alt_seed) * 2.0 - 1.0, rand_from_seed(alt_seed) * 2.0 - 1.0, rand_from_seed(alt_seed) * 2.0 - 1.0) * emission_box_extents;
	}
	if (emission_shape == EMISSION_SHAPE_POINTS || emission_shape == EMISSION_SHAPE_DIRECTED_POINTS) {
		int point = min(emission_texture_point_count - 1, int(params.emission_texture_position * float(emission_texture_point_count)));
		ivec2 emission_tex_size = textureSize(emission_texture_points, 0);
		ivec2 emission_tex_ofs = ivec2(point % emission_tex_size.x, point / emission_tex_size.x);
		pos = texelFetch(emission_texture_points, emission_tex_ofs, 0).xyz;
	}
	if (emission_shape == EMISSION_SHAPE_RING) {
		float radius_clamped = max(0.001, emission_ring_radius);
		float top_radius = max(radius_clamped - tan(radians(90.0 - emission_ring_cone_angle)) * emission_ring_height, 0.0);
		float y_pos = rand_from_seed(alt_seed);
		float skew = max(min(radius_clamped, top_radius) / max(radius_clamped, top_radius), 0.5);
		y_pos = radius_clamped < top_radius ? pow(y_pos, skew) : 1.0 - pow(y_pos, skew);
		float ring_spawn_angle = rand_from_seed(alt_seed) * 2.0 * pi;
		float ring_random_radius = sqrt(rand_from_seed(alt_seed) * (radius_clamped * radius_clamped - emission_ring_inner_radius * emission_ring_inner_radius) + emission_ring_inner_radius * emission_ring_inner_radius);
		ring_random_radius = mix(ring_random_radius, ring_random_radius * (top_radius / radius_clamped), y_pos);
		vec3 axis = emission_ring_axis == vec3(0.0) ? vec3(0.0, 0.0, 1.0) : normalize(emission_ring_axis);
		vec3 ortho_axis = vec3(0.0);
		if (abs(axis) == vec3(1.0, 0.0, 0.0)) {
			ortho_axis = cross(axis, vec3(0.0, 1.0, 0.0));
		} else {
 			ortho_axis = cross(axis, vec3(1.0, 0.0, 0.0));
		}
		ortho_axis = normalize(ortho_axis);
		float s = sin(ring_spawn_angle);
		float c = cos(ring_spawn_angle);
		float oc = 1.0 - c;
		ortho_axis = mat3(
			vec3(c + axis.x * axis.x * oc, axis.x * axis.y * oc - axis.z * s, axis.x * axis.z *oc + axis.y * s),
			vec3(axis.x * axis.y * oc + s * axis.z, c + axis.y * axis.y * oc, axis.y * axis.z * oc - axis.x * s),
			vec3(axis.z * axis.x * oc - axis.y * s, axis.z * axis.y * oc + axis.x * s, c + axis.z * axis.z * oc)
			) * ortho_axis;
		ortho_axis = normalize(ortho_axis);
		pos = ortho_axis * ring_random_radius + (y_pos * emission_ring_height - emission_ring_height / 2.0) * axis;
	}
	}
	return pos * emission_shape_scale + emission_shape_offset;
}

vec3 process_orbit_displacement(DynamicsParameters param, float lifetime, inout uint alt_seed, mat4 transform, mat4 emission_transform, float delta, float total_lifetime) {
	if (abs(param.orbit_velocity) < 0.01 || delta < 0.001) {
		return vec3(0.0);
	}
	vec3 displacement = vec3(0.0);
	float pi = 3.14159;
	if (particle_flags[PARTICLE_FLAG_DISABLE_Z]) {
	float orbit_amount = param.orbit_velocity;
	if (use_tex_param[PARAM_ORBIT_VELOCITY]) {
	if (orbit_uses_curve_xyz) {
	orbit_amount *= texture(orbit_velocity_curve, vec2(lifetime)).r;
	} else {
	orbit_amount *= texture(orbit_velocity_curve, vec2(lifetime)).b;
	}
	}
	if (orbit_amount != 0.0) {
		vec3 pos = transform[3].xyz;
		vec3 org = emission_transform[3].xyz;
		vec3 diff = pos - org;
		float ang = orbit_amount * pi * 2.0 * delta;
		mat2 rot = mat2(vec2(cos(ang), -sin(ang)), vec2(sin(ang), cos(ang)));
		displacement.xy -= diff.xy;
		displacement.xy += rot * diff.xy;
	}
} else {
	vec3 orbit_velocities = vec3(param.orbit_velocity);
	orbit_velocities *= texture(orbit_velocity_curve, vec2(lifetime)).rgb;
	orbit_velocities *= pi * 2.0;
	orbit_velocities *= delta; // We wanna process those by the delta angle.

	// X axis.
	vec3 local_pos = (inverse(emission_transform) * transform[3]).xyz;
	local_pos -= velocity_pivot;
	local_pos.x = 0.0;
	mat3 x_rotation_mat = mat3(
		vec3(1.0, 0.0, 0.0),
		vec3(0.0, cos(orbit_velocities.x), sin(orbit_velocities.x)),
		vec3(0.0, -sin(orbit_velocities.x), cos(orbit_velocities.x))
	);
	vec3 new_pos = x_rotation_mat * local_pos;
	displacement = new_pos - local_pos;

	// Y axis.
	local_pos = (inverse(emission_transform) * transform[3]).xyz;
	local_pos -= velocity_pivot;
	local_pos.y = 0.0;
	mat3 y_rotation_mat = mat3(
		vec3(cos(orbit_velocities.y), 0.0, -sin(orbit_velocities.y)),
		vec3(0.0, 1.0, 0.0),
		vec3(sin(orbit_velocities.y), 0.0, cos(orbit_velocities.y))
	);
	new_pos = y_rotation_mat * local_pos;
	displacement += new_pos - local_pos;

	// Z axis.
	local_pos = (inverse(emission_transform) * transform[3]).xyz;
	local_pos -= velocity_pivot;
	local_pos.z = 0.0;
	mat3 z_rotation_mat = mat3(
		vec3(cos(orbit_velocities.z), sin(orbit_velocities.z), 0.0),
		vec3(-sin(orbit_velocities.z), cos(orbit_velocities.z), 0.0),
		vec3(0.0, 0.0, 1.0)
	);
	new_pos = z_rotation_mat * local_pos;
	displacement += new_pos - local_pos;

	}

	return (emission_transform * vec4(displacement / delta, 0.0)).xyz;
}

vec3 get_random_direction_from_spread(inout uint alt_seed, float spread_angle) {
	float pi = 3.14159;
	float degree_to_rad = pi / 180.0;
	float spread_rad = spread_angle * degree_to_rad;
	if(particle_flags[PARTICLE_FLAG_DISABLE_Z]) {
	float angle1_rad = rand_from_seed_m1_p1(alt_seed) * spread_rad;
	angle1_rad += direction.x != 0.0 ? atan(direction.y, direction.x) : sign(direction.y) * (pi / 2.0);
	vec3 spread_direction = vec3(cos(angle1_rad), sin(angle1_rad), 0.0);
	return spread_direction;
	} else {
	float angle1_rad = rand_from_seed_m1_p1(alt_seed) * spread_rad;
	float angle2_rad = rand_from_seed_m1_p1(alt_seed) * spread_rad * (1.0 - flatness);
	vec3 direction_xz = vec3(sin(angle1_rad), 0.0, cos(angle1_rad));
	vec3 direction_yz = vec3(0.0, sin(angle2_rad), cos(angle2_rad));
	direction_yz.z = direction_yz.z / max(0.0001, sqrt(abs(direction_yz.z))); // Better uniform distribution.
	vec3 spread_direction = vec3(direction_xz.x * direction_yz.z, direction_yz.y, direction_xz.z * direction_yz.z);
	vec3 direction_nrm = length(direction) > 0.0 ? normalize(direction) : vec3(0.0, 0.0, 1.0);
	// Rotate spread to direction.
	vec3 binormal = cross(vec3(0.0, 1.0, 0.0), direction_nrm);
	if (length(binormal) < 0.0001) {
		// Direction is parallel to Y. Choose Z as the binormal.
		binormal = vec3(0.0, 0.0, 1.0);
	}
	binormal = normalize(binormal);
	vec3 normal = cross(binormal, direction_nrm);
	spread_direction = binormal * spread_direction.x + normal * spread_direction.y + direction_nrm * spread_direction.z;
	return normalize(spread_direction);
	}
}

vec3 process_radial_displacement(DynamicsParameters param, float lifetime, inout uint alt_seed, mat4 transform, mat4 emission_transform, float delta) {
	vec3 radial_displacement = vec3(0.0);
	if (delta < 0.001) {
		return radial_displacement;
	}
	float radial_displacement_multiplier = 1.0;
	if (use_tex_param[PARAM_RADIAL_VELOCITY]) {
	radial_displacement_multiplier = texture(radial_velocity_curve, vec2(lifetime)).r;
	}

	vec3 global_pivot = (emission_transform * vec4(velocity_pivot, 1.0)).xyz;
	if (length(transform[3].xyz - global_pivot) > 0.01) {
		radial_displacement = normalize(transform[3].xyz - global_pivot) * radial_displacement_multiplier * param.radial_velocity;
	} else {
		radial_displacement = get_random_direction_from_spread(alt_seed, 360.0) * param.radial_velocity;
	}
	if (radial_displacement_multiplier * param.radial_velocity < 0.0) {
		// Prevent inwards velocity to flicker once the point is reached.
		radial_displacement = normalize(radial_displacement) * min(abs(radial_displacement_multiplier * param.radial_velocity), length(transform[3].xyz - global_pivot) / delta);
	}
	return radial_displacement;
}

vec3 process_directional_displacement(DynamicsParameters param, float lifetime_percent, mat4 transform, mat4 emission_transform) {
	vec3 displacement = texture(directional_velocity_curve, vec2(lifetime_percent)).xyz * param.directional_velocity;
	return displacement;
}

void process_physical_parameters(inout PhysicalParameters params, float lifetime_percent) {
	if(use_tex_param[PARAM_LINEAR_ACCEL]) {
	params.linear_accel *= texture(linear_accel_texture, vec2(lifetime_percent)).r;
	}
	if(use_tex_param[PARAM_RADIAL_ACCEL]) {
	params.radial_accel *= texture(radial_accel_texture, vec2(lifetime_percent)).r;
	}
	if(use_tex_param[PARAM_TANGENTIAL_ACCEL]) {
	params.tangent_accel *= texture(tangent_accel_texture, vec2(lifetime_percent)).r;
	}
	if(use_tex_param[PARAM_DAMPING]) {
	params.damping *= texture(damping_texture, vec2(lifetime_percent)).r;
	}
}

void start() {
	uint base_number = NUMBER;
	uint alt_seed = hash(base_number + uint(1) + RANDOM_SEED);
	DisplayParameters params;
	calculate_initial_display_params(params, alt_seed);
	// Reset alt seed?
	//alt_seed = hash(base_number + uint(1) + RANDOM_SEED);
	DynamicsParameters dynamic_params;
	calculate_initial_dynamics_params(dynamic_params, alt_seed);
	PhysicalParameters physics_params;
	calculate_initial_physical_params(physics_params, alt_seed);
	process_display_param(params, 0.0);
	if (rand_from_seed(alt_seed) > AMOUNT_RATIO) {
		ACTIVE = false;
	}

	if (RESTART_CUSTOM) {
		CUSTOM = vec4(0.0);
		CUSTOM.w = params.lifetime;
		CUSTOM.x = dynamic_params.angle;
	}
	if (RESTART_COLOR) {
		COLOR = params.color;
	}
	if (RESTART_ROT_SCALE) {
		TRANSFORM[0].xyz = vec3(1.0, 0.0, 0.0);
		TRANSFORM[1].xyz = vec3(0.0, 1.0, 0.0);
		TRANSFORM[2].xyz = vec3(0.0, 0.0, 1.0);
	}
	if (RESTART_POSITION) {
		TRANSFORM[3].xyz = calculate_initial_position(params, alt_seed);
		if (turbulence_enabled) {
			float initial_turbulence_displacement = mix(turbulence_initial_displacement_min, turbulence_initial_displacement_max, rand_from_seed(alt_seed));
			vec3 noise_direction = get_noise_direction(TRANSFORM[3].xyz);
			TRANSFORM[3].xyz += noise_direction * initial_turbulence_displacement;
		}
		TRANSFORM = EMISSION_TRANSFORM * TRANSFORM;
	}
	if (RESTART_VELOCITY) {
		VELOCITY = get_random_direction_from_spread(alt_seed, spread) * dynamic_params.initial_velocity_multiplier;
		if(emission_shape == EMISSION_SHAPE_DIRECTED_POINTS) {
		int point = min(emission_texture_point_count - 1, int(params.emission_texture_position * float(emission_texture_point_count)));
		ivec2 emission_tex_size = textureSize(emission_texture_points, 0);
		ivec2 emission_tex_ofs = ivec2(point % emission_tex_size.x, point / emission_tex_size.x);
		if (particle_flags[PARTICLE_FLAG_DISABLE_Z]) {
		{
			mat2 rotm;
			rotm[0] = texelFetch(emission_texture_normal, emission_tex_ofs, 0).xy;
			rotm[1] = rotm[0].yx * vec2(1.0, -1.0);
			VELOCITY.xy = rotm * VELOCITY.xy;
		}
		} else {
			vec3 normal = texelFetch(emission_texture_normal, emission_tex_ofs, 0).xyz;
			vec3 v0 = abs(normal.z) < 0.999 ? vec3(0.0, 0.0, 1.0) : vec3(0.0, 1.0, 0.0);
			vec3 tangent = normalize(cross(v0, normal));
			vec3 bitangent = normalize(cross(tangent, normal));
			VELOCITY = mat3(tangent, bitangent, normal) * VELOCITY;
		}
	}
	}

	process_display_param(params, 0.0);

	VELOCITY = (EMISSION_TRANSFORM * vec4(VELOCITY, 0.0)).xyz;
	VELOCITY += EMITTER_VELOCITY * inherit_emitter_velocity_ratio;
	if (particle_flags[PARTICLE_FLAG_DISABLE_Z]) {
		VELOCITY.z = 0.0;
		TRANSFORM[3].z = 0.0;
	}
}

void process() {
	uint base_number = NUMBER;
	//if (repeatable) {
	//	base_number = INDEX;
	//}
	uint alt_seed = hash(base_number + uint(1) + RANDOM_SEED);
	DisplayParameters params;
	calculate_initial_display_params(params, alt_seed);
	DynamicsParameters dynamic_params;
	calculate_initial_dynamics_params(dynamic_params, alt_seed);
	PhysicalParameters physics_params;
	calculate_initial_physical_params(physics_params, alt_seed);

	float pi = 3.14159;
	float degree_to_rad = pi / 180.0;

	bool just_spawned = CUSTOM.y == 0.0;
	CUSTOM.y += DELTA / LIFETIME;
	CUSTOM.y = mix(CUSTOM.y, 1.0, INTERPOLATE_TO_END);
	float lifetime_percent = CUSTOM.y / params.lifetime;
	if (CUSTOM.y > CUSTOM.w) {
		ACTIVE = false;
	}

	// Calculate all velocity.
	vec3 controlled_displacement = vec3(0.0);
	if(use_tex_param[PARAM_ORBIT_VELOCITY] || particle_flags[PARTICLE_FLAG_DISABLE_Z]) {
		controlled_displacement += process_orbit_displacement(dynamic_params, lifetime_percent, alt_seed, TRANSFORM, EMISSION_TRANSFORM, DELTA, params.lifetime * LIFETIME);
	}
	controlled_displacement += process_radial_displacement(dynamic_params, lifetime_percent, alt_seed, TRANSFORM, EMISSION_TRANSFORM, DELTA);
	if(use_tex_param[PARAM_DIRECTIONAL_VELOCITY]) {
		controlled_displacement += process_directional_displacement(dynamic_params, lifetime_percent, TRANSFORM, EMISSION_TRANSFORM);
	}

	process_physical_parameters(physics_params, lifetime_percent);
	vec3 force;
	{
		// Copied from previous version.
		vec3 pos = TRANSFORM[3].xyz;
		force = gravity;
		// Apply linear acceleration.
		force += length(VELOCITY) > 0.0 ? normalize(VELOCITY) * physics_params.linear_accel : vec3(0.0);
		// Apply radial acceleration.
		vec3 org = EMISSION_TRANSFORM[3].xyz;
		vec3 diff = pos - org;
		force += length(diff) > 0.0 ? normalize(diff) * physics_params.radial_accel : vec3(0.0);
		// Apply tangential acceleration.
		float tangent_accel_val = physics_params.tangent_accel;
		if(particle_flags[PARTICLE_FLAG_DISABLE_Z]) {
			force += length(diff.yx) > 0.0 ? vec3(normalize(diff.yx * vec2(-1.0, 1.0)), 0.0) * tangent_accel_val : vec3(0.0);
		} else {
			vec3 crossDiff = cross(normalize(diff), normalize(gravity));
			force += length(crossDiff) > 0.0 ? normalize(crossDiff) * tangent_accel_val : vec3(0.0);
		}
		if(attractor_enabled) {
			force += ATTRACTOR_FORCE;
		}
		if(particle_flags[PARTICLE_FLAG_DISABLE_Z]) {
			force.z = 0.0;
		}
		// Apply attractor forces.
		VELOCITY += force * DELTA;
	}
	{
		// Copied from previous version.
		if (physics_params.damping > 0.0) {
			float v = length(VELOCITY);
			if(!particle_flags[PARTICLE_FLAG_DAMPING_AS_FRICTION]) {
				v -= physics_params.damping * DELTA;
			} else {
				if (v > 0.001) {
					// Realistic friction formula. We assume the mass of a particle to be 0.05 kg.
					float damp = v * v * physics_params.damping * 0.05 * DELTA;
					v -= damp;
				}
			}
			if (v < 0.0) {
				VELOCITY = vec3(0.0);
			} else {
				VELOCITY = normalize(VELOCITY) * v;
			}
		}
	}

	// Turbulence before limiting.
	if (turbulence_enabled) {
		float turbulence_influence = 1.0;
		if (use_tex_param[PARAM_TURB_INFLUENCE_OVER_LIFE]) {
			turbulence_influence = textureLod(turbulence_influence_over_life, vec2(lifetime_percent, 0.0), 0.0).r;
		}

		vec3 noise_direction = get_noise_direction(TRANSFORM[3].xyz);
	{
			float vel_mag = length(VELOCITY);
			float vel_infl = clamp(dynamic_params.turb_influence * turbulence_influence, 0.0, 1.0) * (DELTA <= 0.0 ? 0.0 : 1.0);
			VELOCITY = mix(VELOCITY, normalize(noise_direction) * vel_mag * (1.0 + (1.0 - vel_infl) * 0.2), vel_infl);
			vel_mag = length(controlled_displacement);
			controlled_displacement = mix(controlled_displacement, normalize(noise_direction) * vel_mag * (1.0 + (1.0 - vel_infl) * 0.2), vel_infl);
		}
	}

	vec3 final_velocity = controlled_displacement + VELOCITY;

	if(use_limiter_curve) {
		// Limit velocity.
		if (length(final_velocity) > 0.001) {
			final_velocity = normalize(final_velocity) * min(abs(length(final_velocity)), abs(texture(velocity_limit_curve, vec2(lifetime_percent)).r));
		}

	}
	if(particle_flags[PARTICLE_FLAG_DISABLE_Z]) {
		final_velocity.z = 0.0;

	}
	TRANSFORM[3].xyz += final_velocity * DELTA;

	process_display_param(params, lifetime_percent);

	float base_angle = dynamic_params.angle;
	if(use_tex_param[PARAM_ANGLE]) {
		base_angle *= texture(angle_texture, vec2(lifetime_percent)).r;
	}
	if(use_tex_param[PARAM_ANGULAR_VELOCITY]) {
		base_angle += CUSTOM.y * LIFETIME * dynamic_params.angular_velocity * texture(angular_velocity_texture, vec2(lifetime_percent)).r;
	} else {
		base_angle += CUSTOM.y * LIFETIME * dynamic_params.angular_velocity;
	}
	CUSTOM.x = base_angle * degree_to_rad;
	COLOR = params.color;

	if (particle_flags[PARTICLE_FLAG_DISABLE_Z]) {
		if (particle_flags[PARTICLE_FLAG_ALIGN_Y_TO_VELOCITY]) {
			if (length(final_velocity) > 0.0) {
				TRANSFORM[1].xyz = normalize(final_velocity);
			} else {
				TRANSFORM[1].xyz = normalize(TRANSFORM[1].xyz);
			}
			TRANSFORM[0].xyz = normalize(cross(TRANSFORM[1].xyz, TRANSFORM[2].xyz));
			TRANSFORM[2] = vec4(0.0, 0.0, 1.0, 0.0);
		} else {
			TRANSFORM[0] = vec4(cos(CUSTOM.x), -sin(CUSTOM.x), 0.0, 0.0);
			TRANSFORM[1] = vec4(sin(CUSTOM.x), cos(CUSTOM.x), 0.0, 0.0);
			TRANSFORM[2] = vec4(0.0, 0.0, 1.0, 0.0);
		}
	} else {
		if (particle_flags[PARTICLE_FLAG_ALIGN_Y_TO_VELOCITY]) {
			if (length(final_velocity) > 0.0) {
				TRANSFORM[1].xyz = normalize(final_velocity);
			} else {
				TRANSFORM[1].xyz = normalize(TRANSFORM[1].xyz);
			}
			if (TRANSFORM[1].xyz == normalize(TRANSFORM[0].xyz)) {
				TRANSFORM[0].xyz = normalize(cross(normalize(TRANSFORM[1].xyz), normalize(TRANSFORM[2].xyz)));
				TRANSFORM[2].xyz = normalize(cross(normalize(TRANSFORM[0].xyz), normalize(TRANSFORM[1].xyz)));
			} else {
				TRANSFORM[2].xyz = normalize(cross(normalize(TRANSFORM[0].xyz), normalize(TRANSFORM[1].xyz)));
				TRANSFORM[0].xyz = normalize(cross(normalize(TRANSFORM[1].xyz), normalize(TRANSFORM[2].xyz)));
			}
		} else {
			TRANSFORM[0].xyz = normalize(TRANSFORM[0].xyz);
			TRANSFORM[1].xyz = normalize(TRANSFORM[1].xyz);
			TRANSFORM[2].xyz = normalize(TRANSFORM[2].xyz);
		}
		if (particle_flags[PARTICLE_FLAG_ROTATE_Y]) {
			vec4 origin = TRANSFORM[3];
			TRANSFORM = mat4(vec4(cos(CUSTOM.x), 0.0, -sin(CUSTOM.x), 0.0), vec4(0.0, 1.0, 0.0, 0.0), vec4(sin(CUSTOM.x), 0.0, cos(CUSTOM.x), 0.0), vec4(0.0, 0.0, 0.0, 1.0));
			TRANSFORM[3] = origin;
		}
	}
	if (particle_flags[PARTICLE_FLAG_DISABLE_Z]) {
		TRANSFORM[3].z = 0.0;

	}
	if (use_tex_param[PARAM_SCALE_OVER_VELOCITY]) {
		if (length(final_velocity) > 0.001) {
			params.scale *= texture(scale_over_velocity_curve, vec2(clamp(length(final_velocity) / (scale_over_velocity_max - scale_over_velocity_min), 0.0, 1.0), 0.0)).rgb;
		} else {
			params.scale *= texture(scale_over_velocity_curve, vec2(0.0)).rgb;
		}
	}
	float scale_sign_x = params.scale.x < 0.0 ? -1.0 : 1.0;
	float scale_sign_y = params.scale.y < 0.0 ? -1.0 : 1.0;
	float scale_sign_z = params.scale.z < 0.0 ? -1.0 : 1.0;
	float scale_minimum = 0.001;
	TRANSFORM[0].xyz *= scale_sign_x * max(abs(params.scale.x), scale_minimum);
	TRANSFORM[1].xyz *= scale_sign_y * max(abs(params.scale.y), scale_minimum);
	TRANSFORM[2].xyz *= scale_sign_z * max(abs(params.scale.z), scale_minimum);

	CUSTOM.z = params.animation_offset + lifetime_percent * params.animation_speed;

	if (sub_emitter_mode != SUB_EMITTER_DISABLED) {
		int emit_count = 0;
		if (sub_emitter_mode == SUB_EMITTER_CONSTANT) {
			float interval_from = CUSTOM.y * LIFETIME - DELTA;
			float interval_rem = sub_emitter_frequency - mod(interval_from, sub_emitter_frequency);
			if (DELTA >= interval_rem) {
				emit_count = 1;
			}
		}
		if (sub_emitter_mode == SUB_EMITTER_AT_END) {
			if ((CUSTOM.y / CUSTOM.w * LIFETIME) > (LIFETIME - DELTA)) {
				emit_count = sub_emitter_amount_at_end;
			}
		}
		if (sub_emitter_mode == SUB_EMITTER_AT_START) {
			if (just_spawned) {
				emit_count = sub_emitter_amount_at_start;
			}
		}
		for (int i = 0; i < emit_count; i++) {
			uint flags = FLAG_EMIT_POSITION | FLAG_EMIT_ROT_SCALE;
			if (sub_emitter_keep_velocity) {
				flags |= FLAG_EMIT_VELOCITY;
			}
			emit_subparticle(TRANSFORM, VELOCITY, vec4(0.0), vec4(0.0), flags);
		}

	}
	if (CUSTOM.y > CUSTOM.w) {
		ACTIVE = false;
	}
}
