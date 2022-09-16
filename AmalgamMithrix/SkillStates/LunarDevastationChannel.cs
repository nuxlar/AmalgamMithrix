using RoR2;
using RoR2.Projectile;
using RoR2.Navigation;
using UnityEngine;
using EntityStates.ClayBoss;
using EntityStates.ClayBoss.ClayBossWeapon;
using UnityEngine.AddressableAssets;
using EntityStates.BrotherHaunt;
using EntityStates.BrotherMonster;
using AmalgamMithrix;
using System.Collections.Generic;

public class LunarDevastationChannel : EntityStates.BaseState
{
  static GameObject golemProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarGolem/LunarGolemTwinShotProjectile.prefab").WaitForCompletion();
  public int totalWaves = ModConfig.P2UltCount.Value;
  public float maxDuration = ModConfig.P2UltDuration.Value;
  private int wavesFired;
  private int charges;
  private float chargeTimer;
  private int grenadeCountMax = 6;
  public static int tarballCountMax = 6;
  public static float baseTimeBetweenShots = 0.5f;
  private int grenadeCount;
  private int tarballCount;
  private float fireTarballTimer;
  private float fireGrenadeTimer;
  private float timeBetweenShots;
  private Transform modelTransform;
  private GameObject channelEffectInstance;

  public override void OnEnter()
  {
    this.charges = FireRandomProjectiles.initialCharges;
    this.modelTransform = this.GetModelTransform();
    int num = (int)Util.PlaySound(UltChannelState.enterSoundString, this.gameObject);
    this.timeBetweenShots = FireBombardment.baseTimeBetweenShots / this.attackSpeedStat;
    Transform modelChild = this.FindModelChild("MuzzleUlt");
    if ((bool)(Object)modelChild && (bool)(Object)UltChannelState.channelEffectPrefab)
      this.channelEffectInstance = Object.Instantiate<GameObject>(UltChannelState.channelEffectPrefab, modelChild.position, Quaternion.identity, modelChild);
    if (!(bool)(Object)UltChannelState.channelBeginMuzzleflashEffectPrefab)
      return;
    EffectManager.SimpleMuzzleFlash(UltChannelState.channelBeginMuzzleflashEffectPrefab, this.gameObject, "MuzzleUlt", false);
  }

  private void FireWave()
  {
    ++this.wavesFired;
    float num = 360f / ModConfig.P2UltOrbCount.Value;
    Vector3 point = Vector3.ProjectOnPlane(this.inputBank.aimDirection, Vector3.up);
    Vector3 footPosition = this.characterBody.footPosition;
    Vector3 corePosition = this.characterBody.corePosition;
    Util.PlaySound(ExitSkyLeap.soundString, this.gameObject);
    EffectManager.SimpleMuzzleFlash(FistSlam.slamImpactEffect, this.gameObject, FistSlam.muzzleString, false);
    for (int idx = 0; idx < ModConfig.P2UltOrbCount.Value; ++idx)
    {
      Vector3 forward = Quaternion.AngleAxis(num * idx, Vector3.up) * point;
      ProjectileManager.instance.FireProjectile(golemProjectile, corePosition, Quaternion.LookRotation(forward), this.gameObject, (this.characterBody.damage * FistSlam.waveProjectileDamageCoefficient) / 4, 0f, Util.CheckRoll(this.characterBody.crit, this.characterBody.master), DamageColorIndex.Default, null, -1f);
      ProjectileManager.instance.FireProjectile(ExitSkyLeap.waveProjectilePrefab, footPosition, Util.QuaternionSafeLookRotation(forward), this.gameObject, this.characterBody.damage * ExitSkyLeap.waveProjectileDamageCoefficient, ExitSkyLeap.waveProjectileForce, Util.CheckRoll(this.characterBody.crit, this.characterBody.master));
    }
  }

  private void FireSingleTarball(string targetMuzzle)
  {
    this.PlayCrossfade("Body", "FireTarBall", 0.1f);
    int num = (int)Util.PlaySound(FireTarball.attackSoundString, this.gameObject);
    Ray aimRay = this.GetAimRay();
    if ((bool)(Object)this.modelTransform)
    {
      ChildLocator component = this.modelTransform.GetComponent<ChildLocator>();
      if ((bool)(Object)component)
      {
        Transform child = component.FindChild(targetMuzzle);
        if ((bool)(Object)child)
          aimRay.origin = child.position;
      }
    }
    this.AddRecoil(-1f * FireTarball.recoilAmplitude, -2f * FireTarball.recoilAmplitude, -1f * FireTarball.recoilAmplitude, 1f * FireTarball.recoilAmplitude);
    if ((bool)(Object)FireTarball.effectPrefab)
      EffectManager.SimpleMuzzleFlash(FireTarball.effectPrefab, this.gameObject, targetMuzzle, false);
    if (this.isAuthority)
    {
      Vector3 forward = Vector3.ProjectOnPlane(aimRay.direction, Vector3.up);
      ProjectileManager.instance.FireProjectile(FireTarball.projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(forward), this.gameObject, this.damageStat * FireTarball.damageCoefficient, 0.0f, Util.CheckRoll(this.critStat, this.characterBody.master));
    }
    this.characterBody.AddSpreadBloom(FireTarball.spreadBloomValue);
  }

