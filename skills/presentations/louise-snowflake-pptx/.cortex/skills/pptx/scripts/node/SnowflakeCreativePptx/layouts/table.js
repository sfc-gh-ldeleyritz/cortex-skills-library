'use strict';
const { COLORS, FONTS, LOGO_PATH, LOGO_X, LOGO_Y, LOGO_W, LOGO_H, SLIDE_W, SLIDE_H } = require('../brand.js');

function renderTable(prs, slide, opts) {
  const accent = opts.accent;
  const navy   = COLORS.navy;

  opts.slide.background = { color: COLORS.white };
  opts.slide.addShape('rect', { x: 0, y: 0, w: SLIDE_W, h: 0.15, fill: { color: accent }, line: { color: accent } });
  opts.slide.addImage({ path: LOGO_PATH, x: LOGO_X, y: LOGO_Y, w: LOGO_W, h: LOGO_H });
  if (slide.title) opts.slide.addText(slide.title, { x: 0.5, y: 0.25, w: LOGO_X - 0.75, h: 0.65, fontFace: FONTS.heading, fontSize: 24, bold: true, color: navy });
  if (slide.subtitle) opts.slide.addText(slide.subtitle, { x: 0.5, y: 0.9, w: SLIDE_W - 1.0, h: 0.4, fontFace: FONTS.body, fontSize: 14, color: COLORS.darkGray });

  const headers = slide.headers || [];
  const rows    = slide.rows || [];
  const numCols = headers.length;
  if (numCols === 0) return;

  const tableY  = slide.subtitle ? 1.45 : 1.1;
  const tableW  = SLIDE_W - 1.0;  // 0.5" margin each side
  const rowH    = (SLIDE_H - tableY - 0.3) / (rows.length + 1);
  const colW    = tableW / numCols;
  const tableX  = 0.5;

  headers.forEach((h, ci) => {
    opts.slide.addShape('rect', { x: tableX + ci * colW, y: tableY, w: colW, h: rowH, fill: { color: accent }, line: { color: COLORS.white } });
    opts.slide.addText(h, { x: tableX + ci * colW + 0.1, y: tableY, w: colW - 0.1, h: rowH, fontFace: FONTS.body, fontSize: 14, bold: true, color: COLORS.white, valign: 'middle' });
  });
  rows.forEach((row, ri) => {
    const rowY  = tableY + (ri + 1) * rowH;
    const rowBg = ri % 2 === 0 ? COLORS.lightGray : COLORS.white;
    row.forEach((cell, ci) => {
      opts.slide.addShape('rect', { x: tableX + ci * colW, y: rowY, w: colW, h: rowH, fill: { color: rowBg }, line: { color: COLORS.lightGray } });
      opts.slide.addText(String(cell), { x: tableX + ci * colW + 0.1, y: rowY, w: colW - 0.1, h: rowH, fontFace: FONTS.body, fontSize: 13, color: navy, valign: 'middle' });
    });
  });
}

module.exports = { renderTable };
