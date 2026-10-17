using Dalamud.Configuration;
namespace MarketLedger;
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public string? Language { get; set; }
    public bool Recording { get; set; } = true;
}
