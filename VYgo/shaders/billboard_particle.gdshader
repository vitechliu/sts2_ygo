shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_always, blend_add;

uniform sampler2D albedo_tex : source_color;
uniform vec4 tint : source_color = vec4(0.7, 0.7, 0.7, 0.5);

void fragment() {
    vec4 tex = texture(albedo_tex, UV);
    vec4 col = 2.0 * tint * tex;
    ALBEDO = col.rgb;
    ALPHA = col.a * COLOR.a;
}
