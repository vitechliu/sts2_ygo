using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Networking.ManagedActions;
using VYgo.Scripts;

namespace VYgo.Core;

public static class DirectExtraDeckSummonNetAction {
    private const string ActionModuleId = "vygo";
    private const string ActionKey = "direct_extra_deck_summon_v2";

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

        return NetCombatCardDb.Instance.TryGetCardId(card, out _);
    }

    public static bool Request(CardModel card) {
        if (!CanRequest(card)
            || card.Owner == null) {
            return false;
        }

        return RitsuLibManagedNetActions.Request(
            RunManager.Instance,
            Descriptor,
            new Payload(card.Owner.NetId, card.Id, NetCombatCard.FromModel(card)),
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
        writer.WriteFullModelId(payload.TargetModelId);
        writer.Write(payload.TargetCard);
        writer.ZeroByteRemainder();
        return writer.Buffer.AsSpan(0, writer.BytePosition).ToArray();
    }

    private static Payload Deserialize(ReadOnlySpan<byte> bytes) {
        var reader = new PacketReader();
        reader.Reset(bytes.ToArray());
        ulong ownerNetId = reader.ReadULong();
        var modelId = reader.ReadFullModelId();
        var targetCard = reader.Read<NetCombatCard>();
        return new Payload(ownerNetId, modelId, targetCard);
    }

    private static async Task Execute(RitsuLibManagedNetActionContext<Payload> context) {
        if (context.Message.OwnerNetId != context.Player.NetId
            || !CombatManager.Instance.IsInProgress
            || CombatManager.Instance.IsEnding
            || !context.Player.Creature.IsAlive) {
            Entry.Logger.Warn(
                $"直接额外卡组召唤在执行前失效：玩家={context.Player.NetId}，" +
                $"载荷玩家={context.Message.OwnerNetId}，战斗中={CombatManager.Instance.IsInProgress}，" +
                $"战斗结束中={CombatManager.Instance.IsEnding}，玩家存活={context.Player.Creature.IsAlive}。");
            return;
        }

        CardModel? card = context.Message.TargetCard.ToCardModelOrNull();
        if (card == null || card.Id != context.Message.TargetModelId) {
            Entry.Logger.Warn(
                $"直接额外卡组召唤无法解析目标卡：玩家={context.Player.NetId}，" +
                $"战斗卡编号={context.Message.TargetCard.CombatCardIndex}，" +
                $"预期模型={context.Message.TargetModelId}，实际模型={card?.Id.ToString() ?? "<null>"}。");
            return;
        }

        if (!TryGetAvailableSpec(card, context.Player, out DirectExtraDeckSummonSpec spec)) {
            Entry.Logger.Warn(
                $"直接额外卡组召唤目标当前不可用：玩家={context.Player.NetId}，" +
                $"战斗卡编号={context.Message.TargetCard.CombatCardIndex}，目标={card.Id}，" +
                $"牌堆={card.Pile?.Type.ToString() ?? "<null>"}。");
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
        ModelId TargetModelId,
        NetCombatCard TargetCard
    );
}
