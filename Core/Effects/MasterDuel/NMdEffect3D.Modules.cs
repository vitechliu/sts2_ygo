using System.Text.Json;
using Godot;
using static VYgo.Core.Effects.MasterDuel.MdCurve;

namespace VYgo.Core.Effects.MasterDuel;

public partial class NMdEffect3D {
    private static Basis Euler(Vector3 radians) => Basis.FromEuler(radians * new Vector3(-1,-1,1), EulerOrder.Yxz);

    private (Vector3,Vector3) SpawnShape(Emitter e,float time,int burstIndex,int burstCount) {
        if(!Enabled(e.Data,"ShapeModule"))return (Vector3.Zero,Vector3.Forward);
        var m=e.Data.GetProperty("ShapeModule");
        float R()=>(float)e.Random.NextDouble();
        Vector3 Sphere() { float z=R()*2-1,a=R()*Mathf.Tau,r=Mathf.Sqrt(1-z*z);return new Vector3(r*Mathf.Cos(a),r*Mathf.Sin(a),z); }
        float radius=F(m.GetProperty("radius"),"value",1),thick=F(m,"radiusThickness",1);
        var arc=m.GetProperty("arc");float angle=F(arc,"value",360)*Mathf.Pi/180;
        float phase=(int)F(arc,"mode") switch { 1=>Mathf.PosMod(time*F(e.Data,"lengthInSec")*Sample(arc.GetProperty("speed"),time),1),2=>1-Math.Abs(Mathf.PosMod(time*F(e.Data,"lengthInSec")*Sample(arc.GetProperty("speed"),time),2)-1),3=>(float)burstIndex/Math.Max(1,burstCount),_=>R() };
        float spread=F(arc,"spread");if(spread>0)phase=Mathf.Floor(phase/spread)*spread;
        float a=phase*angle;Vector3 radial=new(Mathf.Cos(a),Mathf.Sin(a),0),p=Vector3.Zero,d=Vector3.Forward;
        switch((int)F(m,"type")) {
            case 0: case 2:
                d=Sphere();if((int)F(m,"type")==2)d.Z=-Math.Abs(d.Z);
                p=d*radius*Mathf.Pow(Mathf.Lerp(Mathf.Pow(1-thick,3),1,R()),1f/3);break;
            case 4: case 8:
                float radialFraction=Mathf.Sqrt(Mathf.Lerp((1-thick)*(1-thick),1,R()));
                float tangent=Mathf.Tan(Mathf.DegToRad(F(m,"angle")));
                p=radial*(radius*radialFraction);d=(Vector3.Forward+radial*tangent*radialFraction).Normalized();
                if((int)F(m,"type")==8)p+=d*(R()*F(m,"length")/Math.Max(.0001f,-d.Z));break;
            case 5: case 16:
                p=new Vector3(R()-.5f,R()-.5f,R()-.5f);
                if((int)F(m,"type")==16) { int axis=e.Random.Next(3);for(int i=0;i<3;i++)if(i!=axis)p[i]=R()<.5f?-.5f:.5f; }break;
            case 10: p=radial*radius*Mathf.Sqrt(Mathf.Lerp((1-thick)*(1-thick),1,R()));d=radial;break;
            case 12: p=Vector3.Right*((R()*2-1)*radius);d=Vector3.Up;break;
            default:throw new InvalidOperationException("尚未实现的源 Shape："+F(m,"type"));
        }
        p+=Sphere()*F(m,"randomPositionAmount");
        d=d.Lerp(Sphere(),F(m,"randomDirectionAmount")).Normalized();
        var rotation=Euler(Vector(m.GetProperty("m_Rotation"))*Mathf.Pi/180);
        return (rotation*(p*Vector(m.GetProperty("m_Scale")))+Position(m.GetProperty("m_Position")),rotation*d);
    }

