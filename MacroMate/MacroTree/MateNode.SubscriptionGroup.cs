using System;
using System.Collections.Generic;
using System.Linq;
using MacroMate.Extensions.Dotnet.Tree;

namespace MacroMate.MacroTree;

public abstract partial class MateNode : TreeNode<MateNode> {
    /// <summary>
    /// A subscription group is populated by an 3rd-party via the user providing a URL.
    /// source
    /// </summary>
    public class SubscriptionGroup : MateNode {
        public required string SubscriptionUrl { get; set; }

        /// <summary>
        /// The time we last syncronized
        /// </summary>
        public DateTimeOffset? LastSyncTime { get; set; } = null;

        /// <summary>
        /// The ETags we saw when we last syncronized this subscription.
        /// </summary>
        public List<string> LastSyncETags { get; set; } = new();

        /// <summary>
        /// The ETags we saw last time we checked for updates (or updated). This is our
        /// best approximation of the current ETag state of the remote subscription.
        ///
        /// Used to determine if there are updated
        /// if
        /// </summary>
        public List<string> KnownRemoteETags { get; set; } = new();

        /// <summary>
        /// Computes the relative URL of this host from the Subscription Url
        /// </summary>
        public string RelativeUrl(string suburl) {
            var baseUrl = new Uri(SubscriptionUrl);
            var baseUrlSegmentsNoLast = baseUrl.Segments.Take(baseUrl.Segments.Length - 1).ToList();
            baseUrlSegmentsNoLast[baseUrlSegmentsNoLast.Count - 1] = baseUrlSegmentsNoLast[baseUrlSegmentsNoLast.Count - 1].TrimEnd('/');

            var urlBuilder = new UriBuilder(baseUrl);
            urlBuilder.Path = string.Concat(baseUrlSegmentsNoLast);
            urlBuilder.Path += "/" + suburl.TrimStart('/');
            urlBuilder.Query = "";

            return urlBuilder.Uri.ToString();
        }

        public bool HasUpdates() {
            return !LastSyncETags.ToHashSet().SetEquals(KnownRemoteETags.ToHashSet());
        }
    }
}
