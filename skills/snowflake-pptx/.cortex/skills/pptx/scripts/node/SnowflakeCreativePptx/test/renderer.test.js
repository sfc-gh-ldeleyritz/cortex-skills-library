'use strict';
const { describe, it, after } = require('node:test');
const assert = require('node:assert/strict');
const path   = require('node:path');
const fs     = require('node:fs');
const os     = require('node:os');
const yaml   = require('js-yaml');
const { render } = require('../renderer.js');

const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'creative-pptx-test-'));

describe('renderer', () => {
  it('renders minimal spec to a valid PPTX file', async () => {
    const specPath = path.join(__dirname, 'fixtures/minimal.yaml');
    const spec = yaml.load(fs.readFileSync(specPath, 'utf8'));
    const outPath = path.join(tmpDir, 'minimal.pptx');
    await render(spec, specPath, outPath);
    assert(fs.existsSync(outPath), 'output file should exist');
    const stat = fs.statSync(outPath);
    assert(stat.size > 5000, `expected > 5KB, got ${stat.size}`);
    // PPTX is a ZIP -- verify magic bytes PK (0x50 0x4B)
    const buf = Buffer.alloc(2);
    const fd  = fs.openSync(outPath, 'r');
    fs.readSync(fd, buf, 0, 2, 0);
    fs.closeSync(fd);
    assert.equal(buf[0], 0x50);
    assert.equal(buf[1], 0x4B);
  });

  it('renders all-layouts spec without throwing', async () => {
    const specPath = path.join(__dirname, 'fixtures/all-layouts.yaml');
    const spec = yaml.load(fs.readFileSync(specPath, 'utf8'));
    const outPath = path.join(tmpDir, 'all-layouts.pptx');
    await render(spec, specPath, outPath);
    assert(fs.existsSync(outPath));
  });

  it('throws on unknown layout type', async () => {
    const spec = { slides: [{ layout: 'nonexistent' }] };
    await assert.rejects(
      () => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'bad.pptx')),
      /Unknown layout/
    );
  });
});

after(() => { fs.rmSync(tmpDir, { recursive: true, force: true }); });

describe('comparison layout', () => {
  it('renders comparison with two panels without throwing', async () => {
    const spec = {
      slides: [{
        layout: 'comparison', title: 'Compare',
        left:  { heading: 'Option A', bullets: ['Fast', 'Cheap'] },
        right: { heading: 'Option B', bullets: ['Reliable', 'Scalable'] },
      }],
    };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'comparison.pptx')));
  });

  it('renders comparison with accent_color applied to left panel', async () => {
    const spec = {
      slides: [{
        layout: 'comparison', title: 'Compare', accent_color: 'firstLight',
        left:  { heading: 'Left', bullets: ['a', 'b'] },
        right: { heading: 'Right', bullets: ['c', 'd'] },
      }],
    };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'comparison-accent.pptx')));
  });
});

describe('accent_color propagation', () => {
  it('renders split, full-bleed, quote, agenda, table with valenciaOrange without throwing', async () => {
    const spec = {
      slides: [
        { layout: 'split', title: 'T', image: 'background_background_blue_gradient_abstract.jpg', bullets: ['a'], accent_color: 'valenciaOrange' },
        { layout: 'full-bleed', image: 'background_background_blue_gradient_abstract.jpg', quote: 'Q', accent_color: 'valenciaOrange' },
        { layout: 'quote', quote: 'Q', accent_color: 'valenciaOrange' },
        { layout: 'agenda', title: 'T', items: ['a', 'b', 'c'], accent_color: 'valenciaOrange' },
        { layout: 'table', title: 'T', headers: ['A','B'], rows: [['1','2']], accent_color: 'valenciaOrange' },
      ],
    };
    const outPath = path.join(tmpDir, 'accent-propagation.pptx');
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', outPath));
  });
});

