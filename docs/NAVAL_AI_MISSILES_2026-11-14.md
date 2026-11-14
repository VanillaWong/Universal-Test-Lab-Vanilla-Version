# AI 舰导弹可用性（2026-11-14）

> 结论先写：**不是 bug，是数据设定问题**。这些舰船是 AI 专用资产，玩家接管后导弹相关字段从未做过调校。
> 已通过"生成时整份拷贝 + 固定补丁"解决，不改任何原厂文件。

---

## 1. 范围内的问题舰

`data/ships.json` 里 `nation == "Other"` 或 `rank == 0` 的舰船（663 艘中的绝大多数），典型：

| 舰船 | 症状（修复前） |
|---|---|
| `cn_frigate_type054a` | 导弹完全打不出（导引头打不开） |
| `cn_destroyer_type052d` | 自动开火、雷达抽风、导弹不跟目标 |
| `us_destroyer_halsey` | 导弹显示"弹药耗尽" |
| `cn_aircraftcarrier_fujian` / `us_cruiser_rckt_california` / `ussr_destroyer_rckt_pr956` | 导弹发射不出来 |

`us_ashville_class_douglas`（USA rank 4，科技树）是真可玩船，可作对照组。

---

## 2. 根因清单

| # | 根因 | 证据 |
|---|---|---|
| 1 | `applyAllMods:b=no` | 舰船块来自飞机模板；Ask3lad 的 Halsey 是 `yes` |
| 2 | 弹药槽被枪炮占满 | `bulletsN` 是可选弹种列表，枪炮在前则导弹"弹药耗尽" |
| 3 | `special` 组玩家不可见 | HHQ-10 / SM-2 都挂在这个组 |
| 4 | 固定发射井 `fireConeAngle=1` | `limits yaw/pitch = 0,0`，发射架无法对准目标 |
| 5 | `semiActive:b=true` | 半主动需要持续照射，玩家侧火控不开这条通道 |
| 6 | `uncageBeforeLaunch:b=true` | 发射前需开导引头，VLS 打不出 |
| 7 | 导引头频段 9 | 全游戏海军雷达只有 band 2/5/6/8 |
| 8 | 老式自动驾驶（3 个固定增益） | SM-6 用分段 PID + `propNavMult` + `timeToHitGain` |
| 9 | **舵面权限 `finsAoa = 0.125`** | SM-6 是 0.35；`loadFactorMax` 20 vs 24 —— 这是"笨拙"的直接原因 |
| 10 | 光学导引头 `lockAngleMax = 15` | SM-2 IIIB 是 90 |
| 11 | 光学制导缺瞄具 | AI 舰没有 `ship_optical_sight` 传感器 |

---

## 3. 最终补丁（生成时执行）

`UniversalTestLab.Naval.cs`：

| 方法 | 作用 |
|---|---|
| `MissionAmmo` | 导弹优先占前 4 槽；无命名容器的制导组用空串占位，绝不跳号 |
| `DeclareAmmo` | 把槽位引用的弹种补进 `modifications`（否则 `Cannot find bulletMod`） |
| `PlayerRadars` | 目前**停用**（052D 用原装 SPY-1 表现更好）；需要时取消注释 |
| `EnsureOpticalSight` | 有光学制导弹时补 `ship_optical_sight` |
| `GuidedCopy` | 弹文件副本：`semiActive→false`、`uncageBeforeLaunch→false`、`band 9→8` |
| `DonorAutopilot` | 无 `pid0` 的弹，整块借用 SM-6 的 `selfHomingAutopilot` |
| `RestoreFinAuthority` | `finsAoaHor/Ver 0.125→0.35`、`loadFactorMax 20→24` |
| 组拆分 | `special` 组制导弹 → `rockets`；与导弹同组的机炮 → `machinegun` |
| 发射架 | 固定井 `fireConeAngle → 180`（筒不转，垂直发射） |

`UniversalTestLab.BlkTools.cs`（`ConfigureShipPlayer`）：
- `applyAllMods:b=yes`
- `crewSkillK:r=1`
- 弹药槽按 `MissionAmmo` 写入

`UniversalTestLab.MainForm.Generate.cs`（`BuildCustomShip`）：
- AI 舰生成**整份拷贝**（`pkg_local/.../userVehicles/utl_naval_player.blk`）
- 预设优先 `<id>_default`，不吃 ships.json 的 AI 鱼雷预设

---

## 4. 数值比对（存档）

### HHQ-9 vs SM-6：163 个字段中 33 个不同

制导部分几乎全同（band 9、55°、35 km、loft -5/1.35、加速 170、控制时间 1.75、战斗部 64 kg）。

| 字段 | HHQ-9 | SM-6 |
|---|---|---|
| finsAoaHor / Ver | **0.125** | 0.35 |
| loadFactorMax | 20 | 24 |
| thrustVectoringAngle | — | 0.025 |
| 自动驾驶 | 3 个固定增益 | 分段 PID + propNavMult |
| 推力 | 184000 单段 | 四段 |
| 末速 | 1300 m/s | 1540 m/s |
| 最大射程 | 125 km | 240 km |

### HHQ-9 vs S-300（5V55R）：180 个字段中 86 个不同

共用模型 `s300ps_missile_5v55r`，但弹道独立：

- 口径 470 vs 508，重量 1300 vs 1664，战斗部 64 vs 100 kg
- 射程 125 vs 75 km，最小射程 1 vs 8 km
- 过载 20 vs 27 G；末速 1300 vs 1875
- 音效用的是山毛榉 `atgm_buk`

结论：HHQ-9 是**拼装资产**（S-300 模型 + Buk 音效 + 独立数值表），机动性比同源 S-300 还低。

---

## 5. 实测状态

| 舰船 | 状态 |
|---|---|
| `cn_frigate_type054a`（HHQ-16） | ✅ 垂直发射、命中 |
| `us_destroyer_halsey`（SM-2 / SM-6） | ✅ 两型都能打，精度高 |
| `cn_destroyer_type052d`（HHQ-9 / HHQ-10） | ✅ 修复后可命中 |
| 福建 / 加州 / 佩里 / 956 / 福特 / 尼米兹 / 库兹涅佐夫 | ⬜ 未逐一实测（规则已自动套用） |

---

## 6. 复现/回归要点

- 只对 `aiOnly`（`nation=Other || rank=0`）生效，科技树舰船走纯 `include`，零改动
- 补丁文件全部写在 `content\pkg_local`，删除即可回滚
- 生成后需**完全重启游戏**（单位文件在启动时加载）
- 验证：`dist\UniversalTestLab.exe --selftest`
