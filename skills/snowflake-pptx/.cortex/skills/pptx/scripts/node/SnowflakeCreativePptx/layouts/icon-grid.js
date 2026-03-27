'use strict';
const path = require('node:path');
const fs   = require('node:fs');
const { COLORS, FONTS, LOGO_PATH, LOGO_X, LOGO_Y, LOGO_W, LOGO_H, SLIDE_W, ICONS_DIR, BAR_Y, getSvgAspect } = require('../brand.js');

function renderIconGrid(prs, slide, opts) {
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

  const items   = slide.items || [];
  const numCols = items.length === 6 ? 3 : 2;
  const numRows = 2;
  const gapX    = 0.25;
  const gapY    = 0.2;
  const marginX = 0.4;
  const topY    = 1.1;

  const cellW = (SLIDE_W - marginX * 2 - gapX * (numCols - 1)) / numCols;
  const cellH = (BAR_Y - topY - gapY * (numRows - 1)) / numRows;

  items.slice(0, numCols * numRows).forEach((item, idx) => {
    const col  = idx % numCols;
    const row  = Math.floor(idx / numCols);
    const x    = marginX + col * (cellW + gapX);
    const y    = topY + row * (cellH + gapY);

    const bannerH = item.icon ? 0.65 : 0.20;
    // lightGray card background for full cell
    opts.slide.addShape('rect', {
      x, y, w: cellW, h: cellH,
      fill: { color: COLORS.lightGray }, line: { color: COLORS.lightGray },
    });
    // Accent top banner
    opts.slide.addShape('rect', {
      x, y, w: cellW, h: bannerH,
      fill: { color: accent }, line: { color: accent },
    });

    // Icon inside banner, aspect-preserved, bounding box 0.5x0.5, centered
    if (item.icon) {
      const iconPath = (item.icon.includes('/') || item.icon.includes(path.sep))
        ? item.icon : path.join(ICONS_DIR, item.icon);
      if (fs.existsSync(iconPath)) {
        const maxDim  = 0.5;
        const { w: svgW, h: svgH } = getSvgAspect(iconPath);
        const aspect  = svgW / svgH;
        const iconW   = aspect >= 1 ? maxDim : maxDim * aspect;
        const iconH   = aspect >= 1 ? maxDim / aspect : maxDim;
        const iconX   = x + (cellW - iconW) / 2;
        const iconY   = y + (bannerH - iconH) / 2;
        opts.slide.addImage({ path: iconPath, x: iconX, y: iconY, w: iconW, h: iconH });
      } else {
        console.warn(`icon-grid: icon not found '${iconPath}' -- skipped`);
      }
    }

    let contentY = y + bannerH + 0.15;
    if (item.heading) {
      opts.slide.addText(item.heading, {
        x: x + 0.1, y: contentY, w: cellW - 0.2, h: 0.45,
        fontFace: FONTS.heading, fontSize: 15, bold: true, color: navy, wrap: true,
      });
      contentY += 0.5;
    }
    if (item.body) {
      opts.slide.addText(item.body, {
        x: x + 0.1, y: contentY, w: cellW - 0.2, h: cellH - bannerH - 0.15 - (item.heading ? 0.5 : 0) - 0.1,
        fontFace: FONTS.body, fontSize: 12, color: COLORS.darkGray, wrap: true, valign: 'top',
      });
    }
  });
}

module.exports = { renderIconGrid };
