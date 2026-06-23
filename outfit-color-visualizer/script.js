const rows = [
  {
    id: "purple-focus",
    title: "Purple Focus",
    kicker: "Help With Tricky Shades",
    hint: "Purple gets easier when it meets camel, charcoal, denim, blush, or clean ivory.",
    colors: [
      { name: "Lavender Mist", hex: "#C5B4E3" },
      { name: "Dusty Plum", hex: "#8D6A92" },
      { name: "Lilac", hex: "#B89AD9" },
      { name: "Orchid", hex: "#A56CBF" },
      { name: "Periwinkle", hex: "#7E8EDB" },
      { name: "Mauve Taupe", hex: "#8F7483" },
      { name: "Mulberry", hex: "#6A355B" },
      { name: "Aubergine", hex: "#47263F" },
      { name: "Grape", hex: "#5C3D7A" },
      { name: "Eggplant", hex: "#36263B" },
    ],
  },
  {
    id: "grey-focus",
    title: "Grey Focus",
    kicker: "Help With Tricky Shades",
    hint: "Grey is your easiest bridge color. Push it warm with tan and brown, or sharpen it with black and white.",
    colors: [
      { name: "Silver", hex: "#C4C8CC", neutral: true },
      { name: "Dove Grey", hex: "#A9ADB3", neutral: true },
      { name: "Greige", hex: "#B2A99A", neutral: true },
      { name: "Mushroom", hex: "#8E8174", neutral: true },
      { name: "Slate", hex: "#6B7689", neutral: true },
      { name: "Pewter", hex: "#75706C", neutral: true },
      { name: "Graphite", hex: "#515257", neutral: true },
      { name: "Warm Grey", hex: "#8D867D", neutral: true },
      { name: "Mist", hex: "#D6D9DD", neutral: true },
      { name: "Charcoal", hex: "#3F4146", neutral: true },
    ],
  },
  {
    id: "brown-focus",
    title: "Brown Focus",
    kicker: "Help With Tricky Shades",
    hint: "Brown looks strongest when it gets contrast from ivory, sky blue, forest, burgundy, or brushed gold.",
    colors: [
      { name: "Sand", hex: "#D5BD93", neutral: true },
      { name: "Camel", hex: "#C8A46D", neutral: true },
      { name: "Toffee", hex: "#B57D47" },
      { name: "Cognac", hex: "#965625" },
      { name: "Chestnut", hex: "#7C4C2C" },
      { name: "Walnut", hex: "#6B442F" },
      { name: "Cocoa", hex: "#5A3B2A" },
      { name: "Espresso", hex: "#3C261D" },
      { name: "Taupe", hex: "#9C8676", neutral: true },
      { name: "Terracotta", hex: "#B96442" },
    ],
  },
  {
    id: "tops",
    title: "Tops & Shirts",
    kicker: "Build From The Top",
    hint: "Light tops keep the outfit open. Saturated tops look best when the rest of the outfit gives them room.",
    colors: [
      { name: "Ivory", hex: "#F5F0E8", neutral: true },
      { name: "Sky Blue", hex: "#7BB3D3" },
      { name: "Blush Pink", hex: "#E8A5A5" },
      { name: "Forest", hex: "#3D6B4F" },
      { name: "Camel", hex: "#C8A96E", neutral: true },
      { name: "Dusty Rose", hex: "#C98A8A" },
      { name: "Navy", hex: "#1E2D4E" },
      { name: "Sage", hex: "#8BAF8B" },
      { name: "Lilac", hex: "#B6A0D4" },
      { name: "Stone Grey", hex: "#A8A3A0", neutral: true },
    ],
  },
  {
    id: "bottoms",
    title: "Bottoms & Trousers",
    kicker: "Ground The Look",
    hint: "Most bottoms work hardest as the anchor. If you’re unsure, let trousers stay quieter than the top.",
    colors: [
      { name: "Black", hex: "#1A1A1C", neutral: true },
      { name: "Dark Denim", hex: "#2A3550" },
      { name: "Khaki", hex: "#B5A882", neutral: true },
      { name: "Olive", hex: "#5C6437" },
      { name: "Charcoal", hex: "#3C3C3C", neutral: true },
      { name: "Caramel", hex: "#A0714A" },
      { name: "White", hex: "#F2F2EF", neutral: true },
      { name: "Burgundy", hex: "#7A2035" },
      { name: "Slate Grey", hex: "#68707D", neutral: true },
      { name: "Chocolate", hex: "#553224" },
    ],
  },
  {
    id: "outerwear",
    title: "Outerwear",
    kicker: "Layer With Intention",
    hint: "Jackets and coats decide whether a look feels sharp, soft, or rich. Use them as the third balancing piece.",
    colors: [
      { name: "Camel Coat", hex: "#C49A50", neutral: true },
      { name: "Black", hex: "#151518", neutral: true },
      { name: "Olive Trench", hex: "#6B6B3A" },
      { name: "Tan", hex: "#C8B090", neutral: true },
      { name: "Midnight", hex: "#1A1A38" },
      { name: "Forest Green", hex: "#2D5240" },
      { name: "Charcoal", hex: "#484848", neutral: true },
      { name: "Blush", hex: "#D8A090" },
      { name: "Soft Grey", hex: "#AEB2B6", neutral: true },
      { name: "Oxblood", hex: "#5B2433" },
    ],
  },
  {
    id: "shoes",
    title: "Shoes & Boots",
    kicker: "Finish The Outfit",
    hint: "If the pairing feels loud, let the shoes calm it down. If the pairing feels safe, shoes can add the push.",
    colors: [
      { name: "Black", hex: "#141414", neutral: true },
      { name: "White", hex: "#F0EEEA", neutral: true },
      { name: "Tan Leather", hex: "#A8784A", neutral: true },
      { name: "Cognac", hex: "#8A4A22" },
      { name: "Nude", hex: "#D4AA8A", neutral: true },
      { name: "Dark Brown", hex: "#3C2010" },
      { name: "Red", hex: "#A82828" },
      { name: "Metallic", hex: "#B8A878", neutral: true },
      { name: "Grey Suede", hex: "#8A8E96", neutral: true },
      { name: "Plum", hex: "#5B314E" },
    ],
  },
  {
    id: "accessories",
    title: "Accessories & Bags",
    kicker: "Accent Without Overthinking",
    hint: "Accessories are where you can echo one color, repeat a neutral, or introduce a controlled contrast.",
    colors: [
      { name: "Tan", hex: "#C09868", neutral: true },
      { name: "Black", hex: "#1A1A1A", neutral: true },
      { name: "Ivory", hex: "#F0EAD8", neutral: true },
      { name: "Red", hex: "#B83030" },
      { name: "Emerald", hex: "#1C7A58" },
      { name: "Gold", hex: "#C8A840", neutral: true },
      { name: "Cobalt", hex: "#2444A8" },
      { name: "Blush", hex: "#D89898" },
      { name: "Silver", hex: "#B7BBC0", neutral: true },
      { name: "Chocolate", hex: "#5A3726" },
    ],
  },
];

