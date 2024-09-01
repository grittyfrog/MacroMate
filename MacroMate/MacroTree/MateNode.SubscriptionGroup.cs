using MacroMate.Extensions.Dotnet.Tree;

namespace MacroMate.MacroTree;

public abstract partial class MateNode : TreeNode<MateNode> {
    /// <summary>
    /// A subscription group is populated by an 3rd-party via the user providing a URL.
    /// source
    /// </summary>
    public class SubscriptionGroup : MateNode {
        public required string SubscriptionUrl { get; set; }
        // TODO: Last Update Time?
    }
}
