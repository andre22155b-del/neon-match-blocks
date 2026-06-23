const CONTEXT_MENU_SAVE_PAGE = "addem-save-page";
const CONTEXT_MENU_SAVE_SELECTION = "addem-save-selection";
const STORAGE_PENDING_KEY = "pendingCaptures";

const DEFAULT_SETTINGS = {
  endpointUrl: "",
  authToken: "",
  openAddEmAfterSave: false,
  addEmSchemeBase: "addem://capture"
};

chrome.runtime.onInstalled.addListener(async () => {
  await createContextMenus();
  await updateBadge();
});

chrome.runtime.onStartup.addListener(async () => {
  await updateBadge();
});

chrome.commands.onCommand.addListener(async (command) => {
  if (command !== "save-page-to-addem") {
    return;
  }

  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  if (!tab) {
    return;
  }

  const capture = makeCapture({
    title: tab.title || "Untitled Page",
    url: tab.url || "",
    selectedText: "",
    source: "command"
  });

  await handleCapture(capture);
});

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  if (!tab) {
    return;
  }

  if (info.menuItemId !== CONTEXT_MENU_SAVE_PAGE && info.menuItemId !== CONTEXT_MENU_SAVE_SELECTION) {
    return;
  }

  const selectedText = typeof info.selectionText === "string" ? info.selectionText : "";
  const capture = makeCapture({
    title: tab.title || "Untitled Page",
    url: tab.url || "",
    selectedText,
    source: info.menuItemId === CONTEXT_MENU_SAVE_SELECTION ? "context-selection" : "context-page"
  });

  await handleCapture(capture);
});

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  if (!message || typeof message.type !== "string") {
    sendResponse({ ok: false, error: "Invalid message." });
    return;
  }

  if (message.type === "addem-save-capture") {
    handleCapture(message.capture)
      .then((result) => sendResponse({ ok: true, result }))
      .catch((error) => sendResponse({ ok: false, error: String(error) }));
    return true;
  }

  if (message.type === "addem-sync-pending") {
    syncPendingCaptures()
      .then((result) => sendResponse({ ok: true, result }))
      .catch((error) => sendResponse({ ok: false, error: String(error) }));
    return true;
  }

  if (message.type === "addem-get-status") {
    getStatus()
      .then((status) => sendResponse({ ok: true, status }))
      .catch((error) => sendResponse({ ok: false, error: String(error) }));
    return true;
  }

  if (message.type === "addem-open-app") {
    openAddEmApp(message.capture)
      .then(() => sendResponse({ ok: true }))
      .catch((error) => sendResponse({ ok: false, error: String(error) }));
    return true;
  }

  sendResponse({ ok: false, error: "Unknown message type." });
});

async function createContextMenus() {
  chrome.contextMenus.removeAll(() => {
    chrome.contextMenus.create({
      id: CONTEXT_MENU_SAVE_PAGE,
      title: "Save page to AddEm",
      contexts: ["page", "link", "video", "audio"]
    });

    chrome.contextMenus.create({
      id: CONTEXT_MENU_SAVE_SELECTION,
      title: "Save selection to AddEm",
      contexts: ["selection"]
    });
  });
}

function makeCapture(input) {
  const url = String(input.url || "").trim();
  const title = String(input.title || "Untitled").trim() || "Untitled";
  const selectedText = String(input.selectedText || "").trim();
  const source = String(input.source || "popup");
  const now = new Date().toISOString();

  return {
    id: crypto.randomUUID(),
    title,
    url,
    notes: selectedText,
    tags: inferTags(url, title),
    source,
    createdAt: now
  };
}

function inferTags(url, title) {
  const tags = [];
  try {
    const host = new URL(url).hostname.replace(/^www\./, "");
    const root = host.split(".")[0];
    if (root) {
      tags.push(root.toLowerCase());
    }
    tags.push("web");
  } catch {
    tags.push("web");
  }

  if (/youtube|video/i.test(title)) {
    tags.push("video");
  }

  return Array.from(new Set(tags));
}

async function handleCapture(rawCapture) {
  const capture = normalizeCapture(rawCapture);
  const settings = await getSettings();

  if (settings.endpointUrl) {
    try {
      await sendCaptureToEndpoint(capture, settings);
      if (settings.openAddEmAfterSave) {
        await openAddEmApp(capture);
      }
      return {
        mode: "synced",
        message: "Saved to backend.",
        pendingCount: await pendingCount()
      };
    } catch (error) {
      await enqueuePending(capture);
      return {
        mode: "queued",
        message: `Endpoint failed, queued locally. (${String(error)})`,
        pendingCount: await pendingCount()
      };
    }
  }

  await enqueuePending(capture);
  if (settings.openAddEmAfterSave) {
    await openAddEmApp(capture);
  }

  return {
    mode: "queued",
    message: "Saved in AddEm local queue. Configure endpoint to sync.",
    pendingCount: await pendingCount()
  };
}

