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
uniform float p_Color_Noise_Scale;
uniform float p_Color_Noise_Speed;
uniform float p_Color_Smoothstep;
uniform float p_Color_Smoothstep_Bler;
uniform float p_Detail_Noise_Speed;
uniform float p_MASK_Direction;
uniform float p_MASK_Smooth;
uniform float p_MASK_Smooth_Bler;
uniform sampler2D p_MASK_Texture : source_color, filter_linear_mipmap, repeat_enable;
uniform float p_Main_Alpha;
uniform float p_Main_Smoothstep;
uniform float p_Main_Smoothstep_Bler;
uniform vec2 p_Tiling;
uniform float p_sub_Color_Noise_Scale;
uniform float p_sub_Color_Noise_Speed;
uniform float p_sub_Detail_Noise_Speed;
uniform vec4 p_sub_MASK_Channel_Selection;
uniform sampler2D p_sub_MASK_Texture : source_color, filter_linear_mipmap, repeat_enable;
uniform float p_sub_MASK_Texture_Speed;
uniform float p_use_sub_MASK_Texture;
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
vec4 r9=vec4(0.0);
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
r2.xyz = ((-((v1).yxyy)+vec4(1.000000,1.000000,1.000000,0.000000))).xyz;
r3.xy = (((r2).yzyy*vec4(10.000000,10.000000,0.000000,0.000000))).xy;
r2.x = (clamp(((r2).xxxx*vec4(-0.500000,-0.500000,-0.500000,-0.500000)+vec4(1.000000,1.000000,1.000000,1.000000)),vec4(0.0),vec4(1.0))).x;
r4.xz = (vec4(0.0,0.0,0.0,0.0)).xz;
r4.yw = (((vec4((vec4(md_time,sin(md_time),cos(md_time),0.0)).x,(vec4(md_time,sin(md_time),cos(md_time),0.0)).y,(vec4(md_time,sin(md_time),cos(md_time),0.0)).z,(vec4(md_time,sin(md_time),cos(md_time),0.0)).w)).xxxx*vec4(0.000000,0.100000,0.000000,0.100000))).yw;
r4.xyzw = ((-((r4).xyzw)+(v1).zwzw)).xyzw;
r4.xyzw = (((r4).xyzw+vec4(1.000000,1.000000,1.000000,1.000000))).xyzw;
r2.zw = (((v1).xxxy+vec4(0.000000,0.000000,-0.500000,-0.500000))).zw;
r4.xyzw = (((v1).xyxy*(vec4(p_Tiling.x,p_Tiling.y,p_Detail_Noise_Speed,p_sub_Detail_Noise_Speed)).xyxy+(r4).xyzw)).xyzw;
r3.zw = (((vec4((vec4(md_time,sin(md_time),cos(md_time),0.0)).x,(vec4(md_time,sin(md_time),cos(md_time),0.0)).y,(vec4(md_time,sin(md_time),cos(md_time),0.0)).z,(vec4(md_time,sin(md_time),cos(md_time),0.0)).w)).xxxx+vec4(0.000000,0.000000,5.000000,10.000000))).zw;
r5.xyzw = (((r4).zwzw*vec4(3.000000,3.000000,30.000000,30.000000))).xyzw;
r6.xyzw = (floor((r5).xyzw)).xyzw;
r5.xyzw = (fract((r5).xyzw)).xyzw;
r7.xy = (vec4(0.0,8.000000,0.0,0.0)).xy;
r0.w = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).w;
for (int md_loop_0=0; md_loop_0<1024; md_loop_0++) {
r1.w = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r0).wwww))))).w;
if (any(notEqual((r1).wwww,vec4(0.0)))) { break; }
r8.y = (vec4(floatBitsToInt((r0).wwww))).y;
r7.zw = ((r7).xxxy).zw;
r1.w = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).w;
for (int md_loop_1=0; md_loop_1<1024; md_loop_1++) {
r8.z = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r1).wwww))))).z;
if (any(notEqual((r8).zzzz,vec4(0.0)))) { break; }
r8.x = (vec4(floatBitsToInt((r1).wwww))).x;
r8.zw = (((r6).xxxy+(r8).xxxy)).zw;
r9.x = (vec4(dot(((r8).zwzz).xy,(vec4(15.270000,99.410004,0.000000,0.000000)).xy))).x;
r8.z = (vec4(dot(((r8).zwzz).xy,(vec4(47.630001,89.980003,0.000000,0.000000)).xy))).z;
r9.y = (sin((r9).xxxx)).y;

r9.x = (sin((r8).zzzz)).x;

r8.zw = (fract((r9).xxxy)).zw;
r8.zw = (((r3).zzzz*(r8).zzzw)).zw;
r9.x = (sin((r8).zzzz)).x;


