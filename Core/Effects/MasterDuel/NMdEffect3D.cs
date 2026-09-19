using System.Text.Json;
using Godot;
using static VYgo.Core.Effects.MasterDuel.MdCurve;

namespace VYgo.Core.Effects.MasterDuel;

/// <summary>按源模块驱动的独立粒子播放器；仅用于特效预览，不注册游戏触发。</summary>
[ScriptPath("res://Core/Effects/MasterDuel/NMdEffect3D.cs")]
public partial class NMdEffect3D : Node3D {
    [Export] public MdSourceData Source { get; set; } = null!;
    [Export] public bool AutoPlay { get; set; } = true;
    [Export] public bool AutoFree { get; set; }
    [Export] public int Seed { get; set; } = 20260919;
    public bool Playing { get; private set; }
    public string PlaybackError { get; private set; } = "";
    public bool Paused { get; set; }
    public float PlaybackSpeed { get; set; } = 1;
    public float Elapsed { get; private set; }
    public float Duration { get; private set; }
    public bool Looping => _emitters.Any(e => e.Active && F(e.Data,"looping") != 0);
    public int LiveParticleCount => _emitters.Sum(e => e.Particles.Count);
    public int VisibleParticleCount => _emitters.Where(e => e.Visible).Sum(e => e.Particles.Count);
    public int EmittedParticleCount { get; private set; }
    public Aabb PreviewBounds { get; private set; } = new(new Vector3(-1,-1,-1),Vector3.One*2);
    public event Action? Finished;
    private const float Step = 1f / 120f;
    private readonly List<Emitter> _emitters = [];
    private readonly List<Run> _runs = [];
    private readonly Dictionary<long, SourceNode> _objects = [];
    private readonly Dictionary<string, Geometry> _geometry = [];
    private readonly List<ExtraRenderer> _extras = [];
    private ShaderMaterial[] _materials = [];
    private int[] _materialQueues=[];
    private readonly List<ShaderMaterial> _drawMaterials=[];
    private JsonDocument? _document;
    private double _accumulator;
    private bool _estimating;
    private Vector3 _restPosition;

    private sealed class SourceNode {
        public Node3D Node = null!;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
        public SourceNode? Parent;
        public bool OwnActive;
        public bool Active => OwnActive && (Parent?.Active ?? true);
        public Quaternion Rotation => Parent == null ? LocalRotation : Parent.Rotation * LocalRotation;
        public Vector3 Scale => Parent == null ? LocalScale : Parent.Scale * LocalScale;
    }
    private sealed class Emitter {
        public long Id;
        public SourceNode Source = null!;
        public JsonElement Data, Renderer;
        public List<Particle> Particles = [];
        public int[] Materials = [], Streams = [];
        public Geometry[] Meshes = [];
        public MeshInstance3D Draw = null!, TrailDraw = null!;
        public Random Random = null!;
        public FastNoiseLite Noise = null!;
        public bool SubOnly;
        public bool Active => Source.Active;
        public bool Local => F(Data,"moveWithTransform")==0;
        public bool Visible => Active && F(Renderer,"m_Enabled")!=0 && (int)F(Renderer,"m_RenderMode")!=5 && Materials.FirstOrDefault(-1)>=0;
    }
    private sealed class Run {
        public Emitter Emitter = null!;
        public float Start, Duration, Delay, RateAccumulator, LastBurstTime = -0.0001f;
        public Vector3 LastPosition;
        public Particle? Parent;
        public int Inheritance;
        public bool Complete;
    }
    private sealed class Particle {
        public Run Run = null!;
        public float Birth, Lifetime, Random, FrameStart;
        public Vector3 Position, Velocity, CurrentVelocity, Size, Rotation, NoiseOffset, NoiseValue, NoiseRotation, NoiseSize;
        public Color Color;
        public float[] Stable = [], Custom1=[], Custom2=[];
        public bool Dead;
        public int MeshIndex;
        public Vector3 SpawnShift;
        public readonly List<TrailPoint> Trail = [];
    }
    private readonly record struct TrailPoint(Vector3 Position, float Time);

    public override void _Ready() {
        try { InitializeSource(); }
        catch(Exception error) { FailPlayback(error); }
    }

