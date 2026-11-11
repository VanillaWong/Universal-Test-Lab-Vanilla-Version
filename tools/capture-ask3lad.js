#!/usr/bin/env node
// ============================================================================
// capture-ask3lad.js - snapshot Ask3lad's generated files and diff them
// against War Thunder's native data, so each enabled feature shows up as the
// exact set of injected lines.
//
// Usage:
//   node tools/capture-ask3lad.js             # snapshot + diff + report
//   node tools/capture-ask3lad.js --quiet     # diff only, no snapshot folder
//
// Output: captures/ask3lad-<timestamp>/report.txt  (and a copy of each .blk)
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');

const projectRoot = path.resolve(__dirname, '..');
const args = process.argv.slice(2);
const quiet = args.includes('--quiet');
const GAME = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\War Thunder';
const PKG = path.join(GAME, 'content', 'pkg_local');
const MISSIONS = path.join(GAME, 'UserMissions', 'Ask3lad');
const UNITS = path.join(projectRoot, 'universal_units_data/aces.vromfs.bin_u/gamedata/units/tankmodels');
const FMS = path.join(projectRoot, 'universal_game_data/aces.vromfs.bin_u/gamedata/flightmodels');
const stamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const outDir = path.join(projectRoot, 'captures', 'ask3lad-' + stamp);
if (!quiet) fs.mkdirSync(outDir, { recursive: true });

function walk(dir, out = []) {
  if (!fs.existsSync(dir)) return out;
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) walk(p, out);
    else if (/\.blk$/i.test(e.name)) out.push(p);
  }
  return out;
}

// multiset diff of trimmed lines
function diffLines(nativeText, hisText) {
  const S = t => t.split(/\r?\n/).map(s => s.trim())
    .filter(s => s && !/^[{}]+$/.test(s));   // drop bare braces: structural noise
  const a = S(nativeText), b = S(hisText);
  const ca = {}, cb = {};
  for (const x of a) ca[x] = (ca[x] || 0) + 1;
  for (const x of b) cb[x] = (cb[x] || 0) + 1;
  const added = [], removed = [];
  for (const k of new Set([...Object.keys(ca), ...Object.keys(cb)])) {
    const d = (cb[k] || 0) - (ca[k] || 0);
    if (d > 0) for (let i = 0; i < d; i++) added.push(k);
    if (d < 0) for (let i = 0; i < -d; i++) removed.push(k);
  }
  return { added, removed };
}

// path-only case rewrites are noise; group them separately
const isCaseOnly = (a, b) => a.toLowerCase() === b.toLowerCase();
function splitNoise(added, removed) {
  const realAdd = [], caseAdd = [];
  for (const x of added) {
    const twin = removed.find(r => r !== null && isCaseOnly(r, x));
    if (twin) { removed[removed.indexOf(twin)] = null; caseAdd.push(x); }
    else realAdd.push(x);
  }
  return { realAdd, realRemoved: removed.filter(Boolean), caseAdd };
}

const targets = [
  { file: path.join(PKG, 'gameData', 'units', 'tankModels', 'ask3lad_ground_player.blk'), kind: 'ground' },
  { file: path.join(PKG, 'gameData', 'flightModels', 'ask3lad_air_player.blk'), kind: 'air' },
  { file: path.join(PKG, 'gameData', 'flightModels', 'ask3lad_ground_support.blk'), kind: 'air' }
];

const report = [];
report.push('Ask3lad capture  ' + new Date().toISOString());
report.push('='.repeat(78));

for (const t of targets) {
  if (!fs.existsSync(t.file)) { report.push('\n### ' + path.basename(t.file) + '  (missing)'); continue; }
  const his = fs.readFileSync(t.file, 'utf8');
  const header = (his.split(/\r?\n/)[0] || '').trim();
  const m = header.match(/player:\s*([A-Za-z0-9_\-]+)/);
  const base = m ? m[1] : null;
  const natPath = base
    ? (t.kind === 'ground' ? path.join(UNITS, base + '.blk') : path.join(FMS, base + '.blk'))
    : null;
  report.push('');
  report.push('### ' + path.basename(t.file) + '   [' + his.length + ' bytes]');
  report.push('    header: ' + header.slice(0, 160));
  if (!natPath || !fs.existsSync(natPath)) { report.push('    native base NOT FOUND for: ' + base); continue; }
  const nat = fs.readFileSync(natPath, 'utf8');
  const { added, removed } = diffLines(nat, his);
  const { realAdd, realRemoved, caseAdd } = splitNoise(added.slice(), removed.slice());
  report.push('    native base: ' + path.basename(natPath));
  report.push('    added ' + realAdd.length + '   removed ' + realRemoved.length + '   (path-case rewrites: ' + caseAdd.length + ')');
  report.push('    --- ADDED ---');
  realAdd.forEach(x => report.push('    + ' + x));
  report.push('    --- REMOVED ---');
  realRemoved.forEach(x => report.push('    - ' + x));
  if (!quiet) {
    const dst = path.join(outDir, path.basename(t.file));
    fs.writeFileSync(dst, his, 'utf8');
  }
}

// also copy the missions + weapon files so a capture is self-contained
const extras = walk(PKG).filter(p => /ask3lad/i.test(p)).concat(walk(MISSIONS));
if (!quiet) {
  const sub = path.join(outDir, 'files');
  for (const p of extras) {
    const rel = p.replace(/[:\\/]+/g, '_');
    try { fs.copyFileSync(p, path.join(sub ? (fs.mkdirSync(sub, { recursive: true }), sub) : outDir, rel)); } catch (e) {}
  }
  fs.writeFileSync(path.join(outDir, 'report.txt'), report.join('\n'), 'utf8');
  console.log('capture written: ' + path.relative(projectRoot, outDir) + '\\report.txt');
}
console.log(report.join('\n'));
