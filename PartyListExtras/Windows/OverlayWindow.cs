using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace PartyListExtras.Windows;

public class OverlayWindow : IDisposable
{
    // This used to use the window manager hence the setup
    // As I kinda like it this way

    private Plugin plugin;

    // Scales all drawn things
    public float scaling;

    // Used to ensure we don't duplicate the debug status info message
    public List<Tuple<string, string>> missing_ids = new List<Tuple<string, string>>();

    public Vector2 Size;
    public Vector2 Position;

    public unsafe OverlayWindow(Plugin plugin)
    {
        this.Size = new Vector2(10000, 10000);
        this.Position = new Vector2(0, 0);

        this.plugin = plugin;
    }

    public void Dispose() { }

    public unsafe void Draw()
    {

        ImGui.Begin("PLX_OverlayWindow",
            ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground |
            ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings
            );
        ImGui.SetWindowPos(this.Position);

        // If any of this goes wrong we just skip drawing the window
        HudPartyMember* partyMemberList;
        short count;
        AddonPartyList* apl;
        AtkResNode apl_node;
        try
        {
            AgentHUD* pl = Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentHUD();
            partyMemberList = (HudPartyMember*)pl->PartyMemberList;
            count = pl->PartyMemberCount;
            apl = (AddonPartyList*)plugin.GameGui.GetAddonByName("_PartyList");
            this.scaling = apl->AtkUnitBase.Scale;
            apl_node = apl->BackgroundNineGridNode->AtkResNode;
        }
        catch (NullReferenceException e) {
            plugin.log.Verbose("Failure during start of OverlayWindow.Draw - skipping. Message: " + e.Message);
            return;
        }
        // resize window
        // Set width and height so window is to the left of the party list
        //var width = 300;
        //this.Size = new Vector2(width, apl_node.Height);
        //this.Position = new Vector2(apl_node.ScreenX - width - 10, apl_node.ScreenY);

        this.Size = ImGui.GetMainViewport().Size;

        // For each player in the party...
        for (var i = 0; i < count; i++)
        {
            // Pull the status list of the party member
            PlayerCharacter pc; BattleNpc bc;
            StatusList sl;
            var result = plugin.ObjectTable.SearchById(partyMemberList[i].ObjectId);
            if (result?.GetType() == typeof(PlayerCharacter))
            {
                pc = (PlayerCharacter)result;
                sl = pc.StatusList;
            }
            else if (result?.GetType() == typeof(BattleNpc))
            {
                bc = (BattleNpc)result;
                sl = bc.StatusList;
            }
            else
            {
                //if (result == null) PluginLog.Warning("Party List member null");
                //else PluginLog.Warning("Unexpected member of party list: {0}", result.GetType().ToString());
                continue;
            };

            // Get ResNode of the JobIcon - everything is drawn relative to the job icon
            var icon = apl->PartyMember[i].ClassJobIcon;
            if (icon == null) { continue; }
            var node = icon->AtkResNode;
            // TODO: allow repositioning and resizing of the UI element
            var curpos = new Vector2(
                node.ScreenX - ((plugin.Configuration.OverlayWidth * scaling) + plugin.Configuration.OverlayOffsetX),
                node.ScreenY + plugin.Configuration.OverlayOffsetY);
            var cursize = new Vector2(plugin.Configuration.OverlayWidth * scaling, node.Height * scaling);

            //Spin out to helper functions because long
            DrawStatusIcons(ParseStatusList(sl), "party_{0}".Format(i), curpos, cursize);
        }

        ImGui.End();
    }

