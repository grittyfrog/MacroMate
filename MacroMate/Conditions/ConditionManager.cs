using System;
using Dalamud.Plugin.Services;

namespace MacroMate.Conditions;

public class ConditionManager : IDisposable {
    private CurrentConditions? currentConditions;

    public delegate void OnConditionChangeDelegate(CurrentConditions conditions);
    public event OnConditionChangeDelegate? ConditionChange;

    public ConditionManager() {
        Env.Framework.Update += this.OnFrameworkUpdate;
    }

    public void Dispose() {
        Env.Framework.Update -= this.OnFrameworkUpdate;
    }

    public CurrentConditions Conditions { get => currentConditions ?? CurrentConditions.Query(); }

    private void OnFrameworkUpdate(IFramework framework) {
        if (!Env.ClientState.IsLoggedIn) { return; }

        var newConditions = CurrentConditions.Query();
        if (currentConditions != newConditions) {
            currentConditions = newConditions;

            if (ConditionChange != null) {
                ConditionChange(currentConditions);
            }
        }
    }
}
