# UTL UI 改造清单(2026-09-11)

> 目标:解决"界面混乱"与操作体感问题,参考 Ask3lad Test Drive GUI 2.5x(PyQt6)的组织方式。
> 本文档按**批次**推进,每项含:问题 → 方案 → 涉及文件 → 风险 → 工作量(S/M/L)。

---

## 0. 现状摸底

**技术栈**:WPF。主窗口 XAML 是**内嵌 C# 字符串常量**(`ModernXaml.Main`,`ModernShell.Xaml.cs`),
`ModernXaml.Parse` → `XamlReader.Parse` → `Find<T>("控件名")` 取元素;各 Tab 内容由 C# 代码构建。

**文件规模(当前)**:

| 文件 | 行数 | 职责 |
|---|---|---|
| `ModernShell.Ground.cs` | 1902 | EXPERIMENTAL 地面配置面板(炮/弹/挂载/数值) |
| `ModernShell.Options.cs` | 1375 | 任务选项面板(飞行/地面两套实例) |
| `ModernShell.Maps.cs` | 1320 | 地图/组合模式窗口 |
| `ModernShell.MainWindow.Ground.cs` | 997 | 主页弹药区 |
| `ModernShell.MainWindow.Events.cs` | 838 | Tab 构建(BuildOptionsTab / BuildTargetsTab / BuildExperimentalTab / BuildGarageTab) |
| `ModernShell.cs` / `ModernShell.Xaml.cs` | 782 / 208 | 主窗口逻辑 + XAML 字符串/样式 |
| `ModernShell.MainWindow.Interactions.cs` | 776 | 交互入口(选车/打开窗口) |
| `ModernShell.Support.cs` | 476 | 辅助 |
| `ModernShell.Ui.cs` | 384 | 对话框/工具 |
| `ModernShell.MainWindow.SelfTest.cs` | 209 | UI 自测 |

**已知问题(来自使用反馈 + 代码观察)**

1. 5 个 Tab(VEHICLE / TARGETS / OPTIONS / GARAGE / EXPERIMENTAL)职责边界模糊,
   同一设置存在**多个入口**(主页弹药区 vs EXPERIMENTAL 工作台;两套 Options 面板实例)。
2. 样式硬编码散落:C# 里到处 `new Border { CornerRadius = 15, Background = ModernPalette.Brush("#A024324D") }`,
   XAML 里又有一份样式表(`GlassCard`/`ButtonStyle`/`Caption`)→ 两套视觉语言。
3. 面板重建频繁:切 Tab / 换车即重建控件树;长列表(炮/弹目录数千条)未虚拟化。
4. 缺少常见便利功能:收藏、随机、整套预设导入导出、窗口状态记忆、右键菜单、快捷键。
5. 生成流程同步执行(UI 冻结风险);状态反馈只有一行文字。
6. 旧 WinForms 主界面(`UniversalTestLab.MainForm.*`)与新 WPF 界面长期并存,入口不统一。

---

## 1. 批次一 —— 体感提升(低风险,先做)

### 1.1 设计令牌(token)统一
- **问题**:颜色/圆角/间距/字号在 XAML 与 C# 两处硬编码,改一处漏一处。
- **方案**:在 `ModernXaml.Main` 资源里定义完整令牌(颜色组、圆角、间距、字号、控件高度);
  新增 `ModernPalette`/`ModernMetrics` 静态类从资源读取;C# 里禁止再写死颜色字面量。
- **文件**:`ModernShell.Xaml.cs`、`ModernShell.cs`(ModernPalette)、各面板文件(替换硬编码)。
- **风险**:低(纯样式);视觉回归需逐面板过一遍。
- **工作量**:M

### 1.2 生成流程:进度 + 防冻结
- **问题**:生成在 UI 线程同步跑;冷启动解包时可能冻结数秒。
- **方案**:生成逻辑抽成可测试的纯函数(`BuildMission(...)`),UI 用 `Task.Run` + `Dispatcher` 回传进度;
  生成期间禁用按钮、显示进度条与阶段文字(读取资源 → 构建代理 → 写任务)。
