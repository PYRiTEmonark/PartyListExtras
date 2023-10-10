using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Havok;
using ImGuiNET;

namespace PartyListExtras.Windows;

public class OverlayWindow : Window, IDisposable
{
    private Plugin plugin;

    // Scales all drawn things
    public float scaling;

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
        var drawlist = ImGui.GetBackgroundDrawList();

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
            plugin.pluginLog.Verbose("Failure during start of OverlayWindow.Draw - skipping. Message: " + e.Message);
            return;
        }

        var black = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 1f));
        var clear = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0f));

        // resize window
        // Set width and height so window is to the left of the party list
        //var width = 300;
        //this.Size = new Vector2(width, apl_node.Height);
        //this.Position = new Vector2(apl_node.ScreenX - width - 10, apl_node.ScreenY);

        this.Size = ImGui.GetMainViewport().Size;

        // Draw background - old, entire list version
        //drawlist.AddRectFilledMultiColor((Vector2)this.Position, (Vector2)(this.Position + this.Size), clear, black, black, clear);

        for (var i = 0; i < count; i++)
        {
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
            else
            {
                //if (result == null) PluginLog.Warning("Party List member null");
                //else PluginLog.Warning("Unexpected member of party list: {0}", result.GetType().ToString());
                continue;
            };

            // Get ResNode of the JobIcon
            var icon = apl->PartyMember[i].ClassJobIcon;
            if (icon == null) { continue; }
            var node = icon->AtkResNode;
            // TEMP: Offset is temporary
            var curpos = new Vector2(node.ScreenX - ((300 * scaling) + 10), node.ScreenY);
            var cursize = new Vector2(300 * scaling, node.Height * scaling);

            // this.scaling = node.Height / 32;

            // Draw Background
            drawlist.AddRectFilledMultiColor(curpos, (curpos+cursize), clear, black, black, clear);

            // Start child window to make the cursor work "nicer"
            ImGui.SetCursorPos(curpos);
            ImGui.BeginChild("Player {0}".Format(i), cursize, false, Flags);

            //Spin out to helper functions because long
            DrawStatusIcons(ParseStatusList(sl), cursize.Y);

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

        // This Needs to be paralell to the `SpecialEffects` enum
        StatusIcon[] special_fstrings = new StatusIcon[] {
            new StatusIcon {FileName = "stance.png", Label = "Stance"},
            new StatusIcon {FileName = "invuln.png", Label = "Invuln"},
            new StatusIcon {FileName = "living_dead.png", Label = "Living Dead"},
            new StatusIcon {FileName = "block_all.png", Label = "Block All"},
            new StatusIcon {FileName = "kardion.png", Label = "Recv"},
            new StatusIcon {FileName = "kardia.png", Label = "Sent"},
            new StatusIcon {FileName = "dp_g.png", Label = "Sent"},
            new StatusIcon {FileName = "dp_r.png", Label = "Recv"},
            new StatusIcon {FileName = "regen.png", Label = "Regen"},
            new StatusIcon {FileName = "crit_rate_up.png", Label = "Crit Up"},
            //new IconElement {FileName = "barrier.png", Label = "Barrier"},
        };

        for (int i = 0; i < Enum.GetNames(typeof(SpecialEffects)).Length; i++)
        {
            if (datas.Select(x => x.special).Contains((SpecialEffects)i))
                output.Add(special_fstrings[i]);
        }

        // Mitigation
        var phys_mit = multi_sum(datas.Select(x => x.phys_mit));
        var magi_mit = multi_sum(datas.Select(x => x.magi_mit));

        if (phys_mit == magi_mit && phys_mit > 0)
            output.Add(new StatusIcon { FileName="mit_all.png", Info = "{0}%%".Format(phys_mit), Label = "Mitigation"});
        else
        {
            if (phys_mit > 0)
                output.Add(new StatusIcon { FileName = "mit_phys.png", Info = "{0}%%".Format(phys_mit), Label = "Physical Mit" });
            if (magi_mit > 0)
                output.Add(new StatusIcon { FileName = "mit_magi.png", Info = "{0}%%".Format(magi_mit), Label = "Magical Mit" });
        }

        // Damage Up
        var phys_up = multi_sum(datas.Select(x => x.phys_up));
        var magi_up = multi_sum(datas.Select(x => x.magi_up));

        if (phys_up == magi_up && phys_up > 0)
            output.Add(new StatusIcon { FileName = "all_up.png", Info = "{0}%%".Format(phys_up), Label = "Damage Up"});
        else
        {
            if (phys_up > 0)
                output.Add(new StatusIcon { FileName = "phys_up.png", Info = "{0}%%".Format(phys_up), Label = "Phyiscal Dmg Up" });
            if (magi_up > 0)
                output.Add(new StatusIcon { FileName = "magi_up.png", Info = "{0}%%".Format(magi_up), Label = "Magical Dmg Up" });
        }

        // Attack Speed
        var attack_speed_up = multi_sum(datas.Select(x => x.attack_speed_up));
        var cast_speed_up = multi_sum(datas.Select(x => x.cast_speed_up));
        var auto_speed_up = multi_sum(datas.Select(x => x.auto_speed_up));

        if (attack_speed_up > 0)
            output.Add(new StatusIcon { FileName = "attack_speed_up.png", Info = "{0}%%".Format(attack_speed_up), Label = "Attack Speed Up" });
        if (cast_speed_up > 0)
            output.Add(new StatusIcon { FileName = "cast_speed_up.png", Info = "{0}%%".Format(cast_speed_up), Label = "Cast Speed Up" });
        if (auto_speed_up > 0)
            output.Add(new StatusIcon { FileName = "auto_speed_up.png", Info = "{0}%%".Format(auto_speed_up), Label = "Auto Speed Up" });

        // Heal Rate
        var healing_up = multi_sum(datas.Select(x => x.healing_up));
        if (healing_up > 0)
            output.Add(new StatusIcon { FileName = "healing_up.png", Info = "{0}%%".Format(healing_up), Label = "Healing Up" });

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
        if (debugMessage.Length > 0) plugin.pluginLog.Debug("Missing Status Ids: " + debugMessage);

        return output;
    }

    // Render StatusIcons
    // Note: Still uses the cursor
    // This isn't bad, but it's a strange way to do it
    private void DrawStatusIcons(List<StatusIcon> icons, float height)
    {
        if (this.Size == null) return;
        var width = 300 * scaling;
        ImGui.SetCursorPosX(width);
        var startpos = ImGui.GetCursorPos();

        // shorten it out
        var dm = plugin.Configuration.DisplayMode;

        // Set some preferences
        var padding = 5f;
        var imgsize = height - (padding * 2f);
        var posY = ImGui.GetCursorPosY() + padding;

        // TODO: the widths are mostly guesses here, possible to break with scaling
        foreach (var icon in icons)
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - padding);

            if (icon.Label != null && (dm == 0 || (dm == 1 && icon.Info == null)))
            {
                move_cur_scaled(
                    ImGui.GetCursorPosX() - (1f * ImGui.CalcTextSize(icon.Label).X), -padding * scaling,
                    posY * scaling, 0
                );
                startpos = ImGui.GetCursorPos();
                ImGui.Text(icon.Label);
                ImGui.SetCursorPos(startpos);
            }

            move_cur_scaled(
                ImGui.GetCursorPosX() - (1f * imgsize), -padding,
                posY * scaling, 0
            );
            startpos = ImGui.GetCursorPos();
            ImGui.Image(plugin.textures[icon.FileName].ImGuiHandle, new Vector2(imgsize, imgsize));
            ImGui.SetCursorPos(startpos);

            if (icon.Info != null && dm != 3) {
                move_cur_scaled(
                    ImGui.GetCursorPosX() - (1f * ImGui.CalcTextSize(icon.Info).X * scaling), padding,
                    posY * scaling, 0
                );
                startpos = ImGui.GetCursorPos();
                ImGui.Text(icon.Info);
                ImGui.SetCursorPos(startpos);
            }
        }
    }

    internal void move_cur_scaled(float x, float ox, float y, float oy)
    {
        // this was useful, then it wasn't
        // it may become useful later
        ImGui.SetCursorPos(new Vector2(
            x + ox,
            y + oy
        ));
    }

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
}
