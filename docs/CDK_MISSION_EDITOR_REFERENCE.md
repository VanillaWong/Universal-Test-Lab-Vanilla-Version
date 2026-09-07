# CDK Mission Editor 官方 Schema 参考(2026-09-07 研究)

> 来源:`<游戏根>\WarThunderCDK\dagor_cdk\windows-x86_64\plugins\daEditorX\missions\`
> 性质:Mission Editor(daEditor3x missionEd2 插件)"加触发器/单位属性"对话框的字段级定义
> (Squirrel .nut)。**这是生成/校验任务 BLK 触发器字段的权威字典**——比 wiki 更全、带
> 默认值与 tooltip。CDK 编辑器本体打不开(缺 settings.blk,已补最小配置于 CDK 根),但
> schema 是纯文本可直接读。
>
> 本文只记录"读过并确认"的条目;未读大项见 §8。

## 1. 目录地图

| 目录 | 数量 | 内容 |
|---|---|---|
| `events/` | 4 | initMission / periodicEvent(time) / timeExpires(time) / unitWhenInArea(event 形态) |
| `conditions/` | 88 | 触发器条件(含 wiki 无的 unitWhenRespawn / airfieldWhenStatus / isRadarMode / weaponLock / unitsAsSensorTargets / unitWhenObstructed / unitWhenHitOtherUnit / playerTankRangefinderValue / playerAnySupportPlaneIsDead) |
| `actions/` | 186 | 触发器动作(含 missionRestart / missionAttempts / missionCreateRespawnBasesByTemplate / moveRespawnBase / objectGroupSetAirfield 等 wiki 表没有的) |
| `units/` | 8 | 单位 Object Properties 面板定义:aircraft / helicopter / human / infantry / objects_group / squad / area_squad / common |
| `misObjs/` | 1 | abstract(任务目标面板) |
| `waypoints/` | 4 | none / normal / attack / land |

schema 语法:`::scheme <- { controls = [ {name, type, def, options|enum..., extensible, itemsCount, visible, tooltip, onChange} ] }`
- `target_object{target_type=unit|area|unit_or_area|var_int|var_real|var_all|dialog|locStr}` 引用单位/区域/变量
- `extensible=true` + `itemsCount=[n,m]` = 可加多个该字段(UI "+"按钮)
- `group` = 子字段组(可 extensible 重复);`onChange` 只是 UI 显隐联动
- 枚举型动作/条件(如 unitWhenStatus.object_type)即游戏内真实枚举值,直接对应 BLK 字符串

## 2. 重生/出生核心(UTL 最相关)

### missionAttempts(SP)
```
action: enum ["increase","reduce","restore","set","set_max"]  def="restore"
value: int  def=0
```
> **修正旧归档错误**:attempts 次数**可调**(SP 动作,set/set_max 可指定数值/上限)。
> UTL 已全 manual+spawnOnAirfield,不受影响;但"官方无字段可调上限"的说法作废。
> 若未来恢复 attempts 玩法:initMission 触发器加 `missionAttempts{action:t="set_max" value:i=999}`。

### spawnOnAirfield
```
runwayName: target_object(unit_or_area)      ← 可指向 area!(不限于命名跑道)
objects:    target_object(unit) [0..16]
takeoffInterval: real def=-1, extensible [0,1]  ← 起飞间隔
```

### unitRespawn(死后/事件复活全参数)
```
object: unit [1..12]          object_type: 同 status 枚举, def="any"
target: unit_or_area [1..12]  target_type: 同枚举 def="any"
delay: real def=1             offset: point3 def=[0,0,0]
randomSpawn: bool def=false   isStealth def=false
randomObjectsCount(+Var); setObjectMarking / object_marking 0..31
formation{formation_type rows|cols, formation_div, formation_step point2, formation_noise}
resetFormation / shouldKeepFormation(def=true; count>1 的 armada 设 false 否则重生到长机身上)
needStopOnRespawn def=true
strictAxisOriented(沿 target 区域方向落地,否则平行地面)
lookAtTargetUponRespawn / selectUnitsOrderForRespawn first|middle|last / resetAxis
```
> UTL 地面触发器用的 delay/offset/object/target 字段全部合法 ✓。

### unitRespawnAfterTime(周期/延时重生)
object_name unit 1-12 + object_type;just_restore def false;place_to_respawn(area);
time_to_respawn real def1;cycle_respawn def true(循环)。核战 600s/900s 复用思路。

### missionRestart / changeUnit
- missionRestart:controls 空。动作存在;usermission 实测报 not supported(结论不变)。
- changeUnit(有完整 schema;wiki 称不工作,UTL 曾弃用 Typhoon 桥):
  object 1-16+object_type;unit_class text;weaponPreset text;bullets0/bulletsCount0(def999);
  type tank|ship|plane(def tank);target unit_or_area(传送);isIndividual;hangarMission。

## 3. missionMarkAsRespawnPoint(多人生成点注册全参数,11KB)

```
target: unit_or_area [1..50]    object_type: status 枚举 def="any"
team: enum [Both,B,A] def="Both"
kamikazeResp bool
spawnArrangeTo: unit_or_area(朝向);areaNameForSortingByProximity;PosRecalcDelay real def=30
useForTriggerFiltered
loc_name: text  ← "key for localization!"(核战 missions/airfield_spawn1..5 就是它)
isStrictSpawn(按 squad 顺序+精确朝向) / isStrictPosAndDir(不用安全位置)
shouldSpawnAwayFromOtherPlayers / resetStrictSpawnIndex
isAirfield bool(显示 object/autoRespawn/separate_fuel_time/modular_airfield)
  autoRespawn real def -1;separate_fuel_time;modular_airfield(启 cargo/live/parking 模块)