const filters = [
  { id: "all", label: "All" },
  { id: "complementary", label: "Complementary" },
  { id: "analogous", label: "Analogous" },
  { id: "neutral", label: "Neutral" },
  { id: "monochrome", label: "Monochrome" },
];

const rowsRoot = document.querySelector("#rows-root");
const chipsRoot = document.querySelector("#selected-chips");
const cardsRoot = document.querySelector("#cards-root");
const filterPillsRoot = document.querySelector("#filter-pills");
const clearButton = document.querySelector("#clear-selections");
const itemUploadInput = document.querySelector("#item-upload-input");
const itemCameraInput = document.querySelector("#item-camera-input");
const itemGalleryRoot = document.querySelector("#item-gallery");
const clearItemsButton = document.querySelector("#clear-items");
const compareInputA = document.querySelector("#compare-a");
const compareInputB = document.querySelector("#compare-b");
const compareSwatchA = document.querySelector("#compare-swatch-a");
const compareSwatchB = document.querySelector("#compare-swatch-b");
const comparePreview = document.querySelector("#compare-preview");
const compareLabelA = document.querySelector("#compare-label-a");
const compareLabelB = document.querySelector("#compare-label-b");
const compareVerdict = document.querySelector("#compare-verdict");
const comparePickerCopy = document.querySelector("#compare-picker-copy");
const compareTargetAButton = document.querySelector("#compare-target-a");
const compareTargetBButton = document.querySelector("#compare-target-b");
const compareTargetCancelButton = document.querySelector("#compare-target-cancel");
const compareInputCards = Array.from(document.querySelectorAll(".compare-input"));
const suggestionList = document.querySelector("#color-suggestions");

const rowTemplate = document.querySelector("#row-template");
const swatchTemplate = document.querySelector("#swatch-template");
const cardTemplate = document.querySelector("#card-template");

const selectedIds = new Set();
const uploadedItems = [];
let activeFilter = "all";
let activeCompareSlot = "a";
let comparePickMode = false;
let uploadedItemCount = 0;
let uploadedItemsGeneration = 0;
const knownColorMap = new Map();
const colorById = new Map();
const rowOrder = new Map(rows.map((row, index) => [row.id, index]));
const loopStates = new WeakMap();

bootstrapCatalog();
renderFilters();
renderRows();
renderSuggestionList();
renderSelections();
renderCards();
prefillCompare();
syncCompareTargetUI();
updateCompare();
renderItemGallery();

clearButton.addEventListener("click", () => {
  selectedIds.clear();
  renderSelections();
  syncSwatchState();
  renderCards();
});

rowsRoot.addEventListener("click", handleRowsRootClick);
itemGalleryRoot.addEventListener("click", handleItemGalleryClick);
itemUploadInput.addEventListener("change", handleItemInputChange);
itemCameraInput.addEventListener("change", handleItemInputChange);
clearItemsButton.addEventListener("click", clearUploadedItems);

compareInputA.addEventListener("input", updateCompare);
compareInputB.addEventListener("input", updateCompare);
compareInputA.addEventListener("focus", () => setCompareTarget("a", { pickMode: false }));
compareInputB.addEventListener("focus", () => setCompareTarget("b", { pickMode: false }));
compareInputA.addEventListener("click", () => setCompareTarget("a", { pickMode: false }));
compareInputB.addEventListener("click", () => setCompareTarget("b", { pickMode: false }));
compareTargetAButton.addEventListener("click", () => setCompareTarget("a", { pickMode: true }));
compareTargetBButton.addEventListener("click", () => setCompareTarget("b", { pickMode: true }));
compareTargetCancelButton.addEventListener("click", cancelComparePickMode);

compareInputCards.forEach((card) => {
  const slot = card.dataset.compareSlot;
  card.addEventListener("click", () => setCompareTarget(slot, { pickMode: false }));
});

function bootstrapCatalog() {
  const commonAliases = {
    grey: { name: "Grey", hex: "#8D9096", neutral: true },
    gray: { name: "Grey", hex: "#8D9096", neutral: true },
    purple: { name: "Purple", hex: "#7E57C2" },
    brown: { name: "Brown", hex: "#7B4B31" },
    beige: { name: "Beige", hex: "#D7C4A3", neutral: true },
    cream: { name: "Cream", hex: "#F4ECD8", neutral: true },
    navy: { name: "Navy", hex: "#1E2D4E" },
    olive: { name: "Olive", hex: "#5C6437" },
    white: { name: "White", hex: "#F4F2EE", neutral: true },
    black: { name: "Black", hex: "#181818", neutral: true },
    tan: { name: "Tan", hex: "#C09868", neutral: true },
    camel: { name: "Camel", hex: "#C8A46D", neutral: true },
    sage: { name: "Sage", hex: "#8BAF8B" },
    terracotta: { name: "Terracotta", hex: "#B96442" },
  };

  Object.entries(commonAliases).forEach(([key, value]) => {
    knownColorMap.set(key, buildResolvedColor(value));
  });

  rows.forEach((row) => {
    row.colors.forEach((color) => {
      const colorId = `${row.id}-${slug(color.name)}-${slug(color.hex)}`;
      const enriched = {
        ...color,
        id: colorId,
        rowId: row.id,
        rowTitle: row.title,
      };

      colorById.set(colorId, enriched);
      registerLookup(enriched.name, enriched);
      registerLookup(enriched.hex, enriched);
    });
  });
}

function registerLookup(name, color) {
  const resolved = buildResolvedColor(color);
  knownColorMap.set(normalizeColorToken(name), resolved);
}

function renderSuggestionList() {
  const uniqueNames = Array.from(
    new Set(
      Array.from(colorById.values())
        .map((color) => color.name)
        .sort((left, right) => left.localeCompare(right))
    )
  );

  suggestionList.innerHTML = uniqueNames
    .map((name) => `<option value="${name}"></option>`)
    .join("");
}