  private void FireGrenade(string targetMuzzle)
  {
    this.PlayCrossfade("Gesture, Bombardment", nameof(FireBombardment), 0.1f);
    int num = (int)Util.PlaySound(FireBombardment.shootSoundString, this.gameObject);
    Ray aimRay = this.GetAimRay();
    Vector3 position = aimRay.origin;
    if ((bool)(Object)this.modelTransform)
    {
      ChildLocator component = this.modelTransform.GetComponent<ChildLocator>();
      if ((bool)(Object)component)
      {
        Transform child = component.FindChild(targetMuzzle);
        if ((bool)(Object)child)
          position = child.position;
      }
    }
    this.AddRecoil(-1f * FireBombardment.recoilAmplitude, -2f * FireBombardment.recoilAmplitude, -1f * FireBombardment.recoilAmplitude, 1f * FireBombardment.recoilAmplitude);
    if ((bool)(Object)FireBombardment.effectPrefab)
      EffectManager.SimpleMuzzleFlash(FireBombardment.effectPrefab, this.gameObject, targetMuzzle, false);
    if (this.isAuthority)
    {
      float speedOverride = -1f;
      RaycastHit hitInfo;
      if (Util.CharacterRaycast(this.gameObject, aimRay, out hitInfo, float.PositiveInfinity, (LayerMask)((int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask), QueryTriggerInteraction.Ignore))
      {
        Vector3 point = hitInfo.point;
        float velocity = FireBombardment.projectilePrefab.GetComponent<ProjectileSimple>().desiredForwardSpeed;
        Vector3 vector3_1 = position;
        Vector3 vector3_2 = point - vector3_1;
        Vector2 vector2 = new Vector2(vector3_2.x, vector3_2.z);
        float magnitude = vector2.magnitude;
        float initialYspeed = Trajectory.CalculateInitialYSpeed(magnitude / velocity, vector3_2.y);
        Vector3 vector3_3 = new Vector3(vector2.x / magnitude * velocity, initialYspeed, vector2.y / magnitude * velocity);
        speedOverride = vector3_3.magnitude;
        aimRay.direction = vector3_3 / speedOverride;
      }
      float x = Random.Range(0.0f, this.characterBody.spreadBloomAngle);
      float z = Random.Range(0.0f, 360f);
      Vector3 up = Vector3.up;
      Vector3 axis1 = Vector3.Cross(up, aimRay.direction);
      Vector3 vector3 = Quaternion.Euler(0.0f, 0.0f, z) * (Quaternion.Euler(x, 0.0f, 0.0f) * Vector3.forward);
      float y = vector3.y;
      vector3.y = 0.0f;
      double angle1 = (double)Mathf.Atan2(vector3.z, vector3.x) * 57.2957801818848 - 90.0;
      float angle2 = Mathf.Atan2(y, vector3.magnitude) * 57.29578f;
      Vector3 axis2 = up;
      Vector3 forward = Quaternion.AngleAxis((float)angle1, axis2) * (Quaternion.AngleAxis(angle2, axis1) * aimRay.direction);
      ProjectileManager.instance.FireProjectile(FireBombardment.projectilePrefab, position, Util.QuaternionSafeLookRotation(forward), this.gameObject, this.damageStat * FireBombardment.damageCoefficient, 0.0f, Util.CheckRoll(this.critStat, this.characterBody.master), speedOverride: speedOverride);
    }
    this.characterBody.AddSpreadBloom(FireBombardment.spreadBloomValue);
  }

  public override void FixedUpdate()
  {
    base.FixedUpdate();
    if (!this.isAuthority)
      return;
    if (Mathf.CeilToInt(this.fixedAge / this.maxDuration * (float)this.totalWaves) > this.wavesFired)
      this.FireWave();
    this.chargeTimer -= Time.fixedDeltaTime;
    if ((double)this.chargeTimer <= 0.0)
    {
      this.chargeTimer = 0.06f;
      this.charges = Mathf.Min(this.charges + 1, 150);
    }
    this.fireTarballTimer -= Time.fixedDeltaTime;
    if ((double)this.fireTarballTimer <= 0.0)
    {
      if (this.tarballCount < FireTarball.tarballCountMax)
      {
        this.fireTarballTimer += this.timeBetweenShots;
        this.FireSingleTarball("BottomMuzzle");
        ++this.tarballCount;
      }
      else
      {
        this.fireTarballTimer += 9999f;
        this.PlayCrossfade("Body", "ExitTarBall", "ExitTarBall.playbackRate", (FireTarball.cooldownDuration - FireTarball.baseTimeBetweenShots) / this.attackSpeedStat, 0.1f);
      }
    }
    if ((double)this.fireGrenadeTimer <= 0.0 && this.grenadeCount < this.grenadeCountMax)
    {
      this.fireGrenadeTimer += this.timeBetweenShots;
      this.FireGrenade("Muzzle");
      ++this.grenadeCount;
    }
    if ((double)this.fixedAge <= (double)this.maxDuration)
      return;
    this.outer.SetNextState(new LunarDevastationExit());
  }

  public override void OnExit()
  {
    int num = (int)Util.PlaySound(UltChannelState.exitSoundString, this.gameObject);
    if ((bool)(Object)this.channelEffectInstance)
      EntityStates.EntityState.Destroy((Object)this.channelEffectInstance);
    base.OnExit();
  }
}