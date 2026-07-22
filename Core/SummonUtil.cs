using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using VYgo.Core.Cards;
using VYgo.Scripts;
using VYgo.Scripts.Cards;
using VYgo.Utils;

namespace VYgo.Core;

public sealed record FusionSummonRequest(
    CardModel? SourceCard,
    Player Owner,
    PlayerChoiceContext ChoiceContext,
    LocString SelectionPrompt,
    Func<BaseExtraFusionCard, IReadOnlyList<SummonMaterial>> GetAvailableMaterials,
    Func<BaseExtraFusionCard, bool>? FusionCardFilter = null
);

public sealed record LinkSummonRequest(
    CardModel SourceCard,
    Player Owner,
    PlayerChoiceContext ChoiceContext,
    LocString SelectionPrompt,
    Func<BaseExtraLinkCard, CoreCard, IReadOnlyList<SummonMaterial>> GetAvailableMaterials
);

public sealed record ExtraDeckSummonRequest(
    CardModel? SourceCard,
    Player Owner,
    PlayerChoiceContext ChoiceContext,
    LocString SelectionPrompt,
    Func<CardModel, bool> ExtraCardFilter,
    Func<CardModel, SummonMaterialSelectionSpec?> BuildMaterialSelection,
    Func<SummonAnimationContext, Task> PlayAnimation,
    Func<IReadOnlyList<SummonMaterial>, Task>? ConsumeMaterials = null,
    float FinalWaitSeconds = 0.45f
);

public sealed record SelectedExtraDeckSummonRequest(
    CardModel SelectedExtraCard,
    Player Owner,
    PlayerChoiceContext ChoiceContext,
    Func<SummonMaterialSelectionSpec?> BuildMaterialSelection,
    Func<SummonAnimationContext, Task> PlayAnimation,
    CardModel? SourceCard = null,
    Func<IReadOnlyList<SummonMaterial>, Task>? ConsumeMaterials = null,
    float FinalWaitSeconds = 0.45f
);

public sealed record SummonAnimationContext(
    CardModel FinalCard,
    IReadOnlyList<SummonMaterial> Materials,
    IReadOnlyList<CardModel> MaterialCards,
    Vector2 ScreenCenterPos
);

public static class SummonUtil {
    public static Task ExecuteFusionSummon(FusionSummonRequest request) {
        return ExecuteExtraDeckSummon(new ExtraDeckSummonRequest(
            SourceCard: request.SourceCard,
            Owner: request.Owner,
            ChoiceContext: request.ChoiceContext,
            SelectionPrompt: request.SelectionPrompt,
            ExtraCardFilter: card => card is BaseExtraFusionCard fusionCard
                && (request.FusionCardFilter?.Invoke(fusionCard) ?? true),
            BuildMaterialSelection: card => BuildFusionMaterialSelection(
                card,
                request.Owner,
                request.GetAvailableMaterials
            ),
            PlayAnimation: ExtraDeckSummonAnimations.PlayFusionSummonAnimation,
            FinalWaitSeconds: 0.45f
        ));
    }

    public static Task ExecuteLinkSummon(LinkSummonRequest request) {
        return ExecuteExtraDeckSummon(new ExtraDeckSummonRequest(
            SourceCard: request.SourceCard,
            Owner: request.Owner,
            ChoiceContext: request.ChoiceContext,
            SelectionPrompt: request.SelectionPrompt,
            ExtraCardFilter: card => card is BaseExtraLinkCard,
            BuildMaterialSelection: card => BuildLinkMaterialSelection(
                card,
                request.Owner,
                request.GetAvailableMaterials
            ),
            PlayAnimation: ExtraDeckSummonAnimations.PlayLinkSummonAnimation,
            FinalWaitSeconds: 0.8f
        ));
    }

    public static IReadOnlyList<SummonMaterial> GetFieldMonsterMaterials(
        Player owner,
        Func<SummonMaterial, bool>? filter = null
    ) {
        return owner.Creature.Pets
            .Where(SummonMaterial.IsFieldMonster)
            .Select(SummonMaterial.FromFieldMonster)
            .Where(material => material.Card != null)
            .Where(material => filter?.Invoke(material) ?? true)
            .ToList();
    }

    public static IReadOnlyList<SummonMaterial> GetFieldAndHandMonsterMaterials(
        Player owner,
        Func<SummonMaterial, bool>? filter = null
    ) {
        List<SummonMaterial> materials = GetFieldMonsterMaterials(owner, filter).ToList();
        materials.AddRange(PileType.Hand.GetPile(owner).Cards
            .Where(SummonMaterial.IsHandMonsterCard)
            .Select(SummonMaterial.FromHandMonsterCard)
            .Where(material => filter?.Invoke(material) ?? true));
        return materials;
    }