function renderFilters() {
  filterPillsRoot.innerHTML = "";

  filters.forEach((filter) => {
    const button = document.createElement("button");
    button.type = "button";
    button.className = `filter-pill${filter.id === activeFilter ? " is-active" : ""}`;
    button.textContent = filter.label;
    button.addEventListener("click", () => {
      activeFilter = filter.id;
      renderFilters();
      renderCards();
      syncSwatchState();
    });
    filterPillsRoot.appendChild(button);
  });
}

function renderRows() {
  rowsRoot.innerHTML = "";

  rows.forEach((row) => {
    const fragment = rowTemplate.content.cloneNode(true);
    const block = fragment.querySelector(".row-block");
    const kicker = fragment.querySelector(".row-kicker");
    const title = fragment.querySelector("h3");
    const hint = fragment.querySelector(".row-hint");
    const track = fragment.querySelector(".row-track");
    const scrollButtons = fragment.querySelectorAll(".row-scroll-button");

    kicker.textContent = row.kicker;
    title.textContent = row.title;
    hint.textContent = row.hint;
    block.dataset.rowId = row.id;

    for (let cloneIndex = 0; cloneIndex < 3; cloneIndex += 1) {
      row.colors.forEach((color) => {
        const swatchShell = swatchTemplate.content.firstElementChild.cloneNode(true);
        const swatchNode = swatchShell.querySelector(".swatch");
        const textColor = readableTextColor(color.hex);
        const colorId = `${row.id}-${slug(color.name)}-${slug(color.hex)}`;

        swatchShell.dataset.colorId = colorId;
        swatchShell.dataset.rowId = row.id;
        swatchNode.dataset.colorId = colorId;
        swatchNode.dataset.rowId = row.id;
        swatchNode.style.background = color.hex;
        swatchNode.style.color = textColor;
        swatchNode.querySelector(".swatch-name").textContent = color.name;
        swatchNode.querySelector(".swatch-hex").textContent = normalizeHex(color.hex);
        const note = swatchNode.querySelector(".swatch-note");
        const focusText = row.id.includes("focus") ? focusNote(color.name, row.id) : "";
        note.textContent = focusText;
        note.hidden = !focusText;
        track.appendChild(swatchShell);
      });
    }

    rowsRoot.appendChild(fragment);
    const appendedBlock = rowsRoot.lastElementChild;
    const appendedTrack = appendedBlock.querySelector(".row-track");
    initializeLoopTrack(appendedTrack);
    scrollButtons.forEach((button) => {
      button.dataset.rowId = row.id;
    });
  });

  syncSwatchState();
}

function handleRowsRootClick(event) {
  const compareButton = event.target.closest(".swatch-compare-button");
  if (compareButton && rowsRoot.contains(compareButton)) {
    const swatchShell = compareButton.closest(".swatch-shell");
    const color = colorById.get(swatchShell?.dataset.colorId || "");
    if (color) {
      fillCompareSlot(compareButton.dataset.slot, color, { pickMode: false });
    }
    return;
  }

  const scrollButton = event.target.closest(".row-scroll-button");
  if (scrollButton && rowsRoot.contains(scrollButton)) {
    const rowBlock = scrollButton.closest(".row-block");
    const track = rowBlock?.querySelector(".row-track");
    if (track) {
      smoothStepTrack(track, Number(scrollButton.dataset.direction));
    }
    return;
  }

  const swatch = event.target.closest(".swatch");
  if (swatch && rowsRoot.contains(swatch)) {
    handleSwatchClick(swatch.dataset.colorId || swatch.closest(".swatch-shell")?.dataset.colorId);
  }
}

function handleItemGalleryClick(event) {
  const paletteButton = event.target.closest(".item-palette-button");
  if (paletteButton && itemGalleryRoot.contains(paletteButton)) {
    fillCompareSlot(activeCompareSlot, {
      name: paletteButton.dataset.colorName || paletteButton.dataset.colorHex,
      hex: paletteButton.dataset.colorHex,
    }, { pickMode: false });
    return;
  }

  const suggestionButton = event.target.closest(".item-suggestion-button");
  if (suggestionButton && itemGalleryRoot.contains(suggestionButton)) {
    fillCompareSlot(activeCompareSlot, colorById.get(suggestionButton.dataset.colorId), { pickMode: false });
  }
}

function focusNote(name, rowId) {
  const notes = {
    "purple-focus": {
      lavender: "Easy with tan",
      plum: "Strong with grey",
      lilac: "Soft with ivory",
      orchid: "Try charcoal",
      periwinkle: "Great with camel",
      mauve: "Use denim",
      mulberry: "Add blush",
      aubergine: "Ground with black",
      grape: "Works with olive",
      eggplant: "Sharp with cream",
    },
    "grey-focus": {
      silver: "Pair with navy",
      dove: "Soft with blush",
      greige: "Use forest",
      mushroom: "Great with plum",
      slate: "Easy with camel",
      pewter: "Try ivory",
      graphite: "Works with white",
      warm: "Use cognac",
      mist: "Fresh with blue",
      charcoal: "Strong with tan",
    },
    "brown-focus": {
      sand: "Add sky blue",
      camel: "Easy with navy",
      toffee: "Try cream",
      cognac: "Works with grey",
      chestnut: "Use blush",
      walnut: "Great with forest",
      cocoa: "Sharp with ivory",
      espresso: "Add gold",
      taupe: "Soft with lilac",
      terracotta: "Pair with denim",
    },
  };

  const key = normalizeColorToken(name).split("-")[0];
  return notes[rowId][key] || "Easy accent";
}

