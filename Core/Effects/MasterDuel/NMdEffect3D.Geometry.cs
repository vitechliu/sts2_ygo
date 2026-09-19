using System.Text.Json;
using Godot;
using static VYgo.Core.Effects.MasterDuel.MdCurve;

namespace VYgo.Core.Effects.MasterDuel;

public partial class NMdEffect3D {
    public bool DemoMotion { get; set; }
    public int DrawVertexCount { get; private set; }
    private sealed class Geometry {
        public Vector3[] Vertices=[],Normals=[];
        public Color[] Colors=[];
        public float[] Tangents=[];
        public Vector2[][] Uvs=[];
        public int[] Indices=[];
        private float? _radius;
        public float Radius=>_radius??=Vertices.Length==0?1:Vertices.Max(v=>v.Length());
    }
    private sealed class ExtraRenderer {
        public string Type="";
        public SourceNode Source=null!;
        public JsonElement Data,Sprite;
        public MeshInstance3D Draw=null!;
        public int Material;
        public List<TrailPoint> Points=[];
    }
    private static Geometry Quad() => new() { Vertices=[new(-.5f,-.5f,0),new(-.5f,.5f,0),new(.5f,.5f,0),new(.5f,-.5f,0)],
        Normals=[Vector3.Back,Vector3.Back,Vector3.Back,Vector3.Back],Uvs=[[new(0,0),new(0,1),new(1,1),new(1,0)]],Indices=[0,1,2,0,2,3] };
    private readonly Geometry _quad=Quad();

    private static Geometry ReadGeometry(JsonElement j) {
        if(j.TryGetProperty("builtin",out var id)) {
            if(id.GetInt32()==10210)return Quad();
            using PrimitiveMesh primitive=id.GetInt32() switch {10202=>new BoxMesh {Size=Vector3.One},10207=>new SphereMesh {Radius=.5f,Height=1,RadialSegments=24,Rings=16},10209=>new PlaneMesh {Size=new Vector2(10,10)},_=>throw new InvalidOperationException("未知内建网格")};
            var a=primitive.GetMeshArrays();var uv=a[(int)Mesh.ArrayType.TexUV].AsVector2Array();
            return new Geometry {Vertices=a[(int)Mesh.ArrayType.Vertex].AsVector3Array(),Normals=a[(int)Mesh.ArrayType.Normal].AsVector3Array(),Uvs=[uv.Select(v=>new Vector2(v.X,1-v.Y)).ToArray()],Indices=a[(int)Mesh.ArrayType.Index].AsInt32Array(),Tangents=a[(int)Mesh.ArrayType.Tangent].AsFloat32Array()};
        }
        Vector3 V(JsonElement a)=>new(a[0].GetSingle(),a[1].GetSingle(),-a[2].GetSingle());
        var result=new Geometry {Vertices=j.GetProperty("vertices").EnumerateArray().Select(V).ToArray()};
        if(j.GetProperty("normals").ValueKind==JsonValueKind.Array)result.Normals=j.GetProperty("normals").EnumerateArray().Select(V).ToArray();
        if(j.GetProperty("colors").ValueKind==JsonValueKind.Array)result.Colors=j.GetProperty("colors").EnumerateArray().Select(v=>new Color(v[0].GetSingle(),v[1].GetSingle(),v[2].GetSingle(),v[3].GetSingle())).ToArray();
        if(j.GetProperty("tangents").ValueKind==JsonValueKind.Array)result.Tangents=j.GetProperty("tangents").EnumerateArray().SelectMany(v=>new[]{v[0].GetSingle(),v[1].GetSingle(),-v[2].GetSingle(),-v[3].GetSingle()}).ToArray();
        result.Uvs=j.GetProperty("uv_channels").EnumerateArray().Select(c=>c.ValueKind==JsonValueKind.Array?c.EnumerateArray().Select(v=>new Vector2(v[0].GetSingle(),v[1].GetSingle())).ToArray():Array.Empty<Vector2>()).ToArray();
        result.Indices=j.GetProperty("submesh_triangles").EnumerateArray().SelectMany(s=>s.EnumerateArray().SelectMany(t=>new[]{t[0].GetInt32(),t[2].GetInt32(),t[1].GetInt32()})).ToArray();return result;
    }

