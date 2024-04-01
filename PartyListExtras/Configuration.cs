using Dalamud.Configuration;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using PartyListExtras;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using static PartyListExtras.Utils;

namespace PartyListExtras
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public int DisplayMode { get; set; } = 2;

        public int OverlayOffsetX { get; set; } = 10;
        public int OverlayOffsetY { get; set; } = 0;
        public int OverlayWidth { get; set; } = 300;
        public int OverlayPaddingX { get; set; } = 5;
        public int OverlayPaddingY { get; set; } = 5;

        public bool EnableOverlay { get; set; } = true;
        public bool hideOutOfCombat { get; set; } = false;
        public bool alwaysShowInDuty { get; set; } = false;

        public bool enableFloatText { get; set; } = true;
        
        // Also filters out ConstPartyMember, Name not changed to prevent config migration
        public bool showConstSelfs {  get; set; } = false;
        public bool showEssenceSelfs { get; set; } = false;

        public bool doGradientBackground { get; set; } = true;
        public Vector4 colorRight { get; set; } = new Vector4(0f, 0f, 0f, 1f);
        public Vector4 colorLeft { get; set; } = new Vector4(0f, 0f, 0f, 0f);
        public Vector4 colorSingle { get; set; } = new Vector4(0f, 0f, 0f, 1f);

        public StatusIconConfig iconConfig = new StatusIconConfig();

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
            // preemtive config migration
            this.iconConfig.validateIcons();
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }

    [Serializable]
    public struct StatusIconConfig
    {
        public Dictionary<BoolEffect, StatusIcon> SpecialIcons;

        static readonly Dictionary<BoolEffect, StatusIcon> DefaultIcons = new Dictionary<BoolEffect, StatusIcon> {
            { BoolEffect.stance, new StatusIcon { FileName = "stance.png", Label = "Stance" } },
            { BoolEffect.invuln, new StatusIcon { FileName = "invuln.png", Label = "Invuln" } },
            { BoolEffect.living_dead, new StatusIcon { FileName = "living_dead.png", Label = "Living Dead" } },
            { BoolEffect.kardia, new StatusIcon { FileName = "kardia.png", Label = "Sent" } },
            { BoolEffect.kardion, new StatusIcon { FileName = "kardion.png", Label = "Recv" } },
            { BoolEffect.dp_g, new StatusIcon { FileName = "dp_g.png", Label = "Sent" } },
            { BoolEffect.dp_r, new StatusIcon { FileName = "dp_r.png", Label = "Recv" } },
            { BoolEffect.regen, new StatusIcon { FileName = "regen.png", Label = "Regen" } },
            { BoolEffect.barrier, new StatusIcon { FileName = "barrier.png", Label = "Barrier" } }
        };

        public List<BoolEffect> hiddenSpecialEffects = new List<BoolEffect>();

        public bool showMit = true;
        public bool alwaysSplitMit = false;
        
        public bool showDmgUp = true;
        public bool alwaysSplitDmgUp = false;
        
        public bool showSpeedUps = true;
        public bool stackSpeedUps = false;

        public bool showBlockUp = true;

        public bool showHealUps = true;
        public bool showCritDH = true;

        public bool showMoveSpeed = false;
        public bool showHPup = true;
        public bool showMPup = false;

        // Effects that should always be up
        public bool showConstSelf = false;
        public bool showConstPartyMember = false;

        public StatusIconConfig()
        {
            SpecialIcons = DefaultIcons;
        }

        /// <summary>
        /// Adds missing icon fields and removes deprecated ones
        /// </summary>
        public void validateIcons()
        {
            // Deprecated icons removed
            SpecialIcons.Remove(BoolEffect.crit_rate_up);
            SpecialIcons.Remove(BoolEffect.max_hp_up);
            SpecialIcons.Remove(BoolEffect.block_all);

            foreach (var icon in DefaultIcons)
            {
                // Add missing Icons
                if (!SpecialIcons.ContainsKey(icon.Key))
                    SpecialIcons.Add(icon.Key, icon.Value);

                // Add missing tooltips
                else if (SpecialIcons[icon.Key].Tooltip == null)
                {
                    var x = SpecialIcons[icon.Key];
                    x.Tooltip = DefaultIcons[icon.Key].Tooltip;
                    SpecialIcons[icon.Key] = x;
                }
            }
        }
    }
}
