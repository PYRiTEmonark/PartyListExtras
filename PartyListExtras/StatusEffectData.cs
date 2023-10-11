using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PartyListExtras
{
    internal struct StatusEffectData
    {
        // to be used as Mapping Key
        public required int row_id { get; set; }
        // should be as in game
        public required string status_name { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required StatusType status_type { get; set; }

        // special acts as a custom field, e.g. stance, invuln
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SpecialEffects? special { get; set; }

        // Mitigation
        public float? phys_mit { get; set; }
        public float? magi_mit { get; set; }
        public float? othr_mit { get; set; }

        // Damage Up
        public float? phys_up { get; set; }
        public float? magi_up { get; set; }
        public float? othr_up { get; set; }

        // Speed Up
        public float? attack_speed_up { get; set; }
        public float? cast_speed_up { get; set; }
        public float? auto_speed_up { get; set; }

        // Healing Up
        public float? healing_up { get; set; }
    }

    internal struct StatusIcon
    {
        public string FileName { get; set; } // The path to the icon image, e.g. "mit_all.png"
        public string? Label { get; set; } // Static label on the icon, e.g. "Mitigation"
        public string? Info { get; set; } // Info on the icon, e.g. the actual mit percentage
    }

    internal enum StatusType
    {
        Self, // Given to self only, e.g. Tank's Rampart
        ConstSelf, // Can/should always be up, e.g. SAM's Fuka and Fugetsu
        PartyMember, // Granted to targeted party member (or self if no target) e.g. WHM's Regen
        PartyShared, // e.g. DNC's Dance Partner
        PartyWide, // Granted to all party members e.g. GNB's Heart of Light or DRG's Battle Litany
        PartyAoE // Granted by standing in an AoE e.g. BLM's Ley Lines
    }

    // TODO: find/replace these to be capitals and/or better names
    // Also general todo to move away from the special field
    internal enum SpecialEffects
    {
        stance, // Tank Stances
        invuln, // Tank Invulnrabilities
        living_dead, // DRK's Living Dead (before invuln pops)
        block_all, // PLD's Bulwark
        kardion,
        kardia,
        dp_g,
        dp_r,
        regen,
        crit_rate_up,
        barrier // Currently unused
    }
}
