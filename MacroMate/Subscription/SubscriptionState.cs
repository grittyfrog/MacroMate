using System.Collections.Generic;

namespace MacroMate.Subscription;

/// <summary>
/// The state of a single subscription group
/// </summary>
public class SubscriptionState {
    public enum StepState { INFO, IN_PROGRESS, SUCCESS, FAILED };
    public class Step {
        public required StepState State { get; set; }
        public required string Message { get; set; }
        public string? FailMessage { get; set; }
    }

    public List<Step> Steps = new();

    public void Reset() {
        Steps.Clear();
    }

    public Step InProgress(string message) {
        var step = new Step { State = StepState.IN_PROGRESS, Message = message };
        Steps.Add(step);
        return step;
    }

    public Step Success(string message) {
        var step = new Step { State = StepState.SUCCESS, Message = message };
        Steps.Add(step);
        return step;
    }

    public Step Info(string message) {
        var step = new Step { State = StepState.INFO, Message = message };
        Steps.Add(step);
        return step;
    }

    public void FailLast(string failMessage) {
        if (Steps.Count == 0) { return; }

        var last = Steps[Steps.Count - 1];
        last.State = StepState.FAILED;
        last.FailMessage = failMessage;
    }
}
