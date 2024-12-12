using System;
using System.Collections;
using System.Collections.Generic;
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
        if (ImGui.Checkbox("Enable Overlay", ref enable))
            configuration.EnableOverlay = enable;

        bool enablefloat = configuration.enableFloatText;
        if (ImGui.Checkbox("Enable Float Text", ref enablefloat))
            configuration.enableFloatText = enablefloat;


        bool hic = configuration.hideOutOfCombat;
        if (ImGui.Checkbox("Hide when not in combat", ref hic))
            configuration.hideOutOfCombat = hic;

        bool asid = configuration.alwaysShowInDuty;
        if (ImGui.Checkbox("Always show when in Duty", ref asid))
            configuration.alwaysShowInDuty = asid;

        bool consts = configuration.showConstSelfs;
        if (ImGui.Checkbox("Show effects that should always be up", ref consts))
            configuration.showConstSelfs = consts;
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text("This includes buffs that should always be refreshed, e.g. SAM's buffs from doing their combo");
            ImGui.Text("This explicitly doesn't include tank stances, DNC Dance Partner, and SGE's Kardion");
            ImGui.Text("The intent is to make it clearer when an ability is used as part of a burst phase rather than just as part of the rotation");
            ImGui.EndTooltip();
        }

        bool ses = configuration.showEssenceSelfs;
        if (ImGui.Checkbox("Show Save the Queen Essences", ref ses))
            configuration.showEssenceSelfs = ses;

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
        if (ImGui.TreeNode("Icon Configuration"))
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
                bool enablemit = plugin.Configuration.iconConfig.showMit;

                if (IconConfigRow(
                    name: "Mitigation",
                    icon_paths: plugin.Configuration.iconConfig.alwaysSplitMit ?
                        new string[] { "mit_phys.png", "mit_magi.png" } :
                        new string[] { "mit_all.png", "mit_phys.png", "mit_magi.png" },
                    labels: plugin.Configuration.iconConfig.alwaysSplitMit ?
                        new string[] { "Phyiscal Dmg Up", "Magical Dmg Up" } :
                        new string[] { "Mitigation", "Physical Mit", "Magical Mit" },
                    tooltip: "Effects that reduce incoming damage",
                    outp: ref enablemit
                    ))
                    plugin.Configuration.iconConfig.showMit = enablemit;

                ImGui.TableNextColumn();
                bool alwaysplitmit = plugin.Configuration.iconConfig.alwaysSplitMit;
                if (ImGui.Checkbox("Always Split Mit", ref alwaysplitmit))
                    plugin.Configuration.iconConfig.alwaysSplitMit = alwaysplitmit;
            }

            // Damage Up
            {
                bool enabledmg = plugin.Configuration.iconConfig.showDmgUp;

                if (IconConfigRow(
                    name: "Damage Up",
                    icon_paths: plugin.Configuration.iconConfig.alwaysSplitDmgUp ?
                        new string[] { "phys_up.png", "magi_up.png" } :
                        new string[] { "all_up.png", "phys_up.png", "magi_up.png" },
                    labels: plugin.Configuration.iconConfig.alwaysSplitDmgUp ?
                        new string[] { "Phyiscal Dmg Up", "Magical Dmg Up" } :
                        new string[] { "Damage Up", "Phyiscal Dmg Up", "Magical Dmg Up" },
                    tooltip: "Effects that increase outgoing damage",
                    outp: ref enabledmg
                    ))
                    plugin.Configuration.iconConfig.showDmgUp = enabledmg;

                ImGui.TableNextColumn();
                bool alwaysplitdmg = plugin.Configuration.iconConfig.alwaysSplitDmgUp;
                if (ImGui.Checkbox("Always Split Dmg", ref alwaysplitdmg))
                    plugin.Configuration.iconConfig.alwaysSplitDmgUp = alwaysplitdmg;
            }

            // Speed Ups
            {
                bool enablespdup = plugin.Configuration.iconConfig.showSpeedUps;

                if (IconConfigRow(
                    name: "Speed Up",
                    icon_paths: plugin.Configuration.iconConfig.stackSpeedUps ?
                        new string[] { "attack_speed_up.png" } :
                        new string[] { "attack_speed_up.png", "cast_speed_up.png", "auto_speed_up.png" },
                    labels: plugin.Configuration.iconConfig.stackSpeedUps ?
                        new string[] { "Speed Up" } :
                        new string[] { "Attack Speed Up", "Cast Speed Up", "Auto Speed Up" },
                    tooltip: "Effects that increase the speed of some abilities",
                    outp: ref enablespdup
                    ))
                    plugin.Configuration.iconConfig.showSpeedUps = enablespdup;

                ImGui.TableNextColumn();
                bool stackspdup = plugin.Configuration.iconConfig.stackSpeedUps;
                if (ImGui.Checkbox("Stack Speed Ups", ref stackspdup))
                    plugin.Configuration.iconConfig.stackSpeedUps = stackspdup;
            }

            // Heal Ups
            {
                bool enablehealup = plugin.Configuration.iconConfig.showHealUps;

                if (IconConfigRow(
                    name: "Healing Up",
                    icon_paths: new string[] { "healing_up.png", "healing_pot.png" },
                    labels: new string[] { "Healing Up", "Heal Potency Up" },
                    tooltip: "Effects that increase the healing given or received",
                    outp: ref enablehealup
                    ))
                    plugin.Configuration.iconConfig.showHealUps = enablehealup;
            }

            // DH/Crit
            {
                bool enablecritdh = plugin.Configuration.iconConfig.showCritDH;

                if (IconConfigRow(
                    name: "Crit/Direct Hit",
                    icon_paths: new string[] { "crit_rate_up.png", "dh_rate_up.png" },
                    labels: new string[] { "Crit rate Up", "Dhit rate Up" },
                    tooltip: "Effects that increase chances of blocking physical attacks",
                    outp: ref enablecritdh
                    ))
                    plugin.Configuration.iconConfig.showCritDH = enablecritdh;
            }

            // Block Rate
            {
                bool enableblockup = plugin.Configuration.iconConfig.showBlockUp;

                if (IconConfigRow(
                    name: "Block Rate",
                    icon_path: "block_rate.png",
                    label: "Block Rate",
                    tooltip: "Effects that increase chances of blocking physical attacks",
                    outp: ref enableblockup
                    ))
                    plugin.Configuration.iconConfig.showBlockUp = enableblockup;
            }

            // Move Speed Up
            {
                bool showMoveSpeed = plugin.Configuration.iconConfig.showMoveSpeed;

                if (IconConfigRow(
                    name: "Move Speed Up",
                    icon_path: "speed_up.png",
                    label: "Move Speed",
                    tooltip: "Effects that increase your move speed",
                    outp: ref showMoveSpeed
                    ))
                    plugin.Configuration.iconConfig.showMoveSpeed = showMoveSpeed;
            }

            // Max HP up
            {
                bool showHPup = plugin.Configuration.iconConfig.showHPup;

                if (IconConfigRow(
                    name: "Max HP Up",
                    icon_path: "hp_up.png",
                    label: "Max HP up",
                    tooltip: "Effects that increase your maximum HP",
                    outp: ref showHPup
                    ))
                    plugin.Configuration.iconConfig.showHPup = showHPup;
            }

            // Max MP up
            {
                bool showMPup = plugin.Configuration.iconConfig.showMPup;

                if (IconConfigRow(
                    name: "Max MP Up",
                    icon_path: "mp_up.png",
                    label: "Max MP Up",
                    tooltip: "Effects that increase your maximum MP",
                    outp: ref showMPup
                    ))
                    plugin.Configuration.iconConfig.showMPup = showMPup;
            }

            // Special Effects
            foreach (Utils.BoolEffect effect in configuration.iconConfig.SpecialIcons.Keys)
            {
                StatusIcon icon = configuration.iconConfig.SpecialIcons[effect];

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
                ImGui.Text(icon.Label ?? "[label missing]");

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

    private bool IconConfigRow(string name, string icon_path, string? tooltip, string? label, ref bool outp)
    {
        return IconConfigRow(name, new string[1] { icon_path }, tooltip, new string?[1] { label }, ref outp);
    }

    private bool IconConfigRow(string name, string[] icon_paths, string? tooltip, string?[] labels, ref bool outp)
    {
        var imagesize = new Vector2(20, 20);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(name);

        ImGui.TableNextColumn();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(tooltip ?? "[tooltip missing]");
            ImGui.EndTooltip();
        }

        ImGui.TableNextColumn();
        foreach (var icon_path in icon_paths)
            ImGui.Image(plugin.textures[icon_path].ImGuiHandle, imagesize);

        ImGui.TableNextColumn();
        foreach (var label in labels)
            ImGui.Text(label ?? "[label missing]");

        ImGui.TableNextColumn();
        return ImGui.Checkbox("##IconConfigRow_{0}".Format(name), ref outp);
    }

    private bool IconConfigRow(Utils.BoolEffect effect, StatusIcon icon, ref bool outp)
    {
        return IconConfigRow(effect.ToString(), icon.FileName, icon.Tooltip, icon.Label, ref outp);
    }

    private bool IconConfigExtraOpt(string label, ref bool outp)
    {
        ImGui.TableNextColumn();
        return ImGui.Checkbox(label, ref outp);
    }
}
