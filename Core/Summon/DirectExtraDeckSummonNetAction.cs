using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Models.Identity;
using STS2RitsuLib.Networking.ManagedActions;
using VYgo.Scripts;

namespace VYgo.Core;

public static class DirectExtraDeckSummonNetAction {
    private const string ActionModuleId = "vygo";
    private const string ActionKey = "direct_extra_deck_summon_v1";

    private static readonly RitsuLibManagedNetActionDescriptor<Payload> Descriptor = new(
        ActionModuleId,
        ActionKey,
        Serialize,
        Deserialize,
        Execute,
        GameActionType.CombatPlayPhaseOnly
    );

    public static void Register() {
        RitsuLibManagedNetActions.Register(Descriptor);
    }

    public static bool CanRequest(CardModel card) {
        if (!CombatManager.Instance.IsInProgress
            || CombatManager.Instance.IsEnding
            || CombatManager.Instance.PlayerActionsDisabled
            || card.Owner == null
            || !LocalContext.IsMe(card.Owner)
            || !CombatManager.Instance.IsPartOfPlayerTurn(card.Owner)
            || !card.Owner.Creature.IsAlive
            || !TryGetAvailableSpec(card, card.Owner, out _)) {
            return false;
        }

        return RitsuLibFramework.TryGetModelIdentity(card, out _);
    }

    public static bool Request(CardModel card) {
        if (!CanRequest(card)
            || card.Owner == null
            || !RitsuLibFramework.TryGetModelIdentity(card, out ModModelIdentityToken token)) {
            return false;
        }

        return RitsuLibManagedNetActions.Request(
            RunManager.Instance,
            Descriptor,
            new Payload(card.Owner.NetId, token),
            card.Owner.NetId
        );
    }

    internal static bool TryGetAvailableSpec(
        CardModel card,
        Player owner,
        out DirectExtraDeckSummonSpec spec
    ) {
        spec = null!;
        CardPile extraPile = Entry.ExtraPile.GetPile(owner);
        if (card.Owner != owner
            || card.Pile != extraPile
            || !extraPile.Cards.Contains(card)
            || card is not IDirectExtraDeckSummonCard directSummonCard) {
            return false;
        }

        DirectExtraDeckSummonSpec? candidate = directSummonCard.CreateDirectExtraDeckSummonSpec(owner);
        if (candidate?.BuildMaterialSelection()?.HasValidCombination != true) {
            return false;
        }

        spec = candidate;
        return true;
    }

    private static byte[] Serialize(Payload payload) {
        var writer = new PacketWriter { WarnOnGrow = false };
        writer.WriteULong(payload.OwnerNetId);
        writer.WriteFullModelId(payload.TargetToken.ModelId);
        writer.WriteUInt(payload.TargetToken.Identity.Value);
        writer.ZeroByteRemainder();
        return writer.Buffer.AsSpan(0, writer.BytePosition).ToArray();
    }

    private static Payload Deserialize(ReadOnlySpan<byte> bytes) {
        var reader = new PacketReader();
        reader.Reset(bytes.ToArray());
        ulong ownerNetId = reader.ReadULong();
        var modelId = reader.ReadFullModelId();
        var identity = new ModModelIdentity(reader.ReadUInt());
        return new Payload(ownerNetId, new ModModelIdentityToken(identity, modelId));
    }

    private static async Task Execute(RitsuLibManagedNetActionContext<Payload> context) {
        if (context.Message.OwnerNetId != context.Player.NetId
            || !CombatManager.Instance.IsInProgress
            || CombatManager.Instance.IsEnding
            || !context.Player.Creature.IsAlive
            || !RitsuLibFramework.TryResolveModelIdentity(context.Message.TargetToken, out var model)
            || model is not CardModel card
            || !TryGetAvailableSpec(card, context.Player, out DirectExtraDeckSummonSpec spec)) {
            return;
        }

        await SummonUtil.ExecuteSelectedExtraDeckSummon(new SelectedExtraDeckSummonRequest(
            SelectedExtraCard: card,
            Owner: context.Player,
            ChoiceContext: context.PlayerChoiceContext,
            BuildMaterialSelection: spec.BuildMaterialSelection,
            PlayAnimation: spec.PlayAnimation,
            ConsumeMaterials: spec.ConsumeMaterials,
            AfterAutoPlay: spec.AfterAutoPlay,
            OnSummonFailedAfterConsumption: spec.OnSummonFailedAfterConsumption,
            FinalWaitSeconds: spec.FinalWaitSeconds
        ));
    }

    private readonly record struct Payload(
        ulong OwnerNetId,
        ModModelIdentityToken TargetToken
    );
}
