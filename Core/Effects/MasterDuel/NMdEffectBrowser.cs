using System.Text.Json;
using Godot;

namespace VYgo.Core.Effects.MasterDuel;

/// <summary>74 个独立包的浏览、播放与正常速度运行检查，不依赖游戏单例。</summary>
[ScriptPath("res://Core/Effects/MasterDuel/NMdEffectBrowser.cs")]
public partial class NMdEffectBrowser : Control {
    private sealed record Entry(string Id,string Name,string Category,string Title,int Particles,bool Loop,bool Trails,string[] Notes);
    private readonly List<Entry> _entries=[],_filtered=[];
    private ItemList _list=null!;
    private RichTextLabel _detail=null!;
    private Label _time=null!,_count=null!;
    private LineEdit _search=null!;
    private OptionButton _category=null!,_view=null!;
    private CheckBox _repeat=null!,_motion=null!,_grey=null!;
    private Button _pause=null!;
    private SpinBox _seed=null!,_speed=null!,_zoom=null!;
    private NMdEffect2D? _current;
    private Entry? _selected;
    private bool _sequence,_verify,_processing,_warming;
    private double _sincePlay,_wallStart,_maxDelta;
    private int _verifyIndex,_peak,_visiblePeak,_verticesPeak,_frames,_pixelsPeak,_failed;
    private string _captureDirectory="";
    private readonly List<object> _evidence=[];
    private readonly List<(string Path,Image Image)> _captures=[];
    private int _captured;
    private double[] _captureAt=[];

