using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MacroMate.Extensions.Dotnet;
using MacroMate.MacroTree;

namespace MacroMate.Subscription;

public class SubscriptionUrlCache {
    public class Entry {
        public Guid SubscriptionGroupId { get; set; }
        public required string Url { get; set; }
        public string? LastKnownETag { get; set; } = null;
    }

    public Dictionary<string, Entry> Entries = new();

    public void AddEntries(MateNode.SubscriptionGroup sGroup, IDictionary<string, string> urlToEtags) {
        var entries = urlToEtags.Select(kv => new Entry {
            SubscriptionGroupId = sGroup.Id,
            Url = kv.Key,
            LastKnownETag = kv.Value
        }).ToDictionary(e => e.Url);

        Entries.AddRange(entries);
    }

    public bool TryGetEtagForUrl(string url, [MaybeNullWhen(false)] out string knownEtag) {
        if (Entries.TryGetValue(url, out var entry)) {
            knownEtag = entry.LastKnownETag;
            if (knownEtag != null) {
                return true;
            }
        }
        knownEtag = null;
        return false;
    }

    public void ClearForSubscription(Guid subscriptionGroupId) {
        Entries = Entries
            .Where(kv => kv.Value.SubscriptionGroupId != subscriptionGroupId)
            .ToDictionary();
    }

    /// <summary>
    /// We want to update in place to not break other parts of the system that may reference this cache
    /// </summary>
    public void OverwriteFrom(SubscriptionUrlCache other) {
        Entries = new Dictionary<string, Entry>(other.Entries);
    }
}
