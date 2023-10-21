using Dalamud.Configuration;
using System;

namespace MacroMate {
    [Serializable]
    public class Configuration : IPluginConfiguration {
        public int Version { get; set; } = 0;

        public void Save() {
            Env.PluginInterface.SavePluginConfig(this);
        }
    }
}
