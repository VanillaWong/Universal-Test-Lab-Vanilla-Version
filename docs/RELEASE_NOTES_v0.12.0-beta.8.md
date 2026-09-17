# Universal Test Lab v0.12.0-beta.8

This release finishes the gun-swap saga: the experimental lab can now replace
**any** weapon on a vehicle, not just its main gun — including the `SKIP` option
for "leave the main gun alone, I only want the extra replacements". Alongside it
come fixes for multi-gun vehicles whose secondary ammunition collapsed to a
single round (BMD-4's 100 mm) and for Bradley TOW rounds that could not be picked
at all, plus a full data refresh against the **2026-09-17 game update**.

本次把换炮 saga 收尾:实验台现在可以替换车上的**任意**武器,不再只能换主炮,并新增
`SKIP` 选项——"别动主炮,我只要那几条额外替换"。同时修掉多炮车副炮弹药塌成 1 发
(BMD-4 的 100mm)与布拉德利 TOW 弹完全选不到的问题,并针对 **2026-09-17 游戏更新**
做了全量数据刷新。

## Added / 新增

- **Per-slot cannon swaps / 任意槽位换炮**: pick the host weapon slot from a
  dropdown — `AUTO`, `SKIP`, or any `gunner0…N`. `SKIP` keeps the main gun and
  applies only the extra replacements listed below it; each row carries its own
  donor weapon and ammunition. / 宿主槽位下拉可选 `AUTO` / `SKIP` / 任意 `gunnerN`。
  选 `SKIP` 保留主炮,只执行下方额外替换列表,每行各自带来源武器与弹药。
- **All-weapon catalog with triggers / 全武器目录(含 trigger)**: the weapon list
  covers every weapon in a vehicle file — secondary guns, machine guns and empty
  `dummy` camera slots included — and shows each `trigger` name, so vehicles whose
  triggers are not consecutive (BMPT: `gunner0`, `gunner7`, …) no longer look like
  they have empty slots. / 武器列表收录全部武器(含副炮、机枪、空 dummy 观察槽)并显示
  `trigger` 名;trigger 不连续的多炮车(BMPT 的 gunner0、gunner7…)不再显示成空槽。
- Swap settings now persist **per vehicle** across sessions (they were not saved
  at all before). / 换炮设置现在**按载具持久化**(此前完全没保存)。

## Fixed / 修复

- **Native multi-gun vehicles lost their secondary ammunition / 原生多炮车副炮弹药
  丢失**: BMD-4's 100 mm gun came up with one round. The carrier-group ammunition
  path — which fills all four mission slots from the vehicle's containers — was
  treating any vehicle that has a gun as a missile-only carrier. It is now
  restricted to missile-only carriers the player has not configured. / BMD-4 的
  100mm 只剩 1 发:会把四个任务弹槽按容器顺序塞满的"载弹车按组装弹"路径,把"有炮的车"
  误判成纯导弹车。现在只对"纯导弹发射车且玩家未配置弹药"生效。
- **Bradley TOW rounds were unpickable / 布拉德利 TOW 弹选不到**: Tow 2A/2B are
  labelled 152 mm while the launcher is 127 mm, so the calibre filter dropped them
  from the ammo picker. They are now merged into the launcher group by name
  keyword. / TOW 2A/2B 标注 152mm、发射器为 127mm,被口径过滤筛掉了;现按名字关键词
  并入发射器分组。
- `config.json` is written as indented JSON again instead of one long line. /
  `config.json` 恢复缩进格式,不再是一行。

## Changed / 变更

- **Data catalog refreshed for the 2026-09-17 game update / 数据目录随 2026-09-17
  游戏更新刷新**: 1605 aircraft · 1303 ground vehicles · 640 ships · 32670 donor
  mounts · 52210 modifications · 466 sensor blocks.
- Air and naval weapon entries are complete again — the ground-only quick rebuild
  had been preserving them from an older game version, which silently kept those
  two lists stale. / 飞机与舰船武器条目补齐:"只重刷地面"的快速脚本此前会原样保留它们,
  导致这两类列表悄悄停留在旧数据上。
- `Update-Data.ps1` now also unpacks `gamedata/sensors` and rebuilds the radar
  catalogs, so a future game update cannot leave them stale. / `Update-Data.ps1`
  现在同时解包 `gamedata/sensors` 并重建雷达目录,以后不会再残留旧数据。
- Groundwork only, no UI yet: an aircraft radar dataset (`aircraft_radars.json` /
  `aircraft_radar_sites.json`). Nothing reads it at runtime, so behaviour is
  unchanged. / 仅地基、暂无界面:飞机雷达数据集。运行时尚未使用,行为无变化。

## Notes / 说明

- Because the data catalog was rebuilt, the executable is a full refresh — if you
  keep an older copy around, replace it rather than mixing data files.
  / 由于数据目录整体重建,请直接替换整个 exe,不要把新旧数据文件混用。
- Crash reports and bug reports go to GitHub Issues:
  https://github.com/VanillaWong/Universal-Test-Lab-Vanilla-Version/issues
  Please include the crash log and what you were doing.
  报错请到 GitHub Issues,附上崩溃日志与操作描述即可。
