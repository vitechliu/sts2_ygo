shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_disabled;
// 按源 DXBC 前向程序逐指令生成；源文件和散列见迁移清单。
uniform float md_time = 0.0;
uniform float md_near = 0.1;
uniform float md_far = 300.0;
uniform float md_camera_size = 80.0;
const mat4 md_flip = mat4(vec4(1,0,0,0),vec4(0,1,0,0),vec4(0,0,-1,0),vec4(0,0,0,1));
uniform vec4 p_Color_01;
uniform vec4 p_Color_02;
uniform float p_Detail_Noise_Speed;
uniform float p_MASK_Direction;
uniform float p_MASK_Smooth;
uniform float p_MASK_Smooth_Bler;
uniform sampler2D p_MASK_Texture : source_color, filter_linear_mipmap, repeat_enable;
uniform float p_Main_Alpha;
uniform sampler2D p_Texture2DAsset_b3995bba6a78442da7c032aed218f175_Out_0_Texture2D : source_color, filter_linear_mipmap, repeat_enable;
uniform float p_UVDef_Main_Speed;
uniform float p_UVDef_sub_Speed;
varying vec4 md_vary_1;
varying vec4 md_vary_2;
varying vec4 md_vary_3;
varying vec4 md_vary_4;
varying vec4 md_vary_5;
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
vec4 o3=vec4(0.0);
vec4 o4=vec4(0.0);
vec4 o5=vec4(0.0);
vec4 r0=vec4(0.0);
vec4 r1=vec4(0.0);
vec4 v0=vec4(0.0);
vec4 v1=vec4(0.0);
vec4 v3=vec4(0.0);
vec4 v4=vec4(0.0);
vec4 v5=vec4(0.0);
v0=vec4(VERTEX*vec3(1,1,-1),1.0);
v1=vec4(NORMAL*vec3(1,1,-1),0.0);
v3=vec4(UV,UV2);
v4=CUSTOM0;
v5=COLOR;
r0.xyz = (((v0).yyyy*(vec4(md_model[1].x,md_model[1].y,md_model[1].z,md_model[1].w)).xyzx)).xyz;
r0.xyz = (((vec4(md_model[0].x,md_model[0].y,md_model[0].z,md_model[0].w)).xyzx*(v0).xxxx+(r0).xyzx)).xyz;
r0.xyz = (((vec4(md_model[2].x,md_model[2].y,md_model[2].z,md_model[2].w)).xyzx*(v0).zzzz+(r0).xyzx)).xyz;
r0.xyz = (((r0).xyzx+(vec4(md_model[3].x,md_model[3].y,md_model[3].z,md_model[3].w)).xyzx)).xyz;
r1.xyzw = (((r0).yyyy*(vec4(md_vp[1].x,md_vp[1].y,md_vp[1].z,md_vp[1].w)).xyzw)).xyzw;
r1.xyzw = (((vec4(md_vp[0].x,md_vp[0].y,md_vp[0].z,md_vp[0].w)).xyzw*(r0).xxxx+(r1).xyzw)).xyzw;
r1.xyzw = (((vec4(md_vp[2].x,md_vp[2].y,md_vp[2].z,md_vp[2].w)).xyzw*(r0).zzzz+(r1).xyzw)).xyzw;
o4.xyz = ((r0).xyzx).xyz;
o0.xyzw = (((r1).xyzw+(vec4(md_vp[3].x,md_vp[3].y,md_vp[3].z,md_vp[3].w)).xyzw)).xyzw;
o1.xyzw = ((v3).xyzw).xyzw;
o2.xyzw = ((v4).xyzw).xyzw;
o3.xyzw = ((v5).xyzw).xyzw;
r0.x = (vec4(dot(((v1).xyzx).xyz,((vec4(md_inverse_model[0].x,md_inverse_model[0].y,md_inverse_model[0].z,md_inverse_model[0].w)).xyzx).xyz))).x;
r0.y = (vec4(dot(((v1).xyzx).xyz,((vec4(md_inverse_model[1].x,md_inverse_model[1].y,md_inverse_model[1].z,md_inverse_model[1].w)).xyzx).xyz))).y;
r0.z = (vec4(dot(((v1).xyzx).xyz,((vec4(md_inverse_model[2].x,md_inverse_model[2].y,md_inverse_model[2].z,md_inverse_model[2].w)).xyzx).xyz))).z;
r0.w = (vec4(dot(((r0).xyzx).xyz,((r0).xyzx).xyz))).w;
r0.w = (max((r0).wwww,vec4(0.000000,0.000000,0.000000,0.000000))).w;
r0.w = (inversesqrt((r0).wwww)).w;
o5.xyz = (((r0).wwww*(r0).xyzx)).xyz;
POSITION=o0;
md_vary_1=o1;
md_vary_2=o2;
md_vary_3=o3;
md_vary_4=o4;
md_vary_5=o5;
}
// platform4_block0_program1.asm.txt
void fragment() {
mat4 md_model=md_flip*MODEL_MATRIX*md_flip;
mat4 md_inverse_model=inverse(md_model);
mat4 md_vp=PROJECTION_MATRIX*VIEW_MATRIX*md_flip;
mat4 md_inverse_vp=inverse(md_vp);
mat4 md_view=VIEW_MATRIX*md_flip;
vec4 o0=vec4(0.0);
vec4 r0=vec4(0.0);
vec4 r1=vec4(0.0);
vec4 r2=vec4(0.0);
vec4 r3=vec4(0.0);
vec4 r4=vec4(0.0);
vec4 r5=vec4(0.0);
vec4 r6=vec4(0.0);
vec4 r7=vec4(0.0);
vec4 r8=vec4(0.0);
vec4 v1=vec4(0.0);
vec4 v2=vec4(0.0);
vec4 v3=vec4(0.0);
v1=md_vary_1;
v2=md_vary_2;
v3=md_vary_3;
r0.xyz = (intBitsToFloat(-ivec4(greaterThanEqual(vec4(0.003131,0.003131,0.003131,0.000000),(vec4(p_Color_01.x,p_Color_01.y,p_Color_01.z,p_Color_01.w)).xyzx)))).xyz;
r1.xyz = (((vec4(p_Color_01.x,p_Color_01.y,p_Color_01.z,p_Color_01.w)).xyzx*vec4(12.923210,12.923210,12.923210,0.000000))).xyz;
r2.xyz = (log2(abs((vec4(p_Color_01.x,p_Color_01.y,p_Color_01.z,p_Color_01.w)).xyzx))).xyz;
r2.xyz = (((r2).xyzx*vec4(0.416667,0.416667,0.416667,0.000000))).xyz;
r2.xyz = (exp2((r2).xyzx)).xyz;
r2.xyz = (((r2).xyzx*vec4(1.055000,1.055000,1.055000,0.000000)+vec4(-0.055000,-0.055000,-0.055000,0.000000))).xyz;
r0.xyz = (mix((r2).xyzx,(r1).xyzx,notEqual((r0).xyzx,vec4(0.0)))).xyz;
r1.xyz = (intBitsToFloat(-ivec4(greaterThanEqual(vec4(0.003131,0.003131,0.003131,0.000000),(vec4(p_Color_02.x,p_Color_02.y,p_Color_02.z,p_Color_02.w)).xyzx)))).xyz;
r2.xyz = (((vec4(p_Color_02.x,p_Color_02.y,p_Color_02.z,p_Color_02.w)).xyzx*vec4(12.923210,12.923210,12.923210,0.000000))).xyz;
r3.xyz = (log2(abs((vec4(p_Color_02.x,p_Color_02.y,p_Color_02.z,p_Color_02.w)).xyzx))).xyz;
r3.xyz = (((r3).xyzx*vec4(0.416667,0.416667,0.416667,0.000000))).xyz;
r3.xyz = (exp2((r3).xyzx)).xyz;
r3.xyz = (((r3).xyzx*vec4(1.055000,1.055000,1.055000,0.000000)+vec4(-0.055000,-0.055000,-0.055000,0.000000))).xyz;
r1.xyz = (mix((r3).xyzx,(r2).xyzx,notEqual((r1).xyzx,vec4(0.0)))).xyz;
r2.y = (((vec4((vec4(md_time,sin(md_time),cos(md_time),0.0)).x,(vec4(md_time,sin(md_time),cos(md_time),0.0)).y,(vec4(md_time,sin(md_time),cos(md_time),0.0)).z,(vec4(md_time,sin(md_time),cos(md_time),0.0)).w)).xxxx*(vec4(p_MASK_Smooth,p_MASK_Smooth_Bler,p_MASK_Direction,p_UVDef_Main_Speed)).wwww)).y;
r2.xz = (vec4(0.0,0.0,0.0,0.0)).xz;
r2.xy = ((-((r2).xyxx)+(v1).zwzz)).xy;
r2.xy = (((r2).xyxx+vec4(1.000000,1.000000,0.000000,0.000000))).xy;
r2.xy = (((v1).xyxx*vec4(6.000000,1.000000,0.000000,0.000000)+(r2).xyxx)).xy;
r3.xy = (((vec4((vec4(md_time,sin(md_time),cos(md_time),0.0)).x,(vec4(md_time,sin(md_time),cos(md_time),0.0)).y,(vec4(md_time,sin(md_time),cos(md_time),0.0)).z,(vec4(md_time,sin(md_time),cos(md_time),0.0)).w)).xxxx+vec4(5.000000,10.000000,0.000000,0.000000))).xy;
r2.xy = (((r2).xyxx*vec4(5.000000,5.000000,0.000000,0.000000))).xy;
r3.zw = (floor((r2).xxxy)).zw;
r2.xy = (fract((r2).xyxx)).xy;
r4.xy = (vec4(0.0,8.000000,0.0,0.0)).xy;
r0.w = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).w;
for (int md_loop_0=0; md_loop_0<1024; md_loop_0++) {
r1.w = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r0).wwww))))).w;
if (any(notEqual((r1).wwww,vec4(0.0)))) { break; }
r5.y = (vec4(floatBitsToInt((r0).wwww))).y;
r4.zw = ((r4).xxxy).zw;
r1.w = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).w;
for (int md_loop_1=0; md_loop_1<1024; md_loop_1++) {
r5.z = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r1).wwww))))).z;
if (any(notEqual((r5).zzzz,vec4(0.0)))) { break; }
r5.x = (vec4(floatBitsToInt((r1).wwww))).x;
r5.zw = (((r3).zzzw+(r5).xxxy)).zw;
r6.x = (vec4(dot(((r5).zwzz).xy,(vec4(15.270000,99.410004,0.000000,0.000000)).xy))).x;
r5.z = (vec4(dot(((r5).zwzz).xy,(vec4(47.630001,89.980003,0.000000,0.000000)).xy))).z;
r6.y = (sin((r6).xxxx)).y;