function initializeLoopTrack(track) {
  const state = {
    pointerId: null,
    pointerStartedOnColorId: null,
    pointerStartedOnCompareButton: false,
    pointerStartedOnCompareSlot: null,
    pointerStartedOnCompareColorId: null,
    lastX: 0,
    lastTime: 0,
    velocity: 0,
    rafId: 0,
    dragDistance: 0,
    suppressClick: false,
    segmentWidth: 0,
    stepDistance: 184,
  };

  loopStates.set(track, state);

  const recalc = () => {
    state.segmentWidth = track.scrollWidth / 3;
    state.stepDistance = getTrackStepDistance(track);
    if (state.segmentWidth > 0 && track.scrollLeft === 0) {
      track.scrollLeft = state.segmentWidth;
    }
  };

  requestAnimationFrame(recalc);
  window.setTimeout(recalc, 0);
  window.setTimeout(recalc, 120);
  new ResizeObserver(recalc).observe(track);

  track.addEventListener("scroll", () => normalizeLoopPosition(track));
  track.addEventListener("wheel", (event) => {
    if (Math.abs(event.deltaY) > Math.abs(event.deltaX)) {
      track.scrollLeft += event.deltaY;
      normalizeLoopPosition(track);
      event.preventDefault();
    }
  }, { passive: false });

  track.addEventListener("pointerdown", (event) => {
    if (event.button !== 0) {
      return;
    }

    cancelMomentum(state);
    state.pointerId = event.pointerId;
    const compareButton = event.target.closest(".swatch-compare-button");
    state.pointerStartedOnCompareButton = Boolean(compareButton);
    state.pointerStartedOnCompareSlot = compareButton?.dataset.slot || null;
    state.pointerStartedOnCompareColorId =
      compareButton?.closest(".swatch-shell")?.dataset.colorId || null;
    state.pointerStartedOnColorId =
      state.pointerStartedOnCompareButton
        ? null
        : event.target.closest(".swatch")?.dataset.colorId ||
          event.target.closest(".swatch-shell")?.dataset.colorId ||
      null;
    state.lastX = event.clientX;
    state.lastTime = performance.now();
    state.velocity = 0;
    state.dragDistance = 0;
    state.suppressClick = false;
    track.classList.add("is-grabbing");
    track.setPointerCapture(event.pointerId);
  });

  track.addEventListener("pointermove", (event) => {
    if (state.pointerId !== event.pointerId) {
      return;
    }

    const now = performance.now();
    const deltaX = event.clientX - state.lastX;
    const deltaTime = Math.max(1, now - state.lastTime);

    track.scrollLeft -= deltaX;
    normalizeLoopPosition(track);

    state.velocity = state.velocity * 0.8 + (-deltaX / deltaTime) * 0.2;
    state.lastX = event.clientX;
    state.lastTime = now;
    state.dragDistance += Math.abs(deltaX);
    state.suppressClick = state.dragDistance > 8;
  });

  const releasePointer = (event) => {
    if (state.pointerId !== event.pointerId) {
      return;
    }

    track.classList.remove("is-grabbing");
    if (track.hasPointerCapture(event.pointerId)) {
      track.releasePointerCapture(event.pointerId);
    }
    state.pointerId = null;

    const tappedColorId = state.dragDistance <= 8 ? state.pointerStartedOnColorId : null;
    const tappedCompareSlot = state.dragDistance <= 8 ? state.pointerStartedOnCompareSlot : null;
    const tappedCompareColorId = state.dragDistance <= 8 ? state.pointerStartedOnCompareColorId : null;
    state.pointerStartedOnColorId = null;
    state.pointerStartedOnCompareButton = false;
    state.pointerStartedOnCompareSlot = null;
    state.pointerStartedOnCompareColorId = null;

    if (Math.abs(state.velocity) > 0.02 && state.dragDistance > 8) {
      startMomentum(track, state);
    } else {
      state.velocity = 0;
      state.suppressClick = false;
      if (tappedCompareSlot) {
        const color = colorById.get(tappedCompareColorId || "");
        if (color) {
          fillCompareSlot(tappedCompareSlot, color, { pickMode: false });
        }
        return;
      }
      if (tappedColorId) {
        handleSwatchClick(tappedColorId);
      }
    }
  };

  track.addEventListener("pointerup", releasePointer);
  track.addEventListener("pointercancel", releasePointer);
  track.addEventListener(
    "click",
    (event) => {
      if (state.suppressClick) {
        event.preventDefault();
        event.stopPropagation();
        window.setTimeout(() => {
          state.suppressClick = false;
        }, 0);
      }
    },
    true
  );
}

function normalizeLoopPosition(track) {
  const state = loopStates.get(track);
  if (!state || !state.segmentWidth) {
    return;
  }

  if (track.scrollLeft < state.segmentWidth * 0.5) {
    track.scrollLeft += state.segmentWidth;
  } else if (track.scrollLeft > state.segmentWidth * 1.5) {
    track.scrollLeft -= state.segmentWidth;
  }
}

function startMomentum(track, state) {
  cancelMomentum(state);

  let previousTime = performance.now();
  const step = (now) => {
    const deltaTime = now - previousTime;
    previousTime = now;

    track.scrollLeft += state.velocity * deltaTime;
    normalizeLoopPosition(track);
    state.velocity *= Math.pow(0.93, deltaTime / 16);

    if (Math.abs(state.velocity) > 0.015) {
      state.rafId = requestAnimationFrame(step);
    } else {
      state.rafId = 0;
      state.velocity = 0;
      state.suppressClick = false;
    }
  };

  state.rafId = requestAnimationFrame(step);
}

function smoothStepTrack(track, direction) {
  const state = loopStates.get(track);
  if (!state) {
    return;
  }

  cancelMomentum(state);

  const distance = (state.stepDistance || getTrackStepDistance(track)) * direction;
  if (typeof track.scrollBy === "function") {
    track.scrollBy({ left: distance, behavior: "smooth" });
  } else {
    track.scrollLeft += distance;
  }

  const finalize = () => {
    normalizeLoopPosition(track);
    state.rafId = 0;
    state.velocity = 0;
    state.suppressClick = false;
  };

  state.rafId = requestAnimationFrame(() => {
    window.setTimeout(finalize, 340);
  });
}

function cancelMomentum(state) {
  if (state.rafId) {
    cancelAnimationFrame(state.rafId);
    state.rafId = 0;
  }
}

function getTrackStepDistance(track) {
  const swatch = track.querySelector(".swatch");
  if (!swatch) {
    return 180;
  }

  const styles = getComputedStyle(track);
  const gapValue = Number.parseFloat(styles.gap || styles.columnGap || "0");
  return swatch.getBoundingClientRect().width + gapValue;
}

function toggleSelection(colorId) {
  if (selectedIds.has(colorId)) {
    selectedIds.delete(colorId);
  } else {
    selectedIds.add(colorId);
  }

  renderSelections();
  syncSwatchState();
  renderCards();
}

function handleSwatchClick(colorId) {
  if (comparePickMode) {
    fillCompareSlot(activeCompareSlot, colorById.get(colorId), { pickMode: false });
    return;
  }

  toggleSelection(colorId);
}

