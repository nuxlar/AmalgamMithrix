using BepInEx;
using RoR2;
using RoR2.Skills;
using RoR2.Projectile;
using EntityStates.BrotherMonster;
using EntityStates.BrotherMonster.Weapon;
using R2API;
using R2API.Utils;
using System;
using System.Collections.Generic;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;

namespace AmalgamMithrix
{
  [BepInPlugin("com.Nuxlar.UmbralMithrix", "UmbralMithrix", "1.1.3")]
  [BepInDependency("com.bepis.r2api")]
  [BepInDependency("com.rune580.riskofoptions")]
  [R2APISubmoduleDependency(new string[]
    {
        "LanguageAPI",
        "PrefabAPI",
        "ContentAddition",
        "ItemAPI"
    })]

  public class AmalgamMithrix : BaseUnityPlugin
  {
    bool hasfired;
    int phaseCounter = 0;
    float elapsed = 0;
    bool shrineActivated = false;
    bool doppelEventHasTriggered = false;
    HashSet<ItemIndex> doppelBlacklist = new();
    ItemDef AmalgamItem;
    GameObject Mithrix = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/BrotherBody.prefab").WaitForCompletion();
    SkillDef originalDash;
    GameObject MithrixHurt = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/BrotherHurtBody.prefab").WaitForCompletion();
    GameObject Bison = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bison/BisonBody.prefab").WaitForCompletion();
    GameObject ImpBoss = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ImpBoss/ImpBossBody.prefab").WaitForCompletion();
    GameObject BrotherHaunt = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/BrotherHaunt/BrotherHauntBody.prefab").WaitForCompletion();
    SpawnCard MithrixCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Brother/cscBrother.asset").WaitForCompletion();
    SpawnCard MithrixHurtCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/Brother/cscBrotherHurt.asset").WaitForCompletion();
    GameObject MithrixGlass = Addressables.LoadAssetAsync<GameObject>("RoR2/Junk/BrotherGlass/BrotherGlassBody.prefab").WaitForCompletion();
    SpawnCard MithrixGlassCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Junk/BrotherGlass/cscBrotherGlass.asset").WaitForCompletion();
    static GameObject exploderProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarExploder/LunarExploderShardProjectile.prefab").WaitForCompletion();
    static GameObject golemProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarGolem/LunarGolemTwinShotProjectile.prefab").WaitForCompletion();

    public void Awake()
    {
      ModConfig.InitConfig(Config);
      AddContent();
      // CreateDoppelItem();
      On.RoR2.Run.Start += OnRunStart;
      On.RoR2.CharacterBody.OnInventoryChanged += OnInventoryChanged;
      On.RoR2.ShrineBossBehavior.AddShrineStack += AddShrineStack;
      On.RoR2.Stage.Start += StageStart;
      On.RoR2.CharacterMaster.OnBodyStart += CharacterMasterOnBodyStart;
      On.EntityStates.Missions.BrotherEncounter.BrotherEncounterPhaseBaseState.OnEnter += BrotherEncounterPhaseBaseStateOnEnter;
      On.EntityStates.Missions.BrotherEncounter.Phase1.OnEnter += Phase1OnEnter;
      On.EntityStates.Missions.BrotherEncounter.Phase2.OnEnter += Phase2OnEnter;
      On.EntityStates.Missions.BrotherEncounter.Phase3.OnEnter += Phase3OnEnter;
      On.EntityStates.Missions.BrotherEncounter.Phase4.OnEnter += Phase4OnEnter;
      On.EntityStates.BrotherMonster.ExitSkyLeap.OnEnter += ExitSkyLeapOnEnter;
      On.EntityStates.FrozenState.OnEnter += FrozenStateOnEnter;
      On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += AddTimedBuff_BuffDef_float;
      On.EntityStates.BrotherMonster.SprintBash.OnEnter += SprintBashOnEnter;
      On.EntityStates.BrotherMonster.WeaponSlam.OnEnter += WeaponSlamOnEnter;
      On.EntityStates.BrotherMonster.WeaponSlam.OnExit += WeaponSlamOnExit;
      On.EntityStates.BrotherMonster.WeaponSlam.OnEnter += CleanupPillar;
      On.EntityStates.BrotherMonster.SlideIntroState.OnEnter += SlideIntroStateOnEnter;
      On.EntityStates.BrotherMonster.Weapon.FireLunarShards.OnEnter += FireLunarShardsOnEnter;
      On.EntityStates.BrotherMonster.FistSlam.OnEnter += FistSlamOnEnter;
      On.EntityStates.BrotherMonster.FistSlam.FixedUpdate += FistSlamFixedUpdate;
      On.EntityStates.BrotherMonster.SpellChannelEnterState.OnEnter += SpellChannelEnterStateOnEnter;
      On.EntityStates.BrotherMonster.SpellChannelState.OnEnter += SpellChannelStateOnEnter;
      On.EntityStates.BrotherMonster.SpellChannelState.OnExit += SpellChannelStateOnExit;
      On.EntityStates.BrotherMonster.SpellChannelExitState.OnEnter += SpellChannelExitStateOnEnter;
      On.EntityStates.BrotherMonster.StaggerEnter.OnEnter += StaggerEnterOnEnter;
      On.EntityStates.BrotherMonster.StaggerExit.OnEnter += StaggerExitOnEnter;
      On.EntityStates.BrotherMonster.StaggerLoop.OnEnter += StaggerLoopOnEnter;
      On.EntityStates.BrotherMonster.TrueDeathState.OnEnter += TrueDeathStateOnEnter;

      On.EntityStates.GravekeeperMonster.Weapon.GravekeeperBarrage.OnEnter += BlinkStateOnEnter;
      originalDash = Mithrix.GetComponent<SkillLocator>().utility.skillFamily.variants[0].skillDef;
    }

    private void RevertToVanillaStats()
    {
      CharacterBody MithrixBody = Mithrix.GetComponent<CharacterBody>();
      CharacterBody MithrixHurtBody = MithrixHurt.GetComponent<CharacterBody>();
      CharacterDirection MithrixDirection = Mithrix.GetComponent<CharacterDirection>();
      CharacterMotor MithrixMotor = Mithrix.GetComponent<CharacterMotor>();

      MithrixMotor.mass = 900;
      MithrixMotor.airControl = 0.25f;
      MithrixMotor.jumpCount = 1;

      MithrixBody.baseMaxHealth = 1000;
      MithrixBody.levelMaxHealth = 300;
      MithrixBody.baseDamage = 16;
      MithrixBody.levelDamage = 3.2f;

      // Mithrix Hurt
      MithrixHurtBody.baseMaxHealth = 1400;
      MithrixHurtBody.levelMaxHealth = 420;
      MithrixHurtBody.baseArmor = 20;
      // Mithrix Hurt

      MithrixBody.baseAttackSpeed = 1;
      MithrixBody.baseMoveSpeed = 15;
      MithrixBody.baseAcceleration = 45;
      MithrixBody.baseJumpPower = 25;
      MithrixDirection.turnSpeed = 270;

      MithrixBody.baseArmor = 20;

      ProjectileSteerTowardTarget component = FireLunarShards.projectilePrefab.GetComponent<ProjectileSteerTowardTarget>();
      component.rotationSpeed = 20;
      ProjectileDirectionalTargetFinder component2 = FireLunarShards.projectilePrefab.GetComponent<ProjectileDirectionalTargetFinder>();
      component2.lookRange = 80;
      component2.lookCone = 90;
      component2.allowTargetLoss = true;

      WeaponSlam.duration = 3.5f;
      HoldSkyLeap.duration = 3;
      ExitSkyLeap.waveProjectileCount = 12;
      ExitSkyLeap.recastChance = 0;
      UltChannelState.waveProjectileCount = 8;
      UltChannelState.maxDuration = 8;
      UltChannelState.totalWaves = 4;
    }