r6.x = (sin((r5).zzzz)).x;

r5.zw = (fract((r6).xxxy)).zw;
r5.zw = (((r3).xxxx*(r5).zzzw)).zw;
r6.x = (sin((r5).zzzz)).x;


r6.y = (cos((r5).wwww)).y;
r5.xz = (((r6).xxyx*vec4(0.500000,0.000000,0.500000,0.000000)+(r5).xxyx)).xz;
r5.xz = ((-((r2).xxyx)+(r5).xxzx)).xz;
r5.xz = (((r5).xxzx+vec4(0.500000,0.000000,0.500000,0.000000))).xz;
r5.x = (vec4(dot(((r5).xzxx).xy,((r5).xzxx).xy))).x;
r5.x = (sqrt((r5).xxxx)).x;
r5.z = (intBitsToFloat(-ivec4(lessThan((r5).xxxx,(r4).wwww)))).z;
r4.zw = (mix((r4).zzzw,(r5).xxxx,notEqual((r5).zzzz,vec4(0.0)))).zw;
r1.w = (intBitsToFloat(floatBitsToInt((r1).wwww) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).w;
}
r4.xy = ((r4).zwzz).xy;
r0.w = (intBitsToFloat(floatBitsToInt((r0).wwww) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).w;
}
r2.w = (((vec4((vec4(md_time,sin(md_time),cos(md_time),0.0)).x,(vec4(md_time,sin(md_time),cos(md_time),0.0)).y,(vec4(md_time,sin(md_time),cos(md_time),0.0)).z,(vec4(md_time,sin(md_time),cos(md_time),0.0)).w)).xxxx*(vec4(p_UVDef_sub_Speed,0.0,0.0,0.0)).xxxx)).w;
r2.xy = ((-((r2).zwzz)+(v1).zwzz)).xy;
r2.xy = (((v1).xyxx*vec4(6.000000,1.000000,0.000000,0.000000)+(r2).xyxx)).xy;
r2.xy = (((r2).xyxx+vec4(1.000000,1.000000,0.000000,0.000000))).xy;
r5.xyzw = (((r2).xyxy*vec4(30.000000,30.000000,15.000000,15.000000))).xyzw;
r6.xyzw = (floor((r5).xyzw)).xyzw;
r5.xyzw = (fract((r5).xyzw)).xyzw;
r7.xyzw = (((r5).xyzw*(r5).xyzw)).xyzw;
r5.xyzw = ((-((r5).xyzw)*vec4(2.000000,2.000000,2.000000,2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).xyzw;
r5.xyzw = (((r5).xyzw*(r7).xyzw)).xyzw;
r7.xyzw = (((r6).xyxy+vec4(1.000000,0.000000,0.000000,1.000000))).xyzw;
r8.xyzw = (((r6).xyzw+vec4(1.000000,1.000000,1.000000,0.000000))).xyzw;
r0.w = (vec4(dot(((r6).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r0.w = (sin((r0).wwww)).w;

r0.w = (((r0).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r0.w = (fract((r0).wwww)).w;
r1.w = (vec4(dot(((r7).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.w = (fract((r1).wwww)).w;
r2.z = (vec4(dot(((r7).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r2.z = (sin((r2).zzzz)).z;

r2.w = (vec4(dot(((r8).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r2.w = (sin((r2).wwww)).w;

r2.xyzw = (((r2).xyzw*vec4(7.500000,7.500000,43758.546875,43758.546875))).xyzw;
r2.zw = (fract((r2).zzzw)).zw;
r1.xyzw = ((-((r0).xyzw)+(r1).xyzw)).xyzw;
r0.w = (((r5).xxxx*(r1).wwww+(r0).wwww)).w;
r1.w = ((-((r2).zzzz)+(r2).wwww)).w;
r1.w = (((r5).xxxx*(r1).wwww+(r2).zzzz)).w;
r1.w = ((-((r0).wwww)+(r1).wwww)).w;
r0.w = (((r5).yyyy*(r1).wwww+(r0).wwww)).w;
r7.xyzw = (((r6).zwzw+vec4(0.000000,1.000000,1.000000,1.000000))).xyzw;
r1.w = (vec4(dot(((r6).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.w = (fract((r1).wwww)).w;
r2.z = (vec4(dot(((r8).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r2.w = (vec4(dot(((r7).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r2.zw = (sin((r2).zzzw)).zw;

r2.zw = (((r2).zzzw*vec4(0.000000,0.000000,43758.546875,43758.546875))).zw;
r2.zw = (fract((r2).zzzw)).zw;
r3.x = (vec4(dot(((r7).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).x;
r3.x = (sin((r3).xxxx)).x;

r3.x = (((r3).xxxx*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).x;
r3.x = (fract((r3).xxxx)).x;
r2.z = ((-((r1).wwww)+(r2).zzzz)).z;
r1.w = (((r5).zzzz*(r2).zzzz+(r1).wwww)).w;
r2.z = ((-((r2).wwww)+(r3).xxxx)).z;
r2.z = (((r5).zzzz*(r2).zzzz+(r2).wwww)).z;
r2.z = ((-((r1).wwww)+(r2).zzzz)).z;
r1.w = (((r5).wwww*(r2).zzzz+(r1).wwww)).w;
r1.w = (((r1).wwww*vec4(0.250000,0.250000,0.250000,0.250000))).w;
r0.w = (((r0).wwww*vec4(0.125000,0.125000,0.125000,0.125000)+(r1).wwww)).w;
r2.zw = (floor((r2).xxxy)).zw;
r2.xy = (fract((r2).xyxx)).xy;
r3.xz = (((r2).xxyx*(r2).xxyx)).xz;
r2.xy = ((-((r2).xyxx)*vec4(2.000000,2.000000,0.000000,0.000000)+vec4(3.000000,3.000000,0.000000,0.000000))).xy;
r2.xy = (((r2).xyxx*(r3).xzxx)).xy;
r5.xyzw = (((r2).zwzw+vec4(1.000000,0.000000,0.000000,1.000000))).xyzw;
r3.xz = (((r2).zzwz+vec4(1.000000,0.000000,1.000000,0.000000))).xz;
r1.w = (vec4(dot(((r2).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.w = (fract((r1).wwww)).w;
r2.z = (vec4(dot(((r5).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r2.z = (sin((r2).zzzz)).z;

r2.w = (vec4(dot(((r5).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r2.w = (sin((r2).wwww)).w;

r2.zw = (((r2).zzzw*vec4(0.000000,0.000000,43758.546875,43758.546875))).zw;
r2.zw = (fract((r2).zzzw)).zw;
r3.x = (vec4(dot(((r3).xzxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).x;
r3.x = (sin((r3).xxxx)).x;

r3.x = (((r3).xxxx*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).x;
r3.x = (fract((r3).xxxx)).x;
r2.z = ((-((r1).wwww)+(r2).zzzz)).z;
r1.w = (((r2).xxxx*(r2).zzzz+(r1).wwww)).w;
r2.z = ((-((r2).wwww)+(r3).xxxx)).z;
r2.x = (((r2).xxxx*(r2).zzzz+(r2).wwww)).x;
r2.x = ((-((r1).wwww)+(r2).xxxx)).x;
r1.w = (((r2).yyyy*(r2).xxxx+(r1).wwww)).w;
r0.w = (((r1).wwww*vec4(0.500000,0.500000,0.500000,0.500000)+(r0).wwww)).w;
r2.y = (((r0).wwww*(r4).xxxx)).y;
r2.xz = (vec4(0.500000,0.0,0.0,0.0)).xz;
r3.xz = (((r2).xxyx*vec4(0.000000,0.000000,0.500000,0.000000))).xz;
r2.xy = ((vec4(0.000000,0.500000,0.000000,0.000000)*(r2).xyxx+(v1).xyxx)).xy;
r4.x = (vec4(0.0,0.0,0.0,0.0)).x;
r4.yw = (((vec4((vec4(md_time,sin(md_time),cos(md_time),0.0)).x,(vec4(md_time,sin(md_time),cos(md_time),0.0)).y,(vec4(md_time,sin(md_time),cos(md_time),0.0)).z,(vec4(md_time,sin(md_time),cos(md_time),0.0)).w)).xxxx*vec4(0.000000,0.300000,0.000000,0.100000))).yw;
r4.xyzw = ((-((r4).xyxw)+(v1).zwzw)).xyzw;
r4.xyzw = (((r4).xyzw+vec4(1.000000,1.000000,1.000000,1.000000))).xyzw;
r2.xy = (((r2).xyxx*vec4(2.000000,1.000000,0.000000,0.000000)+(r4).xyxx)).xy;
r2.xy = (((r2).xyxx*vec4(3.000000,3.000000,0.000000,0.000000))).xy;
r4.xy = (floor((r2).xyxx)).xy;
r2.xy = (fract((r2).xyxx)).xy;
r5.xy = (vec4(0.0,8.000000,0.0,0.0)).xy;
r0.w = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).w;
for (int md_loop_2=0; md_loop_2<1024; md_loop_2++) {
r1.w = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r0).wwww))))).w;
if (any(notEqual((r1).wwww,vec4(0.0)))) { break; }
r6.y = (vec4(floatBitsToInt((r0).wwww))).y;
r5.zw = ((r5).xxxy).zw;
r1.w = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).w;
for (int md_loop_3=0; md_loop_3<1024; md_loop_3++) {
r3.w = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r1).wwww))))).w;
if (any(notEqual((r3).wwww,vec4(0.0)))) { break; }
r6.x = (vec4(floatBitsToInt((r1).wwww))).x;
r6.zw = (((r4).xxxy+(r6).xxxy)).zw;
r3.w = (vec4(dot(((r6).zwzz).xy,(vec4(15.270000,99.410004,0.000000,0.000000)).xy))).w;
r6.z = (vec4(dot(((r6).zwzz).xy,(vec4(47.630001,89.980003,0.000000,0.000000)).xy))).z;
r7.y = (sin((r3).wwww)).y;

r7.x = (sin((r6).zzzz)).x;

r6.zw = (fract((r7).xxxy)).zw;
r6.zw = (((r3).yyyy*(r6).zzzw)).zw;
r7.x = (sin((r6).zzzz)).x;


r7.y = (cos((r6).wwww)).y;
r6.xz = (((r7).xxyx*vec4(0.500000,0.000000,0.500000,0.000000)+(r6).xxyx)).xz;
r6.xz = ((-((r2).xxyx)+(r6).xxzx)).xz;
r6.xz = (((r6).xxzx+vec4(0.500000,0.000000,0.500000,0.000000))).xz;
r3.w = (vec4(dot(((r6).xzxx).xy,((r6).xzxx).xy))).w;
r3.w = (sqrt((r3).wwww)).w;
r6.x = (intBitsToFloat(-ivec4(lessThan((r3).wwww,(r5).wwww)))).x;
r5.zw = (mix((r5).zzzw,(r3).wwww,notEqual((r6).xxxx,vec4(0.0)))).zw;
r1.w = (intBitsToFloat(floatBitsToInt((r1).wwww) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).w;
}
r5.xy = ((r5).zwzz).xy;
r0.w = (intBitsToFloat(floatBitsToInt((r0).wwww) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).w;
}
r2.xy = (((v1).xyxx*vec4(2.000000,1.000000,0.000000,0.000000)+(r4).zwzz)).xy;
r4.xyzw = (((r2).xyxy*vec4(10.000000,10.000000,5.000000,5.000000))).xyzw;
r6.xyzw = (floor((r4).xyzw)).xyzw;
r4.xyzw = (fract((r4).xyzw)).xyzw;
r7.xyzw = (((r4).xyzw*(r4).xyzw)).xyzw;
r4.xyzw = ((-((r4).xyzw)*vec4(2.000000,2.000000,2.000000,2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).xyzw;
r4.xyzw = (((r4).xyzw*(r7).xyzw)).xyzw;
r7.xyzw = (((r6).xyxy+vec4(1.000000,0.000000,0.000000,1.000000))).xyzw;
r8.xyzw = (((r6).xyzw+vec4(1.000000,1.000000,1.000000,0.000000))).xyzw;
r0.w = (vec4(dot(((r6).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r0.w = (sin((r0).wwww)).w;

r0.w = (((r0).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r0.w = (fract((r0).wwww)).w;
r1.w = (vec4(dot(((r7).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.w = (fract((r1).wwww)).w;
r3.w = (vec4(dot(((r7).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r3.w = (sin((r3).wwww)).w;

r3.w = (((r3).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r3.w = (fract((r3).wwww)).w;
r5.y = (vec4(dot(((r8).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r5.y = (sin((r5).yyyy)).y;

r5.y = (((r5).yyyy*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).y;
r5.y = (fract((r5).yyyy)).y;
r1.w = ((-((r0).wwww)+(r1).wwww)).w;
r0.w = (((r4).xxxx*(r1).wwww+(r0).wwww)).w;
r1.w = ((-((r3).wwww)+(r5).yyyy)).w;
r1.w = (((r4).xxxx*(r1).wwww+(r3).wwww)).w;
r1.w = ((-((r0).wwww)+(r1).wwww)).w;
r0.w = (((r4).yyyy*(r1).wwww+(r0).wwww)).w;
r7.xyzw = (((r6).zwzw+vec4(0.000000,1.000000,1.000000,1.000000))).xyzw;
r1.w = (vec4(dot(((r6).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.w = (fract((r1).wwww)).w;
r3.w = (vec4(dot(((r8).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r3.w = (sin((r3).wwww)).w;

r3.w = (((r3).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r3.w = (fract((r3).wwww)).w;
r4.x = (vec4(dot(((r7).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).x;
r4.x = (sin((r4).xxxx)).x;

r4.y = (vec4(dot(((r7).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r4.y = (sin((r4).yyyy)).y;

r4.xy = (((r4).xyxx*vec4(43758.546875,43758.546875,0.000000,0.000000))).xy;
r4.xy = (fract((r4).xyxx)).xy;
r3.w = ((-((r1).wwww)+(r3).wwww)).w;
r1.w = (((r4).zzzz*(r3).wwww+(r1).wwww)).w;
r3.w = ((-((r4).xxxx)+(r4).yyyy)).w;
r3.w = (((r4).zzzz*(r3).wwww+(r4).xxxx)).w;
r3.w = ((-((r1).wwww)+(r3).wwww)).w;
r1.w = (((r4).wwww*(r3).wwww+(r1).wwww)).w;
r1.w = (((r1).wwww*vec4(0.250000,0.250000,0.250000,0.250000))).w;
r0.w = (((r0).wwww*vec4(0.125000,0.125000,0.125000,0.125000)+(r1).wwww)).w;
r2.xy = (((r2).xyxx*vec4(2.500000,2.500000,0.000000,0.000000))).xy;
r4.xy = (floor((r2).xyxx)).xy;
r2.xy = (fract((r2).xyxx)).xy;
r4.zw = (((r2).xxxy*(r2).xxxy)).zw;
r2.xy = ((-((r2).xyxx)*vec4(2.000000,2.000000,0.000000,0.000000)+vec4(3.000000,3.000000,0.000000,0.000000))).xy;
r2.xy = (((r2).xyxx*(r4).zwzz)).xy;
r6.xyzw = (((r4).xyxy+vec4(1.000000,0.000000,0.000000,1.000000))).xyzw;
r4.zw = (((r4).xxxy+vec4(0.000000,0.000000,1.000000,1.000000))).zw;
r1.w = (vec4(dot(((r4).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.w = (fract((r1).wwww)).w;
r3.w = (vec4(dot(((r6).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r3.w = (sin((r3).wwww)).w;

r3.w = (((r3).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r3.w = (fract((r3).wwww)).w;
r4.x = (vec4(dot(((r6).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).x;
r4.x = (sin((r4).xxxx)).x;

r4.y = (vec4(dot(((r4).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r4.y = (sin((r4).yyyy)).y;

r4.xy = (((r4).xyxx*vec4(43758.546875,43758.546875,0.000000,0.000000))).xy;
r4.xy = (fract((r4).xyxx)).xy;
r3.w = ((-((r1).wwww)+(r3).wwww)).w;
r1.w = (((r2).xxxx*(r3).wwww+(r1).wwww)).w;
r3.w = ((-((r4).xxxx)+(r4).yyyy)).w;
r2.x = (((r2).xxxx*(r3).wwww+(r4).xxxx)).x;
r2.x = ((-((r1).wwww)+(r2).xxxx)).x;
r1.w = (((r2).yyyy*(r2).xxxx+(r1).wwww)).w;
r0.w = (((r1).wwww*vec4(0.500000,0.500000,0.500000,0.500000)+(r0).wwww)).w;
r4.xyz = (((v1).yxyy+vec4(0.186000,-0.500000,-0.500000,0.000000))).xyz;
r4.x = (clamp((r4).xxxx,vec4(0.0),vec4(1.0))).x;
r1.w = (((r4).xxxx*vec4(-2.000000,-2.000000,-2.000000,-2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).w;
r2.x = (((r4).xxxx*(r4).xxxx)).x;
r1.w = (((r1).wwww*(r2).xxxx)).w;
r0.w = (((r5).xxxx*(r0).wwww+(r1).wwww)).w;
r0.w = (clamp(((r0).wwww*vec4(0.500000,0.500000,0.500000,0.500000)),vec4(0.0),vec4(1.0))).w;
r0.w = (((r0).wwww*vec4(1.333333,1.333333,1.333333,1.333333))).w;
r0.w = (min((r0).wwww,vec4(1.000000,1.000000,1.000000,1.000000))).w;
r1.w = (((r0).wwww*vec4(-2.000000,-2.000000,-2.000000,-2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).w;
r0.w = (((r0).wwww*(r0).wwww)).w;
r0.w = (((r0).wwww*(r1).wwww)).w;
r0.xyz = (((r0).wwww*(r1).xyzx+(r0).xyzx)).xyz;
o0.xyz = (((r0).xyzx*(v3).xyzx)).xyz;
r0.xy = ((-((v2).zwzz)+vec4(1.000000,1.000000,0.000000,0.000000))).xy;
r0.z = ((-((v1).yyyy)+vec4(1.000000,1.000000,1.000000,1.000000))).z;
r0.z = (clamp(((r0).zzzz*vec4(-0.500000,-0.500000,-0.500000,-0.500000)+vec4(1.000000,1.000000,1.000000,1.000000)),vec4(0.0),vec4(1.0))).z;
r0.zw = (((r0).zzzz*(r3).xxxz+(v1).xxxy)).zw;
r1.y = (((vec4((vec4(md_time,sin(md_time),cos(md_time),0.0)).x,(vec4(md_time,sin(md_time),cos(md_time),0.0)).y,(vec4(md_time,sin(md_time),cos(md_time),0.0)).z,(vec4(md_time,sin(md_time),cos(md_time),0.0)).w)).xxxx*(vec4(p_Main_Alpha,p_Detail_Noise_Speed,0.0,0.0)).yyyy)).y;
r1.xz = (vec4(0.0,0.0,0.0,0.0)).xz;
r1.w = (((vec4((vec4(md_time,sin(md_time),cos(md_time),0.0)).x,(vec4(md_time,sin(md_time),cos(md_time),0.0)).y,(vec4(md_time,sin(md_time),cos(md_time),0.0)).z,(vec4(md_time,sin(md_time),cos(md_time),0.0)).w)).xxxx*vec4(0.400000,0.400000,0.400000,0.400000))).w;
r1.xyzw = ((-((r1).xyzw)+(v1).zwzw)).xyzw;
r1.xyzw = (((r1).xyzw+vec4(1.000000,1.000000,1.000000,1.000000))).xyzw;
r0.zw = (((r0).zzzw*vec4(0.000000,0.000000,4.000000,1.000000)+(r1).xxxy)).zw;
r0.zw = (((r0).zzzw*vec4(0.000000,0.000000,3.000000,3.000000))).zw;
r1.xy = (floor((r0).zwzz)).xy;
r0.zw = (fract((r0).zzzw)).zw;
r2.xy = (vec4(0.0,8.000000,0.0,0.0)).xy;
r3.x = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).x;
for (int md_loop_4=0; md_loop_4<1024; md_loop_4++) {
r3.z = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r3).xxxx))))).z;
if (any(notEqual((r3).zzzz,vec4(0.0)))) { break; }
r5.y = (vec4(floatBitsToInt((r3).xxxx))).y;
r3.zw = ((r2).xxxy).zw;
r4.x = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).x;
for (int md_loop_5=0; md_loop_5<1024; md_loop_5++) {
r4.w = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r4).xxxx))))).w;
if (any(notEqual((r4).wwww,vec4(0.0)))) { break; }
r5.x = (vec4(floatBitsToInt((r4).xxxx))).x;
r5.zw = (((r1).xxxy+(r5).xxxy)).zw;
r4.w = (vec4(dot(((r5).zwzz).xy,(vec4(15.270000,99.410004,0.000000,0.000000)).xy))).w;
r5.z = (vec4(dot(((r5).zwzz).xy,(vec4(47.630001,89.980003,0.000000,0.000000)).xy))).z;
r6.y = (sin((r4).wwww)).y;

r6.x = (sin((r5).zzzz)).x;

r5.zw = (fract((r6).xxxy)).zw;
r5.zw = (((r3).yyyy*(r5).zzzw)).zw;
r6.x = (sin((r5).zzzz)).x;


r6.y = (cos((r5).wwww)).y;
r5.xz = (((r6).xxyx*vec4(0.500000,0.000000,0.500000,0.000000)+(r5).xxyx)).xz;
r5.xz = ((-((r0).zzwz)+(r5).xxzx)).xz;
r5.xz = (((r5).xxzx+vec4(0.500000,0.000000,0.500000,0.000000))).xz;
r4.w = (vec4(dot(((r5).xzxx).xy,((r5).xzxx).xy))).w;
r4.w = (sqrt((r4).wwww)).w;
r5.x = (intBitsToFloat(-ivec4(lessThan((r4).wwww,(r3).wwww)))).x;
r3.zw = (mix((r3).zzzw,(r4).wwww,notEqual((r5).xxxx,vec4(0.0)))).zw;
r4.x = (intBitsToFloat(floatBitsToInt((r4).xxxx) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).x;
}
r2.xy = ((r3).zwzz).xy;
r3.x = (intBitsToFloat(floatBitsToInt((r3).xxxx) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).x;
}
r0.zw = (((v1).xxxy*vec4(0.000000,0.000000,4.000000,1.000000)+(r1).zzzw)).zw;
r1.xyzw = (((r0).zwzw*vec4(12.000000,12.000000,6.000000,6.000000))).xyzw;
r3.xyzw = (floor((r1).xyzw)).xyzw;
r1.xyzw = (fract((r1).xyzw)).xyzw;
r5.xyzw = (((r1).xyzw*(r1).xyzw)).xyzw;
r1.xyzw = ((-((r1).xyzw)*vec4(2.000000,2.000000,2.000000,2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).xyzw;
r1.xyzw = (((r1).xyzw*(r5).xyzw)).xyzw;
r5.xyzw = (((r3).xyxy+vec4(1.000000,0.000000,0.000000,1.000000))).xyzw;
r6.xyzw = (((r3).xyzw+vec4(1.000000,1.000000,1.000000,0.000000))).xyzw;
r2.y = (vec4(dot(((r3).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r2.y = (sin((r2).yyyy)).y;

r2.y = (((r2).yyyy*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).y;
r2.y = (fract((r2).yyyy)).y;
r3.x = (vec4(dot(((r5).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).x;
r3.x = (sin((r3).xxxx)).x;

r3.x = (((r3).xxxx*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).x;
r3.y = (vec4(dot(((r5).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r3.y = (sin((r3).yyyy)).y;

r3.y = (((r3).yyyy*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).y;
r3.xy = (fract((r3).xyxx)).xy;
r4.x = (vec4(dot(((r6).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).x;
r4.x = (sin((r4).xxxx)).x;

r4.x = (((r4).xxxx*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).x;
r4.x = (fract((r4).xxxx)).x;
r3.x = ((-((r2).yyyy)+(r3).xxxx)).x;
r2.y = (((r1).xxxx*(r3).xxxx+(r2).yyyy)).y;
r3.x = ((-((r3).yyyy)+(r4).xxxx)).x;
r1.x = (((r1).xxxx*(r3).xxxx+(r3).yyyy)).x;
r1.x = ((-((r2).yyyy)+(r1).xxxx)).x;
r1.x = (((r1).yyyy*(r1).xxxx+(r2).yyyy)).x;
r5.xyzw = (((r3).zwzw+vec4(0.000000,1.000000,1.000000,1.000000))).xyzw;
r1.y = (vec4(dot(((r3).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r1.y = (sin((r1).yyyy)).y;

r1.y = (((r1).yyyy*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).y;
r1.y = (fract((r1).yyyy)).y;
r2.y = (vec4(dot(((r6).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r2.y = (sin((r2).yyyy)).y;

r2.y = (((r2).yyyy*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).y;
r2.y = (fract((r2).yyyy)).y;
r3.x = (vec4(dot(((r5).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).x;
r3.x = (sin((r3).xxxx)).x;

r3.x = (((r3).xxxx*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).x;
r3.y = (vec4(dot(((r5).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r3.y = (sin((r3).yyyy)).y;

r3.y = (((r3).yyyy*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).y;
r3.xy = (fract((r3).xyxx)).xy;
r2.y = ((-((r1).yyyy)+(r2).yyyy)).y;
r1.y = (((r1).zzzz*(r2).yyyy+(r1).yyyy)).y;
r2.y = ((-((r3).xxxx)+(r3).yyyy)).y;
r1.z = (((r1).zzzz*(r2).yyyy+(r3).xxxx)).z;
r1.z = ((-((r1).yyyy)+(r1).zzzz)).z;
r1.y = (((r1).wwww*(r1).zzzz+(r1).yyyy)).y;
r1.y = (((r1).yyyy*vec4(0.250000,0.250000,0.250000,0.250000))).y;
r1.x = (((r1).xxxx*vec4(0.125000,0.125000,0.125000,0.125000)+(r1).yyyy)).x;
r0.zw = (((r0).zzzw*vec4(0.000000,0.000000,3.000000,3.000000))).zw;
r1.yz = (floor((r0).zzwz)).yz;
r0.zw = (fract((r0).zzzw)).zw;
r3.xy = (((r0).zwzz*(r0).zwzz)).xy;
r0.zw = ((-((r0).zzzw)*vec4(0.000000,0.000000,2.000000,2.000000)+vec4(0.000000,0.000000,3.000000,3.000000))).zw;
r0.zw = (((r0).zzzw*(r3).xxxy)).zw;
r3.xyzw = (((r1).yzyz+vec4(1.000000,0.000000,0.000000,1.000000))).xyzw;
r4.xw = (((r1).yyyz+vec4(1.000000,0.000000,0.000000,1.000000))).xw;
r1.y = (vec4(dot(((r1).yzyy).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r1.y = (sin((r1).yyyy)).y;

r1.y = (((r1).yyyy*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).y;
r1.z = (vec4(dot(((r3).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r1.z = (sin((r1).zzzz)).z;

r1.z = (((r1).zzzz*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).z;
r1.w = (vec4(dot(((r3).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.yzw = (fract((r1).yyzw)).yzw;
r2.y = (vec4(dot(((r4).xwxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r2.y = (sin((r2).yyyy)).y;

r2.y = (((r2).yyyy*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).y;
r2.y = (fract((r2).yyyy)).y;
r1.z = ((-((r1).yyyy)+(r1).zzzz)).z;
r1.y = (((r0).zzzz*(r1).zzzz+(r1).yyyy)).y;
r1.z = ((-((r1).wwww)+(r2).yyyy)).z;
r0.z = (((r0).zzzz*(r1).zzzz+(r1).wwww)).z;
r0.z = ((-((r1).yyyy)+(r0).zzzz)).z;
r0.z = (((r0).wwww*(r0).zzzz+(r1).yyyy)).z;
r0.z = (((r0).zzzz*vec4(0.500000,0.500000,0.500000,0.500000)+(r1).xxxx)).z;
r0.w = (((vec4(p_MASK_Smooth,p_MASK_Smooth_Bler,p_MASK_Direction,p_UVDef_Main_Speed)).zzzz*vec4(0.017453,0.017453,0.017453,0.017453))).w;
r1.x = (sin((r0).wwww)).x;
r3.x = (cos((r0).wwww)).x;
r3.yz = ((r1).xxxx).yz;
r1.xyz = (((r3).xyzx*vec4(0.500000,-0.500000,0.500000,0.000000)+vec4(0.500000,0.500000,0.500000,0.000000))).xyz;
r1.xyz = (((r1).xyzx*vec4(2.000000,2.000000,2.000000,0.000000)+vec4(-1.000000,-1.000000,-1.000000,0.000000))).xyz;
r3.x = (vec4(dot(((r4).yzyy).xy,((r1).xzxx).xy))).x;
r3.y = (vec4(dot(((r4).zyzz).xy,((r1).xyxx).xy))).y;
r1.xy = (((r3).xyxx+vec4(0.500000,0.500000,0.000000,0.000000))).xy;
r1.xyzw = ((texture(p_MASK_Texture,vec2((((r1).xyxx).xy).x,1.0-(((r1).xyxx).xy).y),((vec4((vec2(0.0)).x,(vec2(0.0)).y,0.0,0.0)).xxxx).x)).xyzw).xyzw;
r2.w = (((vec4((vec4(md_time,sin(md_time),cos(md_time),0.0)).x,(vec4(md_time,sin(md_time),cos(md_time),0.0)).y,(vec4(md_time,sin(md_time),cos(md_time),0.0)).z,(vec4(md_time,sin(md_time),cos(md_time),0.0)).w)).xxxx*vec4(0.500000,0.500000,0.500000,0.500000))).w;
r1.yz = ((-((r2).zzwz)+(v1).zzwz)).yz;
r1.yz = (((v1).xxyx*vec4(0.000000,2.000000,1.000000,0.000000)+(r1).yyzy)).yz;
r1.yz = (((r1).yyzy+vec4(0.000000,1.000000,1.000000,0.000000))).yz;
r3.xyzw = ((texture(p_Texture2DAsset_b3995bba6a78442da7c032aed218f175_Out_0_Texture2D,vec2((((r1).yzyy).xy).x,1.0-(((r1).yzyy).xy).y),((vec4((vec2(0.0)).x,(vec2(0.0)).y,0.0,0.0)).xxxx).x)).xyzw).xyzw;
r0.w = ((-((r1).xxxx)+vec4(1.000000,1.000000,1.000000,1.000000))).w;
r0.w = (((r0).wwww+(r0).wwww)).w;
r1.y = ((-((r3).zzzz)+vec4(1.000000,1.000000,1.000000,1.000000))).y;
r0.w = ((-((r0).wwww)*(r1).yyyy+vec4(1.000000,1.000000,1.000000,1.000000))).w;
r1.y = (vec4(dot(((r3).zzzz).xy,((r1).xxxx).xy))).y;
r1.x = (intBitsToFloat(-ivec4(greaterThanEqual(vec4(0.500000,0.500000,0.500000,0.500000),(r1).xxxx)))).x;
r1.z = (intBitsToFloat(floatBitsToInt((r1).xxxx) & floatBitsToInt(vec4(uintBitsToFloat(1065353216u),uintBitsToFloat(1065353216u),uintBitsToFloat(1065353216u),uintBitsToFloat(1065353216u))))).z;
r1.x = (mix(vec4(1.000000,1.000000,1.000000,1.000000),vec4(0.0,0.0,0.0,0.0),notEqual((r1).xxxx,vec4(0.0)))).x;
r0.w = (((r0).wwww*(r1).xxxx)).w;
r0.w = (clamp(((r1).yyyy*(r1).zzzz+(r0).wwww),vec4(0.0),vec4(1.0))).w;
r0.w = (((r0).wwww+-((vec4(p_MASK_Smooth,p_MASK_Smooth_Bler,p_MASK_Direction,p_UVDef_Main_Speed)).xxxx))).w;
r1.x = ((vec4(1.000000,1.000000,1.000000,1.000000)/(vec4(p_MASK_Smooth,p_MASK_Smooth_Bler,p_MASK_Direction,p_UVDef_Main_Speed)).yyyy)).x;
r0.w = (clamp(((r0).wwww*(r1).xxxx),vec4(0.0),vec4(1.0))).w;
r1.x = (((r0).wwww*vec4(-2.000000,-2.000000,-2.000000,-2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).x;
r0.w = (((r0).wwww*(r0).wwww)).w;
r0.w = (((r0).wwww*(r1).xxxx)).w;
r0.z = (((r2).xxxx*(r0).zzzz+(r0).wwww)).z;
r0.z = (clamp(((r0).zzzz*vec4(0.500000,0.500000,0.500000,0.500000)),vec4(0.0),vec4(1.0))).z;
r0.y = ((-((r0).yyyy)+(r0).zzzz)).y;
r0.x = ((vec4(1.000000,1.000000,1.000000,1.000000)/(r0).xxxx)).x;
r0.x = (clamp(((r0).xxxx*(r0).yyyy),vec4(0.0),vec4(1.0))).x;
r0.y = (((r0).xxxx*vec4(-2.000000,-2.000000,-2.000000,-2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).y;
r0.x = (((r0).xxxx*(r0).xxxx)).x;
r0.x = (((r0).yyyy*(r0).xxxx+vec4(-0.167000,-0.167000,-0.167000,-0.167000))).x;
r0.x = (clamp(((r0).xxxx*vec4(8.771931,8.771931,8.771931,8.771931)),vec4(0.0),vec4(1.0))).x;
r0.y = (((r0).xxxx*vec4(-2.000000,-2.000000,-2.000000,-2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).y;
r0.x = (((r0).xxxx*(r0).xxxx)).x;
r0.x = (((r0).xxxx*(r0).yyyy)).x;
r0.x = (((r0).xxxx*(vec4(p_Main_Alpha,p_Detail_Noise_Speed,0.0,0.0)).xxxx)).x;
o0.w = (((r0).xxxx*(v3).wwww)).w;
ALBEDO=o0.rgb;
ALPHA=o0.a;
}
