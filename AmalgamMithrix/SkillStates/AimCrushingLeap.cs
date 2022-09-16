using EntityStates.Huntress;
using KinematicCharacterController;
using RoR2;
using UnityEngine;
using EntityStates;

namespace AmalgamMithrix
{
  public class AimCrushingLeap : BaseSkillState
  {
    public static GameObject areaIndicatorPrefab = ArrowRain.areaIndicatorPrefab;
    public float maxDuration = 2f;
    private CharacterModel characterModel;
    private HurtBoxGroup hurtboxGroup;
    private GameObject areaIndicatorInstance;

    public override void OnEnter()
    {
      base.OnEnter();
      Transform modelTransform = ((EntityState)this).GetModelTransform();
      if ((bool)(Object)modelTransform)
      {
        this.characterModel = modelTransform.GetComponent<CharacterModel>();
        this.hurtboxGroup = modelTransform.GetComponent<HurtBoxGroup>();
      }
      if ((bool)(Object)this.characterModel)
        ++this.characterModel.invisibilityCount;
      if ((bool)(Object)this.hurtboxGroup)
        ++this.hurtboxGroup.hurtBoxesDeactivatorCounter;
      ((EntityState)this).characterBody.AddBuff((BuffIndex)3);
      int num = (int)Util.PlaySound("Play_moonBrother_phaseJump_land_preWhoosh", ((EntityState)this).gameObject);
      ((EntityState)this).gameObject.layer = LayerIndex.fakeActor.intVal;
      ((BaseCharacterController)((EntityState)this).characterMotor).Motor.RebuildCollidableLayers();
      ((EntityState)this).characterMotor.velocity = Vector3.zero;
      ((BaseCharacterController)((EntityState)this).characterMotor).Motor.SetPosition(new Vector3(((Component)((EntityState)this).characterMotor).transform.position.x, ((Component)((EntityState)this).characterMotor).transform.position.y + 25f, ((Component)((EntityState)this).characterMotor).transform.position.z), true);
      if (!(bool)(Object)AimCrushingLeap.areaIndicatorPrefab)
        return;
      this.areaIndicatorInstance = Object.Instantiate<GameObject>(AimCrushingLeap.areaIndicatorPrefab);
      this.areaIndicatorInstance.transform.localScale = new Vector3(ArrowRain.arrowRainRadius, ArrowRain.arrowRainRadius, ArrowRain.arrowRainRadius);
    }

    private void UpdateAreaIndicator()
    {
      if (!(bool)(Object)this.areaIndicatorInstance)
        return;
      float num = 2000f;
      Ray aimRay = ((BaseState)this).GetAimRay();
      RaycastHit raycastHit;
      double maxDistance = (double)num;
      LayerIndex world = LayerIndex.world;
      int mask = (int)((LayerIndex)world).mask;
      if (!Physics.Raycast(aimRay, out raycastHit, (float)maxDistance, mask))
        return;
      this.areaIndicatorInstance.transform.position = raycastHit.point;
      this.areaIndicatorInstance.transform.up = raycastHit.normal;
    }

    public void HandleFollowupAttack()
    {
      this.characterMotor.Motor.SetPosition(new Vector3(this.areaIndicatorInstance.transform.position.x, this.areaIndicatorInstance.transform.position.y + 1f, this.areaIndicatorInstance.transform.position.z), true);
      this.outer.SetNextState(new ExitCrushingLeap());
    }

    public override void Update()
    {
      base.Update();
      this.UpdateAreaIndicator();
    }

    public override void FixedUpdate()
    {
      base.FixedUpdate();
      if ((bool)(Object)this.characterMotor)
        this.characterMotor.velocity = Vector3.zero;
      if (!this.isAuthority || !(bool)(Object)((EntityState)this).inputBank || (double)((EntityState)this).fixedAge < (double)this.maxDuration && !((InputBankTest.ButtonState)((EntityState)this).inputBank.skill1).justPressed && !((InputBankTest.ButtonState)((EntityState)this).inputBank.skill4).justPressed)
        return;
      this.HandleFollowupAttack();
    }

    public override void OnExit()
    {
      if ((bool)(Object)this.characterModel)
        --this.characterModel.invisibilityCount;
      if ((bool)(Object)this.hurtboxGroup)
        --this.hurtboxGroup.hurtBoxesDeactivatorCounter;
      ((EntityState)this).characterBody.RemoveBuff((BuffIndex)3);
      ((EntityState)this).gameObject.layer = LayerIndex.defaultLayer.intVal;
      this.characterMotor.Motor.RebuildCollidableLayers();
      if ((bool)(Object)this.areaIndicatorInstance)
        EntityState.Destroy((Object)this.areaIndicatorInstance.gameObject);
      base.OnExit();
    }
  }
}