    /// <summary>
    /// Turns a dalamud StatusList object into StatusIcons for easy rendering
    /// </summary>
    /// <param name="sl">The status list as found in a BattleChara object</param>
    /// <returns>A list of parsed status icons</returns>
    private List<StatusIcon> ParseStatusList(StatusList sl)
    {
        var output = new List<StatusIcon>();

        var debug = new List<Tuple<string, string>>();

        List<StatusEffectData> datas = new List<StatusEffectData>();

        // Gather up status data
        foreach (var status in sl) {
            if (status == null) continue;
            if (plugin.statusEffectData.ContainsKey((int)status.GameData.RowId)) {
                datas.Add(plugin.statusEffectData[(int)status.GameData.RowId]);
            } else {
                debug.Add(new Tuple<string, string>(status.GameData.Name.ToString(), status.GameData.RowId.ToString()));
            }
        }

        // Filters
        // ConstSelf
        if (!plugin.Configuration.showConstSelfs)
            datas = datas.Where(x => x.target_type != TargetType.ConstSelf).ToList();

        // StQ essences
        if (!plugin.Configuration.showEssenceSelfs)
            datas = datas.Where(x => x.target_type != TargetType.EssenceSelf).ToList();

        var special_fstrings = plugin.Configuration.iconConfig.SpecialIcons;
        // For each non-null special effect...
        foreach (var sfx in datas.Select(x => x.special).OfType<SpecialEffects>()) {
            // ...If the special effect is in the special_fstrings list...
            if (special_fstrings.ContainsKey(sfx) && !plugin.Configuration.iconConfig.hiddenSpecialEffects.Contains(sfx))
            {
                // ...add it to the list of things to be rendered
                output.Add(special_fstrings[sfx]);
            }
        }

        if (plugin.Configuration.iconConfig.showMit)
        {
            // Mitigation
            var phys_mit = multi_sum(datas.Select(x => x.phys_mit));
            var magi_mit = multi_sum(datas.Select(x => x.magi_mit));

            if (phys_mit == magi_mit && phys_mit > 0 && !plugin.Configuration.iconConfig.alwaysSplitMit)
                output.Add(new StatusIcon { FileName = "mit_all.png", Value = "{0}%".Format(phys_mit), Label = "Mitigation" });
            else
            {
                if (phys_mit > 0)
                    output.Add(new StatusIcon { FileName = "mit_phys.png", Value = "{0}%".Format(phys_mit), Label = "Physical Mit" });
                if (magi_mit > 0)
                    output.Add(new StatusIcon { FileName = "mit_magi.png", Value = "{0}%".Format(magi_mit), Label = "Magical Mit" });
            }
        }

        if (plugin.Configuration.iconConfig.showDmgUp)
        {
            // Damage Up
            var phys_up = multi_sum(datas.Select(x => x.phys_up));
            var magi_up = multi_sum(datas.Select(x => x.magi_up));

            if (phys_up == magi_up && phys_up > 0 && !plugin.Configuration.iconConfig.alwaysSplitDmgUp)
                output.Add(new StatusIcon { FileName = "all_up.png", Value = "{0}%".Format(phys_up), Label = "Damage Up" });
            else
            {
                if (phys_up > 0)
                    output.Add(new StatusIcon { FileName = "phys_up.png", Value = "{0}%".Format(phys_up), Label = "Phyiscal Dmg Up" });
                if (magi_up > 0)
                    output.Add(new StatusIcon { FileName = "magi_up.png", Value = "{0}%".Format(magi_up), Label = "Magical Dmg Up" });
            }
        }

        if (plugin.Configuration.iconConfig.showSpeedUps)
        {
            // Attack Speed
            if (plugin.Configuration.iconConfig.stackSpeedUps)
            {
                // This bit is to make sure we don't take multiple speed ups from one buff
                List<float?> spdups = new List<float?>();
                foreach (StatusEffectData data in datas) {
                    if (data.attack_speed_up.HasValue)
                        spdups.Add(data.attack_speed_up.Value);

                    else if (data.ability_cast_speed_up.HasValue)
                        spdups.Add(data.ability_cast_speed_up.Value);
                    
                    else if (data.cast_speed_up.HasValue)
                        spdups.Add(data.cast_speed_up.Value);

                    else if (data.auto_speed_up.HasValue)
                        spdups.Add(data.auto_speed_up.Value);
                }
                var speed_up = multi_sum(spdups);
                if (speed_up > 0)
                    output.Add(new StatusIcon { FileName = "attack_speed_up.png", Value = "{0}%".Format(speed_up), Label = "Speed Up" });
            }
            else
            {
                var attack_speed_up = multi_sum(datas.Select(x => x.attack_speed_up).Union(datas.Select(x => x.ability_cast_speed_up)));
                var cast_speed_up = multi_sum(datas.Select(x => x.cast_speed_up));
                var auto_speed_up = multi_sum(datas.Select(x => x.auto_speed_up));
                if (attack_speed_up > 0)
                    output.Add(new StatusIcon { FileName = "attack_speed_up.png", Value = "{0}%".Format(attack_speed_up), Label = "Attack Speed Up" });
                if (cast_speed_up > 0)
                    output.Add(new StatusIcon { FileName = "cast_speed_up.png", Value = "{0}%".Format(cast_speed_up), Label = "Cast Speed Up" });
                if (auto_speed_up > 0)
                    output.Add(new StatusIcon { FileName = "auto_speed_up.png", Value = "{0}%".Format(auto_speed_up), Label = "Auto Speed Up" });
            }
        }

        // Heal Rate
        if (plugin.Configuration.iconConfig.showHealUps)
        {
            var healing_up = multi_sum(datas.Select(x => x.healing_up));
            var healing_pot = multi_sum(datas.Select(x => x.healing_pot));

            if (healing_up > 0)
                output.Add(new StatusIcon { FileName = "healing_up.png", Value = "{0}%".Format(healing_up), Label = "Healing Up" });
            if (healing_pot > 0)
                output.Add(new StatusIcon { FileName = "healing_pot.png", Value = "{0}%".Format(healing_pot), Label = "Heal Potency Up" });
        }

        // Crit and DH
        if (plugin.Configuration.iconConfig.showCritDH)
        {
            var crit_up = multi_sum(datas.Select(x => x.crit_rate_up));
            var dh_up = multi_sum(datas.Select(x => x.dhit_rate_up));

            if (crit_up > 0)
                output.Add(new StatusIcon { FileName = "crit_rate_up.png", Value = "{0}%".Format(crit_up), Label = "Crit rate Up" });
            if (dh_up > 0)
                output.Add(new StatusIcon { FileName = "dh_rate_up.png", Value = "{0}%".Format(dh_up), Label = "Dhit rate Up" });
        }

        // Block Rate
        if (plugin.Configuration.iconConfig.showBlockUp)
        {
            var block_rate = sum(datas.Select(x => x.block_rate));

            if (block_rate > 0)
            {
                // Extra special "dont go over 100" maths
                block_rate += 20;
                block_rate = Math.Min(block_rate, 100);
                output.Add(new StatusIcon { FileName = "block_rate.png", Value = "{0}%".Format(block_rate), Label = "Block Rate" });
            }
        }

        // Move Speed
        if (plugin.Configuration.iconConfig.showMoveSpeed)
        {
            var movspeed = max(datas.Select(x => x.move_speed_up));

            if (movspeed > 0)
                output.Add(new StatusIcon { FileName = "speed_up.png", Value = "{0}%".Format(movspeed), Label = "Move Speed" });
        }

        // Max HP
        if (plugin.Configuration.iconConfig.showHPup)
        {
            var hp_up = sum(datas.Select(x => x.max_hp_up));

            if (hp_up > 0)
                output.Add(new StatusIcon { FileName = "max_hp.png", Value = "{0}%".Format(hp_up), Label = "Max HP Up" });
        }

        // Max MP
        if (plugin.Configuration.iconConfig.showMPup)
        {
            var mp_up = sum(datas.Select(x => x.max_mp_up));

            if (mp_up > 0)
                output.Add(new StatusIcon { FileName = "max_mp.png", Value = "{0}%".Format(mp_up), Label = "Max MP Up" });
        }

        // Send Message to log for status effects that are missing
        var debugMessage = "";
        foreach (var item in debug)
        {
            if (!missing_ids.Contains(item))
            {
                debugMessage += "{0} = {1}; ".Format(item.Item1, item.Item2);
                missing_ids.Add(item);
            }
        }
        if (debugMessage.Length > 0) plugin.log.Debug("Missing Status Ids: " + debugMessage);

        return output;
    }

