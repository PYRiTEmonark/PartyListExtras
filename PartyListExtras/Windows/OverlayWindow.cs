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
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using static PartyListExtras.Utils;

namespace PartyListExtras.Windows;

public class OverlayWindow : IDisposable
{
    // This used to use the window manager hence the setup
    // As I kinda like it this way

    private Plugin plugin;

    // Scales all drawn things
    public float partylistscaling;
    public float enemylistscaling;

    // Used to ensure we don't duplicate the debug status info message
    public List<Tuple<string, string>> missing_ids = new List<Tuple<string, string>>();

    // Size and position of overlay
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

        // Party info for getting battlecharas
        HudPartyMember* partyMemberList;
        short partycount;

        // Enemy info
        int enemycount;
        List<uint> enemyIds = new List<uint>();

        // the ATK lists
        AddonPartyList* apl;
        AddonEnemyList* ael;

        // Used to position our UI
        AtkResNode apl_node;
        AtkResNode ael_node;

        // If any of this goes wrong we just skip drawing the window
        try
        {
            // PARTY INFO
            AgentHUD* pl = Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentHUD();
            partyMemberList = (HudPartyMember*)pl->PartyMemberList;
            partycount = pl->PartyMemberCount;

            // PARTY LIST ATK
            apl = (AddonPartyList*)plugin.GameGui.GetAddonByName("_PartyList");
            this.partylistscaling = apl->AtkUnitBase.Scale;
            apl_node = apl->BackgroundNineGridNode->AtkResNode;

            // ENEMY INFO
            var numArray = Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->
                AtkModule.AtkArrayDataHolder.NumberArrays[21];

            // dummied to single enemy
            var enemyObjectId = numArray->IntArray[8 + (0 * 5)];
            enemyIds.Add((uint)enemyObjectId);
            enemycount = 1;

            // ENEMY LIST ATK
            ael = (AddonEnemyList*)plugin.GameGui.GetAddonByName("_EnemyList");
            this.enemylistscaling = ael->AtkUnitBase.Scale;
            ael_node = apl->BackgroundNineGridNode->AtkResNode;
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
        for (var i = 0; i < partycount; i++)
        {
            // Pull the status list of the party member
            BattleChara? battleChara;
            StatusList sl;
            var result = plugin.ObjectTable.SearchById(partyMemberList[i].ObjectId);
            if (!TryGetPartyMemberBattleChara(result, out battleChara))
                continue;

            sl = battleChara!.StatusList;

            // Get ResNode of the JobIcon - everything is drawn relative to the job icon
            var icon = apl->PartyMember[i].ClassJobIcon;
            if (icon == null) { continue; }
            var node = icon->AtkResNode;

            // Final position and size
            var curpos = new Vector2(
                node.ScreenX - ((plugin.Configuration.OverlayWidth * partylistscaling) + plugin.Configuration.OverlayOffsetX),
                node.ScreenY + plugin.Configuration.OverlayOffsetY);
            var cursize = new Vector2(plugin.Configuration.OverlayWidth * partylistscaling, node.Height * partylistscaling);

            // Variables for checking status effects
            var tla = battleChara.ClassJob.GameData!.Abbreviation.ToString();
            Enum.TryParse<Utils.Job>(tla, out var job);

            CondVars cvars = new CondVars()
            {
                targetLevel = battleChara.Level,
                targetJob = job,
            };

            List<uint> statusIds = sl.ToList().Select(x => x.StatusId).ToList();
            uint charaid = battleChara.ObjectId;

            var applied = ParseStatusList(sl, cvars);
            var statusIcons = GetStatusIcons(applied);

            //Spin out to helper functions because long
            DrawStatusIcons(statusIcons, string.Format("party_{0}", i.ToString()), curpos, cursize);
        }

        // For each known enemy...
        for (int i = 0; i < enemycount; i++)
        {
            // Pull the status list of the party member
            BattleChara? battleChara;
            StatusList sl;
            var result = plugin.ObjectTable.SearchById(enemyIds[i]);
            if (!TryGetPartyMemberBattleChara(result, out battleChara))
            {
                plugin.log.Warning("can't find object {0}", enemyIds[i]);
                continue;
            }

            sl = battleChara!.StatusList;

            plugin.log.Warning("{0} a", sl.FirstOrDefault());

            // As with the party list position stuff would go here
            var curpos = new Vector2(10, 10);
            var cursize = new Vector2(100, 20);

            CondVars cvars = new CondVars()
            {
                targetLevel = null,
                targetJob = null,
            };

            List<uint> statusIds = sl.ToList().Select(x => x.StatusId).ToList();
            uint charaid = battleChara.ObjectId;

            var applied = ParseStatusList(sl, cvars);
            plugin.log.Warning("{0}", applied.special ?? new List<BoolEffect>());
            var statusIcons = GetStatusIcons(applied);

            //Spin out to helper functions because long
            DrawStatusIcons(statusIcons, string.Format("enemy_{0}", i.ToString()), curpos, cursize, RTL: false);
        }

        ImGui.End();
    }

