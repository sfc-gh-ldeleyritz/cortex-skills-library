'use strict';
const { describe, it } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const yaml = require('js-yaml');
const fs = require('node:fs');
const { validate } = require('../validator.js');

describe('validate', () => {
  it('returns ok for a minimal valid spec', () => {
    const specPath = path.join(__dirname, 'fixtures/minimal.yaml');
    const spec = yaml.load(fs.readFileSync(specPath, 'utf8'));
    const result = validate(spec, specPath);
    assert.equal(result.ok, true);
    assert.deepEqual(result.errors, []);
  });

  it('errors when slides array is missing', () => {
    const result = validate({}, '/fake/spec.yaml');
    assert.equal(result.ok, false);
    assert(result.errors.some(e => e.includes('slides')));
  });

  it('errors when slides array is empty', () => {
    const result = validate({ slides: [] }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('errors on unknown layout type', () => {
    const result = validate({ slides: [{ layout: 'banana' }] }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
    assert(result.errors.some(e => e.includes('banana')));
  });

  it('errors when title layout is missing title field', () => {
    const result = validate({ slides: [{ layout: 'title' }] }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('errors when columns items count exceeds columns value', () => {
    const result = validate({
      slides: [{ layout: 'columns', columns: 2,
        items: [{ heading: 'A', body: 'a' }, { heading: 'B', body: 'b' }, { heading: 'C', body: 'c' }] }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
    assert(result.errors.some(e => e.includes('items')));
  });

  it('warns (not errors) when columns items count is below columns value', () => {
    const result = validate({
      slides: [{ layout: 'columns', columns: 3,
        items: [{ heading: 'A', body: 'a' }, { heading: 'B', body: 'b' }] }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
    assert(result.warnings.some(w => w.includes('items')));
  });

  it('errors when stat-grid has more than 4 stats', () => {
    const result = validate({
      slides: [{ layout: 'stat-grid', title: 'S',
        stats: [{ number: '1', label: 'a' }, { number: '2', label: 'b' },
                { number: '3', label: 'c' }, { number: '4', label: 'd' }, { number: '5', label: 'e' }] }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('errors when stat-grid has fewer than 2 stats', () => {
    const result = validate({
      slides: [{ layout: 'stat-grid', title: 'S', stats: [{ number: '1', label: 'a' }] }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('errors when full-bleed has neither image nor quote', () => {
    const result = validate({ slides: [{ layout: 'full-bleed' }] }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('accepts full-bleed with only a quote', () => {
    const result = validate({ slides: [{ layout: 'full-bleed', quote: 'A quote.' }] }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
  });

  it('errors when agenda has fewer than 2 items', () => {
    const result = validate({ slides: [{ layout: 'agenda', title: 'A', items: ['One'] }] }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('errors when agenda has more than 6 items', () => {
    const result = validate({
      slides: [{ layout: 'agenda', title: 'A', items: ['a','b','c','d','e','f','g'] }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('errors when free layout has no elements', () => {
    const result = validate({ slides: [{ layout: 'free', elements: [] }] }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('errors when a stat item is missing number', () => {
    const result = validate({
      slides: [{ layout: 'stat-grid', title: 'S', stats: [{ label: 'a' }, { number: '2', label: 'b' }] }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('errors when a stat item is missing label', () => {
    const result = validate({
      slides: [{ layout: 'stat-grid', title: 'S', stats: [{ number: '1' }, { number: '2', label: 'b' }] }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('errors when columns has zero items (items must be non-empty)', () => {
    const result = validate({
      slides: [{ layout: 'columns', columns: 1, items: [] }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('always returns { ok, errors, warnings } shape', () => {
    const result = validate({ slides: [{ layout: 'title', title: 'T' }] }, '/fake/spec.yaml');
    assert('ok' in result);
    assert(Array.isArray(result.errors));
    assert(Array.isArray(result.warnings));
  });

  // --- accent_color validation ---
  it('errors when accent_color has an invalid value', () => {
    const result = validate({ slides: [{ layout: 'section', title: 'T', accent_color: 'hotpink' }] }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
    assert(result.errors.some(e => e.includes('accent_color')));
  });

  it('accepts all valid accent_color values', () => {
    for (const color of ['snowflakeBlue', 'starBlue', 'valenciaOrange', 'firstLight', 'purpleMoon']) {
      const result = validate({ slides: [{ layout: 'section', title: 'T', accent_color: color }] }, '/fake/spec.yaml');
      assert.equal(result.ok, true, `Expected ok for accent_color: ${color}`);
    }
  });

  it('warns when accent_color is set on title layout', () => {
    const result = validate({ slides: [{ layout: 'title', title: 'T', accent_color: 'starBlue' }] }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
    assert(result.warnings.some(w => w.includes('accent_color')));
  });

  it('warns when accent_color is set on thank-you layout', () => {
    const result = validate({ slides: [{ layout: 'thank-you', title: 'T', accent_color: 'starBlue' }] }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
    assert(result.warnings.some(w => w.includes('accent_color')));
  });

  // --- variant validation ---
  it('warns when columns has unknown variant', () => {
    const result = validate({
      slides: [{ layout: 'columns', columns: 2, items: [{ heading: 'A', body: 'a' }, { heading: 'B', body: 'b' }], variant: 'flibbertigibbet' }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
    assert(result.warnings.some(w => w.includes('variant')));
  });

  it('accepts valid columns variants', () => {
    for (const variant of ['default', 'cards', 'numbered', 'bordered']) {
      const result = validate({
        slides: [{ layout: 'columns', columns: 2, items: [{ heading: 'A', body: 'a' }, { heading: 'B', body: 'b' }], variant }],
      }, '/fake/spec.yaml');
      assert.equal(result.ok, true, `Expected ok for columns variant: ${variant}`);
    }
  });

  it('accepts valid section variants', () => {
    for (const variant of ['default', 'diagonal', 'side-panel']) {
      const result = validate({ slides: [{ layout: 'section', title: 'T', variant }] }, '/fake/spec.yaml');
      assert.equal(result.ok, true, `Expected ok for section variant: ${variant}`);
    }
  });

  it('accepts valid stat-grid variants', () => {
    for (const variant of ['default', 'accent-bg', 'inline']) {
      const result = validate({
        slides: [{ layout: 'stat-grid', title: 'S', stats: [{ number: '1', label: 'a' }, { number: '2', label: 'b' }], variant }],
      }, '/fake/spec.yaml');
      assert.equal(result.ok, true, `Expected ok for stat-grid variant: ${variant}`);
    }
  });

  // --- new layout types ---
  it('accepts timeline layout with 3-5 steps', () => {
    const result = validate({
      slides: [{
        layout: 'timeline', title: 'T',
        steps: [
          { number: '1', heading: 'Step 1', body: 'text' },
          { number: '2', heading: 'Step 2', body: 'text' },
          { number: '3', heading: 'Step 3', body: 'text' },
        ],
      }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
  });

  it('errors when timeline has fewer than 3 steps', () => {
    const result = validate({
      slides: [{ layout: 'timeline', title: 'T', steps: [{ number: '1', heading: 'H', body: 'b' }, { number: '2', heading: 'H2', body: 'b2' }] }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
    assert(result.errors.some(e => e.includes('steps')));
  });

  it('errors when timeline has more than 5 steps', () => {
    const steps = [1,2,3,4,5,6].map(n => ({ number: String(n), heading: `H${n}`, body: 'b' }));
    const result = validate({ slides: [{ layout: 'timeline', title: 'T', steps }] }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('errors when timeline step is missing heading', () => {
    const result = validate({
      slides: [{ layout: 'timeline', title: 'T', steps: [{ number: '1', body: 'b' }, { number: '2', heading: 'H', body: 'b' }, { number: '3', heading: 'H', body: 'b' }] }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('accepts icon-grid with 4 items', () => {
    const items = [1,2,3,4].map(n => ({ heading: `H${n}`, body: `b${n}` }));
    const result = validate({ slides: [{ layout: 'icon-grid', title: 'T', items }] }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
  });

  it('accepts icon-grid with 6 items', () => {
    const items = [1,2,3,4,5,6].map(n => ({ heading: `H${n}`, body: `b${n}` }));
    const result = validate({ slides: [{ layout: 'icon-grid', title: 'T', items }] }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
  });

  it('errors when icon-grid has 5 items', () => {
    const items = [1,2,3,4,5].map(n => ({ heading: `H${n}`, body: `b${n}` }));
    const result = validate({ slides: [{ layout: 'icon-grid', title: 'T', items }] }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
    assert(result.errors.some(e => e.includes('items')));
  });

  it('errors when icon-grid has fewer than 4 items', () => {
    const items = [1,2,3].map(n => ({ heading: `H${n}`, body: `b${n}` }));
    const result = validate({ slides: [{ layout: 'icon-grid', title: 'T', items }] }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('accepts comparison layout with 2 panels', () => {
    const result = validate({
      slides: [{
        layout: 'comparison', title: 'T',
        left: { heading: 'Left', bullets: ['a', 'b'] },
        right: { heading: 'Right', bullets: ['c', 'd'] },
      }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
  });

  it('errors when comparison is missing left panel', () => {
    const result = validate({
      slides: [{ layout: 'comparison', title: 'T', right: { heading: 'R', bullets: ['a'] } }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('errors when comparison is missing right panel', () => {
    const result = validate({
      slides: [{ layout: 'comparison', title: 'T', left: { heading: 'L', bullets: ['a'] } }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
  });

  it('errors when comparison panel has more than 5 bullets', () => {
    const result = validate({
      slides: [{
        layout: 'comparison', title: 'T',
        left: { heading: 'L', bullets: ['a','b','c','d','e','f'] },
        right: { heading: 'R', bullets: ['a'] },
      }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, false);
    assert(result.errors.some(e => e.includes('bullets')));
  });

  it('warns when comparison panel has more than 3 bullets', () => {
    const result = validate({
      slides: [{
        layout: 'comparison', title: 'T',
        left: { heading: 'L', bullets: ['a','b','c','d'] },
        right: { heading: 'R', bullets: ['a'] },
      }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
    assert(result.warnings.some(w => w.includes('bullets')));
  });

  // --- content-density warnings ---
  it('warns when timeline step body exceeds 50 chars', () => {
    const longBody = 'A'.repeat(55);
    const result = validate({
      slides: [{
        layout: 'timeline', title: 'T',
        steps: [
          { number: '1', heading: 'H1', body: longBody },
          { number: '2', heading: 'H2', body: 'short' },
          { number: '3', heading: 'H3', body: 'short' },
        ],
      }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
    assert(result.warnings.some(w => w.includes('step 1') && w.includes('55 chars')));
  });

  it('warns when icon-grid item has no icon', () => {
    const items = [1,2,3,4].map(n => ({ heading: `H${n}`, body: `b${n}` }));
    const result = validate({ slides: [{ layout: 'icon-grid', title: 'T', items }] }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
    assert(result.warnings.some(w => w.includes('no icon')));
  });

  it('warns when comparison both panels have <=2 bullets', () => {
    const result = validate({
      slides: [{
        layout: 'comparison', title: 'T',
        left: { heading: 'L', bullets: ['a', 'b'] },
        right: { heading: 'R', bullets: ['c'] },
      }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
    assert(result.warnings.some(w => w.includes('sparse')));
  });

  it('warns when columns:2 all items have short body text', () => {
    const result = validate({
      slides: [{
        layout: 'columns', columns: 2,
        items: [{ heading: 'A', body: 'Short' }, { heading: 'B', body: 'Also short' }],
      }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
    assert(result.warnings.some(w => w.includes('short body')));
  });

  it('warns when agenda has <=3 items', () => {
    const result = validate({
      slides: [{ layout: 'agenda', title: 'A', items: ['One', 'Two', 'Three'] }],
    }, '/fake/spec.yaml');
    assert.equal(result.ok, true);
    assert(result.warnings.some(w => w.includes('visual balance')));
  });
});
