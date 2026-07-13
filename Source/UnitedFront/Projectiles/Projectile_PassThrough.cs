using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace UnitedFront.Projectiles
{
    public class PassThroughExtension : DefModExtension
    {
        public int maxPasses = 1;

        public float damageFalloff = 0.6f;

        public float overshootDistance = 4f;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors())
            {
                yield return e;
            }

            if (maxPasses < 1)
            {
                yield return "maxPasses should be at least 1";
            }
            if (damageFalloff < 0f || damageFalloff > 1f)
            {
                yield return "damageFalloff should be from 0 to 1";
            }
            if (overshootDistance <= 0f)
            {
                yield return "overshootDistance should be positive";
            }
        }
    }

    public class Projectile_PassThrough : Bullet
    {
        private int passesUsed;
        private List<Thing> alreadyHit = new List<Thing>();

        private PassThroughExtension Ext => def.GetModExtension<PassThroughExtension>();

        public override int DamageAmount
        {
            get
            {
                int baseDamage = base.DamageAmount;
                PassThroughExtension ext = Ext;

                if (ext == null || passesUsed <= 0)
                {
                    return baseDamage;
                }

                float scaled = baseDamage * Mathf.Pow(ext.damageFalloff, passesUsed);
                return Mathf.Max(1, Mathf.RoundToInt(scaled));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref passesUsed, "passesUsed", 0);
            Scribe_Collections.Look(ref alreadyHit, "alreadyHit", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && alreadyHit == null)
            {
                alreadyHit = new List<Thing>();
            }
        }

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget,
            LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags,
            bool preventFriendlyFire = false, Thing? equipment = null, ThingDef? targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags,
                preventFriendlyFire, equipment, targetCoverDef);

            PassThroughExtension ext = Ext;
            if (ext == null)
            {
                return;
            }

            Vector3 heading = (destination - origin).Yto0();
            if (heading.sqrMagnitude > 0.0001f)
            {
                destination += heading.normalized * ext.overshootDistance;

                ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
                if (ticksToImpact < 1)
                {
                    ticksToImpact = 1;
                }
                lifetime = ticksToImpact;
            }

            HitFlags = ProjectileHitFlags.IntendedTarget | ProjectileHitFlags.NonTargetPawns;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            PassThroughExtension ext = Ext;

            if (hitThing != null && alreadyHit.Contains(hitThing))
            {
                return;
            }

            bool canPass = ext != null
                        && !blockedByShield
                        && hitThing is Pawn
                        && passesUsed < ext.maxPasses
                        && ticksToImpact > 0;

            if (!canPass)
            {
                base.Impact(hitThing, blockedByShield);
                return;
            }

            if (hitThing != null)
            {
                alreadyHit.Add(hitThing);
                ApplyDamage(hitThing);
            }

            passesUsed++;

        }

        private void ApplyDamage(Thing hitThing)
        {
            BattleLogEntry_RangedImpact log = new BattleLogEntry_RangedImpact(
                launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
            Find.BattleLog.Add(log);

            bool instigatorGuilty = !(launcher is Pawn pawn) || !pawn.Drafted;

            DamageInfo dinfo = new DamageInfo(
                DamageDef,
                DamageAmount,
                ArmorPenetration,
                ExactRotation.eulerAngles.y,
                launcher,
                null,
                equipmentDef,
                DamageInfo.SourceCategory.ThingOrUnknown,
                intendedTarget.Thing,
                instigatorGuilty);

            dinfo.SetWeaponQuality(equipmentQuality);
            hitThing.TakeDamage(dinfo).AssociateWithLog(log);

            (hitThing as Pawn)?.stances?.stagger.Notify_BulletImpact(this);

            if (ExtraDamages == null)
            {
                return;
            }

            foreach (ExtraDamage extra in ExtraDamages)
            {
                if (!Rand.Chance(extra.chance))
                {
                    continue;
                }

                DamageInfo extraInfo = new DamageInfo(
                    extra.def,
                    extra.amount,
                    extra.AdjustedArmorPenetration(),
                    ExactRotation.eulerAngles.y,
                    launcher,
                    null,
                    equipmentDef,
                    DamageInfo.SourceCategory.ThingOrUnknown,
                    intendedTarget.Thing,
                    instigatorGuilty);

                hitThing.TakeDamage(extraInfo).AssociateWithLog(log);
            }
        }
    }
}
