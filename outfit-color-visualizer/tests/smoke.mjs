import assert from "node:assert/strict";
import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { JSDOM } from "jsdom";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(__dirname, "..");

const indexHtml = await fs.readFile(path.join(rootDir, "index.html"), "utf8");
const scriptJs = await fs.readFile(path.join(rootDir, "script.js"), "utf8");

const htmlWithoutStyles = indexHtml.replace(/<link rel="stylesheet" href="\.\/styles\.css"\s*\/>/, "");

const html = htmlWithoutStyles.replace(
  '<script src="./script.js"></script>',
  `<script>
    class ResizeObserver {
      constructor(callback) {
        this.callback = callback;
      }
      observe(target) {
        this.callback([{ target }]);
      }
      disconnect() {}
      unobserve() {}
    }
    window.ResizeObserver = ResizeObserver;
    window.requestAnimationFrame = (callback) => window.setTimeout(() => callback(Date.now()), 16);
    window.cancelAnimationFrame = (id) => window.clearTimeout(id);
    Object.defineProperty(HTMLElement.prototype, "scrollWidth", {
      configurable: true,
      get() {
        if (this.classList?.contains("row-track")) {
          return Math.max(1, this.children.length) * 184;
        }
        return 0;
      }
    });
    HTMLElement.prototype.getBoundingClientRect = function getBoundingClientRect() {
      return {
        width: this.classList?.contains("swatch") ? 170 : 0,
        height: 158,
        top: 0,
        left: 0,
        right: 170,
        bottom: 158
      };
    };
  </script>
  <script>${scriptJs}</script>`
);

const dom = new JSDOM(html, {
  runScripts: "dangerously",
  resources: "usable",
  url: "http://local.test/",
  pretendToBeVisual: true,
});

const { document } = dom.window;

await waitFor(() => document.querySelectorAll(".row-track").length > 0);

const compareInputA = document.querySelector("#compare-a");
const compareInputB = document.querySelector("#compare-b");
const chipsRoot = document.querySelector("#selected-chips");
const pickAButton = document.querySelector("#compare-target-a");
const pickBButton = document.querySelector("#compare-target-b");
const cancelButton = document.querySelector("#compare-target-cancel");

assert.equal(document.querySelectorAll(".row-track").length, 8, "expected 8 scrolling rows");
assert.equal(document.querySelectorAll(".row-scroll-button").length, 16, "expected left/right arrows on every row");

const topSkyBlue = findSwatch("tops", "Sky Blue");
const accessoryTan = findSwatch("accessories", "Tan");
const topSkyBlueAButton = findSwatchCompareButton("tops", "Sky Blue", "a");
const firstRowTrack = document.querySelector(".row-track");
const rightArrow = document.querySelector('.row-scroll-button[data-direction="1"]');

const initialA = compareInputA.value;
const initialB = compareInputB.value;

topSkyBlue.click();
assert.match(chipsRoot.textContent, /Sky Blue/, "normal swatch click should pin the color");
assert.equal(compareInputA.value, initialA, "normal swatch click should not overwrite compare A");
assert.equal(compareInputB.value, initialB, "normal swatch click should not overwrite compare B");

pickAButton.click();
accessoryTan.click();
assert.equal(compareInputA.value, "Tan", "pick mode A should fill compare A from swatch click");
assert.doesNotMatch(chipsRoot.textContent, /Accessories & Bags/, "pick mode should not add a compare-only swatch to chips");

pickBButton.click();
topSkyBlue.click();
assert.equal(compareInputB.value, "Sky Blue", "pick mode B should fill compare B from swatch click");

cancelButton.click();
topSkyBlue.click();
assert.doesNotMatch(chipsRoot.textContent, /Sky Blue · Tops & Shirts.*Sky Blue · Tops & Shirts/s, "selection should toggle back off when pick mode is cancelled");

topSkyBlueAButton.click();
assert.equal(compareInputA.value, "Sky Blue", "swatch A button should fill compare A directly");
assert.doesNotMatch(chipsRoot.textContent, /Sky Blue/, "swatch A button should not pin or unpin the swatch");

const beforeScroll = firstRowTrack.scrollLeft;
rightArrow.click();
await wait(400);
assert.notEqual(firstRowTrack.scrollLeft, beforeScroll, "row arrow should smooth-scroll the row");

console.log("All smoke tests passed.");

function findSwatch(rowId, label) {
  const swatchShells = Array.from(document.querySelectorAll(".swatch-shell"));
  const match = swatchShells.find((node) => node.dataset.rowId === rowId && node.textContent.includes(label));
  assert.ok(match, `could not find swatch ${label} in row ${rowId}`);
  return match.querySelector(".swatch");
}

function findSwatchCompareButton(rowId, label, slot) {
  const swatchShells = Array.from(document.querySelectorAll(".swatch-shell"));
  const match = swatchShells.find((node) => node.dataset.rowId === rowId && node.textContent.includes(label));
  assert.ok(match, `could not find swatch ${label} in row ${rowId}`);
  const button = match.querySelector(`.swatch-compare-button[data-slot="${slot}"]`);
  assert.ok(button, `could not find compare ${slot.toUpperCase()} button for swatch ${label}`);
  return button;
}

function wait(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function waitFor(condition, timeout = 1000) {
  const start = Date.now();
  while (!condition()) {
    if (Date.now() - start > timeout) {
      throw new Error("Timed out waiting for condition");
    }
    await wait(10);
  }
}
