# Ask3lad Test Drive 3.0 — 逆向规格(2026-11-11 抓取)

> 来源:黑盒差分。他在 `content/pkg_local` 写出的是**明文 blk**,我们把它和
> `universal_*_data` 里的原生文件逐行对比即可还原机制,不需要他的源码。
> 捕获工具:`node tools/capture-ask3lad.js`(结果在 `captures/ask3lad-<时间戳>/`)。
> 他自身版本:`3.0.0-alpha.24.3`;备份目录 `%APPDATA%\ask3lad.testdrive3\backups\<时间戳>\`。

## 0. 总体架构

| 项 | 他的做法 | 对比 UTL |
|---|---|---|
| 玩家单位 | **整份原始单位文件复制**到 `pkg_local\gameData\{units\tankModels, flightModels}\ask3lad_*_player.blk`,再按开关**行级注入** | UTL 用薄代理 `userVehicles/<壳>.blk` + `@override:Weapon[1]` / `preset[1]` |
| 关闭/重置 | 重新写一份干净副本(只多 2 行注释) | — |
| 空战机体 | 额外复制一份 **`flightModels/fm/ask3lad_air_player.blk`**,单位里用 `fmFile:t` 指过去 | 无 |
| 武器/挂载 | 每个受影响的武器生成一个**薄包装**:`include` 原文件 + `@override:字段` | 无 |
| 配置回读 | 文件头写一行 JSON 注释 `// Ask3lad loadout: {...}` | 无 |
| 备份 | 每次应用存一份改动前的原文件 + `backup.json`(含 `sha_after`) | 无 |

**结论:他的可靠性来自"整份拷贝",弹性来自"薄包装 + 单字段 override"。**

## 1. 功能开关 → 注入机制(空侧,2026-11-11 EF-2000 AESA 实测)

文件头注释即完整配置结构:

```json
{"cm":[{"slot":0,"option":"bk_27_common","n":0,"flares":0,"chaff":16}, ...],
 "basePreset":"ef_2000_aesa_default",
 "weightless":true, "indestructible":true, "zeroMass":true,
 "cannon":{"blk":"gameData/Weapons/cannon_mauser_bk_27.blk","donor":"ef_2000_aesa","donorKind":"air",
           "ammo":1000,"rate":10,"shellMass":10,"filler":50},
 "radar":{"from":"ef_2000_aesa"},
 "wtune":{}, "ff":true,
 "maws":{"warner":"praetorian","jammer":"dircm"},
 "hmd":true, "drag":{}, "rtune":{}, "gtune":{}}
```

| 开关 | 实现 | 落点 |
|---|---|---|
| **weightless**(挂载无重无阻) | 每个挂载生成薄包装:`include` 原武器 + `"@override:rocket" { "@override:dragCx":r = 0 }`;炸弹额外 `"@override:bomb" { "@override:mass":r = 1 }` | `weapons/ask3lad/ask3lad_light_0..23.blk` |
| **cannon**(武器替换+调校) | `include` 原炮 + 顶层单字段 override + 弹体 override | `weapons/ask3lad/Ask3ladBigCannonSir.blk` |
| **radar** | `sensors{}` 内替换 `sensor.blk`;沿用宿主 `sensorsMode` | 单位 `sensors` 块 |
| **hmd** | 一组单位级字段(见 §2) | 单位顶层 |
| **maws** | 注入 `sensor { blk:t="gameData/sensors/uk_praetorian_maw.blk" }` + DIRCM jammer | `sensors` 块 |
| **cm**(干扰弹) | 生成 `ask3lad_cm_0/1.blk`(按槽位/连发组合),`commonWeapons` 指向它 | `weapons/rocketguns/ask3lad/` |
| **机体调校**(engine/airframe) | 复制 `fm/<机型>.blk` 改数值,单位里 `fmFile:t = "fm/ask3lad_air_player.blk"` | `flightModels/fm/` |
| **wtune / rtune / gtune / drag** | 武器/火箭/炮的薄包装副本改字段 | `weapons/ask3lad/ask3lad_wtune_*` |
| **ff** | 见 §4(待补) | — |

