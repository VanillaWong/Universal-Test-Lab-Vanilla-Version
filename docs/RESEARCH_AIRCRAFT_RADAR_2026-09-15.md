# 研究:飞机换雷达可行性(2026-09-15)

> 结论先行:**文件层完全可行,且比地面版更干净**;真正的风险不在"能不能写进去",
> 而在"客户端会不会给 UTL 自建玩家机渲染雷达界面"。下面把已知/未知分开列。

## 1. 一句话结论

飞机换雷达 = 在**我们自己生成的 flightmodel 文件**里换掉 `sensors` 块里那条雷达
`sensor.blk` 路径。因为是纯文本替换、且目标文件本来就是我们写的副本,不存在
`@override`/代理拼接问题。**地面版 `ApplyRadarSwapToProxy` 那套"找 antenna 位、追加
sensor"的复杂逻辑对飞机不适用,也不需要。**

## 2. 注入点(实测确认)

```
玩家机 unit_class = utl_run_<token>_player
        ↓ 引擎按类名找     gameData/flightModels/<类名>.blk
生成端(BuildCustomAircraft,UniversalTestLab.MainForm.Generate.cs:27~185)
   fm = 读取游戏原生 gamedata/flightmodels/<机型>.blk   ← 全文
   ...各处修改 fm 字符串...
   WriteBytes(pkg_user\gameData\flightModels\<classId>.blk, fm)
```

即:**玩家飞机的 flightmodel 全文由 UTL 写进 pkg_user**,`sensors` 块直接改字符串即可。
（旁证:BuildUi.cs:904 也用同样路径写 `utl_safe_player.blk`,说明类名→flightModels 的映射成立。）

## 3. 飞机 sensors 块解剖(与地面不同)

```blk
sensors {
    sensor { blk:t = "gameData/sensors/us_an_awg_9.blk"  dmPart:t = "radar_dm" }  ← 雷达(搜索+跟踪一体)
    sensor { blk:t = "gameData/sensors/us_an_alr_45v.blk" }                       ← RWR(无 dmPart)
    sensor { blk:t = "gameData/sensors/us_harm_a_esm.blk" external:b = true }     ← ESM(可选)
    sensor { blk:t = "gameData/sensors/LaserDesignatorSensor.blk" }               ← 激光指示(可选)
    sensor { blk:t = "gameData/sensors/ir_tracker_tgp.blk" node:t = "optic1_gun" }← IRST/光电(可选)
    sensorsMode { blockSensorIndex:i = 1 }                                        ← 该列表的"模式索引"
    sensorsMode { blockSensorIndex:i = 0 }
}
```

与地面的三点关键差异:

| | 地面(车) | 飞机 |
|---|---|---|
| 雷达数量 | search + track **两个** sensor | 通常**一个** sensor(search/track/illum 全在一份 blk) |
| 绑定方式 | `dmPart:t="antenna_*_dm"` | `dmPart:t="radar_dm"`(或干脆不写 dmPart) |
| 旁挂 | 少 | 常带 RWR / ESM / IRST / 激光指示,**不能碰** |

→ 地面版靠 `blk 名含 "antenna_"` 判角色;飞机上这个判据**完全失效**,必须换一套
（用 `dmPart == radar_dm` 或"该 sensor blk 是 radio 雷达"来定位雷达条）。

## 4. 三个必须解决/绕开的子问题

### 4.1 雷达画面(最大变数)
飞机雷达的**屏幕显示**不在 sensor blk 里,而在 flightmodel 的 `cockpit` 块:

```blk
cockpit {
    multifunctionDisplays {
        display {
            textureArea:p4 = 0, 0, 0.5, 0.5     ← 屏幕归一化坐标(说明是 HUD 叠加层)
            page { type:t = "radar"  customRadar:t = "su27tactic"  font:t = "ils31" }
            page { type:t = "rwr" }
            page { type:t = "hsd" }
        }
    }
}
```

- `textureArea` 是归一化屏幕矩形 → 这一层在**第三人称也渲染**,所以"雷达屏"存在与否
  取决于**宿主机型**有没有 `page{type:"radar"}`。
