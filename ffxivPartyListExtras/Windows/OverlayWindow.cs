using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;

namespace ffxivPartyListExtras.Windows;

public class OverlayWindow : Window, IDisposable
{
    private Plugin plugin;

    // Used to ensure we don't duplicate the debug status info message
    public List<Tuple<string, string>> missing_ids = new List<Tuple<string, string>>();

    public unsafe OverlayWindow(Plugin plugin) : base(
        "PLX_OverlayWindow",
        ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground |
        ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings)
    {
        this.Size = new Vector2(10000, 10000);
        this.Position = new Vector2(0, 0);

        this.plugin = plugin;
    }

    public void Dispose() { }

    public unsafe override void Draw()
    {
        // Grab some things for later
        // TODO: move some of these idk, imgui will shit itself if you move the drawlist
        var drawlist = ImGui.GetBackgroundDrawList();
        AgentHUD* pl = Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentHUD();
        var partyMemberList = (HudPartyMember*)pl->PartyMemberList;
        var count = pl->PartyMemberCount;

        var black = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 1f));
        var clear = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0f));

        AddonPartyList* apl = (AddonPartyList*)plugin.GameGui.GetAddonByName("_PartyList");
        if (apl == null) { return; }

        var bkg_node = apl->BackgroundNineGridNode;
        if (bkg_node == null) { return; }

        var apl_node = bkg_node->AtkResNode;

        // resize window
        // Set width and height so window is to the left of the party list
        var width = 300;
        this.Size = new Vector2(width, apl_node.Height);
        this.Position = new Vector2(apl_node.ScreenX - width - 10, apl_node.ScreenY);

        // nullable types my beloathed
        drawlist.AddRectFilledMultiColor((Vector2)this.Position, (Vector2)(this.Position + this.Size), clear, black, black, clear);

        // drawlist.AddText(new Vector2(15, 15), ImGui.GetColorU32(ImGuiCol.Text), count.ToString()) ;

        for (var i = 0; i < count; i++)
        {
            // Get ResNode of the JobIcon
            var node = apl->PartyMember[i].ClassJobIcon->AtkResNode;
            // TEMP: Offset is temporary
            var curpos = new Vector2(node.ScreenX - 300, node.ScreenY);
            var childpos = curpos - (Vector2)this.Position;

            ImGui.SetCursorPos(childpos);
            ImGui.BeginChild("Player {0}".Format(i));

            // PlayerCharacter and BattleNPC is both BattleChara
            // TODO: fix this so there isn't the duplication
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
            else {
                //if (result == null) PluginLog.Warning("Party List member null");
                //else PluginLog.Warning("Unexpected member of party list: {0}", result.GetType().ToString());
                continue;
            };

            //Spin out to helper functions because long
            DrawStatusIcons(ParseStatusList(sl));

            ImGui.EndChild();
        }
    }

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

        // Read properties out of data
        // TODO: move all definitions of IconElement out to a json file
        string[] special_labels = {
            "stance", "invuln", "living_dead",
            "kardion", "kardia", "regen", //"barrier",
            "dp_g", "dp_r"
        };
        StatusIcon[] special_fstrings = new StatusIcon[] {
            new StatusIcon {FileName = "stance.png", Label = "Stance"},
            new StatusIcon {FileName = "invuln.png", Label = "Invuln"},
            new StatusIcon {FileName = "living_dead.png", Label = "Living Dead"},
            new StatusIcon {FileName = "kardia.png", Label = "Sent"},
            new StatusIcon {FileName = "kardion.png", Label = "Recv"},
            new StatusIcon {FileName = "regen.png", Label = "Regen"},
            //new IconElement {FileName = "barrier.png", Label = "Barrier"},
            new StatusIcon {FileName = "dp_g.png", Label = "Sent"},
            new StatusIcon {FileName = "dp_r.png", Label = "Recv"},
        };

        for (int i = 0; i < special_labels.Length; i++)
        {
            if (datas.Select(x => x.special).Contains(special_labels[i]))
                output.Add(special_fstrings[i]);
        }

        var phys_mit = multi_sum(datas.Select(x => x.phys_mit));
        var magi_mit = multi_sum(datas.Select(x => x.magi_mit));
        //var othr_mit = multi_sum(datas.Select(x => x.othr_mit));

        var phys_up = multi_sum(datas.Select(x => x.phys_up));
        var magi_up = multi_sum(datas.Select(x => x.magi_up));
        //var othr_up = multi_sum(datas.Select(x => x.othr_up));

        // Mitigation
        if (phys_mit == magi_mit && phys_mit > 0) output.Add(new StatusIcon { FileName="mit_all.png", Info = "{0}%%".Format(phys_mit), Label = "Mitigation"});
        else
        {
            if (phys_mit > 0) output.Add(new StatusIcon { FileName = "mit_phys.png", Info = "{0}%%".Format(phys_mit), Label = "Physical Mit" });
            if (magi_mit > 0) output.Add(new StatusIcon { FileName = "mit_all.png", Info = "{0}%%".Format(magi_mit), Label = "Magical Mit" });
        }

        // Damage Up
        if (phys_up == magi_up && phys_up > 0) output.Add(new StatusIcon { FileName = "all_up.png", Info = "{0}%%".Format(phys_up), Label = "Damage Up"});
        else
        {
            if (phys_up > 0) output.Add(new StatusIcon { FileName = "phys_up.png", Info = "{0}%%".Format(phys_up), Label = "Phyiscal Dmg Up" });
            if (magi_up > 0) output.Add(new StatusIcon { FileName = "magi_up.png", Info = "{0}%%".Format(magi_up), Label = "Magical Dmg Up" });
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
        if (debugMessage.Length > 0) PluginLog.Debug("Missing Status Ids: " + debugMessage);

        return output;
    }

    // Replace $mit_all.png$ with an image when rendering
    private void DrawStatusIcons(List<StatusIcon> icons)
    {

        if (this.Size == null) return;
        var width = ((Vector2)this.Size).X-20;
        var posY = ImGui.GetCursorPosY();
        ImGui.SetCursorPosX(width);
        var startpos = ImGui.GetCursorPos();

        var dm = plugin.Configuration.DisplayMode;

        // TODO: the SetCursorPosX calls are scuffed, work out actual widths
        // just don't question the scalars
        foreach (var icon in icons)
        {
            if (icon.Label != null && (dm == 0 || (dm == 1 && icon.Info == null)))
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (1.5f * ImGui.CalcTextSize(icon.Label).X));
                startpos = ImGui.GetCursorPos();
                ImGui.Text(icon.Label);
                ImGui.SetCursorPos(startpos);
            }

            //ImGui.SetCursorPosY(posY);
            ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() - (1.5f * ImGui.GetFontSize()), posY));
            startpos = ImGui.GetCursorPos();
            ImGui.Image(plugin.textures[icon.FileName].ImGuiHandle, new Vector2(ImGui.GetFontSize(), ImGui.GetFontSize()));
            ImGui.SetCursorPos(startpos);

            if (icon.Info != null && dm != 3) {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - (1f * ImGui.CalcTextSize(icon.Info).X));
                startpos = ImGui.GetCursorPos();
                ImGui.Text(icon.Info);
                ImGui.SetCursorPos(startpos);
            }
        }
    }

    // VS might moan about some of these being possibly Null.
    // VS is wrong, we just filtered out the Null values.
    internal bool one_true(IEnumerable<bool?> values)
    {
        // filter out null values then aggregate using or
        return values.Where(x => x != null).Aggregate(false, (a, b) => a || b.Value);
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

    internal struct StatusIcon
    {
        public string FileName { get; set; } // The icon itself, e.g. "mit_all.png"
        public string? Label { get; set; } // Static label on the icon, e.g. "Mitigation"
        public string? Info { get; set; } // Info on the icon, e.g. the actual mit percentage
    }
}