    public override void _Ready() {
        GetWindow().MinSize=new Vector2I(1280,800);
        Theme=new Theme {DefaultFont=new SystemFont {FontNames=["Microsoft YaHei UI","Microsoft YaHei"]},DefaultFontSize=16};
        var bg=new ColorRect {Color=new Color("10151e"),MouseFilter=MouseFilterEnum.Ignore};AddChild(bg);bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Label("大师决斗 · 独立特效预览",28,20,26);Label("74 个独立包 · 仅预览 · 原作视觉一致性待对照",28,62,16);
        _search=new LineEdit {Position=new Vector2(28,102),Size=new Vector2(356,38),PlaceholderText="搜索源名称或分类"};AddChild(_search);_search.TextChanged+=_=>Filter();
        _category=new OptionButton {Position=new Vector2(28,150),Size=new Vector2(270,36)};AddChild(_category);_category.ItemSelected+=_=>Filter();_count=Label("",308,158,15);
        _list=new ItemList {Position=new Vector2(28,198),Size=new Vector2(356,534)};AddChild(_list);_list.ItemSelected+=i=> { _sequence=false;Select(_filtered[(int)i]); };
        _view=new OptionButton {Position=new Vector2(420,102),Size=new Vector2(145,36)};foreach(var s in new[]{"自动视角","正面","俯视","侧面","斜视"})_view.AddItem(s);AddChild(_view);_view.ItemSelected+=_=>Fit();
        Label("缩放",580,110,15);_zoom=Number(627,102,95,.1,10,.1,1);_zoom.ValueChanged+=_=>Fit();
        _grey=new CheckBox {Text="灰色背景",Position=new Vector2(738,103)};AddChild(_grey);_grey.Toggled+=v=>_current?.SetBackground(v);
        _motion=new CheckBox {Text="移动演示（非源轨迹）",Position=new Vector2(900,103)};AddChild(_motion);_motion.Toggled+=v=> { if(_current!=null)_current.Effect.DemoMotion=v; };
        _detail=new RichTextLabel {Position=new Vector2(420,610),Size=new Vector2(820,78),ScrollActive=true};_detail.AddThemeFontSizeOverride("normal_font_size",15);AddChild(_detail);_time=Label("",420,582,16);
        Button("上一项",420,698,()=>Move(-1));Button("重播",516,698,()=>PlaySelected());
        _pause=Button("暂停",612,698,()=> {if(_current==null)return;_current.Effect.Paused=!_current.Effect.Paused;_pause.Text=_current.Effect.Paused?"继续":"暂停";});
        Button("停止",708,698,()=> {_sequence=false;_repeat.ButtonPressed=false;_current?.Effect.Stop();});
        Button("下一项",804,698,()=>Move(1));Button("播放全部",900,698,()=> {if(_filtered.Count>0){_sequence=true;Select(_filtered[0]);}});
        _repeat=new CheckBox {Text="重复",Position=new Vector2(1004,701),ButtonPressed=true};AddChild(_repeat);
        Label("速度",420,757,15);_speed=Number(468,747,112,.1,2,.1,1);_speed.ValueChanged+=v=> {if(_current!=null)_current.Effect.PlaybackSpeed=(float)v;};
        Label("种子",604,757,15);_seed=Number(652,747,180,0,int.MaxValue,1,20260919);Button("新种子",852,747,()=> {_seed.Value=GD.Randi()%int.MaxValue;PlaySelected();});
        Label("62 个需脚本控制的入口另行登记；\n不属于本列表的 74 个独立资源包。",28,752,14);
        using var doc=JsonDocument.Parse(GD.Load<MdSourceData>(MdEffectLibrary.Root+"catalog.tres").DataJson);
        foreach(var e in doc.RootElement.EnumerateArray())_entries.Add(new Entry(e.GetProperty("id").GetString()!,e.GetProperty("name").GetString()!,e.GetProperty("category").GetString()!,e.GetProperty("title").GetString()!,e.GetProperty("particles").GetInt32(),e.GetProperty("loop").GetBoolean(),e.GetProperty("trails").GetBoolean(),e.GetProperty("notes").EnumerateArray().Select(n=>n.GetString()!).ToArray()));
        var onlyArgument=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--md-effects-only="));
        if(onlyArgument!=null) {var ids=onlyArgument.Split('=',2)[1].Split(',');_entries.RemoveAll(e=>!ids.Contains(e.Id));}
        _category.AddItem("全部分类");foreach(var c in _entries.Select(e=>e.Category).Distinct())_category.AddItem(c);
        _verify=OS.GetCmdlineUserArgs().Contains("--md-effects-verify");
        if(_verify) { _captureDirectory=ProjectSettings.GlobalizePath("res://.context/master-duel-effects/"+(onlyArgument==null?"all-74":"recheck"));System.IO.Directory.CreateDirectory(_captureDirectory); }
        Filter();
    }
    private void Filter() {
        _list.Clear();_filtered.Clear();
        foreach(var e in _entries.Where(e=>(_category.Selected<=0||e.Category==_category.GetItemText(_category.Selected))&&(e.Name+e.Category).Contains(_search.Text,StringComparison.OrdinalIgnoreCase))) {
            _filtered.Add(e);_list.AddItem($"{_entries.IndexOf(e)+1:00}  "+e.Title+(e.Loop?"  ↻":""));_list.SetItemTooltip(_list.ItemCount-1,e.Name);
        }
        _count.Text=$"{_filtered.Count} 项";
        if(_filtered.Count>0)Select(_selected==null&&!_verify?_filtered.FirstOrDefault(e=>e.Id=="hit_03")??_filtered[0]:_filtered[0]);
    }
    private void Move(int offset) { _sequence=false;if(_filtered.Count>0)Select(_filtered[Mathf.PosMod(_filtered.IndexOf(_selected!)+offset,_filtered.Count)]); }
    private void Select(Entry entry) {
        _selected=entry;int i=_filtered.IndexOf(entry);if(i>=0){_list.Select(i);_list.EnsureCurrentIsVisible();}
        _detail.Text=entry.Name+$"\n{entry.Category} · {entry.Particles} 个粒子系统 · "+(entry.Loop?"源循环":"源单次")+" · 视觉待对照\n"+string.Join("；",entry.Notes);
        _motion.Visible=entry.Trails;_motion.ButtonPressed=false;_view.Select(0);_zoom.Value=1;PlaySelected();
    }
    private void Fit()=>_current?.Fit(_view.Selected,(float)_zoom.Value);
    private void PlaySelected() {
        if(_current!=null) {RemoveChild(_current);_current.QueueFree();_current=null;}
        if(_selected==null)return;
        _current=MdEffectLibrary.Create(_selected.Id);_current.AutoFree=false;_current.DisplaySize=800;_current.Seed=(int)_seed.Value;_current.Position=new Vector2(828,350);AddChild(_current);
        _current.Effect.PlaybackSpeed=(float)_speed.Value;_current.Effect.DemoMotion=_motion.ButtonPressed;_current.SetBackground(_grey.ButtonPressed);Fit();
        _pause.Text="暂停";ResetCounters();_warming=_verify;
        if(_verify)GD.Print($"MD_EFFECT_START {_verifyIndex+1}/{_entries.Count} {_selected.Id} {_selected.Title}");
    }
    private void ResetCounters() { _sincePlay=0;_wallStart=Time.GetTicksMsec()/1000.0;_peak=_visiblePeak=_verticesPeak=_frames=_captured=_pixelsPeak=0;_maxDelta=0; }
    public override async void _Process(double delta) {
        if(_current==null||_processing)return;
        var effect=_current.Effect;if(!effect.Paused)_sincePlay+=delta;
        _time.Text=effect.PlaybackError!=""?"播放失败："+effect.PlaybackError:$"{effect.Elapsed:0.00} 秒 · 活跃 / 可见粒子 {effect.LiveParticleCount} / {effect.VisibleParticleCount} · {(effect.Paused?"已暂停":effect.Playing?"播放中":"已结束")} · {effect.PlaybackSpeed:0.0}×";
        if(_verify) {
            if(_warming) {
                if(_sincePlay<1.5)return;
                if(effect.PlaybackError=="")effect.Play((int)_seed.Value);effect.DemoMotion=_selected!.Trails;ResetCounters();_warming=false;
                double end=Math.Clamp(effect.Duration+1.5,4,10);_captureAt=[.12,.35,.7,1.5,end*.75];return;
            }
            _peak=Math.Max(_peak,effect.LiveParticleCount);_visiblePeak=Math.Max(_visiblePeak,effect.VisibleParticleCount);_verticesPeak=Math.Max(_verticesPeak,effect.DrawVertexCount);_frames++;_maxDelta=Math.Max(_maxDelta,delta);
            if(_captured<_captureAt.Length&&_sincePlay>=_captureAt[_captured]) {
                _processing=true;await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                var capture=_current.Viewport.GetTexture().GetImage();capture.Convert(Image.Format.Rgba8);
                var bytes=capture.GetData();int changed=0;for(int i=0;i<bytes.Length;i+=4)if(Math.Abs(bytes[i]-16)+Math.Abs(bytes[i+1]-21)+Math.Abs(bytes[i+2]-30)>12)changed++;
                _pixelsPeak=Math.Max(_pixelsPeak,changed);_captures.Add((System.IO.Path.Combine(_captureDirectory,$"{_selected!.Id}_{_captured:D2}.png"),capture));_captured++;_processing=false;
            }
            if(_sincePlay>=Math.Clamp(effect.Duration+1.5,4,10)) {
                _processing=true;string id=_selected!.Id;int emitted=effect.EmittedParticleCount;double elapsed=effect.Elapsed,wall=Time.GetTicksMsec()/1000.0-_wallStart;
                float cameraSize=_current.Camera.Size;effect.Stop();bool clean=effect.LiveParticleCount==0&&!effect.Playing&&effect.DrawVertexCount==0;
                bool passed=effect.PlaybackError==""&&emitted>0&&_peak>0&&_verticesPeak>0&&_pixelsPeak>0&&clean;if(!passed)_failed++;
                _evidence.Add(new {id,name=_selected.Name,emitted,peak=_peak,visiblePeak=_visiblePeak,verticesPeak=_verticesPeak,pixelsPeak=_pixelsPeak,cameraSize,elapsed,wall,frames=_frames,maxFrameSeconds=_maxDelta,normalSpeed=effect.PlaybackSpeed==1,loop=_selected.Loop,motionDemo=_selected.Trails,stoppedCleanly=clean,passed});
                foreach(var c in _captures){c.Image.SavePng(c.Path);c.Image.Dispose();}_captures.Clear();
                System.IO.File.WriteAllText(System.IO.Path.Combine(_captureDirectory,"results.json"),JsonSerializer.Serialize(_evidence,new JsonSerializerOptions {WriteIndented=true}));
                GD.Print($"MD_EFFECT_CHECK {id}: emitted={emitted} peak={_peak} vertices={_verticesPeak} clean={clean} pass={passed}");
                if(++_verifyIndex<_entries.Count)Select(_entries[_verifyIndex]);else {GD.Print($"MD_EFFECT_CHECK {_entries.Count} 项运行检查结束，失败 {_failed} 项；原作视觉一致性仍待对照。");GetTree().Quit(_failed==0?0:1);}_processing=false;
            }
        } else if((_sequence&&_sincePlay>=Math.Clamp(effect.Duration+1.5,4,10))||(!_sequence&&_repeat.ButtonPressed&&!effect.Playing&&_sincePlay>2)) {
            if(_sequence) {int next=_filtered.IndexOf(_selected!)+1;if(next<_filtered.Count)Select(_filtered[next]);else if(_repeat.ButtonPressed)Select(_filtered[0]);else _sequence=false;}else PlaySelected();
        }
    }
    private Label Label(string text,float x,float y,int font) {var l=new Label {Text=text,Position=new Vector2(x,y)};l.AddThemeFontSizeOverride("font_size",font);AddChild(l);return l;}
    private Button Button(string text,float x,float y,Action action) {var b=new Button {Text=text,Position=new Vector2(x,y),Size=new Vector2(88,38)};AddChild(b);b.Pressed+=action;return b;}
    private SpinBox Number(float x,float y,float width,double min,double max,double step,double value) {var n=new SpinBox {Position=new Vector2(x,y),Size=new Vector2(width,38),MinValue=min,MaxValue=max,Step=step,Value=value};AddChild(n);return n;}
}
