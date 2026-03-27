'use strict';

// Pass-through for raw pptxgenjs elements.
// el.type: "text" | "shape" | "image"
// el.shape: ShapeType string for type "shape" (e.g. "rect", "ellipse").
//   pptxgenjs 4.x addShape() accepts shape type as a string alias (e.g. 'rect', 'ellipse').
//   Pass el.shape directly. If pptxgenjs rejects the value, it will throw -- the caller is responsible for valid shape names.
// el.value: text content for type "text"
// el.path: image path for type "image"
// el.options: pptxgenjs options object passed through as-is
function renderFree(prs, slide, opts) {
  (slide.elements || []).forEach(el => {
    switch (el.type) {
      case 'text':  opts.slide.addText(el.value || '', el.options || {}); break;
      case 'shape': opts.slide.addShape(el.shape || 'rect', el.options || {}); break;
      case 'image': opts.slide.addImage({ path: el.path || '', ...(el.options || {}) }); break;
      default: console.warn(`free layout: unknown element type '${el.type}' -- skipped`);
    }
  });
}

module.exports = { renderFree };
