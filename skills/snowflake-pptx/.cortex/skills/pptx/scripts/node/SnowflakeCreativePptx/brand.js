'use strict';
const fs = require('node:fs');

// All paths relative to repo root (CLI must be invoked from repo root)
const ASSETS_BASE = '.cortex/skills/pptx/scripts/snowflake/assets';

/**
 * Parse an SVG file's natural width/height from its viewBox or width/height attributes.
 * Returns { w, h } in SVG user units. Defaults to { w: 1, h: 1 } if not found.
 */
function getSvgAspect(filePath) {
  try {
    const src = fs.readFileSync(filePath, 'utf8');
    // viewBox="0 0 W H" — prefer this
    const vbMatch = src.match(/viewBox="0\s+0\s+([\d.]+)\s+([\d.]+)"/);
    if (vbMatch) {
      const w = parseFloat(vbMatch[1]), h = parseFloat(vbMatch[2]);
      if (h > 0) return { w, h };
    }
    // Fall back to width="W" height="H"
    const wMatch  = src.match(/width="([\d.]+)[^"]*"/);
    const hMatch  = src.match(/height="([\d.]+)[^"]*"/);
    if (wMatch && hMatch) {
      const w = parseFloat(wMatch[1]), h = parseFloat(hMatch[1]);
      if (h > 0) return { w, h };
    }
  } catch (_) { /* file not found or unreadable */ }
  return { w: 1, h: 1 };
}

const COLORS = {
  snowflakeBlue:   '29B5E8',
  navy:            '1B2A3B',
  white:           'FFFFFF',
  lightGray:       'F2F2F2',
  darkGray:        '333333',
  starBlue:        '75CDD7',
  valenciaOrange:  'FF9F36',
  firstLight:      'D45B90',
  purpleMoon:      '7254A3',
};

const FONTS = {
  heading: 'Arial Black',
  body:    'Arial',
};

const MOODS = {
  'dark and premium':   { bg: 'navy',          text: 'white', accent: 'snowflakeBlue' },
  'clean and minimal':  { bg: 'white',          text: 'navy',  accent: 'snowflakeBlue' },
  'bold and energetic': { bg: 'snowflakeBlue',  text: 'white', accent: 'navy' },
};

const DEFAULT_MOOD = 'clean and minimal';

function resolveMood(mood) {
  const entry = MOODS[mood] || MOODS[DEFAULT_MOOD];
  return {
    bg:     COLORS[entry.bg],
    text:   COLORS[entry.text],
    accent: COLORS[entry.accent],
  };
}

// LAYOUT_WIDE = 13.33" x 7.5"
const SLIDE_W  = 13.33;
const SLIDE_H  = 7.5;
const LOGO_PATH  = `${ASSETS_BASE}/svg/graphic_snowflake_logo_blue.svg`;
const ICONS_DIR  = `${ASSETS_BASE}/svg/`;
const PHOTOS_DIR = `${ASSETS_BASE}/jpg/`;
const LOGO_W   = 1.5;
const { w: _svgW, h: _svgH } = getSvgAspect(LOGO_PATH);
const LOGO_H   = _svgH > 0 ? LOGO_W * (_svgH / _svgW) : 0.6;
const LOGO_X   = SLIDE_W - LOGO_W - 0.23;  // right-aligned with margin
const LOGO_Y   = 0.25;
const BAR_H    = 0.325;
const BAR_Y    = SLIDE_H - BAR_H - 0.325;  // ~6.85

module.exports = { COLORS, FONTS, MOODS, DEFAULT_MOOD, resolveMood, LOGO_PATH, ICONS_DIR, PHOTOS_DIR,
                   SLIDE_W, SLIDE_H, LOGO_W, LOGO_H, LOGO_X, LOGO_Y, BAR_H, BAR_Y, getSvgAspect };