function normalizeCapture(rawCapture) {
  const title = String(rawCapture?.title || "Untitled").trim() || "Untitled";
  const url = String(rawCapture?.url || "").trim();
  const notes = String(rawCapture?.notes || "").trim();
  const tags = normalizeTags(rawCapture?.tags);

  return {
    id: String(rawCapture?.id || crypto.randomUUID()),
    title,
    url,
    notes,
    tags,
    source: String(rawCapture?.source || "popup"),
    createdAt: String(rawCapture?.createdAt || new Date().toISOString())
  };
}

function normalizeTags(value) {
  if (Array.isArray(value)) {
    return Array.from(new Set(value.map((tag) => String(tag).trim().toLowerCase()).filter(Boolean)));
  }

  if (typeof value === "string") {
    return Array.from(
      new Set(
        value
          .split(",")
          .map((tag) => tag.trim().toLowerCase())
          .filter(Boolean)
      )
    );
  }

  return [];
}

async function enqueuePending(capture) {
  const queue = await getPendingCaptures();
  queue.unshift(capture);
  await chrome.storage.local.set({ [STORAGE_PENDING_KEY]: queue.slice(0, 500) });
  await updateBadge();
}

async function removePendingByIds(ids) {
  const removeSet = new Set(ids);
  const queue = await getPendingCaptures();
  const next = queue.filter((item) => !removeSet.has(item.id));
  await chrome.storage.local.set({ [STORAGE_PENDING_KEY]: next });
  await updateBadge();
}

async function getPendingCaptures() {
  const stored = await chrome.storage.local.get(STORAGE_PENDING_KEY);
  const list = stored?.[STORAGE_PENDING_KEY];
  if (!Array.isArray(list)) {
    return [];
  }
  return list;
}

async function pendingCount() {
  const queue = await getPendingCaptures();
  return queue.length;
}

async function updateBadge() {
  const count = await pendingCount();
  if (count <= 0) {
    await chrome.action.setBadgeText({ text: "" });
    return;
  }

  const label = count > 99 ? "99+" : String(count);
  await chrome.action.setBadgeText({ text: label });
  await chrome.action.setBadgeBackgroundColor({ color: "#6F56E7" });
}

async function getSettings() {
  const stored = await chrome.storage.sync.get(Object.keys(DEFAULT_SETTINGS));
  return {
    ...DEFAULT_SETTINGS,
    ...stored
  };
}

async function sendCaptureToEndpoint(capture, settings) {
  const endpointUrl = String(settings.endpointUrl || "").trim();
  if (!endpointUrl) {
    throw new Error("Missing endpoint URL");
  }

  const headers = {
    "Content-Type": "application/json"
  };

  if (settings.authToken) {
    headers.Authorization = `Bearer ${settings.authToken}`;
  }

  const response = await fetch(endpointUrl, {
    method: "POST",
    headers,
    body: JSON.stringify(capture)
  });

  if (!response.ok) {
    const bodyText = await response.text();
    throw new Error(`HTTP ${response.status}: ${bodyText.slice(0, 180)}`);
  }
}

async function syncPendingCaptures() {
  const settings = await getSettings();
  if (!settings.endpointUrl) {
    return {
      synced: 0,
      failed: 0,
      pendingCount: await pendingCount(),
      message: "No endpoint configured."
    };
  }

  const queue = await getPendingCaptures();
  if (queue.length === 0) {
    return {
      synced: 0,
      failed: 0,
      pendingCount: 0,
      message: "Queue already empty."
    };
  }

  const successIds = [];
  let failed = 0;

  for (const capture of queue) {
    try {
      await sendCaptureToEndpoint(capture, settings);
      successIds.push(capture.id);
    } catch {
      failed += 1;
    }
  }

  await removePendingByIds(successIds);

  return {
    synced: successIds.length,
    failed,
    pendingCount: await pendingCount(),
    message: "Sync finished."
  };
}

async function getStatus() {
  const settings = await getSettings();
  return {
    pendingCount: await pendingCount(),
    endpointConfigured: Boolean(settings.endpointUrl),
    endpointUrl: settings.endpointUrl || ""
  };
}

async function openAddEmApp(capture) {
  const settings = await getSettings();
  const base = String(settings.addEmSchemeBase || DEFAULT_SETTINGS.addEmSchemeBase).trim();
  const params = new URLSearchParams({
    url: capture?.url || "",
    title: capture?.title || "",
    notes: capture?.notes || "",
    tags: Array.isArray(capture?.tags) ? capture.tags.join(",") : ""
  });

  const deepLink = `${base}?${params.toString()}`;
  await chrome.tabs.create({ url: deepLink });
}
