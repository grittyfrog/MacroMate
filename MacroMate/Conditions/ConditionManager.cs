using System;
using Dalamud.Plugin.Services;

namespace MacroMate.Conditions;

public class ConditionManager : IDisposable {
    private bool loggedIn;
    private CurrentConditions? currentConditions;

    public delegate void OnConditionChangeDelegate(CurrentConditions conditions);
    public event OnConditionChangeDelegate? OnConditionChange;

    public ConditionManager() {
        Env.Framework.RunOnTick(() => {
            this.loggedIn = Env.PlayerState.IsLoaded;
        });

        Env.Framework.Update += this.OnFrameworkUpdate;
        Env.ClientState.Login += this.OnLogin;
        Env.ClientState.Logout += this.OnLogout;
    }

    public void Dispose() {
        Env.Framework.Update -= this.OnFrameworkUpdate;
        Env.ClientState.Login -= this.OnLogin;
        Env.ClientState.Logout -= this.OnLogout;
    }

    public CurrentConditions Conditions { get => currentConditions ?? CurrentConditions.Query(); }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!loggedIn) { return; }

        try
        {
            var newConditions = CurrentConditions.Query();
            if (currentConditions != newConditions)
            {
                currentConditions = newConditions;
                OnConditionChange?.Invoke(currentConditions);
            }
        }
        catch (Exception)
        {
        }
    }

    private void OnLogin() {
        this.loggedIn = true;
    }

    private void OnLogout(int type, int code) {
        this.loggedIn = false;
    }
}
