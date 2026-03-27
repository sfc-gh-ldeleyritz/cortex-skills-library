'use strict';
const path      = require('node:path');
const PptxGenJS = require('pptxgenjs');

const { COLORS }          = require('./brand.js');
const { renderTitle }     = require('./layouts/title.js');
const { renderSection }   = require('./layouts/section.js');
const { renderColumns }   = require('./layouts/columns.js');
const { renderStatGrid }  = require('./layouts/stat-grid.js');
const { renderSplit }     = require('./layouts/split.js');
const { renderFullBleed } = require('./layouts/full-bleed.js');
const { renderQuote }     = require('./layouts/quote.js');
const { renderAgenda }    = require('./layouts/agenda.js');
const { renderTable }     = require('./layouts/table.js');
const { renderThankYou }  = require('./layouts/thank-you.js');
const { renderFree }      = require('./layouts/free.js');
const { renderTimeline }  = require('./layouts/timeline.js');
const { renderIconGrid }  = require('./layouts/icon-grid.js');
const { renderComparison }= require('./layouts/comparison.js');

const VALID_ACCENT_COLORS = new Set([
  'snowflakeBlue', 'starBlue', 'valenciaOrange', 'firstLight', 'purpleMoon',
]);

// Layouts where accent_color is ignored (always snowflakeBlue)
const ACCENT_FIXED_LAYOUTS = new Set(['title', 'thank-you']);

const HANDLERS = {
  'title':      renderTitle,
  'section':    renderSection,
  'columns':    renderColumns,
  'stat-grid':  renderStatGrid,
  'split':      renderSplit,
  'full-bleed': renderFullBleed,
  'quote':      renderQuote,
  'agenda':     renderAgenda,
  'table':      renderTable,
  'thank-you':  renderThankYou,
  'free':       renderFree,
  'timeline':   renderTimeline,
  'icon-grid':  renderIconGrid,
  'comparison': renderComparison,
};

async function render(spec, specPath, outPath) {
  const prs     = new PptxGenJS();
  const specDir = path.dirname(path.resolve(specPath));
  prs.layout    = 'LAYOUT_WIDE';

  for (const slideSpec of (spec.slides || [])) {
    const handler = HANDLERS[slideSpec.layout];
    if (!handler) throw new Error(`Unknown layout: '${slideSpec.layout}'`);
    const prsSlide = prs.addSlide();
    if (slideSpec.notes) prsSlide.addNotes(slideSpec.notes); // NOTE: hyperlinks in notes are not supported by pptxgenjs; URLs render as plain text

    const accentKey = !ACCENT_FIXED_LAYOUTS.has(slideSpec.layout)
      && slideSpec.accent_color
      && VALID_ACCENT_COLORS.has(slideSpec.accent_color)
      ? slideSpec.accent_color : null;
    const resolvedAccent = accentKey ? COLORS[accentKey] : COLORS.snowflakeBlue;

    handler(prs, slideSpec, { slide: prsSlide, accent: resolvedAccent }, specDir);
  }

  await prs.writeFile({ fileName: outPath });
}

module.exports = { render };
