using System;
using System.Collections;
using System.Collections.Generic;

namespace MacroMate.MacroTree;

public class MateNodeErrors : IEnumerable<MateNodeError> {
    public event MateNodeErrorChangedEventHandler? Changed;
    public delegate void MateNodeErrorChangedEventHandler(Type errorType);

    private MateNodeError.SubscriptionDoesNotContainThis? _subscriptionDoesNotContainThis = null;
    public MateNodeError.SubscriptionDoesNotContainThis? SubscriptionDoesNotContainThis {
        get => _subscriptionDoesNotContainThis;
        set { _subscriptionDoesNotContainThis = value; Changed?.Invoke(typeof(MateNodeError.SubscriptionDoesNotContainThis)); }
    }

    private MateNodeError.SubscriptionSyncError? _subscriptionSyncError = null;
    public MateNodeError.SubscriptionSyncError? SubscriptionSyncError {
        get => _subscriptionSyncError;
        set { _subscriptionSyncError = value; Changed?.Invoke(typeof(MateNodeError.SubscriptionSyncError)); }
    } 

    public IEnumerator<MateNodeError> GetEnumerator() {
        if (SubscriptionDoesNotContainThis != null) { yield return SubscriptionDoesNotContainThis; }
        if (SubscriptionSyncError != null) { yield return SubscriptionSyncError; }
    }

    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
}

public interface MateNodeError {
    public enum ErrorSeverity { ERROR, WARN }
    public ErrorSeverity Severity { get => ErrorSeverity.ERROR; }

    public class ChildrenHaveErrors(int ErrorCount) : MateNodeError {
        ErrorSeverity MateNodeError.Severity => ErrorSeverity.ERROR;
        public override string ToString() => $"{ErrorCount} errors found in this group.";
    }

    public class ChildrenHaveWarnings(int WarningCount) : MateNodeError {
        ErrorSeverity MateNodeError.Severity => ErrorSeverity.WARN;
        public override string ToString() => $"{WarningCount} warnings found in this group.";
    }

    public class SubscriptionDoesNotContainThis() : MateNodeError {
        ErrorSeverity MateNodeError.Severity => ErrorSeverity.WARN;
        public override string ToString() => $"Subscription does not contain this Macro, it may have been removed.";
    }

    public class SubscriptionSyncError(string message) : MateNodeError {
        public override string ToString() => message;
    }
}

public class MateNodeAlertSummary {
    public MateNodeError.ErrorSeverity Severity { get; set; }
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
