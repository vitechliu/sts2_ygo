shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_back;
// 按源 DXBC 前向程序逐指令生成；源文件和散列见迁移清单。
uniform float md_time = 0.0;
uniform float md_near = 0.1;
uniform float md_far = 300.0;
uniform float md_camera_size = 80.0;
const mat4 md_flip = mat4(vec4(1,0,0,0),vec4(0,1,0,0),vec4(0,0,-1,0),vec4(0,0,0,1));
uniform float p_Alpha;
uniform float p_Dust_Scale;
uniform float p_Tiling_Switch;
uniform float p_White_Ratio;
varying vec4 md_vary_1;
varying vec4 md_vary_2;
varying vec4 md_vary_3;
varying vec4 md_vary_4;
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
vec4 r0=vec4(0.0);
vec4 r1=vec4(0.0);
vec4 v0=vec4(0.0);
vec4 v1=vec4(0.0);
vec4 v3=vec4(0.0);
vec4 v4=vec4(0.0);
v0=vec4(VERTEX*vec3(1,1,-1),1.0);
v1=vec4(NORMAL*vec3(1,1,-1),0.0);
v3=vec4(UV,UV2);
v4=COLOR;
r0.xyz = (((v0).yyyy*(vec4(md_model[1].x,md_model[1].y,md_model[1].z,md_model[1].w)).xyzx)).xyz;
r0.xyz = (((vec4(md_model[0].x,md_model[0].y,md_model[0].z,md_model[0].w)).xyzx*(v0).xxxx+(r0).xyzx)).xyz;
r0.xyz = (((vec4(md_model[2].x,md_model[2].y,md_model[2].z,md_model[2].w)).xyzx*(v0).zzzz+(r0).xyzx)).xyz;
r0.xyz = (((r0).xyzx+(vec4(md_model[3].x,md_model[3].y,md_model[3].z,md_model[3].w)).xyzx)).xyz;
r1.xyzw = (((r0).yyyy*(vec4(md_vp[1].x,md_vp[1].y,md_vp[1].z,md_vp[1].w)).xyzw)).xyzw;
r1.xyzw = (((vec4(md_vp[0].x,md_vp[0].y,md_vp[0].z,md_vp[0].w)).xyzw*(r0).xxxx+(r1).xyzw)).xyzw;
r1.xyzw = (((vec4(md_vp[2].x,md_vp[2].y,md_vp[2].z,md_vp[2].w)).xyzw*(r0).zzzz+(r1).xyzw)).xyzw;
o3.xyz = ((r0).xyzx).xyz;
o0.xyzw = (((r1).xyzw+(vec4(md_vp[3].x,md_vp[3].y,md_vp[3].z,md_vp[3].w)).xyzw)).xyzw;
o1.xyzw = ((v3).xyzw).xyzw;
o2.xyzw = ((v4).xyzw).xyzw;
r0.x = (vec4(dot(((v1).xyzx).xyz,((vec4(md_inverse_model[0].x,md_inverse_model[0].y,md_inverse_model[0].z,md_inverse_model[0].w)).xyzx).xyz))).x;
r0.y = (vec4(dot(((v1).xyzx).xyz,((vec4(md_inverse_model[1].x,md_inverse_model[1].y,md_inverse_model[1].z,md_inverse_model[1].w)).xyzx).xyz))).y;
r0.z = (vec4(dot(((v1).xyzx).xyz,((vec4(md_inverse_model[2].x,md_inverse_model[2].y,md_inverse_model[2].z,md_inverse_model[2].w)).xyzx).xyz))).z;
r0.w = (vec4(dot(((r0).xyzx).xyz,((r0).xyzx).xyz))).w;
r0.w = (max((r0).wwww,vec4(0.000000,0.000000,0.000000,0.000000))).w;
r0.w = (inversesqrt((r0).wwww)).w;
o4.xyz = (((r0).wwww*(r0).xyzx)).xyz;
POSITION=o0;
md_vary_1=o1;
md_vary_2=o2;
md_vary_3=o3;
md_vary_4=o4;
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
vec4 v1=vec4(0.0);
vec4 v2=vec4(0.0);
v1=md_vary_1;
v2=md_vary_2;
r0.xyzw = (((v1).xyzw+vec4(0.500000,0.500000,1.000000,1.000000))).xyzw;
r1.xy = (((r0).xyxx*(vec4(p_Alpha,p_Dust_Scale,p_White_Ratio,p_Tiling_Switch)).zzzz)).xy;
r0.xy = (((r0).xyxx*(vec4(p_Alpha,p_Dust_Scale,p_White_Ratio,p_Tiling_Switch)).zzzz+vec4(0.250000,0.250000,0.000000,0.000000))).xy;
r0.xy = (fract((r0).xyxx)).xy;
r0.xy = (((r0).xyxx+vec4(-0.500000,-0.500000,0.000000,0.000000))).xy;
r0.xy = ((abs((r0).xyxx)*vec4(4.000000,4.000000,0.000000,0.000000)+vec4(-1.000000,-1.000000,0.000000,0.000000))).xy;
r2.xy = (dFdx((r1).xyxx)).xy;
r2.zw = (dFdy((r1).xxxy)).zw;
r1.x = (vec4(dot(((r2).xzxx).xy,((r2).xzxx).xy))).x;
r1.y = (vec4(dot(((r2).ywyy).xy,((r2).ywyy).xy))).y;
r2.xy = (sqrt((r1).xyxx)).xy;
r1.xy = ((vec4(0.350000,0.350000,0.000000,0.000000)/(r2).xyxx)).xy;
r1.z = (max((r2).yyyy,(r2).xxxx)).z;
r1.z = (clamp((-((r1).zzzz)+vec4(1.100000,1.100000,1.100000,1.100000)),vec4(0.0),vec4(1.0))).z;
r1.z = (sqrt((r1).zzzz)).z;
r0.xy = (((r0).xyxx*(r1).xyxx)).xy;
r0.xy = (max((r0).xyxx,vec4(-1.000000,-1.000000,0.000000,0.000000))).xy;
r0.xy = (min((r0).xyxx,vec4(1.000000,1.000000,0.000000,0.000000))).xy;
r0.x = (((r0).xxxx*(r0).yyyy)).x;
r0.x = (((r1).zzzz*(r0).xxxx)).x;
r0.x = (((r0).xxxx*vec4(0.500000,0.500000,0.500000,0.500000)+vec4(0.500000,0.500000,0.500000,0.500000))).x;
r1.xyz = (((r0).xxxx*vec4(0.646850,0.602878,0.622730,0.000000)+vec4(0.263528,0.396226,0.377270,0.000000))).xyz;
o0.xyz = (((r1).xyzx*(v2).xyzx)).xyz;
r0.xy = (((v1).xyxx+(v1).xyxx)).xy;
r0.xy = (fract((r0).xyxx)).xy;
r0.xy = (((r0).xyxx*vec4(2.000000,2.000000,0.000000,0.000000)+vec4(-1.000000,-1.000000,0.000000,0.000000))).xy;
r0.xy = (((r0).xyxx*vec4(1.154701,-1.154701,0.000000,0.000000))).xy;
r1.x = (min(abs((r0).yyyy),abs((r0).xxxx))).x;
r1.y = (max(abs((r0).yyyy),abs((r0).xxxx))).y;
r1.y = ((vec4(1.000000,1.000000,1.000000,1.000000)/(r1).yyyy)).y;
r1.x = (((r1).yyyy*(r1).xxxx)).x;
r1.y = (((r1).xxxx*(r1).xxxx)).y;
r1.z = (((r1).yyyy*vec4(0.020835,0.020835,0.020835,0.020835)+vec4(-0.085133,-0.085133,-0.085133,-0.085133))).z;
r1.z = (((r1).yyyy*(r1).zzzz+vec4(0.180141,0.180141,0.180141,0.180141))).z;
r1.z = (((r1).yyyy*(r1).zzzz+vec4(-0.330299,-0.330299,-0.330299,-0.330299))).z;
r1.y = (((r1).yyyy*(r1).zzzz+vec4(0.999866,0.999866,0.999866,0.999866))).y;
r1.z = (((r1).yyyy*(r1).xxxx)).z;
r1.w = (intBitsToFloat(-ivec4(lessThan(abs((r0).yyyy),abs((r0).xxxx))))).w;
r1.z = (((r1).zzzz*vec4(-2.000000,-2.000000,-2.000000,-2.000000)+vec4(1.570796,1.570796,1.570796,1.570796))).z;
r1.z = (intBitsToFloat(floatBitsToInt((r1).wwww) & floatBitsToInt((r1).zzzz))).z;
r1.x = (((r1).xxxx*(r1).yyyy+(r1).zzzz)).x;
r1.y = (intBitsToFloat(-ivec4(lessThan((r0).yyyy,-((r0).yyyy))))).y;
r1.y = (intBitsToFloat(floatBitsToInt((r1).yyyy) & floatBitsToInt(vec4(uintBitsToFloat(3226013659u),uintBitsToFloat(3226013659u),uintBitsToFloat(3226013659u),uintBitsToFloat(3226013659u))))).y;
r1.x = (((r1).yyyy+(r1).xxxx)).x;
r1.y = (min((r0).yyyy,(r0).xxxx)).y;
r1.z = (max((r0).yyyy,(r0).xxxx)).z;
r1.y = (intBitsToFloat(-ivec4(lessThan((r1).yyyy,-((r1).yyyy))))).y;
r1.z = (intBitsToFloat(-ivec4(greaterThanEqual((r1).zzzz,-((r1).zzzz))))).z;
r1.y = (intBitsToFloat(floatBitsToInt((r1).zzzz) & floatBitsToInt((r1).yyyy))).y;
r1.x = (mix((r1).xxxx,-((r1).xxxx),notEqual((r1).yyyy,vec4(0.0)))).x;
r1.y = (((r1).xxxx*vec4(0.954930,0.954930,0.954930,0.954930)+vec4(0.500000,0.500000,0.500000,0.500000))).y;
r1.y = (floor((r1).yyyy)).y;
r1.x = (((r1).yyyy*vec4(1.047198,1.047198,1.047198,1.047198)+-((r1).xxxx))).x;

