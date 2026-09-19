using System.Text.Json;
using Godot;

namespace VYgo.Core.Effects.MasterDuel;

/// <summary>读取 Unity 原始曲线；不用线性采样替换 Hermite 切线。</summary>
internal static class MdCurve {
    public static float F(JsonElement j, string key, float fallback = 0) =>
        j.ValueKind == JsonValueKind.Object && j.TryGetProperty(key, out var v)
            ? v.ValueKind == JsonValueKind.True ? 1 : v.ValueKind == JsonValueKind.False ? 0 :
                v.ValueKind == JsonValueKind.String ? float.Parse(v.GetString()!, System.Globalization.CultureInfo.InvariantCulture) : v.GetSingle()
            : fallback;
    public static bool Enabled(JsonElement j, string key) => j.TryGetProperty(key, out var m) && F(m, "enabled") != 0;
    public static Vector3 Vector(JsonElement j) => new(F(j, "x"), F(j, "y"), F(j, "z"));
    public static Vector3 Position(JsonElement j) => Vector(j) * new Vector3(1, 1, -1);
    public static Quaternion Rotation(JsonElement j) => new Quaternion(-F(j, "x"), -F(j, "y"), F(j, "z"), F(j, "w", 1)).Normalized();
    public static Color Color(JsonElement j) => new(F(j, "r"), F(j, "g"), F(j, "b"), F(j, "a", 1));

    public static float Sample(JsonElement curve, float t, float random = 0.5f) {
        float scalar = F(curve, "scalar");
        return (int)F(curve, "minMaxState") switch {
            0 => scalar,
            1 => scalar * Hermite(curve.GetProperty("maxCurve"), t),
            2 => scalar * Mathf.Lerp(Hermite(curve.GetProperty("minCurve"), t),
                Hermite(curve.GetProperty("maxCurve"), t), random),
            3 => Mathf.Lerp(F(curve, "minScalar"), scalar, random),
            _ => throw new InvalidOperationException("尚未实现的源曲线类型。")
        };
    }

    public static float Hermite(JsonElement curve, float t) {
        var keys = curve.GetProperty("m_Curve");
        if (keys.GetArrayLength() == 0) return 0;
        var a = keys[0];
        if (t <= F(a, "time")) return F(a, "value");
        foreach (var b in keys.EnumerateArray().Skip(1)) {
            float dt = F(b, "time") - F(a, "time");
            if (dt > 0 && t <= F(b, "time")) {
                if (!float.IsFinite(F(a, "outSlope")) || !float.IsFinite(F(b, "inSlope")))
                    return t < F(b, "time") ? F(a, "value") : F(b, "value");
                float u = (t - F(a, "time")) / dt;
                float u2 = u * u, u3 = u2 * u;
                return (2*u3-3*u2+1)*F(a,"value") + (u3-2*u2+u)*dt*F(a,"outSlope")
                     + (-2*u3+3*u2)*F(b,"value") + (u3-u2)*dt*F(b,"inSlope");
            }
            a = b;
        }
        return F(a, "value");
    }

    public static Color Gradient(JsonElement value, float t, float random) => (int)F(value, "minMaxState") switch {
        0 => Color(value.GetProperty("maxColor")),
        1 => EvaluateGradient(value.GetProperty("maxGradient"), t),
        2 => Color(value.GetProperty("minColor")).Lerp(Color(value.GetProperty("maxColor")), random),
        3 => EvaluateGradient(value.GetProperty("minGradient"), t).Lerp(EvaluateGradient(value.GetProperty("maxGradient"), t), random),
        4 => EvaluateGradient(value.GetProperty("maxGradient"), random),
        _ => throw new InvalidOperationException("尚未实现的源渐变类型。")
    };

    public static Color EvaluateGradient(JsonElement j, float t) {
        Color rgb = SampleKeys(j, t, false), alpha = SampleKeys(j, t, true);
        return new Color(rgb.R, rgb.G, rgb.B, alpha.A);
    }

    private static Color SampleKeys(JsonElement j, float t, bool alpha) {
        int count = (int)F(j, alpha ? "m_NumAlphaKeys" : "m_NumColorKeys");
        string prefix = alpha ? "atime" : "ctime";
        Color a = Color(j.GetProperty("key0"));
        float at = F(j, prefix + "0") / 65535f;
        if (t <= at) return a;
        for (int i = 1; i < count; i++) {
            Color b = Color(j.GetProperty("key" + i));
            float bt = F(j, prefix + i) / 65535f;
            if (t <= bt && bt > at)
                return F(j, "m_Mode") == 1 ? b : a.Lerp(b, Mathf.Clamp((t - at) / (bt - at), 0, 1));
            a = b; at = bt;
        }
        return a;
    }
}
