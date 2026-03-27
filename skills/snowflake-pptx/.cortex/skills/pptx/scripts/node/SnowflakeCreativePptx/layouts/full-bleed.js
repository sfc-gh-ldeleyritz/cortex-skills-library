'use strict';
const path = require('node:path');
const { COLORS, FONTS, PHOTOS_DIR, SLIDE_W, SLIDE_H } = require('../brand.js');

function renderFullBleed(prs, slide, opts, specDir) {
  const overlayMap = {
    dark:  { color: COLORS.navy,  transparency: 40 },  // 40 = 40% transparent (60% opaque)
    light: { color: COLORS.white, transparency: 40 },
    none:  null,
  };
  const overlayKey = slide.overlay || 'dark';
  const overlayDef = overlayMap[overlayKey] ?? overlayMap.dark;
  const textColor  = overlayKey === 'light' ? COLORS.navy : COLORS.white;
  const accent     = opts.accent;

  let imgPath = slide.image;
  if (imgPath) {
    if (!path.isAbsolute(imgPath) && !imgPath.includes('/') && !imgPath.includes(path.sep))
      imgPath = path.join(PHOTOS_DIR, imgPath);
    else if (!path.isAbsolute(imgPath))
      imgPath = path.join(specDir, imgPath);
  }

  if (imgPath) opts.slide.addImage({ path: imgPath, x: 0, y: 0, w: SLIDE_W, h: SLIDE_H });
  if (overlayDef) {
    opts.slide.addShape('rect', {
      x: 0, y: 0, w: SLIDE_W, h: SLIDE_H,
      fill: { type: 'solid', color: overlayDef.color, transparency: overlayDef.transparency },
      line: { color: overlayDef.color },
    });
  }
  if (slide.quote) {
    opts.slide.addText('"' + slide.quote + '"', {
      x: 1.5, y: 1.8, w: SLIDE_W - 3.0, h: 2.5,
      fontFace: FONTS.body, fontSize: 28, italic: true, color: textColor,
      align: 'center', valign: 'middle', wrap: true,
    });
  }
  if (slide.attribution) {
    opts.slide.addText('— ' + slide.attribution, {
      x: 1.5, y: 4.5, w: SLIDE_W - 3.0, h: 0.6,
      fontFace: FONTS.body, fontSize: 15, color: accent, align: 'center',
    });
  }
}

module.exports = { renderFullBleed };