    private void RevertToVanillaSkills()
    {
      SkillLocator SklLocate = Mithrix.GetComponent<SkillLocator>();
      SkillLocator skillLocator = MithrixHurt.GetComponent<SkillLocator>();
      // MithrixHurt
      SkillFamily fireLunarShardsHurt = skillLocator.primary.skillFamily;
      SkillDef fireLunarShardsHurtSkillDef = fireLunarShardsHurt.variants[0].skillDef;
      fireLunarShardsHurtSkillDef.baseRechargeInterval = 6;
      fireLunarShardsHurtSkillDef.baseMaxStock = 12;
      // Mithrix
      SkillFamily Hammer = SklLocate.primary.skillFamily;
      SkillDef HammerChange = Hammer.variants[0].skillDef;
      HammerChange.baseRechargeInterval = 4;
      HammerChange.baseMaxStock = 1;

      SkillFamily Bash = SklLocate.secondary.skillFamily;
      SkillDef BashChange = Bash.variants[0].skillDef;
      BashChange.baseRechargeInterval = 5;
      BashChange.baseMaxStock = 1;

      SkillFamily Dash = SklLocate.utility.skillFamily;
      Dash.variants[0].skillDef = originalDash;

      SkillFamily Ult = SklLocate.special.skillFamily;
      SkillDef UltChange = Ult.variants[0].skillDef;
      UltChange.baseRechargeInterval = 30;
    }

    private void AdjustBaseStats()
    {
      Logger.LogMessage("Adjusting Phase 1 Stats");
      int playerCount = PlayerCharacterMasterController.instances.Count;
      float hpMultiplier;
      float mobilityMultiplier;
      if (Run.instance.loopClearCount == 1)
      {
        hpMultiplier = (ModConfig.phase1BaseHPScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase1PlayerHPScaling.Value * playerCount);
        mobilityMultiplier = (ModConfig.phase1BaseMobilityScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase1PlayerMobilityScaling.Value * playerCount);
      }
      else
      {
        hpMultiplier = (ModConfig.phase1LoopHPScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase1PlayerHPScaling.Value * playerCount);
        mobilityMultiplier = (ModConfig.phase1LoopMobilityScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase1PlayerMobilityScaling.Value * playerCount);
      }
      CharacterBody MithrixBody = Mithrix.GetComponent<CharacterBody>();
      CharacterBody MithrixGlassBody = MithrixGlass.GetComponent<CharacterBody>();
      CharacterDirection MithrixDirection = Mithrix.GetComponent<CharacterDirection>();
      CharacterMotor MithrixMotor = Mithrix.GetComponent<CharacterMotor>();

      MithrixMotor.mass = ModConfig.mass.Value;
      MithrixMotor.airControl = ModConfig.aircontrol.Value;
      MithrixMotor.jumpCount = ModConfig.jumpcount.Value;

      MithrixGlassBody.baseDamage = ModConfig.basedamage.Value;
      MithrixGlassBody.levelDamage = ModConfig.leveldamage.Value;

      MithrixBody.baseMaxHealth = ModConfig.basehealth.Value + (ModConfig.basehealth.Value * hpMultiplier);
      MithrixBody.levelMaxHealth = ModConfig.levelhealth.Value + (ModConfig.levelhealth.Value * hpMultiplier);
      MithrixBody.baseDamage = ModConfig.basedamage.Value;
      MithrixBody.levelDamage = ModConfig.leveldamage.Value;

      MithrixBody.baseAttackSpeed = ModConfig.baseattackspeed.Value;
      MithrixBody.baseMoveSpeed = ModConfig.basespeed.Value + (ModConfig.basespeed.Value * mobilityMultiplier);
      MithrixBody.baseAcceleration = ModConfig.acceleration.Value + (ModConfig.acceleration.Value * mobilityMultiplier);
      MithrixBody.baseJumpPower = ModConfig.jumpingpower.Value + (ModConfig.jumpingpower.Value * mobilityMultiplier);
      MithrixDirection.turnSpeed = ModConfig.turningspeed.Value + (ModConfig.turningspeed.Value * mobilityMultiplier);

      MithrixBody.baseArmor = ModConfig.basearmor.Value;

      ProjectileSteerTowardTarget component = FireLunarShards.projectilePrefab.GetComponent<ProjectileSteerTowardTarget>();
      component.rotationSpeed = ModConfig.ShardHoming.Value;
      ProjectileDirectionalTargetFinder component2 = FireLunarShards.projectilePrefab.GetComponent<ProjectileDirectionalTargetFinder>();
      component2.lookRange = ModConfig.ShardRange.Value;
      component2.lookCone = ModConfig.ShardCone.Value;
      component2.allowTargetLoss = true;

      WeaponSlam.duration = (3.5f / ModConfig.baseattackspeed.Value);
      HoldSkyLeap.duration = ModConfig.JumpPause.Value;
      ExitSkyLeap.waveProjectileCount = ModConfig.JumpWaveCount.Value;
      ExitSkyLeap.recastChance = ModConfig.JumpRecast.Value;
      UltChannelState.waveProjectileCount = ModConfig.UltimateWaves.Value;
      UltChannelState.maxDuration = ModConfig.UltimateDuration.Value;
      UltChannelState.totalWaves = ModConfig.UltimateCount.Value;
    }