    private void DrawOverlayBackground(Vector2 position, Vector2 size)
    {
        // Draw Single Colour Background
        var singlecol = ImGui.ColorConvertFloat4ToU32(plugin.Configuration.colorSingle);
        ImGui.GetBackgroundDrawList().AddRectFilled(position, (position + size), singlecol);
    }

    private void DrawOverlayBackgroundGradient(Vector2 position, Vector2 size)
    {
        // Draw Gradient Background
        var leftcol = ImGui.ColorConvertFloat4ToU32(plugin.Configuration.colorLeft);
        var rightcol = ImGui.ColorConvertFloat4ToU32(plugin.Configuration.colorRight);
        ImGui.GetBackgroundDrawList().AddRectFilledMultiColor(position, (position + size), leftcol, rightcol, rightcol, leftcol);
    }


    // Draw Advanced background imitating the native UI
    // Abandoned due to ImGui Weirdness
    //private void DrawOverlayBackgroundAdv(Vector2 position, Vector2 size)
    //{
        //uint leftcol = ImGui.ColorConvertFloat4ToU32(plugin.Configuration.colorLeft);
        //uint rightcol = ImGui.ColorConvertFloat4ToU32(plugin.Configuration.colorRight);

        //var drawlist = ImGui.GetBackgroundDrawList();

