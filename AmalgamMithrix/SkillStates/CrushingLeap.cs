using EntityStates.BrotherMonster;
using RoR2;
using UnityEngine;
using EntityStates;

namespace AmalgamMithrix
{
  public class CrushingLeap : BaseSkillState
  {
    public static float damageCoefficient = 20f;
    public static float projectileDamageCoefficient = 5f;
    public static float projectileCount = 8f;
    public static float projectileForce = 5f;
    public static float baseDuration = 0.5f;
    private float duration;

    public override void OnEnter()
    {
      base.OnEnter();
      int num = (int)Util.PlaySound(EnterSkyLeap.soundString, ((EntityState)this).gameObject);
      this.duration = CrushingLeap.baseDuration / ((BaseState)this).attackSpeedStat;
      ((EntityState)this).PlayAnimation("Body", "EnterSkyLeap", "SkyLeap.playbackRate", this.duration);
      ((EntityState)this).PlayAnimation("FullBody Override", "BufferEmpty");
      ((EntityState)this).characterDirection.moveVector = ((EntityState)this).characterDirection.forward;
      ((EntityState)this).characterBody.AddTimedBuff((BuffIndex)1, EnterSkyLeap.baseDuration);
      AimAnimator aimAnimator = ((EntityState)this).GetAimAnimator();
      if (!(bool)(Object)aimAnimator)
        return;
      ((Behaviour)aimAnimator).enabled = true;
    }

    public override void FixedUpdate()
    {
      base.FixedUpdate();
      if (!((EntityState)this).isAuthority || (double)((EntityState)this).fixedAge <= (double)this.duration)
        return;
      ((EntityState)this).outer.SetNextState((EntityState)new AimCrushingLeap());
    }
  }
}
