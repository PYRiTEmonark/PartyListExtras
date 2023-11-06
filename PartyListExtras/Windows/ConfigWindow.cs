using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Utility;
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
        // Enabled
        bool enable = configuration.EnableOverlay;
        if (ImGui.Checkbox("Enable", ref enable))
            configuration.EnableOverlay = enable;

        bool hic = configuration.hideOutOfCombat;
        if (ImGui.Checkbox("Hide when not in combat", ref hic))
            configuration.hideOutOfCombat = hic;

        bool asid = configuration.alwaysShowInDuty;
        if (ImGui.Checkbox("Always show when in Duty", ref asid))
            configuration.alwaysShowInDuty = asid;

        // Display Mode
        ImGui.Separator();
        ImGui.Text("Icon Appearance");

        int dm = configuration.DisplayMode;
        ImGui.Text("Text to show next to icons");
        var options = new string[]
        {
            "All",
            "Information if present, label otherwise",
            "Information only",
            "None",
        };
        if (ImGui.BeginCombo("##DisplayMode", options[dm]))
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
        ImGui.InputInt("##OffsetX", ref oox, 1, 10);
        configuration.OverlayOffsetX = oox;

        // Background colours
        ImGui.Separator();

        ImGui.Text("Background Colours");
        bool gradbkg = configuration.doGradientBackground;
        if (ImGui.Checkbox("Gradient Background", ref gradbkg))
            configuration.doGradientBackground = gradbkg;
            
        if (gradbkg)
        {
            Vector4 leftcol = configuration.colorLeft;
            if (ImGui.ColorEdit4("Left Hand Colour", ref leftcol))
                configuration.colorLeft = leftcol;

            Vector4 rightcol = configuration.colorRight;
            if (ImGui.ColorEdit4("Right Hand Colour", ref rightcol))
                configuration.colorRight = rightcol;
        }
        else
        {
            Vector4 singlecol = configuration.colorSingle;
            if (ImGui.ColorEdit4("Colour", ref singlecol))
                configuration.colorSingle = singlecol;
        }

        configuration.Save();

        // Advanced icon config
        ImGui.Separator();
        if (ImGui.TreeNode("Advanced Icon Configuration"))
        {
            ImGui.BeginTable("iconconfig", 5);

            ImGui.TableNextColumn();
            ImGui.TableHeader("Name");
            ImGui.TableNextColumn();
            ImGui.TableHeader(""); // Explaination
            ImGui.TableNextColumn();
            ImGui.TableHeader("Icon");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Show");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Label");

            foreach(var effect in configuration.iconConfig.SpecialIcons.Keys)
            {
                var icon = configuration.iconConfig.SpecialIcons[effect];

                // Title
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(effect.ToString());

                // Hover
                ImGui.TableNextColumn();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text(icon.Tooltip ?? "[tooltip missing]");
                    ImGui.EndTooltip();
                }

                // Icon
                ImGui.TableNextColumn();
                ImGui.Image(plugin.textures[icon.FileName].ImGuiHandle, new Vector2(20, 20));

                // Enabled
                ImGui.TableNextColumn();
                bool enableEffect = !configuration.iconConfig.hiddenSpecialEffects.Contains(effect);
                if (ImGui.Checkbox("##enableEffect{0}".Format(effect), ref enableEffect))
                {
                plugin.log.Debug("a: {0}", configuration.iconConfig.hiddenSpecialEffects);
                    if (enableEffect)
                        configuration.iconConfig.hiddenSpecialEffects.Remove(effect);
                    else
                        configuration.iconConfig.hiddenSpecialEffects.Add(effect);
                }

                // Label
                ImGui.TableNextColumn();
                string temp2 = icon.Label!;
                ImGui.Text(temp2);
            }

            ImGui.EndTable();
        }
    }
}
