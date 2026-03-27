'use strict';
const { COLORS, FONTS, LOGO_PATH, LOGO_X, LOGO_Y, LOGO_W, LOGO_H, SLIDE_W, SLIDE_H, BAR_H, BAR_Y } = require('../brand.js');

function renderQuote(prs, slide, opts) {
  const isDark = (slide.background || 'dark') !== 'light';
  const bg     = isDark ? COLORS.navy : COLORS.white;
  const fg     = isDark ? COLORS.white : COLORS.navy;
  const accent = opts.accent;

  opts.slide.background = { color: bg };
  opts.slide.addImage({ path: LOGO_PATH, x: LOGO_X, y: LOGO_Y, w: LOGO_W, h: LOGO_H });
  opts.slide.addText('\u201C', { x: 0.5, y: 0.5, w: 2.0, h: 1.5, fontFace: FONTS.heading, fontSize: 100, color: accent, bold: true });
  opts.slide.addText(slide.quote || '', {
    x: 0.75, y: 1.5, w: SLIDE_W - 1.5, h: 2.8,
    fontFace: FONTS.body, fontSize: 24, color: fg, italic: true, valign: 'middle', wrap: true,
  });
  if (slide.attribution) {
    opts.slide.addText(`\u2014 ${slide.attribution}`, {
      x: 0.75, y: 4.5, w: SLIDE_W - 1.5, h: 0.5,
      fontFace: FONTS.body, fontSize: 14, color: accent,
    });
  }
  opts.slide.addShape('rect', { x: 0, y: BAR_Y, w: SLIDE_W, h: BAR_H, fill: { color: accent }, line: { color: accent } });
}

module.exports = { renderQuote };
