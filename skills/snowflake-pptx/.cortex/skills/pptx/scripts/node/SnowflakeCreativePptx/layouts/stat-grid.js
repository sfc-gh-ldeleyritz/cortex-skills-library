'use strict';
const { COLORS, FONTS, LOGO_PATH, LOGO_X, LOGO_Y, LOGO_W, LOGO_H, SLIDE_W, BAR_Y } = require('../brand.js');

function renderStatGrid(prs, slide, opts) {
  const accent = opts.accent;
  const navy   = COLORS.navy;
  const variant = slide.variant || 'default';

  opts.slide.background = { color: COLORS.white };
  opts.slide.addShape('rect', { x: 0, y: 0, w: SLIDE_W, h: 0.15, fill: { color: accent }, line: { color: accent } });
  opts.slide.addImage({ path: LOGO_PATH, x: LOGO_X, y: LOGO_Y, w: LOGO_W, h: LOGO_H });
  if (slide.title) opts.slide.addText(slide.title, { x: 0.5, y: 0.25, w: LOGO_X - 0.75, h: 0.65, fontFace: FONTS.heading, fontSize: 24, bold: true, color: navy });
  if (slide.subtitle) opts.slide.addText(slide.subtitle, { x: 0.5, y: 0.9, w: SLIDE_W - 1.0, h: 0.4, fontFace: FONTS.body, fontSize: 14, color: COLORS.darkGray });

  const stats  = slide.stats || [];
  const count  = Math.min(stats.length, 4);
  const margin = 0.5;
  const gap    = 0.3;
  const cellW  = (SLIDE_W - margin * 2 - gap * (count - 1)) / count;

  // Vertically centre the 2.4"-tall card block in the available area between header and bottom bar.
  // Note: card background is drawn at y: centerY - 0.2 (the existing addShape offset is preserved).
  const headerBottom = slide.subtitle ? 1.45 : 1.1;
  const availableH   = BAR_Y - headerBottom;
  const centerY      = headerBottom + (availableH - 2.4) / 2 + 0.2;

  stats.slice(0, count).forEach((stat, i) => {
    const x = margin + i * (cellW + gap);

    if (variant === 'accent-bg') {
      opts.slide.addShape('rect', { x, y: centerY - 0.2, w: cellW, h: 2.4, fill: { color: accent }, line: { color: accent } });
      opts.slide.addText(stat.number || '', { x, y: centerY, w: cellW, h: 1.4, fontFace: FONTS.heading, fontSize: 44, bold: true, color: COLORS.white, align: 'center', valign: 'middle', shrinkText: true });
      opts.slide.addText(stat.label || '', { x, y: centerY + 1.45, w: cellW, h: 0.55, fontFace: FONTS.body, fontSize: 16, color: COLORS.white, align: 'center', valign: 'top' });
    } else if (variant === 'inline') {
      if (i > 0) {
        const dividerH = 2.4 * 0.6;
        const dividerY = centerY + (2.4 - dividerH) / 2;
        opts.slide.addShape('rect', {
          x: x - gap / 2 - 0.01, y: dividerY, w: 0.02, h: dividerH,
          fill: { color: COLORS.lightGray }, line: { color: COLORS.lightGray },
        });
      }
      opts.slide.addText(stat.number || '', { x, y: centerY, w: cellW, h: 1.4, fontFace: FONTS.heading, fontSize: 44, bold: true, color: accent, align: 'center', valign: 'middle', shrinkText: true });
      opts.slide.addText(stat.label || '', { x, y: centerY + 1.45, w: cellW, h: 0.55, fontFace: FONTS.body, fontSize: 16, color: navy, align: 'center', valign: 'top' });
    } else {
      // default
      opts.slide.addShape('rect', { x, y: centerY - 0.2, w: cellW, h: 2.4, fill: { color: COLORS.lightGray }, line: { color: COLORS.lightGray } });
      opts.slide.addText(stat.number || '', { x, y: centerY, w: cellW, h: 1.4, fontFace: FONTS.heading, fontSize: 44, bold: true, color: accent, align: 'center', valign: 'middle', shrinkText: true });
      opts.slide.addText(stat.label || '', { x, y: centerY + 1.45, w: cellW, h: 0.55, fontFace: FONTS.body, fontSize: 16, color: navy, align: 'center', valign: 'top' });
    }
  });
}

module.exports = { renderStatGrid };