    public static bool HasFusionSummonTarget(
        Player owner,
        Func<BaseExtraFusionCard, IReadOnlyList<SummonMaterial>> getAvailableMaterials,
        Func<BaseExtraFusionCard, bool>? fusionCardFilter = null
    ) {
        return Entry.ExtraPile.GetPile(owner).Cards
            .OfType<BaseExtraFusionCard>()
            .Where(card => fusionCardFilter?.Invoke(card) ?? true)
            .Any(card => BuildFusionMaterialSelection(
                card,
                owner,
                getAvailableMaterials
            )?.HasValidCombination == true);
    }

    public static async Task ExecuteExtraDeckSummon(ExtraDeckSummonRequest request) {
        CardPile extraPile = Entry.ExtraPile.GetPile(request.Owner);
        if (extraPile.Cards.Count <= 0) return;

        List<CardModel> summonableCards = extraPile.Cards
            .Where(request.ExtraCardFilter)
            .Where(card => request.BuildMaterialSelection(card)?.HasValidCombination == true)
            .ToList();
        if (summonableCards.Count <= 0) return;

        CardModel? selectedExtraCard = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(request.SelectionPrompt, 1),
                context: request.ChoiceContext,
                pile: extraPile,
                player: request.Owner,
                filter: summonableCards.Contains))
            .FirstOrDefault();
        if (selectedExtraCard == null || !extraPile.Cards.Contains(selectedExtraCard)) return;

        await ExecuteSelectedExtraDeckSummon(new SelectedExtraDeckSummonRequest(
            SelectedExtraCard: selectedExtraCard,
            Owner: request.Owner,
            ChoiceContext: request.ChoiceContext,
            BuildMaterialSelection: () => request.BuildMaterialSelection(selectedExtraCard),
            PlayAnimation: request.PlayAnimation,
            SourceCard: request.SourceCard,
            ConsumeMaterials: request.ConsumeMaterials,
            FinalWaitSeconds: request.FinalWaitSeconds
        ));
    }

    public static async Task ExecuteSelectedExtraDeckSummon(SelectedExtraDeckSummonRequest request) {
        CardPile extraPile = Entry.ExtraPile.GetPile(request.Owner);
        CardModel selectedExtraCard = request.SelectedExtraCard;
        if (selectedExtraCard.Owner != request.Owner
            || selectedExtraCard.Pile != extraPile
            || !extraPile.Cards.Contains(selectedExtraCard)) {
            return;
        }

        IReadOnlyList<SummonMaterial> selectedMaterials = await SummonMaterialSelectCmd.Select(
            request.ChoiceContext,
            request.Owner,
            selectedExtraCard,
            request.BuildMaterialSelection
        );
        if (selectedMaterials.Count <= 0) return;

        SummonMaterialSelectionSpec? latestSpec = request.BuildMaterialSelection();
        if (latestSpec == null || !extraPile.Cards.Contains(selectedExtraCard)) return;

        IReadOnlyList<SummonMaterial> materials = latestSpec.ResolveMaterials(
            selectedMaterials.Select(material => material.Card).OfType<CardModel>()
        );
        if (!latestSpec.IsValidSelection(materials)) return;

        if (TestMode.IsOn || NCombatRoom.Instance == null) return;

        NCard? sourceNode = null;
        if (request.SourceCard != null) {
            sourceNode = NCard.FindOnTable(request.SourceCard);
            if (sourceNode == null || !GodotObject.IsInstanceValid(sourceNode) || !sourceNode.IsInsideTree()) {
                return;
            }

            sourceNode.PlayPileTween?.FastForwardToCompletion();
            sourceNode.Visible = false;
        }

        try {
            IReadOnlyList<CardModel> materialCards = materials
                .Select(material => material.Card)
                .OfType<CardModel>()
                .ToList();

            if (request.ConsumeMaterials != null) {
                await request.ConsumeMaterials(materials);
            }
            else {
                await ConsumeSummonMaterials(request.ChoiceContext, materials);
            }

            Vector2 screenCenterPos = NGame.Instance.GetViewportRect().Size * 0.5f;
            await request.PlayAnimation(new SummonAnimationContext(
                FinalCard: selectedExtraCard,
                Materials: materials,
                MaterialCards: materialCards,
                ScreenCenterPos: screenCenterPos
            ));

            if (!selectedExtraCard.Owner.Creature.IsDead) {
                await CardCmd.AutoPlay(
                    request.ChoiceContext,
                    selectedExtraCard,
                    null,
                    AutoPlayType.Default,
                    false,
                    true
                );
            }

            await VFXUtil.Wait(request.FinalWaitSeconds);
        }
        finally {
            if (sourceNode != null && GodotObject.IsInstanceValid(sourceNode)) {
                sourceNode.Visible = true;
            }
        }
    }

    private static SummonMaterialSelectionSpec? BuildFusionMaterialSelection(
        CardModel card,
        Player owner,
        Func<BaseExtraFusionCard, IReadOnlyList<SummonMaterial>> getAvailableMaterials
    ) {
        if (card is not BaseExtraFusionCard fusionCard) return null;

        IReadOnlyList<SummonMaterial> candidates = getAvailableMaterials(fusionCard)
            .Where(material => material.Card != null)
            .Where(fusionCard.CanUseFusionMaterial)
            .ToList();

        return new SummonMaterialSelectionSpec(
            candidates,
            fusionCard.MinFusionMaterialCount,
            fusionCard.MaxFusionMaterialCount,
            materials => fusionCard.HasValidFusionMaterials(materials)
                && CanSummonWithMaterials(owner, materials)
        );
    }

    internal static SummonMaterialSelectionSpec? BuildLinkMaterialSelection(
        CardModel card,
        Player owner,
        Func<BaseExtraLinkCard, CoreCard, IReadOnlyList<SummonMaterial>> getAvailableMaterials
    ) {
        if (card is not BaseExtraLinkCard linkCard) return null;

        CoreCard? coreCard = linkCard.YgoGetCore();
        if (coreCard == null) {
            Entry.Logger.Error("Failed to get core card: " + linkCard.CardId);
            return null;
        }

        IReadOnlyList<SummonMaterial> candidates = getAvailableMaterials(linkCard, coreCard)
            .Where(material => material.Card != null)
            .Where(linkCard.CanUseLinkMaterial)
            .ToList();

        return new SummonMaterialSelectionSpec(
            candidates,
            linkCard.GetMinLinkMaterialCount(coreCard),
            linkCard.GetMaxLinkMaterialCount(coreCard),
            materials => linkCard.HasValidLinkMaterials(coreCard, materials)
                && CanSummonWithMaterials(owner, materials)
        );
    }

    private static bool CanSummonWithMaterials(
        Player owner,
        IReadOnlyList<SummonMaterial> materials
    ) {
        HashSet<Creature> availableFieldMonsters = owner.Creature.Pets
            .Where(SummonMaterial.IsFieldMonster)
            .ToHashSet();
        HashSet<CardModel> availableHandMonsters = PileType.Hand.GetPile(owner).Cards
            .Where(SummonMaterial.IsHandMonsterCard)
            .ToHashSet();
        int consumedFieldMonsterCount = 0;

        foreach (SummonMaterial material in materials) {
            if (material.Creature is { } creature) {
                if (!availableFieldMonsters.Remove(creature)) return false;
                consumedFieldMonsterCount++;
                continue;
            }

            if (material.Card is not { } card || !availableHandMonsters.Remove(card)) {
                return false;
            }
        }

        return owner.MinionCount() - consumedFieldMonsterCount < MinionUtil.MAX_MINION_COUNT;
    }

    private static async Task ConsumeSummonMaterials(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<SummonMaterial> materials
    ) {
        List<Task> consumeTasks = [];
        bool hasFieldMaterial = false;
        foreach (SummonMaterial material in materials) {
            if (material.Creature != null) {
                hasFieldMaterial = true;
                consumeTasks.Add(TaskHelper.RunSafely(MaterialSacrifice(material.Creature)));
            }
            else if (material.Card != null) {
                consumeTasks.Add(TaskHelper.RunSafely(CardCmd.Discard(choiceContext, material.Card)));
            }
        }

        if (consumeTasks.Count <= 0) return;
        if (hasFieldMaterial) {
            SFXUtil.Play("event:/vygo/sfx/material_shine");
        }

        await Task.WhenAll(consumeTasks);
    }

    private static async Task MaterialSacrifice(Creature material) {
        var nCreature = material.GetCreatureNode();
        if (nCreature?.Visuals is not NMonsterVisuals visuals) return;

        nCreature.ToggleIsInteractable(false);
        nCreature.AnimHideIntent();

        async Task PlayDeathAnimation() {
            try {
                await visuals.PlayMaterialVfx();
                await visuals.PlayMaterialExitAnimation();
            }
            finally {
                if (GodotObject.IsInstanceValid(nCreature)) {
                    nCreature.QueueFreeSafely();
                }
            }
        }

        Task deathAnimationTask = TaskHelper.RunSafely(PlayDeathAnimation());
        nCreature.DeathAnimationTask = deathAnimationTask;
        await CreatureCmd.Kill(material, true);
        await deathAnimationTask;
    }
}