    private void InitializeSource() {
        _restPosition=Position;
        _document=JsonDocument.Parse(Source.DataJson);var root=_document.RootElement;
        _materials=Source.Materials.Select(m => (ShaderMaterial)m.Duplicate()).ToArray();
        _materialQueues=root.GetProperty("material_queues").EnumerateArray().Select(v=>v.GetInt32()).ToArray();
        var transforms=new Dictionary<long,SourceNode>();
        foreach (var item in root.GetProperty("transforms").EnumerateArray()) {
            var data=item.GetProperty("data");var obj=data.GetProperty("m_GameObject").GetProperty("m_PathID").GetInt64();
            var q=Rotation(data.GetProperty("m_LocalRotation"));var scale=Vector(data.GetProperty("m_LocalScale"));
            var record=new SourceNode { LocalRotation=q, LocalScale=scale,
                OwnActive=root.GetProperty("objects").GetProperty(obj.ToString()).GetProperty("active").GetBoolean(),
                Node=new Node3D { Name="源节点_"+item.GetProperty("path_id").GetInt64(),Transform=new Transform3D(new Basis(q).ScaledLocal(scale),Position(data.GetProperty("m_LocalPosition"))) } };
            transforms[item.GetProperty("path_id").GetInt64()]=record;_objects[obj]=record;
        }
        foreach (var item in root.GetProperty("transforms").EnumerateArray()) {
            var record=transforms[item.GetProperty("path_id").GetInt64()];long parent=item.GetProperty("data").GetProperty("m_Father").GetProperty("m_PathID").GetInt64();
            if (transforms.TryGetValue(parent,out var p)) { record.Parent=p;p.Node.AddChild(record.Node); } else AddChild(record.Node);
        }
        foreach (var mesh in root.GetProperty("geometry").EnumerateObject()) _geometry[mesh.Name]=ReadGeometry(mesh.Value);
        foreach (var item in root.GetProperty("emitters").EnumerateArray()) {
            var data=item.GetProperty("data");var renderer=item.GetProperty("renderer");
            var e=new Emitter { Id=item.GetProperty("id").GetInt64(), Data=data, Renderer=renderer,
                Source=_objects[data.GetProperty("m_GameObject").GetProperty("m_PathID").GetInt64()],
                Materials=item.GetProperty("materials").EnumerateArray().Select(v=>v.GetInt32()).ToArray(),
                Streams=renderer.GetProperty("m_VertexStreams").EnumerateArray().Select(v=>v.GetInt32()).ToArray(),
                Meshes=item.GetProperty("meshes").EnumerateArray().Where(v=>v.ValueKind==JsonValueKind.String).Select(v=>_geometry[v.GetString()!]).ToArray(),
                Draw=CreateDraw(),TrailDraw=CreateDraw() };
            _emitters.Add(e);Duration=Math.Max(Duration,F(data,"lengthInSec")+Sample(data.GetProperty("startDelay"),0));
        }
        foreach (var e in _emitters.Where(e=>Enabled(e.Data,"SubModule")))
            foreach (var sub in e.Data.GetProperty("SubModule").GetProperty("subEmitters").EnumerateArray())
                _emitters.Single(s=>s.Id==sub.GetProperty("emitter").GetProperty("m_PathID").GetInt64()).SubOnly=true;
        LoadExtras(root.GetProperty("extras"));
        ConfigureSorting();
        EstimateBounds();
        if (AutoPlay) Play(Seed);
    }

    private MeshInstance3D CreateDraw() {
        var draw=new MeshInstance3D { CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,
            CustomAabb=new Aabb(Vector3.One*-100000,Vector3.One*200000) };
        AddChild(draw);return draw;
    }

    public void Play(int? seed=null) {
        Stop();if(seed.HasValue)Seed=seed.Value;
        int index=0;float prewarm=0;
        foreach(var e in _emitters) {
            e.Random=new Random(unchecked(Seed+index++*7919));
            e.Noise=new FastNoiseLite { Seed=Seed+index,NoiseType=FastNoiseLite.NoiseTypeEnum.Perlin,Frequency=1,
                FractalType=FastNoiseLite.FractalTypeEnum.None };
            if(!e.Active || e.SubOnly || F(e.Data,"playOnAwake")==0)continue;
            float duration=F(e.Data,"lengthInSec",1);
            bool warm=F(e.Data,"prewarm")!=0 && F(e.Data,"looping")!=0;
            var run=CreateRun(e,warm?-duration:0,null,0);_runs.Add(run);
            if(warm)prewarm=Math.Max(prewarm,duration);
        }
        Playing=true;Paused=false;
        if(prewarm>0) { Elapsed=-prewarm;while(Elapsed<-Step)Advance(Step);Elapsed=0; }
        Advance(0);UpdateMaterials();
    }