    private static void ClearDraw(MeshInstance3D draw) { var old=draw.Mesh;draw.Mesh=null;old?.Dispose(); }
    private SurfaceTool BeginSurface(int material) {
        var tool=new SurfaceTool();tool.Begin(Mesh.PrimitiveType.Triangles);
        for(int i=0;i<4;i++)tool.SetCustomFormat(i,SurfaceTool.CustomFormat.RgbaFloat);
        tool.SetMaterial(_materials[material]);return tool;
    }
    private static void FinishSurface(SurfaceTool tool,MeshInstance3D draw,int vertices) {
        ClearDraw(draw);if(vertices>0)draw.Mesh=tool.Commit();
    }

    private Vector2 FrameUv(Particle p,Vector2 uv,bool next=false) {
        var data=p.Run.Emitter.Data;if(!Enabled(data,"UVModule"))return uv;
        var m=data.GetProperty("UVModule");int x=Math.Max(1,(int)F(m,"tilesX",1)),y=Math.Max(1,(int)F(m,"tilesY",1)),total=x*y;
        float t=Math.Clamp((Elapsed-p.Birth)/p.Lifetime,0,1);
        float frame=(Sample(m.GetProperty("frameOverTime"),t,p.Random)*F(m,"cycles",1)+p.FrameStart)*total;
        int tile=Mathf.PosMod((int)Mathf.Floor(frame)+(next?1:0),total);
        return new Vector2((uv.X+tile%x)/x,(uv.Y+y-1-tile/x)/y);
    }

    private void Vertex(SurfaceTool tool,Vector3 position,Vector3 normal,Vector3 tangent,float sign,Color color,Vector2 uv,Vector2 uv2,Emitter? e=null,Particle? p=null,bool convertColor=true) {
        float[] packed=new float[20];int cursor=0;
        void Push(float v) { if(cursor>=packed.Length)throw new InvalidOperationException("顶点流超过 5 个 TEXCOORD。");packed[cursor++]=v; }
        if(e==null || p==null) {Push(uv.X);Push(uv.Y);Push(uv2.X);Push(uv2.Y);}
        else {
            var c1=p.Custom1;var c2=p.Custom2;
            foreach(int stream in e.Streams) {
                if(stream<=3)continue;
                if(stream==4) {Push(uv.X);Push(uv.Y);}
                else if(stream==5) {Push(uv2.X);Push(uv2.Y);}
                else if(stream==21)Push(Math.Clamp((Elapsed-p.Birth)/p.Lifetime,0,1));
                else if(stream>=23&&stream<=26)for(int k=0;k<=stream-23;k++)Push(p.Stable[k]);
                else if(stream>=31&&stream<=34)for(int k=0;k<=stream-31;k++)Push(c1[k]);
                else if(stream>=35&&stream<=38)for(int k=0;k<=stream-35;k++)Push(c2[k]);
                else if(stream>=39&&stream<=41)for(int k=0;k<=stream-39;k++)Push(p.NoiseValue[k]);
                else throw new InvalidOperationException("未实现的顶点流："+stream);
            }
        }
        tool.SetUV(new Vector2(packed[0],packed[1]));tool.SetUV2(new Vector2(packed[2],packed[3]));
        for(int i=0;i<4;i++)tool.SetCustom(i,new Color(packed[4+i*4],packed[5+i*4],packed[6+i*4],packed[7+i*4]));
        if(e!=null)convertColor=F(e.Renderer,"m_ApplyActiveColorSpace",1)!=0;
        tool.SetColor(convertColor?color.SrgbToLinear():color);tool.SetNormal(normal.Normalized());tool.SetTangent(new Plane(tangent.Normalized(),sign));tool.AddVertex(position);
    }