## 2. HMD 注入(直接可抄)

```blk
havePointOfInterestDesignator:b = yes
hasHelmetDesignator:b = yes
helmetDesignatorZone0:p3 = -70, -35, 65
helmetDesignatorZone1:p3 = -35, -35, 65
helmetDesignatorZone2:p3 = 0, -35, 65
helmetDesignatorZone3:p3 = 35, -35, 65
helmetDesignatorZone4:p3 = 70, -35, 65
hmdEnabledByDefault:b = yes
hmdScreenBlockRad:r = 0.06
```

## 3. 机体调校(FM)具体改了什么

对 `fm/ef_2000_aesa.blk` 的改动只有 6 个值:

| 字段 | 原生 | 他改成 | 含义 |
|---|---|---|---|
| `VNE` | 1607 | **5000** | 最大允许空速 |
| `MNE` | 2.46 | **10** | 最大允许马赫 |
| `CritOverload` | -430000, 810000 | **-4300000, 8100000** | 临界过载(×10) |
| `EmptyMass` | 11220 | **112** | 空重(→"零质量") |
| `GearDestructionIndSpeed` | 700 | **5000** | 起落架损毁速度 |
| `CockpitOpenedDoorBreakSpeed` | 450 | **5000** | 开舱盖损毁速度 |

## 4. 武器调校样板(薄包装 + 单字段 override)

```blk
// 例:Ask3ladBigCannonSir.blk
include "#/develop/gameBase/gamedata/Weapons/cannon_mauser_bk_27.blk"

"@override:shotFreq":r = 10          ← 射速
reloadTime:r = 0.1
"@override:reloadTime":r = 0.1
bEnablePause:b = no
"@override:bEnablePause":b = no
"@override:recoilOffset":r = 0.0     ← 后坐
"@override:recoilTimes":p2 = 0.01, 0.01
"@override:bullet" { recoilMultiplier:r = 0.0 }

"@override:bullet" { "@override:mass":r = 10 }            ← 弹重
"@override:bullet" { "@override:explosiveMass":r = 50 }   ← 装药
```

**`"@override:字段":类型 = 值` 是顶层写法**,可以只改 include 进来的文件的某一个标量,不必复制整份。
这是整个逆向里最值得抄的一个语法。

## 5. 地面侧(已确认部分)

```blk
// 支援载具(2 行)
support_unit_class:t = "structures/ussr_152mm_d1"
support_unit_tag:t = "artillery"

// 支援飞机(4 行)
allowSupportPlane:b = true
supportPlane {
    supportPlaneClass:t = "ask3lad_ground_support"
    behTreeName:t = "recon_micro"
    planeStartSpeed:r = 500
}
```

武器替换在地面侧的附加动作(相对于原生文件):

```
+ auxiliary_power_unit { ... }          ← 补供电单元
+ hp:r = 50 / armorThrough:r = 3 / dmPart:t = "electronic_equipment_dm"
− 全部 verDriveDm / horDriveDm / shootingDmPart / stabilizerDmPart 的火控/液压绑定
− stopSignalLights { ... }              ← 残块清理
```

## 6. 尚未抓到的(需要再开一次开关)

- 热成像瞄准镜
- 瞄准镜倍率
- 炮塔覆盖(单独开,避免和武器替换混淆)
- 引擎替换(地面)

抓法:开关 → 应用 → `node tools/capture-ask3lad.js`。

## 7. 我们借鉴的优先级

1. **薄包装 + `@override:单字段`** —— 对武器/传感器这类叶子文件,比整份复制更省更稳
2. **整份单位拷贝替代 `@override:Weapon[1]`** —— 根治下标错位类 bug(防空车 preset 那次)
3. **HMD / MAWS / support 字段** —— 纯字段,复制即可
4. **`fmFile:` + 自建 fm 副本** —— 机体/引擎调校的正确姿势
5. 载具级 `weightless / indestructible / zeroMass` —— 玩家最直观的爽点
