using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UnitedFront.Recipes
{

    public class RecipeExtractCulture : Recipe_Surgery
    {
        public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord part = null!)
        {
            if (thing is Pawn pawn && pawn.DevelopmentalStage.Baby())
            {
                return "TooSmall".Translate();
            }
            return base.AvailableReport(thing, part);
        }

        public override void ApplyOnPawn(
            Pawn pawn,
            BodyPartRecord part,
            Pawn billDoer,
            List<Thing> ingredients,
            Bill bill)
        {

            if (recipe.addsHediff != null)
            {
                Hediff hediff = HediffMaker.MakeHediff(recipe.addsHediff, pawn);
                pawn.health.AddHediff(hediff);
            }
            
            OnSurgerySuccess(pawn, part, billDoer, ingredients, bill);
            
            if (IsViolationOnPawn(pawn, part, Faction.OfPlayer))
            {
                ReportViolation(pawn, billDoer, pawn.HomeFaction, -1);
            }
        }

        protected override void OnSurgerySuccess(
            Pawn pawn,
            BodyPartRecord part,
            Pawn billDoer,
            List<Thing> ingredients,
            Bill bill)
        {
            ThingDef cultureDef = DefDatabase<ThingDef>.GetNamedSilentFail("UFR_LabGrownCulture");
            if (cultureDef == null)
            {
                Log.Error("[UFR] Recipe_ExtractCulture: ThingDef 'UFR_LabGrownCulture' not found in DefDatabase.");
                return;
            }

            if (!GenPlace.TryPlaceThing(
                    ThingMaker.MakeThing(cultureDef),
                    pawn.PositionHeld,
                    pawn.MapHeld,
                    ThingPlaceMode.Near))
            {
                Log.Error("[UFR] Could not drop lab-grown culture near " + pawn.PositionHeld.ToString());
            }
        }
    }
}