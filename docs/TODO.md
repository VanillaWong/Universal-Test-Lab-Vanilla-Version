# UTL 待办事项（更新于 2026-11-14 晚）

> 本日完整记录见 `docs/ARCHIVE_2026-11-14.md`；
> AI 舰导弹技术细节见 `docs/NAVAL_AI_MISSILES_2026-11-14.md`。

---

## 0. 已完成

### ✅ AI 舰导弹可用性（11 条补丁，三艘实测通过）
054A / Halsey / 052D 均可正常发射并命中。核心是 `applyAllMods`、导弹优先占弹药槽、
`finsAoa 0.125→0.35`、借用 SM-6 自动驾驶。**不是 bug，是 AI 资产没做玩家侧调校。**

### ✅ 海军导弹进换挂载
16 条 `Naval Missiles` 目录项，飞机/坦克可挂（`utl-sam:` 机制）。

### ✅ 舰船武器数据
`tools/build-naval-weapons.js` → `data/naval_weapons.json`（663 艘 / 15227 组 / 28 弹）。

### ✅ UI 重构（第一批）
- 右栏改为 `SUMMARY`：标签/值两列 + 滚动条 + 弹种名/数量/总计
- 三步骤按钮 `01 配置 / 02 场景 / 03 应用`
- 内联场景面板：地图、时间天气、出生方式+高度、目标摘要 + `编辑…` 弹窗
- 飞行配置三张卡片（AIRFRAME / RADAR / SHIP LOADOUT）搬进内联页
- `OptionCategory` 分类标签条（空分类自动隐藏）
- 实验开关改为紧凑行（名称 + 开关 + tooltip）

### ✅ 弹药容量上限（bug 修复）
`TrimGroundLoadouts` 曾是死代码 → 新增 `TrimToCapacity()`，读档/写回双向裁剪；
选弹种与应用时也各夹一次。四道防线。

---

## 1. 下一步（按优先级）

### P0 · 场景页内嵌 Map & Scenario（用户指定方案 B）

**目标**：把 `ModernMapWindow` 的整块表单直接放进中间卡片的「02 场景」页，去掉现在的摘要版 + “打开编辑器…”按钮。

**做法（B：内嵌窗口内容）**
1. 给 `ModernMapWindow` 加构造参数 `bool inline = false`
2. `inline == true` 时：
   - 不创建窗口外壳/关闭按钮，只构建表单内容
   - Apply 按钮改为调用回调（把 `Result` 交给调用方），**不调用 Close()**
   - 暴露一个 `internal UIElement BuildInlineBody()` 或 `internal UIElement InlineBody`
3. 场景页持有该窗口实例（不 Show），把 `InlineBody` 插入 `SceneInlineHost`
4. 保留弹窗入口作为备选（同一份表单两个出口）

**已知雷点**
- 对未 Show 的 Window 调用 `Close()` 会抛异常 —— Apply 必须走回调
- 窗口根的 Margin/Padding 是为弹窗设计的，内嵌后要去掉外壳内边距
- 场景页已是 ScrollViewer，内嵌表单自带滚动会导致双层滚动条
- 每次 Apply 后需要 `UpdateConfigurationSummary()` 刷新右栏

### P1 · 03 应用页
- 文件清单：本次写入了哪些文件、每个文件几处改动
- 备份：`%APPDATA%\UniversalTestLab\backups\<时间戳>\` + `manifest.json`
- 历史列表 + 撤销上一次应用
- 参考 Ask3lad 的 `01 配置 / 02 场景 / 03 应用` 第三页

### P2 · 清理占位标签
顶部 `TARGETS / OPTIONS / GARAGE / EXPERIMENTAL` 四个占位页仍是
"Stage1: migrating ..."，应合并进三步或删除。

### P3 · 多场景任务拆分
| 场景 | 文件 | chapter | 默认地图 |
|---|---|---|---|
| 空战 | `utl_scene_air.blk` | `TestDrive_Air` | Clean_Testdrive |
| 陆战 | `utl_scene_ground.blk` | `TestDrive_Ground` | Clean_Testdrive |
| 海战 | `utl_scene_naval.blk` | `TestDrive_Naval` | iwo_jima |
| 潜艇 | `utl_scene_sub.blk` | `TestDrive_Sub` | avn_peleliu |

雷点：`Actions.cs:312` 的清理通配 `universal_test_lab_*.blk` 会互删，必须改白名单。

### P4 · 目标表
每目标一行：载具 / 数量 / 武器 / 重生。对标 Ask3lad 的表格。

### P5 · 海军内容
N1 靶场、N2 战争模式、N3 按口径选弹 UI、N5 深水/潜艇（Peleliu 待实测）

---

## 2. 交互原则（已确立）

1. 三栏：左选载具 / 中设置（三步）/ 右摘要
2. **弹窗只用于"选择"**：挑供体飞机、雷达、深度目标编辑
3. **设置一律内联**：弹药、开关、地图、出生、目标数量
4. 右栏永远显示当前配置，改完立刻刷新

---

## 3. 环境备忘

- 编译：`cmd /c compile.bat`（UTL 必须关闭，否则 CS0016）
- 自测：`dist\UniversalTestLab.exe --selftest`
- 新增 `.cs` 必须加进 `build.rsp`
- 改 C# 用 Node 脚本（`edit_file` 对 CRLF/`\r\r\n` 敏感）
- 目录重建：`Build-Catalog.ps1` + `node tools\tsv2json.js`（≈30 分钟）
- 数据重建：`Update-Data.ps1`（≈25 分钟）