    private void AdjustBaseSkills()
    {
      SkillLocator SklLocate = Mithrix.GetComponent<SkillLocator>();
      SkillFamily Hammer = SklLocate.primary.skillFamily;
      SkillDef HammerChange = Hammer.variants[0].skillDef;
      HammerChange.baseRechargeInterval = ModConfig.PrimCD.Value;
      HammerChange.baseMaxStock = ModConfig.PrimStocks.Value;

      SkillFamily Bash = SklLocate.secondary.skillFamily;
      SkillDef BashChange = Bash.variants[0].skillDef;
      BashChange.baseRechargeInterval = ModConfig.SecCD.Value;
      BashChange.baseMaxStock = ModConfig.SecStocks.Value;

      // Replace dash with blink (creating new skilldef so it can be done while midair)
      SkillFamily Dash = SklLocate.utility.skillFamily;
      SkillDef DashChange = Dash.variants[0].skillDef;
      DashChange.baseRechargeInterval = ModConfig.UtilCD.Value;
      DashChange.baseMaxStock = ModConfig.UtilStocks.Value;
      /**SkillDef blink = ScriptableObject.CreateInstance<SkillDef>();
      blink.activationState = new EntityStates.SerializableEntityStateType(typeof(EntityStates.Huntress.MiniBlinkState)); ;
      blink.activationStateMachineName = "Weapon";
      blink.baseMaxStock = ModConfig.UtilStocks.Value;
      blink.baseRechargeInterval = ModConfig.UtilCD.Value;
      blink.beginSkillCooldownOnSkillEnd = true;
      blink.canceledFromSprinting = false;
      blink.cancelSprintingOnActivation = false;
      blink.fullRestockOnAssign = true;
      blink.interruptPriority = EntityStates.InterruptPriority.Skill;
      blink.isCombatSkill = true;
      blink.mustKeyPress = false;
      blink.rechargeStock = 1;
      blink.requiredStock = 1;
      blink.stockToConsume = 1;
      Dash.variants[0].skillDef = blink;
      **/

      SkillFamily Ult = SklLocate.special.skillFamily;
      SkillDef UltChange = Ult.variants[0].skillDef;
      UltChange.baseRechargeInterval = ModConfig.SpecialCD.Value;
      UltChange.activationState = new EntityStates.SerializableEntityStateType(typeof(CrushingLeap));
    }

    private void AdjustPhase2Stats()
    {
      Logger.LogMessage("Adjusting Phase 2 Stats");
      int playerCount = PlayerCharacterMasterController.instances.Count;
      float hpMultiplier;
      float mobilityMultiplier;
      if (Run.instance.loopClearCount == 1)
      {
        hpMultiplier = (ModConfig.phase2BaseHPScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase2PlayerHPScaling.Value * playerCount);
        mobilityMultiplier = (ModConfig.phase2BaseMobilityScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase2PlayerMobilityScaling.Value * playerCount);
      }
      else
      {
        hpMultiplier = (ModConfig.phase2LoopHPScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase2PlayerHPScaling.Value * playerCount);
        mobilityMultiplier = (ModConfig.phase2LoopMobilityScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase2PlayerMobilityScaling.Value * playerCount);
      }
      CharacterBody MithrixBody = Mithrix.GetComponent<CharacterBody>();
      CharacterDirection MithrixDirection = Mithrix.GetComponent<CharacterDirection>();

      MithrixBody.baseMaxHealth = (ModConfig.basehealth.Value + (ModConfig.basehealth.Value * hpMultiplier)) * 10;
      MithrixBody.levelMaxHealth = (ModConfig.levelhealth.Value + (ModConfig.levelhealth.Value * hpMultiplier)) * 10;

      MithrixBody.baseMoveSpeed = ModConfig.basespeed.Value + (ModConfig.basespeed.Value * mobilityMultiplier);
      MithrixBody.baseAcceleration = ModConfig.acceleration.Value + (ModConfig.acceleration.Value * mobilityMultiplier);
      MithrixBody.baseJumpPower = ModConfig.jumpingpower.Value + (ModConfig.jumpingpower.Value * mobilityMultiplier);
      MithrixDirection.turnSpeed = ModConfig.turningspeed.Value + (ModConfig.turningspeed.Value * mobilityMultiplier);

      WeaponSlam.duration = (3.5f / ModConfig.baseattackspeed.Value);
    }

    private void AdjustPhase2Skills()
    {
      SkillLocator mithySkills = Mithrix.GetComponent<SkillLocator>();

      SkillFamily skyleap = mithySkills.special.skillFamily;
      SkillDef skyleapDef = skyleap.variants[0].skillDef;
      skyleapDef.activationState = new EntityStates.SerializableEntityStateType(typeof(CrushingLeap));
    }

    private void AdjustPhase3Stats()
    {
      Logger.LogMessage("Adjusting Phase 3 Stats");
      int playerCount = PlayerCharacterMasterController.instances.Count;
      float hpMultiplier;
      float mobilityMultiplier;
      if (Run.instance.loopClearCount == 1)
      {
        hpMultiplier = (ModConfig.phase3BaseHPScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase3PlayerHPScaling.Value * playerCount);
        mobilityMultiplier = (ModConfig.phase3BaseMobilityScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase3PlayerMobilityScaling.Value * playerCount);
      }
      else
      {
        hpMultiplier = (ModConfig.phase3LoopHPScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase3PlayerHPScaling.Value * playerCount);
        mobilityMultiplier = (ModConfig.phase3LoopMobilityScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase3PlayerMobilityScaling.Value * playerCount);
      }
      CharacterBody MithrixBody = Mithrix.GetComponent<CharacterBody>();
      CharacterDirection MithrixDirection = Mithrix.GetComponent<CharacterDirection>();

      MithrixBody.baseMaxHealth = playerCount > 2 ? ModConfig.basehealth.Value + (ModConfig.basehealth.Value * hpMultiplier) : (ModConfig.basehealth.Value + (ModConfig.basehealth.Value * hpMultiplier)) / 2;
      MithrixBody.levelMaxHealth = playerCount > 2 ? ModConfig.levelhealth.Value + (ModConfig.levelhealth.Value * hpMultiplier) : (ModConfig.levelhealth.Value + (ModConfig.levelhealth.Value * hpMultiplier)) / 2;

      MithrixBody.baseMoveSpeed = ModConfig.basespeed.Value + (ModConfig.basespeed.Value * mobilityMultiplier);
      MithrixBody.baseAcceleration = ModConfig.acceleration.Value + (ModConfig.acceleration.Value * mobilityMultiplier);
      MithrixBody.baseJumpPower = ModConfig.jumpingpower.Value + (ModConfig.jumpingpower.Value * mobilityMultiplier);
      MithrixDirection.turnSpeed = ModConfig.turningspeed.Value + (ModConfig.turningspeed.Value * mobilityMultiplier);

      WeaponSlam.duration = (3.5f / ModConfig.baseattackspeed.Value);
    }

