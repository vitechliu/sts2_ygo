shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_always, blend_add;

uniform sampler2D albedo_tex : source_color, filter_linear_mipmap;
uniform vec2 uv_tiling = vec2(1.96, 1.0);
uniform vec2 uv_offset = vec2(-1.95, 0.0);

void fragment() {
    vec2 uv = UV * uv_tiling + uv_offset;
    
    // 1. 头尾（X轴）硬裁剪：控制光波的出现和消失
    float fade_x = step(0.0, uv.x) * step(uv.x, 1.0);
    
    // 2. 核心修复：两侧（Y轴）边缘软虚化
    // 定义一个极小的边缘宽度（例如 0.03），让靠近 Mesh 左右边缘的像素产生极其微弱的透明渐变
    float margin_y = 0.03; 
    float fade_y = smoothstep(0.0, margin_y, uv.y) * smoothstep(1.0, 1.0 - margin_y, uv.y);
    
    // 3. 正常采样纹理
    vec4 tex = texture(albedo_tex, uv);
    
    ALBEDO = tex.rgb;
    // 4. 将头尾裁剪和两侧软裁剪叠加上去
    ALPHA = tex.a * COLOR.a * fade_x * fade_y;
}
