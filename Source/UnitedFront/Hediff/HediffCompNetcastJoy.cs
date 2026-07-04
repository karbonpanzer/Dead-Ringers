using RimWorld;
using UnityEngine;
using Verse;

namespace UnitedFront.Health
{
    public class HediffCompProperties_NetcastJoy : HediffCompProperties
    {
        public HediffCompProperties_NetcastJoy()
        {
            compClass = typeof(HediffCompNetcastJoy);
        }
    }

    public class HediffCompNetcastJoy : HediffComp
    {
        public float joyGainRate = 1f;
        public JoyKindDef joyKind = null!;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            Need_Joy joy = Pawn.needs?.joy;
            if (joy == null)
            {
                return;
            }
            float amount = joyGainRate * delta * (JoyTunings.BaseJoyGainPerHour / GenDate.TicksPerHour);
            if (amount <= 0f)
            {
                return;
            }
            if (joyKind != null)
            {
                amount *= joy.tolerances.JoyFactorFromTolerance(joyKind);
            }
            amount = Mathf.Min(amount, 1f - joy.CurLevel);
            if (amount <= 0f)
            {
                return;
            }
            joy.CurLevel += amount;
            if (joyKind != null)
            {
                joy.tolerances.Notify_JoyGained(amount, joyKind);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref joyGainRate, "joyGainRate", 1f);
            Scribe_Defs.Look(ref joyKind, "joyKind");
        }
    }
}
