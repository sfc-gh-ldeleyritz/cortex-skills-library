'use strict';
const { COLORS, FONTS, LOGO_PATH, LOGO_X, LOGO_Y, LOGO_W, LOGO_H, SLIDE_W, SLIDE_H } = require('../brand.js');

function renderSection(prs, slide, opts) {
  const accent = opts.accent;
  const variant = slide.variant || 'default';

  if (variant === 'diagonal') {
    opts.slide.background = { color: COLORS.white };
    opts.slide.addShape('rect', { x: 0, y: 0, w: 5.5, h: SLIDE_H, fill: { color: COLORS.navy }, line: { color: COLORS.navy } });
    opts.slide.addShape('rect', {
      x: 4.5, y: -0.5, w: 10, h: SLIDE_H + 1,
      fill: { color: COLORS.white }, line: { color: COLORS.white },
      rotate: 8,
    });
    opts.slide.addImage({ path: LOGO_PATH, x: LOGO_X, y: LOGO_Y, w: LOGO_W, h: LOGO_H });
    opts.slide.addText(slide.title || '', {
      x: 0.6, y: 2.8, w: 4.0, h: 1.2,
      fontFace: FONTS.heading, fontSize: 32, bold: true, color: COLORS.white, valign: 'middle',
    });
    if (slide.subtitle) {
      opts.slide.addText(slide.subtitle, {
        x: 5.8, y: 2.5, w: SLIDE_W - 6.3, h: 1.5,
        fontFace: FONTS.body, fontSize: 22, color: COLORS.navy, valign: 'middle',
      });
    }
    return;
  }

  if (variant === 'side-panel') {
    opts.slide.background = { color: COLORS.white };
    opts.slide.addShape('rect', { x: 0, y: 0, w: 4, h: SLIDE_H, fill: { color: accent }, line: { color: accent } });
    opts.slide.addImage({ path: LOGO_PATH, x: LOGO_X, y: LOGO_Y, w: LOGO_W, h: LOGO_H });
    opts.slide.addText(slide.title || '', {
      x: 0.4, y: 2.5, w: 3.3, h: 1.4,
      fontFace: FONTS.heading, fontSize: 28, bold: true, color: COLORS.white, valign: 'middle',
    });
    if (slide.subtitle) {
      opts.slide.addShape('rect', {
        x: 4.4, y: 3.0, w: SLIDE_W - 4.9, h: 0.04,
        fill: { color: accent || COLORS.snowflakeBlue },
        line: { color: accent || COLORS.snowflakeBlue },
      });
      opts.slide.addText(slide.subtitle, {
        x: 4.4, y: 3.2, w: SLIDE_W - 4.9, h: 1.2,
        fontFace: FONTS.body, fontSize: 20, color: COLORS.darkGray, valign: 'middle',
      });
    } else {
      opts.slide.addText('§', {
        x: 8.0, y: 1.5, w: 3.0, h: 2.0,
        fontFace: FONTS.heading, fontSize: 120, bold: true, color: COLORS.lightGray, align: 'center', valign: 'middle',
      });
    }
    return;
  }

  // default variant
  const isDark = (slide.background || 'dark') !== 'light';
  const bg = isDark ? COLORS.navy : COLORS.white;
  const fg = isDark ? COLORS.white : COLORS.navy;

  opts.slide.background = { color: bg };
  opts.slide.addShape('rect', { x: 0, y: 0, w: 0.2, h: SLIDE_H, fill: { color: accent }, line: { color: accent } });
  opts.slide.addImage({ path: LOGO_PATH, x: LOGO_X, y: LOGO_Y, w: LOGO_W, h: LOGO_H });
  opts.slide.addText(slide.title || '', {
    x: 0.6, y: 2.5, w: SLIDE_W - 1.2, h: 1.4,
    fontFace: FONTS.heading, fontSize: 36, bold: true, color: fg, valign: 'middle',
  });
  if (slide.subtitle) {
    opts.slide.addText(slide.subtitle, {
      x: 0.6, y: 4.0, w: SLIDE_W - 1.2, h: 0.7,
      fontFace: FONTS.body, fontSize: 16, color: accent,
    });
  }
}

module.exports = { renderSection };
