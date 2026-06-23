import path from "node:path";
import { fileURLToPath } from "node:url";
import puppeteer from "puppeteer-core";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(__dirname, "..");
const appUrl = "http://127.0.0.1:4321/";
const chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";

const browser = await puppeteer.launch({
  executablePath: chromePath,
  headless: "new",
  args: ["--no-sandbox"],
  defaultViewport: { width: 1440, height: 2200 },
});

try {
  const page = await browser.newPage();
  const consoleMessages = [];
  const pageErrors = [];

  page.on("console", (message) => {
    consoleMessages.push({
      type: message.type(),
      text: message.text(),
    });
  });

  page.on("pageerror", (error) => {
    pageErrors.push({
      message: error.message,
      stack: error.stack,
    });
  });

  await page.goto(appUrl, { waitUntil: "networkidle0" });
  await page.waitForSelector(".row-track");
  await wait(300);

  await page.evaluate(() => {
    window.__debugClicks = [];
    document.addEventListener(
      "click",
      (event) => {
        const target = event.target;
        const closestSwatch = target.closest?.(".swatch");
        const closestScrollButton = target.closest?.(".row-scroll-button");
        window.__debugClicks.push({
          targetClass: target.className || target.tagName,
          swatch: closestSwatch?.dataset.colorId || null,
          scrollDirection: closestScrollButton?.dataset.direction || null,
          comparePickMode: typeof comparePickMode === "undefined" ? "unavailable" : comparePickMode,
        });
      },
      true
    );
  });

  const before = await page.evaluate(() => {
    const track = document.querySelector(".row-track");
    const viewport = document.querySelector(".row-viewport");
    return {
      scrollLeft: track?.scrollLeft ?? null,
      scrollWidth: track?.scrollWidth ?? null,
      clientWidth: track?.clientWidth ?? null,
      offsetWidth: track?.offsetWidth ?? null,
      childCount: track?.children.length ?? null,
      viewportWidth: viewport?.clientWidth ?? null,
      firstSwatchRect: track?.querySelector(".swatch")?.getBoundingClientRect?.() ?? null,
    };
  });

  await page.click('.row-scroll-button[data-direction="1"]');
  await wait(500);

  const afterPageClickArrow = await page.evaluate(() => {
    const track = document.querySelector(".row-track");
    return {
      scrollLeft: track?.scrollLeft ?? null,
    };
  });

  await page.evaluate(() => {
    document.querySelector('.row-scroll-button[data-direction="1"]')?.click();
  });
  await wait(500);

  const afterDomClickArrow = await page.evaluate(() => {
    const track = document.querySelector(".row-track");
    return {
      scrollLeft: track?.scrollLeft ?? null,
    };
  });

  await page.click(".swatch");
  await wait(150);

  const afterPageClickSwatch = await page.evaluate(() => ({
    chips: document.querySelector("#selected-chips")?.textContent ?? "",
    compareA: document.querySelector("#compare-a")?.value ?? "",
    compareB: document.querySelector("#compare-b")?.value ?? "",
  }));

  await page.evaluate(() => {
    document.querySelector(".swatch")?.click();
  });
  await wait(150);

  const afterDomClickSwatch = await page.evaluate(() => ({
    chips: document.querySelector("#selected-chips")?.textContent ?? "",
    compareA: document.querySelector("#compare-a")?.value ?? "",
    compareB: document.querySelector("#compare-b")?.value ?? "",
  }));

  await page.click("#compare-target-a");
  await wait(100);
  await page.click(".swatch");
  await wait(150);

  const afterPickModePageClick = await page.evaluate(() => ({
    chips: document.querySelector("#selected-chips")?.textContent ?? "",
    compareA: document.querySelector("#compare-a")?.value ?? "",
    compareB: document.querySelector("#compare-b")?.value ?? "",
    helper: document.querySelector("#compare-picker-copy")?.textContent ?? "",
  }));

  const debugClicks = await page.evaluate(() => window.__debugClicks || []);

  console.log(JSON.stringify({
    before,
    afterPageClickArrow,
    afterDomClickArrow,
    afterPageClickSwatch,
    afterDomClickSwatch,
    afterPickModePageClick,
    debugClicks,
    consoleMessages,
    pageErrors,
  }, null, 2));
} finally {
  await browser.close();
}

function wait(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
