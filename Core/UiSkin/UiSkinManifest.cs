using System.Text.Json;
using Godot;

namespace VYgo.Core.UiSkin;

/// <summary>工作台导出的可移植清单，不包含编辑机路径。</summary>
public sealed class UiSkinManifest {
    public int SchemaVersion { get; set; }
    public string Fingerprint { get; set; } = "";
    public List<UiSkinRule> Rules { get; set; } = [];
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
}

public sealed class UiSkinRule {
    public string Id { get; set; } = "";
    public string ComponentId { get; set; } = "";
    public string ComponentScene { get; set; } = "";
    public string Adapter { get; set; } = "static";
    public string Property { get; set; } = "texture";
    public string NodePath { get; set; } = ".";
    public List<UiSkinSelector> Selectors { get; set; } = [];
    public int Priority { get; set; }
    public string ExpectedType { get; set; } = "";
    public bool TextureVariant { get; set; }
    public UiSkinTextureIdentity Texture { get; set; } = new();
    public Dictionary<string, string> States { get; set; } = [];
    public string Mode { get; set; } = "contain";
    public int[] Border { get; set; } = [0, 0, 0, 0];
    public bool Neutralize { get; set; }
    public UiSkinLayout Layout { get; set; } = new();
}
public sealed class UiSkinSelector {
    public string Scene { get; set; } = "";
    public string NodePath { get; set; } = ".";
}
public sealed class UiSkinLayout {
    public UiSkinLayoutEdit? Visual { get; set; }
    public UiSkinLayoutEdit? Hitbox { get; set; }
    public UiSkinTextEdit? Text { get; set; }
}
public sealed class UiSkinLayoutEdit {
    public string NodePath { get; set; } = ".";
    public int[] Offsets { get; set; } = [];
}
public sealed class UiSkinTextEdit {
    public string NodePath { get; set; } = ".";
    public int[] Margins { get; set; } = [];
}
public sealed class UiSkinTextureIdentity {
    public float[]? Color { get; set; }
    public bool MatchesColor(ColorRect rect) => Color is { Length: 4 } c && rect.Color.IsEqualApprox(new Color(c[0], c[1], c[2], c[3]));
    public string Path { get; set; } = "";
    public string Atlas { get; set; } = "";
    public float[] Region { get; set; } = [0, 0, 0, 0];
    public float[] Margin { get; set; } = [0, 0, 0, 0];

    public static UiSkinTextureIdentity From(Texture2D texture) {
        var result = new UiSkinTextureIdentity { Path = texture.ResourcePath };
        if (texture is AtlasTexture atlas) {
            result.Atlas = atlas.Atlas?.ResourcePath ?? "";
            result.Region = [atlas.Region.Position.X, atlas.Region.Position.Y, atlas.Region.Size.X, atlas.Region.Size.Y];
            result.Margin = [atlas.Margin.Position.X, atlas.Margin.Position.Y, atlas.Margin.Size.X, atlas.Margin.Size.Y];
        }
        return result;
    }
    public bool Matches(Texture2D texture) {
        if (!string.IsNullOrEmpty(Path) && texture.ResourcePath == Path) return true;
        if (texture is not AtlasTexture atlas || string.IsNullOrEmpty(Atlas) || atlas.Atlas?.ResourcePath != Atlas) return false;
        return Region.Length == 4 && Margin.Length == 4
            && atlas.Region.IsEqualApprox(new Rect2(Region[0], Region[1], Region[2], Region[3]))
            && atlas.Margin.IsEqualApprox(new Rect2(Margin[0], Margin[1], Margin[2], Margin[3]));
    }
}