isUnit bool(显示 offset/backupZone/radius):backupZone area [0..12]("unit 死后此备用区激活")
  respawnAirBackup text(机场起飞距离不足时的备用空中区)
  spawnOffset point3(一区多 spawn)
forceCreate / useExisting(需 replaceAreas) / ignoreTeamsOnReuse(占点)
isIndividual / onlyOnePlayerPerSpawnPoint
removeAreas / replaceAreas / canSpawnOnNeutral
showOnMap def=true / hideForEnemy
object: unit [0..12](isAirfield=yes 时指向机场实体)
awardByBodyHp def=true / disableAfter int def=-1
tags{...}: 见 §6 通用 tags(载具+国家白名单)
```
> 佐证:核战 42 个注册点结构与此一致;**MP 界面消费**(wiki 标 MP only),usermission SP
> 无重生界面 → UTL 单机注册无效(矩阵结论不变)。

## 4. 机场族

### addAirfield
```
runwayStart: area   runwayEnd: area   runwayWidth: real def=10
army: enum_int [0,1,2] def=0
spawnPoint: area, extensible [0..32]   ← 可多 spawnPoint!
visibleOnHud: bool def=true
cable{ start:area end:area } extensible [0..32]   ← 隐藏功能:拦阻索/电缆对
```
> UTL 用法全部合法;spawnPoint 支持多个、cable 组可研究(航母/短跑道拦阻?)。

### airfieldSetProperties(机场行为/模块 HP)
object unit_or_area [0..12];repairMul / fuelMul / reloadMul(real def1,+Var);
enemySurrenderOnLanding;group airfield{hp,hp_var} / storage / parking / dwelling / bombing_zone(均可 extensible)
> 引擎机场=模块化 HP:airfield/storage/parking/dwelling/bombing_zone(核战 HP 结构照应)。

### airfieldSetIndication / airfieldSetVisibility / airfieldAddModuleHP / objectGroupSetAirfield
- Indication:target 1-12 + team Both|A|B|None + set bool
- Visibility:target 1-16 + team
- AddModuleHP:module airfield|storage|parking|dwelling + hp/hp_var
- objectGroupSetAirfield:army enum_int def2 + target unit 1-12(把对象组标记为机场)

### moveRespawnBase(可移动重生点挂载)
target unit_or_area [1..50] + loc_name(text)+ team [Both,B,A]+ isAirfield。航母/移动出生点方向。

### missionMarkAsLandingZone
target 1-12 + visible def true + spawnEffect def true + radius int def50 + …(尾部未读)

## 5. 常用条件(已确认)

### unitWhenStatus —— object_type 枚举(远比 wiki 全):
isAlive hittedBy killedBy isKilled canFight cantFight noAmmo isDelayed isActive
isInactive damaged isHellfireMarked isNotHellfireMarked haveKilledParts isMarked
isNotMarked isUnitVar isShooting isOnline isOffline notDamaged noBombs canBomb
isOnGround isInAir isDamagedByPlayer isDamagedByPlayerBombOrTorp isKilledByPlayer
hasBombsInWorld hasTorpedoesInWorld hasMissilesInWorld noBombsInWorld
noTorpedoesInWorld noMissilesInWorld isUseless isUsefull isTargetedByPlayer
isTeamA isTeamB isTeamNeutral hasKilledTarget hasTarget hasBreach hasFire
isRepairNeeded
```
另:check_objects enum [all,any,one] def=any;force_check_delayed;unit_type_ex 同枚举。
> UTL 用 isKilled ✓;`hasFire`/`isRepairNeeded`/`isInAir`/`isOnGround`/`noAmmo` 可做更细任务事件。

### unitWhenRespawn(condition,死后检测的另一种姿势)
object unit [1..10] + object_var_name / object_var_comp_op(equal|notEqual|less|more)/
object_var_value。无 object_type —— 语义="单位发生重生"。