    private Run CreateRun(Emitter e,float start,Particle? parent,int inheritance) => new() {
        Emitter=e,Start=start,Parent=parent,Inheritance=inheritance,LastPosition=e.Source.Node.GlobalPosition,
        Duration=parent!=null && (inheritance&16)!=0 ? parent.Lifetime:F(e.Data,"lengthInSec",1),
        Delay=Sample(e.Data.GetProperty("startDelay"),0,(float)e.Random.NextDouble()) };

    public void Stop() {
        Playing=false;Paused=false;Elapsed=0;EmittedParticleCount=0;DrawVertexCount=0;_accumulator=0;_runs.Clear();Position=_restPosition;
        foreach(var e in _emitters) { e.Particles.Clear();ClearDraw(e.Draw);ClearDraw(e.TrailDraw);e.Noise?.Dispose();e.Noise=null!; }
        foreach(var e in _extras) { e.Points.Clear();ClearDraw(e.Draw); }
    }

    public override void _Process(double delta) {
        if(PlaybackError!="")return;
        try { Tick(delta); }
        catch(Exception error) { FailPlayback(error); }
    }

    private void FailPlayback(Exception error) {
        Stop();PlaybackError=error.Message;GD.PushError($"特效 {Name} 播放失败：{error}");
    }

    private void Tick(double delta) {
        if(!Playing || Paused)return;
        _accumulator+=delta*Math.Max(0,PlaybackSpeed);
        while(_accumulator>=Step) { Advance(Step);_accumulator-=Step; }
        UpdateMaterials();DrawParticles();DrawExtras();
        if(!Looping && Elapsed>=Duration && LiveParticleCount==0 && _runs.All(r=>r.Complete)) {
            Playing=false;Finished?.Invoke();if(AutoFree)QueueFree();
        }
    }

    private void UpdateMaterials() {
        var camera=GetViewport().GetCamera3D();
        foreach(var m in _materials.Concat(_drawMaterials)) {
            m.SetShaderParameter("md_time",Elapsed);
            if(camera!=null) { m.SetShaderParameter("md_near",camera.Near);m.SetShaderParameter("md_far",camera.Far);m.SetShaderParameter("md_camera_size",camera.Size); }
        }
    }

    private void Advance(float dt) {
        Elapsed+=dt;
        if(DemoMotion&&!_estimating)Position=_restPosition+new Vector3(Mathf.Sin(Elapsed*2)*12,(Mathf.Cos(Elapsed*2)-1)*5,0);
        for(int index=0;index<_runs.Count;index++) {
            var run=_runs[index];var e=run.Emitter;
            float local=Elapsed-run.Start-run.Delay;
            if(run.Complete || local<0)continue;
            bool loop=F(e.Data,"looping")!=0 && run.Parent==null;
            if(Enabled(e.Data,"EmissionModule")) {
                var emission=e.Data.GetProperty("EmissionModule");float end=loop?local:Math.Min(local,run.Duration);
                int first=Math.Max(0,(int)Math.Floor(Math.Max(0,run.LastBurstTime)/run.Duration));int last=loop?(int)Math.Floor(end/run.Duration):0;
                for(int cycle=first;cycle<=last;cycle++)
                    foreach(var burst in emission.GetProperty("m_Bursts").EnumerateArray())
                        for(int n=0;n<(int)F(burst,"cycleCount",1);n++) {
                            float at=cycle*run.Duration+F(burst,"time")+n*F(burst,"repeatInterval");
                            if(at<=run.LastBurstTime || at>end || at>= (cycle+1)*run.Duration)continue;
                            if(e.Random.NextDouble()>F(burst,"probability",1))continue;
                            int count=(int)Sample(burst.GetProperty("countCurve"),at/run.Duration,(float)e.Random.NextDouble());
                            for(int i=0;i<count;i++)Spawn(run,run.Start+run.Delay+at,i,count);
                        }
                run.LastBurstTime=end;
                if(loop || local<=run.Duration) {
                    float t=Mathf.PosMod(local,run.Duration)/run.Duration;
                    run.RateAccumulator+=Math.Max(0,Sample(emission.GetProperty("rateOverTime"),t,.5f))*Math.Min(dt,local);
                    run.RateAccumulator+=Math.Max(0,Sample(emission.GetProperty("rateOverDistance"),t,.5f))*e.Source.Node.GlobalPosition.DistanceTo(run.LastPosition);
                    while(run.RateAccumulator>=1) { Spawn(run,Elapsed);run.RateAccumulator--; }
                }
            }
            run.LastPosition=e.Source.Node.GlobalPosition;
            if(!loop && local>=run.Duration)run.Complete=true;
        }
        foreach(var e in _emitters) {
            for(int i=e.Particles.Count-1;i>=0;i--) {
                var p=e.Particles[i];float age=Elapsed-p.Birth;
                if(age>=p.Lifetime) { p.Dead=true;e.Particles.RemoveAt(i);continue; }
                float step=Math.Min(dt,Math.Max(0,age));UpdateParticle(e,p,age,step);
                if(Enabled(e.Data,"TrailModule")) {
                    var trail=e.Data.GetProperty("TrailModule");var position=WorldPosition(p);
                    if(p.Trail.Count==0 || p.Trail[^1].Position.DistanceTo(position)>=F(trail,"minVertexDistance",.05f))p.Trail.Add(new TrailPoint(position,Elapsed));
                    float life=Sample(trail.GetProperty("lifetime"),age/p.Lifetime,p.Random)*(F(trail,"sizeAffectsLifetime")!=0?p.Size.X:1);
                    p.Trail.RemoveAll(v=>Elapsed-v.Time>life);
                }
            }
        }
        _runs.RemoveAll(r=>r.Complete && !r.Emitter.Particles.Any(p=>ReferenceEquals(p.Run,r)));
    }