describe('getSvgAspect', () => {
  it('returns correct aspect ratio for a known SVG', () => {
    const { getSvgAspect } = require('../brand.js');
    const logoPath = '.cortex/skills/pptx/scripts/snowflake/assets/svg/graphic_snowflake_logo_blue.svg';
    const { w, h } = getSvgAspect(logoPath);
    assert(w > 0, 'w must be positive');
    assert(h > 0, 'h must be positive');
    // Logo is 279x62 -- wide, not square
    assert(w / h > 2, `expected aspect > 2 for logo SVG, got ${w}/${h} = ${w/h}`);
  });

  it('returns 1:1 default for a missing file', () => {
    const { getSvgAspect } = require('../brand.js');
    const { w, h } = getSvgAspect('/nonexistent/file.svg');
    assert.equal(w, 1);
    assert.equal(h, 1);
  });

  it('LOGO_H is consistent with logo SVG aspect ratio', () => {
    const { LOGO_W, LOGO_H, getSvgAspect, LOGO_PATH } = require('../brand.js');
    const { w, h } = getSvgAspect(LOGO_PATH);
    const expected = LOGO_W * (h / w);
    assert(Math.abs(LOGO_H - expected) < 0.01,
      `LOGO_H should be ${expected.toFixed(3)} based on SVG, got ${LOGO_H}`);
  });
});

describe('stat-grid centerY', () => {
  it('new centerY is less than old centerY (cards moved down toward centre)', () => {
    // Old formula: (7.5 - 2.4) / 2 + 0.3 = 2.85
    // New formula should place cards more centrally -- with subtitle, centerY ≈ 3.125
    const BAR_Y = 6.85;
    const headerBottom = 1.45;  // with subtitle
    const availableH   = BAR_Y - headerBottom;
    const centerY      = headerBottom + (availableH - 2.4) / 2 + 0.2;
    // Card top = centerY - 0.2; card bottom = centerY - 0.2 + 2.4
    const cardTop    = centerY - 0.2;
    const cardBottom = cardTop + 2.4;
    const spaceAbove = cardTop - headerBottom;
    const spaceBelow = BAR_Y - cardBottom;
    // Space above and below should be approximately equal (within 0.05")
    assert(Math.abs(spaceAbove - spaceBelow) < 0.05,
      `Expected equal margins above/below cards. Above: ${spaceAbove.toFixed(3)}, Below: ${spaceBelow.toFixed(3)}`);
  });
});

describe('agenda startY', () => {
  it('startY centres 5 items vertically with equal space above and below', () => {
    const SLIDE_H = 7.5;
    const rowH    = 0.65;
    const items5  = ['a', 'b', 'c', 'd', 'e'];
    // 5 items → useTwoCol = true → half = 3 → totalListH = 3 * 0.65 = 1.95
    const half       = Math.ceil(items5.length / 2);
    const totalListH = half * rowH;
    const startY     = Math.max(1.35, (SLIDE_H - totalListH) / 2);
    const listBottom = startY + totalListH;
    const spaceAbove = startY;
    const spaceBelow = SLIDE_H - listBottom;
    assert(Math.abs(spaceAbove - spaceBelow) < 0.05,
      `Expected equal margins. Above: ${spaceAbove.toFixed(3)}, Below: ${spaceBelow.toFixed(3)}`);
  });
});

describe('brand secondary colors', () => {
  it('exports all five accent color keys', () => {
    const { COLORS } = require('../brand.js');
    const required = ['snowflakeBlue', 'starBlue', 'valenciaOrange', 'firstLight', 'purpleMoon'];
    for (const key of required) {
      assert(key in COLORS, `COLORS.${key} is missing`);
      assert(/^[0-9A-Fa-f]{6}$/.test(COLORS[key]), `COLORS.${key} is not a 6-hex string`);
    }
  });
});

