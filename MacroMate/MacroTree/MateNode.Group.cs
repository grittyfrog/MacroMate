using MacroMate.Extensions.Dotnet.Tree;

namespace MacroMate.MacroTree;

public abstract partial class MateNode : TreeNode<MateNode> {
    public class Group : MateNode {}
}
