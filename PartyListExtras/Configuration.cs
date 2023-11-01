using Dalamud.Configuration;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using System;

namespace PartyListExtras
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public int DisplayMode { get; set; } = 2;

        public int OverlayOffset { get; set; } = 10;

        public bool doGradientBackground { get; set; } = true;
        public Vector4 colorRight { get; set; } = new Vector4(0f, 0f, 0f, 1f);
        public Vector4 colorLeft { get; set; } = new Vector4(0f, 0f, 0f, 0f);
        public Vector4 colorSingle { get; set; } = new Vector4(0f, 0f, 0f, 1f);

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
