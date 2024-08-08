using System;
using System.Linq;
using Dalamud.Plugin.Ipc;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.Extensions.Dalamud.Str;
using MacroMate.MacroTree;

namespace MacroMate.Ipc;

public class IPCManager : IDisposable {
    private bool isAvailable = false;
    private readonly ICallGateProvider<bool> cgIsAvailable;
    private readonly ICallGateProvider<string, string, string?, uint?, bool> cgCreateMacro;
    private readonly ICallGateProvider<string, bool> cgCreateGroup;
    private readonly ICallGateProvider<string, (bool, string?)> cgValidatePath;
    private readonly ICallGateProvider<string, (bool, string?)> cgValidateMacroPath;
    private readonly ICallGateProvider<string, (bool, string?)> cgValidateGroupPath;

    public IPCManager() {
        cgIsAvailable = Env.PluginInterface.GetIpcProvider<bool>("MacroMate.IsAvailable");
        cgIsAvailable.RegisterFunc(IsAvailable);

        cgCreateMacro = Env.PluginInterface.GetIpcProvider<string, string, string?, uint?, bool>("MacroMate.CreateOrUpdateMacro");
        cgCreateMacro.RegisterFunc(CreateOrUpdateMacro);

        cgCreateGroup = Env.PluginInterface.GetIpcProvider<string, bool>("MacroMate.CreateGroup");
        cgCreateGroup.RegisterFunc(CreateGroup);

        cgValidatePath = Env.PluginInterface.GetIpcProvider<string, (bool, string?)>("MacroMate.ValidatePath");
        cgValidatePath.RegisterFunc(ValidatePath);

        cgValidateMacroPath = Env.PluginInterface.GetIpcProvider<string, (bool, string?)>("MacroMate.ValidateMacroPath");
        cgValidateMacroPath.RegisterFunc(ValidateMacroPath);

        cgValidateGroupPath = Env.PluginInterface.GetIpcProvider<string, (bool, string?)>("MacroMate.ValidateGroupPath");
        cgValidateGroupPath.RegisterFunc(ValidateGroupPath);

        isAvailable = true;
        cgIsAvailable.SendMessage();
    }

    private bool IsAvailable() {
        return isAvailable;
    }

    /**
     * IPC Function to create or update a macro.
     */
    private bool CreateOrUpdateMacro(
        string name,
        string lines,
        string? parentPath,
        uint? iconId
    ) {
        var parentGroup = GetGroupFromPath(parentPath);

        var macro = Env.MacroConfig.CreateOrFindMacroByName(name, parentGroup);
        macro.Name = name;
        macro.IconId = iconId ?? macro.IconId;
        macro.Lines = SeStringEx.ParseFromText(lines);
        Env.MacroConfig.NotifyEdit();

        return true;
    }

    /// <summary>
    /// IPC function to create a new group or set of groups.
    ///
    /// Does not fail on missing named group, instead it will create all missing groups to ensure this path exists.
    /// </summary>
    private bool CreateGroup(string path) {
        var parsedPath = MacroPath.ParseText(path ?? "/");
        if (parsedPath.Segments.Count == 0) { return true; }

        MateNode.Group? attachmentPoint = null;
        // The 'top' new group, only attached at the end in case we error mid-function
        MateNode.Group? highestNewGroupAdded = null;

        MateNode.Group currentNode = (MateNode.Group)Env.MacroConfig.Root;
        foreach (var segment in parsedPath.Segments) {
            var childNode = currentNode.GetChildFromPathSegment(segment);
            if (childNode == null) {
                if (segment is MacroPathSegment.ByName nameSegment) {
                    if (nameSegment.offset != 0) {
                        throw new ArgumentException($"cannot create group for missing named index (path: {path})");
                    }

                    var newChild = new MateNode.Group { Name = nameSegment.name };
                    // We
                    if (highestNewGroupAdded == null) {
                        attachmentPoint = currentNode;
                        highestNewGroupAdded = newChild;
                    } else {
                        currentNode.Attach(newChild);
                    }
                    currentNode = newChild;
                } else {
                    throw new ArgumentException($"cannot create group for missing index (path: {path}))");
                }
            } else if (childNode is MateNode.Group childGroup) {
                currentNode = childGroup;
            } else {
                throw new ArgumentException($"cannot create group for path containing macro (path: {path})");
            }
        }

        if (highestNewGroupAdded != null && attachmentPoint != null) {
            Env.MacroConfig.MoveInto(highestNewGroupAdded, attachmentPoint);
        }

        return true;
    }

    /// <returns>(true, null) if valid, otherwise (false, "the validation error")</returns>
    private (bool, string?) ValidatePath(string path) {
        try {
            var parsedPath = MacroPath.ParseText(path ?? "/");
            var target = Env.MacroConfig.Root.Walk(parsedPath);
            if (target == null) { return (false, $"no node found at '{path}'"); }
            return (true, null);
        } catch (ArgumentException ex) {
            return (false, ex.Message);
        }
    }

    /// <summary>Validates that `path` is valid and points to a macro</summary>
    /// <returns>(true, null) if valid, otherwise (false, "the validation error")</returns>
    private (bool, string?) ValidateMacroPath(string path) {
        try {
            var parsedPath = MacroPath.ParseText(path ?? "/");
            var target = Env.MacroConfig.Root.Walk(parsedPath);
            if (target == null) { return (false, $"no node found at '{path}'"); }
            if (target is not MateNode.Macro) { return (false, $"expected macro at '{path}'"); }
            return (true, null);
        } catch (ArgumentException ex) {
            return (false, ex.Message);
        }
    }

    /// <summary>Validates that `path` is valid and points to a group</summary>
    /// <returns>(true, null) if valid, otherwise (false, "the validation error")</returns>
    private (bool, string?) ValidateGroupPath(string path) {
        try {
            var parsedPath = MacroPath.ParseText(path ?? "/");
            var target = Env.MacroConfig.Root.Walk(parsedPath);
            if (target == null) { return (false, $"no node found at '{path}'"); }
            if (target is not MateNode.Group) { return (false, $"expected group at '{path}'"); }
            return (true, null);
        } catch (ArgumentException ex) {
            return (false, ex.Message);
        }
    }

    public void Dispose() {
        isAvailable = false;
        cgIsAvailable.UnregisterFunc();
    }

    private MateNode.Group GetGroupFromPath(string? parentPath) {
        var parsedParentPath = MacroPath.ParseText(parentPath ?? "/");
        var parent = Env.MacroConfig.Root.Walk(parsedParentPath);
        if (parent == null) {
            throw new ArgumentException($"Cannot create macro: parent not found (path: '{parentPath}')");
        }

        if (parent is MateNode.Group parentGroup) {
            return parentGroup;
        } else {
            throw new ArgumentException($"Cannot create macro: parent must point to a group (path: '{parentPath}')");
        }
    }
}