    private void Spawn(Run run,float birth,int burstIndex=0,int burstCount=1) {
        var e=run.Emitter;var initial=e.Data.GetProperty("InitialModule");
        if(e.Particles.Count>=(int)F(initial,"maxNumParticles",1000))return;
        float RandomValue()=>(float)e.Random.NextDouble();float time=Mathf.PosMod(birth-run.Start-run.Delay,run.Duration)/run.Duration;
        var (position,direction)=SpawnShape(e,time,burstIndex,burstCount);
        float size=Sample(initial.GetProperty("startSize"),time,RandomValue());
        var p=new Particle { Run=run,Birth=birth,Lifetime=Math.Max(.001f,Sample(initial.GetProperty("startLifetime"),time,RandomValue())),
            Position=position,Velocity=direction*Sample(initial.GetProperty("startSpeed"),time,RandomValue()),
            Size=new Vector3(size,F(initial,"size3D")!=0?Sample(initial.GetProperty("startSizeY"),time,RandomValue()):size,F(initial,"size3D")!=0?Sample(initial.GetProperty("startSizeZ"),time,RandomValue()):size),
            Rotation=new Vector3(F(initial,"rotation3D")!=0?Sample(initial.GetProperty("startRotationX"),time,RandomValue()):0,F(initial,"rotation3D")!=0?Sample(initial.GetProperty("startRotationY"),time,RandomValue()):0,Sample(initial.GetProperty("startRotation"),time,RandomValue())),
            Color=Gradient(initial.GetProperty("startColor"),time,RandomValue()),Random=RandomValue(),Stable=[RandomValue(),RandomValue(),RandomValue(),RandomValue()],MeshIndex=e.Meshes.Length==0?0:e.Random.Next(e.Meshes.Length) };
        if(F(initial,"randomizeRotationDirection")>RandomValue())p.Rotation.Z=-p.Rotation.Z;
        if(Enabled(e.Data,"UVModule"))p.FrameStart=Sample(e.Data.GetProperty("UVModule").GetProperty("startFrame"),time,RandomValue());
        if((int)F(e.Data,"scalingMode")==2)p.Position*=e.Source.Scale;
        if(run.Parent is {} parent) {
            p.SpawnShift=WorldPosition(parent)-run.Parent.Run.Emitter.Source.Node.GlobalPosition;
            if((run.Inheritance&1)!=0)p.Color*=CurrentColor(parent);
            if((run.Inheritance&2)!=0)p.Size*=CurrentSize(parent);
            if((run.Inheritance&4)!=0)p.Rotation+=parent.Rotation;
            if((run.Inheritance&8)!=0)p.Lifetime*=parent.Lifetime;
        }
        if(!e.Local) { p.Position=SimulationTransform(e)*p.Position+p.SpawnShift;p.Velocity=SimulationTransform(e).Basis*p.Velocity;p.SpawnShift=Vector3.Zero; }
        e.Particles.Add(p);EmittedParticleCount++;
        if(Enabled(e.Data,"SubModule"))
            foreach(var sub in e.Data.GetProperty("SubModule").GetProperty("subEmitters").EnumerateArray()) {
                if((int)F(sub,"type")!=0)throw new InvalidOperationException("本批只包含出生子发射器。");
                if(RandomValue()>F(sub,"emitProbability",1))continue;
                var child=_emitters.Single(s=>s.Id==sub.GetProperty("emitter").GetProperty("m_PathID").GetInt64());
                _runs.Add(CreateRun(child,birth,p,(int)F(sub,"properties")));
            }
    }

