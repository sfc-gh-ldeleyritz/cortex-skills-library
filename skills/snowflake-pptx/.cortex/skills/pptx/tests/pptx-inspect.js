'use strict';
const fs = require('node:fs');

/**
 * Lightweight PPTX structural inspector.
 * Parses the ZIP central directory to enumerate entries without external deps.
 *
 * @param {string} filePath - Path to the .pptx file
 * @returns {{ error: string|null, fileSizeBytes: number, isValidZip: boolean, hasContentTypes: boolean, hasPresentation: boolean, slideCount: number }}
 */
function inspectPptx(filePath) {
  const result = {
    error: null,
    fileSizeBytes: 0,
    isValidZip: false,
    hasContentTypes: false,
    hasPresentation: false,
    slideCount: 0,
  };

  try {
    const stat = fs.statSync(filePath);
    result.fileSizeBytes = stat.size;
  } catch (err) {
    result.error = `Cannot stat file: ${err.message}`;
    return result;
  }

  let buf;
  try {
    buf = fs.readFileSync(filePath);
  } catch (err) {
    result.error = `Cannot read file: ${err.message}`;
    return result;
  }

  // ZIP magic bytes: PK\x03\x04
  if (buf.length < 4 || buf[0] !== 0x50 || buf[1] !== 0x4B || buf[2] !== 0x03 || buf[3] !== 0x04) {
    result.error = 'Not a valid ZIP file (bad magic bytes)';
    return result;
  }
  result.isValidZip = true;

  // Find the End of Central Directory record (EOCD).
  // EOCD signature: PK\x05\x06 — search backwards from end of file.
  let eocdOffset = -1;
  for (let i = buf.length - 22; i >= 0 && i >= buf.length - 65557; i--) {
    if (buf[i] === 0x50 && buf[i + 1] === 0x4B && buf[i + 2] === 0x05 && buf[i + 3] === 0x06) {
      eocdOffset = i;
      break;
    }
  }

  if (eocdOffset === -1) {
    result.error = 'Cannot find ZIP End of Central Directory';
    return result;
  }

  const cdEntries = buf.readUInt16LE(eocdOffset + 10);
  const cdSize    = buf.readUInt32LE(eocdOffset + 12);
  const cdOffset  = buf.readUInt32LE(eocdOffset + 16);

  // Walk central directory entries
  const entries = [];
  let offset = cdOffset;
  for (let i = 0; i < cdEntries && offset + 46 <= buf.length; i++) {
    // Central directory file header signature: PK\x01\x02
    if (buf[offset] !== 0x50 || buf[offset + 1] !== 0x4B || buf[offset + 2] !== 0x01 || buf[offset + 3] !== 0x02) {
      break;
    }
    const fnLen    = buf.readUInt16LE(offset + 28);
    const extraLen = buf.readUInt16LE(offset + 30);
    const commentLen = buf.readUInt16LE(offset + 32);

    if (offset + 46 + fnLen > buf.length) break;
    const fileName = buf.toString('utf8', offset + 46, offset + 46 + fnLen);
    entries.push(fileName);

    offset += 46 + fnLen + extraLen + commentLen;
  }

  result.hasContentTypes = entries.some(e => e === '[Content_Types].xml');
  result.hasPresentation = entries.some(e => e === 'ppt/presentation.xml');
  result.slideCount = entries.filter(e => /^ppt\/slides\/slide\d+\.xml$/.test(e)).length;

  return result;
}

module.exports = { inspectPptx };
