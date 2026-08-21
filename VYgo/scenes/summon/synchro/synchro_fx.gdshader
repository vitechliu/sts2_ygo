shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never;

uniform sampler2D _MainTex : source_color;
uniform vec4 _TintColor : source_color = vec4(1.0);
uniform vec4 _AddColor : source_color = vec4(0.0);
uniform float _Amplitude = 0.0;
uniform float _offset = 0.0;
uniform float _FakeBlend = 1.0;
uniform vec4 _MainTex_ST = vec4(1.0, 1.0, 0.0, 0.0);
uniform float _RING_Radial : hint_range(0.0, 1.0) = 1.0;

void fragment() {
    vec2 uv = UV * _MainTex_ST.xy + _MainTex_ST.zw;
    uv.x += _offset;
    uv.y += sin((uv.x + TIME) * 12.0) * _Amplitude * 0.015;
    vec4 sample_color = texture(_MainTex, uv);
    float radial = distance(UV, vec2(0.5)) * 2.0;
    float radial_mask = 1.0 - smoothstep(_RING_Radial, _RING_Radial + 0.08, radial);
    vec4 color = sample_color * _TintColor + _AddColor;
    ALBEDO = color.rgb;
    EMISSION = color.rgb * (1.0 + _Amplitude);
    ALPHA = color.a * radial_mask * clamp(_FakeBlend, 0.0, 1.0);
}
