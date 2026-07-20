using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace VYgo.Core;

public sealed class SummonMaterialSelectionSpec {
    private readonly Dictionary<CardModel, SummonMaterial> _materialsByCard;
    private readonly IReadOnlyList<HashSet<SummonMaterial>> _validCombinationSets;

    public IReadOnlyList<SummonMaterial> Candidates { get; }
    public IReadOnlyList<CardModel> CandidateCards { get; }
    public int MinSelect { get; }
    public int MaxSelect { get; }
    public IReadOnlyList<IReadOnlyList<SummonMaterial>> ValidCombinations { get; }
    public bool HasValidCombination => ValidCombinations.Count > 0;
    public IReadOnlyList<SummonMaterial> FirstValidCombination =>
        ValidCombinations.FirstOrDefault() ?? Array.Empty<SummonMaterial>();

    public SummonMaterialSelectionSpec(
        IReadOnlyList<SummonMaterial> candidates,
        int minSelect,
        int? maxSelect,
        Func<IReadOnlyList<SummonMaterial>, bool> isValidCombination
    ) {
        Candidates = candidates
            .Where(material => material.Card != null)
            .Distinct()
            .ToList();
        CandidateCards = Candidates
            .Select(material => material.Card!)
            .ToList();
        _materialsByCard = Candidates.ToDictionary(material => material.Card!);

        MinSelect = Math.Max(1, minSelect);
        MaxSelect = Math.Min(maxSelect ?? Candidates.Count, Candidates.Count);

        List<IReadOnlyList<SummonMaterial>> validCombinations = [];
        if (MinSelect <= MaxSelect) {
            for (int count = MinSelect; count <= MaxSelect; count++) {
                List<SummonMaterial> current = new(count);
                AddCombinations(0, count, current, validCombinations, isValidCombination);
            }
        }

        ValidCombinations = validCombinations;
        _validCombinationSets = validCombinations
            .Select(combination => combination.ToHashSet())
            .ToList();
    }

    public SummonMaterial? GetMaterial(CardModel card) {
        return _materialsByCard.GetValueOrDefault(card);
    }

    public IReadOnlyList<SummonMaterial> ResolveMaterials(IEnumerable<CardModel> cards) {
        List<SummonMaterial> materials = [];
        HashSet<SummonMaterial> seen = [];
        foreach (CardModel card in cards) {
            if (_materialsByCard.TryGetValue(card, out SummonMaterial? material) && seen.Add(material)) {
                materials.Add(material);
            }
        }

        return materials;
    }

    public bool IsValidSelection(IEnumerable<SummonMaterial> selection) {
        HashSet<SummonMaterial> selected = selection.ToHashSet();
        return _validCombinationSets.Any(combination => combination.SetEquals(selected));
    }

    public bool CanExtendSelection(
        IEnumerable<SummonMaterial> selection,
        SummonMaterial? materialToAdd = null
    ) {
        HashSet<SummonMaterial> selected = selection.ToHashSet();
        if (materialToAdd != null) {
            selected.Add(materialToAdd);
        }

        return _validCombinationSets.Any(combination => selected.IsSubsetOf(combination));
    }

    private void AddCombinations(
        int startIndex,
        int remainingCount,
        List<SummonMaterial> current,
        List<IReadOnlyList<SummonMaterial>> results,
        Func<IReadOnlyList<SummonMaterial>, bool> isValidCombination
    ) {
        if (remainingCount == 0) {
            if (isValidCombination(current)) {
                results.Add(current.ToList());
            }

            return;
        }

        int lastStartIndex = Candidates.Count - remainingCount;
        for (int i = startIndex; i <= lastStartIndex; i++) {
            current.Add(Candidates[i]);
            AddCombinations(i + 1, remainingCount - 1, current, results, isValidCombination);
            current.RemoveAt(current.Count - 1);
        }
    }
}
