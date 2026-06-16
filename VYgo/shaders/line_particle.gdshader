shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_always, blend_add;

uniform sampler2D albedo_tex : source_color;
uniform vec4 tint : source_color = vec4(1.0, 1.0, 1.0, 0.5);

void fragment() {
    vec4 tex = texture(albedo_tex, UV);
    ALBEDO = tex.rgb * tint.rgb;
    ALPHA = tex.a * tint.a * COLOR.a;
}
