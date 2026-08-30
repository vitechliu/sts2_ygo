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
using VYgo.Core.Settings;
using VYgo.Scripts;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Monsters;
using VYgo.Utils;

namespace VYgo.Core;

public sealed record FusionSummonRequest(
    CardModel? SourceCard,
    Player Owner,
    PlayerChoiceContext ChoiceContext,
    LocString SelectionPrompt,
    Func<BaseExtraFusionCard, IReadOnlyList<SummonMaterial>> GetAvailableMaterials,
    Func<SummonMaterial, PileType> GetMaterialDestination,
    Func<BaseExtraFusionCard, bool>? FusionCardFilter = null
);

public sealed record LinkSummonRequest(
    CardModel SourceCard,
    Player Owner,
    PlayerChoiceContext ChoiceContext,
    LocString SelectionPrompt,
    Func<BaseExtraLinkCard, CoreCard, IReadOnlyList<SummonMaterial>> GetAvailableMaterials
);

public sealed record XyzSummonRequest(
    CardModel? SourceCard,
    Player Owner,
    PlayerChoiceContext ChoiceContext,
    LocString SelectionPrompt,
    Func<BaseExtraXyzCard, CoreCard, IReadOnlyList<SummonMaterial>> GetAvailableMaterials,
    Func<BaseExtraXyzCard, bool>? XyzCardFilter = null
);

public sealed record SynchroSummonRequest(
    CardModel? SourceCard,
    Player Owner,
    PlayerChoiceContext ChoiceContext,
    LocString SelectionPrompt,
    Func<BaseExtraSynchroCard, CoreCard, IReadOnlyList<SummonMaterial>> GetAvailableMaterials,
    Func<BaseExtraSynchroCard, bool>? SynchroCardFilter = null
);

public enum ExtraDeckSummonType {
    Fusion,
    Link,
    Xyz,
    Synchro
}

public sealed record ExtraDeckSummonRequest(
    CardModel? SourceCard,
    Player Owner,
    PlayerChoiceContext ChoiceContext,
    LocString SelectionPrompt,
    Func<CardModel, bool> ExtraCardFilter,
    Func<CardModel, SummonMaterialSelectionSpec?> BuildMaterialSelection,
    ExtraDeckSummonType SummonType,
    Func<SummonAnimationContext, Task> PlayAnimation,
    Func<IReadOnlyList<SummonMaterial>, Task<bool>>? ConsumeMaterials = null,
    Func<SummonPostPlayContext, Task<bool>>? AfterAutoPlay = null,
    Func<IReadOnlyList<SummonMaterial>, Task>? OnSummonFailedAfterConsumption = null,
    float FinalWaitSeconds = 0.45f
);

public sealed record SelectedExtraDeckSummonRequest(
    CardModel SelectedExtraCard,
    Player Owner,
    PlayerChoiceContext ChoiceContext,
    Func<SummonMaterialSelectionSpec?> BuildMaterialSelection,
    ExtraDeckSummonType SummonType,
    Func<SummonAnimationContext, Task> PlayAnimation,
    CardModel? SourceCard = null,
    Func<IReadOnlyList<SummonMaterial>, Task<bool>>? ConsumeMaterials = null,
    Func<SummonPostPlayContext, Task<bool>>? AfterAutoPlay = null,
    Func<IReadOnlyList<SummonMaterial>, Task>? OnSummonFailedAfterConsumption = null,
    float FinalWaitSeconds = 0.45f
);

public sealed record SummonAnimationContext(
    CardModel FinalCard,
    IReadOnlyList<SummonMaterial> Materials,
    IReadOnlyList<CardModel> MaterialCards,
    Vector2 ScreenCenterPos,
    ExtraDeckSummonType SummonType
);

public sealed record SummonPostPlayContext(
    CardModel FinalCard,
    Creature SummonedCreature,
    Player Owner,
    PlayerChoiceContext ChoiceContext,
    IReadOnlyList<SummonMaterial> Materials
);

public sealed record ExtraDeckSummonResult(
    bool Success,
    CardModel? SummonedCard,
    Creature? SummonedCreature,
    IReadOnlyList<SummonMaterial> Materials
) {
    public static ExtraDeckSummonResult Failed(
        CardModel? summonedCard = null,
        Creature? summonedCreature = null,
        IReadOnlyList<SummonMaterial>? materials = null
    ) {
        return new ExtraDeckSummonResult(
            false,
            summonedCard,
            summonedCreature,
            materials ?? Array.Empty<SummonMaterial>()
        );
    }
}

public static class SummonUtil {
    private const string NoValidFusionSummonMessage =
        "V_YGO_SUMMON_MESSAGE_NO_VALID_FUSION";
    private const string NoValidSynchroSummonMessage =
        "V_YGO_SUMMON_MESSAGE_NO_VALID_SYNCHRO";

