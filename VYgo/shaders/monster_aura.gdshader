shader_type canvas_item;
render_mode unshaded, blend_add;

uniform vec4 aura_color : source_color = vec4(0.6, 0.95, 0.72, 1.0);
uniform float phase = 0.0;
uniform float brightness = 0.1;
uniform float pulse = 0.0;

// 使用径向距离在当前屏幕像素中的变化量过滤线条，避免缩小时落成断续亮点。
float antialiased_ring(float distance_to_ring, float half_width, float min_pixel_width, float pixel_width) {
    float width = max(half_width, pixel_width * min_pixel_width * 0.5);
    float aa = pixel_width * 0.5;
    return 1.0 - smoothstep(width - aa, width + aa, distance_to_ring);
}

void fragment() {
    // 在圆形 UV 空间旋转纹样，节点只负责压成脚下椭圆，避免整张椭圆翻转。
    vec2 p = (UV - vec2(0.5)) * 2.0;
    float radius = length(p);
    float angle = atan(p.y, p.x);
    float ring_distance = abs(radius - 0.72);
    float inner_distance = abs(radius - 0.56);
    float radial_pixel = max(fwidth(radius), 0.00001);
    // 主环约为原先半高宽的 1.35 倍，小尺寸时至少保留约 1.1 像素宽度。
    float ring = antialiased_ring(ring_distance, 0.014, 1.1, radial_pixel);
    float inner_ring = antialiased_ring(inner_distance, 0.012, 0.8, radial_pixel) * 0.36;
    float spoke = cos((angle - phase) * 12.0);
    float spoke_aa = max(fwidth(spoke) * 0.5, 0.025);
    float ticks = smoothstep(0.68 - spoke_aa, 0.68 + spoke_aa, spoke);
    ticks *= smoothstep(0.60 - radial_pixel * 0.5, 0.60 + radial_pixel * 0.5, radius)
        * (1.0 - smoothstep(0.69 - radial_pixel * 0.5, 0.69 + radial_pixel * 0.5, radius));
    // 主环短轴不足 30 屏幕像素时逐渐减少刻线，14 像素以下只保留连续环与柔光。
    float short_axis_pixels = 1.44 / max(max(length(dFdx(p)), length(dFdy(p))), 0.00001);
    ticks *= smoothstep(14.0, 30.0, short_axis_pixels);
    float moving_arc = pow(max(0.0, cos(angle + phase * 2.0)), 12.0);
    float halo = exp(-ring_distance * ring_distance * 150.0) * 0.18;
    float floor_glow = exp(-radius * radius * 5.0) * 0.035;
    float flare = exp(-pow(radius - (0.58 + (1.0 - pulse) * 0.22), 2.0) * 800.0) * pulse;
    float intensity = (ring * (0.6 + moving_arc * 0.7) + inner_ring + ticks * 0.7 + halo + floor_glow) * brightness;
    intensity += flare * 0.75;
    float edge = 1.0 - smoothstep(0.88, 1.0, radius);
    COLOR = vec4(aura_color.rgb, intensity * edge * aura_color.a) * COLOR;
}
