using Microsoft.EntityFrameworkCore.ChangeTracking;
using PRFactory.Domain.ValueObjects;

namespace PRFactory.Infrastructure.Persistence.Configurations;

public static class ValueComparerHelpers
{
    public static ValueComparer<List<string>> CreateStringListComparer()
    {
        return new ValueComparer<List<string>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());
    }

    public static ValueComparer<List<Guid>> CreateGuidListComparer()
    {
        return new ValueComparer<List<Guid>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList());
    }

    public static ValueComparer<Dictionary<string, string>?> CreateStringDictionaryComparer()
    {
        return new ValueComparer<Dictionary<string, string>?>(
            (c1, c2) => (c1 == null && c2 == null) ||
                        (c1 != null && c2 != null && c1.OrderBy(kv => kv.Key).SequenceEqual(c2.OrderBy(kv => kv.Key))),
            c => c == null ? 0 : c.Aggregate(0, (a, kv) => HashCode.Combine(a, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
            c => c == null ? null : new Dictionary<string, string>(c));
    }

    public static ValueComparer<List<SuccessCriterion>> CreateSuccessCriterionListComparer()
    {
        return new ValueComparer<List<SuccessCriterion>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.Select(sc => sc).ToList());
    }
}
