using System;
using Dalamud.Plugin.Services;

namespace MacroMate.Conditions;

public class ConditionManager : IDisposable {
    private bool loggedIn;
    private CurrentConditions? currentConditions;

    public delegate void OnConditionChangeDelegate(CurrentConditions conditions);
    public event OnConditionChangeDelegate? ConditionChange;

    public ConditionManager() {
        Env.Framework.RunOnTick(() => {
            this.loggedIn = Env.ClientState.LocalPlayer != null;
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

    public CurrentConditions CurrentConditions() {
        return currentConditions ?? QueryCurrentConditions();
    }

    private void OnFrameworkUpdate(IFramework framework) {
        if (!loggedIn) { return; }

        var newConditions = QueryCurrentConditions();
        if (currentConditions != newConditions) {
            currentConditions = newConditions;

            if (ConditionChange != null) {
                ConditionChange(currentConditions);
            }
        }
    }

    private CurrentConditions QueryCurrentConditions() {
        return new CurrentConditions(
            content: ContentCondition.Current(),
            location: LocationCondition.Current(),
            targetNpc: TargetNameCondition.Current(),
            job: JobCondition.Current(),
            pvpState: PvpStateCondition.Current(),
            playerCondition: PlayerConditionCondition.Current(),
            hudLayoutCondition: HUDLayoutCondition.Current(),
            craftMaxDurabilityCondition: CurrentCraftMaxDurabilityCondition.Current(),
            craftMaxQualityCondition: CurrentCraftMaxQualityCondition.Current(),
            craftDifficultyCondition: CurrentCraftDifficultyCondition.Current()
        );
    }

    private void OnLogin() {
        this.loggedIn = true;
    }

    private void OnLogout(int type, int code) {
        this.loggedIn = false;
    }
}