    private void AdjustPhase4Stats()
    {
      Logger.LogMessage("Adjusting Phase 4 Stats");
      int playerCount = PlayerCharacterMasterController.instances.Count;
      float hpMultiplier;
      if (Run.instance.loopClearCount == 1)
        hpMultiplier = (ModConfig.phase4BaseHPScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase4PlayerHPScaling.Value * playerCount);
      else
        hpMultiplier = (ModConfig.phase4LoopHPScaling.Value * Run.instance.loopClearCount) + (ModConfig.phase4PlayerHPScaling.Value * playerCount);
      CharacterBody MithrixHurtBody = MithrixHurt.GetComponent<CharacterBody>();
      MithrixHurtBody.baseMaxHealth = Run.instance.loopClearCount > 1 ? (ModConfig.basehealth.Value + (ModConfig.basehealth.Value * hpMultiplier)) * 10 : (ModConfig.basehealth.Value + (ModConfig.basehealth.Value * hpMultiplier)) * 3;
      MithrixHurtBody.levelMaxHealth = Run.instance.loopClearCount > 1 ? (ModConfig.levelhealth.Value + (ModConfig.levelhealth.Value * hpMultiplier)) * 10 : (ModConfig.levelhealth.Value + (ModConfig.levelhealth.Value * hpMultiplier)) * 3;

      MithrixHurtBody.baseArmor = ModConfig.basearmor.Value;
      SkillLocator skillLocator = MithrixHurt.GetComponent<SkillLocator>();
      SkillFamily fireLunarShardsHurt = skillLocator.primary.skillFamily;
      SkillDef fireLunarShardsHurtSkillDef = fireLunarShardsHurt.variants[0].skillDef;
      fireLunarShardsHurtSkillDef.baseRechargeInterval = ModConfig.SuperShardCD.Value;
      fireLunarShardsHurtSkillDef.baseMaxStock = ModConfig.SuperShardCount.Value;
    }

    private void CreateBlacklist()
    {
      // N'kuhanas Opinion
      doppelBlacklist.Add(RoR2Content.Items.NovaOnHeal.itemIndex);
      // Tesla Coil
      doppelBlacklist.Add(RoR2Content.Items.ShockNearby.itemIndex);
      // Razorwire
      doppelBlacklist.Add(RoR2Content.Items.Thorns.itemIndex);
      // Empathy Cores
      doppelBlacklist.Add(RoR2Content.Items.RoboBallBuddy.itemIndex);
      // Spare Drone Parts
      doppelBlacklist.Add(DLC1Content.Items.DroneWeapons.itemIndex);
    }

    private void AddContent()
    {
      // Add our new EntityStates to the game
      ContentAddition.AddEntityState<LunarDevastationEnter>(out _);
      ContentAddition.AddEntityState<LunarDevastationChannel>(out _);
      ContentAddition.AddEntityState<LunarDevastationExit>(out _);

      ContentAddition.AddEntityState<AimCrushingLeap>(out _);
      ContentAddition.AddEntityState<CrushingLeap>(out _);
      ContentAddition.AddEntityState<ExitCrushingLeap>(out _);
      /** For Debugging
      SurvivorDef mitchell = ScriptableObject.CreateInstance<SurvivorDef>();
      mitchell.bodyPrefab = MithrixHurt;
      mitchell.descriptionToken = "MITHRIX_DESCRIPTION";
      mitchell.displayPrefab = MithrixHurt;
      mitchell.primaryColor = new Color(0.5f, 0.5f, 0.5f);
      mitchell.displayNameToken = "Mitchell";
      mitchell.desiredSortPosition = 99f;
      ContentAddition.AddSurvivorDef(mitchell);
      **/
    }
    private void CreateDoppelItem()
    {
      LanguageAPI.Add("AMALGAMMITHRIX_ITEM", "Amalgam Skin");
      LanguageAPI.Add("AMALGAMMITHRIX_PICKUP", "For Mithrix ONLY >:(");
      LanguageAPI.Add("AMALGAMMITHRIX_DESC", "Funny mithy skin");

      LanguageAPI.Add("AMALGAMMITHRIX_SUBTITLENAMETOKEN", "Scourge of Petrichor V");
      LanguageAPI.Add("AMALGAMMITHRIX_MODIFIER", "Amalgam");

      AmalgamItem = ScriptableObject.CreateInstance<ItemDef>();
      AmalgamItem.name = "AmalgamMithrixItem";
      AmalgamItem.deprecatedTier = ItemTier.NoTier;
      AmalgamItem.nameToken = "AMALGAMMITHRIX_NAME";
      AmalgamItem.pickupToken = "AMALGAMMITHRIX_PICKUP";
      AmalgamItem.descriptionToken = "AMALGAMMITHRIX_DESC";
      // AmalgamItem.tags = new ItemTag[] { ItemTag.WorldUnique, ItemTag.BrotherBlacklist, ItemTag.CannotSteal };
      ItemDisplayRule[] idr = new ItemDisplayRule[0];
      //ContentAddition.AddItemDef(AmalgamItem);
      ItemAPI.Add(new CustomItem(AmalgamItem, idr));

      On.RoR2.CharacterBody.GetSubtitle += (orig, self) =>
      {
        if (self.inventory && self.inventory.GetItemCount(AmalgamItem) > 0)
        {
          return Language.GetString("AMALGAMMITHRIX_SUBTITLENAMETOKEN");
        }
        return orig(self);
      };

      On.RoR2.Util.GetBestBodyName += (orig, bodyObject) =>
      {
        string toReturn = orig(bodyObject);
        CharacterBody cb = bodyObject.GetComponent<CharacterBody>();
        if (cb && cb.inventory && cb.inventory.GetItemCount(AmalgamItem) > 0)
        {
          toReturn = Language.GetString("AMALGAMMITHRIX_MODIFIER") + " " + toReturn; ;
        }
        return toReturn;
      };


      IL.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects += (il) =>
      {
        ILCursor c = new ILCursor(il);
        c.GotoNext(
                     x => x.MatchLdsfld(typeof(RoR2Content.Items), "InvadingDoppelganger")
                    );
        c.Index += 2;
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<int, CharacterBody, int>>((vengeanceCount, self) =>
                {
                  int toReturn = vengeanceCount;
                  if (self.inventory)
                  {
                    toReturn += self.inventory.GetItemCount(AmalgamItem);
                  }
                  return toReturn;
                });
      };

      IL.RoR2.CharacterModel.UpdateOverlays += (il) =>
      {
        ILCursor c = new ILCursor(il);
        c.GotoNext(
                   x => x.MatchLdsfld(typeof(RoR2Content.Equipment), "AffixPoison")
                  );
        c.Index += 2;
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<int, CharacterModel, int>>((vengeanceCount, self) =>
              {
                int toReturn = vengeanceCount;
                if (self.body && self.body.inventory)
                {
                  toReturn += self.body.inventory.GetItemCount(AmalgamItem);
                }
                return toReturn;
              });
      };
    }

