'use strict';
const path = require('node:path');
const fs   = require('node:fs');
const { COLORS, FONTS, LOGO_PATH, LOGO_X, LOGO_Y, LOGO_W, LOGO_H, SLIDE_W, ICONS_DIR, BAR_Y, getSvgAspect } = require('../brand.js');

function renderColumns(prs, slide, opts) {
  const accent  = opts.accent;
  const navy    = COLORS.navy;
  const variant = slide.variant || 'default';

  opts.slide.background = { color: COLORS.white };

  // Top accent bar and logo (all variants except bordered)
  if (variant !== 'bordered') {
    opts.slide.addShape('rect', { x: 0, y: 0, w: SLIDE_W, h: 0.15, fill: { color: accent }, line: { color: accent } });
  }
  opts.slide.addImage({ path: LOGO_PATH, x: LOGO_X, y: LOGO_Y, w: LOGO_W, h: LOGO_H });

  if (slide.title) opts.slide.addText(slide.title, { x: 0.5, y: 0.25, w: LOGO_X - 0.75, h: 0.65, fontFace: FONTS.heading, fontSize: 24, bold: true, color: navy });
  if (slide.subtitle) opts.slide.addText(slide.subtitle, { x: 0.5, y: 0.9, w: SLIDE_W - 1.0, h: 0.4, fontFace: FONTS.body, fontSize: 14, color: COLORS.darkGray });

  const items   = slide.items || [];
  const numCols = Math.min(items.length, slide.columns || items.length, 4);
  if (numCols === 0) return;

  const marginX = 0.5;
  const gapX    = 0.3;
  const colW    = (SLIDE_W - marginX * 2 - gapX * (numCols - 1)) / numCols;
  const iconH   = 0.7;

  const anyIcon = variant === 'numbered'
    ? true  // numbered always reserves icon height slot to prevent overlap
    : items.slice(0, numCols).some(item => {
        if (!item.icon) return false;
        const iconPath = (item.icon.includes('/') || item.icon.includes(path.sep))
          ? item.icon : path.join(ICONS_DIR, item.icon);
        return fs.existsSync(iconPath);
      });
  const anyHeading = items.slice(0, numCols).some(item => !!item.heading);

  const availableTop = slide.subtitle ? 1.35 : 0.9;

  if (variant === 'cards') {
    _renderCards(items, numCols, colW, gapX, marginX, availableTop, accent, navy, opts);
    return;
  }

  const blockH   = (anyIcon ? iconH + 0.1 : 0) + (anyHeading ? 0.55 : 0) + 0.15 + 2.0;
  const availableH = BAR_Y - availableTop;
  const contentY = availableTop + (availableH - blockH) / 2;
  const headingY = anyIcon ? contentY + iconH + 0.1 : contentY;

  if (variant === 'bordered') {
    // Vertical accent bar per column: start below the header area, run down to the bottom bar
    items.slice(0, numCols).forEach((item, i) => {
      const x = marginX + i * (colW + gapX);
      opts.slide.addShape('rect', { x, y: availableTop, w: 0.06, h: BAR_Y - availableTop, fill: { color: accent }, line: { color: accent } });
    });
  }

  items.slice(0, numCols).forEach((item, i) => {
    const x     = marginX + i * (colW + gapX);
    const textX = variant === 'bordered' ? x + 0.06 : x;
    const textW = variant === 'bordered' ? colW - 0.06 : colW;

    if (variant === 'numbered') {
      // Number renders at contentY (same slot as icon would)
      opts.slide.addText(String(i + 1), {
        x: textX, y: contentY, w: textW, h: iconH,
        fontFace: FONTS.heading, fontSize: 48, bold: true, color: accent, valign: 'middle',
      });
    } else if (item.icon) {
      const iconPath = (item.icon.includes('/') || item.icon.includes(path.sep))
        ? item.icon : path.join(ICONS_DIR, item.icon);
      if (fs.existsSync(iconPath)) {
        const { w: svgW, h: svgH } = getSvgAspect(iconPath);
        const iconW = iconH * (svgW / svgH);
        opts.slide.addImage({ path: iconPath, x: textX, y: contentY, w: iconW, h: iconH });
      } else {
        console.warn(`columns: icon not found '${iconPath}' -- skipped`);
      }
    }

    let y = headingY;
    if (item.heading) {
      opts.slide.addText(item.heading, { x: textX, y, w: textW, h: 0.5, fontFace: FONTS.heading, fontSize: 16, bold: true, color: navy });
      y += 0.55;
    }
    // Heading underline: skip for numbered and bordered variants
    if (variant !== 'numbered' && variant !== 'bordered') {
      opts.slide.addShape('rect', { x: textX, y, w: textW * 0.4, h: 0.04, fill: { color: accent }, line: { color: accent } });
    }
    y += 0.15;
    if (item.body) {
      const bodyFontSize = (variant === 'bordered' && numCols <= 2) ? 16 : 14;
      opts.slide.addText(item.body, { x: textX, y, w: textW, h: 2.0, fontFace: FONTS.body, fontSize: bodyFontSize, color: COLORS.darkGray, wrap: true, valign: 'top' });
    }
  });
}

function _renderCards(items, numCols, colW, gapX, marginX, availableTop, accent, navy, opts) {
  const cardBottom = BAR_Y - 0.3;
  const cardTop    = availableTop;
  const cardH      = cardBottom - cardTop;

  items.slice(0, numCols).forEach((item, i) => {
    const x = marginX + i * (colW + gapX);
    // Card background
    opts.slide.addShape('roundRect', {
      x, y: cardTop, w: colW, h: cardH,
      fill: { color: COLORS.lightGray }, line: { color: COLORS.lightGray },
      rectRadius: 0.1,
    });
    // Accent top border
    opts.slide.addShape('rect', {
      x, y: cardTop, w: colW, h: 0.1,
      fill: { color: accent }, line: { color: accent },
    });
    let y = cardTop + 0.25;
    if (item.icon) {
      const iconPath = (item.icon.includes('/') || item.icon.includes(path.sep))
        ? item.icon : path.join(ICONS_DIR, item.icon);
      if (fs.existsSync(iconPath)) {
        const iconH = 0.7;
        const { w: svgW, h: svgH } = getSvgAspect(iconPath);
        const iconW = iconH * (svgW / svgH);
        opts.slide.addImage({ path: iconPath, x: x + 0.15, y, w: iconW, h: iconH });
        y += iconH + 0.1;
      } else {
        console.warn(`columns: icon not found '${iconPath}' -- skipped`);
      }
    }
    if (item.heading) {
      opts.slide.addText(item.heading, { x: x + 0.15, y, w: colW - 0.3, h: 0.5, fontFace: FONTS.heading, fontSize: 16, bold: true, color: navy });
      y += 0.55;
    }
    if (item.body) {
      opts.slide.addText(item.body, {
        x: x + 0.15, y, w: colW - 0.3, h: cardBottom - y - 0.2,
        fontFace: FONTS.body, fontSize: 13, color: COLORS.darkGray, wrap: true, valign: 'top',
      });
    }
  });
}

module.exports = { renderColumns };