r1.x = (cos((r1).xxxx)).x;
r0.x = (vec4(dot(((r0).xyxx).xy,((r0).xyxx).xy))).x;
r0.x = (sqrt((r0).xxxx)).x;
r0.y = (((r0).xxxx*(r1).xxxx)).y;
r0.x = ((-((r1).xxxx)*(r0).xxxx+vec4(1.000000,1.000000,1.000000,1.000000))).x;
r1.x = (dFdx((r0).yyyy)).x;
r0.y = (dFdy((r0).yyyy)).y;
r0.y = ((abs((r0).yyyy)+abs((r1).xxxx))).y;
r0.x = (clamp(((r0).xxxx/(r0).yyyy),vec4(0.0),vec4(1.0))).x;
r1.xy = (((v1).xyxx+vec4(-0.500000,-0.500000,0.000000,0.000000))).xy;
r1.zw = (((v1).xxxy*vec4(0.000000,0.000000,2.000000,2.000000)+(r0).zzzw)).zw;
r1.zw = (fract((r1).zzzw)).zw;
r1.zw = (((r1).zzzw*vec4(0.000000,0.000000,10.000000,10.000000))).zw;
r2.xy = (floor((r1).zwzz)).xy;
r1.zw = (fract((r1).zzzw)).zw;
r3.xy = (-((r1).zwzz)).xy;
r4.z = (vec4(0.500000,0.500000,0.500000,0.500000)).z;
r4.w = ((r3).yyyy).w;
r3.z = (vec4(0.500000,0.500000,0.500000,0.500000)).z;
r1.zw = (vec4(0.0,0.0,0.0,8.000000)).zw;
r0.y = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).y;
for (int md_loop_0=0; md_loop_0<1024; md_loop_0++) {
r2.z = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r0).yyyy))))).z;
if (any(notEqual((r2).zzzz,vec4(0.0)))) { break; }
r4.y = (vec4(floatBitsToInt((r0).yyyy))).y;
r2.zw = ((r1).zzzw).zw;
r3.y = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).y;
for (int md_loop_1=0; md_loop_1<1024; md_loop_1++) {
r3.w = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r3).yyyy))))).w;
if (any(notEqual((r3).wwww,vec4(0.0)))) { break; }
r4.x = (vec4(floatBitsToInt((r3).yyyy))).x;
r5.xy = (((r2).xyxx+(r4).xyxx)).xy;
r3.w = (vec4(dot(((r5).xyxx).xy,(vec4(15.270000,99.410004,0.000000,0.000000)).xy))).w;
r5.x = (vec4(dot(((r5).xyxx).xy,(vec4(47.630001,89.980003,0.000000,0.000000)).xy))).x;
r6.y = (sin((r3).wwww)).y;