function fillCompareSlot(slot, color, options = {}) {
  if (!color) {
    return;
  }

  if (slot === "a") {
    compareInputA.value = color.name;
  } else {
    compareInputB.value = color.name;
  }

  setCompareTarget(slot, { pickMode: Boolean(options.pickMode) });

  updateCompare();
}

function setCompareTarget(slot, options = {}) {
  activeCompareSlot = slot;
  comparePickMode = Boolean(options.pickMode);
  syncCompareTargetUI();
}

function cancelComparePickMode() {
  comparePickMode = false;
  syncCompareTargetUI();
}

function syncCompareTargetUI() {
  compareTargetAButton.classList.toggle("is-active", activeCompareSlot === "a");
  compareTargetBButton.classList.toggle("is-active", activeCompareSlot === "b");
  compareTargetCancelButton.classList.toggle("is-active", comparePickMode);

  compareInputCards.forEach((card) => {
    card.classList.toggle("is-active-target", card.dataset.compareSlot === activeCompareSlot);
  });

  if (!comparePickMode) {
    comparePickerCopy.textContent =
      activeCompareSlot === "a"
        ? "Use tile `A`, or press `Next for A`."
        : "Use tile `B`, or press `Next for B`.";
    return;
  }

  comparePickerCopy.textContent =
    activeCompareSlot === "a"
      ? "Next tile tap fills A."
      : "Next tile tap fills B.";
}

async function handleItemInputChange(event) {
  const files = Array.from(event.target.files || []);
  if (!files.length) {
    return;
  }

  const generation = uploadedItemsGeneration;

  const nextItems = await Promise.all(files.map((file) => buildUploadedItem(file)));

  if (generation !== uploadedItemsGeneration) {
    nextItems.forEach(revokeUploadedItemResources);
    event.target.value = "";
    return;
  }

  uploadedItems.unshift(...nextItems);
  renderItemGallery();
  event.target.value = "";
}

function clearUploadedItems() {
  uploadedItemsGeneration += 1;
  uploadedItems.splice(0).forEach(revokeUploadedItemResources);
  renderItemGallery();
}

async function buildUploadedItem(file) {
  const itemNumber = ++uploadedItemCount;
  const previewUrl = URL.createObjectURL(file);
  const fallbackName = file.name || `Item ${itemNumber}`;
  const fallbackColor = buildResolvedColor({ name: "Uploaded item", hex: "#8D9096", neutral: true });

  try {
    const paletteHexes = await extractPaletteFromImage(previewUrl);
    const palette = paletteHexes.map((hex, index) => {
      const nearest = findNearestCatalogColor(hex);
      const label = nearest.distance <= 54 ? nearest.name : `Picked tone ${index + 1}`;

      return {
        id: `palette-${itemNumber}-${index}`,
        hex,
        name: label,
        resolved: buildResolvedColor({ name: label, hex }),
        nearestColorId: nearest.id,
      };
    });

    const dominantColor = palette[0]?.resolved || fallbackColor;
    const suggestions = buildItemSuggestions(dominantColor);

    return {
      id: `upload-${itemNumber}`,
      name: fallbackName,
      previewUrl,
      palette,
      dominantColor,
      suggestions,
      error: null,
    };
  } catch (error) {
    console.error("Could not process item image.", error);

    return {
      id: `upload-${itemNumber}`,
      name: fallbackName,
      previewUrl,
      palette: [],
      dominantColor: fallbackColor,
      suggestions: buildItemSuggestions(fallbackColor),
      error: "Could not read colors from this image. Try another photo with clearer lighting.",
    };
  }
}

async function extractPaletteFromImage(imageUrl) {
  const image = await loadImage(imageUrl);
  const canvas = document.createElement("canvas");
  const context = canvas.getContext("2d", { willReadFrequently: true });
  const maxSide = 140;
  const scale = Math.min(1, maxSide / Math.max(image.naturalWidth || image.width, image.naturalHeight || image.height));
  const width = Math.max(1, Math.round((image.naturalWidth || image.width) * scale));
  const height = Math.max(1, Math.round((image.naturalHeight || image.height) * scale));

  canvas.width = width;
  canvas.height = height;
  context.drawImage(image, 0, 0, width, height);

  const { data } = context.getImageData(0, 0, width, height);
  const buckets = new Map();

  for (let index = 0; index < data.length; index += 16) {
    const alpha = data[index + 3];
    if (alpha < 160) {
      continue;
    }

    const red = data[index];
    const green = data[index + 1];
    const blue = data[index + 2];
    const quantized = quantizeRgb(red, green, blue);
    const key = `${quantized.r}-${quantized.g}-${quantized.b}`;
    buckets.set(key, (buckets.get(key) || 0) + 1);
  }

  return Array.from(buckets.entries())
    .sort((left, right) => right[1] - left[1])
    .map(([key]) => {
      const [red, green, blue] = key.split("-").map(Number);
      return rgbToHex(red, green, blue);
    })
    .filter((hex, index, list) => list.findIndex((candidate) => colorDistance(hex, candidate) < 28) === index)
    .slice(0, 5);
}

function loadImage(imageUrl) {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.onerror = () => reject(new Error("Image failed to load"));
    image.src = imageUrl;
  });
}

function quantizeRgb(r, g, b) {
  const step = 24;
  return {
    r: Math.min(255, Math.round(r / step) * step),
    g: Math.min(255, Math.round(g / step) * step),
    b: Math.min(255, Math.round(b / step) * step),
  };
}

