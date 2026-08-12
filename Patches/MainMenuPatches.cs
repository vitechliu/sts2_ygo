using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2RitsuLib.Audio;
using VYgo.Scripts;
using VYgo.Scripts.UI;

namespace VYgo.Patches;

[HarmonyPatch]
public static class MainMenuPatches {
    private const string MainMenuMusicEvent = "event:/vygo/music/main_menu";
    private const string MainMenuMusicGuid = "{197b80ae-ae3d-45bf-9432-4df4a71bc092}";
    private static WeakReference<NMainMenu>? _activeMainMenu;
    private static bool _deferredAudioReady;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
    private static void AfterMainMenuReady(NMainMenu __instance) {
        _activeMainMenu = new WeakReference<NMainMenu>(__instance);

        //等待其他帧
        Callable.From(() => {
            if (!GodotObject.IsInstanceValid(__instance) || !__instance.IsInsideTree()) return;

            try {
                MainMenuSkinController.Install(__instance);
            }
            catch (Exception exception) {
                Entry.Logger.Error($"Main-menu skin installation failed; keeping the vanilla menu: {exception}");
            }
        }).CallDeferred();

        if (_deferredAudioReady) {
            TryPlayMainMenuMusic(__instance);
        }
        else {
            Entry.Logger.Info("Main-menu music is waiting for RitsuLib deferred FMOD initialization.");
        }
    }

    internal static void NotifyDeferredAudioReady() {
        _deferredAudioReady = true;
        Entry.Logger.Info("RitsuLib deferred initialization completed; main-menu music is ready.");

        if (!TryGetActiveMainMenu(out _)) {
            return;
        }

        // Let every DeferredInitializationCompletedEvent handler finish first. RitsuLib's
        // bank flush waits for all FMOD loads synchronously during this lifecycle dispatch.
        Callable.From(() => {
            if (TryGetActiveMainMenu(out var mainMenu)) {
                TryPlayMainMenuMusic(mainMenu);
            }
        }).CallDeferred();
    }

    private static bool TryGetActiveMainMenu(out NMainMenu mainMenu) {
        if (_activeMainMenu?.TryGetTarget(out mainMenu!) == true
            && GodotObject.IsInstanceValid(mainMenu)
            && mainMenu.IsInsideTree()) {
            return true;
        }

        mainMenu = null!;
        return false;
    }

    private static void TryPlayMainMenuMusic(NMainMenu mainMenu) {
        if (!GodotObject.IsInstanceValid(mainMenu) || !mainMenu.IsInsideTree()) {
            return;
        }

        try {
            if (FmodStudioServer.TryCheckEventGuid(MainMenuMusicGuid) != true) {
                Entry.Logger.Warn("VYgo main-menu FMOD event is not loaded; keeping vanilla music.");
                return;
            }

            var audioManager = NAudioManager.Instance;
            if (audioManager is null) {
                Entry.Logger.Warn("NAudioManager is unavailable; keeping vanilla main-menu music.");
                return;
            }

            // Keep the game's native master/BGM volume controls and music lifecycle.
            audioManager.PlayMusic(MainMenuMusicEvent);
            Entry.Logger.Info("VYgo main-menu music started.");
        }
        catch (Exception exception) {
            // Vanilla music was already started earlier in NMainMenu._Ready.
            Entry.Logger.Warn($"Main-menu music replacement failed; keeping vanilla music: {exception.Message}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NMainMenu), "MainMenuButtonFocused")]
    private static bool BeforeMainMenuButtonFocused(NMainMenuTextButton button) {
        return button.GetParent()?.Name != MainMenuSkinController.ToolbarName
            && !MainMenuLeftMenuController.IsCustomizedButton(button);
    }
}
