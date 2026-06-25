using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 3)]
public class LinkSummon() : BaseSummonCard(0, CardType.Skill, CardRarity.Basic, TargetType.None) {

    private async Task MaterialSacrifice(Creature material) {
        var nCreature = material.GetCreatureNode();
        if (nCreature is null) return;
        var visuals = nCreature.Visuals as NMonsterVisuals;
        if (visuals is null) return;

        nCreature.ToggleIsInteractable(false);
        nCreature.AnimHideIntent();

        async Task PlayDeathAnimation() {
            try {
                await visuals.PlayMaterialVfx();
                await visuals.PlayMaterialExitAnimation();
            }
            finally {
                if (Godot.GodotObject.IsInstanceValid(nCreature)) {
                    nCreature.QueueFreeSafely();
                }
            }
        }

        var deathAnimationTask = TaskHelper.RunSafely(PlayDeathAnimation());
        nCreature.DeathAnimationTask = deathAnimationTask;
        await CreatureCmd.Kill(material, true);
        await deathAnimationTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        var pile = Entry.ExtraPile.GetPile(Owner);
        if (pile.Cards.Count <= 0) return;
        
        Entry.Logger.Info("aa1");
        //todo linkSelect
        if ((await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: pile, player: Owner))
            .FirstOrDefault() is not BaseExtraLinkCard cardModel) return;
        var coreCard = cardModel.YgoGetCore();
        if (coreCard is null) {
            Entry.Logger.Error("Failed to get core card: " + cardModel.CardId);
            return;
        }
        Entry.Logger.Info("aa2");

        // TrySelectLinkMaterials currently returns a deferred LINQ iterator over Pets.
        // MaterialSacrifice removes creatures from that collection, so take a snapshot
        // before starting any sacrifice task.
        var materials = TrySelectLinkMaterials(coreCard)?.ToList();
        if (materials is null) return;
        Entry.Logger.Info("aa3");
        
        //第一步，为素材播放选中动画,并处死
        List<Task> anim = new();
        Entry.Logger.Info("aa4");
        
        foreach (var material in materials) {
            anim.Add(TaskHelper.RunSafely(MaterialSacrifice(material)));
        }
        Entry.Logger.Info("aa5");
        
        await Task.WhenAll(anim);

        //第二步，播放卡片动画，并素材进入墓地
        
        //第三步，展开连接圆盘并播放圆盘动画
        
        //第四步，弹出链接目标并播放粒子
        
        //第五步，生成
    }
}