    private void DrawParticles() {
        DrawVertexCount=0;var camera=GetViewport().GetCamera3D();if(camera==null)return;
        var inverse=GlobalTransform.AffineInverse();
        foreach(var e in _emitters) {
            if(!e.Visible||e.Particles.Count==0) {ClearDraw(e.Draw);DrawParticleTrails(e,camera);continue;}
            using var tool=BeginSurface(e.Materials[0]);int count=0;
            IEnumerable<Particle> particles=e.Particles;
            if((int)F(e.Renderer,"m_SortMode")==1)particles=particles.OrderByDescending(p=>camera.GlobalPosition.DistanceSquaredTo(WorldPosition(p)));
            if((int)F(e.Renderer,"m_SortMode")==2)particles=particles.OrderByDescending(p=>p.Birth);
            if((int)F(e.Renderer,"m_SortMode")==3)particles=particles.OrderBy(p=>p.Birth);
            foreach(var p in particles) {
                p.Custom1=Custom(p,0);p.Custom2=Custom(p,1);
                int mode=(int)F(e.Renderer,"m_RenderMode"),alignment=(int)F(e.Renderer,"m_RenderAlignment");
                var geometry=mode==4&&e.Meshes.Length>0?e.Meshes[p.MeshIndex]:_quad;
                Basis basis=alignment switch {2=>new Basis(GlobalBasis.GetRotationQuaternion()*e.Source.Rotation),1=>Basis.Identity,_=>camera.GlobalBasis.Orthonormalized()};
                if(alignment==4&&p.CurrentVelocity.LengthSquared()>.000001f)basis=Basis.LookingAt(p.CurrentVelocity.Normalized(),Math.Abs(p.CurrentVelocity.Normalized().Dot(Vector3.Up))>.99f?Vector3.Right:Vector3.Up);
                var size=CurrentSize(p);if((int)F(e.Data,"scalingMode")!=2)size*=(int)F(e.Data,"scalingMode")==1?e.Source.LocalScale:e.Source.Scale;
                if(mode!=4) {
                    float maxSize=F(e.Renderer,"m_MaxParticleSize",1)*camera.Size;
                    float diameter=Math.Max(Math.Abs(size.X),Math.Abs(size.Y));if(diameter>maxSize&&diameter>0)size*=maxSize/diameter;
                }
                basis*=Euler(p.Rotation+p.NoiseRotation);
                if(mode==1) {
                    var v=e.Local?SimulationTransform(e).Basis*p.CurrentVelocity:p.CurrentVelocity;
                    var y=v.LengthSquared()>.00001f?v.Normalized():basis.Y;
                    var x=y.Cross(camera.GlobalBasis.Z).Normalized();if(x.LengthSquared()<.001f)x=camera.GlobalBasis.X;
                    basis=new Basis(x,y,x.Cross(y).Normalized());
                    size.Y=Math.Abs(size.Y)*F(e.Renderer,"m_LengthScale",2)+v.Length()*F(e.Renderer,"m_VelocityScale");
                }
                basis=basis.ScaledLocal(size);var center=WorldPosition(p);
                var pivot=e.Renderer.TryGetProperty("m_Pivot",out var pv)?Position(pv):Vector3.Zero;
                var color=CurrentColor(p);
                bool flipBillboard=mode!=4&&size.X*size.Y<0;
                for(int vertexIndex=0;vertexIndex<geometry.Indices.Length;vertexIndex++) {
                    // 负拉伸翻转长度/UV，但 Billboard 的正面仍需与其朝向一致。
                    int windingIndex=flipBillboard&&vertexIndex%3!=0?vertexIndex/3*3+(3-vertexIndex%3):vertexIndex;
                    int index=geometry.Indices[windingIndex];
                    var uv=geometry.Uvs.Length>0&&geometry.Uvs[0].Length>index?geometry.Uvs[0][index]:Vector2.Zero;
                    var uv2=Enabled(e.Data,"UVModule")?FrameUv(p,uv,true):geometry.Uvs.Length>1&&geometry.Uvs[1].Length>index?geometry.Uvs[1][index]:Vector2.Zero;
                    var n=geometry.Normals.Length>index?geometry.Normals[index]:Vector3.Back;
                    var tangent=geometry.Tangents.Length>index*4+3?new Vector3(geometry.Tangents[index*4],geometry.Tangents[index*4+1],geometry.Tangents[index*4+2]):Vector3.Right;
                    float sign=geometry.Tangents.Length>index*4+3?geometry.Tangents[index*4+3]:1;
                    var vertexColor=geometry.Colors.Length>index?geometry.Colors[index]:Colors.White;
                    var normalBasis=basis.Determinant()==0?basis:basis.Inverse().Transposed();
                    Vertex(tool,inverse*(center+basis*(geometry.Vertices[index]-pivot)),inverse.Basis*(normalBasis*n),inverse.Basis*(basis*tangent),sign,color*vertexColor,FrameUv(p,uv),uv2,e,p);count++;
                }
            }
            FinishSurface(tool,e.Draw,count);DrawVertexCount+=count;DrawParticleTrails(e,camera);
        }
    }

