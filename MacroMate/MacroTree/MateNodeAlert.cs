using System;
using System.Collections;
using System.Collections.Generic;

namespace MacroMate.MacroTree;

public class MateNodeAlerts : IEnumerable<MateNodeAlert> {
    public event MateNodeErrorChangedEventHandler? Changed;
    public delegate void MateNodeErrorChangedEventHandler(Type errorType);

    private MateNodeAlert.SubscriptionDoesNotContainThis? _subscriptionDoesNotContainThis = null;
    public MateNodeAlert.SubscriptionDoesNotContainThis? SubscriptionDoesNotContainThis {
        get => _subscriptionDoesNotContainThis;
        set { _subscriptionDoesNotContainThis = value; Changed?.Invoke(typeof(MateNodeAlert.SubscriptionDoesNotContainThis)); }
    }

    private MateNodeAlert.SubscriptionSyncError? _subscriptionSyncError = null;
    public MateNodeAlert.SubscriptionSyncError? SubscriptionSyncError {
        get => _subscriptionSyncError;
        set { _subscriptionSyncError = value; Changed?.Invoke(typeof(MateNodeAlert.SubscriptionSyncError)); }
    } 

    public IEnumerator<MateNodeAlert> GetEnumerator() {
        if (SubscriptionDoesNotContainThis != null) { yield return SubscriptionDoesNotContainThis; }
        if (SubscriptionSyncError != null) { yield return SubscriptionSyncError; }
    }

    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
}

public interface MateNodeAlert {
    public enum AlertSeverity { ERROR, WARN }
    public AlertSeverity Severity { get => AlertSeverity.ERROR; }

    public class ChildrenHaveErrors(int ErrorCount) : MateNodeAlert {
        AlertSeverity MateNodeAlert.Severity => AlertSeverity.ERROR;
        public override string ToString() => $"{ErrorCount} errors found in this group.";
    }

    public class ChildrenHaveWarnings(int WarningCount) : MateNodeAlert {
        AlertSeverity MateNodeAlert.Severity => AlertSeverity.WARN;
        public override string ToString() => $"{WarningCount} warnings found in this group.";
    }

    public class SubscriptionDoesNotContainThis() : MateNodeAlert {
        AlertSeverity MateNodeAlert.Severity => AlertSeverity.WARN;
        public override string ToString() => $"Subscription does not contain this Macro, it may have been removed.";
    }

    public class SubscriptionSyncError(string message) : MateNodeAlert {
        public override string ToString() => message;
    }
}

public class MateNodeAlertSummary {
    public MateNodeAlert.AlertSeverity Severity { get; set; }
    public int SelfCount { get; set; } = 0;
    public int DescendentCount { get; set; } = 0;
    public int Total => SelfCount + DescendentCount;
}


// public class MateNodeErrorSummary {
//     public int SelfWarningCount { get; set; } = 0;
//     public int SelfErrorCount { get; set; } = 0;
//     public int DescendentWarningCount { get; set; } = 0;
//     public int DescendentErrorCount { get; set; } = 0;

//     public int TotalWarnings => SelfWarningCount + DescendentWarningCount;
//     public int TotalErrors => SelfErrorCount + DescendentErrorCount;

//     public int Total(MateNodeError.ErrorSeverity severity) {
//         return severity switch {
//             MateNodeError.ErrorSeverity.ERROR => TotalErrors,
//             MateNodeError.ErrorSeverity.WARN => TotalWarnings
//         };
//     }

//     public int DescendentCount(MateNodeError.ErrorSeverity severity) {
//     }
// }
