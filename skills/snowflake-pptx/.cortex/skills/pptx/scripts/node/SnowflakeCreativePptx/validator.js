'use strict';

const KNOWN_LAYOUTS = new Set([
  'title','section','columns','stat-grid','split',
  'full-bleed','quote','agenda','table','thank-you','free',
  'timeline','icon-grid','comparison',
]);

const VALID_ACCENT_COLORS = new Set(['snowflakeBlue','starBlue','valenciaOrange','firstLight','purpleMoon']);
const ACCENT_FIXED_LAYOUTS = new Set(['title', 'thank-you']);
const VARIANT_ALLOWED = {
  columns:     new Set(['default', 'cards', 'numbered', 'bordered']),
  section:     new Set(['default', 'diagonal', 'side-panel']),
  'stat-grid': new Set(['default', 'accent-bg', 'inline']),
};

function validate(spec, specPath) {
  const errors = [], warnings = [];

  if (!spec || !Array.isArray(spec.slides)) {
    errors.push('spec.slides must be an array');
    return { ok: false, errors, warnings };
  }
  if (spec.slides.length === 0) {
    errors.push('spec.slides must not be empty');
    return { ok: false, errors, warnings };
  }

  spec.slides.forEach((slide, i) => {
    const idx = `slide[${i}]`;
    const layout = slide.layout;
    if (!layout) { errors.push(`${idx}: missing 'layout'`); return; }
    if (!KNOWN_LAYOUTS.has(layout)) { errors.push(`${idx}: unknown layout '${layout}'`); return; }

    switch (layout) {
      case 'title':
      case 'section':
      case 'thank-you':
        if (!slide.title) errors.push(`${idx} (${layout}): missing required field 'title'`);
        break;
      case 'columns': {
        const cols = slide.columns, items = slide.items;
        if (!cols || cols < 1 || cols > 4) errors.push(`${idx} (columns): 'columns' must be 1-4`);
        if (!items || !Array.isArray(items) || items.length === 0)
          errors.push(`${idx} (columns): 'items' must be a non-empty array`);
        else if (cols && items.length > cols)
          errors.push(`${idx} (columns): items count (${items.length}) exceeds columns value (${cols})`);
        else if (cols && items.length < cols)
          warnings.push(`${idx} (columns): items count (${items.length}) < columns (${cols}) -- fewer columns will render`);
        if (cols === 2 && items && Array.isArray(items) && items.length > 0 &&
            items.every(it => !it.body || it.body.length < 80)) {
          warnings.push(`${idx} (columns): all items have short body text (<80 chars) - consider a denser layout`);
        }
        break;
      }
      case 'stat-grid': {
        const stats = slide.stats;
        if (!stats || !Array.isArray(stats)) errors.push(`${idx} (stat-grid): missing 'stats'`);
        else if (stats.length < 2) errors.push(`${idx} (stat-grid): need >= 2 stats, got ${stats.length}`);
        else if (stats.length > 4) errors.push(`${idx} (stat-grid): max 4 stats, got ${stats.length}`);
        else stats.forEach((s, j) => {
          if (!s.number) errors.push(`${idx} (stat-grid): stats[${j}] missing 'number'`);
          if (!s.label)  errors.push(`${idx} (stat-grid): stats[${j}] missing 'label'`);
        });
        break;
      }
      case 'split':
        if (!slide.image) errors.push(`${idx} (split): missing 'image'`);
        if (!slide.bullets || !Array.isArray(slide.bullets) || slide.bullets.length === 0)
          errors.push(`${idx} (split): 'bullets' must be a non-empty array`);
        break;
      case 'full-bleed':
        if (!slide.image && !slide.quote) errors.push(`${idx} (full-bleed): need 'image' or 'quote'`);
        break;
      case 'quote':
        if (!slide.quote) errors.push(`${idx} (quote): missing 'quote'`);
        break;
      case 'agenda': {
        const items = slide.items;
        if (!items || !Array.isArray(items)) errors.push(`${idx} (agenda): missing 'items'`);
        else if (items.length < 2) errors.push(`${idx} (agenda): need >= 2 items`);
        else if (items.length > 6) errors.push(`${idx} (agenda): max 6 items`);
        else if (items.length <= 3) warnings.push(`${idx} (agenda): only ${items.length} items - consider adding more for visual balance`);
        break;
      }
      case 'table':
        if (!slide.headers || !Array.isArray(slide.headers) || slide.headers.length === 0)
          errors.push(`${idx} (table): missing 'headers'`);
        if (!slide.rows || !Array.isArray(slide.rows) || slide.rows.length === 0)
          errors.push(`${idx} (table): 'rows' must be non-empty`);
        break;
      case 'free':
        if (!slide.elements || !Array.isArray(slide.elements) || slide.elements.length === 0)
          errors.push(`${idx} (free): 'elements' must be non-empty`);
        break;
      case 'timeline': {
        const steps = slide.steps || [];
        if (steps.length < 3 || steps.length > 5) {
          errors.push(`${idx} (timeline): requires 3-5 steps, got ${steps.length}`);
        }
        steps.forEach((step, si) => {
          if (!step.heading) errors.push(`${idx} (timeline): step ${si+1} is missing 'heading'`);
          if (step.body && step.body.length > 50) {
            warnings.push(`${idx} (timeline): step ${si+1} body is ${step.body.length} chars (>50) - may truncate`);
          }
        });
        break;
      }
      case 'icon-grid': {
        const items = slide.items || [];
        if (items.length !== 4 && items.length !== 6) {
          errors.push(`${idx} (icon-grid): items must be exactly 4 or 6 (got ${items.length})`);
        }
        items.forEach((item, j) => {
          if (!item.icon) {
            warnings.push(`${idx} (icon-grid): item ${j} has no icon - banner will be compact`);
          }
        });
        break;
      }
      case 'comparison': {
        if (!slide.left)  errors.push(`${idx} (comparison): requires 'left' panel`);
        if (!slide.right) errors.push(`${idx} (comparison): requires 'right' panel`);
        const leftBullets  = slide.left  && slide.left.bullets  ? slide.left.bullets.length  : 0;
        const rightBullets = slide.right && slide.right.bullets ? slide.right.bullets.length : 0;
        for (const side of ['left', 'right']) {
          const panel = slide[side];
          if (panel && panel.bullets) {
            if (panel.bullets.length > 5) {
              errors.push(`${idx} (comparison): ${side} panel has ${panel.bullets.length} bullets (max 5)`);
            } else if (panel.bullets.length > 3) {
              warnings.push(`${idx} (comparison): ${side} panel has ${panel.bullets.length} bullets - consider <=3 for readability`);
            }
          }
        }
        if (leftBullets <= 2 && rightBullets <= 2 && leftBullets > 0 && rightBullets > 0) {
          warnings.push(`${idx} (comparison): both panels have <=2 bullets - layout may look sparse`);
        }
        break;
      }
    }

    // accent_color validation (applies to all layouts)
    if (slide.accent_color !== undefined) {
      if (ACCENT_FIXED_LAYOUTS.has(slide.layout)) {
        warnings.push(`${idx} (${slide.layout}): accent_color is ignored`);
      } else if (!VALID_ACCENT_COLORS.has(slide.accent_color)) {
        errors.push(`${idx} (${slide.layout}): invalid accent_color '${slide.accent_color}'. Must be one of: ${[...VALID_ACCENT_COLORS].join(', ')}`);
      }
    }

    // variant validation
    if (slide.variant !== undefined && VARIANT_ALLOWED[slide.layout]) {
      if (!VARIANT_ALLOWED[slide.layout].has(slide.variant)) {
        warnings.push(`${idx} (${slide.layout}): unknown variant '${slide.variant}'`);
      }
    }
  });

  return { ok: errors.length === 0, errors, warnings };
}

module.exports = { validate };
