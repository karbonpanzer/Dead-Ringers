using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace UnitedFront.Comps
{
    public class CompProperties_ShieldUFR : CompProperties_Shield
    {
        public Material BubbleMat = null!;

        public string bubbleTexPath = null!;

        public List<DamageDef> extraBlockedDamageDefs = new List<DamageDef>();

        public bool blocksExplosiveDamage = true;

        public float lowEnergyPulseThreshold = 0.25f;
        
        public bool randomizeRotation = true;
        
        public float breakFlashScale = 2f;
        
        public int breakDustPuffs = 0;

        public float breakEffecterScale = 1f;

        public CompProperties_ShieldUFR()
        {
            compClass = typeof(CompShieldUFR);

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                string path = bubbleTexPath.NullOrEmpty() ? "Other/ShieldBubble" : bubbleTexPath;
                BubbleMat = MaterialPool.MatFrom(path, ShaderDatabase.TransparentPostLight);
            });
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string item in base.ConfigErrors(parentDef))
            {
                yield return item;
            }

            if (lowEnergyPulseThreshold < 0f || lowEnergyPulseThreshold > 1f)
            {
                yield return "lowEnergyPulseThreshold should be from 0 to 1";
            }
        }
    }

    public class CompShieldUFR : CompShield
    {
        private const float MaxDamagedJitterDist = 0.06f;
        private const int JitterDurationTicks = 8;

        private Vector3 incomingAngleVect;
        private int lastAbsorbDamageTick = -9999;

        private CompProperties_ShieldUFR PropsUFR => (CompProperties_ShieldUFR)props;

        private float EnergyMax => parent.GetStatValue(StatDefOf.EnergyShieldEnergyMax);

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            if (ShieldState != ShieldState.Active || PawnOwner == null)
            {
                return;
            }

            if (dinfo.Def == DamageDefOf.EMP)
            {
                energy = 0f;
                BreakShield();
                return;
            }

            if (dinfo.Def.ignoreShields)
            {
                return;
            }

            bool blocked = dinfo.Def.isRanged
                        || (dinfo.Def.isExplosive && PropsUFR.blocksExplosiveDamage)
                        || (PropsUFR.extraBlockedDamageDefs != null
                            && PropsUFR.extraBlockedDamageDefs.Contains(dinfo.Def));

            if (!blocked)
            {
                return;
            }

            energy -= dinfo.Amount * Props.energyLossPerDamage;

            if (energy <= 0f)
            {
                BreakShield();
            }
            else
            {
                AbsorbDamage(dinfo);
            }

            absorbed = true;
        }

        private void AbsorbDamage(DamageInfo dinfo)
        {
            if (PawnOwner.Spawned)
            {
                incomingAngleVect = Vector3Utility.HorizontalVectorFromAngle(dinfo.Angle);
                Vector3 loc = PawnOwner.TrueCenter() + incomingAngleVect.RotatedBy(180f) * 0.5f;

                SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(
                    new TargetInfo(PawnOwner.Position, PawnOwner.Map));

                float scale = Mathf.Min(10f, 2f + dinfo.Amount / 10f);
                FleckMaker.Static(loc, PawnOwner.Map, FleckDefOf.ExplosionFlash, scale);

                int puffs = (int)scale;
                for (int i = 0; i < puffs; i++)
                {
                    FleckMaker.ThrowDustPuff(loc, PawnOwner.Map, Rand.Range(0.8f, 1.2f));
                }
            }

            lastAbsorbDamageTick = Find.TickManager.TicksGame;
            KeepDisplaying();
        }

        private void BreakShield()
        {
            if (PawnOwner != null && PawnOwner.Spawned)
            {
                EffecterDefOf.Shield_Break.SpawnAttached(
                    parent, parent.MapHeld, PropsUFR.breakEffecterScale);

                if (PropsUFR.breakFlashScale > 0f)
                {
                    FleckMaker.Static(PawnOwner.TrueCenter(), PawnOwner.Map,
                        FleckDefOf.ExplosionFlash, PropsUFR.breakFlashScale);
                }

                for (int i = 0; i < PropsUFR.breakDustPuffs; i++)
                {
                    Vector3 offset = Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360))
                                     * Rand.Range(0.3f, 0.6f);
                    FleckMaker.ThrowDustPuff(PawnOwner.TrueCenter() + offset, PawnOwner.Map,
                        Rand.Range(0.8f, 1.2f));
                }
            }

            energy = 0f;
            ticksToReset = Props.startingTicksToReset;
        }

        private void DrawBubble()
        {
            if (ShieldState != ShieldState.Active || !ShouldDisplay)
            {
                return;
            }

            float size = Mathf.Lerp(Props.minDrawSize, Props.maxDrawSize, energy);

            Vector3 drawPos = PawnOwner.Drawer.DrawPos;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            int sinceHit = Find.TickManager.TicksGame - lastAbsorbDamageTick;
            if (sinceHit < JitterDurationTicks)
            {
                float jitter = (float)(JitterDurationTicks - sinceHit)
                               / JitterDurationTicks * MaxDamagedJitterDist;
                drawPos += incomingAngleVect * jitter;
                size -= jitter;
            }

            float angle = PropsUFR.randomizeRotation ? Rand.Range(0, 360) : 0f;

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(
                drawPos,
                Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(size, 1f, size));

            Material mat = PropsUFR.BubbleMat;
            if (mat == null)
            {
                return;
            }

            if (energy <= PropsUFR.lowEnergyPulseThreshold * EnergyMax)
            {
                mat = FadedMaterialPool.FadedVersionOf(
                    mat, 0.5f + 0.5f * Mathf.PingPong(Time.time, 1f));
            }

            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }

        public override void CompDrawWornExtras()
        {
            if (IsApparel)
            {
                DrawBubble();
            }
        }

        public override void PostDraw()
        {
            if (!IsApparel)
            {
                DrawBubble();
            }
        }
    }
}