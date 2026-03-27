'use strict';
const path = require('node:path');
const { COLORS, FONTS, LOGO_PATH, LOGO_X, LOGO_Y, LOGO_W, LOGO_H, PHOTOS_DIR, SLIDE_W, SLIDE_H } = require('../brand.js');

function renderSplit(prs, slide, opts, specDir) {
  const accent  = opts.accent;
  const navy    = COLORS.navy;
  const imgLeft = (slide.image_position || 'left') === 'left';

  opts.slide.background = { color: COLORS.white };

  let imgPath = slide.image;
  if (imgPath) {
    if (!path.isAbsolute(imgPath) && !imgPath.includes('/') && !imgPath.includes(path.sep))
      imgPath = path.join(PHOTOS_DIR, imgPath);
    else if (!path.isAbsolute(imgPath))
      imgPath = path.join(specDir, imgPath);
  }

  const splitX   = SLIDE_W / 2;   // 6.665"
  const imgX     = imgLeft ? 0      : splitX;
  const contentX = imgLeft ? splitX + 0.4 : 0.5;
  const contentW = splitX - 0.6;

  if (imgPath) opts.slide.addImage({ path: imgPath, x: imgX, y: 0, w: splitX, h: SLIDE_H });
  else { console.warn('split: no image provided -- rendering content full-width'); }

  // Accent bar across the top of the content half
  opts.slide.addShape('rect', { x: contentX - 0.2, y: 0, w: contentW + 0.2, h: 0.15, fill: { color: accent }, line: { color: accent } });

  // Logo: right-aligned within the content half
  const logoX = imgLeft ? LOGO_X : splitX - LOGO_W - 0.23;
  opts.slide.addImage({ path: LOGO_PATH, x: logoX, y: LOGO_Y, w: LOGO_W, h: LOGO_H });

  let y = 0.35;
  if (slide.title) {
    opts.slide.addText(slide.title, { x: contentX, y, w: contentW - LOGO_W - 0.3, h: 0.75, fontFace: FONTS.heading, fontSize: 22, bold: true, color: navy });
    y += 0.8;
  }
  if (slide.subtitle) {
    opts.slide.addText(slide.subtitle, { x: contentX, y, w: contentW, h: 0.4, fontFace: FONTS.body, fontSize: 13, color: COLORS.darkGray });
    y += 0.5;
  }
  (slide.bullets || []).forEach(b => {
    opts.slide.addText(`\u2022  ${b}`, { x: contentX, y, w: contentW, h: 0.55, fontFace: FONTS.body, fontSize: 15, color: navy, wrap: true, shrinkText: true });
    y += 0.58;
  });
}

module.exports = { renderSplit };