function renderItemGallery() {
  if (uploadedItems.length === 0) {
    itemGalleryRoot.className = "item-gallery empty";
    itemGalleryRoot.textContent = "Upload one item to pull colors.";
    return;
  }

  itemGalleryRoot.className = "item-gallery";
  itemGalleryRoot.innerHTML = "";

  uploadedItems.forEach((item) => {
    const card = document.createElement("article");
    card.className = "item-card";
    const safeItemName = escapeHtml(item.name);
    const safeDominantName = escapeHtml(item.dominantColor?.name || "Uploaded item");
    const safeError = escapeHtml(item.error || "No palette detected yet.");

    const paletteMarkup = item.palette.length
      ? item.palette
          .map((paletteColor) => `
            <button
              class="item-palette-button"
              type="button"
              data-color-hex="${paletteColor.hex}"
              data-color-name="${paletteColor.name}"
              style="--item-color:${paletteColor.hex};"
            >
              <span class="item-palette-dot"></span>
              <span>${escapeHtml(paletteColor.name)}</span>
              <small>${escapeHtml(paletteColor.hex)}</small>
            </button>
          `)
          .join("")
      : `<div class="item-card-empty">${safeError}</div>`;

    const suggestionMarkup = item.suggestions.length
      ? item.suggestions
          .map((suggestion) => `
            <button
              class="item-suggestion-button"
              type="button"
              data-color-id="${suggestion.color.id}"
            >
              <span class="item-suggestion-dot" style="background:${suggestion.color.hex}"></span>
              <span>${escapeHtml(suggestion.color.name)}</span>
              <small>${escapeHtml(suggestion.color.rowTitle)}</small>
            </button>
          `)
          .join("")
      : `<div class="item-card-empty">Suggestions will appear once the item colors are extracted.</div>`;

    card.innerHTML = `
      <div class="item-card-media">
        <img src="${item.previewUrl}" alt="${safeItemName}" />
      </div>
      <div class="item-card-body">
        <div class="item-card-head">
          <div>
            <p class="row-kicker">Uploaded Item</p>
            <h3>${safeItemName}</h3>
          </div>
          <div class="item-card-main-color">
            <span>Main tone</span>
            <strong style="color:${readableTextColor(item.dominantColor?.hex || fallbackUploadedItemColor().hex)}; background:${item.dominantColor?.hex || fallbackUploadedItemColor().hex};">
              ${safeDominantName}
            </strong>
          </div>
        </div>
        <div class="item-card-section">
          <span class="item-card-label">Extracted palette</span>
          <div class="item-palette-grid">${paletteMarkup}</div>
        </div>
        <div class="item-card-section">
          <span class="item-card-label">Best next pieces</span>
          <div class="item-suggestion-grid">${suggestionMarkup}</div>
        </div>
      </div>
    `;

    itemGalleryRoot.appendChild(card);
  });
}

function revokeUploadedItemResources(item) {
  if (item.previewUrl) {
    URL.revokeObjectURL(item.previewUrl);
  }
}

