'use strict';
const fs = require('node:fs');
const { COLORS, FONTS, LOGO_PATH, LOGO_X, LOGO_Y, LOGO_W, LOGO_H, SLIDE_W, SLIDE_H, BAR_H, BAR_Y } = require('../brand.js');

function renderTitle(prs, slide, opts) {
  const isDark = (slide.background || 'dark') !== 'light';
  const bg     = isDark ? COLORS.navy : COLORS.white;
  const fg     = isDark ? COLORS.white : COLORS.navy;
  const accent = COLORS.snowflakeBlue;

  opts.slide.background = { color: bg };

  // Left accent bar — light backgrounds only (navy bg makes it nearly invisible)
  if (!isDark) {
    opts.slide.addShape('rect', {
      x: 0, y: 0, w: 0.2, h: SLIDE_H,
      fill: { color: accent }, line: { color: accent },
    });
  }

  opts.slide.addImage({ path: LOGO_PATH, x: LOGO_X, y: LOGO_Y, w: LOGO_W, h: LOGO_H });
  opts.slide.addText(slide.title || '', {
    x: 0.6, y: 2.5, w: SLIDE_W - 0.85, h: 1.3,
    fontFace: FONTS.heading, fontSize: 44, bold: true, color: fg, valign: 'middle',
  });
  if (slide.subtitle) {
    opts.slide.addText(slide.subtitle, {
      x: 0.6, y: 4.0, w: SLIDE_W - 0.85, h: 0.7,
      fontFace: FONTS.body, fontSize: 18, color: accent,
    });
  }
  if (slide.headshot && fs.existsSync(slide.headshot)) {
    opts.slide.addImage({
      path: slide.headshot,
      x: 0.6, y: 5.0, w: 1.2, h: 1.2,
      rounding: true,
    });
  } else if (slide.headshot) {
    console.warn(`title: headshot not found '${slide.headshot}' -- skipped`);
  }
  opts.slide.addShape('rect', {
    x: 0, y: BAR_Y, w: SLIDE_W, h: BAR_H,
    fill: { color: accent }, line: { color: accent },
  });
}

module.exports = { renderTitle };
