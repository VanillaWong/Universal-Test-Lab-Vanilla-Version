#!/usr/bin/env node
// ============================================================================
// rebuild-unit-weapons.js — 只重建 data/unit_weapons.json（武器列表）
// ----------------------------------------------------------------------------
// 为什么单独有这个脚本：
//   Build-Catalog.ps1 全量重建要跑 40 分钟（飞机→挂载→地面→弹药→合并）。
//   而"换炮武器列表"只依赖 ground.tsv + units/tankmodels/*.blk，
//   单独重刷只要几秒，之后重新编译即可生效。
//
// 用法：
//   node tools/rebuild-unit-weapons.js
//   node tools/rebuild-unit-weapons.js --tsv-dir data --units-dir universal_units_data/...
//   （默认从项目根运行；保留舰船/飞机的现有条目，只重刷地面车的全部武器）
//
// 输出：data/unit_weapons.json + data/unit_weapons.tsv
// 之后：运行 compile.bat 让 exe 嵌入新的 JSON
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');

const projectRoot = path.resolve(__dirname, '..');
const args = process.argv.slice(2);
function argValue(name, fallback) {
  const i = args.indexOf('--' + name);
  return i >= 0 && args[i + 1] ? args[i + 1] : fallback;
}
const dataDir = path.join(projectRoot, argValue('tsv-dir', 'data'));
const unitsRoot = path.join(projectRoot, argValue('units-dir', 'universal_units_data/aces.vromfs.bin_u/gamedata/units'));
const tanksDir = path.join(unitsRoot, 'tankmodels');
const jsonPath = path.join(dataDir, 'unit_weapons.json');
const tsvPath = path.join(dataDir, 'unit_weapons.tsv');

function blocksNamed(text, name) {
  const out = [];
  const re = new RegExp('(?:^|\\n)\\s*' + name + '\\s*\\{', 'g');
  let m;
  while ((m = re.exec(text))) {
    const open = text.indexOf('{', m.index);
    let depth = 0, end = -1;
    for (let i = open; i < text.length; i++) {
      if (text[i] === '{') depth++;
      else if (text[i] === '}') { depth--; if (depth === 0) { end = i; break; } }
    }
    if (end > open) out.push(text.slice(open, end + 1));
  }
  return out;
}
function displayName(blk) {
  const leaf = blk.replace(/\\/g, '/').split('/').pop().replace(/\.blk$/i, '');
  return leaf.replace(/[_\.]+/g, ' ').replace(/\s+/g, ' ').trim();
}

if (!fs.existsSync(tanksDir)) { console.error('tankmodels dir not found:', tanksDir); process.exit(1); }
if (!fs.existsSync(jsonPath)) { console.error('missing existing catalog:', jsonPath); process.exit(1); }

const existing = JSON.parse(fs.readFileSync(jsonPath, 'utf8'));
const keep = existing.filter(e => (e.domain || '').toLowerCase() !== 'ground');
console.log('[weapons] existing entries:', existing.length, '| keeping non-ground:', keep.length);

const groundLines = fs.readFileSync(path.join(dataDir, 'ground.tsv'), 'utf8').split(/\r?\n/).filter(Boolean);
console.log('[weapons] ground vehicles:', groundLines.length);

const rows = [];
const seen = new Set();
let missing = 0;
for (const line of groundLines) {
  const p = line.split('\t');
  const id = (p[0] || '').replace(/^\uFEFF/, '').trim();
  const display = (p[1] || id).trim();
  if (!id) continue;
  let text;
  try { text = fs.readFileSync(path.join(tanksDir, id + '.blk'), 'utf8'); }
  catch (e) { missing++; continue; }
  for (const w of blocksNamed(text, 'Weapon')) {
    const bm = w.match(/^\s*blk:t\s*=\s*"([^"]+)"/m);
    if (!bm) continue;
    const blk = bm[1];
    const tm = w.match(/^\s*trigger:t\s*=\s*"([^"]+)"/m);
    const trigger = tm ? tm[1] : '';
    const isDummy = /dummy_weapon/i.test(blk);
    const key = id + '|ground|' + trigger + '|' + blk;
    if (seen.has(key)) continue;
    seen.add(key);
    rows.push({
      unitId: id, domain: 'ground', unitDisplay: display, weaponBlk: blk,
      weaponDisplay: isDummy ? '(empty slot)' : displayName(blk),
      kind: isDummy ? 'dummy' : 'cannon', trigger
    });
  }
}
console.log('[weapons] ground entries:', rows.length, '| dummy slots:', rows.filter(r => r.kind === 'dummy').length,
  '| vehicles without file:', missing);

const merged = rows.concat(keep);
fs.writeFileSync(jsonPath, JSON.stringify(merged, null, 2) + '\n', 'utf8');
fs.writeFileSync(tsvPath, merged.map(r => [r.unitId, r.domain, r.unitDisplay, r.weaponBlk, r.weaponDisplay, r.kind, r.trigger || ''].join('\t')).join('\n') + '\n', 'utf8');
console.log('[weapons] written', merged.length, 'entries ->', jsonPath.replace(projectRoot + path.sep, ''));
console.log('[weapons] now run: compile.bat');