    private void OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
      orig(self);
      if (NetworkServer.active && self.inventory && shrineActivated && phaseCounter == 3 && (self.inventory.GetItemCount(AmalgamItem) > 0 || self.inventory.GetItemCount(RoR2Content.Items.InvadingDoppelganger) > 0))
      {
        // Remove Blacklisted Items
        foreach (ItemIndex item in doppelBlacklist)
        {
          int itemCount = self.inventory.GetItemCount(item);
          if (itemCount > 0)
            self.inventory.RemoveItem(item, itemCount);
        }
      }
    }
    private void StageStart(On.RoR2.Stage.orig_Start orig, RoR2.Stage self)
    {
      orig(self);
      if (self.sceneDef.cachedName == "moon2")
        SpawnUmbralShrine();
    }
    private void SpawnUmbralShrine()
    {
      // 409.8 -157.9 515.9
      SpawnCard umbralShrineCard = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/ShrineBoss/iscShrineBoss.asset").WaitForCompletion();
      DirectorPlacementRule placementRule = new DirectorPlacementRule();
      placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
      GameObject spawnedShrine = umbralShrineCard.DoSpawn(new Vector3(409.8f, -157.9f, 515.9f), Quaternion.identity, new DirectorSpawnRequest(umbralShrineCard, placementRule, Run.instance.runRNG)).spawnedInstance;
      spawnedShrine.name = "UmbralShrine";
      NetworkServer.Spawn(spawnedShrine);
    }
    private void AddShrineStack(On.RoR2.ShrineBossBehavior.orig_AddShrineStack orig, RoR2.ShrineBossBehavior self, Interactor interactor)
    {
      if (self.purchaseInteraction.gameObject.name == "UmbralShrine")
      {
        shrineActivated = true;
        Chat.SendBroadcastChat(new Chat.SimpleChatMessage() { baseToken = $"<color=#8826dd>The Umbral King awaits...</color>" });
        EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData()
        {
          origin = self.transform.position,
          rotation = Quaternion.identity,
          scale = 1f,
          color = (Color32)new Color(0.7372549f, 0.9058824f, 0.945098f)
        }, true);
      }
      else
        orig(self, interactor);
    }

    private void BlinkStateOnEnter(On.EntityStates.GravekeeperMonster.Weapon.GravekeeperBarrage.orig_OnEnter orig, EntityStates.GravekeeperMonster.Weapon.GravekeeperBarrage self)
    {
      Chat.SendBroadcastChat(new Chat.SimpleChatMessage() { baseToken = $"duration {EntityStates.GravekeeperMonster.Weapon.GravekeeperBarrage.baseDuration}" });
      Chat.SendBroadcastChat(new Chat.SimpleChatMessage() { baseToken = $"frequency {EntityStates.GravekeeperMonster.Weapon.GravekeeperBarrage.missileSpawnFrequency}" });
      Chat.SendBroadcastChat(new Chat.SimpleChatMessage() { baseToken = $"delay {EntityStates.GravekeeperMonster.Weapon.GravekeeperBarrage.missileSpawnDelay}" });
      orig(self);
    }

    private void OnRunStart(On.RoR2.Run.orig_Start orig, Run self)
    {
      shrineActivated = false;
      CreateBlacklist();
      RevertToVanillaStats();
      RevertToVanillaSkills();
      orig(self);
    }
    // Prevent freezing from affecting Mithrix after 10 stages or if the config is enabled
    private void FrozenStateOnEnter(On.EntityStates.FrozenState.orig_OnEnter orig, EntityStates.FrozenState self)
    {
      if ((self.characterBody.name == "BrotherBody(Clone)" || self.characterBody.name == "BrotherHurtBody(Clone)") && (Run.instance.loopClearCount >= 2 || ModConfig.debuffResistance.Value))
        return;
      orig(self);
    }
    // Prevent tentabauble from affecting Mithrix after 10 stages or if the config is enabled
    private void AddTimedBuff_BuffDef_float(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
    {
      if ((self.name == "BrotherBody(Clone)" || self.name == "BrotherHurtBody(Clone)") && buffDef == RoR2Content.Buffs.Nullified && (Run.instance.loopClearCount >= 2 || ModConfig.debuffResistance.Value))
        return;
      orig(self, buffDef, duration);
    }

    private void CharacterMasterOnBodyStart(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
    {
      orig(self, body);
      if (shrineActivated)
      {
        // Make Mithrix an Umbra
        if (body.name == "BrotherBody(Clone)" || body.name == "BrotherHurtBody(Clone)")
          self.inventory.GiveItemString(RoR2Content.Items.NovaOnLowHealth.name);
        //self.inventory.GiveItemString(AmalgamItem.name);
      }
    }
    // Phase 2 change to encounter spawns (Mithrix instead of Chimera)
    private void BrotherEncounterPhaseBaseStateOnEnter(On.EntityStates.Missions.BrotherEncounter.BrotherEncounterPhaseBaseState.orig_OnEnter orig, EntityStates.Missions.BrotherEncounter.BrotherEncounterPhaseBaseState self)
    {
      if (shrineActivated)
      {
        phaseCounter++;
        // BrotherEncounterBaseState OnEnter
        self.childLocator = self.GetComponent<ChildLocator>();
        Transform child1 = self.childLocator.FindChild("ArenaWalls");
        Transform child2 = self.childLocator.FindChild("ArenaNodes");
        if ((bool)child1)
          child1.gameObject.SetActive(self.shouldEnableArenaWalls);
        if (!(bool)child2)
          return;
        child2.gameObject.SetActive(self.shouldEnableArenaNodes);
        // BrotherEncounterBaseState OnEnter
        if ((bool)PhaseCounter.instance)
        {
          phaseCounter = PhaseCounter.instance.phase;
          PhaseCounter.instance.GoToNextPhase();
        }
        if ((bool)self.childLocator)
        {
          self.phaseControllerObject = self.childLocator.FindChild(self.phaseControllerChildString).gameObject;
          if ((bool)self.phaseControllerObject)
          {
            self.phaseScriptedCombatEncounter = self.phaseControllerObject.GetComponent<ScriptedCombatEncounter>();
            self.phaseBossGroup = self.phaseControllerObject.GetComponent<BossGroup>();
            self.phaseControllerSubObjectContainer = self.phaseControllerObject.transform.Find("PhaseObjects").gameObject;
            self.phaseControllerSubObjectContainer.SetActive(true);
          }
          GameObject gameObject = self.childLocator.FindChild("AllPhases").gameObject;
          if ((bool)gameObject)
            gameObject.SetActive(true);
        }
        self.healthBarShowTime = Run.FixedTimeStamp.now + self.healthBarShowDelay;
        if ((bool)DirectorCore.instance)
        {
          foreach (Behaviour component in DirectorCore.instance.GetComponents<CombatDirector>())
            component.enabled = false;
        }
        if (!NetworkServer.active || self.phaseScriptedCombatEncounter == null)
          return;
        // Make Mithrix spawn for phase 2
        if (phaseCounter == 1)
        {
          Mithrix.transform.position = new Vector3(-88.5f, 491.5f, -0.3f);
          Mithrix.transform.rotation = Quaternion.identity;
          Transform explicitSpawnPosition = Mithrix.transform;
          ScriptedCombatEncounter.SpawnInfo spawnInfoMithrix = new ScriptedCombatEncounter.SpawnInfo
          {
            explicitSpawnPosition = explicitSpawnPosition,
            spawnCard = MithrixCard,
          };
          self.phaseScriptedCombatEncounter.spawns = new ScriptedCombatEncounter.SpawnInfo[] { spawnInfoMithrix };
        }
        self.phaseScriptedCombatEncounter.combatSquad.onMemberAddedServer += new Action<CharacterMaster>(self.OnMemberAddedServer);
      }
      else
        orig(self);
    }

    private void Phase1OnEnter(On.EntityStates.Missions.BrotherEncounter.Phase1.orig_OnEnter orig, EntityStates.Missions.BrotherEncounter.Phase1 self)
    {
      doppelEventHasTriggered = false;
      if (shrineActivated)
      {
        Logger.LogMessage("Amalgam the King of Nothing");
        AdjustBaseSkills();
        AdjustBaseStats();
      }
      orig(self);
    }

    private void Phase2OnEnter(On.EntityStates.Missions.BrotherEncounter.Phase2.orig_OnEnter orig, EntityStates.Missions.BrotherEncounter.Phase2 self)
    {
      if (shrineActivated)
      {
        self.KillAllMonsters();
        AdjustPhase2Stats();
        AdjustPhase2Skills();
      }
      orig(self);
    }

    private void Phase3OnEnter(On.EntityStates.Missions.BrotherEncounter.Phase3.orig_OnEnter orig, EntityStates.Missions.BrotherEncounter.Phase3 self)
    {
      if (shrineActivated)
      {
        self.KillAllMonsters();
        AdjustPhase3Stats();
      }
      orig(self);
    }

    private void Phase4OnEnter(On.EntityStates.Missions.BrotherEncounter.Phase4.orig_OnEnter orig, EntityStates.Missions.BrotherEncounter.Phase4 self)
    {
      if (shrineActivated)
        AdjustPhase4Stats();
      orig(self);
    }

    private void ExitSkyLeapOnEnter(On.EntityStates.BrotherMonster.ExitSkyLeap.orig_OnEnter orig, ExitSkyLeap self)
    {
      if (shrineActivated)
      {
        // EntityStates BaseState OnEnter
        if (!(bool)self.characterBody)
          return;
        self.attackSpeedStat = self.characterBody.attackSpeed;
        self.damageStat = self.characterBody.damage;
        self.critStat = self.characterBody.crit;
        self.moveSpeedStat = self.characterBody.moveSpeed;
        // EntityStates BaseState OnEnter
        self.duration = ExitSkyLeap.baseDuration / self.attackSpeedStat;
        int num = (int)Util.PlaySound(ExitSkyLeap.soundString, self.gameObject);
        self.PlayAnimation("Body", nameof(ExitSkyLeap), "SkyLeap.playbackRate", self.duration);
        self.PlayAnimation("FullBody Override", "BufferEmpty");
        self.characterBody.AddTimedBuff(RoR2Content.Buffs.ArmorBoost, ExitSkyLeap.baseDuration);
        AimAnimator aimAnimator = self.GetAimAnimator();
        if ((bool)aimAnimator)
          aimAnimator.enabled = true;
        if (self.isAuthority)
        {
          self.FireRingAuthority();
          // custom Ring Authority
          float num1 = 360f / ExitSkyLeap.waveProjectileCount;
          Vector3 point = Vector3.ProjectOnPlane(self.inputBank.aimDirection, Vector3.up);
          Vector3 point2 = Vector3.ProjectOnPlane(self.inputBank.aimDirection, Vector3.forward);
          Vector3 corePosition = self.characterBody.corePosition;
          for (int index = 0; index < ExitSkyLeap.waveProjectileCount; ++index)
          {
            Vector3 forward3 = Quaternion.AngleAxis(num1 * index, Vector3.up) * point;
            ProjectileManager.instance.FireProjectile(FistSlam.waveProjectilePrefab, corePosition, Util.QuaternionSafeLookRotation(forward3), self.gameObject, self.characterBody.damage * FistSlam.waveProjectileDamageCoefficient, FistSlam.waveProjectileForce, Util.CheckRoll(self.characterBody.crit, self.characterBody.master));
          }
        }
        if (!(bool)PhaseCounter.instance)
          return;
        if ((double)UnityEngine.Random.value < ExitSkyLeap.recastChance)
          self.recast = true;
        GenericSkill genericSkill = (bool)self.skillLocator ? self.skillLocator.special : null;
        if (!(bool)genericSkill)
          return;
        UltChannelState.replacementSkillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(LunarDevastationEnter));
        genericSkill.SetSkillOverride(self.outer, UltChannelState.replacementSkillDef, GenericSkill.SkillOverridePriority.Contextual);
      }
      else
        orig(self);
    }

    private void SprintBashOnEnter(On.EntityStates.BrotherMonster.SprintBash.orig_OnEnter orig, SprintBash self)
    {
      if (shrineActivated)
      {
        if (self.isAuthority)
        {
          Ray aimRay = self.GetAimRay();
          for (int i = 0; i < 12; i++)
          {
            Util.PlaySound(EntityStates.BrotherMonster.Weapon.FireLunarShards.fireSound, self.gameObject);
            ProjectileManager.instance.FireProjectile(FireLunarShards.projectilePrefab, aimRay.origin, Quaternion.LookRotation(aimRay.direction), self.gameObject, self.characterBody.damage * 0.1f / 12f, 0f, Util.CheckRoll(self.characterBody.crit, self.characterBody.master), DamageColorIndex.Default, null, -1f);
          }
        }
      }
      orig(self);
    }

    private void SlideIntroStateOnEnter(On.EntityStates.BrotherMonster.SlideIntroState.orig_OnEnter orig, SlideIntroState self)
    {
      if (shrineActivated)
      {
        if (self.characterBody.name == "BrotherBody(Clone)")
        {
          int playerCount = PlayerCharacterMasterController.instances.Count;
          if (playerCount > 2)
            playerCount = 2;
          for (int i = 0; i < playerCount; i++)
          {
            DirectorPlacementRule placementRule = new DirectorPlacementRule();
            placementRule.placementMode = DirectorPlacementRule.PlacementMode.Approximate;
            placementRule.minDistance = 3f;
            placementRule.maxDistance = 20f;
            placementRule.spawnOnTarget = self.gameObject.transform;
            Xoroshiro128Plus rng = RoR2Application.rng;
            DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(MithrixGlassCard, placementRule, rng);
            directorSpawnRequest.summonerBodyObject = self.gameObject;
            directorSpawnRequest.onSpawnedServer += (Action<SpawnCard.SpawnResult>)(spawnResult => spawnResult.spawnedInstance.GetComponent<Inventory>().GiveItem(RoR2Content.Items.HealthDecay, 2));
            DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
          }
        }
      }
      orig(self);
    }

    private void WeaponSlamOnEnter(On.EntityStates.BrotherMonster.WeaponSlam.orig_OnEnter orig, WeaponSlam self)
    {
      if (shrineActivated)
      {
        GameObject projectilePrefab = WeaponSlam.pillarProjectilePrefab;
        projectilePrefab.transform.localScale = new Vector3(4f, 4f, 4f);
        projectilePrefab.GetComponent<ProjectileController>().ghostPrefab.transform.localScale = new Vector3(4f, 4f, 4f);
        hasfired = false;
        if (phaseCounter == 0 && PhaseCounter.instance)
          PhaseCounter.instance.phase = 3;
      }
      orig(self);
    }

    private void WeaponSlamOnExit(On.EntityStates.BrotherMonster.WeaponSlam.orig_OnExit orig, WeaponSlam self)
    {
      if (shrineActivated)
        PhaseCounter.instance.phase = 1;
      orig(self);
    }

    private void FireLunarShardsOnEnter(On.EntityStates.BrotherMonster.Weapon.FireLunarShards.orig_OnEnter orig, FireLunarShards self)
    {
      if (shrineActivated && phaseCounter == 2)
      {
        self.duration = FireLunarShards.baseDuration / self.attackSpeedStat;
        if (self.isAuthority)
        {
          Ray aimRay = self.GetAimRay();
          Transform modelChild = self.FindModelChild(FireLunarShards.muzzleString);
          if ((bool)(UnityEngine.Object)modelChild)
            aimRay.origin = modelChild.position;
          aimRay.direction = Util.ApplySpread(aimRay.direction, 0.0f, self.maxSpread, self.spreadYawScale, self.spreadPitchScale);
          for (int i = 0; i < 10; i++)
          {
            int num = (int)Util.PlaySound(FireLunarShards.fireSound, self.gameObject);
            ProjectileManager.instance.FireProjectile(new FireProjectileInfo()
            {
              position = aimRay.origin,
              rotation = Quaternion.LookRotation(aimRay.direction),
              crit = self.characterBody.RollCrit(),
              damage = self.characterBody.damage * self.damageCoefficient,
              damageColorIndex = DamageColorIndex.Default,
              owner = self.gameObject,
              procChainMask = new ProcChainMask(),
              force = 0.0f,
              useFuseOverride = false,
              useSpeedOverride = false,
              target = (GameObject)null,
              projectilePrefab = EntityStates.ImpBossMonster.FireVoidspikes.projectilePrefab
            });
          }
        }
        self.PlayAnimation("Gesture, Additive", nameof(FireLunarShards));
        self.PlayAnimation("Gesture, Override", nameof(FireLunarShards));
        self.AddRecoil(-0.4f * FireLunarShards.recoilAmplitude, -0.8f * FireLunarShards.recoilAmplitude, -0.3f * FireLunarShards.recoilAmplitude, 0.3f * FireLunarShards.recoilAmplitude);
        self.characterBody.AddSpreadBloom(FireLunarShards.spreadBloomValue);
        EffectManager.SimpleMuzzleFlash(FireLunarShards.muzzleFlashEffectPrefab, self.gameObject, FireLunarShards.muzzleString, false);
      }
      if (shrineActivated && phaseCounter == 1)
      {
        self.duration = FireLunarShards.baseDuration / self.attackSpeedStat;
        if (self.isAuthority)
        {
          Ray aimRay = self.GetAimRay();
          Transform modelChild = self.FindModelChild(FireLunarShards.muzzleString);
          if ((bool)(UnityEngine.Object)modelChild)
            aimRay.origin = modelChild.position;
          aimRay.direction = Util.ApplySpread(aimRay.direction, 0.0f, self.maxSpread, self.spreadYawScale, self.spreadPitchScale);
          for (int i = 0; i < 5; i++)
          {
            int num = (int)Util.PlaySound(FireLunarShards.fireSound, self.gameObject);
            ProjectileManager.instance.FireProjectile(new FireProjectileInfo()
            {
              position = aimRay.origin,
              rotation = Quaternion.LookRotation(aimRay.direction),
              crit = self.characterBody.RollCrit(),
              damage = self.characterBody.damage * self.damageCoefficient,
              damageColorIndex = DamageColorIndex.Default,
              owner = self.gameObject,
              procChainMask = new ProcChainMask(),
              force = 0.0f,
              useFuseOverride = false,
              useSpeedOverride = false,
              target = (GameObject)null,
              projectilePrefab = FireLunarShards.projectilePrefab
            });
          }
        }
        self.PlayAnimation("Gesture, Additive", nameof(FireLunarShards));
        self.PlayAnimation("Gesture, Override", nameof(FireLunarShards));
        self.AddRecoil(-0.4f * FireLunarShards.recoilAmplitude, -0.8f * FireLunarShards.recoilAmplitude, -0.3f * FireLunarShards.recoilAmplitude, 0.3f * FireLunarShards.recoilAmplitude);
        self.characterBody.AddSpreadBloom(FireLunarShards.spreadBloomValue);
        EffectManager.SimpleMuzzleFlash(FireLunarShards.muzzleFlashEffectPrefab, self.gameObject, FireLunarShards.muzzleString, false);
      }
      if (shrineActivated && phaseCounter == 0)
      {
        if (self.isAuthority)
        {
          Ray aimRay = self.GetAimRay();
          Transform transform = self.FindModelChild(FireLunarShards.muzzleString);
          if (transform)
          {
            aimRay.origin = transform.position;
          }
          FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
          fireProjectileInfo.position = aimRay.origin;
          fireProjectileInfo.rotation = Quaternion.LookRotation(aimRay.direction);
          fireProjectileInfo.crit = self.characterBody.RollCrit();
          fireProjectileInfo.damage = self.characterBody.damage * self.damageCoefficient;
          fireProjectileInfo.damageColorIndex = DamageColorIndex.Default;
          fireProjectileInfo.owner = self.gameObject;
          fireProjectileInfo.procChainMask = default(ProcChainMask);
          fireProjectileInfo.force = 0f;
          fireProjectileInfo.useFuseOverride = false;
          fireProjectileInfo.useSpeedOverride = false;
          fireProjectileInfo.target = null;
          fireProjectileInfo.projectilePrefab = FireLunarShards.projectilePrefab;

          for (int i = 0; i < ModConfig.LunarShardAdd.Value; i++)
          {
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            aimRay.direction = Util.ApplySpread(aimRay.direction, 0f, self.maxSpread * (1f + 0.45f * i), self.spreadYawScale * (1f + 0.45f * i), self.spreadPitchScale * (1f + 0.45f * i), 0f, 0f);
            fireProjectileInfo.rotation = Quaternion.LookRotation(aimRay.direction);
          }
        }
      }
      orig(self);
    }
    private void FistSlamOnEnter(On.EntityStates.BrotherMonster.FistSlam.orig_OnEnter orig, FistSlam self)
    {
      if (shrineActivated)
      {
        FistSlam.waveProjectileDamageCoefficient = 2.3f;
        FistSlam.healthCostFraction = 0.0f;
        FistSlam.waveProjectileCount = 20;
        FistSlam.baseDuration = 3.5f;
      }
      orig(self);
    }
    private void FistSlamFixedUpdate(On.EntityStates.BrotherMonster.FistSlam.orig_FixedUpdate orig, FistSlam self)
    {
      if (shrineActivated)
      {
        if ((bool)(UnityEngine.Object)self.modelAnimator && (double)self.modelAnimator.GetFloat("fist.hitBoxActive") > 0.5 && !self.hasAttacked)
        {
          if (self.isAuthority)
          {
            Ray aimRay = self.GetAimRay();
            float num = 360f / (float)FistSlam.waveProjectileCount;
            Vector3 vector3 = Vector3.ProjectOnPlane(self.inputBank.aimDirection, Vector3.up);
            Vector3 footPosition = self.characterBody.footPosition;
            Vector3 corePosition = self.characterBody.corePosition;
            for (int index = 0; index < FistSlam.waveProjectileCount; ++index)
            {
              Vector3 forward = Quaternion.AngleAxis(num * (float)index, Vector3.up) * vector3;
              ProjectileManager.instance.FireProjectile(golemProjectile, corePosition, Util.QuaternionSafeLookRotation(forward), self.gameObject, (self.characterBody.damage * FistSlam.waveProjectileDamageCoefficient) / 4, 0f, Util.CheckRoll(self.characterBody.crit, self.characterBody.master), DamageColorIndex.Default, null, -1f);
              ProjectileManager.instance.FireProjectile(exploderProjectile, corePosition, Util.QuaternionSafeLookRotation(forward), self.gameObject, (self.characterBody.damage * FistSlam.waveProjectileDamageCoefficient) / 10, 0f, Util.CheckRoll(self.characterBody.crit, self.characterBody.master), DamageColorIndex.Default, null, -1f);
              ProjectileManager.instance.FireProjectile(FireLunarShards.projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), self.gameObject, self.characterBody.damage * 0.1f / 12f, 0f, Util.CheckRoll(self.characterBody.crit, self.characterBody.master), DamageColorIndex.Default, null, -1f);
            }
          }
        }
      }
      orig(self);
    }

    private void SpellChannelEnterStateOnEnter(On.EntityStates.BrotherMonster.SpellChannelEnterState.orig_OnEnter orig, SpellChannelEnterState self)
    {
      if (shrineActivated)
      {
        if (Run.instance.loopClearCount == 0)
          SpellChannelEnterState.duration = 20;
        else
          SpellChannelEnterState.duration = 20 / Run.instance.loopClearCount;
      }
      orig(self);
    }

    private void SpellChannelStateOnEnter(On.EntityStates.BrotherMonster.SpellChannelState.orig_OnEnter orig, SpellChannelState self)
    {
      if (shrineActivated)
      {
        int loopClearCount = 1;
        if (Run.instance.loopClearCount != 0)
          loopClearCount = Run.instance.loopClearCount;
        SpellChannelState.stealInterval = 0.75f / loopClearCount;
        SpellChannelState.delayBeforeBeginningSteal = 0.0f;
        SpellChannelState.maxDuration = 15f / loopClearCount;
        self.PlayAnimation("Body", "SpellChannel");
        int num = (int)Util.PlaySound("Play_moonBrother_phase4_itemSuck_start", self.gameObject);
        self.spellChannelChildTransform = self.FindModelChild("SpellChannel");
        if ((bool)(UnityEngine.Object)self.spellChannelChildTransform)
          self.channelEffectInstance = UnityEngine.Object.Instantiate<GameObject>(SpellChannelState.channelEffectPrefab, self.spellChannelChildTransform.position, Quaternion.identity, self.spellChannelChildTransform);
      }
      else
        orig(self);
    }
    private void SpellChannelStateOnExit(On.EntityStates.BrotherMonster.SpellChannelState.orig_OnExit orig, SpellChannelState self)
    {
      orig(self);
      if (shrineActivated)
      {
        // Spawn in BrotherHaunt (Random Flame Lines)
        GameObject brotherHauntGO = Instantiate(BrotherHaunt);
        brotherHauntGO.GetComponent<TeamComponent>().teamIndex = (TeamIndex)2;
        NetworkServer.Spawn(brotherHauntGO);
      }
    }

    private void SpellChannelExitStateOnEnter(On.EntityStates.BrotherMonster.SpellChannelExitState.orig_OnEnter orig, SpellChannelExitState self)
    {
      if (shrineActivated)
      {
        SpellChannelExitState.lendInterval = 0.04f;
        SpellChannelExitState.duration = 2.5f;
      }
      orig(self);
    }

    private void StaggerEnterOnEnter(On.EntityStates.BrotherMonster.StaggerEnter.orig_OnEnter orig, StaggerEnter self)
    {
      if (shrineActivated)
        self.duration = 0.0f;
      orig(self);
    }

    private void StaggerExitOnEnter(On.EntityStates.BrotherMonster.StaggerExit.orig_OnEnter orig, StaggerExit self)
    {
      if (shrineActivated)
        self.duration = 0.0f;
      orig(self);
    }

    private void StaggerLoopOnEnter(On.EntityStates.BrotherMonster.StaggerLoop.orig_OnEnter orig, StaggerLoop self)
    {
      if (shrineActivated)
        self.duration = 0.0f;
      orig(self);
    }

    private void TrueDeathStateOnEnter(On.EntityStates.BrotherMonster.TrueDeathState.orig_OnEnter orig, TrueDeathState self)
    {
      if (shrineActivated)
      {
        TrueDeathState.dissolveDuration = 3f;
        // Kill BrotherHaunt once MithrixHurt dies
        GameObject.Find("BrotherHauntBody(Clone)").GetComponent<HealthComponent>().Suicide();
      }
      orig(self);
    }

    private void CleanupPillar(On.EntityStates.BrotherMonster.WeaponSlam.orig_OnEnter orig, EntityStates.BrotherMonster.WeaponSlam self)
    {
      if (shrineActivated)
      {
        GameObject projectilePrefab = EntityStates.BrotherMonster.WeaponSlam.pillarProjectilePrefab;
        projectilePrefab.transform.localScale = new Vector3(1f, 1f, 1f);
        projectilePrefab.GetComponent<ProjectileController>().ghostPrefab.transform.localScale = new Vector3(1f, 1f, 1f);
      }
      orig(self);
    }
  }
}