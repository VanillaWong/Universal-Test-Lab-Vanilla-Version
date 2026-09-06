# Universal Test Lab v0.12.0-beta.7

Hotfix for a fresh-install crash reported through the new crash dialog (the
reporting pipeline already earning its keep). Map & Targets crashed on startup
for players without saved era presets; the release package now also ships the
PDB so future crash reports carry exact source line numbers.

本次为热修复:有玩家通过新版崩溃弹窗上报了全新安装时的启动崩溃(崩溃上报体系首战告捷)。
没有保存过时代预设的玩家打开「地图与目标」会直接崩溃;另外发布包现在附带 PDB,
以后玩家上报的崩溃日志会带精确源码行号,定位更快。

## Fixed / 修复

- **Fresh-install crash on Map & Targets / 全新安装打开地图与目标崩溃**: `EraPresets`
  (static) was initialized before `BuiltinEraPresets`, but its loader falls back to the
  built-in array when no presets are saved — so on clean machines the fallback returned a
  still-uninitialized `null`, and every `MapPanel` / `ModernMapWindow` constructor threw a
  bare `NullReferenceException` right at startup. Declarations are reordered in both
  classes. / `EraPresets`(静态字段)初始化先于 `BuiltinEraPresets`,而加载器在没有已保存
  预设时会回退到内置数组——全新机器上回退拿到尚未初始化的 null,导致 MapPanel 与
  ModernMapWindow 构造时直接抛 NullReferenceException。两个类中的声明顺序均已调整
  (回退数组先于使用它的字段初始化),并在代码中注释了原因。
- **PDB shipped with the release / 发布包附带 PDB**: crash logs from players now include
  exact source line numbers, not just method names. / 玩家崩溃日志现在包含精确源码行号,
  不再只有方法名。

## Notes / 说明

- Crash reports go to GitHub Issues:
  https://github.com/VanillaWong/Universal-Test-Lab-Vanilla-Version/issues
  Please include the crash log and what you were doing.
  报错请到 GitHub Issues,附上崩溃日志与操作描述即可。
