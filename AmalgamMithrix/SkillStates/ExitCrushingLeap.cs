using EntityStates.BrotherMonster;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using EntityStates;

namespace AmalgamMithrix
{
  public class ExitCrushingLeap : BaseSkillState
  {
    private float duration;
    private float baseDuration = 0.5f;

    public override void OnEnter()
    {
      base.OnEnter();
      this.duration = ExitSkyLeap.baseDuration / this.attackSpeedStat;
      int num = (int)Util.PlaySound(ExitSkyLeap.soundString, this.gameObject);
      this.PlayAnimation("Body", nameof(ExitSkyLeap), "SkyLeap.playbackRate", this.duration);
      this.PlayAnimation("FullBody Override", "BufferEmpty");
      this.characterBody.AddTimedBuff(RoR2Content.Buffs.ArmorBoost, ExitSkyLeap.baseDuration);
      AimAnimator aimAnimator = ((EntityState)this).GetAimAnimator();
      if ((bool)(Object)aimAnimator)
        ((Behaviour)aimAnimator).enabled = true;
      float num2 = 360f / (float)ExitSkyLeap.waveProjectileCount;
      Vector3 vector3_1 = Vector3.ProjectOnPlane(((EntityState)this).inputBank.aimDirection, Vector3.up);
      Vector3 footPosition = ((EntityState)this).characterBody.footPosition;
      for (int index = 0; index < ExitSkyLeap.waveProjectileCount; ++index)
      {
        Vector3 vector3_2 = Quaternion.AngleAxis(num2 * (float)index, Vector3.up) * vector3_1;
        ProjectileManager.instance.FireProjectile(ExitSkyLeap.waveProjectilePrefab, footPosition, Util.QuaternionSafeLookRotation(vector3_2), ((EntityState)this).gameObject, ((EntityState)this).characterBody.damage * ExitSkyLeap.waveProjectileDamageCoefficient, ExitSkyLeap.waveProjectileForce, Util.CheckRoll(((EntityState)this).characterBody.crit, ((EntityState)this).characterBody.master), (DamageColorIndex)0, (GameObject)null, -1f);
      }
      GenericSkill genericSkill = (bool)this.skillLocator ? this.skillLocator.special : null;
      if (!(bool)genericSkill)
        return;
      UltChannelState.replacementSkillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(UltEnterState));
      genericSkill.SetSkillOverride(this.outer, UltChannelState.replacementSkillDef, GenericSkill.SkillOverridePriority.Contextual);
    }

    public override void FixedUpdate()
    {
      base.FixedUpdate();
      if (!this.isAuthority)
        return;
      if ((double)this.fixedAge <= (double)this.duration)
        return;
      this.outer.SetNextStateToMain();
    }
  }
}