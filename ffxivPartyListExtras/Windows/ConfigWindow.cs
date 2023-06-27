using System;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace ffxivPartyListExtras.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base(
        "Party List Extras Config")
    {
        this.Size = new Vector2(500, 500);
        this.SizeCondition = ImGuiCond.Once;

        this.Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("Text to show next to icons");
        var options = new string[]
        {
            "All",
            "Information if present, label otherwise",
            "Information only",
            "None",
        };
        ImGui.SetNextItemWidth(ImGui.GetWindowContentRegionMax().X);
        if (ImGui.BeginCombo("", options[Configuration.DisplayMode]))
        {
            for (int i = 0; i < options.Length; i++)
            {
                var x = Configuration.DisplayMode == i;
                if (ImGui.Selectable(options[i], x))
                {
                    Configuration.DisplayMode = i;
                    Configuration.Save();
                }
                if (x)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }
    }
}
