shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, shadows_disabled;
uniform sampler2D card_texture : source_color, filter_linear, repeat_disable;
uniform vec4 tint : source_color = vec4(1.0);
void fragment() {
    vec2 uv=(UV-0.5)*1.12+0.5;
    float bounds=step(0.0,uv.x)*step(uv.x,1.0)*step(0.0,uv.y)*step(uv.y,1.0);
    vec4 card=texture(card_texture,clamp(uv,vec2(0.0),vec2(1.0)));
    ALBEDO=2.0*card.rgb*tint.rgb/max(card.a,0.0001);
    ALPHA=card.a*tint.a*bounds;
}