    public static Task<ExtraDeckSummonResult> ExecuteFusionSummon(FusionSummonRequest request) {
        if (!HasFusionSummonTarget(
                request.Owner,
                request.GetAvailableMaterials,
                request.GetMaterialDestination,
                request.FusionCardFilter
            )) {
            ThinkCmd.Play(
                new LocString("combat_messages", NoValidFusionSummonMessage),
                request.Owner.Creature,
                3
            );
            return Task.FromResult(ExtraDeckSummonResult.Failed());
        }

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
                request.GetAvailableMaterials,
                request.GetMaterialDestination
            ),
            SummonType: ExtraDeckSummonType.Fusion,
            PlayAnimation: ExtraDeckSummonAnimations.PlayFusionSummonAnimation,
            ConsumeMaterials: materials => ConsumeSummonMaterials(
                request.ChoiceContext,
                request.Owner,
                materials,
                request.GetMaterialDestination,
                ExtraDeckSummonType.Fusion
            ),
            AfterAutoPlay: context =>
                ((BaseExtraFusionCard)context.FinalCard).InvokeAfterFusionSummoned(context),
            FinalWaitSeconds: 0.45f
        ));
    }

    public static Task<ExtraDeckSummonResult> ExecuteLinkSummon(LinkSummonRequest request) {
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
            SummonType: ExtraDeckSummonType.Link,
            PlayAnimation: ExtraDeckSummonAnimations.PlayLinkSummonAnimation,
            AfterAutoPlay: TriggerLinkMaterialEffects,
            FinalWaitSeconds: 0.8f
        ));
    }

    internal static async Task<bool> TriggerLinkMaterialEffects(SummonPostPlayContext context) {
        foreach (SummonMaterial material in context.Materials) {
            if (material.Creature?.Monster is BaseMonster monster) {
                await monster.OnUsedAsLinkMaterial(
                    context.ChoiceContext,
                    context.Owner,
                    context.Materials
                );
            }
        }
        return true;
    }

    public static Task<ExtraDeckSummonResult> ExecuteXyzSummon(XyzSummonRequest request) {
        return ExecuteExtraDeckSummon(new ExtraDeckSummonRequest(
            SourceCard: request.SourceCard,
            Owner: request.Owner,
            ChoiceContext: request.ChoiceContext,
            SelectionPrompt: request.SelectionPrompt,
            ExtraCardFilter: card => card is BaseExtraXyzCard xyzCard
                && (request.XyzCardFilter?.Invoke(xyzCard) ?? true),
            BuildMaterialSelection: card => BuildXyzMaterialSelection(
                card,
                request.Owner,
                request.GetAvailableMaterials
            ),
            SummonType: ExtraDeckSummonType.Xyz,
            PlayAnimation: ExtraDeckSummonAnimations.PlayXyzSummonAnimation,
            ConsumeMaterials: materials =>
                XyzMaterialCmd.ReserveForSummon(
                    request.Owner,
                    materials,
                    ExtraDeckSummonType.Xyz
                ),
            AfterAutoPlay: XyzMaterialCmd.AttachReservedToSummonedMonster,
            OnSummonFailedAfterConsumption: materials =>
                XyzMaterialCmd.SendReservedToGraveyard(request.Owner, materials),
            FinalWaitSeconds: 0.45f
        ));
    }

    public static Task<ExtraDeckSummonResult> ExecuteSynchroSummon(
        SynchroSummonRequest request
    ) {
        if (!HasSynchroSummonTarget(
                request.Owner,
                request.GetAvailableMaterials,
                request.SynchroCardFilter
            )) {
            Entry.Logger.Info("Synchro summon rejected: no legal target/material combination.");
            ThinkCmd.Play(
                new LocString("combat_messages", NoValidSynchroSummonMessage),
                request.Owner.Creature,
                3
            );
            return Task.FromResult(ExtraDeckSummonResult.Failed());
        }

        return ExecuteExtraDeckSummon(new ExtraDeckSummonRequest(
            SourceCard: request.SourceCard,
            Owner: request.Owner,
            ChoiceContext: request.ChoiceContext,
            SelectionPrompt: request.SelectionPrompt,
            ExtraCardFilter: card => card is BaseExtraSynchroCard synchroCard
                && (request.SynchroCardFilter?.Invoke(synchroCard) ?? true),
            BuildMaterialSelection: card => BuildSynchroMaterialSelection(
                card,
                request.Owner,
                request.GetAvailableMaterials
            ),
            SummonType: ExtraDeckSummonType.Synchro,
            PlayAnimation: ExtraDeckSummonAnimations.PlaySynchroSummonAnimation,
            FinalWaitSeconds: 0.45f
        ));
    }

    internal static DirectExtraDeckSummonSpec CreateDirectXyzSummonSpec(
        BaseExtraXyzCard card,
        Player owner,
        Func<BaseExtraXyzCard, CoreCard, IReadOnlyList<SummonMaterial>> getAvailableMaterials
    ) {
        return new DirectExtraDeckSummonSpec(
            BuildMaterialSelection: () => BuildXyzMaterialSelection(
                card,
                owner,
                getAvailableMaterials
            ),
            SummonType: ExtraDeckSummonType.Xyz,
            PlayAnimation: ExtraDeckSummonAnimations.PlayXyzSummonAnimation,
            ConsumeMaterials: materials =>
                XyzMaterialCmd.ReserveForSummon(
                    owner,
                    materials,
                    ExtraDeckSummonType.Xyz
                ),
            AfterAutoPlay: XyzMaterialCmd.AttachReservedToSummonedMonster,
            OnSummonFailedAfterConsumption: materials =>
                XyzMaterialCmd.SendReservedToGraveyard(owner, materials),
            FinalWaitSeconds: 0.45f
        );
    }

    internal static DirectExtraDeckSummonSpec CreateDirectSynchroSummonSpec(
        BaseExtraSynchroCard card,
        Player owner,
        Func<BaseExtraSynchroCard, CoreCard, IReadOnlyList<SummonMaterial>> getAvailableMaterials
    ) {
        return new DirectExtraDeckSummonSpec(
            BuildMaterialSelection: () => BuildSynchroMaterialSelection(
                card,
                owner,
                getAvailableMaterials
            ),
            SummonType: ExtraDeckSummonType.Synchro,
            PlayAnimation: ExtraDeckSummonAnimations.PlaySynchroSummonAnimation,
            FinalWaitSeconds: 0.45f
        );
    }

    internal static DirectExtraDeckSummonSpec CreateDirectFusionSummonSpec(
        BaseExtraFusionCard card,
        Player owner,
        Func<BaseExtraFusionCard, IReadOnlyList<SummonMaterial>> getAvailableMaterials
    ) {
        return new DirectExtraDeckSummonSpec(
            BuildMaterialSelection: () => BuildFusionMaterialSelection(
                card,
                owner,
                getAvailableMaterials,
                _ => PileType.Discard
            ),
            SummonType: ExtraDeckSummonType.Fusion,
            PlayAnimation: ExtraDeckSummonAnimations.PlayFusionSummonAnimation,
            AfterAutoPlay: card.InvokeAfterFusionSummoned,
            FinalWaitSeconds: 0.45f
        );
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

    public static bool HasValidFieldTribute(
        Player owner,
        int tributeCount,
        Func<SummonMaterial, bool>? filter = null
    ) {
        return BuildFieldTributeSelection(owner, tributeCount, filter)
            .HasValidCombination;
    }

    public static async Task<bool> ExecuteFieldTribute(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel targetCard,
        int tributeCount,
        Func<SummonMaterial, bool>? filter = null
    ) {
        Func<SummonMaterialSelectionSpec?> buildSelection =
            () => BuildFieldTributeSelection(owner, tributeCount, filter);

        IReadOnlyList<SummonMaterial> selectedMaterials =
            await SummonMaterialSelectCmd.Select(
                choiceContext,
                owner,
                targetCard,
                buildSelection
            );
        if (selectedMaterials.Count <= 0) return false;

        SummonMaterialSelectionSpec latestSelection =
            BuildFieldTributeSelection(owner, tributeCount, filter);
        IReadOnlyList<SummonMaterial> resolvedMaterials =
            latestSelection.ResolveMaterials(
                selectedMaterials
                    .Select(material => material.Card)
                    .OfType<CardModel>()
            );
        if (!latestSelection.IsValidSelection(resolvedMaterials)) return false;

        return await ConsumeSummonMaterials(
            choiceContext,
            owner,
            resolvedMaterials.ToList(),
            _ => PileType.Discard
        );
    }

    public static IReadOnlyList<SummonMaterial> GetMonsterMaterialsFromPiles(
        Player owner,
        IEnumerable<PileType> sourcePiles,
        Func<SummonMaterial, bool>? filter = null
    ) {
        List<SummonMaterial> materials = [];
        foreach (PileType sourcePile in sourcePiles.Distinct()) {
            if (sourcePile is not (
                    PileType.Draw
                    or PileType.Hand
                    or PileType.Discard
                    or PileType.Exhaust
                )) {
                throw new ArgumentOutOfRangeException(
                    nameof(sourcePiles),
                    sourcePile,
                    "Monster material sources must be draw, hand, discard, or exhaust."
                );
            }

            materials.AddRange(sourcePile.GetPile(owner).Cards
                .Where(card => SummonMaterial.IsMonsterCardInPile(card, sourcePile))
                .Select(SummonMaterial.FromMonsterCard)
                .Where(material => filter?.Invoke(material) ?? true));
        }

        return materials;
    }

    public static IReadOnlyList<SummonMaterial> GetFieldAndMonsterMaterialsFromPiles(
        Player owner,
        IEnumerable<PileType> sourcePiles,
        Func<SummonMaterial, bool>? filter = null
    ) {
        List<SummonMaterial> materials = GetFieldMonsterMaterials(owner, filter).ToList();
        materials.AddRange(GetMonsterMaterialsFromPiles(owner, sourcePiles, filter));
        return materials;
    }

    public static IReadOnlyList<SummonMaterial> GetEquippedMonsterMaterials(
        Player owner,
        Func<SummonMaterial, bool>? filter = null
    ) {
        return EquipCmd.GetAllEquipment(owner)
            .Where(card => card is BaseMonsterCard && card.Pile?.Type == Entry.EquipPile)
            .Select(SummonMaterial.FromMonsterCard)
            .Where(material => filter?.Invoke(material) ?? true)
            .ToList();
    }

    public static IReadOnlyList<SummonMaterial> GetFieldAndHandMonsterMaterials(
        Player owner,
        Func<SummonMaterial, bool>? filter = null
    ) {
        return GetFieldAndMonsterMaterialsFromPiles(owner, [PileType.Hand], filter);
    }

    public static bool HasFusionSummonTarget(
        Player owner,
        Func<BaseExtraFusionCard, IReadOnlyList<SummonMaterial>> getAvailableMaterials,
        Func<SummonMaterial, PileType> getMaterialDestination,
        Func<BaseExtraFusionCard, bool>? fusionCardFilter = null
    ) {
        return Entry.ExtraPile.GetPile(owner).Cards
            .OfType<BaseExtraFusionCard>()
            .Where(card => fusionCardFilter?.Invoke(card) ?? true)
            .Any(card => BuildFusionMaterialSelection(
                card,
                owner,
                getAvailableMaterials,
                getMaterialDestination
            )?.HasValidCombination == true);
    }

    public static bool HasSynchroSummonTarget(
        Player owner,
        Func<BaseExtraSynchroCard, CoreCard, IReadOnlyList<SummonMaterial>> getAvailableMaterials,
        Func<BaseExtraSynchroCard, bool>? synchroCardFilter = null
    ) {
        return Entry.ExtraPile.GetPile(owner).Cards
            .OfType<BaseExtraSynchroCard>()
            .Where(card => synchroCardFilter?.Invoke(card) ?? true)
            .Any(card => BuildSynchroMaterialSelection(
                card,
                owner,
                getAvailableMaterials
            )?.HasValidCombination == true);
    }

    public static async Task<ExtraDeckSummonResult> ExecuteExtraDeckSummon(
        ExtraDeckSummonRequest request
    ) {
        CardPile extraPile = Entry.ExtraPile.GetPile(request.Owner);
        if (extraPile.Cards.Count <= 0) return ExtraDeckSummonResult.Failed();

        List<CardModel> summonableCards = extraPile.Cards
            .Where(request.ExtraCardFilter)
            .Where(card => request.BuildMaterialSelection(card)?.HasValidCombination == true)
            .ToList();
        if (summonableCards.Count <= 0) return ExtraDeckSummonResult.Failed();

        CardModel? selectedExtraCard = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(request.SelectionPrompt, 1),
                context: request.ChoiceContext,
                pile: extraPile,
                player: request.Owner,
                filter: summonableCards.Contains))
            .FirstOrDefault();
        if (selectedExtraCard == null || !extraPile.Cards.Contains(selectedExtraCard)) {
            return ExtraDeckSummonResult.Failed(selectedExtraCard);
        }

        return await ExecuteSelectedExtraDeckSummon(new SelectedExtraDeckSummonRequest(
            SelectedExtraCard: selectedExtraCard,
            Owner: request.Owner,
            ChoiceContext: request.ChoiceContext,
            BuildMaterialSelection: () => request.BuildMaterialSelection(selectedExtraCard),
            SummonType: request.SummonType,
            PlayAnimation: request.PlayAnimation,
            SourceCard: request.SourceCard,
            ConsumeMaterials: request.ConsumeMaterials,
            AfterAutoPlay: request.AfterAutoPlay,
            OnSummonFailedAfterConsumption: request.OnSummonFailedAfterConsumption,
            FinalWaitSeconds: request.FinalWaitSeconds
        ));
    }

    public static async Task<ExtraDeckSummonResult> ExecuteSelectedExtraDeckSummon(
        SelectedExtraDeckSummonRequest request
    ) {
        CardPile extraPile = Entry.ExtraPile.GetPile(request.Owner);
        CardModel selectedExtraCard = request.SelectedExtraCard;
        if (selectedExtraCard.Owner != request.Owner
            || selectedExtraCard.Pile != extraPile
            || !extraPile.Cards.Contains(selectedExtraCard)
            || selectedExtraCard is not BaseMonsterCard summonCard) {
            return ExtraDeckSummonResult.Failed(selectedExtraCard);
        }

        IReadOnlyList<SummonMaterial> selectedMaterials = await SummonMaterialSelectCmd.Select(
            request.ChoiceContext,
            request.Owner,
            selectedExtraCard,
            request.BuildMaterialSelection
        );
        if (selectedMaterials.Count <= 0) {
            return ExtraDeckSummonResult.Failed(selectedExtraCard);
        }

        SummonMaterialSelectionSpec? latestSpec = request.BuildMaterialSelection();
        if (latestSpec == null || !extraPile.Cards.Contains(selectedExtraCard)) {
            return ExtraDeckSummonResult.Failed(selectedExtraCard);
        }

        IReadOnlyList<SummonMaterial> materials = latestSpec.ResolveMaterials(
            selectedMaterials.Select(material => material.Card).OfType<CardModel>()
        );
        if (!latestSpec.IsValidSelection(materials)) {
            return ExtraDeckSummonResult.Failed(selectedExtraCard, materials: materials);
        }

        NCombatRoom? combatRoom = TestMode.IsOn ? null : NCombatRoom.Instance;
        NCard? sourceNode = combatRoom != null && request.SourceCard != null
            ? NCard.FindOnTable(request.SourceCard)
            : null;
        bool sourceNodeWasHidden = sourceNode != null
            && GodotObject.IsInstanceValid(sourceNode)
            && sourceNode.IsInsideTree();
        if (sourceNodeWasHidden) {
            sourceNode!.PlayPileTween?.FastForwardToCompletion();
            sourceNode.Visible = false;
        }

        bool materialsConsumed = false;
        bool summonCompleted = false;
        Creature? summonedCreature = null;
        try {
            IReadOnlyList<CardModel> materialCards = materials
                .Select(material => material.Card)
                .OfType<CardModel>()
                .ToList();

            if (request.ConsumeMaterials != null) {
                materialsConsumed = await request.ConsumeMaterials(materials);
            }
            else {
                materialsConsumed = await ConsumeSummonMaterials(
                    request.ChoiceContext,
                    request.Owner,
                    materials,
                    _ => PileType.Discard,
                    request.SummonType
                );
            }
            if (!materialsConsumed) {
                return ExtraDeckSummonResult.Failed(selectedExtraCard, materials: materials);
            }

            EffectMode effectMode = VYgoModSettings.GetEffectMode(request.Owner);
            bool playedAnimation = false;
            SummonAnimationContext? animationContext = null;
            if (combatRoom != null && effectMode != EffectMode.none) {
                Vector2 screenCenterPos = combatRoom.GetViewportRect().Size * 0.5f;
                animationContext = new SummonAnimationContext(
                    FinalCard: selectedExtraCard,
                    Materials: materials,
                    MaterialCards: materialCards,
                    ScreenCenterPos: screenCenterPos,
                    SummonType: request.SummonType
                );
            }

            if (animationContext != null && effectMode == EffectMode.full) {
                try {
                    await request.PlayAnimation(animationContext);
                    playedAnimation = true;
                }
                catch (Exception ex) {
                    Entry.Logger.Warn(
                        $"Extra deck summon animation failed for {selectedExtraCard.GetType().Name}: {ex}"
                    );
                }
            }

            if (!selectedExtraCard.Owner.Creature.IsDead) {
                summonedCreature = await summonCard.AutoPlayAndCaptureSummonedCreature(
                    request.ChoiceContext,
                    null,
                    AutoPlayType.Default,
                    false,
                    true,
                    playSummonCardFly: false,
                    playMonsterSummonVfx: effectMode == EffectMode.full
                );

                if (summonedCreature != null
                    && animationContext != null
                    && effectMode == EffectMode.minimal) {
                    try {
                        await ExtraDeckSummonAnimations.PlayMinimalSummonAnimation(
                            animationContext,
                            summonedCreature
                        );
                        playedAnimation = true;
                    }
                    catch (Exception ex) {
                        Entry.Logger.Warn(
                            $"快速额外卡组召唤演出失败，已降级继续召唤：" +
                            $"{selectedExtraCard.GetType().Name}，{ex}"
                        );
                    }
                }

                if (request.AfterAutoPlay != null
                    && summonedCreature != null
                    && !await request.AfterAutoPlay(new SummonPostPlayContext(
                        selectedExtraCard,
                        summonedCreature,
                        request.Owner,
                        request.ChoiceContext,
                        materials
                    ))) {
                    return ExtraDeckSummonResult.Failed(
                        selectedExtraCard,
                        summonedCreature,
                        materials
                    );
                }

                if (summonedCreature != null) {
                    summonCompleted = true;
                    extraPile.InvokeCardRemoveFinished();
                }
            }

            if (playedAnimation) {
                await VFXUtil.Wait(
                    effectMode == EffectMode.minimal
                        ? ExtraDeckSummonAnimations.MinimalFinalWaitSeconds
                        : request.FinalWaitSeconds
                );
            }
            return summonCompleted
                ? new ExtraDeckSummonResult(
                    true,
                    selectedExtraCard,
                    summonedCreature,
                    materials
                )
                : ExtraDeckSummonResult.Failed(
                    selectedExtraCard,
                    summonedCreature,
                    materials
                );
        }
        finally {
            if (materialsConsumed
                && !summonCompleted
                && request.OnSummonFailedAfterConsumption != null) {
                await request.OnSummonFailedAfterConsumption(materials);
            }

            if (sourceNodeWasHidden && GodotObject.IsInstanceValid(sourceNode)) {
                sourceNode.Visible = true;
            }
        }
    }

    private static SummonMaterialSelectionSpec? BuildFusionMaterialSelection(
        CardModel card,
        Player owner,
        Func<BaseExtraFusionCard, IReadOnlyList<SummonMaterial>> getAvailableMaterials,
        Func<SummonMaterial, PileType> getMaterialDestination
    ) {
        if (card is not BaseExtraFusionCard fusionCard) return null;

        IReadOnlyList<SummonMaterial> candidates = getAvailableMaterials(fusionCard)
            .Where(material => material.Card != null)
            .Where(material => CanMoveMaterialToDestination(
                material,
                getMaterialDestination(material)
            ))
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
        if (coreCard?.LinkCount is not > 0) {
            Entry.Logger.Error("Failed to get Link rating data: " + linkCard.CardId);
            return null;
        }

        IReadOnlyList<SummonMaterial> candidates = getAvailableMaterials(linkCard, coreCard)
            .Where(material => material.Card != null)
            .Where(linkCard.CanUseLinkMaterial)
            .ToList();

        int targetLinkValue = coreCard.LinkCount.Value;
        int? configuredMax = linkCard.GetMaxLinkMaterialCount(coreCard);
        int maxMaterialCount = Math.Min(configuredMax ?? targetLinkValue, targetLinkValue);

        return new SummonMaterialSelectionSpec(
            candidates,
            linkCard.GetMinLinkMaterialCount(coreCard),
            maxMaterialCount,
            materials => linkCard.HasValidLinkMaterials(coreCard, materials)
                && CanSummonWithMaterials(owner, materials)
        );
    }

    /// <summary>
    /// 校验一组素材能否精确组成目标怪兽的连接值。
    /// 普通怪兽只能计为 1；连接怪兽可以计为 1 或自身 LINK 值，
    /// 但不能计为两者之间的数值。
    /// </summary>
    public static bool HasExactLinkMaterialValue(
        CoreCard targetCard,
        IReadOnlyList<SummonMaterial> materials
    ) {
        if (targetCard.LinkCount is not > 0 || materials.Count == 0) return false;

        int targetLinkValue = targetCard.LinkCount.Value;
        HashSet<int> reachableValues = [0];

        foreach (SummonMaterial material in materials) {
            int materialLinkValue = Math.Max(1, material.CoreCard?.LinkCount ?? 1);
            HashSet<int> nextValues = [];

            foreach (int currentValue in reachableValues) {
                int valueAsOneMaterial = currentValue + 1;
                if (valueAsOneMaterial <= targetLinkValue) {
                    nextValues.Add(valueAsOneMaterial);
                }

                if (materialLinkValue > 1) {
                    int valueAsLinkRating = currentValue + materialLinkValue;
                    if (valueAsLinkRating <= targetLinkValue) {
                        nextValues.Add(valueAsLinkRating);
                    }
                }
            }

            if (nextValues.Count == 0) return false;
            reachableValues = nextValues;
        }

        return reachableValues.Contains(targetLinkValue);
    }

    internal static SummonMaterialSelectionSpec? BuildXyzMaterialSelection(
        CardModel card,
        Player owner,
        Func<BaseExtraXyzCard, CoreCard, IReadOnlyList<SummonMaterial>> getAvailableMaterials
    ) {
        if (card is not BaseExtraXyzCard xyzCard) return null;

        CoreCard? coreCard = xyzCard.YgoGetCore();
        if (coreCard?.Rank is not > 0) {
            Entry.Logger.Error("Failed to get Xyz rank data: " + xyzCard.CardId);
            return null;
        }

        IReadOnlyList<SummonMaterial> candidates = getAvailableMaterials(xyzCard, coreCard)
            // 衍生物离场即消失，不能成为需要长期挂载的超量素材。
            // 此处独立过滤，避免具体超量卡覆写 CanUseXyzMaterial 时绕过规则。
            .Where(material => material.Card is not BaseTokenCard)
            .Where(material => xyzCard.CanUseXyzMaterial(coreCard, material))
            .ToList();

        return new SummonMaterialSelectionSpec(
            candidates,
            xyzCard.MinXyzMaterialCount,
            xyzCard.MaxXyzMaterialCount,
            materials => xyzCard.HasValidXyzMaterials(coreCard, materials)
                && CanSummonWithMaterials(owner, materials)
        );
    }

    internal static SummonMaterialSelectionSpec? BuildSynchroMaterialSelection(
        CardModel card,
        Player owner,
        Func<BaseExtraSynchroCard, CoreCard, IReadOnlyList<SummonMaterial>> getAvailableMaterials
    ) {
        if (card is not BaseExtraSynchroCard synchroCard) return null;

        CoreCard? coreCard = synchroCard.YgoGetCore();
        int? targetLevel = coreCard == null
            ? null
            : synchroCard.GetSynchroTargetLevel(coreCard);
        if (coreCard == null || targetLevel is not > 0) {
            Entry.Logger.Error($"Failed to get Synchro target level data: {synchroCard.CardId}");
            return null;
        }

        IReadOnlyList<SummonMaterial> candidates = getAvailableMaterials(synchroCard, coreCard)
            .Where(material => material.Card != null)
            .Where(material => synchroCard.GetSynchroMaterialLevel(coreCard, material) is > 0)
            .Where(material => synchroCard.CanUseSynchroMaterial(coreCard, material))
            .ToList();

        if (candidates.Count == 0) {
            Entry.Logger.Info($"Synchro summon {synchroCard.CardId} has no usable field materials.");
        }

        return new SummonMaterialSelectionSpec(
            candidates,
            synchroCard.GetMinSynchroMaterialCount(coreCard),
            synchroCard.GetMaxSynchroMaterialCount(coreCard),
            materials => synchroCard.HasValidSynchroMaterials(coreCard, materials)
                && CanSummonWithMaterials(owner, materials)
        );
    }

    private static bool CanSummonWithMaterials(
        Player owner,
        IReadOnlyList<SummonMaterial> materials
    ) {
        HashSet<CardModel> seenCards = [];
        HashSet<Creature> seenFieldMonsters = [];
        int consumedFieldMonsterCount = 0;

        foreach (SummonMaterial material in materials) {
            if (material.Card is not { } card
                || card.Owner != owner
                || !seenCards.Add(card)
                || card.Pile?.Type != material.SourcePile
                || !card.Pile.Cards.Contains(card)) {
                return false;
            }

            if (material.Creature is { } creature) {
                if (!seenFieldMonsters.Add(creature)
                    || creature is not { IsAlive: true, Monster: BaseMonster monster }
                    || creature.PetOwner != owner
                    || !owner.Creature.Pets.Contains(creature)
                    || monster.SourceCard != card
                    || material.SourcePile != Entry.MonsterPile) {
                    return false;
                }

                consumedFieldMonsterCount++;
                continue;
            }

            if (card is not BaseMonsterCard monsterCard) {
                return false;
            }

            bool validPile = material.SourcePile switch {
                PileType.Draw or PileType.Hand => !monsterCard.IsExtra,
                PileType.Discard or PileType.Exhaust => true,
                _ when material.SourcePile == Entry.EquipPile =>
                    EquipCmd.IsOnField(owner, card),
                _ => false
            };
            if (!validPile) {
                return false;
            }
        }

        return owner.MinionCount() - consumedFieldMonsterCount < owner.GetMaxMinionCount();
    }

    private static bool CanMoveMaterialToDestination(
        SummonMaterial material,
        PileType destination
    ) {
        bool validDestination = destination is PileType.Draw or PileType.Discard or PileType.Exhaust;
        return validDestination
            && (material.IsField || destination != material.SourcePile);
    }

    private static SummonMaterialSelectionSpec BuildFieldTributeSelection(
        Player owner,
        int tributeCount,
        Func<SummonMaterial, bool>? filter
    ) {
        if (tributeCount <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(tributeCount),
                tributeCount,
                "Tribute count must be greater than zero."
            );
        }

        IReadOnlyList<SummonMaterial> candidates = GetFieldMonsterMaterials(owner, filter)
            .Where(material => material.Creature is { IsAlive: true })
            .ToList();

        return new SummonMaterialSelectionSpec(
            candidates,
            tributeCount,
            tributeCount,
            materials => materials.Count == tributeCount
                && materials.All(material =>
                    material.Creature is { IsAlive: true } creature
                    && owner.Creature.Pets.Contains(creature))
        );
    }

    public static async Task<bool> ConsumeSummonMaterials(
        PlayerChoiceContext choiceContext,
        Player owner,
        IReadOnlyList<SummonMaterial> materials,
        Func<SummonMaterial, PileType> getMaterialDestination,
        ExtraDeckSummonType? summonType = null
    ) {
        if (materials.Count <= 0 || !CanSummonWithMaterials(owner, materials)) {
            return false;
        }

        List<(SummonMaterial Material, PileType Destination)> moves = [];
        foreach (SummonMaterial material in materials) {
            PileType destination = getMaterialDestination(material);
            if (!CanMoveMaterialToDestination(material, destination)) {
                Entry.Logger.Error(
                    $"Invalid summon material destination {destination} for " +
                    $"{material.Card?.GetType().Name ?? "unknown material"}."
                );
                return false;
            }

            moves.Add((material, destination));
        }

        List<(BaseMonster Monster, CardModel Card)> fieldReservations = [];
        foreach ((SummonMaterial material, _) in moves.Where(move => move.Material.IsField)) {
            if (material is not {
                    Card: { } card,
                    Creature: { Monster: BaseMonster monster }
                }
                || !monster.TryReserveSourceCardAsSummonMaterial(card)) {
                foreach ((BaseMonster reservedMonster, CardModel reservedCard) in fieldReservations) {
                    reservedMonster.CancelSourceCardMaterialReservation(reservedCard);
                }
                return false;
            }

            fieldReservations.Add((monster, card));
        }

        try {
            EffectMode effectMode = VYgoModSettings.GetEffectMode(owner);
            if (fieldReservations.Count > 0 && effectMode != EffectMode.none) {
                //召唤素材发光的音效
                SFXUtil.Play("event:/vygo/sfx/material_shine");
            }

            List<(SummonMaterial Material, PileType Destination)> fieldMoves = moves
                .Where(move => move.Material.IsField)
                .ToList();

            await Task.WhenAll(fieldMoves.Select(move =>
                MaterialSacrifice(move.Material.Creature!, effectMode, summonType)));

            foreach ((SummonMaterial material, PileType destination) in fieldMoves) {
                BaseMonster monster = (BaseMonster)material.Creature!.Monster;
                if (!await monster.SendReservedSourceCardAsSummonMaterial(
                        choiceContext,
                        owner,
                        destination
                    )) {
                    return false;
                }
            }

            List<CardModel> equippedCards = moves
                .Where(move => !move.Material.IsField
                    && move.Material.SourcePile == Entry.EquipPile
                    && move.Destination == PileType.Discard)
                .Select(move => move.Material.Card!)
                .ToList();
            foreach (CardModel equippedCard in equippedCards) {
                if (!await EquipCmd.SendToGraveyard(choiceContext, equippedCard)) {
                    return false;
                }
            }

            List<CardModel> discardCards = moves
                .Where(move => !move.Material.IsField
                    && move.Material.SourcePile != Entry.EquipPile
                    && move.Destination == PileType.Discard)
                .Select(move => move.Material.Card!)
                .ToList();
            if (discardCards.Count > 0) {
                await CardCmd.Discard(choiceContext, discardCards);
            }

            foreach ((SummonMaterial material, PileType destination) in moves
                         .Where(move => !move.Material.IsField
                             && move.Destination != PileType.Discard)) {
                CardModel card = material.Card!;
                if (destination == PileType.Exhaust) {
                    await CardCmd.Exhaust(choiceContext, card);
                }
                else {
                    await CardPileCmd.Add(card, destination);
                }
            }

            // 素材移动成功后，送墓/除外触发可以立即把卡移动到其他区域。
            // 此类后续移动不应反过来令已经完成的素材消费失败。
            return true;
        }
        catch (Exception ex) {
            Entry.Logger.Error("Failed to consume summon materials: " + ex);
            return false;
        }
        finally {
            foreach ((BaseMonster monster, CardModel card) in fieldReservations) {
                // 并行牺牲或后续素材移动部分失败时，已经死亡的衍生物也不能滞留在场上牌堆。
                if (card is BaseTokenCard token
                    && monster.Creature is not { IsAlive: true }) {
                    await token.DisappearFromCombat();
                }
                monster.CancelSourceCardMaterialReservation(card);
            }
        }
    }

    internal static async Task MaterialSacrifice(
        Creature material,
        EffectMode effectMode,
        ExtraDeckSummonType? summonType = null
    ) {
        var nCreature = material.GetCreatureNode();
        if (nCreature == null) {
            await CreatureCmd.Kill(material, true);
            return;
        }

        nCreature.ToggleIsInteractable(false);
        nCreature.AnimHideIntent();

        if (effectMode == EffectMode.none
            || (effectMode == EffectMode.minimal
                && nCreature.Visuals is not NMonsterVisuals)) {
            // CreatureCmd.Kill 会在没有预设 DeathAnimationTask 时启动原版死亡动画。
            // 用一个待完成任务占位，保留完整死亡结算，同时阻止任何死亡演出。
            var animationBlocker = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            nCreature.DeathAnimationTask = animationBlocker.Task;
            try {
                await CreatureCmd.Kill(material, true);
            }
            finally {
                if (GodotObject.IsInstanceValid(nCreature)) {
                    nCreature.QueueFreeSafely();
                }
                animationBlocker.TrySetResult();
            }
            return;
        }

        if (nCreature.Visuals is not NMonsterVisuals visuals) {
            await CreatureCmd.Kill(material, true);
            return;
        }

        async Task PlayDeathAnimation() {
            try {
                if (effectMode == EffectMode.minimal) {
                    await visuals.PlayQuickMaterialAnimation(
                        ExtraDeckSummonAnimations.GetMinimalAccentColor(summonType)
                    );
                }
                else {
                    await visuals.PlayMaterialVfx();
                    await visuals.PlayMaterialExitAnimation();
                }
            }
            catch (Exception ex) {
                Entry.Logger.Warn(
                    $"召唤素材演出失败，已降级继续清理：{material.Monster?.GetType().Name}，{ex}"
                );
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