describe('accent_color resolution', () => {
  it('renders a slide with accent_color: starBlue without throwing', async () => {
    const spec = {
      slides: [{ layout: 'section', title: 'Accent Test', accent_color: 'starBlue' }],
    };
    const outPath = path.join(tmpDir, 'accent-test.pptx');
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', outPath));
  });

  it('falls back to snowflakeBlue for unknown accent_color', async () => {
    const spec = {
      slides: [{ layout: 'section', title: 'Fallback Test', accent_color: 'banana' }],
    };
    const outPath = path.join(tmpDir, 'accent-fallback.pptx');
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', outPath));
  });
});

describe('columns variants', () => {
  const baseItems = [{ heading: 'A', body: 'alpha' }, { heading: 'B', body: 'beta' }];

  it('renders columns variant=cards without throwing', async () => {
    const spec = { slides: [{ layout: 'columns', columns: 2, items: baseItems, variant: 'cards' }] };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'cols-cards.pptx')));
  });

  it('renders columns variant=numbered without throwing', async () => {
    const spec = { slides: [{ layout: 'columns', columns: 2, items: baseItems, variant: 'numbered' }] };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'cols-numbered.pptx')));
  });

  it('renders columns variant=bordered without throwing', async () => {
    const spec = { slides: [{ layout: 'columns', columns: 2, items: baseItems, variant: 'bordered' }] };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'cols-bordered.pptx')));
  });

  it('renders columns variant=bordered with subtitle without throwing', async () => {
    const items = [
      { heading: 'Col A', body: 'Body text for column A that is long enough to matter' },
      { heading: 'Col B', body: 'Body text for column B that is long enough to matter' },
      { heading: 'Col C', body: 'Body text for column C that is long enough to matter' },
    ];
    const spec = {
      slides: [{
        layout: 'columns', columns: 3, items,
        variant: 'bordered', subtitle: 'Subtitle present',
        accent_color: 'valenciaOrange',
      }],
    };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'cols-bordered-subtitle.pptx')));
  });

  it('renders columns variant=bordered without subtitle without throwing', async () => {
    const items = [
      { heading: 'Col A', body: 'Body A' },
      { heading: 'Col B', body: 'Body B' },
    ];
    const spec = {
      slides: [{ layout: 'columns', columns: 2, items, variant: 'bordered' }],
    };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'cols-bordered-no-sub.pptx')));
  });
});

describe('section variants', () => {
  it('renders section variant=diagonal without throwing', async () => {
    const spec = { slides: [{ layout: 'section', title: 'Diag', variant: 'diagonal' }] };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'section-diagonal.pptx')));
  });

  it('renders section variant=side-panel without throwing', async () => {
    const spec = { slides: [{ layout: 'section', title: 'Side', variant: 'side-panel' }] };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'section-side.pptx')));
  });

  it('renders section variant=side-panel with subtitle without throwing', async () => {
    const spec = {
      slides: [{
        layout: 'section', title: 'Chapter One',
        subtitle: 'What we cover here',
        variant: 'side-panel', accent_color: 'purpleMoon',
      }],
    };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'section-side-subtitle.pptx')));
  });

  it('renders section variant=side-panel without subtitle (decorative glyph) without throwing', async () => {
    const spec = {
      slides: [{ layout: 'section', title: 'No Subtitle', variant: 'side-panel' }],
    };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'section-side-no-sub.pptx')));
  });
});

describe('title layout headshot', () => {
  it('renders title with existing headshot file (positive path) without throwing and produces valid output', async () => {
    const headshotPath = path.join(__dirname, '../../../snowflake/assets/jpg/headshots/headshot_1.jpg');
    assert(fs.existsSync(headshotPath), `fixture headshot not found: ${headshotPath}`);
    const outPath = path.join(tmpDir, 'title-headshot-exists.pptx');
    const spec = { slides: [{ layout: 'title', title: 'Test', subtitle: 'Sub', headshot: headshotPath }] };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', outPath));
    assert(fs.existsSync(outPath), 'output PPTX should exist');
    const stat = fs.statSync(outPath);
    assert(stat.size > 5000, `expected > 5KB, got ${stat.size}`);
  });

  it('renders title with headshot field pointing to nonexistent file (negative path) without throwing', async () => {
    const outPath = path.join(tmpDir, 'title-headshot-missing.pptx');
    const spec = { slides: [{ layout: 'title', title: 'Test', subtitle: 'Sub', headshot: '/tmp/nonexistent-headshot.jpg' }] };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', outPath));
    assert(fs.existsSync(outPath), 'output PPTX should exist');
  });
});

