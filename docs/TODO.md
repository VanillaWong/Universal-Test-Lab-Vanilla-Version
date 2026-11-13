# UTL 待办事项（更新于 2026-11-13）

> 背景与历史见 `docs/ARCHIVE_2026-11-13.md`（本轮，含海军机制与坑）与更早归档。
> 每项格式：目标 → 现状 → 涉及文件 → 下一步。

---

## 0. 当前主线：海军

玩家开船已通（602/663 艘可选）。现在的短板是**内容与弹药**，不是链路。

### N1. 海军靶场（3 / 6 / 9 / 12 / 15 km）
- 目标：按距离铺开舰种（驱逐→轻巡→重巡→战列→航母）+ 2 组靶机
- 现状：任务里只有模板自带的 1 艘大和 + 2 架靶机
- 做法：生成时克隆 N 个 `ships{...}` 兄弟块，名字 `UTL_Range_XX`，`props.army=2`
- 文件：`UniversalTestLab.BlkTools.cs`（`ConfigureShipPlayer` 附近）

### N2. 战争模式（靶船反击）
- 目标：开关打开后靶船主动攻击玩家
- 实现：`unitAttackTarget{ object:"Target_Ship_XX" target:"You" playerAttracted:yes fireRandom:yes }`
- 现状：未做；Ask3lad 用 3–4 条该动作实现

### N3. 舰船弹药面板（按口径选弹）
- 目标：127mm / 40mm / 20mm 各自选弹种，写进 `bullets0..3`
- 现状：`bulletsN:t=""` + 9999（引擎用默认弹，主炮可用）
- 参考写法（Ask3lad）：
  ```
  bullets0:t="127mm_usa_mk34_common_he"
  bullets1:t="40mm_bofors_l60_navy_universal"
  bullets2:t="20mm_oerlikon_navy_he"
  ```
- 数据：可从舰船各 Weapon 的炮文件容器列表生成

### N4. 未确认项
- **鱼雷**：注册预设名修复后是否真的出现（`mk6_bomb_mortar_mk15_torpedoes`）
- **航母 HHQ-10**：4 座火箭发射器能否切换/发射
- **官方海图出生点**：每图找一组稳定坐标，写进默认值

### N5. 深水 / 潜艇
- 现状：`Clean_Testdrive` 抬海面（water 1→100）能潜但视觉差，已回滚
- 已加官方海图（Black Sea port / Guadalcanal / Saipan …）待实测深度
- 真要深水图：CDK 的 `heightmapLand.plugin.blk` 有 `minUnderwaterBottomDepth`，但需 Dagor 重新导出

---

## A. 飞机（实验性功能）

### A1. weightless（挂载无重无阻）
- 做法：每个挂载生成薄包装 `include` 原武器 + `"@override:rocket"{ "@override:dragCx":r = 0 }`（炸弹再加 `mass:r = 1`）
- 现状：未做

### A2. 武器调校（wtune）
- 做法：薄包装 + 顶层 `"@override:字段":类型 = 值`（射速/后坐/弹重/装药）
- 现状：未做

### A3. 飞机换机炮
- 现状：地面换炮已有；飞机 `commonWeapons` 仍是原生 preset
- 做法：改 `Weapon.blk` 指向薄包装文件

---

## B. 弹药 / 换炮（旧账）

### B1. 4 种弹上限提醒
- 界面显示「弹药种类 X/4」，第 5 种红字阻止；生成时提示"已忽略 N 种"

### B2. `(fixed ammo)` 标注
- 换炮页 donor 武器没有可编辑弹种时标注

### B3. 工作台覆盖原生弹药
- 方案 A：工作台在「原生炮」模式不写回 `GroundAmmoLoadouts`

---

## C. 架构 / UI

### C1. 用"整份单位拷贝"替代 `@override:Weapon[1]` / `preset[1]`
- 根治下标错位类 bug（防空车 preset 那次），Ask3lad 已采用该架构

### C2. UI 重构三批
- 见 `docs/UI_REFACTOR_PLAN_2026-09-11.md`

---

## D. 环境备忘

- 改代码：`code_execution` 写补丁脚本 → `user_exec node <脚本>`（绕开审批超时）
- 编译：`cmd /c compile.bat`（**UTL 必须关闭**；崩溃会留僵尸进程，需任务管理器）
- 自测：`dist\UniversalTestLab.exe --selftest`
- 数据重建：`Update-Data.ps1` ≈25 分钟，后台跑 + 轮询
- Ask3lad 抓取：`node tools/capture-ask3lad.js`
