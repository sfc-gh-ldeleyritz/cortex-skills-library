'use strict';
const { COLORS, FONTS, LOGO_PATH, LOGO_W, LOGO_H, SLIDE_W, SLIDE_H, BAR_H, BAR_Y } = require('../brand.js');

function renderThankYou(prs, slide, opts) {
  const accent = COLORS.snowflakeBlue;
  opts.slide.background = { color: COLORS.navy };
  // Centered logo
  opts.slide.addImage({ path: LOGO_PATH, x: (SLIDE_W - LOGO_W * 2) / 2, y: 1.0, w: LOGO_W * 2, h: LOGO_H * 2 });
  opts.slide.addText(slide.title || 'Thank You', {
    x: 1.0, y: 2.6, w: SLIDE_W - 2.0, h: 1.4,
    fontFace: FONTS.heading, fontSize: 48, bold: true,
    color: COLORS.white, align: 'center', valign: 'middle',
  });
  if (slide.subtitle) {
    opts.slide.addText(slide.subtitle, {
      x: 1.0, y: 4.2, w: SLIDE_W - 2.0, h: 0.7,
      fontFace: FONTS.body, fontSize: 18, color: accent, align: 'center',
    });
  }
  opts.slide.addShape('rect', { x: 0, y: BAR_Y, w: SLIDE_W, h: BAR_H, fill: { color: accent }, line: { color: accent } });
}

module.exports = { renderThankYou };
