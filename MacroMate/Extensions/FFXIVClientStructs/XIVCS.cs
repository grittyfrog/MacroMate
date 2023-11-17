using System.Reflection;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MacroMate.Extensions.FFXIVClientStructs;

/// <summary>
/// FFXIVClientStructs helper functions
/// </summary>
public static unsafe class XIVCS {
    public static AgentInterface* GetAgent(AgentId agentId) {
        return Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(agentId);
    }

    public static T* GetAgent<T>() where T : unmanaged {
        var attr = typeof(T).GetCustomAttribute<AgentAttribute>();
        if (attr == null) return null;
        return (T*)GetAgent(attr.ID);
    }
}