    // TODO: Condvars is really a hack. Will fall apart v quick.

    /// <summary>
    /// Parses a Statuslist object and turns it into the applied effects
    /// CondVars are used to calculate the actual values
    /// The plugin configuration is used to filter out some effects
    /// </summary>
    /// <param name="sl"></param>
    /// <param name="cvars"></param>
    /// <returns></returns>
    internal AppliedEffects ParseStatusList(StatusList sl, CondVars cvars)
    {
        var debug = new List<Tuple<string, string>>();

        List<StatusEffectData> sed = new List<StatusEffectData>();

        // Gather up status data
        foreach (var status in sl)
        {
            if (status == null) continue;
            if (plugin.statusEffectData.ContainsKey((int)status.GameData.RowId))
            {
                sed.Add(plugin.statusEffectData[(int)status.GameData.RowId]);
            }
            else
            {
                debug.Add(new Tuple<string, string>(status.GameData.Name.ToString(), status.GameData.RowId.ToString()));
            }
        }

        // Send Message to log for status effects that are missing
        var debugMessage = "";
        foreach (var item in debug)
        {
            if (!missing_ids.Contains(item))
            {
                debugMessage += string.Format("{0} = {1}; ", item.Item1, item.Item2);
                missing_ids.Add(item);
            }
        }
        if (debugMessage.Length > 0) plugin.log.Debug("Missing Status Ids: " + debugMessage);

        // Filters
        // ConstSelf
        if (!plugin.Configuration.showConstSelfs)
        {
            sed = sed
                .Where(x => x.target_type != TargetType.ConstSelf)
                .Where(x => x.target_type != TargetType.ConstPartyMember)
                .ToList();
        }

        // StQ essences
        if (!plugin.Configuration.showEssenceSelfs)
            sed = sed.Where(x => x.target_type != TargetType.EssenceSelf).ToList();

        // Combine and return a single
        if (sed.Count > 0)
            return sed.Select(x => x.Compute(cvars)).Aggregate((a, b) => a.Combine(b));
        else
            return new AppliedEffects();
    }