    private Transform3D SimulationTransform(Emitter e) {
        if((int)F(e.Data,"scalingMode")==0)return e.Source.Node.GlobalTransform;
        var rotation=GlobalBasis.GetRotationQuaternion()*e.Source.Rotation;
        int mode=(int)F(e.Data,"scalingMode");
        Vector3 scale=mode==2?Vector3.One:mode==1?e.Source.LocalScale:e.Source.Scale;
        return new Transform3D(new Basis(rotation).ScaledLocal(scale),e.Source.Node.GlobalPosition);
    }
    private Vector3 WorldPosition(Particle p) => p.Run.Emitter.Local ? SimulationTransform(p.Run.Emitter)*(p.Position+p.NoiseOffset)+p.SpawnShift:p.Position+p.NoiseOffset;
    private Vector3 CurrentSize(Particle p) {
        var e=p.Run.Emitter;var size=p.Size;float t=Mathf.Clamp((Elapsed-p.Birth)/p.Lifetime,0,1);
        if(Enabled(e.Data,"SizeModule")) { var m=e.Data.GetProperty("SizeModule");float x=Sample(m.GetProperty("curve"),t,p.Random);size*=new Vector3(x,F(m,"separateAxes")!=0?Sample(m.GetProperty("y"),t,p.Random):x,F(m,"separateAxes")!=0?Sample(m.GetProperty("z"),t,p.Random):x); }
        return size+p.NoiseSize;
    }
    private Color CurrentColor(Particle p) => Enabled(p.Run.Emitter.Data,"ColorModule") ?
        p.Color*Gradient(p.Run.Emitter.Data.GetProperty("ColorModule").GetProperty("gradient"),Mathf.Clamp((Elapsed-p.Birth)/p.Lifetime,0,1),p.Random):p.Color;

    private void EstimateBounds() {
        _estimating=true;Play(Seed);bool has=false;Aabb bounds=default;
        float duration=Math.Clamp(Duration+1,3,12);
        for(int i=0;i<duration*30;i++) {
            Advance(1f/30);
            foreach(var e in _emitters.Where(e=>e.Visible))foreach(var p in e.Particles) {
                if(CurrentColor(p).A<=.005f)continue;
                float meshRadius=(int)F(e.Renderer,"m_RenderMode")==4&&e.Meshes.Length>0?e.Meshes[p.MeshIndex].Radius:.71f;
                float radius=CurrentSize(p).Abs().MaxAxisValue()*meshRadius;
                if((int)F(e.Data,"scalingMode")!=2)radius*=e.Source.Scale.Abs().MaxAxisValue();
                radius=Math.Max(.01f,radius);var center=ToLocal(WorldPosition(p));
                var box=new Aabb(center-Vector3.One*radius,Vector3.One*radius*2);
                bounds=has?bounds.Merge(box):box;has=true;
            }
        }
        foreach(var e in _extras) { var center=ToLocal(e.Source.Node.GlobalPosition);var box=new Aabb(center-Vector3.One*5,Vector3.One*10);bounds=has?bounds.Merge(box):box;has=true; }
        PreviewBounds=has?bounds:new Aabb(Vector3.One*-1,Vector3.One*2);Stop();_estimating=false;
    }

    public override void _ExitTree() {
        _document?.Dispose();foreach(var e in _emitters) { e.Noise?.Dispose();e.Draw.Mesh?.Dispose();e.TrailDraw.Mesh?.Dispose(); }
        foreach(var e in _extras)e.Draw.Mesh?.Dispose();foreach(var m in _materials.Concat(_drawMaterials))m.Dispose();
    }
}

internal static class MdVectorExtensions {
    public static float MaxAxisValue(this Vector3 value)=>Math.Max(value.X,Math.Max(value.Y,value.Z));
}
