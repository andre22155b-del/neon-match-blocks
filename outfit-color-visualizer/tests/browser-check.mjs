import assert from "node:assert/strict";
import fs from "node:fs/promises";
import http from "node:http";
import path from "node:path";
import { fileURLToPath } from "node:url";
import puppeteer from "puppeteer-core";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(__dirname, "..");
const chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
const fixturePath = path.join(rootDir, "tests", "fixtures", "sample-shirt.svg");
const brokenFixturePath = path.join(rootDir, "tests", "fixtures", "broken-item.txt");

const server = http.createServer(async (request, response) => {
  const requestPath = request.url === "/" ? "/index.html" : request.url;
  const filePath = path.join(rootDir, decodeURIComponent(requestPath));

  try {
    const body = await fs.readFile(filePath);
    response.writeHead(200, { "content-type": contentTypeFor(filePath) });
    response.end(body);
  } catch {
    response.writeHead(404, { "content-type": "text/plain; charset=utf-8" });
    response.end("Not found");
  }
});

await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
const address = server.address();
const appUrl = `http://127.0.0.1:${address.port}/`;

const browser = await puppeteer.launch({
  executablePath: chromePath,
  headless: "new",
  args: ["--no-sandbox"],
  defaultViewport: { width: 1440, height: 2200 },
});

const consoleMessages = [];
const pageErrors = [];

try {
  const page = await browser.newPage();

  page.on("console", (message) => {
    consoleMessages.push({ type: message.type(), text: message.text() });
  });

  page.on("pageerror", (error) => {
    pageErrors.push(error.message);
  });

  await page.goto(appUrl, { waitUntil: "networkidle0" });
  await page.waitForSelector(".row-track");
  await wait(300);

  const initialTrack = await page.evaluate(() => {
    const track = document.querySelector(".row-track");
    return {
      scrollLeft: track?.scrollLeft ?? -1,
      scrollWidth: track?.scrollWidth ?? -1,
      clientWidth: track?.clientWidth ?? -1,
    };
  });

  assert.ok(initialTrack.scrollWidth > initialTrack.clientWidth, "row should overflow horizontally");

  await page.click('.row-scroll-button[data-direction="1"]');
  await waitFor(async () => {
    const current = await page.evaluate(() => document.querySelector(".row-track")?.scrollLeft ?? -1);
    return current !== initialTrack.scrollLeft;
  }, 1200, "row arrow should move the track");

  const compareBeforeSelection = await page.evaluate(() => ({
    compareA: document.querySelector("#compare-a")?.value ?? "",
    compareB: document.querySelector("#compare-b")?.value ?? "",
  }));

  await clickSwatch(page, "purple-focus", "Lavender Mist");

  const afterSelection = await page.evaluate(() => ({
    chips: document.querySelector("#selected-chips")?.textContent ?? "",
    compareA: document.querySelector("#compare-a")?.value ?? "",
    compareB: document.querySelector("#compare-b")?.value ?? "",
  }));

  assert.match(afterSelection.chips, /Lavender Mist/, "normal swatch tap should pin the color");
  assert.equal(afterSelection.compareA, compareBeforeSelection.compareA, "normal swatch tap should not overwrite compare A");
  assert.equal(afterSelection.compareB, compareBeforeSelection.compareB, "normal swatch tap should not overwrite compare B");

  await page.click("#compare-target-a");
  await clickSwatch(page, "accessories", "Tan");

  const afterPickA = await page.evaluate(() => ({
    compareA: document.querySelector("#compare-a")?.value ?? "",
    compareB: document.querySelector("#compare-b")?.value ?? "",
    helper: document.querySelector("#compare-picker-copy")?.textContent ?? "",
  }));

  assert.equal(afterPickA.compareA, "Tan", "pick mode A should fill compare A from a swatch");
  assert.equal(afterPickA.compareB, compareBeforeSelection.compareB, "pick mode A should leave compare B alone");
  assert.doesNotMatch(afterPickA.helper, /Pick mode is on/, "pick mode should end after a successful pick");

  await page.click("#compare-target-b");
  await clickSwatch(page, "tops", "Sky Blue");

  const afterPickB = await page.evaluate(() => ({
    compareA: document.querySelector("#compare-a")?.value ?? "",
    compareB: document.querySelector("#compare-b")?.value ?? "",
  }));

  assert.equal(afterPickB.compareB, "Sky Blue", "pick mode B should fill compare B from a swatch");

  await page.evaluate(() => {
    document.querySelector("#compare-a").value = "Black";
    document.querySelector("#compare-a").dispatchEvent(new Event("input", { bubbles: true }));
  });
  const compareABeforeSwatchButton = await page.evaluate(() => document.querySelector("#compare-a")?.value ?? "");
  await clickSwatchCompareButton(page, "accessories", "Tan", "a");

  const afterDirectAButton = await page.evaluate(() => ({
    compareA: document.querySelector("#compare-a")?.value ?? "",
    chips: document.querySelector("#selected-chips")?.textContent ?? "",
  }));

  assert.equal(afterDirectAButton.compareA, "Tan", "swatch A button should fill compare A directly");
  assert.doesNotMatch(afterDirectAButton.chips, /Tan · Accessories & Bags/, "swatch A button should not pin the swatch");
  assert.notEqual(afterDirectAButton.compareA, compareABeforeSwatchButton, "swatch A button should change compare A");

  const uploadInput = await page.$("#item-upload-input");
  assert.ok(uploadInput, "upload input should exist");
  await uploadInput.uploadFile(fixturePath);

  await waitFor(async () => {
    const count = await page.evaluate(() => document.querySelectorAll(".item-card").length);
    return count > 0;
  }, 2000, "uploaded item card should render");

  const uploadState = await page.evaluate(() => ({
    paletteCount: document.querySelectorAll(".item-palette-button").length,
    suggestionCount: document.querySelectorAll(".item-suggestion-button").length,
    capture: document.querySelector("#item-camera-input")?.getAttribute("capture") ?? "",
  }));

  assert.ok(uploadState.paletteCount > 0, "uploaded item should extract a palette");
  assert.ok(uploadState.suggestionCount > 0, "uploaded item should generate match suggestions");
  assert.equal(uploadState.capture, "environment", "camera input should request the rear camera when supported");

  await page.click("#compare-a");
  const compareABeforePalette = await page.evaluate(() => document.querySelector("#compare-a")?.value ?? "");
  await page.click(".item-palette-button");

  await waitFor(async () => {
    const current = await page.evaluate(() => document.querySelector("#compare-a")?.value ?? "");
    return current !== compareABeforePalette;
  }, 1000, "palette click should fill the active compare slot");

  const finalState = await page.evaluate(() => ({
    compareA: document.querySelector("#compare-a")?.value ?? "",
    compareB: document.querySelector("#compare-b")?.value ?? "",
    verdict: document.querySelector("#compare-verdict")?.textContent ?? "",
  }));

  assert.ok(finalState.compareB.length > 0, "palette click should leave compare B with a value");
  assert.match(finalState.verdict, /\S/, "compare verdict should stay populated");

  await page.evaluate(() => {
    window.__originalExtractPaletteFromImage = window.extractPaletteFromImage;
    window.extractPaletteFromImage = (...args) =>
      new Promise((resolve, reject) => {
        window.setTimeout(() => {
          Promise.resolve(window.__originalExtractPaletteFromImage(...args)).then(resolve, reject);
        }, 250);
      });
  });

  await uploadInput.uploadFile(fixturePath);
  await page.click("#clear-items");
  await wait(700);

  const afterClearRace = await page.evaluate(() => ({
    itemCount: document.querySelectorAll(".item-card").length,
    galleryText: document.querySelector("#item-gallery")?.textContent ?? "",
  }));

  assert.equal(afterClearRace.itemCount, 0, "clearing items during processing should keep the gallery empty");
  assert.match(afterClearRace.galleryText, /Upload one item to pull colors/, "gallery should return to the empty state after clearing");

  await page.evaluate(() => {
    if (window.__originalExtractPaletteFromImage) {
      window.extractPaletteFromImage = window.__originalExtractPaletteFromImage;
    }
  });

  await uploadInput.uploadFile(brokenFixturePath);

  await waitFor(async () => {
    const count = await page.evaluate(() => document.querySelectorAll(".item-card").length);
    return count > 0;
  }, 2000, "broken upload should still render a fallback item card");

  const brokenUploadState = await page.evaluate(() => ({
    itemCount: document.querySelectorAll(".item-card").length,
    firstCardText: document.querySelector(".item-card")?.textContent ?? "",
    paletteCount: document.querySelectorAll(".item-card:first-child .item-palette-button").length,
  }));

  assert.equal(brokenUploadState.itemCount, 1, "broken upload should render exactly one fallback item after clearing");
  assert.match(brokenUploadState.firstCardText, /Could not read colors from this image/i, "broken upload should show the fallback message");
  assert.equal(brokenUploadState.paletteCount, 0, "broken upload should not invent palette swatches");
  assert.deepEqual(pageErrors, [], "page should not throw runtime errors");

  console.log("Browser QA passed.");
} finally {
  await browser.close();
  await new Promise((resolve, reject) => server.close((error) => (error ? reject(error) : resolve())));
}

