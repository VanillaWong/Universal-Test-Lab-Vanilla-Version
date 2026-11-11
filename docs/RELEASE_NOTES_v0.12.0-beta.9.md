# Universal Test Lab v0.12.0-beta.9

A maintenance release that catches the tool up with the **2026-11-11 game update**
and clears two long-standing bugs: one-click assembly presets that spawned with no
ammunition, and aircraft that came back from the dead at 0 km/h. It also adds the
first batch of **experimental airframe options** to Flight Configure.

本次为维护版本：跟随 **2026-11-11 游戏更新**刷新数据，并修掉两个老问题——一键装配后
弹药耗尽、以及飞机重生时速度为 0。同时「飞行配置」新增第一批**实验性机体选项**。

## Added / 新增

- **Experimental airframe options / 实验性机体选项** (Flight Configure → EXPERIMENTAL AIRFRAME):
  - *Zero mass* — the flight model's empty mass is scaled to ~1 %. / **零质量**——空重缩到约 1%。
  - *Indestructible* — never-exceed speed, Mach, overload and gear/canopy limits are raised. /
    **超速过载不损毁**——放宽 VNE、马赫、过载与起落架/舱盖限制。
  - *Helmet-mounted designator (HMD)* — adds HMD designator fields to any aircraft. /
    **头盔瞄准具**——为任意机型补上 HMD 瞄准指示字段。
  - *Missile-approach warner (MAWS)* — appends a missile-approach warner to the sensor set. /
    **导弹来袭告警**——在传感器列表末尾追加来袭告警器。

  The first two write a tuned copy of the game's own flight model and reference it from the
  unit, so the game's original files are never modified. / 前两项生成一份调校后的机体文件并
  由单位引用，游戏原文件不动。

## Fixed / 修复

- **One-click assembly presets spawned with no ammunition / 一键装配后弹药耗尽**: the
  weapon-preset redirect that feeds a swapped gun its default round needs the donor
  vehicle; presets only recorded the donor cannon, so the redirect pointed at a stale or
  empty vehicle. Presets now carry the donor vehicle and fall back to a reverse lookup
  from the donor cannon. / 换炮后"默认弹"依赖武器预设重定向，而重定向需要来源载具；此前
  预设只记录来源炮，导致重定向指向旧车或干脆不写。现在预设记录来源载具，缺失时按来源炮
  反查。
- **Aircraft respawned at 0 km/h / 飞机重生 0 速度**: in the default in-air respawn mode
  the mission's respawn speed restore had been removed for the old runway behaviour. It is
  now kept for in-air respawns and still dropped for runway returns (cruise speed while
  parked would launch the aircraft). / 默认"空中重生"模式下，速度恢复触发器曾被为"回跑道"
  移除。现在空中重生保留、回跑道仍然移除。
- Loose ends from a previous cannon swap (host slot, extra slot swaps) no longer leak into
  a one-click preset. / 上一次换炮残留的宿主槽位与额外替换不再串进一键装配。

## Changed / 变更

- **Data catalog rebuilt for the 2026-11-11 game update / 数据目录随 2026-11-11 更新重建**:
  1621 aircraft · 1321 ground vehicles · 663 ships · 35028 donor mounts ·
  52822 modifications · 11617 presets · 485 sensor blocks.
- The build pipeline keeps the sensor tree and both radar catalogs in sync with the game
  version. / 构建流程继续同步传感器树与两份雷达目录。

## Notes / 说明

- The airframe options are **experimental**: they change parameters the engine does not
  normally allow. Use them for testing, not for reproducing stock behaviour. / 机体选项为
  **实验性**：会改动引擎通常不允许的参数，仅用于测试。
- Crash reports and bug reports go to GitHub Issues:
  https://github.com/VanillaWong/Universal-Test-Lab-Vanilla-Version/issues
  Please include the crash log and what you were doing.
  报错请到 GitHub Issues，附上崩溃日志与操作描述即可。
