using Dalamud.Game;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Collections.Generic;
using static PartyListExtras.Utils;

namespace PartyListExtras
{
    internal class FlyText : IDisposable
    {
        private Plugin plugin;

        private FlyTextMatcher matcher;

        private unsafe delegate void AddToScreenLogDelegate(
            Character* target,
            Character* source,
            FlyTextKind logKind,
            int option,
            int actionKind,
            int actionId,
            int val1,
            int val2,
            int serverAttackType,
            int val4);

        //[Signature("E8 ?? ?? ?? ?? BF ?? ?? ?? ?? 41 F6 87", DetourName = nameof(AddToScreenLogDetour))]
        private Hook<AddToScreenLogDelegate>? addToScreenLogHook = null;

        public unsafe FlyText(Plugin plugin)
        {
            this.plugin = plugin;

            plugin.FlyTextGui.FlyTextCreated += onFlyText;
            
            var addScreenLogPtr = plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? BF ?? ?? ?? ?? 41 F6 87");
            addToScreenLogHook = plugin.Hooks.HookFromAddress<AddToScreenLogDelegate>(addScreenLogPtr, AddToScreenLogDetour);

            addToScreenLogHook?.Enable();

            matcher = new FlyTextMatcher();
        }

        public void Dispose()
        {
            plugin.FlyTextGui.FlyTextCreated -= onFlyText;
            addToScreenLogHook?.Dispose();
        }

        static List<FlyTextKind> DamageKinds = new List<FlyTextKind>() {
            FlyTextKind.Damage, FlyTextKind.DamageDh, FlyTextKind.DamageCrit, FlyTextKind.DamageCritDh,
        };
        static List<FlyTextKind> AutoKinds = new List<FlyTextKind>() {
            FlyTextKind.AutoAttackOrDot, FlyTextKind.AutoAttackOrDotDh, FlyTextKind.AutoAttackOrDotCrit, FlyTextKind.AutoAttackOrDotCritDh
        };
        static List<FlyTextKind> HealingKinds = new List<FlyTextKind>() {
            FlyTextKind.Healing, FlyTextKind.HealingCrit
        };

        private unsafe void AddToScreenLogDetour(
            Character* target,
            Character* source,
            FlyTextKind kind,
            int option,
            int actionKind,
            int actionId,
            int val1,
            int val2,
            int serverAttackType,
            int val4)
        {
            addToScreenLogHook?.Original(target, source, kind, option, actionKind, actionId, val1, val2, serverAttackType, val4);
            try
            {

                if (!plugin.Configuration.enableFloatText) return;

                // This feels safer. (vibes based programming)
                var _source = plugin.ObjectTable.SearchById(source->GameObject.ObjectID);
                var _target = plugin.ObjectTable.SearchById(target->GameObject.ObjectID);

                // Get AppliedEffects for source and target

                AppliedEffects sourceEffects;
                if (TryGetPartyMemberBattleChara(_source, out var bcsource))
                {
                    sourceEffects = plugin.OverlayWindow.ParseStatusList(bcsource!.StatusList, new CondVars());
                }

                else // Source is not a party member
                    sourceEffects = new AppliedEffects();

                AppliedEffects targetEffects;
                if (TryGetPartyMemberBattleChara(_target, out var bctarget))
                {
                    targetEffects = plugin.OverlayWindow.ParseStatusList(bctarget!.StatusList, new CondVars());
                }
                else // Target is not a party member
                    targetEffects = new AppliedEffects();

                float tagValue = 0f;
                FlyTextStatusType statusType = FlyTextStatusType.None;

                // Damage and source in party -> Damage up
                if (DamageKinds.Contains(kind) && IsCharaInParty(bcsource))
                {
                    tagValue = sourceEffects.GetEffectOrDefault(FloatEffect.phys_up);
                    statusType = FlyTextStatusType.DamageUp;
                }
                // Damage and target in party -> Mitigation
                else if ((DamageKinds.Contains(kind) || AutoKinds.Contains(kind)) && IsCharaInParty(bctarget))
                {
                    tagValue = targetEffects.GetEffectOrDefault(FloatEffect.phys_mit);
                    statusType = FlyTextStatusType.Mitigation;
                }
                // Healing and both in party -> healing
                else if (HealingKinds.Contains(kind) && IsCharaInParty(bctarget) && IsCharaInParty(bcsource))
                {
                    tagValue = sourceEffects.GetEffectOrDefault(FloatEffect.healing_pot);
                    tagValue = multi_sum(tagValue, sourceEffects.GetEffectOrDefault(FloatEffect.healing_up));
                    statusType = FlyTextStatusType.HealingUp;
                }

                matcher.AddData(statusType, tagValue, kind, val1, val2);

            } catch (Exception ex)
            {
                plugin.log.Warning(ex.ToString());
            }
        }

        internal void onFlyText(ref FlyTextKind kind, ref int val1, ref int val2, ref SeString text1, ref SeString text2, ref uint color, ref uint icon, ref uint damageTypeIcon, ref float yOffset, ref bool handled)
        {
            if (matcher.MatchAndPop(out FlyTextStatusType? _statusType, out float? _tagValue, kind, val1, val2)) {

                if (_tagValue!.Value > 0)
                {
                    string tagValue = to_percent(_tagValue!.Value);
                    string statusType = EnumStringAttribute.EnumToString(_statusType!.Value);

                    SeString icontext = new SeStringBuilder()
                        .Append(text2)
                        .AddText(string.Format("{0} {1}", statusType, tagValue))
                        .Build();

                    text2 = icontext;
                }

            } else {
                plugin.log.Warning("Couldn't match float text");
            }
        }

        internal class FlyTextMatcher()
        {
            List<FlyTextData> store = new List<FlyTextData>();

            internal void AddData(FlyTextStatusType statusType, float tagValue, FlyTextKind kind, int val1, int val2) {
                store.Add(new FlyTextData() { statusType = statusType, tagValue = tagValue, kind = kind, val1 = val1, val2 = val2 });
            }

            internal bool MatchAndPop(out FlyTextStatusType? statusType, out float? tagValue, FlyTextKind kind, int val1, int val2) {
                var outp = store.FindLast(x => x.Match(kind, val1, val2));

                if (outp == null)
                {
                    statusType = null;
                    tagValue = null;
                    return false;
                }
                else
                {
                    store.Remove(outp);
                    statusType = outp.statusType;
                    tagValue = outp.tagValue;
                    return true;
                }
            }
        }

        internal record FlyTextData
        {
            internal required FlyTextStatusType statusType;
            internal required float tagValue;

            internal required FlyTextKind kind;
            internal required int val1;
            internal required int val2;

            internal bool Match(FlyTextKind kind, int val1, int val2)
            {
                return (kind == this.kind) && (val1 == this.val1) && (val2 == this.val2);
            }
        }

        /// <summary>
        /// Represents what status effect should be shown on a flytext
        /// </summary>
        internal enum FlyTextStatusType
        {
            None,
            [EnumString("Damage Up")]
            DamageUp,
            [EnumString("Magic Damage Up")]
            MagiDamageUp,
            [EnumString("Physical Damage Up")]
            PhysDamageUp,
            [EnumString("Mitigation")]
            Mitigation,
            [EnumString("Magic Mitigation")]
            MagiMitigation,
            [EnumString("Physical Mitigation")]
            PhysMitigation,
            [EnumString("Healing Up")]
            HealingUp
        }

    }
}
