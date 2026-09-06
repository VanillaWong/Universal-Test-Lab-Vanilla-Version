# Universal Test Lab v0.12.0-beta.6

This beta **listens to the game instead of trusting paper values**: the aircraft
"maximum load" figure is no longer treated as a hard limit the game enforces
(official Su-33 loadouts reach 7504 kg against a 6500 kg reference), airfield
takeoffs now spawn at the runway end instead of its middle, and the app gained
**global crash reporting** — any unexpected error writes a timestamped log under
`%LOCALAPPDATA%\UniversalTestLab\crash.log` and shows a dialog that lets players
copy or open it, so bugs can be reported with a full stack trace.

本测试版**以游戏实测为准、不再轻信纸面数值**：不再把飞机"最大挂载"当成游戏强制上限（官方苏-33 可挂 7504kg 而对 6500kg 的参考值视而不见）、机场起飞改为在跑道端头出生（原来在跑道正中间），并新增**全局崩溃上报**——任何意外错误都会把带时间戳的日志写入 `%LOCALAPPDATA%\UniversalTestLab\crash.log`,弹窗支持一键复制/打开日志文件夹,玩家可把完整堆栈发给作者修复。

## Added / 新增

- **Crash reporting / 崩溃上报**: global handlers for UI-thread, background-task and
  AppDomain exceptions; a timestamped `crash_YYYYMMDD_HHMMSS.log` plus `crash.log`
  under `%LOCALAPPDATA%\UniversalTestLab` (fallback beside the executable), including
  version, OS, CLR and the full exception chain. A bilingual dialog explains where the
  log is and offers Copy log / Open folder, plus a link to GitHub Issues.
  / UI 线程、后台任务与 AppDomain 异常统一捕获;日志写入 `%LOCALAPPDATA%\UniversalTestLab\`
  (`crash.log` + 带时间戳副本,写不进去时回退到 exe 目录),内容含版本/OS/CLR 与完整异常链;
  双语弹窗告知日志位置,提供「复制日志 / 打开文件夹 / GitHub Issues」入口。

## Fixed / 修复

- **Load-limit gate removed / 挂载上限不再硬拦**: `ConfirmRiskyLoadout()` treated
  `maxloadMass` as an engine-enforced limit, but official loadouts exceed it (Su-33:
  6500 kg reference vs the stock 7504 kg 28×FAB-250 preset; the Su-27 reference of 8040
  exactly matches its heaviest preset while the F-15C Golden Eagle's 11793 far exceeds
  every stock loadout). The check is now a Yes/No warning the player can accept. /
  `ConfirmRiskyLoadout()` 曾把 `maxloadMass` 当作引擎强制上限,但官方预设本身就会超过它
  (苏-33: 6500kg 参考值 vs 官方 28×FAB-250 的 7504kg 预设;苏-27 的 8040 恰等于其最重
  预设,而金鹰 F-15C 的 11793 远超其全部官方方案)。该检查已降级为可确认的 Yes/No 提示。
- **Airfield takeoff spawn moved to the runway end / 机场起飞改在跑道端头**: the
  airfield spawn point sat at z=575.1 — the exact middle of the 1250 m runway — so every
  takeoff started mid-runway. It now spawns near the runway end (z=1160) for a full
  ground roll. / 机场出生点原在 z=575.1(1250m 跑道的正中间),每次起飞都从跑道中部开始;
  现改到跑道末端附近(z=1160),可全跑道滑跑起飞。

## Notes / 说明

- Crash reports go to GitHub Issues:
  https://github.com/VanillaWong/Universal-Test-Lab-Vanilla-Version/issues
  Please include the crash log and what you were doing.
  报错请到 GitHub Issues,附上崩溃日志与操作描述即可。