        //float[] xs = [0, (float)(size.X * 0.4), (float)(size.X * 0.95), (float)(size.X)];
        //float[] ys = [0, (float)(size.Y * 0.2), (float)(size.Y * 0.8), (float)(size.Y)];

        //uint[,] colours = new uint[4,4]
        //{
        //    { leftcol, leftcol, leftcol, leftcol },
        //    { leftcol, rightcol, rightcol, leftcol },
        //    { leftcol, rightcol, rightcol, leftcol },
        //    { leftcol, leftcol, leftcol, leftcol }
        //};

        //for (var y = 0; y < ys.Length - 1; y++)
        //{
        //    for (var x = 0; x < xs.Length - 1; x++)
        //    {
        //        drawlist.AddRectFilledMultiColor(
        //            position + new Vector2(xs[x], ys[y]),
        //            position + new Vector2(xs[x+1], ys[y+1]),
        //            colours[y, x],
        //            colours[y, x+1],
        //            colours[y+1, x+1],
        //            colours[y+1, x]
        //        );
        //    }
        //}
    //}

    /// <summary>
    /// Draw a formatted list of status icons
    /// Depends on config settings
    /// </summary>
    /// <param name="icons">list of icons to render</param>
    /// <param name="windowId">unique number to prevent overlapping windows</param>
    /// <param name="position">position of the subwindow</param>
    /// <param name="size">size of the subwindow</param>
    private void DrawStatusIcons(List<StatusIcon> icons, string windowId, Vector2 position, Vector2 size)
    {
        // Start child window to make the cursor work "nicer"
        //ImGui.BeginChild("PLX_StatusIconList_{0}".Format(windowId), size);
        ImGui.SetWindowPos(position);

        var width = size.X;
        var paddingX = plugin.Configuration.OverlayPaddingX;
        var paddingY = plugin.Configuration.OverlayPaddingY;
        var cursor = position + new Vector2(width, paddingY);

        // shorten the display mode
        var dm = plugin.Configuration.DisplayMode;

        // Set some constants
        var imgsize = size.Y - (paddingY * 2f);
        var white = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1));
        var drawlist = ImGui.GetBackgroundDrawList();

        // Draw Background
        if (plugin.Configuration.doGradientBackground)
        {
            DrawOverlayBackgroundGradient(position, size);
        }
        else
        {
            DrawOverlayBackground(position, size);
        }

        // Draw icons, We go RTL so subtracting the X offset
        // TODO: the widths are mostly guesses here, possible to break with scaling
        foreach (var icon in icons)
        {

            // the label e.g. damage up
            if (icon.Label != null && (dm == 0 || (dm == 1 && icon.Value == null)))
            {
                cursor += new Vector2(
                    -(1f * ImGui.CalcTextSize(icon.Label).X) + (-paddingX * scaling),
                    0
                );
                drawlist.AddText(cursor, white, icon.Label);
            }

            // The icon itself
            cursor += new Vector2(
                -(1f * imgsize) - paddingX,
                0
            );
            drawlist.AddImage(plugin.textures[icon.FileName].ImGuiHandle, cursor, cursor + new Vector2(imgsize, imgsize));

            // Info, e.g. mit percent
            if (icon.Value != null && dm != 3) {
                cursor += new Vector2(
                    -(1f * ImGui.CalcTextSize(icon.Value).X * scaling) - paddingX,
                    0
                );
                drawlist.AddText(cursor, white, icon.Value);
            }
        }

        //ImGui.EndChild();
    }

    internal bool one_true(IEnumerable<bool?> values)
    {
        // filter out null values then aggregate using or
        return values.Where(x => x != null).Aggregate(false, (a, b) => a || b.Value);
    }

    internal float sum(IEnumerable<float?> values)
    {
        // filter out null values then sum
        float x = values.Where(x => x != null).Sum() ?? 0;
        return to_percent(x);
    }

    internal float max(IEnumerable<float?> values)
    {
        // filter out null values then sum
        float x = values.Where(x => x != null).Max() ?? 0;
        return to_percent(x);
    }

    internal float multi_sum(IEnumerable<float?> values)
    {
        // filter out null values then do the multiplicative application
        var x = values.Where(x => x != null).Select(x => 1 - x).Aggregate(1f, (a, b) => a * b.Value);
        return to_percent(1f - x);
    }

    internal int to_percent(float value)
    {
        return (int)Math.Round(value * 100, 0);
    }
}
