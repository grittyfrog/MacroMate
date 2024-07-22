# Macro Mate IPC

Macro Mate provides a few IPC functions via the normal Dalamud IPC interface. 

## Example

In your service you could have a client like this:

```csharp
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

class MacroMateIPCClient {
    private ICallGateSubscriber<bool> csIsAvailable;
    private ICallGateSubscriber<string, string, string?, uint?, bool> csCreateOrUpdateMacro;
    private ICallGateSubscriber<string, bool> csCreateGroup;
    private ICallGateSubscriber<string, (bool, string?)> csValidatePath;
    private ICallGateSubscriber<string, (bool, string?)> csValidateMacroPath;
    private ICallGateSubscriber<string, (bool, string?)> csValidateGroupPath;

    public MacroMateIPCClient(IDalamudPluginInterface pluginInterface) {
        csIsAvailable = pluginInterface.GetIpcSubscriber<bool>("MacroMate.IsAvailable");
        csCreateOrUpdateMacro = pluginInterface.GetIpcSubscriber<string, string, string?, uint?, bool>("MacroMate.CreateOrUpdateMacro");
        csCreateGroup = pluginInterface.GetIpcSubscriber<string, bool>("MacroMate.CreateGroup");
        csValidatePath = pluginInterface.GetIpcSubscriber<string, (bool, string?)>("MacroMate.ValidatePath");
        csValidateMacroPath = pluginInterface.GetIpcSubscriber<string, (bool, string?)>("MacroMate.ValidateMacroPath");
        csValidateGroupPath = pluginInterface.GetIpcSubscriber<string, (bool, string?)>("MacroMate.ValidateGroupPath");
    }

    public bool IsAvailable() => csIsAvailable.InvokeFunc();

    public bool CreateOrUpdateMacro(
        string name,
        string lines,
        string? parentPath = null,
        uint? iconId = null
    ) => csCreateOrUpdateMacro.InvokeFunc(name, lines, parentPath, iconId);

    public bool CreateGroup(string path) => csCreateGroup.InvokeFunc(path);
    
    public (bool, string?) ValidatePath(string path) => csValidatePath.InvokeFunc(path);
    public (bool, string?) ValidateMacroPath(string path) => csValidateMacroPath.InvokeFunc(path);
    public (bool, string?) ValidateGroupPath(string path) => csValidateGroupPath.InvokeFunc(path);
}
```

It can be used like so:

```csharp
MacroMateIPCClient macroMateClient = null!; /* get this from IoC or service or whatever */ 

void Example() {
    // First make a group for our macros, we probably don't want them all in Root
    macroMateClient.CreateGroup("/CoolAddon");

    // Now we can add a Macro into the group
    macroMateClient.CreateOrUpdateMacro(
      "My Cool Macro",
      "/echo Hello World\n/echo This is my cool macro",
      "/CoolAddon",
      66001
    );

    // Later we might want to update our Macro, we can do it by creating the macro again with the
    // same name and parent path.
    macroMateClient.CreateOrUpdateMacro(
      "My Cool Macro",
      "/echo My new and improved macro with a better icon",
      "/CoolAddon",
      4769
    );
}
```

## Reference

### `MacroMate.IsAvailable`

**Signature:** `bool IsAvailable()`

Returns `true` when `MacroMate` has been initialized and is ready to recieve IPC calls.

Usage:

```csharp
ICallGateSubscriber<bool> csIsAvailable;
csIsAvailable = pluginInterface.GetIpcSubscriber<bool>("MacroMate.IsAvailable");
csIsAvailable.InvokeFunc(); 
```

### `MacroMate.CreateOrUpdateMacro`

**Signature:** `bool CreateMacro(string name, string lines, string? parentPath, uint? iconId)`

Create or updates a macro with the given name and lines. 

If a macro with the same name already exists under `parentPath` it will be updated, otherwise
a new macro will be created.

If `parentPath` is provided the macro will be placed under the group identified by `parentPath`. 
See [Paths](./paths.md) for information about path syntax. Otherwise the macro will be placed at
the root of the macro tree.

If `iconId` is provided the given icon will be set. If not provided icon id defaults to 66001 (the default "M" icon for macros).

Auto translate payloads can also be included in `lines`. See [Auto Translation](./auto-translation.md) for details.

Usage:

```csharp
ICallGateSubscriber<string, string, string?, uint?, bool> csCreateOrUpdateMacro;
csCreateOrUpdateMacro = pluginInterface.GetIpcSubscriber<string, string, string?, uint?, bool>("MacroMate.CreateOrUpdateMacro");
csCreateOrUpdateMacro.InvokeFunc(
    "My Cool Macro",                                  // Macro Name
    "/echo Hello World\n/echo This is my cool macro", // Macro Text
    "/",                                              // Parent group for this macro
    66001                                             // Icon ID
)
```

Throws `ArgumentException` when:

- `path` does not start with `/`


### `MacroMate.CreateGroup`

**Signature:** `bool CreateGroup(string path)`

Creates a path of groups with the given names. Will not recreate groups that already exist (by name).

Usage:

```csharp
ICallGateSubscriber<string, bool> csCreateGroup;
csCreateGroup = pluginInterface.GetIpcSubscriber<string, bool>("MacroMate.CreateGroup");
csCreateGroup.InvokeFunc("/CoolGroup/Subgroup")
```

Throws `ArgumentException` when:

- `path` does not start with `/`
- `path` includes an index (`@0`) _and_ that index does not already exist
- `path` includes a named index (`Hello@2`) _and_ that index does not already exist
- `path` includes a macro (only groups are allowed)

### `MacroMate.ValidatePath`

**Signature: `(bool, string?) ValidatePath(string path)`**

Validates that `path` is valid and points a node in the tree

Returns `(true, null)` if valid, otherwise `(false, "the validation error")`

Usage:
```
ICallGateSubscriber<string, (bool, string?)> csValidatePath;
csValidatePath = pluginInterface.GetIpcSubscriber<string, (bool, string?)>("MacroMate.ValidatePath");
var (valid, err) = csValidatePath.InvokeFunc("/My Cool Path");
if (!valid) { /* do something with err */ }
```


### `MacroMate.ValidateMacroPath`

**Signature: `(bool, string?) ValidateMacroPath(string path)`**

Validates that `path` is valid and points a macro in the tree

Returns `(true, null)` if valid, otherwise `(false, "the validation error")`

Usage:
```
ICallGateSubscriber<string, (bool, string?)> csValidateMacroPath;
csValidateMacroPath = pluginInterface.GetIpcSubscriber<string, (bool, string?)>("MacroMate.ValidateMacroPath");
var (valid, err) = csValidateMacroPath.InvokeFunc("/My Macro");
if (!valid) { /* do something with err */ }
```


### `MacroMate.ValidateGroupPath`

**Signature: `(bool, string?) ValidateGroupPath(string path)`**

Validates that `path` is valid and points a group in the tree

Returns `(true, null)` if valid, otherwise `(false, "the validation error")`

Usage:
```
ICallGateSubscriber<string, (bool, string?)> csValidateGroupPath;
csValidateGroupPath = pluginInterface.GetIpcSubscriber<string, (bool, string?)>("MacroMate.ValidateGroupPath");
var (valid, err) = csValidateGroupPath.InvokeFunc("/My Group");
if (!valid) { /* do something with err */ }
```