- `customRadar` 只有 10 种:`fa18cRadarATTK / typhoonRadar / f4Radar / f106Radar /
  su27tactic / mig25Radar / rafaelRadar / jas39radar / jas39Eradar / su30Radar`
  → 是**渲染皮肤**,跟换哪台雷达解耦(换 AWG-9 进 F-4E,画面仍是 f4Radar 风格)。

**统计(1623 个 root flightmodel):**

| 指标 | 数量 |
|---|---|
| 有 radio 雷达 sensor | 368 |
| 雷达 + 有 `page{type:"radar"}`(有屏) | 201 |
| 有雷达但**没有**雷达屏 | 167(F-100D / F-104 / 掠夺者 / 海雌狐 …——游戏原生也没有屏) |

→ 有屏的机型换雷达 = 直接可见效果;**无屏机型需要连 `page{type:"radar"}` 一起注入**
（可从同名布局机型抄一份,`textureArea` 是屏幕坐标,不依赖座舱模型,视觉上能看）。

### 4.2 索引耦合(最容易踩的坑)
`sensors` 列表是**按顺序索引**的,至少被两处引用:
- 顶层 `sensorsMode{ blockSensorIndex:i = N }`(navy/现代机常见,51 个机型有)
- **WeaponPreset 内自己的** `sensors{...}` + `Weapon{ externalSensorIndex:i = N }`
  （吊舱/反辐射弹等外挂传感器,如 Mirage 2000-5F 的 `externalSensorIndex = 4`)

→ **实现铁律:1:1 原位替换 blk 路径,绝不增删 sensor 条目、绝不改顺序。**
（这点和地面版相反——地面版会追加 sensor,那套搬过来会把飞机索引全打乱。）

### 4.3 附带能力会被一起换掉
- **SARH 照射**:368 架里只有 229 架的雷达带 `illuminationTransmitter`。换上不带照射的
  雷达 → 半主动弹打不出去。换的时候必须提示/过滤。
- **IRST 合并型**:Su-27/MiG-29 系用 `sensorTypes` + `irstSearchModes` 把雷达+IRST
  做成一份 blk;换成纯雷达 blk 会**丢 IRST**。
- **`dmPart`**:`radar_dm` 在机体的 `DamageParts` 里(510 架有)。没有该部位的机型
  要么省略 `dmPart`（IR 跟踪器就是这样写的,可用),要么补一个部位定义。
- 目标数与 TWS 来自 sensor blk 的 `weaponTargetsMax` / `twsa` 扫描模式,换了就变。

## 5. 数据准备:目录基本已就绪

- `data/sensors.json`(442 条)**已经包含全部飞机雷达**（`us_an_awg_9 / apq_120 /
  apg_68_v_7 / su_n_019e …` 都在),因为 Build-Catalog 扫的是整个 `gamedata/sensors`
  目录,不分地面/飞机。
- 但现有字段不够,建议扩一版**飞机雷达专用目录**:
  `id / display / type / band / rangeMax / weaponTargetsMax / 是否有 illuminationTransmitter /
  是否 tws / 是否 IR 型(visibilityType=infraRed) / 现有机型引用数`。
- 关键判据:**`type:t="radar"` 不等于真雷达**——光电/IRST 跟踪器(`ir_tracker.blk`、`us_flir.blk` 等)
  在引擎里同样写成 `type:t="radar"`。**不要用 `visibilityType = "infraRed"` 判断**:苏系
  (Su-27 / MiG-29 / J-10 / 台风 / 阵风)把雷达+IRST 合并成一份 blk,里面也有 infraRed 通道。
  实测可用的判据 = **`band:i > 0` 或带 `illuminationTransmitter`** → radio 雷达(330 个);
  其余(band 缺失或 -1)= IR/光电跟踪器(93 个)。另有用 id 关键词(`esm|rwr|spo[-_]|_apr_|harm`)
  识别的**被动/反辐射传感器**——它们也是 radio,但不是雷达,不能选。

## 6. 未知(必须靠一次游戏实测回答)