    /// <summary>
    /// Turns an appliedeffects object into StatusIcons for easy rendering
    /// Uses the plugin configuration to filter some effects and change render outputs
    /// </summary>
    /// <param name="sl">The status list as found in a BattleChara object</param>
    /// <param name="cvars">Condvars object corresponding to information about that player</param>
    /// <returns>A list of parsed status icons</returns>
    private List<StatusIcon> GetStatusIcons(AppliedEffects applied)
    {
        var output = new List<StatusIcon>();

        var special_fstrings = plugin.Configuration.iconConfig.SpecialIcons;
        // For each special effect...
        foreach (var sfx in applied.special ?? new List<BoolEffect>()) {
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
            var phys_mit = applied.GetEffectOrDefault(FloatEffect.phys_mit);
            var magi_mit = applied.GetEffectOrDefault(FloatEffect.magi_mit);

            if (phys_mit == magi_mit && phys_mit > 0 && !plugin.Configuration.iconConfig.alwaysSplitMit)
                output.Add(new StatusIcon { FileName = "mit_all.png", Value = phys_mit, Label = "Mitigation" });
            else
            {
                if (phys_mit > 0)
                    output.Add(new StatusIcon { FileName = "mit_phys.png", Value = phys_mit, Label = "Physical Mit" });
                if (magi_mit > 0)
                    output.Add(new StatusIcon { FileName = "mit_magi.png", Value = magi_mit, Label = "Magical Mit" });
            }
        }

        if (plugin.Configuration.iconConfig.showDmgUp)
        {
            // Damage Up
            var phys_up = applied.GetEffectOrDefault(FloatEffect.phys_up);
            var magi_up = applied.GetEffectOrDefault(FloatEffect.magi_up);

            if (phys_up == magi_up && phys_up > 0 && !plugin.Configuration.iconConfig.alwaysSplitDmgUp)
                output.Add(new StatusIcon { FileName = "all_up.png", Value = phys_up, Label = "Damage Up" });
            else
            {
                if (phys_up > 0)
                    output.Add(new StatusIcon { FileName = "phys_up.png", Value = phys_up, Label = "Phyiscal Dmg Up" });
                if (magi_up > 0)
                    output.Add(new StatusIcon { FileName = "magi_up.png", Value = magi_up, Label = "Magical Dmg Up" });
            }
        }

        if (plugin.Configuration.iconConfig.showSpeedUps)
        {
            if (plugin.Configuration.iconConfig.stackSpeedUps) {
                var speed_up = applied.GetEffectOrDefault(FloatEffect.cast_speed_up);
                if (speed_up > 0)
                    output.Add(new StatusIcon { FileName = "attack_speed_up.png", Value = speed_up, Label = "Speed Up" });
            }
            else
            {
                var attack_speed_up = multi_sum(
                    applied.GetEffectOrDefault(FloatEffect.attack_speed_up),
                    applied.GetEffectOrDefault(FloatEffect.ability_cast_speed_up));
                var cast_speed_up = applied.GetEffectOrDefault(FloatEffect.cast_speed_up);
                var auto_speed_up = applied.GetEffectOrDefault(FloatEffect.auto_speed_up);
                if (attack_speed_up > 0)
                    output.Add(new StatusIcon { FileName = "attack_speed_up.png", Value = attack_speed_up, Label = "Attack Speed Up" });
                if (cast_speed_up > 0)
                    output.Add(new StatusIcon { FileName = "cast_speed_up.png", Value = cast_speed_up, Label = "Cast Speed Up" });
                if (auto_speed_up > 0)
                    output.Add(new StatusIcon { FileName = "auto_speed_up.png", Value = auto_speed_up, Label = "Auto Speed Up" });
            }
        }

        // Heal Rate
        if (plugin.Configuration.iconConfig.showHealUps)
        {
            var healing_up = applied.GetEffectOrDefault(FloatEffect.healing_up);
            var healing_pot = applied.GetEffectOrDefault(FloatEffect.healing_pot);

            if (healing_up > 0)
                output.Add(new StatusIcon { FileName = "healing_up.png", Value = healing_up, Label = "Healing Up" });
            if (healing_pot > 0)
                output.Add(new StatusIcon { FileName = "healing_pot.png", Value = healing_pot, Label = "Heal Potency Up" });
        }

        // Crit and DH
        if (plugin.Configuration.iconConfig.showCritDH)
        {
            var crit_rate_up = applied.GetEffectOrDefault(FloatEffect.crit_rate_up);
            var dhit_rate_up = applied.GetEffectOrDefault(FloatEffect.dhit_rate_up);

            if (crit_rate_up > 0)
                output.Add(new StatusIcon { FileName = "crit_rate_up.png", Value = crit_rate_up, Label = "Crit rate Up" });
            if (dhit_rate_up > 0)
                output.Add(new StatusIcon { FileName = "dh_rate_up.png", Value = dhit_rate_up, Label = "Dhit rate Up" });
        }

        // Block Rate
        if (plugin.Configuration.iconConfig.showBlockUp)
        {
            var block_rate = applied.GetEffectOrDefault(FloatEffect.block_rate);

            if (block_rate > 0)
            {
                // Extra special "dont go over 100" maths
                block_rate = Math.Min(block_rate, 100);
                output.Add(new StatusIcon { FileName = "block_rate.png", Value = block_rate, Label = "Block Rate" });
            }
        }

        // Move Speed
        if (plugin.Configuration.iconConfig.showMoveSpeed)
        {
            var move_speed_up = applied.GetEffectOrDefault(FloatEffect.move_speed_up);

            if (move_speed_up > 0)
                output.Add(new StatusIcon { FileName = "speed_up.png", Value = move_speed_up, Label = "Move Speed" });
        }

        // Max HP
        if (plugin.Configuration.iconConfig.showHPup)
        {
            var max_hp_up = applied.GetEffectOrDefault(FloatEffect.max_hp_up);

            if (max_hp_up > 0)
                output.Add(new StatusIcon { FileName = "max_hp.png", Value = max_hp_up, Label = "Max HP Up" });
        }

        // Max MP
        if (plugin.Configuration.iconConfig.showMPup)
        {
            var max_mp_up = applied.GetEffectOrDefault(FloatEffect.max_mp_up);

            if (max_mp_up > 0)
                output.Add(new StatusIcon { FileName = "max_mp.png", Value = max_mp_up, Label = "Max MP Up" });
        }

        return output;
    }