    private int Ribbon(SurfaceTool tool,List<TrailPoint> points,Camera3D camera,Func<float,float> width,Func<float,Color> color,Vector2 textureScale,bool convertColor=true) {
        if(points.Count<2)return 0;float total=0;float[] distance=new float[points.Count];
        for(int i=1;i<points.Count;i++) {total+=points[i].Position.DistanceTo(points[i-1].Position);distance[i]=total;}
        if(total<.000001f)return 0;var inverse=GlobalTransform.AffineInverse();
        for(int i=0;i<points.Count-1;i++) {
            // 每段两个三角形；同一中心线两侧各取半宽。
            var dir=(points[i+1].Position-points[i].Position).Normalized();var side=dir.Cross(camera.GlobalBasis.Z).Normalized();
            foreach(var (index,sign) in new[]{(i,-1),(i+1,-1),(i+1,1),(i,-1),(i+1,1),(i,1)}) {
                float u=distance[index]/total;var pos=points[index].Position+side*(width(1-u)*sign*.5f);
                Vertex(tool,inverse*pos,inverse.Basis*camera.GlobalBasis.Z,inverse.Basis*dir,1,color(1-u),new Vector2(u,(sign+1)*.5f)*textureScale,Vector2.Zero,convertColor:convertColor);
            }
        }
        return (points.Count-1)*6;
    }

    private void DrawParticleTrails(Emitter e,Camera3D camera) {
        if(!Enabled(e.Data,"TrailModule")||e.Materials.Length<2||e.Materials[1]<0) {ClearDraw(e.TrailDraw);return;}
        using var tool=BeginSurface(e.Materials[1]);var m=e.Data.GetProperty("TrailModule");int count=0;
        foreach(var p in e.Particles) {
            float age=Math.Clamp((Elapsed-p.Birth)/p.Lifetime,0,1);
            var tint=Gradient(m.GetProperty("colorOverLifetime"),age,p.Random)*(F(m,"inheritParticleColor")!=0?CurrentColor(p):Colors.White);
            count+=Ribbon(tool,p.Trail,camera,t=>Sample(m.GetProperty("widthOverTrail"),t,p.Random)*(F(m,"sizeAffectsWidth")!=0?CurrentSize(p).X:1),t=>tint*Gradient(m.GetProperty("colorOverTrail"),t,p.Random),new Vector2(F(m.GetProperty("textureScale"),"x",1),F(m.GetProperty("textureScale"),"y",1)),F(e.Renderer,"m_ApplyActiveColorSpace",1)!=0);
        }
        FinishSurface(tool,e.TrailDraw,count);DrawVertexCount+=count;
    }

