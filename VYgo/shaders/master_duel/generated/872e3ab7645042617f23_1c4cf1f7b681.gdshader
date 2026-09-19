shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_disabled;
// 按源 DXBC 前向程序逐指令生成；源文件和散列见迁移清单。
uniform float md_time = 0.0;
uniform float md_near = 0.1;
uniform float md_far = 300.0;
uniform float md_camera_size = 80.0;
const mat4 md_flip = mat4(vec4(1,0,0,0),vec4(0,1,0,0),vec4(0,0,-1,0),vec4(0,0,0,1));
uniform sampler2D p_MainTex : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 p_MainTex_ST;
uniform vec4 p_TintColor;
varying vec4 md_vary_1;
varying vec4 md_vary_2;
// platform4_block0_program0.asm.txt
void vertex() {
mat4 md_model=md_flip*MODEL_MATRIX*md_flip;
mat4 md_inverse_model=inverse(md_model);
mat4 md_vp=PROJECTION_MATRIX*VIEW_MATRIX*md_flip;
mat4 md_inverse_vp=inverse(md_vp);
mat4 md_view=VIEW_MATRIX*md_flip;
vec4 o0=vec4(0.0);
vec4 o1=vec4(0.0);
vec4 o2=vec4(0.0);
vec4 r0=vec4(0.0);
vec4 r1=vec4(0.0);
vec4 v0=vec4(0.0);
vec4 v1=vec4(0.0);
vec4 v2=vec4(0.0);
v0=vec4(VERTEX*vec3(1,1,-1),1.0);
v1=COLOR;
v2=vec4(UV,UV2);
r0.xyzw = (((v0).yyyy*(vec4(md_model[1].x,md_model[1].y,md_model[1].z,md_model[1].w)).xyzw)).xyzw;
r0.xyzw = (((vec4(md_model[0].x,md_model[0].y,md_model[0].z,md_model[0].w)).xyzw*(v0).xxxx+(r0).xyzw)).xyzw;
r0.xyzw = (((vec4(md_model[2].x,md_model[2].y,md_model[2].z,md_model[2].w)).xyzw*(v0).zzzz+(r0).xyzw)).xyzw;
r0.xyzw = (((r0).xyzw+(vec4(md_model[3].x,md_model[3].y,md_model[3].z,md_model[3].w)).xyzw)).xyzw;
r1.xyzw = (((r0).yyyy*(vec4(md_vp[1].x,md_vp[1].y,md_vp[1].z,md_vp[1].w)).xyzw)).xyzw;
r1.xyzw = (((vec4(md_vp[0].x,md_vp[0].y,md_vp[0].z,md_vp[0].w)).xyzw*(r0).xxxx+(r1).xyzw)).xyzw;
r1.xyzw = (((vec4(md_vp[2].x,md_vp[2].y,md_vp[2].z,md_vp[2].w)).xyzw*(r0).zzzz+(r1).xyzw)).xyzw;
o0.xyzw = (((vec4(md_vp[3].x,md_vp[3].y,md_vp[3].z,md_vp[3].w)).xyzw*(r0).wwww+(r1).xyzw)).xyzw;
o1.xyzw = ((v1).xyzw).xyzw;
o2.xy = (((v2).xyxx*(vec4(p_MainTex_ST.x,p_MainTex_ST.y,p_MainTex_ST.z,p_MainTex_ST.w)).xyxx+(vec4(p_MainTex_ST.x,p_MainTex_ST.y,p_MainTex_ST.z,p_MainTex_ST.w)).zwzz)).xy;
POSITION=o0;
md_vary_1=o1;
md_vary_2=o2;
}
// platform4_block0_program2.asm.txt
void fragment() {
mat4 md_model=md_flip*MODEL_MATRIX*md_flip;
mat4 md_inverse_model=inverse(md_model);
mat4 md_vp=PROJECTION_MATRIX*VIEW_MATRIX*md_flip;
mat4 md_inverse_vp=inverse(md_vp);
mat4 md_view=VIEW_MATRIX*md_flip;
vec4 o0=vec4(0.0);
vec4 r0=vec4(0.0);
vec4 r1=vec4(0.0);
vec4 v1=vec4(0.0);
vec4 v2=vec4(0.0);
v1=md_vary_1;
v2=md_vary_2;
r0.xyzw = (((v1).xyzw+(v1).xyzw)).xyzw;
r0.xyzw = (((r0).xyzw*(vec4(p_TintColor.x,p_TintColor.y,p_TintColor.z,p_TintColor.w)).xyzw)).xyzw;
r1.xyzw = ((texture(p_MainTex,vec2((((v2).xyxx).xy).x,1.0-(((v2).xyxx).xy).y))).xyzw).xyzw;
o0.xyzw = (((r0).xyzw*(r1).xyzw)).xyzw;
ALBEDO=o0.rgb;
ALPHA=o0.a;
}
