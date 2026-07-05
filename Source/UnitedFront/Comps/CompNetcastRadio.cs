using System.Collections.Generic;
using RimWorld;
using UnitedFront.Hediff;
using Verse;
using Verse.Sound;

namespace UnitedFront.Comps
{
    public class CompNetcastRadio : ThingComp
    {
        private int _currentIndex;
        private Sustainer _activeSustainer = null!;
        private SoundDef _activeSustainerSound = null!;

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
                StopSustainer();
                return;
            }
            if (parent.IsHashIntervalTick(Props.rerollInterval, delta))
            {
                Reroll();
            }
            NetcastBroadcast? broadcast = PowerOn ? CurrentBroadcast : null;
            MaintainSustainer(broadcast);
            if (!parent.IsHashIntervalTick(Props.tickRate, delta))
            {
                return;
            }
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
                if (!SameRoomAsParent(pawn))
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

        private void MaintainSustainer(NetcastBroadcast? broadcast)
        {
            SoundDef? desired = broadcast?.sound;
            if (desired != _activeSustainerSound)
            {
                StopSustainer();
                if (desired != null)
                {
                    _activeSustainer = desired.TrySpawnSustainer(SoundInfo.InMap(new TargetInfo(parent.Position, parent.Map)));
                    _activeSustainerSound = desired;
                }
            }
            _activeSustainer?.Maintain();
        }

        private void StopSustainer()
        {
            if (_activeSustainer != null)
            {
                _activeSustainer.End();
                _activeSustainer = null!;
            }
            _activeSustainerSound = null!;
        }

        public void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            StopSustainer();
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
            Verse.Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(Props.hediffDef, pawn);
                hediff.Severity = 1f;
                pawn.health.AddHediff(hediff);
            }
            HediffCompNetcastJoy joyComp = hediff.TryGetComp<HediffCompNetcastJoy>();
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
            Verse.Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
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
            if (Props.drawRadiusRing)
            {
                GenDraw.DrawRadiusRing(parent.Position, Props.radius);
            }
        }
    }
}
