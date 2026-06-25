using MegaCrit.Sts2.Core.Helpers;
using STS2RitsuLib.Audio;

namespace VYgo.Utils;

public static class SFXUtil {

    public static void PlayAfter(string path, float time) {
       _ = PlayAsync(path, time);
    }

    static async Task PlayAsync(string path, float time) {
        await VFXUtil.Wait(time);
        Play(path);
    }
    public static AudioPlayResult Play(string path, float volume = 1f) {
        return GameAudioService.Shared.PlayOneShot(
            AudioSource.Event(path),
            new AudioPlaybackOptions
            {
                Volume = volume,
                Scope = AudioLifecycleScope.Room,
            });
    }

    public static AudioLoopHandle Loop(string path, float volume = 1f) {
        return GameAudioService.Shared.PlayLoop(
            AudioSource.Event(path),
            new AudioPlaybackOptions
            {
                Volume = volume,
                Scope = AudioLifecycleScope.Run,
            });
    }

    public static async Task PlayLoopIn(string path, float time, float volume = 1f) {
        var loop = Loop(path, volume);
        await VFXUtil.Wait(time);
        loop.Dispose();
    }
}
