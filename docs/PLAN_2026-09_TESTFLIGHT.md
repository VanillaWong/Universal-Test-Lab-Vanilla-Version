# PLAN — UTL 试驾模式（Test Flight Mode）2026-09-05

> 状态：调研完成、方案待用户拍板。本文档 = 自包含设计，压缩对话后照此开工。
> 背景源头：mis.vromfs 解包产物在 `C:\Users\honam\AppData\Local\Temp\wt_mis\mis.vromfs.bin_u\`（官方任务）
> 与 `...\Temp\wt_game\`（game.vromfs 全解）、`...\Temp\wt_gamecommon_unit\`（unit 脚本）、`...\Temp\wt_gui\`。

## 0. 用户拍板过的目标

把**官方试驾**做成 UTL **第二任务模式**，作为 Clean Test Range（现有 `template/universal_test_lab.blk`，level=`levels/Clean_Testdrive.bin`）的补充：
- 保留 UTL 核心价值：**玩家车与靶车全自定义**（车/弹/改装/挂载照旧由 UTL 配置注入）——官方是固定玩家车+固定靶，我们只借壳
- 吸收官方试驾体验：出生点矩阵 / 天气与时间 / 标靶等级筛选
- 涉及文件夹（用户点名看过）：`gamedata/missions/training/testflight/tank/`（坦克试驾）
  与 `.../testflight/aircraft/`（飞机试驾）

## 1. 官方实现事实（挖完的结论，勿再重复考古）

### 1.1 坦克试驾（tank/）
- 入口文件 `testflight_era_tft.blk`（23KB，era=爆反时代靶场）：玩家 `t1_player01`=ussr_zsu_23_4（army1）
  + 7 辆苏系靶车（IT-1/T-62/T-55A/T-64A/T-10M/T-90A/M901，army2，每辆带
  `modification:t="tank_tool_kit"` + `modification:t="manual_extinguisher"` —— **AI 靶车自修自灭**，
  玩家侧带同款行无效（消耗品库存机制只对 bot 生效））+ 移动靶（mig-15bis/il_28×2/山猫直升机）
  + 两个圈：`rearm_circle`（补弹）、`atgm_circle`（ATGM 课目区，带 checkpoint_pillar 界标，
  玩家车开进去被 M901 打 = 测爆反/装甲）
- level=`levels/firing_range.bin`（玩家游戏自带）
- `imports{import_record{file:"gameData/missions/..."}}` 引用同目录共享模板
  `testflight_template.blk`（32KB，13 组触发器：ATGM 课目开关/玩家被 M901 打/靶重生/航路…）
- 玩家车不带 modification（官方玩家车不被烧的前提；灭火在 usermission 无解，见 §5）

### 1.2 飞机试驾（aircraft/）
- 6 张地图壳 `test_flight_universal_{afghan,denmark,equatorial_island,israel,ladoga,tunisia}.blk`
  （各 31-43KB），level=`levels/air_afghan.bin` 等**空战正式图**；import 三个共享模板：
  `test_flight_template.blk`（出生/重生/课目逻辑）+ `test_flight_unit_template.blk`（靶单位）
  + `test_flight_bases_destroy_part_template.blk`（炸基地课目，仅 5KB：bomb_areas_mid_init 判定
  炸弹落 `mid_bases_area`）
- 玩家 wing=`armada_01`（不是 "You"！）；`mission_settings.player{army1, wing}`

### 1.3 出生矩阵（飞机，实现在 test_flight_template.blk）
- 机制 = **车库 UI 用 `fromDescriptor` 塞变量 → 任务触发器按变量分支**：
  | 变量 | 含义 | 任务内动作 |
  |---|---|---|
  | `is_airfield_spawn:b` | 机场跑道 | `spawnOnAirfield{runwayName:t=@airfield_spawn}` |
  | `is_ship_spawn:b` | 航母 | 航母=“船型机场”：unit_template 里 `ships{name:t="us_aircraftcarrier_forrestal_destroyer" unit_class:t="us_destroyer_fletcher"...}`（航母位=destroyer 载体+甲板跑道 `airfield_ship_unit`/`airfield01ship`），同样 spawnOnAirfield 落甲板 |
  | `is_water_spawn:b` | 水上(水机) | 先 `unitCheckTag{target=@player tags{air:b=yes type_hydroplane:b=yes}}` 校验水机 → `water_spawn_point` 水面区出生 |
  | `air_spawn_point:i` | 空中高度 | 与 `spawn_area0{n}`（n 对应档位）拼名；afghan 图预置 spawn_area01-05，y=2532/3532/4532/6532/8532（差 1000m，即 1/2/3/5/7km 档）+ `airfield_spawnpoint_high`(y≈1555 机场上空低空) |
  | `target_rank:i` | 标靶等级 | `varAddString` 拼 `target_tanks_squad{rank}` → 按等级换靶编队 |
- 死后 `check_spawn_reload` 按原选择重生；`spawn_move_area`/`spawn_idle_area`=靶机移动/盘旋区
- **防空区出生：未定位到明确实现**（afghan 无独立 AA 区块；防空车疑为靶编队的一部分）——若用户坚持要此项需再查 unit_template/其它图

### 1.4 UTL 翻译结论
官方做“运行时分支”是为了一张任务服务所有选择；UTL 生成端**每次生成时直接把选项烧进任务**（写死所选跑道/高度坐标/航母块），不需要变量分支。`target_rank` 同理：UTL 自选靶，等级仅作筛选 UI。

## 2. UTL 实现草案（待用户拍板细节）

### 架构
- **新模板**：从官方复制出一份“试驾母版”（坦克版基于 firing_range + era 布局；飞机版基于某张 air_ 图 + universal 布局），存为 `template/universal_test_lab_tft_ground.blk` 等 → 嵌入 exe（build.rsp 加 /resource）→ Embedded.Text 读取
- **生成路径**：用户选“模式=Clean Range / Tank TFT / Air TFT”→ 读对应模板 → 玩家单位注入
  （现 ConfigureGroundPlayer/UpdateUnit 硬编码块名 "You"，需参数化块名：官方玩家块 = `t1_player01`(tank)/`armada_01`(air)，或干脆改名后复用）
- **出生**：UI 选出生方式 → 生成端把玩家块 tm/或写入固定 spawnOnAirfield/高度坐标（沿用现有 spawn_mode/spawn_speed 相关代码扩展）
- **天气/时间**：改模板 `mission{environment / weather}` 两字段（小工）
- **靶**：官方靶编队保留为“默认”，但 UTL 现有靶注入逻辑（UpdateUnit 按名字换 Target_Air_01/CTR_* 等）应对齐新模板的靶块名——**自定义靶的块名映射表要做**
- 地图文件都在玩家游戏内（firing_range.bin / air_*.bin）→ usermission 引用直接可用，无需打包

### 必须先做的手工验证（风险项）
1. **import 在 usermission 是否可用**：官方壳靠 `imports` 拉共享模板。UTL 模板若是“合并后单文件”（推荐，无 import 依赖）则绕过；若想借 import（文件更小）须实测 usermission 是否允许 import 官方路径
2. 官方模板触发器对玩家块名的引用（t1_player01/armada_01/@player 变量）替换成 UTL 玩家名后是否全部自洽
3. ATGM 课目、炸基地判定在 usermission 里是否照常工作（官方这些是 training 模式任务，usermission 单机 gamemode 差异待测）

### 建议落地批次
- P0 手工验证（§2 风险项；可直接把官方 era 组合文件装进 UserMissions 试跑，玩家车先固定）
- P1 坦克试驾模板进 UTL（地图选择 UI + 玩家车/靶自定义复用）
- P2 飞机试驾模板 + 出生矩阵（高度档/机场）
- P3 天气/时间选择 + 靶等级筛选 UI（target_rank 仅筛选，不改变自选靶逻辑）
- P4 航母/水上（依赖 P2 出生机制；航母图 air_equatorial_island 等才有航母——需确认哪张图带航母）

## 3. 相关代码现状速查（2026-09-05 拆分后）

- 玩家地面车生成：`UniversalTestLab.BlkTools.ConfigureGroundPlayer`（写死 name "You" + tm `6.35,41.5,-622.332` + 注入了 tank_tool_kit/manual_extinguisher 两行——AI 侧才有用，留着无害）
- 任务模板：`template/universal_test_lab.blk`（5044 行；level=Clean_Testdrive.bin；玩家出生在射击区；模板自带可占圈 `ctr_cz_circle` at (900,30,700) + area `ctr_capture_zone` 半径80；机场区 airfield_area 在 (551,30,550)；本模板机场=机场跑道区）
- 模板加载：`MainForm.Actions.cs` `Embedded.Text("UTL.universal_test_lab.blk")` → 一串 BlkTools 变换 → WriteBytes 到 `UserMissions\Universal Test Lab\universal_test_lab_hot.blk`
- 触发器注入范例：BlkTools 里 “UTL APS Carrier Recovery Compatible”/“UTL Rapid Fire” 块 = template 预置 + ConfigureXxx 改 is_enabled
- units 顶层：`units{...}`；areas 顶层：`areas{...}`；triggers 顶层 `triggers{`（模板第 42 行起）

## 4. git 状态（本会话尾）

已 push：a4cb76d 及更早。**未 push（4 个）**：991c5b0（EXPERIMENTAL 两栏）、619ead6（玩家车 modification 两行）、cc8473b（restore-zone+kits，随后被 5078246 撤销）、5078246（撤销 zone/kits）。工作区干净。
注意：CC8473B 已把 zone/kits 注入带进历史但 5078246 已删——最终产物无 zone/kits。

## 5. 挂起问题（别丢）

1. **灭火/修车 saga 结论**：usermission 玩家自修不可行（SUITABLE 置真在引擎层；消耗品库存仅对 bot）；
   玩家侧工具包/灭火器行无效；capture_zone_circle army1 能修 DM 部件（灭火/补员不行）但体验一般已撤；
   唯一可行灭火 = fullRestore（全修 trade-off，用户实测认可“full 可以”）；用户最终决定**挂起灭火**，
   未做 fire-suppression 触发器（用户拒绝了插入）。Rapid Fire **PARTIAL** 模式 = 用户想要的
   “关键件修复、爆反不补”，已有（间隔可调）。修车灭火要恢复讨论时从这行开始。
2. 防空区出生实现未定位（见 §1.3 末）
3. 航空母舰存在于哪些 air_ 图未确认（P4 前置）
4. 旧的待办仍有效：Update-Data 补 tsv2json 已做（本轮提交过？——是，随 push 或在未 push commit 里——见 §4 历史确认）；副油箱；老式挂载隔离；EXPERIMENTAL 布局重构已完成（991c5b0）待游戏验证

## 6. 用户提醒过的界面特性（做 UI 时照抄）

官方试驾面板可选：出生点（机场/敌方防空区/滑翔路径/机场上空 1/2/3/5/7km）、标靶载具等级、
天气、时间；水机=水上起飞、舰载机=航母起飞。（防空区/滑翔路径这两项在挖到的代码里含义不明，见 §1.3）
