using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace UkrChatSupport;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool ReactOnlyToUkLayout { get; set; } = true;
    public bool ReplaceOnlyOnUkLayout { get; set; } = true;
    public bool ReplaceInput { get; set; } = true;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
