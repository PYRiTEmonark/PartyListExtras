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
        public bool showEssenceSelfs { get; set; } = true;

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
        public Dictionary<SpecialEffects, StatusIcon> SpecialIcons;

        static readonly Dictionary<SpecialEffects, StatusIcon> DefaultIcons = new Dictionary<SpecialEffects, StatusIcon> {
            { SpecialEffects.stance, new StatusIcon { FileName = "stance.png", Label = "Stance" } },
            { SpecialEffects.invuln, new StatusIcon { FileName = "invuln.png", Label = "Invuln" } },
            { SpecialEffects.living_dead, new StatusIcon { FileName = "living_dead.png", Label = "Living Dead" } },
            { SpecialEffects.kardia, new StatusIcon { FileName = "kardia.png", Label = "Sent" } },
            { SpecialEffects.kardion, new StatusIcon { FileName = "kardion.png", Label = "Recv" } },
            { SpecialEffects.dp_g, new StatusIcon { FileName = "dp_g.png", Label = "Sent" } },
            { SpecialEffects.dp_r, new StatusIcon { FileName = "dp_r.png", Label = "Recv" } },
            { SpecialEffects.regen, new StatusIcon { FileName = "regen.png", Label = "Regen" } },
            { SpecialEffects.barrier, new StatusIcon { FileName = "barrier.png", Label = "Barrier" } }
        };

        public List<SpecialEffects> hiddenSpecialEffects = new List<SpecialEffects>();

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
            SpecialIcons.Remove(SpecialEffects.crit_rate_up);
            SpecialIcons.Remove(SpecialEffects.max_hp_up);
            SpecialIcons.Remove(SpecialEffects.block_all);

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
