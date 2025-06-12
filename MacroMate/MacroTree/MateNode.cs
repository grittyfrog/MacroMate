using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MacroMate.Extensions.Dotnet.Tree;

namespace MacroMate.MacroTree;

public abstract partial class MateNode : TreeNode<MateNode> {
    public required string Name { get; set; }
    public override string NodeName => Name;

    /// <summary>
    /// User-facing error messages about this node
    /// </summary>
    public List<string> Errors { get; init; } = new();

    /// <summary>Attempts to find the node indicated by [path] using [this] as Root</summary>
    public MateNode? Walk(MacroPath path) {
        MateNode? current = this;
        foreach (var segment in path.Segments) {
            if (current == null) { return null; }
            current = current.GetChildFromPathSegment(segment);
        }
        return current;
    }

    public MateNode? GetChildFromPathSegment(MacroPathSegment segment) {
        switch (segment) {
            case MacroPathSegment.ByName byName:
                return (MateNode?)this.Children
                    .Where(child => child.Name == byName.name)
                    .ElementAtOrDefault(byName.offset);
            case MacroPathSegment.ByIndex byIndex:
                return (MateNode?)this.Children.ElementAtOrDefault(byIndex.index);
            default: throw new Exception($"unrecognised macro path segment: {segment}");
        }
    }

    /// <summary>
    /// Compute the MacroPath to this node
    /// </summary>
    public MacroPath PathTo() {
        return new MacroPath(
            Path().Skip(1).Select(node => new MacroPathSegment.ByName(node.Name)).ToImmutableList<MacroPathSegment>()
        );
    }
}
