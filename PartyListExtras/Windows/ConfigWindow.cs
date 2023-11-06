using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Utility;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace PartyListExtras.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration configuration;
    private Plugin plugin;

    // Used for arming the icon config reset button
    private bool armIconReset = false;

    public ConfigWindow(Plugin plugin) : base(
        "Party List Extras Config", ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.Size = new Vector2(500, 500);
        this.SizeCondition = ImGuiCond.Always;

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
        ImGui.Text("Overlay Appearance");

        ImGui.BeginTable("PositionTable", 5);

        int wd = configuration.OverlayWidth;
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Size");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("X##OverlayWidth", ref wd, 1, 10);
        configuration.OverlayWidth = wd;

        int oox = configuration.OverlayOffsetX;
        int ooy = configuration.OverlayOffsetY;
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Offset");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("X##OverlayOffsetX", ref oox, 1, 10);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("Y##OverlayOffsetY", ref ooy, 1, 10);
        configuration.OverlayOffsetX = oox;
        configuration.OverlayOffsetY = ooy;

        int pdx = configuration.OverlayPaddingX;
        int pdy = configuration.OverlayPaddingY;
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Padding");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("X##OverlayPaddingX", ref pdx, 1, 10);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("Y##OverlayPadding", ref pdy, 1, 10);
        configuration.OverlayPaddingX = pdx;
        configuration.OverlayPaddingY = pdy;

        ImGui.EndTable();

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

        // Advanced icon config
        ImGui.Separator();
        if (ImGui.TreeNode("Advanced Icon Configuration"))
        {
            var imagesize = new Vector2(20, 20);

            if (!armIconReset)
            {
                if (ImGui.Button("Reset icons to Default"))
                    armIconReset = true;
            } else {
                ImGui.Text("Are you Sure?");
                ImGui.SameLine();
                if (ImGui.Button("Reset"))
                {
                    plugin.Configuration.iconConfig = new StatusIconConfig();
                    armIconReset = false;
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                    armIconReset = false;
            }

            ImGui.BeginTable("iconconfig", 6);

            ImGui.TableNextColumn();
            ImGui.TableHeader("Name");
            ImGui.TableNextColumn();
            ImGui.TableHeader(""); // Explaination
            ImGui.TableNextColumn();
            ImGui.TableHeader("Icon");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Label");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Show");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Extra Options");

            // Mitigation
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Mitigation");

                ImGui.TableNextColumn();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Effects that reduce incoming damage");
                    ImGui.EndTooltip();
                }

                ImGui.TableNextColumn();
                if (!plugin.Configuration.iconConfig.alwaysSplitMit)
                    ImGui.Image(plugin.textures["mit_all.png"].ImGuiHandle, imagesize);
                ImGui.Image(plugin.textures["mit_phys.png"].ImGuiHandle, imagesize);
                ImGui.Image(plugin.textures["mit_magi.png"].ImGuiHandle, imagesize);

                ImGui.TableNextColumn();
                if (!plugin.Configuration.iconConfig.alwaysSplitMit)
                    ImGui.Text("Mitigation");
                ImGui.Text("Physical Mit");
                ImGui.Text("Magical Mit");

                ImGui.TableNextColumn();
                bool enablemit = plugin.Configuration.iconConfig.showMit;
                if (ImGui.Checkbox("##enableMit", ref enablemit))
                    plugin.Configuration.iconConfig.showMit = enablemit;

                ImGui.TableNextColumn();
                bool alwaysstackmit = plugin.Configuration.iconConfig.alwaysSplitMit;
                if (ImGui.Checkbox("Always Split Mit", ref alwaysstackmit))
                    plugin.Configuration.iconConfig.alwaysSplitMit = alwaysstackmit;
            }

            // Damage Up
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Damage Up");

                ImGui.TableNextColumn();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Effects that increase outgoing damage");
                    ImGui.EndTooltip();
                }

                ImGui.TableNextColumn();
                if (!plugin.Configuration.iconConfig.alwaysSplitDmgUp)
                    ImGui.Image(plugin.textures["all_up.png"].ImGuiHandle, imagesize);
                ImGui.Image(plugin.textures["phys_up.png"].ImGuiHandle, imagesize);
                ImGui.Image(plugin.textures["magi_up.png"].ImGuiHandle, imagesize);

                ImGui.TableNextColumn();
                if (!plugin.Configuration.iconConfig.alwaysSplitDmgUp)
                    ImGui.Text("Damage Up");
                ImGui.Text("Phyiscal Dmg Up");
                ImGui.Text("Magical Dmg Up");

                ImGui.TableNextColumn();
                bool enabledmg = plugin.Configuration.iconConfig.showDmgUp;
                if (ImGui.Checkbox("##enableDmgUp", ref enabledmg))
                    plugin.Configuration.iconConfig.showDmgUp = enabledmg;

                ImGui.TableNextColumn();
                bool alwaysplitdmg = plugin.Configuration.iconConfig.alwaysSplitDmgUp;
                if (ImGui.Checkbox("Always Split Damage Up", ref alwaysplitdmg))
                    plugin.Configuration.iconConfig.alwaysSplitDmgUp = alwaysplitdmg;
            }

            // Speed Ups
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Speed Up");

                ImGui.TableNextColumn();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Effects that increase the speed of some abilities");
                    ImGui.EndTooltip();
                }

                ImGui.TableNextColumn();
                if (plugin.Configuration.iconConfig.stackSpeedUps)
                    ImGui.Image(plugin.textures["attack_speed_up.png"].ImGuiHandle, imagesize);
                else
                {
                    ImGui.Image(plugin.textures["attack_speed_up.png"].ImGuiHandle, imagesize);
                    ImGui.Image(plugin.textures["cast_speed_up.png"].ImGuiHandle, imagesize);
                    ImGui.Image(plugin.textures["auto_speed_up.png"].ImGuiHandle, imagesize);
                }

                ImGui.TableNextColumn();
                if (plugin.Configuration.iconConfig.stackSpeedUps)
                    ImGui.Text("Speed Up");
                else
                {
                    ImGui.Text("Attack Speed Up");
                    ImGui.Text("Cast Speed Up");
                    ImGui.Text("Auto Speed Up");
                }

                ImGui.TableNextColumn();
                bool enablespdup = plugin.Configuration.iconConfig.showSpeedUps;
                if (ImGui.Checkbox("##enableSpeedUp", ref enablespdup))
                    plugin.Configuration.iconConfig.showSpeedUps = enablespdup;

                ImGui.TableNextColumn();
                bool stackspdup = plugin.Configuration.iconConfig.stackSpeedUps;
                if (ImGui.Checkbox("Stack Speed Ups", ref stackspdup))
                    plugin.Configuration.iconConfig.stackSpeedUps = stackspdup;
            }

            // Heal Ups
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Healing Up");

                ImGui.TableNextColumn();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Effects that increase the healing given or recived");
                    ImGui.EndTooltip();
                }

                ImGui.TableNextColumn();

                ImGui.Image(plugin.textures["healing_up.png"].ImGuiHandle, imagesize);
                ImGui.Image(plugin.textures["healing_pot.png"].ImGuiHandle, imagesize);

                ImGui.TableNextColumn();
                ImGui.Text("Healing Up");
                ImGui.Text("Heal Potency Up");

                ImGui.TableNextColumn();
                bool enablehealup = plugin.Configuration.iconConfig.showHealUps;
                if (ImGui.Checkbox("##enableHealUp", ref enablehealup))
                    plugin.Configuration.iconConfig.showHealUps = enablehealup;
            }

            // DH/Crit
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Crit/Direct Hit");

                ImGui.TableNextColumn();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Effects that increase chances of crits or direct hits");
                    ImGui.EndTooltip();
                }

                ImGui.TableNextColumn();

                ImGui.Image(plugin.textures["crit_rate_up.png"].ImGuiHandle, imagesize);
                ImGui.Image(plugin.textures["dh_rate_up.png"].ImGuiHandle, imagesize);

                ImGui.TableNextColumn();
                ImGui.Text("Crit rate Up");
                ImGui.Text("Dhit rate Up");

                ImGui.TableNextColumn();
                bool enablecritdh = plugin.Configuration.iconConfig.showCritDH;
                if (ImGui.Checkbox("##enableCritDH", ref enablecritdh))
                    plugin.Configuration.iconConfig.showCritDH = enablecritdh;
            }

            // Special Effects
            foreach (var effect in configuration.iconConfig.SpecialIcons.Keys)
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
                ImGui.Image(plugin.textures[icon.FileName].ImGuiHandle, imagesize);

                // Label
                ImGui.TableNextColumn();
                string temp2 = icon.Label!;
                ImGui.Text(temp2);

                // Enabled
                ImGui.TableNextColumn();
                bool enableEffect = !configuration.iconConfig.hiddenSpecialEffects.Contains(effect);
                if (ImGui.Checkbox("##enableEffect{0}".Format(effect), ref enableEffect))
                {
                    if (enableEffect)
                        configuration.iconConfig.hiddenSpecialEffects.Remove(effect);
                    else
                        configuration.iconConfig.hiddenSpecialEffects.Add(effect);
                }
            }

            ImGui.EndTable();
        }

        configuration.Save();

    }
}