    private void UpdateParticle(Emitter e,Particle p,float age,float dt) {
        float t=Math.Clamp(age/p.Lifetime,0,1);
        float C(JsonElement m,string key)=>Sample(m.GetProperty(key),t,p.Random);
        Vector3 InSpace(Vector3 v,bool world) => world==!e.Local ? v : world ? WorldToSimulationVector(e,v):SimulationTransform(e).Basis*v;
        var initial=e.Data.GetProperty("InitialModule");
        p.Velocity+=InSpace(Vector3.Down*(9.81f*C(initial,"gravityModifier")),true)*dt;
        Vector3 velocity=p.Velocity;
        if(Enabled(e.Data,"VelocityModule")) {
            var m=e.Data.GetProperty("VelocityModule");bool world=F(m,"inWorldSpace")!=0;
            Vector3 linear=new(C(m,"x"),C(m,"y"),-C(m,"z"));
            Vector3 orbital=new(-C(m,"orbitalX"),-C(m,"orbitalY"),C(m,"orbitalZ"));
            Vector3 offset=new(C(m,"orbitalOffsetX"),C(m,"orbitalOffsetY"),-C(m,"orbitalOffsetZ"));
            var fromOrigin=e.Local?p.Position:WorldToSimulationVector(e,p.Position-SimulationTransform(e).Origin);
            velocity+=InSpace(linear+orbital.Cross(fromOrigin-offset)+fromOrigin.Normalized()*C(m,"radial"),world);
            velocity*=C(m,"speedModifier");
        }
        if(Enabled(e.Data,"ClampVelocityModule")) {
            var m=e.Data.GetProperty("ClampVelocityModule");var limited=velocity;
            if(F(m,"separateAxis")!=0) {
                bool world=F(m,"inWorldSpace")!=0;var basis=SimulationTransform(e).Basis;
                var v=world==!e.Local?velocity:world?basis*velocity:WorldToSimulationVector(e,velocity);
                for(int i=0;i<3;i++)v[i]=Math.Clamp(v[i],-Math.Abs(C(m,new[]{"x","y","z"}[i])),Math.Abs(C(m,new[]{"x","y","z"}[i])));
                limited=InSpace(v,world);
            } else limited=velocity.LimitLength(Math.Max(0,C(m,"magnitude")));
            var damped=velocity.Lerp(limited,1-Mathf.Pow(1-F(m,"dampen"),dt*60));
            float drag=Math.Max(0,C(m,"drag"));
            if(F(m,"multiplyDragByParticleSize")!=0)drag*=CurrentSize(p).X;
            if(F(m,"multiplyDragByParticleVelocity")!=0)drag*=damped.Length();
            damped*=Mathf.Exp(-drag*dt);p.Velocity+=damped-velocity;velocity=damped;
        }
        p.CurrentVelocity=velocity;p.Position+=velocity*dt;
        if(Enabled(e.Data,"RotationModule")) {
            var m=e.Data.GetProperty("RotationModule");
            p.Rotation+=new Vector3(F(m,"separateAxes")!=0?C(m,"x"):0,F(m,"separateAxes")!=0?C(m,"y"):0,C(m,"curve"))*dt;
        }
        p.NoiseOffset=Vector3.Zero;
        if(Enabled(e.Data,"NoiseModule")) {
            var m=e.Data.GetProperty("NoiseModule");float frequency=F(m,"frequency",1),amp=1,freq=frequency;
            var point=p.Position+new Vector3(p.Stable[0],p.Stable[1],p.Stable[2])*127+Vector3.One*(age*C(m,"scrollSpeed"));
            Vector3 noise=Vector3.Zero;
            for(int i=0;i<(int)F(m,"octaves",1);i++) {
                var q=point*freq;noise+=new Vector3(e.Noise.GetNoise3Dv(q),e.Noise.GetNoise3Dv(q+Vector3.One*43),e.Noise.GetNoise3Dv(q-Vector3.One*71))*amp;
                amp*=F(m,"octaveMultiplier",.5f);freq*=F(m,"octaveScale",2);
            }
            if(F(m,"remapEnabled")!=0)for(int i=0;i<3;i++)noise[i]=Sample(m.GetProperty(i==0||F(m,"separateAxes")==0?"remap":i==1?"remapY":"remapZ"),noise[i]*.5f+.5f,p.Random);
            float strength=C(m,"strength");noise*=new Vector3(strength,F(m,"separateAxes")!=0?C(m,"strengthY"):strength,F(m,"separateAxes")!=0?C(m,"strengthZ"):strength);
            if(F(m,"damping")!=0)noise/=Math.Max(.0001f,frequency);
            p.NoiseOffset=noise*C(m,"positionAmount");p.NoiseValue=noise;
            p.NoiseRotation=noise*(C(m,"rotationAmount")*Mathf.Pi/180);p.NoiseSize=noise*C(m,"sizeAmount");
        }
    }

    private Vector3 WorldToSimulationVector(Emitter e,Vector3 value) {
        if(value==Vector3.Zero)return Vector3.Zero;
        var basis=SimulationTransform(e).Basis;
        if(Math.Abs(basis.Determinant())>1e-12f)return basis.Inverse()*value;
        // 平面效果允许源节点存在零缩放轴；沿塌缩轴的世界运动没有局部分量。
        var rotation=new Basis(GlobalBasis.GetRotationQuaternion()*e.Source.Rotation);
        var local=rotation.Transposed()*value;var scale=e.Source.Scale;
        for(int i=0;i<3;i++)local[i]=Math.Abs(scale[i])>1e-8f?local[i]/scale[i]:0;
        return local;
    }

    private float[] Custom(Particle p,int channel) {
        var data=p.Run.Emitter.Data;float[] result=new float[4];
        if(!Enabled(data,"CustomDataModule"))return result;
        var m=data.GetProperty("CustomDataModule");float t=Math.Clamp((Elapsed-p.Birth)/p.Lifetime,0,1);
        if((int)F(m,"mode"+channel)==2) { var c=Gradient(m.GetProperty("color"+channel),t,p.Random);return [c.R,c.G,c.B,c.A]; }
        if((int)F(m,"mode"+channel)==1)for(int i=0;i<(int)F(m,"vectorComponentCount"+channel);i++)result[i]=Sample(m.GetProperty($"vector{channel}_{i}"),t,p.Random);
        return result;
    }
}
