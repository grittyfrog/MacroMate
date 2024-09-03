using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using MacroMate.MacroTree;
using MacroMate.Subscription;

namespace MacroMate.Windows;

public class SubscriptionStatusWindow : Window {
    public static readonly string NAME = "Subscription";

    public MateNode.SubscriptionGroup? Subscription { get; set; }

    public SubscriptionStatusWindow() : base(NAME) {
        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(200, 100),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.Size = new Vector2(500, 400);
    }

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
                SubscriptionState.StepState.FINISHED => step.Outcome != null ? $" ... {step.Outcome}" : "",
                _ => " ... ???"
            };

            ImGui.TextUnformatted($"{step.Message}{stateText}");
        }


    }
}