function fallbackUploadedItemColor() {
  return buildResolvedColor({ name: "Uploaded item", hex: "#8D9096", neutral: true });
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function buildItemSuggestions(baseColor) {
  return rows
    .filter((row) => ["tops", "bottoms", "outerwear", "shoes", "accessories"].includes(row.id))
    .map((row) => {
      const best = row.colors
        .map((color) => colorById.get(`${row.id}-${slug(color.name)}-${slug(color.hex)}`))
        .map((color) => ({ color, analysis: analyzePair(baseColor, color) }))
        .sort((left, right) => right.analysis.score - left.analysis.score)[0];

      return best;
    })
    .filter(Boolean)
    .sort((left, right) => right.analysis.score - left.analysis.score)
    .slice(0, 4);
}

function findNearestCatalogColor(hex) {
  const resolved = buildResolvedColor({ name: hex, hex });
  let nearest = null;

  Array.from(colorById.values()).forEach((color) => {
    const distance = colorDistance(resolved.hex, color.hex);
    if (!nearest || distance < nearest.distance) {
      nearest = { id: color.id, name: color.name, distance };
    }
  });

  return nearest || { id: null, name: hex, distance: Number.POSITIVE_INFINITY };
}

function renderSelections() {
  if (selectedIds.size === 0) {
    chipsRoot.className = "chips empty";
    chipsRoot.textContent = "Tap colors to pin them.";
    return;
  }

  chipsRoot.className = "chips";
  chipsRoot.innerHTML = "";

  const sortedSelections = Array.from(selectedIds)
    .map((id) => colorById.get(id))
    .sort((left, right) => {
      const rowDelta = rowOrder.get(left.rowId) - rowOrder.get(right.rowId);
      return rowDelta || left.name.localeCompare(right.name);
    });

  sortedSelections.forEach((color) => {
    const chip = document.createElement("div");
    chip.className = "chip";
    chip.innerHTML = `
      <span class="chip-dot" style="background:${color.hex}"></span>
      <span>${color.name} · ${color.rowTitle}</span>
      <button type="button" aria-label="Remove ${color.name}">×</button>
    `;
    chip.querySelector("button").addEventListener("click", () => toggleSelection(color.id));
    chipsRoot.appendChild(chip);
  });
}

function syncSwatchState() {
  const anchor = selectedIds.size === 1 ? colorById.get(Array.from(selectedIds)[0]) : null;

  document.querySelectorAll(".swatch-shell").forEach((swatchShell) => {
    const swatch = swatchShell.querySelector(".swatch");
    const color = colorById.get(swatchShell.dataset.colorId);
    const isSelected = selectedIds.has(color.id);

    swatch.classList.toggle("is-selected", isSelected);
    swatch.classList.remove("is-match", "is-muted");

    if (anchor && anchor.id !== color.id) {
      const analysis = analyzePair(anchor, color);
      swatch.classList.add(analysis.score >= 62 ? "is-match" : "is-muted");
    }
  });
}

function renderCards() {
  const selectedColors = Array.from(selectedIds)
    .map((id) => colorById.get(id))
    .sort((left, right) => rowOrder.get(left.rowId) - rowOrder.get(right.rowId));

  const cards = [];

  for (let leftIndex = 0; leftIndex < selectedColors.length; leftIndex += 1) {
    for (let rightIndex = leftIndex + 1; rightIndex < selectedColors.length; rightIndex += 1) {
      const left = selectedColors[leftIndex];
      const right = selectedColors[rightIndex];

      if (left.rowId === right.rowId) {
        continue;
      }

      const analysis = analyzePair(left, right);
      if (!passesFilter(analysis)) {
        continue;
      }

      cards.push({ left, right, analysis });
    }
  }

  if (cards.length === 0) {
    cardsRoot.className = "cards-grid empty";
    cardsRoot.textContent =
      selectedColors.length < 2
        ? "Pick colors from two rows."
        : "No matches for this filter.";
    return;
  }

  cardsRoot.className = "cards-grid";
  cardsRoot.innerHTML = "";

  cards
    .sort((left, right) => right.analysis.score - left.analysis.score)
    .forEach(({ left, right, analysis }) => {
      const card = cardTemplate.content.firstElementChild.cloneNode(true);
      const labels = card.querySelector(".pair-card-labels");
      const pill = card.querySelector(".pair-card-pill");
      const swatches = card.querySelectorAll(".pair-card-swatch");
      const copy = card.querySelector(".pair-card-copy");
      const suggestions = card.querySelector(".pair-card-suggestions");

      labels.innerHTML = `<span>${left.rowTitle}</span><span>${right.rowTitle}</span>`;
      pill.textContent = analysis.label;
      pill.style.borderColor = `${analysis.accent}55`;
      pill.style.background = `${analysis.accent}22`;
      pill.style.color = analysis.accentText;

      [left, right].forEach((color, index) => {
        swatches[index].querySelector(".pair-card-color").style.background = color.hex;
        swatches[index].querySelector(".pair-card-name").textContent = color.name;
        swatches[index].querySelector(".pair-card-meta").textContent = `${normalizeHex(color.hex)} · ${color.rowTitle}`;
      });

      copy.textContent = analysis.reason;
      suggestions.innerHTML = "";
      analysis.suggestions.forEach((suggestion) => {
        const chip = document.createElement("span");
        chip.className = "suggestion-chip";
        chip.textContent = suggestion;
        suggestions.appendChild(chip);
      });

      cardsRoot.appendChild(card);
    });
}

function passesFilter(analysis) {
  if (activeFilter === "all") {
    return true;
  }

  return analysis.tags.includes(activeFilter);
}

function prefillCompare() {
  compareInputA.value = "Sky Blue";
  compareInputB.value = "Tan";
}

function updateCompare() {
  const left = resolveColorInput(compareInputA.value);
  const right = resolveColorInput(compareInputB.value);

  updateInputPreview(compareSwatchA, left);
  updateInputPreview(compareSwatchB, right);

  if (!left || !right) {
    comparePreview.style.opacity = "0.55";
    comparePreview.children[0].style.background = left?.hex || "#222";
    comparePreview.children[1].style.background = right?.hex || "#222";
    compareLabelA.innerHTML = left ? `${left.name}<small>${left.hex}</small>` : "Color A";
    compareLabelB.innerHTML = right ? `${right.name}<small>${right.hex}</small>` : "Color B";
    compareVerdict.className = "verdict-card empty";
    compareVerdict.textContent = "Enter two colors.";
    return;
  }

  comparePreview.style.opacity = "1";
  comparePreview.children[0].style.background = left.hex;
  comparePreview.children[1].style.background = right.hex;
  compareLabelA.style.color = readableTextColor(left.hex);
  compareLabelB.style.color = readableTextColor(right.hex);
  compareLabelA.innerHTML = `${left.name}<small>${left.hex}</small>`;
  compareLabelB.innerHTML = `${right.name}<small>${right.hex}</small>`;

  const analysis = analyzePair(left, right);
  compareVerdict.className = "verdict-card";
  compareVerdict.innerHTML = `
    <div class="verdict-top">
      <span class="verdict-badge" style="border-color:${analysis.accent}55;background:${analysis.accent}22;color:${analysis.accentText};">
        ${analysis.label}
      </span>
      <strong>${left.name} + ${right.name}</strong>
    </div>
    <div>${analysis.reason}</div>
    <div class="pair-card-suggestions" style="margin-top:14px;">
      ${analysis.suggestions.map((item) => `<span class="suggestion-chip">${item}</span>`).join("")}
    </div>
  `;
}

function updateInputPreview(node, color) {
  node.style.background = color?.hex || "transparent";
  node.style.borderColor = color ? "rgba(255,255,255,0.22)" : "rgba(255,255,255,0.12)";
}

function resolveColorInput(input) {
  const trimmed = input.trim();
  if (!trimmed) {
    return null;
  }

  const normalized = normalizeColorToken(trimmed);
  if (knownColorMap.has(normalized)) {
    return knownColorMap.get(normalized);
  }

  if (/^#?[0-9a-f]{3}([0-9a-f]{3})?$/i.test(trimmed)) {
    const hex = normalizeHex(trimmed);
    return buildResolvedColor({ name: hex, hex, neutral: inferNeutralFromHex(hex) });
  }

  return null;
}

function buildResolvedColor(color) {
  const hex = normalizeHex(color.hex);
  return {
    name: color.name,
    hex,
    neutral: Boolean(color.neutral) || inferNeutralFromHex(hex),
    metrics: getColorMetrics(hex),
  };
}

function analyzePair(left, right) {
  const leftResolved = left.metrics ? left : buildResolvedColor(left);
  const rightResolved = right.metrics ? right : buildResolvedColor(right);
  const hueGap = hueDistance(leftResolved.metrics.h, rightResolved.metrics.h);
  const lightnessGap = Math.abs(leftResolved.metrics.l - rightResolved.metrics.l);
  const contrast = contrastRatio(leftResolved.hex, rightResolved.hex);
  const bothNeutral = leftResolved.neutral && rightResolved.neutral;
  const oneNeutral = leftResolved.neutral || rightResolved.neutral;
  const isMonochrome = hueGap <= 14 && !bothNeutral;
  const isComplementary = hueGap >= 150 && hueGap <= 210;
  const isAnalogous = hueGap >= 14 && hueGap <= 42;
  const tags = [];

  if (oneNeutral) {
    tags.push("neutral");
  }
  if (isMonochrome) {
    tags.push("monochrome");
  }
  if (isComplementary) {
    tags.push("complementary");
  }
  if (isAnalogous) {
    tags.push("analogous");
  }

  let mode = "balanced";
  let label = "Balanced";
  let accent = "#8BAF8B";
  let accentText = "#d9f3dc";
  let reason =
    "The tones create enough separation to feel styled, but not so much that the outfit feels busy.";
  let suggestions = [
    "Repeat one color in the bag",
    "Keep metal simple",
    "Let one piece lead",
  ];
  let score = 58;

  if (bothNeutral || oneNeutral) {
    mode = "neutral";
    label = "Neutral";
    accent = "#D7AE73";
    accentText = "#ffe7be";
    reason =
      "A neutral is doing the grounding here, which makes the second color easier to wear and much harder to miss.";
    suggestions = [
      "Echo the neutral in shoes or a bag",
      "Add texture instead of another color",
      "Use gold or silver if you want lift",
    ];
    score = 74 + Math.min(10, contrast * 2);
  }

  if (isMonochrome) {
    mode = "monochrome";
    label = "Monochrome";
    accent = "#9F86C0";
    accentText = "#efe5ff";
    reason =
      "These shades stay in the same hue family, so the look reads polished and easy while the lightness difference keeps it from feeling flat.";
    suggestions = [
      "Break it up with ivory",
      "Use texture to separate pieces",
      "Keep hardware minimal",
    ];
    score = 82 - Math.max(0, lightnessGap - 36) * 0.25;
  } else if (isComplementary) {
    mode = "complementary";
    label = "Complementary";
    accent = "#C65252";
    accentText = "#ffe0e0";
    reason =
      "These sit opposite on the wheel, so the contrast feels intentional and bold. Let one lead and let the other act like the accent.";
    suggestions = [
      "Add black to ground it",
      "Ivory softens both",
      "Use tan leather to tie it together",
    ];
    score = 88 + Math.min(6, contrast);
  } else if (isAnalogous) {
    mode = "analogous";
    label = "Analogous";
    accent = "#69985D";
    accentText = "#e5ffde";
    reason =
      "The hues sit close together, so the outfit feels smooth and pulled together without the colors competing for attention.";
    suggestions = [
      "Keep the third piece creamy or tonal",
      "Use a quiet shoe color",
      "Let accessories repeat one shade",
    ];
    score = 78 - Math.abs(lightnessGap - 24) * 0.3;
  }

  if (leftResolved.name.toLowerCase().includes("purple") || rightResolved.name.toLowerCase().includes("purple") || leftResolved.name.toLowerCase().includes("plum") || rightResolved.name.toLowerCase().includes("plum")) {
    suggestions = uniqueSuggestions([
      "Camel or charcoal makes purple easier",
      "Denim keeps it casual",
      ...suggestions,
    ]);
  }

  if (leftResolved.name.toLowerCase().includes("grey") || rightResolved.name.toLowerCase().includes("grey") || leftResolved.name.toLowerCase().includes("gray") || rightResolved.name.toLowerCase().includes("gray")) {
    suggestions = uniqueSuggestions([
      "White keeps grey crisp",
      "Tan warms grey fast",
      ...suggestions,
    ]);
  }

  if (leftResolved.name.toLowerCase().includes("brown") || rightResolved.name.toLowerCase().includes("brown") || leftResolved.name.toLowerCase().includes("camel") || rightResolved.name.toLowerCase().includes("camel") || leftResolved.name.toLowerCase().includes("cognac") || rightResolved.name.toLowerCase().includes("cognac")) {
    suggestions = uniqueSuggestions([
      "Ivory lifts brown",
      "Sky blue opens it up",
      ...suggestions,
    ]);
  }

  return {
    mode,
    label,
    tags,
    accent,
    accentText,
    reason,
    suggestions: uniqueSuggestions(suggestions).slice(0, 3),
    score,
  };
}

function uniqueSuggestions(items) {
  return Array.from(new Set(items));
}

function inferNeutralFromHex(hex) {
  const metrics = getColorMetrics(hex);
  return metrics.s < 18 || metrics.l > 92 || metrics.l < 11 || (metrics.h >= 25 && metrics.h <= 50 && metrics.s < 40 && metrics.l > 45);
}

function getColorMetrics(hex) {
  const { r, g, b } = hexToRgb(hex);
  return rgbToHsl(r, g, b);
}

function readableTextColor(hex) {
  return relativeLuminance(hex) > 0.45 ? "#111111" : "#f7f4ef";
}

function relativeLuminance(hex) {
  const { r, g, b } = hexToRgb(hex);
  const channel = (value) => {
    const normalized = value / 255;
    return normalized <= 0.03928 ? normalized / 12.92 : ((normalized + 0.055) / 1.055) ** 2.4;
  };

  return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
}

function contrastRatio(leftHex, rightHex) {
  const leftLum = relativeLuminance(leftHex);
  const rightLum = relativeLuminance(rightHex);
  const lighter = Math.max(leftLum, rightLum);
  const darker = Math.min(leftLum, rightLum);
  return (lighter + 0.05) / (darker + 0.05);
}

function hueDistance(leftHue, rightHue) {
  const raw = Math.abs(leftHue - rightHue);
  return Math.min(raw, 360 - raw);
}

function rgbToHsl(r, g, b) {
  const red = r / 255;
  const green = g / 255;
  const blue = b / 255;
  const max = Math.max(red, green, blue);
  const min = Math.min(red, green, blue);
  const lightness = (max + min) / 2;
  const delta = max - min;
  let hue = 0;
  let saturation = 0;

  if (delta !== 0) {
    saturation =
      lightness > 0.5 ? delta / (2 - max - min) : delta / (max + min);

    switch (max) {
      case red:
        hue = (green - blue) / delta + (green < blue ? 6 : 0);
        break;
      case green:
        hue = (blue - red) / delta + 2;
        break;
      default:
        hue = (red - green) / delta + 4;
        break;
    }
  }

  return {
    h: Math.round(hue * 60),
    s: Math.round(saturation * 100),
    l: Math.round(lightness * 100),
  };
}

function hexToRgb(hex) {
  const normalized = normalizeHex(hex).replace("#", "");
  const value = normalized.length === 3
    ? normalized.split("").map((digit) => digit + digit).join("")
    : normalized;

  return {
    r: Number.parseInt(value.slice(0, 2), 16),
    g: Number.parseInt(value.slice(2, 4), 16),
    b: Number.parseInt(value.slice(4, 6), 16),
  };
}

function rgbToHex(r, g, b) {
  const channel = (value) => Math.max(0, Math.min(255, value)).toString(16).padStart(2, "0");
  return `#${channel(r)}${channel(g)}${channel(b)}`.toUpperCase();
}

function colorDistance(leftHex, rightHex) {
  const left = hexToRgb(leftHex);
  const right = hexToRgb(rightHex);

  return Math.sqrt(
    ((left.r - right.r) ** 2) * 0.3 +
    ((left.g - right.g) ** 2) * 0.59 +
    ((left.b - right.b) ** 2) * 0.11
  );
}

function normalizeHex(value) {
  const trimmed = value.trim().replace("#", "");
  if (trimmed.length === 3) {
    return `#${trimmed
      .split("")
      .map((digit) => `${digit}${digit}`)
      .join("")
      .toUpperCase()}`;
  }
  return `#${trimmed.toUpperCase()}`;
}

function normalizeColorToken(value) {
  return value
    .trim()
    .toLowerCase()
    .replace(/#/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "");
}

function slug(value) {
  return normalizeColorToken(value);
}
