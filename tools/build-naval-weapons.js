// Build the naval weapon tables used by the generator:
//
//   data/naval_weapons.json
//     ships  : unit -> weapon groups -> ammo containers (mission bullets0..3)
//     missiles: every naval missile container -> source blk, guidance, ships
//
// The mission only has four ammo slots, so the group list is what the generator
// must respect: one container per group, in file order.
const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..');
const unitsDir = path.join(ROOT, 'universal_units_data/aces.vromfs.bin_u/gamedata/units/ships');
const weaponsDir = path.join(ROOT, 'universal_weapons_data/aces.vromfs.bin_u/gamedata/weapons');
const outFile = path.join(ROOT, 'data/naval_weapons.json');

const ships = JSON.parse(fs.readFileSync(path.join(ROOT, 'data/ships.json'), 'utf8'));
const byId = new Map(ships.map(s => [s.id, s]));

function blockAt(text, open) {
  let depth = 0;
  for (let i = open; i < text.length; i++) {
    if (text[i] === '{') depth++;
    else if (text[i] === '}') { depth--; if (!depth) return text.slice(open, i + 1); }
  }
  return null;
}
function named(text, name) {
  const m = new RegExp('(^|\\n)\\t?' + name + '\\s*\\{').exec(text);
  if (!m) return null;
  return blockAt(text, text.indexOf('{', m.index + m[0].length - 1));
}

// Top-level bullet containers inside a weapon blk. Many AI launchers ship a single
// unnamed `bullet { ... }` instead of a named container; those are reported with an
// empty name so the mission can keep the slot blank (engine default ammo).
function containers(weaponText) {
  const found = [];
  if (/(^|\n)bullet\s*\{/.test(weaponText)) {
    const open = weaponText.indexOf('{', weaponText.search(/(^|\n)bullet\s*\{/));
    const body = blockAt(weaponText, open) || '';
    const guided = /guidanceType:t = "([a-z]+)"/.exec(body);
    found.push({
      name: '',
      caliber: Math.round(parseFloat((body.match(/caliber:r = ([\d.]+)/) || [])[1] || '0') * 1000),
      mass: parseFloat((body.match(/\bmass:r = ([\d.]+)/) || [])[1] || '0'),
      isAam: /isAam:b = true/.test(body),
      guidance: guided ? guided[1] : '',
    });
  }
  const re = /(^|\n)([0-9][0-9a-zA-Z_]+)\s*\{/g;
  let m;
  while ((m = re.exec(weaponText))) {
    const open = weaponText.indexOf('{', m.index + m[0].length - 1);
    const body = blockAt(weaponText, open);
    if (!body || body.indexOf('bullet {') < 0) continue;
    const caliber = parseFloat((body.match(/caliber:r = ([\d.]+)/) || [])[1] || '0');
    const mass = parseFloat((body.match(/\bmass:r = ([\d.]+)/) || [])[1] || '0');
    const isAam = /isAam:b = true/.test(body);
    const guided = /guidanceType:t = "([a-z]+)"/.exec(body);
    found.push({
      name: m[2],
      caliber: caliber ? Math.round(caliber * 1000) : 0,
      mass: mass,
      isAam: isAam,
      guidance: guided ? guided[1] : '',
    });
  }
  return found;
}

const result = { generated: new Date().toISOString(), ships: {}, missiles: [] };
const missileIndex = new Map();
let groupCount = 0, containerCount = 0, missileCount = 0;

for (const file of fs.readdirSync(unitsDir)) {
  if (!file.endsWith('.blk')) continue;
  const id = file.slice(0, -4);
  let text;
  try { text = fs.readFileSync(path.join(unitsDir, file), 'utf8'); } catch { continue; }
  const cw = named(text, 'commonWeapons');
  if (!cw) continue;

  const groups = [];
  const re = /(^|\n)\tWeapon \{/g;
  let m;
  while ((m = re.exec(cw))) {
    const body = blockAt(cw, cw.indexOf('{', m.index + m[0].length - 1));
    if (!body) continue;
    const get = k => (body.match(new RegExp(k + ':t = "([^"]+)"')) || [])[1] || '';
    const blk = get('blk');
    if (!blk) continue;
    const limits = body.match(/limits \{\s*\r?\n\s*yaw:p2 = ([^\r\n]+)\r?\n\s*pitch:p2 = ([^\r\n]+)/);
    const cones = [...body.matchAll(/fireConeAngle:r = ([\d.]+)/g)].map(x => x[1]);

    const weaponPath = path.join(weaponsDir, blk.replace(/^gamedata\/weapons\//i, ''));
    let conts = [];
    let guidance = '';
    let semi = false;
    let weaponText = null;
    try { weaponText = fs.readFileSync(weaponPath, 'utf8'); } catch { /* packed-only weapon */ }
    if (weaponText) {
      conts = containers(weaponText);
      guidance = [...new Set([...weaponText.matchAll(/guidanceType:t = "([a-z]+)"/g)].map(x => x[1]))].join('/');
      semi = /semiActive:b = true/.test(weaponText);
    }

    const fixedMount = limits ? (limits[1].trim() === '0, 0' && limits[2].trim() === '0, 0') : false;
    groups.push({
      group: get('triggerGroup'),
      trigger: get('trigger'),
      blk: blk,
      guidance: guidance,
      semiActive: semi,
      fixedMount: fixedMount,
      cone: cones.length ? cones[cones.length - 1] : '',
      containers: conts.map(c => c.name),
    });
    groupCount++;
    containerCount += conts.length;

    if (guidance) {
      for (const c of conts) {
        const key = c.name || path.basename(blk, '.blk');
        let entry = missileIndex.get(key);
        if (!entry) {
          entry = {
            name: key, blk: blk, container: c.name, guidance: c.guidance || guidance,
            semiActive: semi, isAam: c.isAam, caliber: c.caliber, mass: c.mass, ships: [],
          };
          missileIndex.set(key, entry);
          missileCount++;
        }
        if (!entry.ships.includes(id)) entry.ships.push(id);
      }
    }
  }
  if (!groups.length) continue;

  const meta = byId.get(id) || {};
  result.ships[id] = {
    nation: meta.nation || '',
    rank: meta.rank || 0,
    aiOnly: (meta.nation === 'Other' || !meta.rank),
    groups: groups,
  };
}

result.missiles = [...missileIndex.values()].sort((a, b) => a.name.localeCompare(b.name));
fs.writeFileSync(outFile, JSON.stringify(result, null, 1), 'utf8');

console.log('ships  :', Object.keys(result.ships).length);
console.log('groups :', groupCount);
console.log('containers:', containerCount);
console.log('missiles  :', missileCount);
console.log('written:', outFile);

// quick sanity print for the ships the user reported
for (const id of ['cn_frigate_type054a', 'us_cruiser_rckt_california', 'cn_aircraftcarrier_fujian', 'ussr_destroyer_rckt_pr956']) {
  const s = result.ships[id];
  if (!s) continue;
  console.log('\n' + id);
  for (const g of s.groups) {
    console.log('  ' + g.group.padEnd(12) + (g.guidance || 'gun').padEnd(8) +
      (g.fixedMount ? 'FIXED ' : '      ') + (g.containers.join(',') || '(no containers)'));
  }
}
