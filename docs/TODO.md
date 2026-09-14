# UTL 待办事项(截至 2026-09-14)

> 背景与历史见 `docs/ARCHIVE_2026-09-14.md`(本轮)与更早归档。
> 每项格式:目标 → 现状 → 涉及文件 → 下一步。

---

## A. 弹药 / 换炮(近期主线)

### A1. 4 种弹上限的"提醒"而非静默截断
- 目标:弹药界面显示「弹药种类 X/4」;加第 5 种时红字阻止;生成时若有历史超限配置,状态栏提示"已忽略 N 种"
- 现状:引擎任务层 `bullets0..3` 硬上限 4 种;生成端尾部截断,用户不知情
- 文件:`ModernShell.MainWindow.Ground.cs`(主页弹药区)、`ModernShell.Ground.cs`(工作台)、`UniversalTestLab.MainForm.Generate.cs`
- 下一步:UI 计数 + 阻止写入;生成端返回"被忽略数量"给状态栏

### A2. `(fixed ammo)` 标注
- 目标:换炮页里 donor 武器没有可编辑弹种时,下拉项标注 `(fixed ammo)`,避免用户以为漏了
- 文件:`ModernShell.Ground.cs`(AddSwapRow / RefreshCannonBox)
- 判据:该武器 blk 在 `vehicle_weapons.json` 里没有 beltOptions/容器

### A3. 工作台覆盖原生弹药(用户实测:主窗口填 12 → 被改回 1)
- 根因:EXPERIMENTAL 面板打开时把原生配置读进来,槽匹配不到"当前炮的弹"就重置为 1;
  点 APPLY CONFIG 即写回,覆盖主窗口的配置
- 方案 A(推荐):工作台在「炮选择器 = 原生炮」模式下**不写回 `GroundAmmoLoadouts`**(保留主窗口配置)
- 文件:`ModernShell.Ground.cs`(`Save()` 两套面板:GroundConfigurePanel / ModernGroundConfigureWindow)
- 备注:这是"两处编辑器共用一份配置"的隐患,和 UI 重构的"单一入口"原则一并解决

### A4. BMPT 类多炮车的弹药槽分配验证
- BMPT 有 8 门武器但任务只有 4 个弹槽;需要实测"引擎按容器归属分配"的行为
- 下一步:按 30mm(2 组,游戏原生也如此)+ ATGM 配置,生成后逐门武器确认弹药

### A5. 换炮:多 donor 车混搭(暂缓)
- 现状:额外替换列表的 donor 武器来自**当前选中的同一辆来源载具**
- 若要做"gunner1 用 A 车的、gunner4 用 B 车的":需要每行独立 domain/unit + 多份预设重定向

---

## B. 飞机相关

### B1. 飞机换雷达(可行性已确认:高,建议实施)
- **数据结构**(实测 F-14A):
  ```
  flightmodels/<机>.blk
    sensors {
      sensor { blk:t = "gameData/sensors/us_an_awg_9.blk" dmPart:t = "radar_dm" }   ← 雷达
      sensor { blk:t = "gameData/sensors/us_an_alr_45v.blk" }                         ← RWR
    }
  ```
- 与地面换雷达(`ApplyRadarSwapToProxy`)结构同类,但**飞机是单一 sensor 兼搜索/跟踪**;
  地面是 search + track 两个 → 飞机版只需"换一个雷达 + 保留 RWR"
- 实现要点:
  1. 采集飞机雷达目录:遍历 `flightmodels/*.blk` 的 `sensors` 块,收集 sensor blk + 机型名(生成 `data/aircraft_radars.json`;可直接扩 `Build-Catalog.ps1` 的 AIR 段)
  2. UI:飞机页(OPTIONS 或 GARAGE)加"雷达替换"选择器
  3. Generate:飞机代理(flightmodel 副本)重建 `sensors` 块 → 写入所选 sensor blk,保留 RWR/其它 sensor
- **风险(必须实测)**:
  - 跨机型雷达的**操作界面/模式**是否可用(sensor blk 自带 UI 定义;F-14 的 AWG-9 装到 F-16 上界面可能异常)
  - 给**无雷达**机型加雷达(HUD/UI 元素可能缺失)
  - 部分机型的雷达配套字段(若有 range/TWS 等)需一并处理

### B2. 投弹视角(bomber_view)—— 已确认是引擎限制,暂不做
- 结论:`bomber_view` 需要「引擎客户端玩家机」路径(`player:b=true` + 官方机型类名 + 账号已解锁);
  自建类/未解锁机型拿不到。官方 PvE 有是因为它走客户端装配
- 详见 `docs/ARCHIVE_2026-09-10.md`

---

## C. UI 重构(清单见 `docs/UI_REFACTOR_PLAN_2026-09-11.md`)

优先级从高到低:
1. **设计令牌统一**(颜色/间距/字号集中,当前两套硬编码)
2. **生成流程进度 + 防冻结**(线程化)
3. **最近使用 / 收藏 / 随机**
4. **整套预设 保存/加载/导出/导入**
5. 窗口状态记忆(大小/位置/Tab)
6. Tab 内分组卡片重排 + 「当前配置」常驻汇总(已有雏形:爆改汇总)
7. 车辆选择器:搜索/过滤/**预览图**/键盘操作
8. 长列表虚拟化(炮/弹目录数千条)
9. `ModernShell.Ground.cs`(1900+ 行)拆分
10. 细节:右键菜单、快捷键、暗色-亮色切换、文案规范、旧 WinForms 界面清理

---

## D. 工具 / 环境(备忘)

- 编译 `cmd /c compile.bat`(**必须先关 UTL**,否则 exe 占用失败);自测 `dist\UniversalTestLab.exe --selftest`
- 只重建武器表:`node tools\rebuild-unit-weapons.js` → `compile.bat`(秒级)
- 全量重建目录:`Update-Data.ps1 -SkipExtract`(**约 40 分钟,慎用**)
- 计时日志:`%LOCALAPPDATA%\UniversalTestLab\extract_timing.log`
- 配置:`%LOCALAPPDATA%\UniversalTestLab\config.json`(缩进 JSON)
- **CRLF/正则坑**:改 `.cs`/`.ps1` 用 Node 脚本;JS 字符串里写正则要 `\\s`/`\\t`(写 `\s` 会被吃掉 → 曾导致 Build-Catalog GROUND 段全空)
- 换炮设置现在是 per-vehicle 持久化(`aircraft_settings[car].inject_cannon_*` + `cannon_swaps`)
