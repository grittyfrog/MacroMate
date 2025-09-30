# ImRaii Usage Guide

ImRaii is a helper library provided by Dalamud. It is used to automatically `Begin`/`End` ImGui components.

This guide covers when to use different patterns for ImGui scoped objects in MacroMate.

## Patterns Overview

### 1. Top-level `ImRaii` using (Most Preferred)

**When to use:** When the entire method or large code block should be scoped and it's not awkward to structure the code this way.

```csharp
public void DrawCategoryTree() {
    using var child = ImRaii.Child("ImguiChild");
    if (!child) { return; }

    // Draw code here, all lines are scoped within `Child`
    ImGui.Text("Example content");
}
```

**Use cases:**
- When the entire method should be within the scope
- Large UI blocks that would be awkward as lambdas
- Complex logic with early returns, loops, or ref parameters

### 2. `ImRaii.Use()` Extension Method (Preferred)

**When to use:** For scoped blocked that don't fit within a Top-level `ImRaii` function.

```csharp
ImRaii.Child("ImguiChild").Use(() => {
    // Code that should run inside the child window
    ImGui.Text("Example content");
});
```

**Use cases:**
- UI blocks that don't fit in a method and don't require mutating `ref` parameters.
- Simple to moderately complex UI sections

### 3. `ImRaii` with `using` Block

**When to use:** When you need to mutate `ref` parameters or capture variables that can't be used in a lambda.

```csharp
using (var child = ImRaii.Child("ImguiChild")) {
    if (child) {
        lastTextScrollY = ImGui.GetScrollY(); // Capturing to local variable

        // Code that runs within the child scope
        ImGui.Text("Example content");
    }
}
```

**Use cases:**
- When you need to mutate ref parameters
- When lambda capture semantics don't work for your use case
- Complex logic that requires local variable mutation

### 4. Traditional `ImGui.Begin`/`ImGui.End` (Legacy)

**When to use:** Existing code that hasn't been migrated yet. Avoid for new code.

**Unconditional End (Child, Window, etc.):**
```csharp
if (ImGui.BeginChild("ImguiChild")) {
    // Child content
    ImGui.Text("Example content");
}
ImGui.EndChild(); // Always call End, even if Begin returns false
```

**Conditional End (MenuBar, Menu, etc.):**
```csharp
if (ImGui.BeginMenuBar()) {
    // MenuBar content
    ImGui.Text("Example content");
    ImGui.EndMenuBar(); // Only call End if Begin returns true
}
```

**Note:** This pattern is still widely used in the codebase but we are gradually migrating to ImRaii patterns. Prefer ImRaii patterns for new code. Some ImGui elements require unconditional `End` calls (like `Child`, `Window`) while others require conditional `End` calls only when `Begin` returns true (like `MenuBar`, `Menu`).

**Use cases:**
- Legacy code that hasn't been migrated
- APIs that don't have ImRaii wrappers yet

## Guidelines

1. **Prefer top-level `using var`** when it's natural to scope the entire method or large block
2. **Use `ImRaii.Use()`** for smaller, contained UI blocks that fit well in lambdas
3. **Use `ImRaii` with `using` block** when you need complex control flow or ref mutations that don't fit #1 or #2
4. **Migrate from `ImGui.Begin/End`** to ImRaii patterns when working on existing code
