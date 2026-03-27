'use strict';
const { COLORS, FONTS, LOGO_PATH, LOGO_X, LOGO_Y, LOGO_W, LOGO_H, SLIDE_W, BAR_Y } = require('../brand.js');

function renderTimeline(prs, slide, opts) {
  const accent = opts.accent;
  const navy   = COLORS.navy;

  opts.slide.background = { color: COLORS.white };
  opts.slide.addShape('rect', { x: 0, y: 0, w: SLIDE_W, h: 0.15, fill: { color: accent }, line: { color: accent } });
  opts.slide.addImage({ path: LOGO_PATH, x: LOGO_X, y: LOGO_Y, w: LOGO_W, h: LOGO_H });
  if (slide.title) {
    opts.slide.addText(slide.title, {
      x: 0.5, y: 0.25, w: LOGO_X - 0.75, h: 0.65,
      fontFace: FONTS.heading, fontSize: 24, bold: true, color: navy,
    });
  }

  const steps   = slide.steps || [];
  const n       = steps.length;
  if (n === 0) return;

  const marginX  = 1.2;
  const lineY    = 3.8;
  const spread   = SLIDE_W - 2 * marginX;

  // Horizontal connector line
  opts.slide.addShape('rect', {
    x: marginX, y: lineY - 0.025, w: spread, h: 0.05,
    fill: { color: COLORS.lightGray }, line: { color: COLORS.lightGray },
  });

  const colW = Math.min(2.5, spread / n * 0.85);
  const r    = 0.22;

  steps.forEach((step, i) => {
    const stepX = n === 1 ? marginX + spread / 2 : marginX + (i / (n - 1)) * spread;

    // Circle node
    opts.slide.addShape('ellipse', {
      x: stepX - r, y: lineY - r, w: r * 2, h: r * 2,
      fill: { color: accent }, line: { color: accent },
    });
    // Step number inside circle
    opts.slide.addText(step.number || String(i + 1), {
      x: stepX - r, y: lineY - r, w: r * 2, h: r * 2,
      fontFace: FONTS.body, fontSize: 12, bold: true, color: COLORS.white,
      align: 'center', valign: 'middle',
    });

    // Heading below line
    const textLeft  = Math.max(0.3, stepX - colW / 2);
    const textRight = Math.min(SLIDE_W - 0.3, textLeft + colW);
    const textW     = textRight - textLeft;
    opts.slide.addText(step.heading || '', {
      x: textLeft, y: 4.15, w: textW, h: 0.5,
      fontFace: FONTS.heading, fontSize: 14, bold: true, color: navy,
      align: 'center', wrap: true,
    });

    // Body below heading
    if (step.body) {
      opts.slide.addText(step.body, {
        x: textLeft, y: 4.7, w: textW, h: BAR_Y - 4.7 - 0.1,
        fontFace: FONTS.body, fontSize: 13, color: COLORS.darkGray,
        align: 'center', wrap: true, valign: 'top',
      });
    }
  });
}

module.exports = { renderTimeline };
