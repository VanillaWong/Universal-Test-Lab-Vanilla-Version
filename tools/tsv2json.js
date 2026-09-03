#!/usr/bin/env node
// ============================================================================
// UTL TSV -> JSON 通用转换器
// ----------------------------------------------------------------------------
// 用法:  node tools/tsv2json.js            (转换 data/*.tsv -> data/*.json)
//        node tools/tsv2json.js ground     (只转换指定文件, 可多次/逗号分隔)
//
// 规则:
//  - 每个 TSV 一张 schema 表 (列名数组, 列语义取自 UniversalTestLab.cs 解析器)
//  - era_presets.tsv 自带 header 行 (首行列名即 schema)
//  - 短行补空串 / 长行截断到 schema 列数
//  - 单元格类型推断: int / float / bool / '' / string
//  - 输出: data/<name>.json  (pretty 2 空格, 数组 of objects)
//  - 不动原 .tsv
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');

const DATA_DIR = path.join(__dirname, '..', 'data');

// ---- schema 表 (文件名 -> 列名数组; hasHeader: 首行即列名) ------------------
const SCHEMAS = {
  'aircraft.tsv': {
    cols: ['id', 'display', 'type', 'defaultPreset', 'nation', 'rank', 'maxLoad', 'kind']
  },
  'ground.tsv': {
    cols: ['id', 'display', 'defaultPreset', 'nation', 'rank', 'type',
           'mainWeaponBlk', 'maxAmmo', 'mass', 'enginePower',
           'forwardSpeed', 'reverseSpeed', 'reloadSeconds', 'recoil']
  },
  'ships.tsv': {
    cols: ['id', 'display', 'defaultPreset', 'nation', 'rank', 'type']
  },
  'donor_weapons.tsv': {
    cols: ['aircraftId', 'aircraftDisplay', 'slot', 'mount', 'trigger', 'blk',
           'emitter', 'bullets', 'icon', 'name', 'category', 'unitMass', 'totalMass']
  },
  'weapon_catalog.tsv': {
    cols: ['trigger', 'blk', 'bullets', 'icon', 'name', 'category', 'unitMass', 'totalMass']
  },
  'naval_cannons.tsv': {
    cols: ['key', 'value']
  },
  'unit_weapons.tsv': {
    cols: ['unitId', 'domain', 'unitDisplay', 'weaponBlk', 'weaponDisplay', 'kind']
  },
  'air_ordnance.tsv': {
    cols: ['blk', 'display', 'kind']
  },
  'aircraft_slots.tsv': {
    cols: ['aircraftId', 'slot', 'order', 'tier', 'maxLoad', 'anchorMount']
  },
  'modifications.tsv': {
    cols: ['aircraftId', 'id', 'display', 'tier', 'modClass', 'group', 'requires']
  },
  'combined_maps.tsv': {
    // kind=capture: detail=点id; kind=spawn: detail=spawn选项
    cols: ['id', 'display', 'level', 'kind', 'side', 'detail', 'label', 'transform', 'objectClass']
  },
  'belt_type_limits.tsv': {
    cols: ['id', 'value']
  },
  'nuclear.tsv': {
    cols: ['aircraftId', 'display', 'loadoutId']
  },
  'presets.tsv': {
    cols: ['aircraftId', 'presetId', 'name']
  },
  'preset_slots.tsv': {
    cols: ['aircraftId', 'presetId', 'slot', 'item']
  },
  'era_presets.tsv': {
    hasHeader: true,
    cols: ['name', 'groundIds', 'airIds', 'airCounts', 'shipId', 'shipCount']
  },
  'sensors.tsv': {
    cols: ['id', 'display', 'band']
  }
};

// ---- 类型推断 --------------------------------------------------------------
function inferType(raw) {
  const s = raw.trim();
  if (s === '') return '';
  if (/^[-+]?\d+$/.test(s)) return parseInt(s, 10);
  if (/^[-+]?\d*\.\d+([eE][-+]?\d+)?$/.test(s) || /^[-+]?\d+[eE][-+]?\d+$/.test(s)) {
    const v = parseFloat(s);
    return Number.isFinite(v) ? v : s;
  }
  if (/^(true|yes)$/i.test(s)) return true;
  if (/^(false|no)$/i.test(s)) return false;
  return s;
}

// ---- 单文件转换 -------------------------------------------------------------
function convertFile(name) {
  const spec = SCHEMAS[name];
  if (!spec) { console.log('skip (no schema): ' + name); return null; }
  const p = path.join(DATA_DIR, name);
  if (!fs.existsSync(p)) { console.log('skip (missing): ' + name); return null; }

  const rawLines = fs.readFileSync(p, 'utf8').split(/\r?\n/);
  const rows = [];
  let startIdx = 0;
  if (spec.hasHeader) startIdx = 1; // 首行列名与 schema 相同, 直接丢弃

  for (let i = startIdx; i < rawLines.length; i++) {
    const line = rawLines[i];
    if (!line || !line.trim()) continue;           // 跳空行
    if (line.trim().startsWith('#')) continue;     // 跳注释
    const cells = line.split('\t');
    const obj = {};
    for (let c = 0; c < spec.cols.length; c++) {
      obj[spec.cols[c]] = c < cells.length ? inferType(cells[c]) : '';
    }
    rows.push(obj);
  }

  const out = path.join(DATA_DIR, name.replace(/\.tsv$/, '.json'));
  fs.writeFileSync(out, JSON.stringify(rows, null, 2) + '\n', 'utf8');

  // Compact twin under data/embed/<name>.json - this is what gets embedded into
  // the executable (build.rsp). Pretty copies stay in data/ for humans.
  const embedDir = path.join(DATA_DIR, 'embed');
  if (!fs.existsSync(embedDir)) fs.mkdirSync(embedDir, { recursive: true });
  fs.writeFileSync(path.join(embedDir, name.replace(/\.tsv$/, '.json')), JSON.stringify(rows), 'utf8');
  return { rows: rows.length, bytes: fs.statSync(p).size, out: path.basename(out) };
}

// ---- main ------------------------------------------------------------------
const args = process.argv.slice(2);
const targets = args.length
  ? args.flatMap(a => a.split(',')).map(a => a.trim() + (/\.tsv$/i.test(a) ? '' : '.tsv'))
  : Object.keys(SCHEMAS);

let ok = 0, fail = 0;
for (const t of targets) {
  try {
    const r = convertFile(t);
    if (r) { console.log(`OK   ${t.padEnd(24)} ${String(r.rows).padStart(6)} rows  ${(r.bytes / 1024).toFixed(0).padStart(5)}KB -> ${r.out}`); ok++; }
    else fail++;
  } catch (e) { console.log(`ERR  ${t}: ${e.message}`); fail++; }
}
console.log(`\ndone: ${ok} converted, ${fail} skipped`);
