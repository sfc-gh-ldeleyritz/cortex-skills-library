'use strict';
const { COLORS, FONTS, LOGO_PATH, LOGO_X, LOGO_Y, LOGO_W, LOGO_H, SLIDE_W, BAR_Y } = require('../brand.js');

function renderComparison(prs, slide, opts) {
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

  const panelTop  = 1.0;
  const panelH    = BAR_Y - panelTop;
  const headerH   = 0.6;
  const splitX    = SLIDE_W / 2;   // 6.665"
  const dividerW  = 0.1;
  const leftX     = 0.4;
  const leftW     = splitX - leftX - dividerW / 2;
  const rightX    = splitX + dividerW / 2;
  const rightW    = SLIDE_W - rightX - 0.4;

  // Divider
  opts.slide.addShape('rect', {
    x: splitX - dividerW / 2, y: panelTop, w: dividerW, h: panelH,
    fill: { color: COLORS.lightGray }, line: { color: COLORS.lightGray },
  });

  // Resolve per-panel optional color override (darkGray | navy | accent -> resolved hex)
  const PANEL_COLOR_MAP = { darkGray: COLORS.darkGray, navy: COLORS.navy, accent };
  const leftHeaderColor  = (slide.left  && slide.left.color  && PANEL_COLOR_MAP[slide.left.color])  || accent;
  const rightHeaderColor = (slide.right && slide.right.color && PANEL_COLOR_MAP[slide.right.color]) || navy;

  _renderPanel(opts.slide, slide.left  || {}, leftX,  leftW,  panelTop, headerH, leftHeaderColor,  COLORS, FONTS, BAR_Y);
  _renderPanel(opts.slide, slide.right || {}, rightX, rightW, panelTop, headerH, rightHeaderColor, COLORS, FONTS, BAR_Y);
}

function _renderPanel(prsSlide, panel, x, w, panelTop, headerH, headerColor, COLORS, FONTS, barY) {
  const navy = COLORS.navy;

  // Header background
  prsSlide.addShape('rect', {
    x, y: panelTop, w, h: headerH,
    fill: { color: headerColor }, line: { color: headerColor },
  });

  // Heading text in header
  if (panel.heading) {
    prsSlide.addText(panel.heading, {
      x: x + 0.15, y: panelTop, w: w - 0.3, h: headerH,
      fontFace: FONTS.heading, fontSize: 16, bold: true, color: COLORS.white,
      valign: 'middle',
    });
  }

  // Bullets
  const bullets = panel.bullets || [];
  const bulletCount = bullets.length;
  const bulletSpacing = bulletCount <= 3 ? 0.75 : 0.58;
  const bulletFontSize = bulletCount <= 3 ? 16 : 14;
  let y = panelTop + headerH + 0.15;
  bullets.slice(0, 5).forEach(b => {
    prsSlide.addText(`\u2713  ${b}`, {
      x: x + 0.15, y, w: w - 0.3, h: 0.55,
      fontFace: FONTS.body, fontSize: bulletFontSize, color: navy,
      wrap: true, valign: 'top',
    });
    y += bulletSpacing;
  });
}

module.exports = { renderComparison };