r9.y = (cos((r8).wwww)).y;
r8.xz = (((r9).xxyx*vec4(0.500000,0.000000,0.500000,0.000000)+(r8).xxyx)).xz;
r8.xz = ((-((r5).xxyx)+(r8).xxzx)).xz;
r8.xz = (((r8).xxzx+vec4(0.500000,0.000000,0.500000,0.000000))).xz;
r8.x = (vec4(dot(((r8).xzxx).xy,((r8).xzxx).xy))).x;
r8.x = (sqrt((r8).xxxx)).x;
r8.z = (intBitsToFloat(-ivec4(lessThan((r8).xxxx,(r7).wwww)))).z;
r7.zw = (mix((r7).zzzw,(r8).xxxx,notEqual((r8).zzzz,vec4(0.0)))).zw;
r1.w = (intBitsToFloat(floatBitsToInt((r1).wwww) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).w;
}
r7.xy = ((r7).zwzz).xy;
r0.w = (intBitsToFloat(floatBitsToInt((r0).wwww) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).w;
}
r5.xy = (((r5).zwzz*(r5).zwzz)).xy;
r5.zw = ((-((r5).zzzw)*vec4(0.000000,0.000000,2.000000,2.000000)+vec4(0.000000,0.000000,3.000000,3.000000))).zw;
r5.xy = (((r5).zwzz*(r5).xyxx)).xy;
r8.xyzw = (((r6).zwzw+vec4(1.000000,0.000000,0.000000,1.000000))).xyzw;
r5.zw = (((r6).zzzw+vec4(0.000000,0.000000,1.000000,1.000000))).zw;
r0.w = (vec4(dot(((r6).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r0.w = (sin((r0).wwww)).w;

r0.w = (((r0).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r0.w = (fract((r0).wwww)).w;
r1.w = (vec4(dot(((r8).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.w = (fract((r1).wwww)).w;
r3.z = (vec4(dot(((r8).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r3.z = (sin((r3).zzzz)).z;

r3.z = (((r3).zzzz*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).z;
r3.z = (fract((r3).zzzz)).z;
r5.z = (vec4(dot(((r5).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r5.z = (sin((r5).zzzz)).z;

r5.z = (((r5).zzzz*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).z;
r5.z = (fract((r5).zzzz)).z;
r1.xyzw = ((-((r0).xyzw)+(r1).xyzw)).xyzw;
r0.w = (((r5).xxxx*(r1).wwww+(r0).wwww)).w;
r1.w = ((-((r3).zzzz)+(r5).zzzz)).w;
r1.w = (((r5).xxxx*(r1).wwww+(r3).zzzz)).w;
r1.w = ((-((r0).wwww)+(r1).wwww)).w;
r0.w = (((r5).yyyy*(r1).wwww+(r0).wwww)).w;
r4.xyzw = (((r4).xyzw*vec4(15.000000,15.000000,7.500000,7.500000))).xyzw;
r5.xyzw = (floor((r4).xyzw)).xyzw;
r4.xyzw = (fract((r4).xyzw)).xyzw;
r6.xyzw = (((r4).xyzw*(r4).xyzw)).xyzw;
r4.xyzw = ((-((r4).xyzw)*vec4(2.000000,2.000000,2.000000,2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).xyzw;
r4.xyzw = (((r4).xyzw*(r6).xyzw)).xyzw;
r6.xyzw = (((r5).xyxy+vec4(1.000000,0.000000,0.000000,1.000000))).xyzw;
r8.xyzw = (((r5).xyzw+vec4(1.000000,1.000000,1.000000,0.000000))).xyzw;
r1.w = (vec4(dot(((r5).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.w = (fract((r1).wwww)).w;
r3.z = (vec4(dot(((r6).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r3.z = (sin((r3).zzzz)).z;

r3.z = (((r3).zzzz*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).z;
r3.z = (fract((r3).zzzz)).z;
r5.x = (vec4(dot(((r6).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).x;
r5.x = (sin((r5).xxxx)).x;

r5.y = (vec4(dot(((r8).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r5.y = (sin((r5).yyyy)).y;

r5.xy = (((r5).xyxx*vec4(43758.546875,43758.546875,0.000000,0.000000))).xy;
r5.xy = (fract((r5).xyxx)).xy;
r3.z = ((-((r1).wwww)+(r3).zzzz)).z;
r1.w = (((r4).xxxx*(r3).zzzz+(r1).wwww)).w;
r3.z = ((-((r5).xxxx)+(r5).yyyy)).z;
r3.z = (((r4).xxxx*(r3).zzzz+(r5).xxxx)).z;
r3.z = ((-((r1).wwww)+(r3).zzzz)).z;
r1.w = (((r4).yyyy*(r3).zzzz+(r1).wwww)).w;
r1.w = (((r1).wwww*vec4(0.250000,0.250000,0.250000,0.250000))).w;
r0.w = (((r0).wwww*vec4(0.125000,0.125000,0.125000,0.125000)+(r1).wwww)).w;
r6.xyzw = (((r5).zwzw+vec4(0.000000,1.000000,1.000000,1.000000))).xyzw;
r1.w = (vec4(dot(((r5).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.w = (fract((r1).wwww)).w;
r3.z = (vec4(dot(((r8).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r3.z = (sin((r3).zzzz)).z;

r3.z = (((r3).zzzz*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).z;
r3.z = (fract((r3).zzzz)).z;
r4.x = (vec4(dot(((r6).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).x;
r4.x = (sin((r4).xxxx)).x;

r4.y = (vec4(dot(((r6).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r4.y = (sin((r4).yyyy)).y;

r4.xy = (((r4).xyxx*vec4(43758.546875,43758.546875,0.000000,0.000000))).xy;
r4.xy = (fract((r4).xyxx)).xy;
r3.z = ((-((r1).wwww)+(r3).zzzz)).z;
r1.w = (((r4).zzzz*(r3).zzzz+(r1).wwww)).w;
r3.z = ((-((r4).xxxx)+(r4).yyyy)).z;
r3.z = (((r4).zzzz*(r3).zzzz+(r4).xxxx)).z;
r3.z = ((-((r1).wwww)+(r3).zzzz)).z;
r1.w = (((r4).wwww*(r3).zzzz+(r1).wwww)).w;
r0.w = (((r1).wwww*vec4(0.500000,0.500000,0.500000,0.500000)+(r0).wwww)).w;
r0.w = (((r0).wwww*(r7).xxxx)).w;
r2.y = (((r2).xxxx*(r0).wwww)).y;
r2.xy = (((r2).xyxx*vec4(0.000000,0.500000,0.000000,0.000000)+(v1).xyxx)).xy;
r4.y = (((vec4((vec4(md_time,sin(md_time),cos(md_time),0.0)).x,(vec4(md_time,sin(md_time),cos(md_time),0.0)).y,(vec4(md_time,sin(md_time),cos(md_time),0.0)).z,(vec4(md_time,sin(md_time),cos(md_time),0.0)).w)).xxxx*(vec4(p_Color_Smoothstep,p_Color_Smoothstep_Bler,p_Color_Noise_Scale,p_Color_Noise_Speed)).wwww)).y;
r4.xz = (vec4(0.0,0.0,0.0,0.0)).xz;
r4.xy = ((-((r4).xyxx)+(v1).zwzz)).xy;
r4.xy = (((r4).xyxx+vec4(1.000000,1.000000,0.000000,0.000000))).xy;
r4.xy = (((r2).xyxx*(vec4(p_Tiling.x,p_Tiling.y,p_Detail_Noise_Speed,p_sub_Detail_Noise_Speed)).xyxx+(r4).xyxx)).xy;
r4.xy = (((r4).xyxx*(vec4(p_Color_Smoothstep,p_Color_Smoothstep_Bler,p_Color_Noise_Scale,p_Color_Noise_Speed)).zzzz)).xy;
r5.xy = (floor((r4).xyxx)).xy;
r4.xy = (fract((r4).xyxx)).xy;
r5.zw = (vec4(0.0,0.0,0.0,8.000000)).zw;
r0.w = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).w;
for (int md_loop_2=0; md_loop_2<1024; md_loop_2++) {
r1.w = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r0).wwww))))).w;
if (any(notEqual((r1).wwww,vec4(0.0)))) { break; }
r6.y = (vec4(floatBitsToInt((r0).wwww))).y;
r6.zw = ((r5).zzzw).zw;
r1.w = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).w;
for (int md_loop_3=0; md_loop_3<1024; md_loop_3++) {
r3.z = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r1).wwww))))).z;
if (any(notEqual((r3).zzzz,vec4(0.0)))) { break; }
r6.x = (vec4(floatBitsToInt((r1).wwww))).x;
r7.xy = (((r5).xyxx+(r6).xyxx)).xy;
r3.z = (vec4(dot(((r7).xyxx).xy,(vec4(15.270000,99.410004,0.000000,0.000000)).xy))).z;
r7.x = (vec4(dot(((r7).xyxx).xy,(vec4(47.630001,89.980003,0.000000,0.000000)).xy))).x;
r8.y = (sin((r3).zzzz)).y;

r8.x = (sin((r7).xxxx)).x;

r7.xy = (fract((r8).xyxx)).xy;
r7.xy = (((r7).xyxx*vec4(10.000000,10.000000,0.000000,0.000000))).xy;
r8.x = (sin((r7).xxxx)).x;


r8.y = (cos((r7).yyyy)).y;
r7.xy = (((r8).xyxx*vec4(0.500000,0.500000,0.000000,0.000000)+(r6).xyxx)).xy;
r7.xy = ((-((r4).xyxx)+(r7).xyxx)).xy;
r7.xy = (((r7).xyxx+vec4(0.500000,0.500000,0.000000,0.000000))).xy;
r3.z = (vec4(dot(((r7).xyxx).xy,((r7).xyxx).xy))).z;
r3.z = (sqrt((r3).zzzz)).z;
r6.x = (intBitsToFloat(-ivec4(lessThan((r3).zzzz,(r6).wwww)))).x;
r6.zw = (mix((r6).zzzw,(r3).zzzz,notEqual((r6).xxxx,vec4(0.0)))).zw;
r1.w = (intBitsToFloat(floatBitsToInt((r1).wwww) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).w;
}
r5.zw = ((r6).zzzw).zw;
r0.w = (intBitsToFloat(floatBitsToInt((r0).wwww) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).w;
}
r6.yw = (((vec4((vec4(md_time,sin(md_time),cos(md_time),0.0)).x,(vec4(md_time,sin(md_time),cos(md_time),0.0)).y,(vec4(md_time,sin(md_time),cos(md_time),0.0)).z,(vec4(md_time,sin(md_time),cos(md_time),0.0)).w)).xxxx*(vec4(p_sub_Color_Noise_Scale,p_sub_Color_Noise_Speed,0.0,0.0)).yyyy)).yw;
r6.xz = (vec4(0.0,0.0,0.0,0.0)).xz;
r6.xyzw = ((-((r6).xyzw)+(v1).zwzw)).xyzw;
r6.xyzw = (((v1).xyxy*(vec4(p_Tiling.x,p_Tiling.y,p_Detail_Noise_Speed,p_sub_Detail_Noise_Speed)).xyxy+(r6).xyzw)).xyzw;
r6.xyzw = (((r6).xyzw+vec4(1.000000,1.000000,1.000000,1.000000))).xyzw;
r4.xy = (((r6).zwzz*(vec4(p_sub_Color_Noise_Scale,p_sub_Color_Noise_Speed,0.0,0.0)).xxxx)).xy;
r5.xy = (floor((r4).xyxx)).xy;
r4.xy = (fract((r4).xyxx)).xy;
r7.xy = (((r4).xyxx*(r4).xyxx)).xy;
r4.xy = ((-((r4).xyxx)*vec4(2.000000,2.000000,0.000000,0.000000)+vec4(3.000000,3.000000,0.000000,0.000000))).xy;
r4.xy = (((r4).xyxx*(r7).xyxx)).xy;
r7.xyzw = (((r5).xyxy+vec4(1.000000,0.000000,0.000000,1.000000))).xyzw;
r8.xy = (((r5).xyxx+vec4(1.000000,1.000000,0.000000,0.000000))).xy;
r0.w = (vec4(dot(((r5).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r0.w = (sin((r0).wwww)).w;

r0.w = (((r0).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r0.w = (fract((r0).wwww)).w;
r1.w = (vec4(dot(((r7).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.w = (fract((r1).wwww)).w;
r3.z = (vec4(dot(((r7).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r3.z = (sin((r3).zzzz)).z;

r3.z = (((r3).zzzz*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).z;
r3.z = (fract((r3).zzzz)).z;
r5.x = (vec4(dot(((r8).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).x;
r5.x = (sin((r5).xxxx)).x;

r5.x = (((r5).xxxx*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).x;
r5.x = (fract((r5).xxxx)).x;
r1.w = ((-((r0).wwww)+(r1).wwww)).w;
r0.w = (((r4).xxxx*(r1).wwww+(r0).wwww)).w;
r1.w = ((-((r3).zzzz)+(r5).xxxx)).w;
r1.w = (((r4).xxxx*(r1).wwww+(r3).zzzz)).w;
r1.w = ((-((r0).wwww)+(r1).wwww)).w;
r0.w = (((r4).yyyy*(r1).wwww+(r0).wwww)).w;
r7.xyzw = (((vec4(p_sub_Color_Noise_Scale,p_sub_Color_Noise_Speed,0.0,0.0)).xxxx*vec4(0.500000,0.500000,0.250000,0.250000))).xyzw;
r6.xyzw = (((r6).xyzw*(r7).xyzw)).xyzw;
r7.xyzw = (floor((r6).xyzw)).xyzw;
r6.xyzw = (fract((r6).xyzw)).xyzw;
r8.xyzw = (((r6).xyzw*(r6).xyzw)).xyzw;
r6.xyzw = ((-((r6).xyzw)*vec4(2.000000,2.000000,2.000000,2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).xyzw;
r6.xyzw = (((r6).xyzw*(r8).xyzw)).xyzw;
r8.xyzw = (((r7).xyxy+vec4(1.000000,0.000000,0.000000,1.000000))).xyzw;
r9.xyzw = (((r7).xyzw+vec4(1.000000,1.000000,1.000000,0.000000))).xyzw;
r1.w = (vec4(dot(((r7).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.w = (fract((r1).wwww)).w;
r3.z = (vec4(dot(((r8).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r3.z = (sin((r3).zzzz)).z;

r3.z = (((r3).zzzz*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).z;
r3.z = (fract((r3).zzzz)).z;
r4.x = (vec4(dot(((r8).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).x;
r4.x = (sin((r4).xxxx)).x;

r4.y = (vec4(dot(((r9).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r4.y = (sin((r4).yyyy)).y;

r4.xy = (((r4).xyxx*vec4(43758.546875,43758.546875,0.000000,0.000000))).xy;
r4.xy = (fract((r4).xyxx)).xy;
r3.z = ((-((r1).wwww)+(r3).zzzz)).z;
r1.w = (((r6).xxxx*(r3).zzzz+(r1).wwww)).w;
r3.z = ((-((r4).xxxx)+(r4).yyyy)).z;
r3.z = (((r6).xxxx*(r3).zzzz+(r4).xxxx)).z;
r3.z = ((-((r1).wwww)+(r3).zzzz)).z;
r1.w = (((r6).yyyy*(r3).zzzz+(r1).wwww)).w;
r1.w = (((r1).wwww*vec4(0.250000,0.250000,0.250000,0.250000))).w;
r0.w = (((r0).wwww*vec4(0.125000,0.125000,0.125000,0.125000)+(r1).wwww)).w;
r8.xyzw = (((r7).zwzw+vec4(0.000000,1.000000,1.000000,1.000000))).xyzw;
r1.w = (vec4(dot(((r7).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.w = (fract((r1).wwww)).w;
r3.z = (vec4(dot(((r9).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r3.z = (sin((r3).zzzz)).z;

r3.z = (((r3).zzzz*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).z;
r3.z = (fract((r3).zzzz)).z;
r4.x = (vec4(dot(((r8).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).x;
r4.x = (sin((r4).xxxx)).x;

r4.y = (vec4(dot(((r8).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r4.y = (sin((r4).yyyy)).y;

r4.xy = (((r4).xyxx*vec4(43758.546875,43758.546875,0.000000,0.000000))).xy;
r4.xy = (fract((r4).xyxx)).xy;
r3.z = ((-((r1).wwww)+(r3).zzzz)).z;
r1.w = (((r6).zzzz*(r3).zzzz+(r1).wwww)).w;
r3.z = ((-((r4).xxxx)+(r4).yyyy)).z;
r3.z = (((r6).zzzz*(r3).zzzz+(r4).xxxx)).z;
r3.z = ((-((r1).wwww)+(r3).zzzz)).z;
r1.w = (((r6).wwww*(r3).zzzz+(r1).wwww)).w;
r0.w = (((r1).wwww*vec4(0.500000,0.500000,0.500000,0.500000)+(r0).wwww)).w;
r0.w = (((r5).zzzz*(r0).wwww+(v1).yyyy)).w;
r0.w = (clamp(((r0).wwww*vec4(0.500000,0.500000,0.500000,0.500000)),vec4(0.0),vec4(1.0))).w;
r0.w = (((r0).wwww+-((vec4(p_Color_Smoothstep,p_Color_Smoothstep_Bler,p_Color_Noise_Scale,p_Color_Noise_Speed)).xxxx))).w;
r1.w = ((vec4(1.000000,1.000000,1.000000,1.000000)/(vec4(p_Color_Smoothstep,p_Color_Smoothstep_Bler,p_Color_Noise_Scale,p_Color_Noise_Speed)).yyyy)).w;
r0.w = (clamp(((r0).wwww*(r1).wwww),vec4(0.0),vec4(1.0))).w;
r1.w = (((r0).wwww*vec4(-2.000000,-2.000000,-2.000000,-2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).w;
r0.w = (((r0).wwww*(r0).wwww)).w;
r0.w = (((r0).wwww*(r1).wwww)).w;
r0.xyz = (((r0).wwww*(r1).xyzx+(r0).xyzx)).xyz;
o0.xyz = (((r0).xyzx*(v3).xyzx)).xyz;
r0.xy = ((-((v2).zwzz)+vec4(1.000000,1.000000,0.000000,0.000000))).xy;
r1.yw = (((vec4((vec4(md_time,sin(md_time),cos(md_time),0.0)).x,(vec4(md_time,sin(md_time),cos(md_time),0.0)).y,(vec4(md_time,sin(md_time),cos(md_time),0.0)).z,(vec4(md_time,sin(md_time),cos(md_time),0.0)).w)).xxxx*(vec4(p_Tiling.x,p_Tiling.y,p_Detail_Noise_Speed,p_sub_Detail_Noise_Speed)).zzzw)).yw;
r1.xz = (vec4(0.0,0.0,0.0,0.0)).xz;
r1.xyzw = ((-((r1).xyzw)+(v1).zwzw)).xyzw;
r0.zw = (((r2).xxxy*(vec4(p_Tiling.x,p_Tiling.y,p_Detail_Noise_Speed,p_sub_Detail_Noise_Speed)).xxxy+(r1).xxxy)).zw;
r0.zw = (((r0).zzzw+vec4(0.000000,0.000000,1.000000,1.000000))).zw;
r0.zw = (((r0).zzzw*vec4(0.000000,0.000000,4.500000,4.500000))).zw;
r1.xy = (floor((r0).zwzz)).xy;
r0.zw = (fract((r0).zzzw)).zw;
r2.xy = (vec4(0.0,8.000000,0.0,0.0)).xy;
r3.z = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).z;
for (int md_loop_4=0; md_loop_4<1024; md_loop_4++) {
r4.x = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r3).zzzz))))).x;
if (any(notEqual((r4).xxxx,vec4(0.0)))) { break; }
r4.y = (vec4(floatBitsToInt((r3).zzzz))).y;
r5.xy = ((r2).xyxx).xy;
r5.z = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).z;
for (int md_loop_5=0; md_loop_5<1024; md_loop_5++) {
r5.w = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r5).zzzz))))).w;
if (any(notEqual((r5).wwww,vec4(0.0)))) { break; }
r4.x = (vec4(floatBitsToInt((r5).zzzz))).x;
r6.xy = (((r1).xyxx+(r4).xyxx)).xy;
r5.w = (vec4(dot(((r6).xyxx).xy,(vec4(15.270000,99.410004,0.000000,0.000000)).xy))).w;
r6.x = (vec4(dot(((r6).xyxx).xy,(vec4(47.630001,89.980003,0.000000,0.000000)).xy))).x;
r7.y = (sin((r5).wwww)).y;

r7.x = (sin((r6).xxxx)).x;

r6.xy = (fract((r7).xyxx)).xy;
r6.xy = (((r3).wwww*(r6).xyxx)).xy;
r7.x = (sin((r6).xxxx)).x;


r7.y = (cos((r6).yyyy)).y;
r6.xy = (((r7).xyxx*vec4(0.500000,0.500000,0.000000,0.000000)+(r4).xyxx)).xy;
r6.xy = ((-((r0).zwzz)+(r6).xyxx)).xy;
r6.xy = (((r6).xyxx+vec4(0.500000,0.500000,0.000000,0.000000))).xy;
r4.x = (vec4(dot(((r6).xyxx).xy,((r6).xyxx).xy))).x;
r4.x = (sqrt((r4).xxxx)).x;
r5.w = (intBitsToFloat(-ivec4(lessThan((r4).xxxx,(r5).yyyy)))).w;
r5.xy = (mix((r5).xyxx,(r4).xxxx,notEqual((r5).wwww,vec4(0.0)))).xy;
r5.z = (intBitsToFloat(floatBitsToInt((r5).zzzz) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).z;
}
r2.xy = ((r5).xyxx).xy;
r3.z = (intBitsToFloat(floatBitsToInt((r3).zzzz) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).z;
}
r0.zw = (((v1).xxxy*(vec4(p_Tiling.x,p_Tiling.y,p_Detail_Noise_Speed,p_sub_Detail_Noise_Speed)).xxxy+(r1).zzzw)).zw;
r0.zw = (((r0).zzzw+vec4(0.000000,0.000000,1.000000,1.000000))).zw;
r1.xyzw = (((r0).zwzw*vec4(12.000000,12.000000,6.000000,6.000000))).xyzw;
r5.xyzw = (floor((r1).xyzw)).xyzw;
r1.xyzw = (fract((r1).xyzw)).xyzw;
r6.xyzw = (((r1).xyzw*(r1).xyzw)).xyzw;
r1.xyzw = ((-((r1).xyzw)*vec4(2.000000,2.000000,2.000000,2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).xyzw;
r1.xyzw = (((r1).xyzw*(r6).xyzw)).xyzw;
r6.xyzw = (((r5).xyxy+vec4(1.000000,0.000000,0.000000,1.000000))).xyzw;
r7.xyzw = (((r5).xyzw+vec4(1.000000,1.000000,1.000000,0.000000))).xyzw;
r2.y = (vec4(dot(((r5).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r2.y = (sin((r2).yyyy)).y;

r2.y = (((r2).yyyy*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).y;
r2.y = (fract((r2).yyyy)).y;
r3.z = (vec4(dot(((r6).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r3.z = (sin((r3).zzzz)).z;

r3.z = (((r3).zzzz*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).z;
r3.w = (vec4(dot(((r6).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r3.w = (sin((r3).wwww)).w;

r3.w = (((r3).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r3.zw = (fract((r3).zzzw)).zw;
r4.x = (vec4(dot(((r7).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).x;
r4.x = (sin((r4).xxxx)).x;

r4.x = (((r4).xxxx*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).x;
r4.x = (fract((r4).xxxx)).x;
r3.z = ((-((r2).yyyy)+(r3).zzzz)).z;
r2.y = (((r1).xxxx*(r3).zzzz+(r2).yyyy)).y;
r3.z = ((-((r3).wwww)+(r4).xxxx)).z;
r1.x = (((r1).xxxx*(r3).zzzz+(r3).wwww)).x;
r1.x = ((-((r2).yyyy)+(r1).xxxx)).x;
r1.x = (((r1).yyyy*(r1).xxxx+(r2).yyyy)).x;
r6.xyzw = (((r5).zwzw+vec4(0.000000,1.000000,1.000000,1.000000))).xyzw;
r1.y = (vec4(dot(((r5).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r1.y = (sin((r1).yyyy)).y;

r1.y = (((r1).yyyy*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).y;
r1.y = (fract((r1).yyyy)).y;
r2.y = (vec4(dot(((r7).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r2.y = (sin((r2).yyyy)).y;

r2.y = (((r2).yyyy*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).y;
r2.y = (fract((r2).yyyy)).y;
r3.z = (vec4(dot(((r6).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r3.z = (sin((r3).zzzz)).z;

r3.z = (((r3).zzzz*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).z;
r3.w = (vec4(dot(((r6).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r3.w = (sin((r3).wwww)).w;

r3.w = (((r3).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r3.zw = (fract((r3).zzzw)).zw;
r2.y = ((-((r1).yyyy)+(r2).yyyy)).y;
r1.y = (((r1).zzzz*(r2).yyyy+(r1).yyyy)).y;
r2.y = ((-((r3).zzzz)+(r3).wwww)).y;
r1.z = (((r1).zzzz*(r2).yyyy+(r3).zzzz)).z;
r1.z = ((-((r1).yyyy)+(r1).zzzz)).z;
r1.y = (((r1).wwww*(r1).zzzz+(r1).yyyy)).y;
r1.y = (((r1).yyyy*vec4(0.250000,0.250000,0.250000,0.250000))).y;
r1.x = (((r1).xxxx*vec4(0.125000,0.125000,0.125000,0.125000)+(r1).yyyy)).x;
r0.zw = (((r0).zzzw*vec4(0.000000,0.000000,3.000000,3.000000))).zw;
r1.yz = (floor((r0).zzwz)).yz;
r0.zw = (fract((r0).zzzw)).zw;
r3.zw = (((r0).zzzw*(r0).zzzw)).zw;
r0.zw = ((-((r0).zzzw)*vec4(0.000000,0.000000,2.000000,2.000000)+vec4(0.000000,0.000000,3.000000,3.000000))).zw;
r0.zw = (((r0).zzzw*(r3).zzzw)).zw;
r5.xyzw = (((r1).yzyz+vec4(1.000000,0.000000,0.000000,1.000000))).xyzw;
r3.zw = (((r1).yyyz+vec4(0.000000,0.000000,1.000000,1.000000))).zw;
r1.y = (vec4(dot(((r1).yzyy).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
r1.y = (sin((r1).yyyy)).y;

r1.y = (((r1).yyyy*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).y;
r1.z = (vec4(dot(((r5).xyxx).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).z;
r1.z = (sin((r1).zzzz)).z;

r1.z = (((r1).zzzz*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).z;
r1.w = (vec4(dot(((r5).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).w;
r1.w = (sin((r1).wwww)).w;

r1.w = (((r1).wwww*vec4(43758.546875,43758.546875,43758.546875,43758.546875))).w;
r1.yzw = (fract((r1).yyzw)).yzw;
r2.y = (vec4(dot(((r3).zwzz).xy,(vec4(12.989800,78.233002,0.000000,0.000000)).xy))).y;
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
r0.z = (((r0).zzzz*(r2).xxxx)).z;
r3.xy = (clamp((r3).xyxx,vec4(0.0),vec4(1.0))).xy;
r1.xy = (clamp(((v1).xyxx*vec4(10.000000,10.000000,0.000000,0.000000)),vec4(0.0),vec4(1.0))).xy;
r1.xy = (((r1).xyxx*(r3).xyxx)).xy;
r0.w = (((r1).yyyy*(r1).xxxx)).w;
r1.x = (((vec4(p_MASK_Direction,p_MASK_Smooth,p_MASK_Smooth_Bler,p_use_sub_MASK_Texture)).xxxx*vec4(0.017453,0.017453,0.017453,0.017453))).x;
vec4 md_sincos_0=(r1).xxxx;
r1.x = (sin(md_sincos_0)).x;
r3.x = (cos(md_sincos_0)).x;
r3.yz = ((r1).xxxx).yz;
r1.xyz = (((r3).xyzx*vec4(0.500000,-0.500000,0.500000,0.000000)+vec4(0.500000,0.500000,0.500000,0.000000))).xyz;
r1.xyz = (((r1).xyzx*vec4(2.000000,2.000000,2.000000,0.000000)+vec4(-1.000000,-1.000000,-1.000000,0.000000))).xyz;
r2.x = (vec4(dot(((r2).zwzz).xy,((r1).xzxx).xy))).x;
r2.y = (vec4(dot(((r2).wzww).xy,((r1).xyxx).xy))).y;
r1.xy = (((r2).xyxx+vec4(0.500000,0.500000,0.000000,0.000000))).xy;
r1.xyzw = ((texture(p_MASK_Texture,vec2((((r1).xyxx).xy).x,1.0-(((r1).xyxx).xy).y),((vec4((vec2(0.0)).x,(vec2(0.0)).y,0.0,0.0)).xxxx).x)).xyzw).xyzw;
r4.w = (((vec4((vec4(md_time,sin(md_time),cos(md_time),0.0)).x,(vec4(md_time,sin(md_time),cos(md_time),0.0)).y,(vec4(md_time,sin(md_time),cos(md_time),0.0)).z,(vec4(md_time,sin(md_time),cos(md_time),0.0)).w)).xxxx*(vec4(p_sub_MASK_Texture_Speed,0.0,0.0,0.0)).xxxx)).w;
r1.yz = ((-((r4).zzwz)+(v1).zzwz)).yz;
r1.yz = (((r1).yyzy+(v1).xxyx)).yz;
r1.yz = (((r1).yyzy+vec4(0.000000,1.000000,1.000000,0.000000))).yz;
r2.xyzw = ((texture(p_sub_MASK_Texture,vec2((((r1).yzyy).xy).x,1.0-(((r1).yzyy).xy).y),((vec4((vec2(0.0)).x,(vec2(0.0)).y,0.0,0.0)).xxxx).x)).xyzw).xyzw;
r1.yzw = (((r2).xxyw*(vec4(p_sub_MASK_Channel_Selection.x,p_sub_MASK_Channel_Selection.y,p_sub_MASK_Channel_Selection.z,p_sub_MASK_Channel_Selection.w)).xxyw)).yzw;
r1.y = (((r1).zzzz+(r1).yyyy)).y;
r1.y = (((r2).zzzz*(vec4(p_sub_MASK_Channel_Selection.x,p_sub_MASK_Channel_Selection.y,p_sub_MASK_Channel_Selection.z,p_sub_MASK_Channel_Selection.w)).zzzz+(r1).yyyy)).y;
r1.z = (min((r1).wwww,vec4(1.000000,1.000000,1.000000,1.000000))).z;
r1.y = (((r1).zzzz+(r1).yyyy)).y;
r1.zw = ((-((r1).xxxy)+vec4(0.000000,0.000000,1.000000,1.000000))).zw;
r1.z = (((r1).zzzz+(r1).zzzz)).z;
r1.z = ((-((r1).zzzz)*(r1).wwww+vec4(1.000000,1.000000,1.000000,1.000000))).z;
r1.y = (vec4(dot(((r1).yyyy).xy,((r1).xxxx).xy))).y;
r1.w = (intBitsToFloat(-ivec4(greaterThanEqual(vec4(0.500000,0.500000,0.500000,0.500000),(r1).xxxx)))).w;
r2.x = (intBitsToFloat(floatBitsToInt((r1).wwww) & floatBitsToInt(vec4(uintBitsToFloat(1065353216u),uintBitsToFloat(1065353216u),uintBitsToFloat(1065353216u),uintBitsToFloat(1065353216u))))).x;
r1.w = (mix(vec4(1.000000,1.000000,1.000000,1.000000),vec4(0.0,0.0,0.0,0.0),notEqual((r1).wwww,vec4(0.0)))).w;
r1.z = (((r1).zzzz*(r1).wwww)).z;
r1.y = (((r1).yyyy*(r2).xxxx+(r1).zzzz)).y;
r1.z = (intBitsToFloat(-ivec4(notEqual(vec4(0.000000,0.000000,0.000000,0.000000),(vec4(p_MASK_Direction,p_MASK_Smooth,p_MASK_Smooth_Bler,p_use_sub_MASK_Texture)).wwww)))).z;
r1.x = (clamp(mix((r1).xxxx,(r1).yyyy,notEqual((r1).zzzz,vec4(0.0))),vec4(0.0),vec4(1.0))).x;
r1.x = (((r1).xxxx+-((vec4(p_MASK_Direction,p_MASK_Smooth,p_MASK_Smooth_Bler,p_use_sub_MASK_Texture)).yyyy))).x;
r1.y = ((vec4(1.000000,1.000000,1.000000,1.000000)/(vec4(p_MASK_Direction,p_MASK_Smooth,p_MASK_Smooth_Bler,p_use_sub_MASK_Texture)).zzzz)).y;
r1.x = (clamp(((r1).yyyy*(r1).xxxx),vec4(0.0),vec4(1.0))).x;
r1.y = (((r1).xxxx*vec4(-2.000000,-2.000000,-2.000000,-2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).y;
r1.x = (((r1).xxxx*(r1).xxxx)).x;
r1.x = (((r1).xxxx*(r1).yyyy)).x;
r0.z = (((r0).zzzz*(r0).wwww+(r1).xxxx)).z;
r0.z = (clamp(((r0).zzzz*vec4(0.500000,0.500000,0.500000,0.500000)),vec4(0.0),vec4(1.0))).z;
r0.y = ((-((r0).yyyy)+(r0).zzzz)).y;
r0.x = ((vec4(1.000000,1.000000,1.000000,1.000000)/(r0).xxxx)).x;
r0.x = (clamp(((r0).xxxx*(r0).yyyy),vec4(0.0),vec4(1.0))).x;
r0.y = (((r0).xxxx*vec4(-2.000000,-2.000000,-2.000000,-2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).y;
r0.x = (((r0).xxxx*(r0).xxxx)).x;
r0.x = (((r0).yyyy*(r0).xxxx+-((vec4(p_Main_Alpha,p_Main_Smoothstep,p_Main_Smoothstep_Bler,0.0)).yyyy))).x;
r0.y = ((vec4(1.000000,1.000000,1.000000,1.000000)/(vec4(p_Main_Alpha,p_Main_Smoothstep,p_Main_Smoothstep_Bler,0.0)).zzzz)).y;
r0.x = (clamp(((r0).yyyy*(r0).xxxx),vec4(0.0),vec4(1.0))).x;
r0.y = (((r0).xxxx*vec4(-2.000000,-2.000000,-2.000000,-2.000000)+vec4(3.000000,3.000000,3.000000,3.000000))).y;
r0.x = (((r0).xxxx*(r0).xxxx)).x;
r0.x = (((r0).xxxx*(r0).yyyy)).x;
r0.x = (((r0).xxxx*(vec4(p_Main_Alpha,p_Main_Smoothstep,p_Main_Smoothstep_Bler,0.0)).xxxx)).x;
o0.w = (((r0).xxxx*(v3).wwww)).w;
ALBEDO=o0.rgb;
ALPHA=o0.a;
}
