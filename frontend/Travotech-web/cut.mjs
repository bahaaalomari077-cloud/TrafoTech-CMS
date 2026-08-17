import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const src = process.argv[2];
const out = process.argv[3];
const b = await chromium.launch({ args: ['--no-sandbox'] });
const p = await b.newPage();

const dataUrl = 'data:image/jpeg;base64,' + fs.readFileSync(src).toString('base64');

const result = await p.evaluate(async (url) => {
  const img = new Image();
  img.src = url;
  await img.decode();

  const c = document.createElement('canvas');
  c.width = img.width; c.height = img.height;
  const ctx = c.getContext('2d', { willReadFrequently: true });
  ctx.drawImage(img, 0, 0);
  const d = ctx.getImageData(0, 0, c.width, c.height);
  const px = d.data;

  // Sample the corners: a flat logo backdrop is whatever they agree on.
  const corner = (x, y) => { const i = (y * c.width + x) * 4; return [px[i], px[i+1], px[i+2]]; };
  const bg = corner(2, 2);

  // Distance from the background colour decides transparency. A generous
  // threshold clears JPEG compression noise around the letters without
  // eating the pale blue squares in the mark.
  const near = (r, g, bl, tol) =>
    Math.abs(r - bg[0]) <= tol && Math.abs(g - bg[1]) <= tol && Math.abs(bl - bg[2]) <= tol;

  let minX = c.width, minY = c.height, maxX = -1, maxY = -1;
  for (let y = 0; y < c.height; y++) {
    for (let x = 0; x < c.width; x++) {
      const i = (y * c.width + x) * 4;
      if (near(px[i], px[i+1], px[i+2], 18)) {
        px[i+3] = 0;
      } else {
        if (x < minX) minX = x;
        if (y < minY) minY = y;
        if (x > maxX) maxX = x;
        if (y > maxY) maxY = y;
      }
    }
  }
  ctx.putImageData(d, 0, 0);

  // Crop to the ink, with a small breathing margin.
  const pad = 8;
  minX = Math.max(0, minX - pad); minY = Math.max(0, minY - pad);
  maxX = Math.min(c.width - 1, maxX + pad); maxY = Math.min(c.height - 1, maxY + pad);
  const w = maxX - minX + 1, h = maxY - minY + 1;

  const o = document.createElement('canvas');
  o.width = w; o.height = h;
  o.getContext('2d').drawImage(c, minX, minY, w, h, 0, 0, w, h);

  return { png: o.toDataURL('image/png'), bg, w, h, from: [img.width, img.height] };
}, dataUrl);

fs.writeFileSync(out, Buffer.from(result.png.split(',')[1], 'base64'));
console.log(`background sampled: rgb(${result.bg.join(',')})`);
console.log(`cropped ${result.from[0]}x${result.from[1]} -> ${result.w}x${result.h}`);
console.log(`written: ${path.basename(out)} (${fs.statSync(out).size} bytes)`);
await b.close();
