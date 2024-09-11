using System;
using System.Collections.Generic;
using System.Linq;
using MacroMate.Subscription;

namespace MacroMate.Serialization.V1;

public class SubscriptionUrlCacheXML {
    public required List<SubscriptionUrlCacheEntryXML> Urls { get; set; }

    public SubscriptionUrlCache ToReal() {
        var entries = Urls
          .Where(u => u.LastKnownETag != null)
          .ToDictionary(
              u => u.Url,
              u => new SubscriptionUrlCache.Entry {
                  SubscriptionGroupId = u.SubscriptionGroupId,
                  Url = u.Url,
                  LastKnownETag = u.LastKnownETag!
              }
          );
        return new SubscriptionUrlCache { Entries = entries };
    }

    public static SubscriptionUrlCacheXML From(SubscriptionUrlCache cache) {
        var urlXML = cache.Entries.Values
            .Select((e) =>
                new SubscriptionUrlCacheEntryXML {
                    SubscriptionGroupId = e.SubscriptionGroupId,
                    Url = e.Url,
                    LastKnownETag = e.LastKnownETag
                }
            );
        return new SubscriptionUrlCacheXML { Urls = urlXML.ToList() };
    }
}

/// <summary>
/// Stores metadata about a URL used for subscriptions -- primarly used for caching
/// </summary>
public class SubscriptionUrlCacheEntryXML {
    public required Guid SubscriptionGroupId { get; set; }
    public required string Url { get; set; }
    public string? LastKnownETag { get; set; } = null;
}
