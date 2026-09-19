using Godot;

namespace VYgo.Core.Effects.MasterDuel;

/// <summary>独立预览视口：在同一个 3D 缓冲区完成原材质混合与深度判断。</summary>
[ScriptPath("res://Core/Effects/MasterDuel/NMdEffect2D.cs")]
public partial class NMdEffect2D : Node2D {
    [Export] public PackedScene EffectScene { get; set; } = null!;
    [Export] public bool AutoFree { get; set; }
    [Export] public float DisplaySize { get; set; } = 800;
    [Export] public int Seed { get; set; } = 20260919;
    public NMdEffect3D Effect { get; private set; } = null!;
    public Camera3D Camera { get; private set; } = null!;
    public SubViewport Viewport { get; private set; } = null!;
    private Godot.Environment _environment=null!;

    public override void _Ready() {
        Viewport = new SubViewport { Name="效果视口",Size=new Vector2I(960,540),TransparentBg=false,OwnWorld3D=true,HandleInputLocally=false,RenderTargetUpdateMode=SubViewport.UpdateMode.Always };
        AddChild(Viewport);
        _environment=new Godot.Environment { BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("10151e"),BackgroundEnergyMultiplier=1,TonemapMode=Godot.Environment.ToneMapper.Linear };
        Viewport.AddChild(new WorldEnvironment {Environment=_environment});
        Camera=new Camera3D {Projection=Camera3D.ProjectionType.Orthogonal,Near=.01f,Far=1000,Current=true};Viewport.AddChild(Camera);
        Effect=EffectScene.Instantiate<NMdEffect3D>();Effect.Seed=Seed;Viewport.AddChild(Effect);
        Effect.Finished+=()=> {if(AutoFree)QueueFree();};
        AddChild(new Sprite2D {Texture=Viewport.GetTexture(),Scale=Vector2.One*DisplaySize/960});
        Fit();
    }
    public void Fit(int view=0,float zoom=1) {
        var bounds=Effect.PreviewBounds;var center=bounds.GetCenter();Vector3 direction;
        if(view==0) {var d=bounds.Size;direction=d.Y<d.Z*.45f?new Vector3(0,1,.15f):d.Z<d.Y*.45f?Vector3.Back:new Vector3(0,.5f,1);}
        else direction=view switch {1=>Vector3.Back,2=>new Vector3(0,1,.001f),3=>Vector3.Right,_=>new Vector3(1,.7f,1)};
        float extent=Math.Max(.1f,bounds.Size.Length());Camera.Position=center+direction.Normalized()*extent*2;
        Camera.LookAt(center,Math.Abs(direction.Normalized().Dot(Vector3.Up))>.999f?Vector3.Forward:Vector3.Up);
        var inverse=Camera.GlobalTransform.AffineInverse();float maxX=0,maxY=0;
        for(int i=0;i<8;i++) {var p=inverse*bounds.GetEndpoint(i);maxX=Math.Max(maxX,Math.Abs(p.X));maxY=Math.Max(maxY,Math.Abs(p.Y));}
        Camera.Size=Math.Max(.1f,Math.Max(maxY*2,maxX*2*540/960)*1.12f)/Math.Max(.1f,zoom);Camera.Near=Math.Max(.001f,extent*.0001f);Camera.Far=extent*5;
    }
    public void SetBackground(bool grey) => _environment.BackgroundColor=grey?new Color("4c4c4c"):new Color("10151e");
}
