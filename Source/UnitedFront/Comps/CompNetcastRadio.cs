using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UnitedFront.Comps
{
    public class CompNetcastRadio : ThingComp
    {
        private int currentIndex;

        public CompPropertiesNetcastRadio Props => (CompPropertiesNetcastRadio)props;

        private CompPowerTrader PowerTrader => parent.TryGetComp<CompPowerTrader>();

        private bool PowerOn => !Props.requiresPower || PowerTrader == null || PowerTrader.PowerOn;

        private NetcastBroadcast CurrentBroadcast
        {
            get
            {
                if (Props.broadcasts.NullOrEmpty() || currentIndex < 0 || currentIndex >= Props.broadcasts.Count)
                {
                    return null!;
                }
                return Props.broadcasts[currentIndex];
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                Reroll();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentIndex, "currentIndex", 0);
        }

        private void Reroll()
        {
            NetcastBroadcast picked = Props.broadcasts.RandomElementByWeight(b => b.weight);
            currentIndex = Props.broadcasts.IndexOf(picked);
        }

        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (!parent.Spawned)
            {
                return;
            }
            if (parent.IsHashIntervalTick(Props.rerollInterval, delta))
            {
                Reroll();
            }
            if (!parent.IsHashIntervalTick(Props.tickRate, delta))
            {
                return;
            }
            if (!PowerOn)
            {
                return;
            }
            NetcastBroadcast broadcast = CurrentBroadcast;
            if (broadcast == null)
            {
                return;
            }
            float radiusSquared = Props.radius * Props.radius;
            IReadOnlyList<Pawn> pawns = parent.Map.mapPawns.AllPawnsSpawned;
            for (int i = pawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = pawns[i];
                if (!ValidPawn(pawn))
                {
                    continue;
                }
                if (pawn.Position.DistanceToSquared(parent.Position) > radiusSquared)
                {
                    continue;
                }
                GiveOrRefreshJoy(pawn, broadcast);
                if (broadcast.bonusHediff != null)
                {
                    GiveOrRefreshHediff(pawn, broadcast.bonusHediff);
                }
            }
        }

        private bool ValidPawn(Pawn pawn)
        {
            if (pawn.Dead || pawn.needs?.joy == null)
            {
                return false;
            }
            if (pawn.RaceProps.Humanlike)
            {
                return Props.allowHumanlike;
            }
            if (pawn.RaceProps.Animal)
            {
                return Props.allowAnimals;
            }
            return false;
        }

        private void GiveOrRefreshJoy(Pawn pawn, NetcastBroadcast broadcast)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(Props.hediffDef, pawn);
                hediff.Severity = 1f;
                pawn.health.AddHediff(hediff);
            }
            UnitedFront.Health.HediffCompNetcastJoy joyComp = hediff.TryGetComp<UnitedFront.Health.HediffCompNetcastJoy>();
            if (joyComp != null)
            {
                joyComp.joyGainRate = broadcast.joyGainRate;
                joyComp.joyKind = Props.joyKind;
            }
            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = Props.tickRate + 5;
            }
        }

        private void GiveOrRefreshHediff(Pawn pawn, HediffDef def)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(def, pawn);
                hediff.Severity = 1f;
                pawn.health.AddHediff(hediff);
            }
            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = Props.tickRate + 5;
            }
        }


        public override string CompInspectStringExtra()
        {
            NetcastBroadcast broadcast = CurrentBroadcast;
            if (broadcast == null)
            {
                return null!;
            }
            return "UFR_NowBroadcasting".Translate(broadcast.label);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (Props.drawRadiusRing)
            {
                GenDraw.DrawRadiusRing(parent.Position, Props.radius);
            }
        }
    }
}
