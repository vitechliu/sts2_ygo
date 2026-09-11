shader_type spatial;
render_mode unshaded, blend_premul_alpha, cull_disabled, depth_draw_never, shadows_disabled;

// 源 XYZ shader 已是 DummyShaderTextExporter；以下按原纹理/参数与录屏重建。
uniform sampler2D main_tex : source_color, filter_linear, repeat_enable;
uniform sampler2D mask_tex : filter_linear, repeat_enable;
uniform vec4 tint : source_color = vec4(1.0);
uniform vec4 particle_color : source_color = vec4(1.0);
uniform int mode = 0;
uniform float gain = 1.0;
uniform float alpha_gain = 1.0;
uniform bool opaque_core = false;
uniform bool spot_white_core = false;
uniform float arc_angle = 2.8;
uniform float spot_power = 3.0;
uniform float spot_blur = 0.72;
uniform float trail_tiling = 3.0;
uniform float trail_offset = -2.89;
uniform float position_offset = 0.3;
uniform float mask_scale_x = 0.5;
uniform float mask_scale_y = 1.03;
uniform float mask_offset_x = -0.5;
uniform float mask_offset_y = 0.03;

void fragment() {
    vec2 uv = UV;
    vec4 tex = texture(main_tex, uv);
    vec3 color = tint.rgb * particle_color.rgb * COLOR.rgb;
    float alpha = tint.a * particle_color.a * COLOR.a;
    if (mode == 1) {
        // 三维螺旋幕布沿网格 UV 滚动遮罩。
        tex = texture(main_tex, vec2(uv.x * 5.0, uv.y));
        // 保留起始扫入位置；收束阶段仍保留螺旋尾部，避免耀斑出现前整层消失。
        vec2 mask_uv = uv * vec2(mask_scale_x, mask_scale_y) + vec2(max(mask_offset_x,-0.2), mask_offset_y);
        float mask = texture(mask_tex, mask_uv).r;
        color *= tex.rgb * 8.4;
        alpha *= mask * tex.a * gain;
    } else if (mode == 2) {
        // XYZInMesh01 本身带高度变化，使用原 UV 曲线扫过卷曲网格。
        vec2 trail_uv = vec2(uv.x * trail_tiling + trail_offset, uv.y);
        tex = texture(main_tex, clamp(trail_uv, vec2(0.0), vec2(1.0)));
        float inside = step(0.0, trail_uv.x) * step(trail_uv.x, 1.0);
        color = mix(vec3(5.992157,5.06537,1.129412), vec3(0.986529,0.779313,0.0), uv.y) * tex.rgb * 0.35;
        float band = exp(-pow((uv.y-position_offset)/0.12,2.0));
        alpha *= tex.a * inside * band;
    } else if (mode == 3) {
        tex = texture(main_tex, vec2(uv.x * 4.0, uv.y));
        // 保留竖向光丝，避免所有通道同时饱和后变成灰白的实心锥面。
        color = vec3(0.15,3.8,0.2) * tex.rgb;
        alpha *= tex.a * smoothstep(0.0,0.18,uv.y);
    } else if (mode == 4) {
        // 源 ParticleAdd 使用云纹整体叠加，不能改成仅边缘发光的空心罩。
        color *= tex.rgb * 2.0;
        alpha *= tex.a * 2.0;
    } else if (mode == 5) {
        float radial = max(0.0,1.0-length((uv-0.5)*2.0));
        alpha *= pow(radial, max(2.0,spot_power+spot_blur*3.0)) * gain;
        // 光柱底部为青白亮核与金色外晕，不能扩大成整块柔黄光团。
        if (spot_white_core) color = mix(color,vec3(1.5,3.0,3.5),pow(radial,4.0));
    } else if (mode == 6) {
        // 单条外侧金色弧带，仅在结果阶段沿球壳外围流动。
        float angle = atan(uv.y-0.5,uv.x-0.5);
        float distance_angle = abs(mod(angle-arc_angle+3.14159265,6.2831853)-3.14159265);
        alpha *= tex.a*(1.0-smoothstep(0.65,1.2,distance_angle));
        color = vec3(3.0,2.6,1.1)*tex.rgb;
    } else {
        color *= tex.rgb * gain;
        alpha *= tex.a * alpha_gain;
    }
    // 普通光效只累加颜色；仅中心耀斑的白核需要遮住背景，蓝晕保持透明。
    ALBEDO = color * clamp(alpha, 0.0, 1.0);
    ALPHA = opaque_core ? smoothstep(0.65, 0.95, min(tex.r,min(tex.g,tex.b))) * clamp(alpha*2.0,0.0,1.0) : 0.0;
}
