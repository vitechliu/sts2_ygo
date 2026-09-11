using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;
using VYgo.Core;

namespace VYgo.Patches;

// 两个重载最终汇入此入口。只改路径，沿用原版参数、音量和音效混音路由。
[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.PlayOneShot),
    [typeof(string), typeof(Dictionary<string, float>), typeof(float)])]
public static class UiAudioPatches {
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void Prefix(ref string path) => UiAudioReplacements.Replace(ref path);
}
