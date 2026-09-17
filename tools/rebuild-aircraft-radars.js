#!/usr/bin/env node
// ============================================================================
// rebuild-aircraft-radars.js — 生成飞机换雷达用的数据(秒级)
// ----------------------------------------------------------------------------
// 为什么单独有这个脚本：
//   Build-Catalog.ps1 全量重建约 40 分钟;而"飞机雷达"数据只依赖
//   sensors/*.blk + flightmodels/*.blk + units/**/*.blk,单独重刷只要几秒。
//   (与 tools/rebuild-unit-weapons.js 同一思路)
//
// 输出：
//   data/aircraft_radars.json / .tsv        雷达候选目录(传感 blk 全量 + 能力字段)
//   data/aircraft_radar_sites.json / .tsv   每架飞机的传感器位布局(生成端/UI 门槛判定)
//
// 用法：
//   node tools/rebuild-aircraft-radars.js
//   node tools/rebuild-aircraft-radars.js --sensors-dir ... --flightmodels-dir ...
//
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

const dataDir = path.join(projectRoot, argValue('data-dir', 'data'));
const sensorsDir = path.join(projectRoot, argValue('sensors-dir',
  'universal_units_data/aces.vromfs.bin_u/gamedata/sensors'));
const flightModelsDir = path.join(projectRoot, argValue('flightmodels-dir',
  'universal_game_data/aces.vromfs.bin_u/gamedata/flightmodels'));
const unitsDir = path.join(projectRoot, argValue('units-dir',
  'universal_units_data/aces.vromfs.bin_u/gamedata/units'));

for (const d of [sensorsDir, flightModelsDir, unitsDir]) {
  if (!fs.existsSync(d)) { console.error('missing input dir:', d); process.exit(1); }
}

// ---------------------------------------------------------------- blk helpers
// Direct children of the block whose '{' sits at openIdx -> [{name, start, end}]
function directChildren(text, openIdx) {
  const out = [];
  for (let i = openIdx + 1; i < text.length; i++) {
    const c = text[i];
    if (c === '{') {
      // matching close of this child block
      let d = 0, j = i;
      for (; j < text.length; j++) {
        if (text[j] === '{') d++;
        else if (text[j] === '}') { d--; if (d === 0) break; }
      }
      // name written just before this brace (same line)
      let s = i - 1;
      while (s >= 0 && !/\n/.test(text[s])) s--;
      const raw = text.slice(s + 1, i).trim();
      const nm = raw.match(/^"?([A-Za-z0-9_.@:$-]+)"?\s*$/);
      out.push({ name: nm ? nm[1] : raw, start: i, end: j });
      i = j;
    } else if (c === '}') {
      break; // container closed
    }
  }
  return out;
}
// Find the first block named `name` at "top level" (brace depth 0) and return its text
function firstTopBlock(text, name) {
  const re = new RegExp('(?:^|\\n)[ \\t]*' + name + '\\s*\\{', 'g');
  let m;
  while ((m = re.exec(text))) {
    // only accept a real top-level block (brace depth 0 before it)
    let preDepth = 0;
    for (let i = 0; i < m.index; i++) { if (text[i] === '{') preDepth++; else if (text[i] === '}') preDepth--; }
    if (preDepth !== 0) continue;
    const open = text.indexOf('{', m.index);
    let depth = 0, end = -1;
    for (let i = open; i < text.length; i++) {
      if (text[i] === '{') depth++;
      else if (text[i] === '}') { depth--; if (depth === 0) { end = i; break; } }
    }
    if (end > open) {
      const children = directChildren(text, open);
      return { text: text.slice(open, end + 1), open, end, children };
    }
  }
  return null;
}
function field(text, name) {
  const m = text.match(new RegExp('^\\s*' + name + '\\s*:\\s*[a-z]+\\s*=\\s*"([^"]*)"', 'm'))
    || text.match(new RegExp('^\\s*' + name + '\\s*:\\s*[a-z]+\\s*=\\s*([^\\s]+)', 'm'));
  return m ? m[1] : '';
}
function number(text, name) {
  const m = text.match(new RegExp('^\\s*' + name + '\\s*:\\s*[a-z]+\\s*=\\s*(-?[\\d.]+)', 'm'));
  return m ? parseFloat(m[1]) : NaN;
}
function sensorIdOf(blkPath) {
  return String(blkPath || '').replace(/\\/g, '/').split('/').pop().replace(/\.blk$/i, '').toLowerCase();
}
function walkBlks(dir, out = []) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) {
      if (/weaponpresets/i.test(e.name)) continue;
      walkBlks(p, out);
    } else if (e.name.toLowerCase().endsWith('.blk')) out.push(p);
  }
  return out;
}