### 通用 target_type/object_type 长枚举(unitRestore/changeUnit/playerForceMoveToRespawnScreen/squad 用)
any isAlive canFight canBomb damaged notDamaged noBombs isKilled cantFight killedBy isActive
isDelayed isHellfireMarked isNotHellfireMarked haveKilledParts isMarked isNotMarked isUnitVar
isShooting isOnline isOffline isInactive isOnGround isInAir isFormationLeader isDamagedByPlayer
isPlayer isNotPlayer isDamagedByPlayerBombOrTorp isKilledByPlayer hasBombsInWorld
hasTorpedoesInWorld noBombsInWorld noTorpedoesInWorld isValid isUseless isUsefull isTeamA
isTeamB isTeamNeutral isTargetedByPlayer hittedBy noAmmo

### events 字段
initMission(空)/ periodicEvent{time real def1, var int}/ timeExpires{time def400, var int}/
unitWhenInArea{object unit 1-5, target area}(事件形态进区检测)

## 6. 通用 tags 白名单(capture/rearm/respawn point/template 共用)

```
air def=true;type_bomber/fighter/assault/interceptor/aa_fighter/light_bomber/
frontline_bomber/longrange_bomber def=true;type_hydroplane def=true;
carrier_take_off/airfield_take_off/vtol_jet/none_can_spawn def=true;
human/helicopter/type_attack_helicopter/type_utility_helicopter def=true;
tank/type_heavy_tank/type_medium_tank/type_light_tank/type_tank_destroyer/type_spaa
  def=**false**;artillery/aaa def=true;ship/boat/type_*_cruiser/... def=false;
walker def=false;country_japan/germany/usa/britain/australia/ussr def=true
```
> 陆地车占点/补给必须显式开 tank:b=yes(09-01 归档结论与之一致 ✓)。
> missionMarkAsRespawnPoint 另含 type_submarine / type_human / type_battlecruiser / battleship。

## 7. 单位属性面板(units/,Object Properties 可编辑字段)

### common(全单位)
army;count(>1 显示 formation rows|cols + div/step/noise);uniqueName;
attack_type: fire_at_will|fire_at_will_gnd|fire_at_will_air|dont_aim|hold_fire|attack_target|
attack_player|kill_target|kill_player|return_fire|kill_player_A|attack_player_A|kill_player_B|
attack_player_B(def fire_at_will);targetAir/targetGnd;use_search_radar;isDelayed def true;
stealthRadius def-1/setStealth/calmDetection;accuracy 0-1 def0.9;effShootingRate;
avoidObstacles;targetableByAi;groupAsOne;skin;unitReplacementType

### aircraft(飞机)
player bool;wing_formation Diamond|Line|Finger|Echelon|Vee|Column + row/col_distances +
super_formation/super_row/col_distances;ai_skill ROOKIE..ACE;task NONE|WAIT|STAY_FROMATION|
FLY_WAYPOINT|DEFENCE|DEFENDING|ATTACK_AIR|ATTACK_GROUND|ATTACK|TAKEOFF|LANDING;
count 1-128;numInWing 1-16;free_distance def70;floating_distance def50;
minimum_distance_to_earth def20;altLimit def6000;attack_type 全枚举;accuracy;fuel 0-100;
speed;skill 0-5 def4;aiEnabled;ikPilotModel/ikGunnerModel;canLeaveRouteForAtack;allowAntimissile
→ 模板 You armada props(army/count/free_distance/attack_type/skill/speed/plane{...})同构 ✓

### objects_group / squad / misObjs
- objects_group:army/active
- squad:squad_members 1-255 + object_type/object_marking(0-31)
- misObjs/abstract:isPrimary def true;timeLimit int def1800;blink def true;alwaysShow def false;
  dialogueAdd|Complete|Failed(dialog);failDesc(object_or_text locStr);team Both|B|A

### waypoints
- normal:speed def300;tas;moveType MOVETO_STRAIGHT|MOVETO_ZIGZAG|ZIGZAG|HALT|USE_SPLINES|
  GATHER_TO(def MOVETO_STRAIGHT);zzPeriod def3/zzAmp def50;shouldKeepFormation(USE_SPLINES);
  waitTime;canUsePathFinder;waypointReachedDist def10
- attack:speed def300 + tas;land:空

## 8. 覆盖状态(截至 2026-09-07 晚深读)

