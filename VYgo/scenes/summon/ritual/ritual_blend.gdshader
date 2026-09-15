shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, shadows_disabled;
uniform sampler2D main_tex : filter_linear, repeat_enable;
uniform vec4 tint = vec4(0.5);
uniform vec4 particle_color = vec4(1.0);
uniform vec4 color_start = vec4(1.0,0.28,0.0,1.0);
uniform vec4 color_end = vec4(0.98,0.0,0.9,1.0);
uniform vec2 uv_scale = vec2(1.0);
uniform float clock = 0.0;
uniform float tiling_x = 1.0;
uniform float tiling_y = 1.0;
uniform float offset_x = 0.0;
uniform float offset_y = 0.0;
uniform float material_alpha = 1.0;
uniform float gain = 1.0;
uniform float flow_speed = 2.25;
uniform int mode = 0;
varying flat vec3 sheet_data;
void vertex() { sheet_data=INSTANCE_CUSTOM.rgb; }
vec3 gamma_to_linear(vec3 c) {
    return mix(c / 12.92, pow((max(c,vec3(0.0)) + 0.055) / 1.055, vec3(2.4)), step(vec3(0.04045),c));
}
void fragment() {
    vec2 uv=UV*uv_scale*vec2(tiling_x,tiling_y)+vec2(offset_x,-offset_y);
    vec2 tiles=max(sheet_data.yz,vec2(1.0));
    uv=(uv+vec2(mod(sheet_data.x,tiles.x),floor(sheet_data.x/tiles.x)))/tiles;
    vec4 tex=texture(main_tex,uv);
    vec4 col=2.0*tex*tint*COLOR*particle_color;
    col.rgb*=gain;
    // 火焰/光轨的源 shader 是占位：利用原 RGB 噪声通道重建流动及冷热颜色。
    if(mode==1) {
        // 源局部 X 经 Prefab 的 Z=90° 转为竖直，pivot.x=0.51 让 UV.x=0 落在火焰根部。
        float h=UV.x / 0.9;
        vec4 noise=texture(main_tex,vec2(UV.y*1.7,h*1.4-clock*flow_speed*0.3));
        float width=mix(0.38,0.20,h)*(0.65+noise.b*1.1);
        float across=UV.y-0.5+(noise.g-0.5)*0.22*h;
        float body=exp(-2.0*pow(across/width,2.0));
        float vertical=smoothstep(0.0,0.22,h)*(1.0-smoothstep(0.62,1.0,h+noise.g*0.15));
        float shape=pow(body,1.3)*(0.45+noise.r*0.85)*vertical;
        float halo=exp(-0.8*pow(across/width,2.0))*vertical*0.12;
        shape=clamp(shape+halo,0.0,1.0);
        vec3 fire=mix(color_start.rgb,color_end.rgb,smoothstep(0.10,0.62,h));
        float core=exp(-5.0*pow(across/width,2.0))*exp(-pow((h-0.24)/0.2,2.0));
        fire=mix(fire,vec3(1.0,0.9,0.78),core*0.32);
        col=vec4(fire*sqrt(gain),shape)*COLOR*particle_color;
    } else if(mode==3) {
        // 材质UV偏移推进一道短光轨；域外为透明，禁止重复平铺变成整环。
        vec4 noise=texture(main_tex,vec2(uv.x,uv.y+clock*flow_speed*0.05));
        float window=step(0.0,uv.x)*step(uv.x,1.0);
        float edge=smoothstep(0.0,0.12,UV.y)*smoothstep(1.0,0.88,UV.y);
        float head=smoothstep(0.0,0.12,uv.x)*smoothstep(1.0,0.35,uv.x);
        col=vec4(mix(color_start.rgb,color_end.rgb,uv.x)*gain,noise.r*window*edge*head)*COLOR*particle_color;
    } else if(mode==2) {
        tex=texture(main_tex,uv+vec2(clock*0.1,-clock*0.7));
        col=2.0*tex*tint*COLOR*particle_color;
        col.a*=smoothstep(0.0,0.08,UV.y)*smoothstep(1.0,0.5,UV.y);
    }
    // 加色纹理的黑色必须在透明舞台中保留零覆盖率；同时保持原 RGB×alpha 的发光能量。
    float coverage=1.0;
    ALBEDO=gamma_to_linear(col.rgb)/max(coverage,0.0001);
    ALPHA=clamp(col.a*material_alpha*coverage,0.0,1.0);
}