1. **客户端是否给 UTL 自建机渲染雷达屏/雷达功能?**
   —— 这是唯一的"生死问题"。参考教训:投弹视角结论是"必须是官方机型类名 + 已解锁"。
   雷达比它核心,大概率不受该限制(传感器是模拟层),但**没有直接证据**。
2. **地面雷达替换本身是否已在游戏里验证过?**
   —— 翻遍 ARCHIVE/CHEATSHEET 只有写法与坑的记录(「换 9S35 时只换天线位」等),
   **没有找到"已验证生效"的记录**。若地面版也从未实测,建议先花 5 分钟验证地面版,
   再投飞机版——地面验证成本低得多。
3. 换成跨代雷达(AWG-9 装 F-16)后,`sensorsMode` / MFD 页面是否仍正常切模式。

## 7. 建议的实施路径(最小闭环)

**MVP(先证机制,两周内可完成)**
1. 数据:由 `sensors.json` 生成 `data/aircraft_radars.json`(过滤掉 IR 型、标注 illumination/TWS)。
2. 生成端:`BuildCustomAircraft` 里加 `ApplyAircraftRadarSwap(ref fm, settings)`:
   定位 `dmPart:t = "radar_dm"` 的 sensor(没有则定位第一条 radio 雷达),
   **只替换 `blk:t` 路径**,其余原样;`settings.RadarSearchBlk` 复用现有字段
   （飞机只有一个雷达位,`RadarTrackBlk` 在飞机上忽略或作为"无 dmPart 时的第二候选"）。
3. UI:飞机页加"雷达替换"选择器(读 `aircraft_radars.json`),复用地面雷达选择器组件。
4. 测试用例:**F-14A(AWG-9,有屏)↔ F-4E(APQ-120,有屏)**——两边都有雷达屏,
   能直接对比探测距离/模式数量,排除"没屏"干扰。
5. 反例对照:F-86F(有测距雷达、无屏)换 F-16 的 APG-68 → 验证"无屏机型换上先进雷达"
   的表现（有无锁定能力、是否上屏）。

**第二阶段**:无屏机型注入 `page{type:"radar"}`；IRST 合并型机型的保 IRST 处理；
不兼容提示(缺 illumination 时警告"半主动弹将失效")。

## 8. 进度

- [x] **2026-09-15 数据层完成**:新增 `tools/rebuild-aircraft-radars.js`(秒级,不跑 Build-Catalog),
      产出 `data/aircraft_radars.json`(423 条雷达候选)与 `data/aircraft_radar_sites.json`
      (560 架带传感器的机型布局)。两者已登记进 `build.rsp` 嵌入,`compile.bat` + `--selftest` 全绿。
- [ ] 生成端:`ApplyAircraftRadarSwap(ref fm, settings)`(1:1 原位换 `blk:t`,不增删 sensor)
- [ ] UI:飞机页雷达选择器(读 `aircraft_radars.json`,按 `!ir && !passive` 过滤)
- [ ] 实测:F-14A(AWG-9,有屏)↔ F-4E(APQ-120,有屏);反例 F-86F(有雷达无屏)换 APG-68

实测前提醒:选中的雷达若不带 `illumination`(目录 `illumination` 字段为空),半主动弹会失效,
UI 应提示;`aircraft_radar_sites.json` 的 `hasRadarPage=false` 机型换完看不到雷达屏(只能体会探测/锁定)。

## 9. 复现命令(数据采集)

数据全在本地解包树,无需重新解包:
```
universal_game_data\aces.vromfs.bin_u\gamedata\flightmodels\*.blk   (1623 个 root 机型)
universal_units_data\aces.vromfs.bin_u\gamedata\sensors\*.blk       (442 个传感 blk)
universal_units_data\aces.vromfs.bin_u\gamedata\sensors\naval\*.blk (122 个,舰载)
universal_units_data\aces.vromfs.bin_u\gamedata\units\**\*.blk      (2643 个,反查地面/舰船引用)
```
重建数据:`node tools\rebuild-aircraft-radars.js` → `compile.bat`(秒级)