describe('stat-grid variants', () => {
  const baseStats = [{ number: '1K', label: 'Users' }, { number: '99%', label: 'Uptime' }];

  it('renders stat-grid variant=accent-bg without throwing', async () => {
    const spec = { slides: [{ layout: 'stat-grid', title: 'T', stats: baseStats, variant: 'accent-bg' }] };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'stat-accent-bg.pptx')));
  });

  it('renders stat-grid variant=inline without throwing', async () => {
    const spec = { slides: [{ layout: 'stat-grid', title: 'T', stats: baseStats, variant: 'inline' }] };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'stat-inline.pptx')));
  });
});

describe('icon-grid layout', () => {
  it('renders icon-grid with 4 items without throwing', async () => {
    const items = [1,2,3,4].map(n => ({ heading: `Item ${n}`, body: `Body ${n}` }));
    const spec = { slides: [{ layout: 'icon-grid', title: 'Grid', items }] };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'icon-grid-4.pptx')));
  });

  it('renders icon-grid with 6 items without throwing', async () => {
    const items = [1,2,3,4,5,6].map(n => ({ heading: `Item ${n}`, body: `Body ${n}` }));
    const spec = { slides: [{ layout: 'icon-grid', title: 'Grid', items }] };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'icon-grid-6.pptx')));
  });
});

describe('timeline layout', () => {
  it('renders timeline with 3 steps without throwing', async () => {
    const spec = {
      slides: [{
        layout: 'timeline', title: 'Roadmap',
        steps: [
          { number: '1', heading: 'Discover', body: 'Research phase' },
          { number: '2', heading: 'Build', body: 'Implementation phase' },
          { number: '3', heading: 'Launch', body: 'Go to market' },
        ],
      }],
    };
    const outPath = path.join(tmpDir, 'timeline-3.pptx');
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', outPath));
    assert(fs.existsSync(outPath));
  });

  it('renders timeline with 5 steps without throwing', async () => {
    const spec = {
      slides: [{
        layout: 'timeline', title: 'Process',
        steps: [1,2,3,4,5].map(n => ({ number: String(n), heading: `Step ${n}`, body: `Details ${n}` })),
      }],
    };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'timeline-5.pptx')));
  });

  it('renders timeline with 4 steps and long body text without throwing', async () => {
    const longBody = 'This is a longer body text that exceeds fifty characters to test clamping behavior';
    const spec = {
      slides: [{
        layout: 'timeline', title: 'Long Body Test',
        steps: [1,2,3,4].map(n => ({ number: String(n), heading: `Step ${n}`, body: longBody })),
      }],
    };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'timeline-long.pptx')));
  });
});

describe('section diagonal subtitle size', () => {
  it('renders section diagonal with subtitle at larger size', async () => {
    const spec = {
      slides: [{
        layout: 'section', title: 'Big Chapter', subtitle: 'A longer subtitle that should be visible',
        variant: 'diagonal',
      }],
    };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'section-diag-subtitle.pptx')));
  });
});

describe('agenda dynamic sizing', () => {
  it('renders agenda with 3 items using dynamic sizing', async () => {
    const spec = {
      slides: [{ layout: 'agenda', title: 'Short Agenda', items: ['Item A', 'Item B', 'Item C'] }],
    };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'agenda-3.pptx')));
  });
});

describe('icon-grid without icons', () => {
  it('renders icon-grid without icons using reduced banner', async () => {
    const items = [1,2,3,4].map(n => ({ heading: `Feature ${n}`, body: `Description for feature ${n}` }));
    const spec = { slides: [{ layout: 'icon-grid', title: 'No Icons', items }] };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'icon-grid-no-icons.pptx')));
  });
});

