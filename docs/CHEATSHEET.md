# UTL 改动配方速查（CHEATSHEET）

> 目的：常见操作照抄即用，减少依赖 agent 探索。所有路径基于项目根
> `Universal-Test-Lab-0.12.0-beta.2_modified\Universal-Test-Lab-0.12.0-beta.2`。
> 游戏根 `C:\Program Files (x86)\Steam\steamapps\common\War Thunder`。

---

## 0. 铁律（血泪总结）

| # | 规则 |
|---|---|
| 1 | **catalog 数据嵌在 exe 里**（build.rsp `/resource:data\*.json`）——改 data/*.json 必须重编译才生效；运行时只读嵌入资源，无磁盘回退 |
| 2 | **json 与 tsv 双改**（运行时读 json；tsv 是 Build-Catalog 重建源——不同步下次重建会覆盖） |
| 3 | **解包树 ≠ 游戏**：`universal_*_data/aces.vromfs.bin_u/...` 只是 UTL 编辑素材；游戏只读 `content/pkg_user/` + `content/pkg_local/` 覆写层 + aces 包 |
| 4 | **新文件必须发布进 pkg**：新建弹/容器若只放解包树 → 游戏引用不存在文件 → 挂载失效/崩溃（9K79 空射崩溃真因） |
| 5 | **自制弹不要沿用已知弹的 bulletName**：游戏按弹身份走内置模板——指令弹（MCLOS 如 Kh-23M）会锁死无 seeker（无锁定框/直飞）；用全新 bulletName + rocket 段 mesh 即正常 |
| 6 | **native 直选 ≠ 换弹**：UTL 非注入模式选武器只引用原生预设名（donor 行改 blk 无效）——真正换弹走 INJECT 模式（weapon_catalog 全局列表） |
| 7 | **UTL 运行时 UTL.exe 被锁**：编译前必须关 UTL |
| 8 | 大改前先备份：`cp 文件 文件.bak_<标签>`（.bak 不进 git） |

---

## 1. 编译与发布

```
关 UTL → 项目根跑 compile.bat（或 csc @build.rsp）→ 开 UTL
```

- 编译输出：`dist\UniversalTestLab.exe`（57MB，含全部嵌入数据）
- 游戏更新后 pkg 会被清 → 重新发布自建文件

**ExtractGameBlk 解析顺序**：exe 旁 `universal_*_data/aces.vromfs.bin_u/gamedata/<路径>` → 缓存 `%LOCALAPPDATA%\UniversalTestLab\cache\<hash>\aces.vromfs.bin_u\gamedata\...`

**发布目标位置**：
| 内容 | 目标 |
|---|---|
| 地面代理车 | `content\pkg_local\gameData\units\tankModels\userVehicles\` |
| 地面武器 | `content\pkg_local\gameData\Weapons\groundModels_weapons\utl_ground\utl_ground_cannon.blk` |
| 飞机生成 fm | `content\pkg_user\gameData\flightModels\utl_run_*_player.blk` |
| 飞机武器预设 | `content\pkg_user\gameData\flightModels\weaponPresets\` |
| **自建弹/容器（通用）** | `pkg_user` + `pkg_local` 两处都放 `gameData\Weapons\rocketGuns\`（同名双保险） |

---

## 2. 关键数据文件

```
data/aircraft.json            飞机目录
data/aircraft_slots.json      每机可编辑槽（aircraftId/slot/order/tier/maxLoad/anchorMount）
data/donor_weapons.json       每机每槽原生武器清单（NATIVE 模式列表源）
data/weapon_catalog.json      全局武器（INJECT 模式列表源）
data/sensors.json             雷达目录（442 行：id/display/band/role/rangeMax/type/...）
data/ground_ammo.json         地面弹药
data/air_ordnance.json        blk→display 映射（弹显示名）
data/unit_weapons.json / vehicle_weapons.json / presets.json / modifications.json ...
```

**槽锚（anchorMount）**：aircraft_slots 里每槽的锚 = INJECT 时锚定的原生 WeaponPreset 名。
把锚改到目标 preset（如 `x23m`）→ 注入时保留该预设的挂架 emitter + DependentWeaponPreset（吊舱联动）。

---

## 3. 地面 SAM/武器上车

三条路线（按经验选择）：

| 路线 | 适用 | 做法 | 坑 |
|---|---|---|---|
| A. 整炮换 | 新式车/武器本体自洽 | cannon 层整个换目标炮 blk | 源文件无玩家火控的 AI 弹会无弹（走 C） |
| B. 真半主动 | 9A310（AI Buk 发射车） | 自带 9S35（band8+illumination），全 SARH 弹直接上 | 别加搜索雷达（毁锁定） |
| C. inject-shell | S-75 V-759 / 9M38 注入原生发射器 | 弹注入原生发射器（209mm/Osa 轨）而非整炮换 rocket_launcher | 预设里勾 "Inject into native launcher" |

**关键本体名**：
```
车：Osa=ussr_9a33bm3  AI Buk=ussr_ai_sam_launcher_9a310  Buk-M3=ussr_buk_m3_launcher
    菊花-S=ussr_9p157（不可改装——sensors/weapons 同塔系统，覆盖连坐雷达断链——不碰）
    箭10=it_9a35_m / ussr_9a35_m2
弹：5V55=508mm_s300ps_5v55_rocket_loader    V-759=654mm_s_75_v_755_20ds
    9M38=400mm_buk_9m38_rocket_loader       MIM-104=410mm_patriot_mim_104_rocket_loader（真主动）
雷达：9S35=su_9s35（band8·60km·track/illum）  9S18=su_9s18（search·挂 location 天线）
     Viking=su_viking（band6·无照射——打半主动需换 9S35）
```

**雷达替换注意**：换 9S35 时只换天线位（dmPart=antenna_*）；optic 位保留（防双 sensor 抢锁）。
sensor 块格式：`sensor { blk:t="..." turretIndex dmPart:t="antenna_*_dm" ... }`
9S18 搜索站挂法：`turretIndex -1 车体站 + dmPart location + designationFromBody true`，不能直接挂塔。

---

## 4. 飞机挂载改动（本会话验证）

**流程**：UTL → 选机 → 挂载面板开 INJECT → 选 global 武器 → 生成 → 进任务。

- 任务 = usermission，**无挂载菜单**——UTL 直接配好
- 原生预设槽 = 游戏 fm 内 WeaponPreset（含 ShowNodes/Weapon{trigger/blk/emitter}/DependentWeaponPreset）
- **换弹不改槽结构**（inject-shell 精神）：保留挂架 emitter + 吊舱依赖，只换 Weapon.blk 引用
- 发射键：trigger 决定（aam=空空/atgm=空地/rockets/bombs/targetingPod=吊舱）

**MiG-23ML 参考**：Kh-23M = 槽1/5 x23m 预设（apu_68um_kh23m_001 挂架 + DependentWeaponPreset delta_ng 吊舱）。
已还原干净，别乱改（用后 git 恢复）。

---

## 5. 自制自导弹配方（IR/成像——Kh-23 实验成品配方）

```
新文件：universal_weapons_data/.../weapons/rocketguns/<名>_ir.blk
结构 = 某成品制导弹全件（如 us_agm_65g.blk 复制）+ 改：
├─ rocket { bulletName:t = "<全新名>"        ← 铁律 5（勿沿用指令弹名）
│          mesh:t = "外形导弹网格"            ← rocket 段 mesh = 飞行渲染（Kh-23 用 su_kh_23m_missile）
│          guidance { opticalSeeker { targetSignatureType:t="infraRed"
│                                     groundVehiclesAsTarget:b=true
│                                     surfaceAsTarget:b=true
│                                     designationSourceType:t="camera"
│                                     fov/lockAngleMax/rangeMax... }
│                     guidanceAutopilot {...} table0-3 {...} }
│          + 毁伤字段（见 §6） }
顶层 mesh = 挂架静态显示
```

- **制导核心抄成品弹全件**（AGM-65G/R-73）——seeker 激活依赖其参数组合；"参数拼凑"易翻车
- 锁地面目标实测无需吊舱（玩家视角锁定即可）——camera designation 通吃
- 发布：§1 表（解包树 + pkg_user + pkg_local 三处）
- 注册：weapon_catalog.json/.tsv 加行（INJECT 列表）+ air_ordnance 加行（显示名）
- 移除：删三处文件 + catalog 行 + 备份删除（参考 git ad19589）

---

## 6. 毁伤字段速查（弹 rocket 段内）

```
explosiveType:t="tg_40"       装药类型（tg_40=俄系高爆大装药）
explosiveMass:r=75            装药 kg（Kh-23M 原值 75——9K79 只有 1.45=废物）
hitPowerMult:r=400            伤害倍率
explosionEffect / groundCollisionEffect  爆炸特效（bomb_expl_200kg = 大爆）
trendinstDamageRadius / explosionPatchRadius  范围
mass/massEnd                   弹重（Kh-23M 289/225.5）
timeFire/force                 燃时/推力
maxDistance/minDistance        射程
machMax/maxSpeed               速度
```

---

## 7. 一键装配预设（8 个已建，GroundConfigurePanel 一键装配入口）

bk S300 整合 / osa 5V55 S300 6发 / osa v759（注入 209+654mm SACLOS）/ osa buk 9M38（整炮真半 6发）/
9A310 整合（9S18 搜索）/ buk_m3 ai 9S35 9M38 / 箭10 FM-3000 / tor FM-3000

---

## 8. 已知问题与坑位

| 问题 | 状态 |
|---|---|
| PickRadars 崩溃（ModernDialogWindow.ShowDialog NRE/挂死） | ✅ 已修（bfeb1c3：Owner + null 防护 + overlay 拆栈） |
| 菊花-S 改装 | 不可（sensors/weapons 同塔系统）——保持原生 |
| FB-10/特殊武器（R 键）车换弹 | 无效（导弹定义在改装层） |
| usermission 无挂载选择 | 正常——UTL 全配 |
| 预设系统 | 改 settings 后必须 WorkspaceSetSettings() 写回（副本问题） |

---

## 9. 归档与接续

- 每会话结束：追加 `ARCHIVE_2026-09-02.md`（分节：日期/主题/成果/坑）+ commit + push
- 新会话接续：只带 ARCHIVE 尾部 + git log——所有背景在文档里
- git 惯例：commit 信息结尾 `Co-authored-by: Chatbox <chatbox@chatboxai.com>`

---

## 10. 待办速改模板（照抄即用）

### A. TARGETS 地图选择 Picker 化（MapPanel——**半成品待完成，当前会 NRE**）

现状：ModernShell.cs MapPanel 构造（~4256 行）里 mapStack 已是按钮 `mapPickerButton`（**无 Click handler**），但
字段 `mapBox`（~4127 声明）**从未赋值**——构造尾部 `mapBox.SelectionChanged += ...`（~4372）和
`RefreshCombinedSpawns()` 里 `mapBox.SelectedItem`（~4400）引用它 → **进 TARGETS 视图构造即 NullReferenceException**。

完成步骤：

1) 类字段加当前地图：
```csharp
private CombinedMap currentMap;   // 替换 mapBox 的角色（mapBox 字段可删）
```
2) 构造里 mapPickerButton 接 Click（样式用已有 toggleStyle，别用 FindResource——不在树里会抛）：
```csharp
mapPickerButton.Style = toggleStyle;
mapPickerButton.Click += delegate { PickCombinedMap(); };
```
3) 类内新方法（照抄 GroundConfigurePanel.PickRadars 的 Picker 模式）：
```csharp
private void PickCombinedMap()
{
    List<ModernPickerItem> items = new List<ModernPickerItem>();
    foreach (CombinedMap m in allCombinedMaps)
        items.Add(new ModernPickerItem { Display = m.Display, Detail = m.Level, Tag = m });
    ModernPickerDialog dlg = new ModernPickerDialog(ModernText.L("SELECT MAP", "选择地图"), items,
        ModernText.L("SELECT MAP", "选择地图")) { Owner = System.Windows.Window.GetWindow(this) };
    if (dlg.ShowDialog() == true && dlg.Selected != null)
    {
        currentMap = (CombinedMap)dlg.Selected.Tag;
        TextBlock label = mapPickerButton.Content as TextBlock;
        if (label != null) label.Text = currentMap.Display;
        RefreshCombinedSpawns();
    }
}
```
4) 引用替换：
- 删构造里 `mapBox.SelectionChanged += delegate { RefreshCombinedSpawns(); };`（~4372）
- `RefreshCombinedSpawns()` 里 `CombinedMap map = mapBox.SelectedItem as CombinedMap;` → `CombinedMap map = currentMap;`
5) 构造里初始化 `currentMap = state.CurrentMap ?? allCombinedMaps.FirstOrDefault();` 并同步按钮文本。
6) 检查 `MapPanelState.CurrentMap` 已有（保存/恢复用）。完成后：删 `mapBox` 字段声明。

### B. EXPERIMENTAL 一键归零按钮（GroundConfigurePanel，~5253 类）

在 VEHICLE TUNING 页 resetAll 按钮旁加"RESET ALL MODS（清空全部爆改）"：

```csharp
Button resetMods = new Button { Content = ModernText.L("RESET ALL MODS", "清空全部爆改"),
    Style = buttonStyle, Padding = new Thickness(14, 2, 14, 2), Margin = new Thickness(10, 10, 0, 4) };
resetMods.Click += delegate
{
    // 清爆改字段（写 currentSettings——生成时读它；original 是构造副本可同清）
    if (currentSettings != null)
    {
        currentSettings.InjectedCannonBlk = null; currentSettings.InjectedCannonDomain = null;
        currentSettings.InjectedCannonUnit = null; currentSettings.InjectedCannonRound = null;
        currentSettings.InjectedCannonRounds = 0;  currentSettings.InjectNativeLauncher = false;
        currentSettings.UnlimitedAmmo = false;     currentSettings.FakeArhConversion = false;
        currentSettings.RadarSearchBlk = null;     currentSettings.RadarTrackBlk = null;
        currentSettings.RadarStripAiSensors = false;
        currentSettings.OverrideGroundBallistics = false;
        currentSettings.ProjectileMassMultiplier = 1; currentSettings.MuzzleVelocityMultiplier = 1;
        currentSettings.ExplosiveMassMultiplier = 1;  currentSettings.PenetrationMultiplier = 1;
        currentSettings.ReloadSeconds = 0; currentSettings.RecoilMultiplier = 1;
        currentSettings.EnginePowerMultiplier = 1;  currentSettings.VehicleMassMultiplier = 1;
        currentSettings.ForwardSpeedMultiplier = 1; currentSettings.ReverseSpeedMultiplier = 1;
        // 换炮/弹药槽回原生
        currentSettings.InjectedCannonBlk = null;
        currentSettings.GroundAmmoLoadouts.Clear();
    }
    // UI 同步（照抄现有重置的调用链）
    ResetAllValues();
    overrideBallistics.IsChecked = false; ammoUnlimitedBox.IsChecked = false; fakeArhBox.IsChecked = false;
    radarSearchSel = null; radarTrackSel = null; stripAiBox.IsChecked = false; UpdateRadarStatus();
    roundsBox.Text = "0"; injectBox.IsChecked = false;
    SelectInitialCannon(); RefreshAmmo(); RefreshSlotEditors();
};
```
注意：真正的"会话默认干净"（启动不自动套上次爆改）是 M4 完整版——改 `LoadAircraftSettings`/启动路径，
暂缓（涉及 AircraftSettings 持久化语义，别急着动）。

### C. 已知：当前代码 TARGETS 视图会崩（mapBox null）
进 UTL 的 TARGETS 前先完成上面 A（或临时把 `mapBox.SelectionChanged +=` 那行删掉/注释掉）。