    private void LoadExtras(JsonElement extras) {
        foreach(var item in extras.EnumerateArray()) {
            var data=item.GetProperty("data");var e=new ExtraRenderer {Type=item.GetProperty("type").GetString()!,Data=data,Source=_objects[data.GetProperty("m_GameObject").GetProperty("m_PathID").GetInt64()],Material=item.GetProperty("materials")[0].GetInt32(),Draw=CreateDraw()};
            if(item.TryGetProperty("sprite",out var sprite))e.Sprite=sprite;_extras.Add(e);
        }
    }

    private void DrawExtras() {
        var camera=GetViewport().GetCamera3D();if(camera==null)return;
        foreach(var e in _extras) {
            if(!e.Source.Active||F(e.Data,"m_Enabled")==0)continue;
            using var tool=BeginSurface(e.Material);int count=0;
            if(e.Type=="TrailRenderer") {
                var position=e.Source.Node.GlobalPosition;
                if(F(e.Data,"m_Emitting")!=0&&(e.Points.Count==0||position.DistanceTo(e.Points[^1].Position)>=F(e.Data,"m_MinVertexDistance",.01f)))e.Points.Add(new TrailPoint(position,Elapsed));
                e.Points.RemoveAll(p=>Elapsed-p.Time>F(e.Data,"m_Time",.5f));var p=e.Data.GetProperty("m_Parameters");
                count=Ribbon(tool,e.Points,camera,t=>F(p,"widthMultiplier",1)*Hermite(p.GetProperty("widthCurve"),t),t=>EvaluateGradient(p.GetProperty("colorGradient"),t),new Vector2(F(p.GetProperty("textureScale"),"x",1),F(p.GetProperty("textureScale"),"y",1)),F(e.Data,"m_ApplyActiveColorSpace",1)!=0);
            } else {
                var rect=e.Sprite.GetProperty("m_Rect");float units=F(e.Sprite,"m_PixelsToUnits",100);var pivot=e.Sprite.GetProperty("m_Pivot");
                foreach(int i in _quad.Indices) {
                    var uv=_quad.Uvs[0][i];var point=new Vector3((uv.X-F(pivot,"x",.5f))*F(rect,"width")/units,(uv.Y-F(pivot,"y",.5f))*F(rect,"height")/units,0);
                    if(F(e.Data,"m_FlipX")!=0)point.X=-point.X;if(F(e.Data,"m_FlipY")!=0)point.Y=-point.Y;
                    Vertex(tool,ToLocal(e.Source.Node.ToGlobal(point)),Vector3.Back,Vector3.Right,1,Color(e.Data.GetProperty("m_Color")),uv,Vector2.Zero);count++;
                }
            }
            FinishSurface(tool,e.Draw,count);DrawVertexCount+=count;
        }
    }

    private void ConfigureSorting() {
        var records=new List<(JsonElement Data,MeshInstance3D Draw,int Material)>();
        foreach(var e in _emitters) {
            if(e.Materials.Length>0&&e.Materials[0]>=0)records.Add((e.Renderer,e.Draw,e.Materials[0]));
            if(e.Materials.Length>1&&e.Materials[1]>=0)records.Add((e.Renderer,e.TrailDraw,e.Materials[1]));
        }
        records.AddRange(_extras.Select(e=>(e.Data,e.Draw,e.Material)));
        var keys=records.Select(r=>((int)F(r.Data,"m_SortingLayer"),(int)F(r.Data,"m_SortingOrder"),_materialQueues[r.Material])).Distinct().OrderBy(k=>k.Item1).ThenBy(k=>k.Item2).ThenBy(k=>k.Item3).ToList();
        foreach(var r in records) {
            var material=(ShaderMaterial)_materials[r.Material].Duplicate();
            material.RenderPriority=keys.IndexOf(((int)F(r.Data,"m_SortingLayer"),(int)F(r.Data,"m_SortingOrder"),_materialQueues[r.Material]));
            r.Draw.MaterialOverride=material;r.Draw.SortingOffset=-F(r.Data,"m_SortingFudge");_drawMaterials.Add(material);
        }
    }
}
