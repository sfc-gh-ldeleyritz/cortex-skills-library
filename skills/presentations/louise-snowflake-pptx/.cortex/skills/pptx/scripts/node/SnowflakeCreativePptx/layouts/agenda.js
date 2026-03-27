'use strict';
const { COLORS, FONTS, LOGO_PATH, LOGO_X, LOGO_Y, LOGO_W, LOGO_H, SLIDE_W, SLIDE_H } = require('../brand.js');

function renderAgenda(prs, slide, opts) {
  const accent = opts.accent;
  const navy   = COLORS.navy;

  opts.slide.background = { color: COLORS.white };
  opts.slide.addShape('rect', { x: 0, y: 0, w: SLIDE_W, h: 0.15, fill: { color: accent }, line: { color: accent } });
  opts.slide.addImage({ path: LOGO_PATH, x: LOGO_X, y: LOGO_Y, w: LOGO_W, h: LOGO_H });
  opts.slide.addText(slide.title || 'Agenda', {
    x: 0.75, y: 0.35, w: LOGO_X - 1.0, h: 0.8,
    fontFace: FONTS.heading, fontSize: 28, bold: true, color: navy,
  });

  const items     = slide.items || [];
  const useTwoCol = items.length > 4;
  const colWidth  = useTwoCol ? 5.5 : SLIDE_W - 1.5;
  const rowH      = items.length <= 4 ? 0.95 : items.length <= 5 ? 0.85 : 0.65;
  const itemFontSize = items.length <= 4 ? 20 : 16;
  const circleSize   = items.length <= 4 ? 0.48 : 0.38;
  const half      = Math.ceil(items.length / 2);

  // Vertically centre the item list.
  // Two-column: height = taller column = half * rowH.
  // Single-column: height = items.length * rowH.
  const totalListH = useTwoCol ? half * rowH : items.length * rowH;
  const startY     = Math.max(1.35, (SLIDE_H - totalListH) / 2);

  items.forEach((item, i) => {
    const col = useTwoCol && i >= half ? 1 : 0;
    const row = col === 1 ? i - half : i;
    const x   = 0.75 + col * 6.5;
    const y   = startY + row * rowH;

    // Numbered circle: addShape draws the filled circle, addText floats the number on top
    opts.slide.addShape('ellipse', { x, y: y + 0.1, w: circleSize, h: circleSize, fill: { color: accent }, line: { color: accent } });
    opts.slide.addText(String(i + 1), {
      x, y: y + 0.08, w: circleSize, h: circleSize,
      fontFace: FONTS.body, fontSize: 12, bold: true,
      color: COLORS.white, align: 'center', valign: 'middle',
    });
    opts.slide.addText(item, {
      x: x + circleSize + 0.12, y, w: colWidth - circleSize - 0.22, h: rowH,
      fontFace: FONTS.body, fontSize: itemFontSize, color: navy, valign: 'middle',
    });
  });
}

module.exports = { renderAgenda };