r6.x = (sin((r5).xxxx)).x;

r5.xy = (fract((r6).xyxx)).xy;
r5.xy = (((r5).xyxx*vec4(28.500000,28.500000,0.000000,0.000000))).xy;
r6.x = (sin((r5).xxxx)).x;


r6.y = (cos((r5).yyyy)).y;
r5.xy = (((r6).xyxx*vec4(0.500000,0.500000,0.000000,0.000000)+(r4).zyzz)).xy;
r5.yw = (((r4).xxxw+(r5).xxxy)).yw;
r5.yw = (((r3).xxxz+(r5).yyyw)).yw;
r3.w = (vec4(dot(((r5).ywyy).xy,((r5).ywyy).xy))).w;
r5.z = (sqrt((r3).wwww)).z;
r3.w = (intBitsToFloat(-ivec4(lessThan((r5).zzzz,(r2).wwww)))).w;
r2.zw = (mix((r2).zzzw,(r5).xxxz,notEqual((r3).wwww,vec4(0.0)))).zw;
r3.y = (intBitsToFloat(floatBitsToInt((r3).yyyy) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).y;
}
r1.zw = ((r2).zzzw).zw;
r0.y = (intBitsToFloat(floatBitsToInt((r0).yyyy) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).y;
}
r0.y = (intBitsToFloat(-ivec4(greaterThanEqual(vec4(0.004000,0.004000,0.004000,0.004000),(r1).zzzz)))).y;
r0.y = (intBitsToFloat(floatBitsToInt((r0).yyyy) & floatBitsToInt(vec4(uintBitsToFloat(1065353216u),uintBitsToFloat(1065353216u),uintBitsToFloat(1065353216u),uintBitsToFloat(1065353216u))))).y;
r0.x = (((r0).yyyy*(r0).xxxx)).x;
r0.yz = (((r0).zzwz+(v1).xxyx)).yz;
r0.yz = (((r0).yyzy*(vec4(p_Alpha,p_Dust_Scale,p_White_Ratio,p_Tiling_Switch)).yyyy)).yz;
r1.zw = (floor((r0).yyyz)).zw;
r0.yz = (fract((r0).yyzy)).yz;
r2.xy = (-((r0).yzyy)).xy;
r3.z = (vec4(0.500000,0.500000,0.500000,0.500000)).z;
r3.w = ((r2).yyyy).w;
r2.z = (vec4(0.500000,0.500000,0.500000,0.500000)).z;
r0.yzw = (vec4(0.0,0.0,8.000000,uintBitsToFloat(4294967295u))).yzw;
for (int md_loop_2=0; md_loop_2<1024; md_loop_2++) {
r2.y = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r0).wwww))))).y;
if (any(notEqual((r2).yyyy,vec4(0.0)))) { break; }
r3.y = (vec4(floatBitsToInt((r0).wwww))).y;
r2.yw = ((r0).yyyz).yw;
r4.x = (vec4(uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u),uintBitsToFloat(4294967295u))).x;
for (int md_loop_3=0; md_loop_3<1024; md_loop_3++) {
r4.y = (intBitsToFloat(-ivec4(lessThan(floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))),floatBitsToInt((r4).xxxx))))).y;
if (any(notEqual((r4).yyyy,vec4(0.0)))) { break; }
r3.x = (vec4(floatBitsToInt((r4).xxxx))).x;
r4.yz = (((r1).zzwz+(r3).xxyx)).yz;
r4.w = (vec4(dot(((r4).yzyy).xy,(vec4(15.270000,99.410004,0.000000,0.000000)).xy))).w;
r4.y = (vec4(dot(((r4).yzyy).xy,(vec4(47.630001,89.980003,0.000000,0.000000)).xy))).y;
r5.xy = (sin((r4).ywyy)).xy;

