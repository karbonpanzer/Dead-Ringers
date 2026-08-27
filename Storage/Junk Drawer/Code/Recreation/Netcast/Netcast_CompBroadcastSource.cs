using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UnitedFront.Comps
{
    public class CompBroadcastSource : ThingComp
    {
        private int _currentIndex;

        public CompPropertiesBroadcastSource Props => (CompPropertiesBroadcastSource)props;

        public NetcastBroadcast CurrentBroadcast
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
                return;
            }
            if (parent.IsHashIntervalTick(Props.rerollInterval, delta))
            {
                Reroll();
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
    }
}
