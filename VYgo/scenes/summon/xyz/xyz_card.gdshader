shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_prepass_alpha, shadows_disabled;
uniform sampler2D card_texture : source_color, filter_linear, repeat_disable;
uniform float opacity = 1.0;
vec4 sample_card(vec2 uv) {
    float bounds = step(0.0,uv.x)*step(uv.x,1.0)*step(0.0,uv.y)*step(uv.y,1.0);
    return texture(card_texture, clamp(uv,vec2(0.0),vec2(1.0))) * bounds;
}
void fragment() {
    vec2 uv = (UV - 0.5) * 1.12 + 0.5;
    vec4 card = sample_card(uv);
    vec2 pixel = 1.0 / vec2(textureSize(card_texture, 0));
    // 同心、像素等距的高斯采样：细亮芯外接连续衰减，避免 max 膨胀产生硬矩形。
    float soft = 0.0;
    float core = 0.0;
    float weight_sum = 0.0;
    for (int x = -3; x <= 3; x++) {
        for (int y = -3; y <= 3; y++) {
            vec2 offset = vec2(float(x), float(y));
            float weight = exp(-dot(offset, offset) / 4.5);
            soft += sample_card(uv + offset * pixel * 3.0).a * weight;
            core += sample_card(uv + offset * pixel * 0.8).a * weight;
            weight_sum += weight;
        }
    }
    float halo = clamp((soft * 1.15 + core * 1.8) / weight_sum, 0.0, 1.0) * (1.0 - card.a);
    float alpha = card.a + halo;
    // 捕获纹理是预乘透明度；组合后转回 blend_mix 所需的直通颜色，仅轮廓发光。
    ALBEDO = (card.rgb + vec3(2.4, 1.65, 0.12) * halo) / max(alpha, 0.0001);
    ALPHA = alpha * opacity;
}
