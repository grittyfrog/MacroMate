using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MacroMate.MacroTree;

public class MateNodeAlerts : IEnumerable<MateNodeAlert> {
    public event MateNodeErrorChangedEventHandler? Changed;
    public delegate void MateNodeErrorChangedEventHandler(Type errorType);

    private MateNodeAlert.UnownedSubscriptionMacro? _unownedSubscriptionMacro = null;
    public MateNodeAlert.UnownedSubscriptionMacro? UnownedSubscriptionMacro {
        get => _unownedSubscriptionMacro;
        set { _unownedSubscriptionMacro = value; Changed?.Invoke(typeof(MateNodeAlert.UnownedSubscriptionMacro)); }
    }

    private MateNodeAlert.SubscriptionSyncError? _subscriptionSyncError = null;
    public MateNodeAlert.SubscriptionSyncError? SubscriptionSyncError {
        get => _subscriptionSyncError;
        set { _subscriptionSyncError = value; Changed?.Invoke(typeof(MateNodeAlert.SubscriptionSyncError)); }
    } 

    public IEnumerator<MateNodeAlert> GetEnumerator() {
        if (UnownedSubscriptionMacro != null) { yield return UnownedSubscriptionMacro; }
        if (SubscriptionSyncError != null) { yield return SubscriptionSyncError; }
    }

    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
}

public interface MateNodeAlert {
    public enum AlertSeverity { ERROR, WARN }
    public AlertSeverity Severity { get => AlertSeverity.ERROR; }

    public class UnownedSubscriptionMacro() : MateNodeAlert {
        AlertSeverity MateNodeAlert.Severity => AlertSeverity.WARN;
        public override string ToString() => $"Unowned Macro: Macro does not exist in this subscription, it may have been removed.";
    }

    public class SubscriptionSyncError(string message) : MateNodeAlert {
        public override string ToString() => message;
    }
}

public class MateNodeAlertSummary {
    public ImmutableHashSet<Type> AlertTypes { get; init; } = ImmutableHashSet<Type>.Empty;
    public required int SelfCount { get; set; }
    public int DescendentCount { get; set; } = 0;
    public int Total => SelfCount + DescendentCount;

    public MateNodeAlertSummary Combine(MateNodeAlertSummary other) {
        return new MateNodeAlertSummary {
            AlertTypes = this.AlertTypes.Union(other.AlertTypes),
            SelfCount = this.SelfCount + other.SelfCount,
            DescendentCount = this.DescendentCount + other.DescendentCount
        };
    }

    public MateNodeAlertSummary CombineWithChildren(IEnumerable<MateNodeAlertSummary> children) {
        var childrenCombined = children.Aggregate(
            MateNodeAlertSummary.Empty,
            (combined, summary) => combined.Combine(summary)
        );
        return new MateNodeAlertSummary {
            AlertTypes = this.AlertTypes.Union(childrenCombined.AlertTypes),
            SelfCount = this.SelfCount,
            DescendentCount = this.DescendentCount + childrenCombined.Total,
        };
    }

    public MateNodeAlertSummary CombineWith(params MateNodeAlertSummary[] rest) {
        return CombineAll(new[] { this }.Concat(rest));
    }

    public static MateNodeAlertSummary CombineAll(IEnumerable<MateNodeAlertSummary> summaries) {
        return summaries.Aggregate((combined, summary) => combined.Combine(summary));
    }

    public static MateNodeAlertSummary Empty => new MateNodeAlertSummary { SelfCount = 0 };
}
