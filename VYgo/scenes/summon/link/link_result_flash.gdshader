shader_type canvas_item;
render_mode blend_add;
void fragment() {
    // 只使用实时卡面轮廓，爆发不显示固定卡图或偏移的桌面投影。
    COLOR = vec4(vec3(0.65,0.9,1.0)*1.8, texture(TEXTURE,UV).a*COLOR.a);
}
