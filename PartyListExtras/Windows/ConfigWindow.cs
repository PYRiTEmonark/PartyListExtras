using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;

namespace PartyListExtras.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration configuration;
    private Plugin plugin;

    public ConfigWindow(Plugin plugin) : base(
        "Party List Extras Config", ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.Size = new Vector2(500, 500);
        this.SizeCondition = ImGuiCond.Once;

        this.configuration = plugin.Configuration;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        int dm = configuration.DisplayMode;
        ImGui.Text("Text to show next to icons");
        var options = new string[]
        {
            "All",
            "Information if present, label otherwise",
            "Information only",
            "None",
        };
        if (ImGui.BeginCombo(" ", options[dm]))
        {
            for (int i = 0; i < options.Length; i++)
            {
                var x = configuration.DisplayMode == i;
                if (ImGui.Selectable(options[i], x))
                {
                    configuration.DisplayMode = i;
                    dm = i;
                }
                if (x)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }

        // Display mode Demo
        ImGui.Text("Example");
        var imgsize = 20;
        if (dm != 3)
        {
            ImGui.Text("20%%");
            ImGui.SameLine();
        }
        ImGui.Image(plugin.textures["all_up.png"].ImGuiHandle, new Vector2(imgsize, imgsize));
        if (dm == 0)
        {
            ImGui.SameLine();
            ImGui.Text("Damage Up");
        }

        ImGui.SameLine();

        ImGui.Image(plugin.textures["stance.png"].ImGuiHandle, new Vector2(imgsize, imgsize));
        if (dm != 2 && dm != 3)
        {
            ImGui.SameLine();
            ImGui.Text("Stance");
        }

        // Overlay Position

        ImGui.Separator();
        ImGui.Text("Overlay Position");

        int oox = configuration.OverlayOffsetX;
        ImGui.Text("Offset");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt(" ", ref oox, 1, 10);
        configuration.OverlayOffsetX = oox;


        // Background colours
        ImGui.Separator();

        if (ImGui.TreeNode("Background Colours"))
        {
            bool gradbkg = configuration.doGradientBackground;
            if (ImGui.Checkbox("Gradient Background", ref gradbkg))
                configuration.doGradientBackground = gradbkg;
            
            if (gradbkg)
            {
                Vector4 leftcol = configuration.colorLeft;
                if (ImGui.ColorPicker4("Left Hand Colour", ref leftcol))
                    configuration.colorLeft = leftcol;
                
                ImGui.SameLine();
                
                Vector4 rightcol = configuration.colorRight;
                if (ImGui.ColorPicker4("Right Hand Colour", ref rightcol))
                    configuration.colorRight = rightcol;
            }
            else
            {
                Vector4 singlecol = configuration.colorSingle;
                if (ImGui.ColorPicker4("Colour", ref singlecol))
                    configuration.colorSingle = singlecol;
            }
        }

        configuration.Save();

    }
}