r4.yz = (fract((r5).xxyx)).yz;
r4.yz = (((r4).yyzy*vec4(0.000000,54.400002,54.400002,0.000000))).yz;
r5.x = (sin((r4).yyyy)).x;


r5.y = (cos((r4).zzzz)).y;
r5.xy = (((r5).xyxx*vec4(0.500000,0.500000,0.000000,0.000000)+(r3).zyzz)).xy;
r4.yz = (((r3).xxwx+(r5).xxyx)).yz;
r4.yz = (((r2).xxzx+(r4).yyzy)).yz;
r3.x = (vec4(dot(((r4).yzyy).xy,((r4).yzyy).xy))).x;
r5.z = (sqrt((r3).xxxx)).z;
r3.x = (intBitsToFloat(-ivec4(lessThan((r5).zzzz,(r2).wwww)))).x;
r2.yw = (mix((r2).yyyw,(r5).xxxz,notEqual((r3).xxxx,vec4(0.0)))).yw;
r4.x = (intBitsToFloat(floatBitsToInt((r4).xxxx) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).x;
}
r0.yz = ((r2).yywy).yz;
r0.w = (intBitsToFloat(floatBitsToInt((r0).wwww) + floatBitsToInt(vec4(uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u),uintBitsToFloat(1u))))).w;
}
r0.y = (intBitsToFloat(-ivec4(greaterThanEqual(vec4(0.005600,0.005600,0.005600,0.005600),(r0).yyyy)))).y;
r0.z = (vec4(dot(((r1).xyxx).xy,((r1).xyxx).xy))).z;
r0.z = (sqrt((r0).zzzz)).z;
r0.z = ((-((r0).zzzz)*vec4(2.000000,2.000000,2.000000,2.000000)+vec4(1.000000,1.000000,1.000000,1.000000))).z;
r0.z = (intBitsToFloat(-ivec4(greaterThanEqual((r0).zzzz,vec4(0.100000,0.100000,0.100000,0.100000))))).z;
r0.yz = (intBitsToFloat(floatBitsToInt((r0).yyzy) & floatBitsToInt(vec4(0.0,uintBitsToFloat(1065353216u),uintBitsToFloat(1065353216u),0.0)))).yz;
r0.y = ((-((r0).yyyy)*(r0).zzzz+vec4(1.000000,1.000000,1.000000,1.000000))).y;
r0.y = (intBitsToFloat(-ivec4(greaterThanEqual(vec4(0.020000,0.020000,0.020000,0.020000),(r0).yyyy)))).y;
r0.y = (intBitsToFloat(floatBitsToInt((r0).yyyy) & floatBitsToInt(vec4(uintBitsToFloat(1065353216u),uintBitsToFloat(1065353216u),uintBitsToFloat(1065353216u),uintBitsToFloat(1065353216u))))).y;
r0.z = (intBitsToFloat(-ivec4(notEqual(vec4(0.000000,0.000000,0.000000,0.000000),(vec4(p_Alpha,p_Dust_Scale,p_White_Ratio,p_Tiling_Switch)).wwww)))).z;
r0.x = (mix((r0).yyyy,(r0).xxxx,notEqual((r0).zzzz,vec4(0.0)))).x;
r0.x = (((r0).xxxx*(v2).wwww)).x;
o0.w = (((r0).xxxx*(vec4(p_Alpha,p_Dust_Scale,p_White_Ratio,p_Tiling_Switch)).xxxx)).w;
ALBEDO=o0.rgb;
ALPHA=o0.a;
}
