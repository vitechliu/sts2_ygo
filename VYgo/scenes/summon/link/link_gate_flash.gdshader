shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never, blend_add, shadows_disabled;
uniform sampler2D main_tex : source_color, filter_linear, repeat_disable;
void fragment() {
    vec4 tex = texture(main_tex, UV);
    // 只增强源纹理中的光区，门框暗部不参与白色蓄光。
    float light_mask = smoothstep(0.45, 0.9, max(tex.r, max(tex.g, tex.b)));
    ALBEDO = tex.rgb * vec3(1.5, 1.7, 1.8);
    ALPHA = tex.a * COLOR.a * light_mask;
}
