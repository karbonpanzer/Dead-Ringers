using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UnitedFront.Comps
{
    public class CompNetcastRadio : ThingComp
    {
        private static readonly List<IntVec3> _tmpCells = new List<IntVec3>();

        private int _currentIndex;

        public CompPropertiesNetcastRadio Props => (CompPropertiesNetcastRadio)props;

        private CompPowerTrader PowerTrader => parent.TryGetComp<CompPowerTrader>();

        private bool PowerOn => !Props.requiresPower || PowerTrader.PowerOn;

        private NetcastBroadcast CurrentBroadcast
        {
            get
            {
                if (Props.broadcasts.NullOrEmpty() || _currentIndex < 0 || _currentIndex >= Props.broadcasts.Count)
                {
                    return null!;
                }
                return Props.broadcasts[_currentIndex];
            }
        }

        private float JoyPerTick => JoyTunings.BaseJoyGainPerHour / GenDate.TicksPerHour;

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
            Scribe_Values.Look(ref _currentIndex, "currentIndex");
        }

        private void Reroll()
        {
            NetcastBroadcast picked = Props.broadcasts.RandomElementByWeight(b => b.weight);
            _currentIndex = Props.broadcasts.IndexOf(picked);
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
                TryApplyBroadcast(pawn, broadcast, radiusSquared);
                if (pawn.carryTracker.CarriedThing is Pawn carried)
                {
                    TryApplyBroadcast(carried, broadcast, radiusSquared);
                }
            }
        }

        private void TryApplyBroadcast(Pawn pawn, NetcastBroadcast broadcast, float radiusSquared)
        {
            if (!ValidPawn(pawn))
            {
                return;
            }
            if (pawn.Position.DistanceToSquared(parent.Position) > radiusSquared)
            {
                return;
            }
            if (!SameRoomAsParent(pawn))
            {
                return;
            }
            float joyAmount = broadcast.joyGainRate * Props.tickRate * JoyPerTick;
            pawn.needs.joy.GainJoy(joyAmount, Props.joyKind);
            if (broadcast.bonusHediff != null)
            {
                GiveOrRefreshHediff(pawn, broadcast.bonusHediff);
            }
        }

        private bool SameRoomAsParent(Pawn pawn)
        {
            Room parentRoom = parent.GetRoom();
            if (parentRoom == null)
            {
                return true;
            }
            Room pawnRoom = pawn.GetRoom();
            return pawnRoom == parentRoom;
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
            if (disappears == null)
            {
                Log.ErrorOnce("CompNetcastRadio has a broadcast bonusHediff without a HediffComp_Disappears: " + def.defName, 74829612);
            }
            else
            {
                disappears.ticksToDisappear = Props.tickRate + 5;
            }
        }

        public override string CompInspectStringExtra()
        {
            NetcastBroadcast broadcast = CurrentBroadcast;
            return "UFR_NowBroadcasting".Translate(broadcast.label);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (DebugSettings.godMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Skip broadcast",
                    defaultDesc = "Reroll the current broadcast immediately.",
                    action = Reroll
                };
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (!Props.drawRadiusRing)
            {
                return;
            }
            _tmpCells.Clear();
            Room parentRoom = parent.GetRoom();
            int num = GenRadial.NumCellsInRadius(Props.radius);
            for (int i = 0; i < num; i++)
            {
                IntVec3 cell = parent.Position + GenRadial.RadialPattern[i];
                if (!cell.InBounds(parent.Map))
                {
                    continue;
                }
                if (parentRoom != null && cell.GetRoom(parent.Map) != parentRoom)
                {
                    continue;
                }
                _tmpCells.Add(cell);
            }
            GenDraw.DrawFieldEdges(_tmpCells);
        }
    }
}
