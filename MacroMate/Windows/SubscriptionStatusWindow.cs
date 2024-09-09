using System.Linq;
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

        var rootTaskDetails = Env.SubscriptionManager.GetSubscriptionTaskDetails(Subscription);
        if (rootTaskDetails.Children.IsEmpty) {
            ImGui.Text("No operation active");
            return;
        }

        foreach (var taskDetail in rootTaskDetails.Children.OrderBy(c => c.CreatedAt)) {
            DrawTaskDetails(taskDetail, isRoot: true);
        }
    }

    private void DrawTaskDetails(SubscriptionTaskDetails taskDetails, bool isRoot) {
        ImGui.PushID(taskDetails.Id.ToString());
        var taskPopup = DrawTaskPopup(taskDetails);

        var loadingSpinner = taskDetails.IsLoading ? " " + ("|/-\\"[(int)(ImGui.GetTime() / 0.05f) % 3]).ToString() : "";
        var body = isRoot ? taskDetails.Summary : taskDetails.Message;
        var message = body + loadingSpinner;

        if (taskDetails.Children.IsEmpty) {
            ImGui.TextUnformatted(message);
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                ImGui.OpenPopup(taskPopup);
            }
        } else if (ImGui.TreeNode(message)) {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                ImGui.OpenPopup(taskPopup);
            }

            foreach (var child in taskDetails.Children.OrderBy(c => c.CreatedAt)) {
                DrawTaskDetails(child, isRoot: false);
            }
            ImGui.TreePop();
        }

        ImGui.PopID();
    }

    private uint DrawTaskPopup(SubscriptionTaskDetails taskDetails) {
        var popupName = $"subscription_status_window/task_popup/{taskDetails.Id}";
        var popupId = ImGui.GetID(popupName);

        if (ImGui.BeginPopup(popupName)) {
            if (ImGui.Selectable("Copy Text to Clipboard")) {
                ImGui.SetClipboardText(taskDetails.Message);
            }
            ImGui.EndPopup();
        }

        return popupId;
    }
}
