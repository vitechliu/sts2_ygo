shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, shadows_disabled;

// 黑洞需要覆盖背景颜色；加法混合会使黑紫中心完全消失。
uniform sampler2D main_tex : source_color, filter_linear, repeat_enable;
uniform sampler2D mask_tex : filter_linear, repeat_disable;
uniform vec4 tint : source_color = vec4(0.462264,0.462264,0.462264,1.0);
uniform vec4 particle_color : source_color = vec4(1.0);
void fragment() {
    vec4 stars = texture(main_tex, UV);
    float mask = texture(mask_tex, UV).r;
    ALBEDO = stars.rgb * tint.rgb * vec3(1.15,0.6,1.8) + vec3(0.014,0.01,0.04)*mask;
    ALPHA = pow(mask,0.85) * tint.a * COLOR.a * particle_color.a;
}
