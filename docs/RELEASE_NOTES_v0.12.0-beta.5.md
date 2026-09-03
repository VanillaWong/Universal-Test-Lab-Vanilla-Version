# Universal Test Lab v0.12.0-beta.5

This beta focuses on **data-layer speed and maintainability**: the embedded catalog is now JSON, weapon selection no longer re-extracts game archives, and the catalog has been refreshed against the latest game update. It also fixes missile-vehicle weapon swaps and converts the experimental SARH-to-fake-ARH switch to handle twin-missile files.

本测试版聚焦**数据层速度与可维护性**：内嵌目录已全面转为 JSON、选择武器不再重新解包游戏档案、目录已随最新游戏版本刷新；同时修复导弹车换炮、并让实验性伪ARH 转换支持双弹组文件。

## Added / 新增

- **English–Chinese UI localization** (`ModernText`) applied across the main windows, with an in-app language toggle and XAML node localization pass. / 主界面**中英双语本地化**（ModernText），应用内语言切换 + XAML 节点本地化。
- **Home ammunition panel (Ask3lad-style)** for ground vehicles: per-slot loadout editor with belt/round grouping, capacity bookkeeping and constrained per-type sliders. / 地面载具**主页弹药面板**（Ask3lad 式）：逐槽位挂载编辑、弹链/弹种分组、容量核算与受限滑块。
- Catalog JSON migration: all embedded catalogs converted from TSV to JSON (`tools/tsv2json.js`, 18 JSON catalogs embedded in the executable). / 目录 JSON 迁移：全部内嵌目录由 TSV 转为 JSON（`tools/tsv2json.js`，exe 内嵌 18 份 JSON 目录）。
- **Zero-extract weapon browsing**: `ExtractGameBlk` now reads a pre-extracted `universal_units_data` / `universal_weapons_data` tree next to the executable first, so swapping weapons no longer launches the extraction tool for uncached resources. / **零解包武器浏览**：ExtractGameBlk 优先读取 exe 旁预解包的 universal_units_data / universal_weapons_data 数据树，换武器不再为未缓存资源启动解包工具。
- Weapon-swap main-gun detection now skips `dummy:b=true` camera weapons, so launcher/SAM vehicles swap the real missile mount instead of the observation sight. / 换炮主武器识别现在跳过 `dummy:b=true` 观瞄武器——发射车/防空车换炮会替换真实导弹架而非观察镜。
- Refreshed catalog from the 2026-09-01 game update (1,577 aircraft, 29,837 donor mounts, 51,258 modifications). / 目录已随 2026-09-01 游戏更新刷新（1577 架飞机、29837 个挂载、51258 项改装）。

## Fixed / 修复

- The experimental SARH → fake-ARH conversion now patches **every** guidance/radarSeeker block, so twin-missile files (MIM-104, 5V55/S-300, …) convert both missile groups instead of leaving the second one as true SARH. / 实验性伪ARH 转换现在会修补**每一个** guidance/radarSeeker 块——双弹组文件（MIM-104、5V55/S-300 等）两组弹都会转换，不再留下第二组真 SARH。
- Legacy GROUND CONFIGURE window removed (functionality split into the home ammunition panel and the EXPERIMENTAL gun-swap lab). / 移除旧版 GROUND CONFIGURE 窗口（功能已分流至主页弹药面板与 EXPERIMENTAL 换炮实验室）。
- Self-test crash when the legacy `.tsv` embedded catalogs were referenced after the JSON migration (both core and UI screenshot renderers). / 修复 JSON 迁移后引用旧 `.tsv` 内嵌目录导致的自检崩溃（核心与 UI 截图渲染两处）。
- Build-Catalog now resolves its data roots correctly when run from the project root (relative `..\` defaults assumed a script subfolder). / Build-Catalog 在项目根目录运行时能正确解析数据根路径（默认的 `..\` 前缀原假定脚本位于子目录）。

Solo sandbox mode and the beta.4 airport are unchanged.

单机沙盒模式与 beta.4 的机场功能保持不变。
