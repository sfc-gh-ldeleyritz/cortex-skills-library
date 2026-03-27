'use strict';
// Tests must run from the repo root because brand.js uses relative paths
// anchored to the repo root (e.g. ".cortex/skills/pptx/scripts/snowflake/assets/")
const path = require('node:path');
const { spawnSync } = require('node:child_process');

const repoRoot = path.resolve(__dirname, '../../../../../../..');
const testFiles = [
  '.cortex/skills/pptx/scripts/node/SnowflakeCreativePptx/test/validator.test.js',
  '.cortex/skills/pptx/scripts/node/SnowflakeCreativePptx/test/renderer.test.js',
];

const result = spawnSync(
  process.execPath,
  ['--test', ...testFiles],
  { cwd: repoRoot, stdio: 'inherit' }
);
process.exit(result.status ?? 1);
