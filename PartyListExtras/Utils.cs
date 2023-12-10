using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PartyListExtras
{
    public static class Utils
    {

        [JsonConverter(typeof(StringEnumConverter))]
        public enum TargetType
        {
            Self, // Given to self only, e.g. Tank's Rampart
            StanceSelf, // Tank stances
            ConstSelf, // Can/should always be up, e.g. SAM's Fuka and Fugetsu
            EssenceSelf, // Save the Queen Essences and Deep Essences
            PartyMember, // Granted to self or targeted party member e.g. WHM's Regen
            ConstPartyMember, // Can/Should always be up and granted to self or targeted party member e.g. Some StQ lost actions
            PartyShared, // e.g. DNC's Dance Partner
            PartyWide, // Granted to all party members e.g. GNB's Heart of Light or DRG's Battle Litany
            PartyAoE // Granted by standing in an AoE e.g. BLM's Ley Lines
        }

        public enum FloatEffect
        {
            // Mitigation, "othr_mit" unused, may become effective mit
            phys_mit,
            magi_mit,
            othr_mit,
            max_hp_up,
            block_rate,
            // Damage Up by dmg type, "othr_up" as above
            phys_up,
            magi_up,
            othr_up,
            crit_rate_up,
            dhit_rate_up,
            // Speed Ups
            attack_speed_up,
            cast_speed_up,
            ability_cast_speed_up,
            auto_speed_up,
            // Other Stats Up
            move_speed_up,
            evade_up,
            max_mp_up,
            healing_up,
            healing_pot,
        }


        // TODO: find/replace these to be capitals and/or better names
        [JsonConverter(typeof(StringEnumConverter))]
        public enum BoolEffect
        {
            stance, // Tank Stances
            invuln, // Tank Invulnrabilities
            living_dead, // DRK's Living Dead (before invuln pops)
            block_all, // DEPRECATED - DO NOT USE - to be removed in 0.2.0.0
            kardia,
            kardion,
            dp_g,
            dp_r,
            regen,
            knockback_immunity, // NOT IMPLIMENTED
            barrier,
            crit_rate_up, // DEPRECATED - DO NOT USE - to be removed in 0.2.0.0
            max_hp_up // DEPRECATED - DO NOT USE - to be removed in 0.2.0.0
        }

        internal static float sum(float? a, float? b)
        {
            // filter out null values then sum
            //float x = values.Where(x => x != null).Sum() ?? 0;
            //return x;
            float c = a ?? 0;
            float d = b ?? 0;
            return c + d;
        }

        internal static float max(float? a, float? b)
        {
            // filter out null values then sum
            //float x = values.Where(x => x != null).Max() ?? 0;
            //return x;
            float c = a ?? 0;
            float d = b ?? 0;
            return Math.Max(c, d);
        }

        internal static float multi_sum(float? a, float? b)
        {
            //var x = values.Where(x => x != null).Select(x => 1 - x).Aggregate(1f, (a, b) => a * b!.Value);
            //return 1f - x;
            float c = a ?? 0;
            float d = b ?? 0;
            return 1 - ((1 - c) * (1 - d));
        }

        // Standin instead of e.g. Lumina data
        public enum Job
        {
            AST, BLM, BRD,
            DNC, DRG, DRK,
            GNB, MCH, MNK,
            NIN, PLD, RDM,
            RPR, SAM, SCH,
            SGE, SMN, WAR, WHM
        }

        // Roles are collections of Jobs
        internal class Role : EnumClass
        {
            public required List<Job> jobs = new();

            public static Role DPS = new Role
            {
                id = 0,
                jobs = new List<Job>() { Job.BLM, Job.BRD, Job.DNC, Job.DRG, Job.MCH, Job.MNK, Job.NIN, Job.RDM, Job.RPR, Job.SAM, Job.SMN }
            };
            public static Role Tank = new Role
            {
                id = 1,
                jobs = new List<Job>() { Job.DRK, Job.GNB, Job.PLD, Job.WAR }
            };
            public static Role Healer = new Role
            {
                id = 2,
                jobs = new List<Job>() { Job.AST, Job.SCH, Job.SGE, Job.WHM }
            };
            public static Role Melee = new Role
            {
                id = 3,
                jobs = new List<Job>() { Job.DRG, Job.DRK, Job.GNB, Job.MNK, Job.NIN, Job.PLD, Job.RPR, Job.SAM, Job.WAR }
            };
            public static Role Ranged = new Role
            {
                id = 4,
                jobs = new List<Job>() { Job.AST, Job.BLM, Job.BRD, Job.DNC, Job.MCH, Job.RDM, Job.SCH, Job.SGE, Job.SMN, Job.WHM }
            };
        }

        internal enum Combi { multi_sum, sum, max }

        internal static Dictionary<FloatEffect, Combi> statusCombinators =
            new Dictionary<FloatEffect, Combi>() {
        { FloatEffect.phys_mit, Combi.multi_sum },
        { FloatEffect.magi_mit, Combi.multi_sum },
        { FloatEffect.othr_mit, Combi.multi_sum },
        { FloatEffect.max_hp_up, Combi.sum },
        { FloatEffect.block_rate, Combi.sum },
        { FloatEffect.phys_up, Combi.multi_sum },
        { FloatEffect.magi_up, Combi.multi_sum },
        { FloatEffect.othr_up, Combi.multi_sum },
        { FloatEffect.crit_rate_up, Combi.multi_sum },
        { FloatEffect.dhit_rate_up, Combi.multi_sum },
        { FloatEffect.attack_speed_up, Combi.multi_sum },
        { FloatEffect.cast_speed_up, Combi.multi_sum },
        { FloatEffect.ability_cast_speed_up, Combi.multi_sum },
        { FloatEffect.auto_speed_up, Combi.multi_sum },
        { FloatEffect.move_speed_up, Combi.max },
        { FloatEffect.evade_up, Combi.multi_sum },
        { FloatEffect.max_mp_up, Combi.sum },
        { FloatEffect.healing_up, Combi.multi_sum },
        { FloatEffect.healing_pot, Combi.multi_sum }
        };
        
    }
}
