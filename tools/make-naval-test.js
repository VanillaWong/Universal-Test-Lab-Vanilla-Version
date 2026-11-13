#!/usr/bin/env node
// ============================================================================
// make-naval-test.js - PORT TEST for a player-controlled ship.
//
// War Thunder has no "play this ship" flow in user missions by itself; the
// recipe is a two-line proxy unit under
//   pkg_local/gameData/units/ships/userVehicles/<shell>.blk
// plus a player unit inside the mission's ships{} tree.
//
// This harness takes the reference naval mission as a skeleton (its level,
// targets and triggers), swaps in our own proxy/class/campaign and drops the
// result into UserMissions/UTL so it can be loaded in game. It is a TEST
// harness: once the recipe is confirmed we implement it natively in UTL.
//
// Usage:
//   node tools/make-naval-test.js
//   node tools/make-naval-test.js --ship us_cruiser_rckt_california
//   node tools/make-naval-test.js --list
// ============================================================================
'use strict';
const fs = require('fs');
const path = require('path');

const projectRoot = path.resolve(__dirname, '..');
const args = process.argv.slice(2);
const argValue = (n, d) => { const i = args.indexOf('--' + n); return i >= 0 && args[i + 1] ? args[i + 1] : d; };

const GAME = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\War Thunder';
const REFERENCE = path.join(GAME, 'UserMissions', 'Ask3lad', 'ask3lad_testdrive_naval.blk');
const OUT_DIR = path.join(GAME, 'UserMissions', 'UTL');
const PROXY_DIR = path.join(GAME, 'content', 'pkg_local', 'gameData', 'units', 'ships', 'userVehicles');
const SHELL = 'utl_naval_player';
const shipId = argValue('ship', 'germ_minelayer_drache');
const ships = JSON.parse(fs.readFileSync(path.join(projectRoot, 'data', 'ships.json'), 'utf8'));

if (args.includes('--list')) {
  ships.slice(0, 40).forEach(s => console.log('  ' + s.id.padEnd(40) + s.display));
  console.log('  ... ' + ships.length + ' ships total');
  process.exit(0);
}
const ship = ships.find(s => s.id.toLowerCase() === shipId.toLowerCase());
if (!ship) throw new Error('Ship not found in data/ships.json: ' + shipId);
if (!fs.existsSync(REFERENCE)) throw new Error('Reference naval mission missing: ' + REFERENCE);

let mission = fs.readFileSync(REFERENCE, 'utf8');
const before = mission;
mission = mission
  .replace(/campaign:t="[^"]*"/g, 'campaign:t="UTL"')
  .replace(/chapter:t="[^"]*"/g, 'chapter:t="TestDrive"')
  .replace(/locName:t="[^"]*"/g, 'locName:t="UTL Naval Test - ' + ship.display.replace(/"/g, '') + '"')
  .replace(/locDesc:t="[^"]*"/g, 'locDesc:t="Port test: player-controlled ship on a stock naval map."')
  .replace(/unit_class:t="userVehicles\/us_pt6"/g, 'unit_class:t="userVehicles/' + SHELL + '"');
if (mission === before) throw new Error('Nothing was replaced - reference mission layout changed.');

fs.mkdirSync(OUT_DIR, { recursive: true });
fs.mkdirSync(PROXY_DIR, { recursive: true });

const proxy = 'include "#/develop/gameBase/gameData/units/ships/' + ship.id + '.blk"' + String.fromCharCode(10);
fs.writeFileSync(path.join(PROXY_DIR, SHELL + '.blk'), proxy, 'utf8');
const out = path.join(OUT_DIR, 'utl_naval_test.blk');
fs.writeFileSync(out, mission, 'utf8');

console.log('ship     : ' + ship.id + '  (' + ship.display + ')');
console.log('proxy    : ' + path.relative(GAME, path.join(PROXY_DIR, SHELL + '.blk')));
console.log('mission  : ' + path.relative(GAME, out) + '  (' + mission.length + ' bytes)');
console.log('load     : User Missions -> campaign "UTL" -> UTL Naval Test');
