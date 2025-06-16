using System;
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
    public MateNodeErrors Errors { get; init; } = new();

    private MateNodeAlertSummary? _errorSummary = null;
    public MateNodeAlertSummary ErrorSummary {
        get {
            if (_errorSummary == null) {
                _errorSummary = ComputeAlertSummary(MateNodeError.ErrorSeverity.ERROR, (n) => n.ErrorSummary);
            }
            return _errorSummary;
        }
        private set { _errorSummary = value; }
    }

    private MateNodeAlertSummary? _warningSummary = null;
    public MateNodeAlertSummary WarningSummary {
        get {
            if (_warningSummary == null) {
                _warningSummary = ComputeAlertSummary(MateNodeError.ErrorSeverity.WARN, (n) => n.WarningSummary);
            }
            return _warningSummary;
        }
        private set { _warningSummary = value; }
    }

    public MateNodeAlertSummary AlertSummaryFor(MateNodeError.ErrorSeverity severity) {
        return severity switch {
            MateNodeError.ErrorSeverity.ERROR => ErrorSummary,
            MateNodeError.ErrorSeverity.WARN => WarningSummary,
            _ => throw new Exception("Unexpected ErrorSeverity")
        };
    }

    public MateNode() {
        Errors.Changed += OnErrorChanged;
    }

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

    /// Propagate the `ChildrenHaveWarnings` and `ChildrenHaveErrors` upwards
    private void OnErrorChanged(Type errorType) {
        foreach (var ancestor in SelfAndAncestors()) {
            ancestor._errorSummary = null;
            ancestor._warningSummary = null;
        }
    }

    private MateNodeAlertSummary ComputeAlertSummary(
        MateNodeError.ErrorSeverity severity,
        Func<MateNode, MateNodeAlertSummary> summarySelector
    ) {
        return new MateNodeAlertSummary {
            SelfCount = Errors.Where(e => e.Severity == severity).Count(),
            DescendentCount = Children.Sum(c => summarySelector(c).Total)
        };
    }
}
