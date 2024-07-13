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

    public IPCManager() {
        cgIsAvailable = Env.PluginInterface.GetIpcProvider<bool>("MacroMate.IsAvailable");
        cgIsAvailable.RegisterFunc(IsAvailable);

        cgCreateMacro = Env.PluginInterface.GetIpcProvider<string, string, string?, uint?, bool>("MacroMate.CreateOrUpdateMacro");
        cgCreateMacro.RegisterFunc(CreateOrUpdateMacro);

        cgCreateGroup = Env.PluginInterface.GetIpcProvider<string, bool>("MacroMate.CreateGroup");
        cgCreateGroup.RegisterFunc(CreateGroup);

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

        var existingNode = parentGroup.Children.FirstOrDefault(child => child.Name == name);
        if (existingNode is MateNode.Macro existingMacro) {
            existingMacro.IconId = iconId ?? existingMacro.IconId;
            existingMacro.Lines = SeStringEx.ParseFromText(lines);
            Env.MacroConfig.NotifyEdit();
        } else {
            var macro = new MateNode.Macro {
                Name = name,
                IconId = iconId ?? VanillaMacro.DefaultIconId,
                Lines = SeStringEx.ParseFromText(lines)
            };

            Env.MacroConfig.MoveInto(macro, parentGroup);
        }


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