describe('comparison with 2 bullets', () => {
  it('renders comparison with 2 bullets per side', async () => {
    const spec = {
      slides: [{
        layout: 'comparison', title: 'Sparse Compare',
        left:  { heading: 'Option A', bullets: ['Pro 1', 'Pro 2'] },
        right: { heading: 'Option B', bullets: ['Pro 1', 'Pro 2'] },
      }],
    };
    await assert.doesNotReject(() => render(spec, '/fake/spec.yaml', path.join(tmpDir, 'comparison-2.pptx')));
  });
});

// ── PPTX Output Quality Tests ────────────────────────────────────────────────
// These tests render fixture YAML files and verify the output PPTX structural
// integrity: file existence, minimum size, ZIP magic bytes, and slide count.

const { inspectPptx } = require('../../../../tests/pptx-inspect.js');

/**
 * Helper: render a fixture and run structural assertions.
 * @param {string} fixtureName - filename without extension in test/fixtures/
 * @param {number} expectedSlides - expected slide count in the output PPTX
 */
async function renderAndAssert(fixtureName, expectedSlides) {
  const specPath = path.join(__dirname, `fixtures/${fixtureName}.yaml`);
  const spec = yaml.load(fs.readFileSync(specPath, 'utf8'));
  const outPath = path.join(tmpDir, `quality-${fixtureName}.pptx`);

  await render(spec, specPath, outPath);

  // File exists
  assert(fs.existsSync(outPath), `output file should exist for ${fixtureName}`);

  // Structural inspection
  const info = inspectPptx(outPath);
  assert.equal(info.error, null, `inspect error for ${fixtureName}: ${info.error}`);
  assert(info.fileSizeBytes > 5000, `${fixtureName}: expected >5KB, got ${info.fileSizeBytes}`);
  assert.equal(info.isValidZip, true, `${fixtureName}: not a valid ZIP`);
  assert.equal(info.hasContentTypes, true, `${fixtureName}: missing [Content_Types].xml`);
  assert.equal(info.hasPresentation, true, `${fixtureName}: missing ppt/presentation.xml`);
  assert.equal(info.slideCount, expectedSlides, `${fixtureName}: expected ${expectedSlides} slides, got ${info.slideCount}`);
}

describe('output quality: existing fixtures', () => {
  it('minimal.yaml produces 6 slides with valid structure', async () => {
    await renderAndAssert('minimal', 6);
  });

  it('all-layouts.yaml produces 21 slides with valid structure', async () => {
    await renderAndAssert('all-layouts', 21);
  });
});

describe('output quality: all-variants fixture', () => {
  it('all-variants.yaml renders all section/columns/stat-grid variants (11 slides)', async () => {
    await renderAndAssert('all-variants', 11);
  });
});

describe('output quality: mood fixtures', () => {
  it('all-moods-dark.yaml renders dark premium theme (8 slides)', async () => {
    await renderAndAssert('all-moods-dark', 8);
  });

  it('all-moods-bold.yaml renders bold energetic theme (8 slides)', async () => {
    await renderAndAssert('all-moods-bold', 8);
  });
});

describe('output quality: accent colors', () => {
  it('all-accents.yaml renders all 5 accent colors (12 slides)', async () => {
    await renderAndAssert('all-accents', 12);
  });
});

describe('output quality: images and icons', () => {
  it('images-and-icons.yaml renders SVG+JPG assets (8 slides)', async () => {
    await renderAndAssert('images-and-icons', 8);
  });
});

describe('output quality: long deck', () => {
  it('long-deck.yaml renders 20-slide realistic deck', async () => {
    await renderAndAssert('long-deck', 20);
  });
});

describe('output quality: max content', () => {
  it('max-content.yaml renders fields at character limits (10 slides)', async () => {
    await renderAndAssert('max-content', 10);
  });
});
