shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never, blend_add, shadows_disabled;
uniform sampler2D main_tex : source_color, filter_linear, repeat_disable;
uniform float opacity = 1.0;
uniform float gain = 1.5;
void fragment() {
    vec4 tex = texture(main_tex, UV);
    ALBEDO = tex.rgb * gain;
    ALPHA = tex.a * opacity;
}