// ------------------------------------------------------------- 1) 传感 blk 表
const sensorMeta = new Map();
const sensorFiles = [];
for (const sub of ['', 'naval']) {
  const dir = sub ? path.join(sensorsDir, sub) : sensorsDir;
  if (!fs.existsSync(dir)) continue;
  for (const f of fs.readdirSync(dir)) if (f.toLowerCase().endsWith('.blk')) sensorFiles.push({ file: path.join(dir, f), naval: !!sub });
}
for (const { file, naval } of sensorFiles) {
  const text = fs.readFileSync(file, 'utf8');
  const id = path.basename(file).replace(/\.blk$/i, '').toLowerCase();
  const type = (text.match(/^\s*type\s*:\s*t\s*=\s*"([^"]+)"/m) || [])[1] || '';
  const name = (text.match(/^\s*name\s*:\s*t\s*=\s*"([^"]+)"/m) || [])[1] || id;
  // Radio band: prefer the first positive band:i in the file (the IRST channel of a
  // combined radar+IRST set carries no band, or a negative one).
  const bandAll = [...text.matchAll(/^\s*band\s*:\s*i\s*=\s*(-?\d+)/gm)].map(m => parseInt(m[1], 10));
  const bandValue = bandAll.some(v => v > 0) ? bandAll.find(v => v > 0) : (bandAll.length ? bandAll[0] : NaN);
  // rangeMax: take the largest value found in the transivers table
  let rangeMax = 0;
  for (const m of text.matchAll(/^\s*rangeMax\s*:\s*r\s*=\s*([\d.]+)/gm)) rangeMax = Math.max(rangeMax, parseFloat(m[1]));
  if (!rangeMax) {
    const rm = text.match(/^\s*rangeMax\s*:\s*r\s*=\s*([\d.]+)/m);
    if (rm) rangeMax = parseFloat(rm[1]);
  }
  const fsms = [...new Set([...text.matchAll(/^\s*fsm\s*:\s*t\s*=\s*"([^"]+)"/gm)].map(m => m[1]).filter(v => !/parkAntenna|sleep|slewing/i.test(v)))];
  const has = (re) => re.test(text);
  const fsmHas = (re) => fsms.some(v => re.test(v));
  // A radio radar carries a positive radio band, or an illumination transmitter.
  // IR/optical trackers are ALSO typed "radar" by the engine but have band -1/absent -
  // and combined radar+IRST sets (Su-27 / MiG-29) stay radio radars even though
  // they contain an infraRed visibilityType for their IRST channel.
  const illumination = has(/illuminationTransmitter/i);
  const radio = (Number.isFinite(bandValue) && bandValue > 0) || illumination;
  const ir = !radio;
  sensorMeta.set(id, {
    id,
    display: name,
    type,
    band: Number.isFinite(bandValue) ? bandValue : '',
    rangeMax: rangeMax || '',
    weaponTargetsMax: Number.isFinite(number(text, 'weaponTargetsMax')) ? number(text, 'weaponTargetsMax') : '',
    // capability flags (aircraft picker / detail card)
    ir,
    // passive = ESM / anti-radiation / RWR-style sensor that happens to be typed "radar"
    passive: /esm|rwr|spo[-_]|_apr_|harm/i.test(id),
    canSearch: fsmHas(/search|scan|surveil|acq/i),
    canTws: fsmHas(/tws/i) || has(/\btws\b/i),
    canLock: fsmHas(/lock/i),
    canTrack: fsmHas(/track/i),
    illumination,
    launchZone: has(/showMissileLaunchZone\s*:\s*b\s*=\s*true/i),
    irstCombined: has(/^\s*irst\w*\s*\{/im) || has(/irstSearchModes|irstTrack/i),
    fsm: fsms.join(','),
    naval
  });
}

// ------------------------------------------------- 2) 飞机: 传感器位 + 雷达屏
const aircraftFiles = fs.readdirSync(flightModelsDir).filter(f => f.toLowerCase().endsWith('.blk'));
const airUse = new Map();      // sensorId -> [aircraftId]
const sites = [];
for (const f of aircraftFiles) {
  const unitId = f.replace(/\.blk$/i, '');
  const text = fs.readFileSync(path.join(flightModelsDir, f), 'utf8');
  const hasRadarPage = /page\s*\{[^{}]*type\s*:\s*t\s*=\s*"radar"/.test(text);
  const hasRadarDm = /\bradar_dm\b/.test(text);
  const container = firstTopBlock(text, 'sensors');
  let sensors = [];
  if (container) {
    let index = 0;
    for (const child of container.children) {
      if (child.name !== 'sensor') continue;
      const body = text.slice(child.start, child.end + 1);
      const blk = field(body, 'blk');
      const id = sensorIdOf(blk);
      const meta = sensorMeta.get(id);
      sensors.push({
        index: index++,
        id,
        blk,
        dmPart: field(body, 'dmPart'),
        node: field(body, 'node'),
        external: /external\s*:\s*b\s*=\s*true/i.test(body),
        type: meta ? meta.type : '',
        ir: meta ? meta.ir : false,
        passive: meta ? !!meta.passive : false,
        // an active (radio, non-ESM) radar - the ones the swap picker may install
        radioRadar: !!meta && meta.type === 'radar' && !meta.ir && !meta.passive
      });
      if (meta) {
        if (!airUse.has(id)) airUse.set(id, []);
        airUse.get(id).push(unitId);
      }
    }
  }
  // The aircraft's real radar is the radio radar bound to the radar_dm damage
  // part; extra radio sensors (ESM / anti-radiation pods) must not be picked.
  const radars = sensors.filter(s => s.radioRadar);
  const mainRadar = radars.find(s => /^radar/i.test(s.dmPart)) || radars[0] || null;
  if (sensors.length === 0) continue;          // 无 sensors 块 -> 不记录(UI 视为不支持)
  sites.push({
    unitId,
    hasRadarPage,
    hasRadarDm,
    radarCount: radars.length,
    radarIndex: mainRadar ? mainRadar.index : null,
    radarId: mainRadar ? mainRadar.id : '',
    radarDmPart: mainRadar ? mainRadar.dmPart : '',
    // passive sensors (ESM / anti-radiation) carried alongside the radar
    esmCount: sensors.filter(s => s.passive).length,
    sensors
  });
}

// ------------------------------------------------ 3) 地面/舰船 对传感 blk 的引用
const groundUse = new Map(), navalUse = new Map(), otherUse = new Map();
for (const p of walkBlks(unitsDir)) {
  const rel = path.relative(unitsDir, p).replace(/\\/g, '/');
  const text = fs.readFileSync(p, 'utf8');
  const refs = [...text.matchAll(/blk\s*:\s*t\s*=\s*"([^"]*gameData\/sensors\/[^"]+)"/gi)].map(m => sensorIdOf(m[1]));
  if (!refs.length) continue;
  const bucket = /^(ships)\//i.test(rel) ? navalUse
    : /^(tankmodels|tracked_vehicles|wheeled_vehicles|air_defence|radars)\//i.test(rel) ? groundUse : otherUse;
  const unitId = path.basename(p).replace(/\.blk$/i, '');
  for (const id of new Set(refs)) {
    if (!bucket.has(id)) bucket.set(id, []);
    bucket.get(id).push(unitId);
  }
}

// ------------------------------------------------------------- 4) 写出目录
const radarRows = [];
for (const meta of sensorMeta.values()) {
  if (meta.type !== 'radar') continue;                       // rwr / lws / mlws / lds 不进雷达选择器
  const air = airUse.get(meta.id) || [];
  const gr = groundUse.get(meta.id) || [];
  const nv = navalUse.get(meta.id) || [];
  const ot = otherUse.get(meta.id) || [];
  radarRows.push({
    id: meta.id,
    display: meta.display,
    band: meta.band,
    role: meta.canSearch && meta.canTrack ? 'search+track' : (meta.canSearch ? 'search' : (meta.canTrack ? 'track' : '')),
    rangeMax: meta.rangeMax,
    type: meta.type,
    fsm: meta.fsm,
    weaponTargetsMax: meta.weaponTargetsMax,
    irst: meta.irstCombined ? 1 : '',
    ir: meta.ir ? 1 : '',
    passive: meta.passive ? 1 : '',
    illumination: meta.illumination ? 1 : '',
    tws: meta.canTws ? 1 : '',
    launchZone: meta.launchZone ? 1 : '',
    airUse: air.length,
    groundUse: gr.length + ot.length,
    navalUse: nv.length,
    airExample: air.length ? air.slice(0, 3).join(',') : ''
  });
}
radarRows.sort((a, b) => (b.airUse - a.airUse) || (b.rangeMax || 0) - (a.rangeMax || 0) || a.id.localeCompare(b.id));

const radarJson = path.join(dataDir, 'aircraft_radars.json');
const radarTsv = path.join(dataDir, 'aircraft_radars.tsv');
fs.writeFileSync(radarJson, JSON.stringify(radarRows, null, 2) + '\n', 'utf8');
fs.writeFileSync(radarTsv, radarRows.map(r => [
  r.id, r.display, r.band, r.role, r.rangeMax, r.type, r.fsm, r.weaponTargetsMax,
  r.irst, r.ir, r.passive, r.illumination, r.tws, r.launchZone, r.airUse, r.groundUse, r.navalUse, r.airExample
].join('\t')).join('\n') + '\n', 'utf8');

const sitesJson = path.join(dataDir, 'aircraft_radar_sites.json');
const sitesTsv = path.join(dataDir, 'aircraft_radar_sites.tsv');
const sitesSorted = sites.slice().sort((a, b) => a.unitId.localeCompare(b.unitId));
fs.writeFileSync(sitesJson, JSON.stringify(sitesSorted, null, 2) + '\n', 'utf8');
fs.writeFileSync(sitesTsv, sitesSorted.map(s => [
  s.unitId, s.hasRadarPage ? 1 : '', s.hasRadarDm ? 1 : '', s.radarCount,
  s.radarIndex === null ? '' : s.radarIndex, s.radarId, s.radarDmPart, s.esmCount,
  s.sensors.map(x => x.index + ':' + (x.radioRadar ? 'radar' : (x.ir ? 'ir' : x.type || '?')) + ':' + x.id).join(' ')
].join('\t')).join('\n') + '\n', 'utf8');

// ------------------------------------------------------------------ 5) 汇总
const radioCount = radarRows.filter(r => !r.ir).length;
const irCount = radarRows.filter(r => r.ir).length;
const withRadar = sites.filter(s => s.radarCount > 0);
const withPage = withRadar.filter(s => s.hasRadarPage);
console.log('[radars] sensor blks       :', sensorFiles.length);
console.log('[radars] radar-type blks   :', radarRows.length, '(radio ' + radioCount + ' / ir ' + irCount + ')');
console.log('[radars] illumination      :', radarRows.filter(r => r.illumination).length);
console.log('[radars] referenced by air :', radarRows.filter(r => r.airUse > 0).length);
console.log('[sites ] aircraft with sensors:', sites.length);
console.log('[sites ]   ... with a radar  :', withRadar.length, '(with radar page ' + withPage.length + ')');
for (const [p, n] of [[radarJson, radarRows.length], [sitesJson, sitesSorted.length]])
  console.log('[radars] wrote', path.relative(projectRoot, p).replace(/\\/g, '/'), '(' + n + ' rows)');
console.log('[radars] now run: compile.bat');
