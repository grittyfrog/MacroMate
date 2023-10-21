
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace MacroMate.Windows;

public abstract class EventWindow<T> : Window where T : notnull {
    private Dictionary<string, Queue<T>> Events { get; init; } = new();
    protected string? CurrentEventScope { get; set; } = null;

    public EventWindow(string name) : base(name) {}
    public EventWindow(string name, ImGuiWindowFlags flags) : base(name, flags) {}

    protected void EnqueueEvent(T ev) {
        if (CurrentEventScope == null) { return; }
        if (!Events.ContainsKey(CurrentEventScope)) {
            Events[CurrentEventScope] = new();
        }
        Events[CurrentEventScope].Enqueue(ev);
    }

    public bool TryDequeueEvent(string scopeName, out T? ev) {
        if (!Events.ContainsKey(scopeName)) {
            ev = default(T);
            return false;
        }
        return Events[scopeName].TryDequeue(out ev);
    }
}