- **文件**:`MainForm.Actions.cs`、`ModernShell.MainWindow.Interactions.cs`、`ModernShell.Support.cs`。
- **风险**:中(需把 MessageBox/控件访问 marshal 回 UI 线程;已有 `Dispatcher.BeginInvoke` 先例)。
- **工作量**:M

### 1.3 状态栏统一
- **问题**:状态提示分散(`SetStatus` / 各处 status.Text),位置与生命周期不一致。
- **方案**:主窗口底部固定状态区:左侧状态文字、右侧进度/耗时;所有提示走统一 API。
- **文件**:`ModernShell.Xaml.cs`、`ModernShell.MainWindow.Events.cs`。
- **风险**:低
- **工作量**:S

### 1.4 最近使用 / 收藏 / 随机
- **问题**:每次都要翻列表找车;Ask3lad 有 recently_used / favourites / 随机按钮。
- **方案**:
  - 最近使用(地面/舰船/飞机分别记录,已有 `session` 前半部分)→ 顶部快捷行
  - 收藏(星标)+ 只看收藏过滤
  - 随机:随机车辆 / 随机目标 / 随机天气时间(一键"随便来一局")
- **文件**:`ModernShell.MainWindow.Events.cs`、`ModernShell.MainWindow.Ground.cs`、`UniversalTestLab.Storage.cs`(持久化)。
- **风险**:低
- **工作量**:M

### 1.5 整套预设:保存 / 加载 / 重命名 / 删除 / 导出 / 导入
- **问题**:现在只有弹药预设与 era 预设;整套配置(车辆+挂载+弹药+目标+选项)无法一键复用/分享。
- **方案**:统一预设模型(JSON,可读缩进)+ 预设管理面板;导出为单个 `.json` 文件,导入时校验版本。
- **文件**:`UniversalTestLab.Storage.cs`(模型/IO)、新建预设面板、`ModernShell.MainWindow.Events.cs`。
- **风险**:低(与现有 ammo/era preset 并存,逐步统一)。
- **工作量**:M

### 1.6 窗口状态记忆
- **问题**:窗口大小/位置/Tab/滚动位置不记忆。
- **方案**:关闭时写入 config;启动恢复(含"上次选中的车"已有,补齐窗口几何与 Tab)。
- **文件**:`ModernShell.cs`、`UniversalTestLab.Storage.cs`。
- **风险**:低
- **工作量**:S

---

## 2. 批次二 —— 结构重做(中等风险,体验核心)

### 2.1 Tab 职责与分组卡片重排
- **问题**:Tab 内控件堆叠无层次;同一设置多点入口。
- **方案**:参考 Ask3lad 的 QGroupBox 组织——每个 Tab 内用**带标题的分组卡片**分区
  (如 VEHICLE:选择 / 挂载 / 摘要;OPTIONS:出生 / 弹药 / 快速修复);
  明确"单一入口"原则:每个设置只在**一个**位置可改,其余位置只读展示 + 跳转。
- **文件**:`ModernShell.MainWindow.Events.cs`(4 个 Build*Tab)、`ModernShell.Xaml.cs`。
- **风险**:中(信息架构调整,需与用户确认每个 Tab 的最终分区)。
- **工作量**:L

### 2.2 "当前配置"汇总面板
- **问题**:生成前无法一眼复核将要生效的爆改(换炮/弹药/目标/选项)。
- **方案**:常驻侧栏(Ask3lad 的 CURRENT MODS 思路):列出所有非默认设置 + 一键"全部重置"。
- **文件**:`ModernShell.MainWindow.Events.cs`、`ModernShell.Support.cs`。
- **风险**:低
- **工作量**:M

### 2.3 车辆选择器升级(搜索 / 过滤 / 预览图 / 键盘)
- **问题**:列表长、无过滤、无预览;找不到车。
- **方案**:
  - 搜索(已有,补齐拼音/别名?)、按国别/类型/等级过滤
  - **预览图**(从游戏资源或缓存抓取;Ask3lad 用 Vehicle_Previews 图集)
  - 键盘:上下选择 + Enter 确认 + Esc 取消;双击直接选用
  - 显示"最近/收藏"分组置顶