async function clickSwatch(page, rowId, label) {
  const result = await page.evaluate(({ wantedRowId, wantedLabel }) => {
    const swatchShells = Array.from(document.querySelectorAll(".swatch-shell"));
    const shell = swatchShells.find(
      (node) => node.dataset.rowId === wantedRowId && node.textContent.includes(wantedLabel)
    );

    if (!shell) {
      return false;
    }

    shell.querySelector(".swatch")?.click();
    return true;
  }, { wantedRowId: rowId, wantedLabel: label });

  assert.ok(result, `Expected swatch ${label} in row ${rowId}`);
  await wait(80);
}

async function clickSwatchCompareButton(page, rowId, label, slot) {
  const result = await page.evaluate(({ wantedRowId, wantedLabel, wantedSlot }) => {
    const swatchShells = Array.from(document.querySelectorAll(".swatch-shell"));
    const shell = swatchShells.find(
      (node) => node.dataset.rowId === wantedRowId && node.textContent.includes(wantedLabel)
    );

    if (!shell) {
      return false;
    }

    shell.querySelector(`.swatch-compare-button[data-slot="${wantedSlot}"]`)?.click();
    return true;
  }, { wantedRowId: rowId, wantedLabel: label, wantedSlot: slot });

  assert.ok(result, `Expected compare ${slot.toUpperCase()} button on swatch ${label} in row ${rowId}`);
  await wait(80);
}

async function waitFor(predicate, timeoutMs, message) {
  const start = Date.now();
  while (!(await predicate())) {
    if (Date.now() - start > timeoutMs) {
      throw new Error(message);
    }
    await wait(30);
  }
}

function wait(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function contentTypeFor(filePath) {
  const extension = path.extname(filePath).toLowerCase();
  const contentTypes = {
    ".css": "text/css; charset=utf-8",
    ".html": "text/html; charset=utf-8",
    ".js": "text/javascript; charset=utf-8",
    ".svg": "image/svg+xml",
  };

  return contentTypes[extension] || "application/octet-stream";
}