    // Solid colour background
    private void DrawOverlayBackground(Vector2 position, Vector2 size)
    {
        // Draw Single Colour Background
        var singlecol = ImGui.ColorConvertFloat4ToU32(plugin.Configuration.colorSingle);
        ImGui.GetBackgroundDrawList().AddRectFilled(position, (position + size), singlecol);
    }
    
    // Gradient background
    private void DrawOverlayBackgroundGradient(Vector2 position, Vector2 size, bool RTL)
    {
        // Draw Gradient Background
        var leftcol = ImGui.ColorConvertFloat4ToU32(plugin.Configuration.colorLeft);
        var rightcol = ImGui.ColorConvertFloat4ToU32(plugin.Configuration.colorRight);
        
        if (RTL) ImGui.GetBackgroundDrawList().AddRectFilledMultiColor(position, (position + size), leftcol, rightcol, rightcol, leftcol);
        else ImGui.GetBackgroundDrawList().AddRectFilledMultiColor(position, (position + size), rightcol, leftcol, leftcol, rightcol);
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
    private void DrawStatusIcons(List<StatusIcon> icons, string windowId, Vector2 position, Vector2 size, bool RTL = true)
    {
        // Start child window to make the cursor work "nicer"
        //ImGui.BeginChild("PLX_StatusIconList_{0}".Format(windowId), size);
        ImGui.SetWindowPos(position);

        var width = size.X;
        var paddingX = plugin.Configuration.OverlayPaddingX;
        if (RTL) paddingX = -paddingX; // Invert padding so we pad to the left
        var paddingY = plugin.Configuration.OverlayPaddingY;
        var cursor = position + new Vector2(RTL ? width : 0, paddingY);

        // shorten the display mode
        var dm = plugin.Configuration.DisplayMode;

        // Set some constants
        var imgsize = size.Y - (paddingY * 2f);
        var white = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1));
        var drawlist = ImGui.GetBackgroundDrawList();

        // Draw Background
        if (plugin.Configuration.doGradientBackground)
        {
            DrawOverlayBackgroundGradient(position, size, RTL);
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
                    (RTL ? -1 : 1) * ((1f * ImGui.CalcTextSize(icon.Label).X) + (paddingX * partylistscaling)),
                    0
                );
                drawlist.AddText(cursor, white, icon.Label);
            }

            // The icon itself
            cursor += new Vector2(
                (RTL ? -1 : 1) * ((1f * imgsize) + paddingX),
                0
            );
            drawlist.AddImage(plugin.textures[icon.FileName].ImGuiHandle, cursor, cursor + new Vector2(imgsize, imgsize));

            // Info, e.g. mit percent
            if (icon.Value != null && dm != 3) {
                cursor += new Vector2(
                    (RTL ? -1 : 1) * ((1f * ImGui.CalcTextSize(icon.ValueStr()).X * partylistscaling) + paddingX),
                    0
                );
                drawlist.AddText(cursor, white, icon.ValueStr());
            }
        }

        //ImGui.EndChild();
    }
}
