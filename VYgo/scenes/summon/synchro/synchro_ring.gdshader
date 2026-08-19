shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never;

uniform vec4 _TintColor : source_color = vec4(1.0);
uniform vec4 _AddColor : source_color = vec4(0.0);
uniform float _FakeBlend = 1.0;

void fragment() {
    // 圆环颜色在此锁定，防止多层加算及 Viewport 色调映射将青色冲成白色。
    vec3 synchro_cyan = vec3(0.015, 0.78, 0.72);
    ALBEDO = synchro_cyan;
    EMISSION = synchro_cyan * 0.08;
    ALPHA = _TintColor.a * clamp(_FakeBlend, 0.0, 1.0);
}
