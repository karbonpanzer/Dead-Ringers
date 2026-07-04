using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UnitedFront.Comps
{
    public class CompSpawnerSelectable : ThingComp
    {
        private int _ticksUntilSpawn;
        private int _selectedIndex;

        public CompPropertiesSpawnerSelectable Props
            => (CompPropertiesSpawnerSelectable)props;

        private ThingDefCountClass CurrentOption
            => Props.spawnOptions[_selectedIndex];

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                ResetTimer();
            }
        }

        private void ResetTimer()
        {
            _ticksUntilSpawn = Props.spawnIntervalRange.RandomInRange;
        }

        public override void CompTick()
        {
            if (Props.requiresPower)
            {
                CompPowerTrader power = parent.TryGetComp<CompPowerTrader>();
                if (power is { PowerOn: false })
                {
                    return;
                }
            }

            _ticksUntilSpawn--;
            if (_ticksUntilSpawn <= 0)
            {
                TryDoSpawn();
                ResetTimer();
            }
        }

        private void TryDoSpawn()
        {
            ThingDefCountClass option = CurrentOption;

            if (Props.spawnMaxAdjacent >= 0)
            {
                int adjacent = 0;
                foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(parent))
                {
                    if (cell.InBounds(parent.Map))
                    {
                        List<Thing> thingsAt = cell.GetThingList(parent.Map);
                        foreach (var t in thingsAt)
                        {
                            if (t.def == option.thingDef)
                            {
                                adjacent += t.stackCount;
                            }
                        }
                    }
                }
                if (adjacent >= Props.spawnMaxAdjacent)
                {
                    return;
                }
            }

            Thing thing = ThingMaker.MakeThing(option.thingDef);
            thing.stackCount = option.count;

            if (Props.inheritFaction && parent.Faction != null)
            {
                thing.SetFaction(parent.Faction);
            }

            if (Props.spawnForbidden)
            {
                thing.SetForbidden(true, false);
            }

            if (!GenPlace.TryPlaceThing(thing, parent.Position, parent.Map,
                ThingPlaceMode.Near))
            {
                thing.Destroy();
                return;
            }

            if (Props.showMessageIfOwned && parent.Faction == Faction.OfPlayer)
            {
                Messages.Message(
                    "MessageCompSpawnerSpawnedItem".Translate(
                        option.thingDef.LabelCap),
                    thing,
                    MessageTypeDefOf.PositiveEvent);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            ThingDefCountClass option = CurrentOption;

            yield return new Command_Action
            {
                defaultLabel = "UFR_FoodSynth_GizmoLabel".Translate(
                    option.thingDef.LabelCap),
                defaultDesc = "UFR_FoodSynth_GizmoDesc".Translate(
                    option.thingDef.LabelCap,
                    option.count),
                icon = GetOptionIcon(option),
                action = delegate
                {
                    List<FloatMenuOption> menuOptions = new List<FloatMenuOption>();
                    for (int i = 0; i < Props.spawnOptions.Count; i++)
                    {
                        int localIndex = i;
                        ThingDefCountClass opt = Props.spawnOptions[i];
                        string label;
                        if (i == _selectedIndex)
                        {
                            label = "UFR_FoodSynth_CurrentMarker".Translate(
                                opt.thingDef.LabelCap,
                                opt.count);
                        }
                        else
                        {
                            label = "UFR_FoodSynth_MenuOption".Translate(
                                opt.thingDef.LabelCap,
                                opt.count);
                        }
                        menuOptions.Add(new FloatMenuOption(label, delegate
                        {
                            _selectedIndex = localIndex;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(menuOptions));
                }
            };
        }

        private static Texture2D GetOptionIcon(ThingDefCountClass option)
        {
            if (option.thingDef.uiIcon != null)
            {
                return option.thingDef.uiIcon;
            }
            return BaseContent.BadTex;
        }

        public override string CompInspectStringExtra()
        {
            ThingDefCountClass option = CurrentOption;
            string text = "UFR_FoodSynth_Producing".Translate(
                option.thingDef.LabelCap,
                option.count);

            if (Props.writeTimeLeftToSpawn)
            {
                text += "\n" + "NextSpawnedItemIn".Translate(
                    _ticksUntilSpawn.ToStringTicksToPeriod());
            }

            return text;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            string prefix = Props.saveKeysPrefix;
            Scribe_Values.Look(ref _ticksUntilSpawn,
                prefix + "_ticksUntilSpawn");
            Scribe_Values.Look(ref _selectedIndex,
                prefix + "_selectedIndex");
        }
    }
}