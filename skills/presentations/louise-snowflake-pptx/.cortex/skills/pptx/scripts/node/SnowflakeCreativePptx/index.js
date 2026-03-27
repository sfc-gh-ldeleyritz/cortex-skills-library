#!/usr/bin/env node
'use strict';
const path = require('node:path');
const fs   = require('node:fs');
const yaml = require('js-yaml');
const { validate } = require('./validator.js');
const { render }   = require('./renderer.js');

const [,, command, ...args] = process.argv;

function parseArgs(args) {
  const r = {};
  for (let i = 0; i < args.length; i++) {
    if (args[i].startsWith('--')) { r[args[i].slice(2)] = args[i + 1]; i++; }
  }
  return r;
}

function loadSpec(specArg) {
  if (!specArg) { console.error('Missing spec argument'); process.exit(1); }
  const specPath = path.resolve(specArg);
  if (!fs.existsSync(specPath)) { console.error(`Spec file not found: ${specPath}`); process.exit(1); }
  try {
    return { spec: yaml.load(fs.readFileSync(specPath, 'utf8')), specPath };
  } catch (e) { console.error(`Failed to parse YAML: ${e.message}`); process.exit(1); }
}

async function cmdBuild(args) {
  const { spec: specArg, out: outArg } = parseArgs(args);
  if (!outArg) { console.error('Missing --out argument'); process.exit(1); }
  const { spec, specPath } = loadSpec(specArg);
  const outPath = path.resolve(outArg);

  const { ok, errors, warnings } = validate(spec, specPath);
  warnings.forEach(w => console.warn(`  WARN  ${w}`));
  if (!ok) { console.error('Spec validation failed:'); errors.forEach(e => console.error(`  ERROR ${e}`)); process.exit(1); }

  try {
    await render(spec, specPath, outPath);
    console.log(`OK Built: ${outPath}`);
  } catch (e) { console.error(`Render failed: ${e.message}`); process.exit(1); }
}

function cmdValidate(args) {
  // validate takes a positional arg: `node index.js validate spec.yaml`
  // args[0] is the spec path (process.argv[3])
  const [specArg] = args;
  const { spec, specPath } = loadSpec(specArg);
  const { ok, errors, warnings } = validate(spec, specPath);
  warnings.forEach(w => console.warn(`  WARN  ${w}`));
  if (ok) { console.log(`OK Spec is valid (${spec.slides.length} slides)`); process.exit(0); }
  else { console.error('Spec validation failed:'); errors.forEach(e => console.error(`  ERROR ${e}`)); process.exit(1); }
}

(async () => {
  switch (command) {
    case 'build':    await cmdBuild(args); break;
    case 'validate': cmdValidate(args);    break;
    default: console.error('Usage: node index.js <build|validate> [options]'); process.exit(1);
  }
})();