- **文件**:`ModernShell.MainWindow.Events.cs`、新建 `VehiclePickerDialog`、`ModernShell.MainForm.cs`(数据来源)。
- **风险**:中(预览图来源需要确认:游戏内 .avif / 现成图集 / 抓取缓存)。
- **工作量**:M~L

### 2.4 长列表虚拟化与增量加载
- **问题**:炮/弹目录数千条,全量生成控件导致卡顿。
- **方案**:列表统一用 `VirtualizingStackPanel` + `IsVirtualizing=True`;
  目录数据按需分页/过滤后再绑定;避免每次选择重建整个面板(改为局部刷新)。
- **文件**:`ModernShell.Ground.cs`(炮选择器/弹列表)、`ModernShell.MainWindow.Ground.cs`。
- **风险**:中(需确认现有 ItemsSource 绑定方式)。
- **工作量**:M

### 2.5 地面配置面板拆分
- **问题**:`ModernShell.Ground.cs` 1902 行,炮/弹药/槽位/数值/雷达混在一起。
- **方案**:按功能区拆文件(炮与弹药、槽位与预设、数值调参、雷达/传感器),纯搬迁不改行为。
- **风险**:低(机械拆分,编译即验证)。
- **工作量**:M

---

## 3. 批次三 —— 细节与清理(逐步)

| # | 项目 | 说明 | 风险 | 量 |
|---|---|---|---|---|
| 3.1 | 右键菜单 | 车辆列表:收藏 / 复制 ID / 打开资源目录 / 随机此国 | 低 | S |
| 3.2 | 快捷键 | Ctrl+1..5 切 Tab、Ctrl+G 生成、Ctrl+F 搜索、F5 刷新 | 低 | S |
| 3.3 | 暗色/亮色切换 | 现状仅暗色;令牌化后加亮色主题 + 记忆 | 低 | M |
| 3.4 | 文案规范 | 英文全大写标签 vs 中文用词统一;术语表(挂载/弹种/默认弹) | 低 | S |
| 3.5 | 错误提示统一 | 对话框统一"可复制的详细错误"+ 日志文件链接 | 低 | S |
| 3.6 | 生成产物校验提示 | 生成后展示"本次写入的文件清单"(便于排查) | 低 | S |
| 3.7 | 计时日志收窄 | 只在慢路径(UNPACK)或总耗时异常时记录 | 低 | S |
| 3.8 | 旧 WinForms 界面清理 | `UniversalTestLab.MainForm.*` 与 WPF 界面并存;确认保留/迁移/删除 | 中 | L |
| 3.9 | UI 自测扩展 | `ModernShell.MainWindow.SelfTest.cs` 增加各 Tab 的 smoke 检查(控件存在、绑定非空) | 低 | M |
| 3.10 | 无障碍 | 焦点顺序、Tab 导航、字号缩放 | 低 | S |

---

## 4. 建议推进顺序

1. **批次一**(1.1 → 1.3 → 1.6 → 1.4 → 1.5 → 1.2):先统一视觉与反馈,再加便利功能,最后做线程化。
2. **批次二**(2.2 → 2.1 → 2.4 → 2.3 → 2.5):先加汇总面板(改动小、收益直观),再动 Tab 结构。
3. **批次三**:随手做,与功能开发并行。

**每批次结束**:编译 + `--selftest` + 用户过一遍界面(截图对照)。

---

## 5. 需要用户确认的点

1. **2.1 Tab 分区方案**:每个 Tab 内希望怎么分组?(可先由我提一版 XAML 草图)
2. **2.3 预览图来源**:用游戏资源抓取(需要时间与体积),还是先做"文字信息卡"(型号/国别/等级/主炮)?
3. **3.8 旧 WinForms 界面**:还有哪些功能只能在旧界面用?(决定是迁移还是删除)
4. 是否需要**亮色主题**,还是继续纯暗色。
