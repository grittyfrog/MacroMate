using Dalamud.Interface.Windowing;
using ImGuiNET;
using MacroMate.MacroTree;
using MacroMate.Subscription;

namespace MacroMate.Windows;

public class SubscriptionStatusWindow : Window {
    public static readonly string NAME = "Subscription";

    public MateNode.SubscriptionGroup? Subscription { get; set; }

    public SubscriptionStatusWindow() : base(NAME, ImGuiWindowFlags.AlwaysAutoResize) {}

    public override void Draw() {
        if (Subscription == null) { return; }

        ImGui.TextUnformatted(Subscription.Name);
        ImGui.Separator();

        var status = Env.SubscriptionManager.GetSubscriptionState(Subscription);
        if (status == null) {
            ImGui.Text("No operation active");
            return;
        }

        foreach (var step in status.Steps) {
            var stateText = step.State switch {
                SubscriptionState.StepState.IN_PROGRESS => " ... " + ("|/-\\"[(int)(ImGui.GetTime() / 0.05f) % 3]).ToString(),
                SubscriptionState.StepState.SUCCESS => " ... OK",
                SubscriptionState.StepState.FAILED => $" ... ERR: {step.FailMessage}",
                SubscriptionState.StepState.INFO => "",
                _ => " ... ???"
            };

            ImGui.TextUnformatted($"{step.Message}{stateText}");
        }


    }
}
