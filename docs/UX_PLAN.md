# UTL UX 重构计划（2026-09-03 规划归档）

> 对标 Ask3lad（分层标签 + 搜索选择 + 详情信息），解决"载具/武器量大选择难"、
> "EXPERIMENTAL 堆叠杂乱"、"爆改默认生效难还原"三个痛点。
> 状态：规划完成，待按里程碑实施。

---

## 一、界面分级架构（对标 Ask3lad mode_tabs → tab_widget → sub_tab）

```
L1 模式层（新增——顶部分段按钮）
   [陆战] [空战] [直升机] [海战(置灰·规划中)]
   行为：过滤载具列表到该域 + 锁定工作区类型 + 每模式独立记忆选择/目标
   复用：现有 GroundSelected/playerKind/FilterAircraft/GarageKind 逻辑包装，不重构核心

L2 功能页（现 5 视图按钮——基本达标，保留）
   Vehicle / Targets / Options / Garage / Experimental
   模式层激活后：各模式显示自己的 Vehicle（车列表）与 Targets

L3 EXPERIMENTAL 子页化（对标 Ask3lad Experimental sub_tab）
   [弹药] [换炮] [雷达] [弹道/数值] [选项]
   现状 GroundConfigurePanel 一长面板 → 拆子页，每页清爽
```

## 二、选择器统一（ModernPickerDialog 全面铺开 + 详情卡）

已完成基础：ModernPickerDialog（Ask3lad 式：搜索即时过滤 + Esc 清空 + 无匹配提示 + 自动聚焦）
+ sensors catalog（442 雷达——fsm 自动打 role 标签待实施）

| 待替换 | 现状 | 目标 |
|---|---|---|
| EXPERIMENTAL 炮选择 cannonSelector | ComboBox | Picker（搜索） |
| 跨域联动选炮（域/单位/炮/弹 4 下拉） | ComboBox 链 | Picker 链（每级 Picker） |
| 地图/目标/ERA 等长列表 | ComboBox | Picker |
| 弹药选择 | 搜索框+列表（已有） | 保持 + 详情卡 |

## 三、详情卡（像游戏 stat card——选东西时看参数）

| 卡 | 内容 | 数据源 | 状态 |
|---|---|---|---|
| 载具卡 | 型号/国家/类型/级别 + 火力(主炮+弹) + 机动(质量/马力/速度) | ground.tsv + vehicle_weapons.json | 数据大部分有 |
| 武器/弹药卡 | 弹名/类型/质量/初速/装药/穿深/口径 | ground_ammo.json | ✅ 全有 |
| 雷达卡 | 型号/band/角色(搜索·跟踪)/rangeMax | sensors catalog（需扩展） | 扩展中 |

展示：Picker 内右侧预览区 + 主界面选中项旁（可折叠）
组件：ModernDetailCard（三型复用——后续统一）

## 四、资讯数据扩展（先做）

- sensors.tsv 加 role 列（fsm 判定：search fsm=搜索；track/lock/acquisition=跟踪——已研究可行）
- sensors.tsv 加 rangeMax（sensor blk 内 transivers rangeMax）
- 重跑 Build-Catalog → tsv2json → 嵌入

## 五、爆改状态管理（新增需求——2026-09-03）

目标：**打开程序默认干净（原生车），不再自动套用上次爆改；爆改可保存为预设按需加载；支持一键/分别归零**

### 状态模型
- 爆改设置（换炮 cannon/弹、雷达替换、弹道覆盖、伪ARH、无限弹药等）归入"爆改预设"
- **默认：程序启动 = 无爆改（车原生）**——上次会话的爆改不自动生效
- 用户从"爆改预设库"加载某预设 → 应用（生成时生效）

### UI（EXPERIMENTAL 面板顶部工具条）
```
[载入预设 ▾] [保存当前为预设…] [──归零──] [全部归零] [分别归零 ▾]
分别归零 = 每子页内独立"还原原生"按钮（换炮还原/雷达还原/弹道还原/伪ARH 关…）
```

### 行为
- 一键归零：清空所有爆改字段（InjectedCannonBlk/Radar*/弹道乘数/开关）→ 车原生
- 分别归零：只清单类（如雷达还原——保留换炮）
- 保存预设：命名存（类似弹药预设 ammo_loadouts 机制——新建爆改预设库 mod_loadouts 或并入 Garage 预设）
- 打开默认干净：改持久化加载逻辑（MissionSettings/AircraftSettings 的爆改字段启动时不自动 Load——或加载到"预设库"而不自动应用）

## 六、实施里程碑（建议顺序）

| M | 内容 | 依赖 |
|---|---|---|
| M1 | 资讯数据扩展：sensors role/range + 重跑 catalog | 无 |
| M2 | 详情卡组件 + Picker 预览 | M1 |
| M3 | EXPERIMENTAL 子页化 + 炮/雷达选择 Picker 化 | M2 |
| M4 | 爆改状态管理：默认干净 + 归零 + 预设保存/加载 | M3 |
| M5 | 模式层（陆战/空战/直升机/海战灰） | M3 |
| M6 | 长列表全面 Picker 化 + 载具卡 | M2 |

## 七、备注
- Picker/雷达替换/SAM 界面自动启用（本轮已完成并编译）
- inject-shell 换弹壳（未实施——M3 配套）
- 海战：结构预留（模式标签灰 + 数据域 ships 已有）