字段级已深读:events 4;重生/出生核心(missionAttempts/spawnOnAirfield/unitRespawn/
unitRespawnAfterTime/unitRestore/missionRestart/missionCreateRespawnBasesByTemplate/
missionMarkAsRespawnPoint);标记动作(missionMarkAsCaptureZone/RearmZone/LandingZone 主体);
机场族(addAirfield/airfieldSetProperties/Indication/Visibility/AddModuleHP/moveRespawnBase/
objectGroupSetAirfield);changeUnit/playerForceMoveToRespawnScreen;unitSetProperties(主体);
条件 unitWhenStatus/unitWhenRespawn(其余 86 个仅名录);单位面板 common/aircraft/
objects_group/squad;misObjs/abstract;waypoints normal/attack/land。

仍可继续(按需):helicopter/human/infantry/area_squad 单位面板、unitSetProperties 尾部少量
movement 字段、其余 ~160 个 action 的逐字段细节(文件名即语义,写触发器时按名抽查)。

## 9. 深读补遗(要点)

### missionMarkAsCaptureZone(占点,对照 09-01 ctr_capture_zone)
target area 1-12;zoneDefenders{defender unit 0-16}(防守单位存活不可占);iconIndex 0-3(+Var);
army;canCaptureOnGround def false/canCaptureInAir def true/canCaptureByGM/onlyPlayersCanCapture;
playAirfieldSound;zoneType capture|domination|capture_one_time|capture_hold|supremacy
(supremacy:无票耗+结束事件);silent_mode/auto_smoke/useHUDMarkers;timeMultiplier ×20=单人占秒;
disableZone;airfield/createRespawnBaseFromAirfield/respawnBase(area)/name_for_respawn_base/
makeRespawnBaseAsDefault(把占点变成重生基地!);captureNoPenalty;
markUnitPreset tank_decal|ship_buoys(markUnits 放 decal objgroup/buoys squad);
showBorderOnMap(+borderWidthCoef/borderTransparencyCoef);tags 全套(§6)

### missionMarkAsRearmZone(补给区,addAirfield 之外的独立路径)
area_name unit_or_area 1-50;army;enabled def true;restoreWhenNoAmmoLeft def false;
reloadExplosives def false(tooltip:关区内炸弹/导弹补给);hideMarkers;
needToShowInWorldHUD(区内中心图标);tags 全套

### missionCreateRespawnBasesByTemplate(MP 模板化批量建点)
target text 1-50(模板前缀)+ postfix text + loc_name(本地化键);maxBasesCount def1/
varMaxBasesCount;createRandomBase def true;useExisting/isIndividual/removeAreas/
canSpawnOnNeutral/showOnMap def true/hideForEnemy;team Both|B|A;isAirfield→object/
autoRespawn;isUnit→backupZone/offset(point3)/radius;autoRespawn real def-1;
disableAfter int def-1;awardByBodyHp def true;tags 全套(核战未用它,直接 42 个 markAsRespawnPoint)

### playerForceMoveToRespawnScreen(MP:强制回重生界面)
target unit 1-12 + target_type 全枚举

### unitRestore(Rapid Fire 字段对照)
target + target_type;ressurectIfDead def false;fullRestore def true;supportPlanesRestore def false;
ammoRestore def true;fuelRestore def false;partRestore(尾部略)——与 09-01 归档参数一致

### unitSetProperties(20KB,PVE TI_init 即此动作;分组字段)
- controls/plane 组(同构):ai_skill ROOKIE..ACE;task(枚举同 aircraft);aggressiveWingman;
  kamikaze;silence;targetDeviation{trigger machine gun|cannon|rockets|bombs|torpedoes|gunner,
  defaultVal def0.9, limits [0,1]}(0-6 组);weaponTriggers{trigger,set}
- object/object_type(状态筛选)
- Fire params:gndAccuracy/airAccuracy(0-10 def0.9);maxDeviationAngle(0=不应用,0.0001=最大精度);
  checkVisibilityTarget/visibilityTreeTransparencyThreshold;effShootingRate/airEffShootingRate/
  effShootingRateByPlayer;disableProjectileDamage;cannotShoot(禁射);aiGunnersEnabled/
  aiGunnersCanTargetGroundUnits/aiGunnersDistance(def-1);ignoresEnemy;avoidFriendlyFire;
  trackingTime;adaptiveAccuracyTime;increasedCollisionDamage;bombDelayExplosion;
  maxNumAttackersPerTarget(+GroupIdx)
- Movement params:move_type zigzag_move|stand|move|zigzag_stand|NOE|teleport|use_splines|HALT|
  gather_to|navmesh;speed/speedVar(覆盖航路速度,配 lockSpeed);throttle;waypointReachedDist;
  shipTurnRadius/enableShipCollisionAvoidance/advancedCollisionAvoidance(船);
  slowWhenEnemyNear(0.5)/slowWhenEnemyNearDistance(1000);minDistBetween;ignoreCollisions;
  ignoresObstaclesAfterTime;movable;cannotMove(禁动);allowOvertakeMode(落后追赶);
  followLeader/denyLeadership/keep_leader_dist(船编队);startFullSpeed
