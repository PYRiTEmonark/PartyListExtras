using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using static PartyListExtras.Utils;

namespace PartyListExtras
{
    internal struct StatusEffectData
    {
        // to be used as Mapping Key
        public required int row_id { get; set; }
        // should be as in game
        public required string status_name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TargetType target_type { get; set; }

        // Conditionals - see Conditoinal struct for details
        public List<Conditional> cond { get; set; }

        // always applied no matter what
        public AppliedEffects cond_default { get; set; }

        // applied if none of the conditions are met. unspecified behaviour if there are not conds.
        public AppliedEffects cond_else { get; set; }

        public AppliedEffects Compute(CondVars condvars)
        {
            AppliedEffects result = new AppliedEffects();
            bool onemet = false;

            if (cond is not null)
                foreach (Conditional item in cond)
                    if (item.IsMet(condvars))
                    {
                        result = result.Combine(item.then);
                        onemet = true;
                    }

            if (cond is not null && cond.Count > 0 && !onemet)
                result = result.Combine(cond_else);

            result = result.Combine(cond_default);

            return result;
        }

    }

    /// <summary>
    /// Data for a conditional effect
    /// In the data this is the part that determines if an effect is applied
    /// </summary>
    internal struct Conditional
    {
        internal bool IsMet(CondVars other)
        {
            bool matched = true;

            if (targetLevel_gte is not null)
                matched &= targetLevel_gte >= other.targetLevel;

            if (targetJob is not null)
                matched &= targetJob == other.targetJob;

            if (targetRole is not null && other.targetJob.HasValue)
                matched &= targetRole.jobs.Contains(other.targetJob.Value);

            return matched;

        }

        public AppliedEffects then { get; set; }
        public int? targetLevel_gte { get; set; }
        public Job? targetJob { get; set; }
        public Role? targetRole { get; set; }

    }

    internal struct CondVars
    {
        public bool GetByName(string name, out object? value)
        {
            value = null;
            
            var prop = GetType().GetProperty(name);
            if (prop is null) return false;
            
            value = prop.GetValue(this, null);
            return true;
        }

        public int? targetLevel { get; set; }
        public Job? targetJob { get; set; }
    }

    public struct AppliedEffects
    {
        public AppliedEffects() { }

        public Dictionary<FloatEffect, float> standard { get; set; }
            = new Dictionary<FloatEffect, float>();

        // special acts as a custom field, e.g. stance, invuln
        [JsonProperty("special", ItemConverterType = typeof(StringEnumConverter))]
        public List<BoolEffect>? special { get; set; }
            = new List<Utils.BoolEffect>();

        internal AppliedEffects Combine(AppliedEffects othr)
        {
            var t_spc = this.special ?? new List<BoolEffect>();
            var o_spc = othr.special ?? new List<BoolEffect>();

            var t_stn = this.standard ?? new Dictionary<FloatEffect, float>();
            var o_stn = othr.standard ?? new Dictionary<FloatEffect, float>();

            var out_stn = new Dictionary<FloatEffect, float>();
            var ks = t_stn.Keys.ToList();
            ks.AddRange(o_stn.Keys.ToList());

            foreach (var k in ks)
            {
                float a; float b;
                if (!t_stn.TryGetValue(k, out a)) a = 0;
                if (!o_stn.TryGetValue(k, out b)) b = 0;

                switch (statusCombinators.GetValueOrDefault(k, Combi.multi_sum))
                {
                    case Utils.Combi.sum:
                        out_stn[k] = a + b; break;
                    case Combi.multi_sum:
                        out_stn[k] = multi_sum(a, b); break;
                    case Combi.max:
                        out_stn[k] = Math.Max(a, b); break;
                }

            }

            return new AppliedEffects
            {
                standard = out_stn,
                special = t_spc.Concat(o_spc).ToList()
            };
        }
    
        internal float GetEffectOrDefault(Utils.FloatEffect effect, float dflt = 0)
        {
            if (standard.ContainsKey(effect))
                return standard[effect];
            return dflt;
        }

        #region OnDeserialized

        [JsonExtensionData]
        private IDictionary<string, JToken>? additionalData;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            standard = new Dictionary<FloatEffect, float>();

            if (additionalData is null) return;
            foreach (var kv in additionalData!)
            {
                if (!Enum.TryParse<FloatEffect>(kv.Key, out var key))
                    continue;

                if (!float.TryParse(kv.Value.ToString(), out var value))
                    continue;

                standard[key] = value;
            }
        }

        #endregion

    }

    public struct StatusIcon
    {
        /// The path to the icon image, e.g. "mit_all.png"
        public string FileName { get; set; }

        /// Static label on the icon, e.g. "Mitigation"
        public string? Label { get; set; }

        /// Info on the icon, e.g. the actual mit percentage
        public float? Value { get; set; }

        /// Tooltop to show on hover
        public string? Tooltip { get; set; }

        /// <summary>
        /// Format the Value propety as a percentage.
        /// </summary>
        /// <returns>Formatted string or empty string if Value is null.</returns>
        public string ValueStr()
        {
            if (Value is null) return "";
            return Math.Round((decimal)(Value! * 100), 0).ToString() + "%";
        }
    }
}
