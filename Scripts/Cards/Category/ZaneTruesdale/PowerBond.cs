using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class PowerBond() : BaseSpellCard(energyCost, CardType.Skill, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 37630732;

    private const int energyCost = 1;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.FusionSummon()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay
    ) {
        ExtraDeckSummonResult result = await SummonUtil.ExecuteFusionSummon(new FusionSummonRequest(
            SourceCard: this,
            Owner: Owner,
            ChoiceContext: choiceContext,
            SelectionPrompt: SelectionScreenPrompt,
            GetAvailableMaterials: _ => SummonUtil.GetFieldAndHandMonsterMaterials(Owner),
            GetMaterialDestination: _ => PileType.Discard
        ));

        if (!result.Success
            || result.SummonedCard is not BaseMonsterCard
            || result.SummonedCreature is not { } summonedCreature) {
            return;
        }
        int attackIncrease = summonedCreature.GetPower<AttackPower>()?.Amount ?? 0;
        if (attackIncrease <= 0) return;

        await PowerCmd.Apply<AttackPower>(
            choiceContext,
            summonedCreature,
            attackIncrease,
            Owner.Creature,
            this
        );
        await PowerCmd.Apply<PowerBondDamagePower>(
            choiceContext,
            Owner.Creature,
            attackIncrease,
            summonedCreature,
            this
        );
    }

    protected override void OnUpgrade() {
        EnergyCost.UpgradeBy(-1);
    }
}
