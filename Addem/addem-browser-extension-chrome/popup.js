const form = document.getElementById("capture-form");
const urlInput = document.getElementById("url");
const titleInput = document.getElementById("title");
const notesInput = document.getElementById("notes");
const tagsInput = document.getElementById("tags");
const statusEl = document.getElementById("status");
const metaEl = document.getElementById("meta");
const syncPendingButton = document.getElementById("sync-pending");
const openAppButton = document.getElementById("open-app");
const openOptionsButton = document.getElementById("open-options");

init().catch((error) => setStatus(`Init failed: ${String(error)}`, true));

async function init() {
  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  if (tab) {
    urlInput.value = tab.url || "";
    titleInput.value = tab.title || "Untitled";

    const selectedText = await getSelectedText(tab.id);
    if (selectedText) {
      notesInput.value = selectedText;
    }

    const defaultTags = inferTags(tab.url || "", tab.title || "");
    tagsInput.value = defaultTags.join(", ");
  }

  form.addEventListener("submit", onSubmitCapture);
  syncPendingButton.addEventListener("click", onSyncPending);
  openAppButton.addEventListener("click", onOpenApp);
  openOptionsButton.addEventListener("click", () => chrome.runtime.openOptionsPage());

  await refreshStatus();
}

async function getSelectedText(tabId) {
  if (!tabId) {
    return "";
  }

  try {
    const result = await chrome.scripting.executeScript({
      target: { tabId },
      func: () => window.getSelection()?.toString()?.trim() || ""
    });
    return result?.[0]?.result || "";
  } catch {
    return "";
  }
}

function inferTags(url, title) {
  const tags = ["web"];

  try {
    const host = new URL(url).hostname.replace(/^www\./, "");
    const root = host.split(".")[0];
    if (root) {
      tags.unshift(root.toLowerCase());
    }
  } catch {
    // Ignore URL parse errors.
  }

  if (/youtube|video/i.test(title)) {
    tags.push("video");
  }

  return Array.from(new Set(tags));
}

function makeCaptureFromForm() {
  const cleanUrl = normalizeURL(urlInput.value);
  const cleanTitle = titleInput.value.trim() || "Untitled";
  const cleanNotes = notesInput.value.trim();
  const cleanTags = tagsInput.value
    .split(",")
    .map((tag) => tag.trim().toLowerCase())
    .filter(Boolean);

  return {
    title: cleanTitle,
    url: cleanUrl,
    notes: cleanNotes,
    tags: Array.from(new Set(cleanTags)),
    source: "popup"
  };
}

function normalizeURL(value) {
  const raw = String(value || "").trim();
  if (!raw) {
    return "";
  }

  if (/^[a-zA-Z][a-zA-Z\d+\-.]*:/.test(raw)) {
    return raw;
  }

  return `https://${raw}`;
}

async function onSubmitCapture(event) {
  event.preventDefault();

  const capture = makeCaptureFromForm();
  if (!capture.url) {
    setStatus("URL is required.", true);
    return;
  }

  setStatus("Saving...", false);

  const response = await chrome.runtime.sendMessage({
    type: "addem-save-capture",
    capture
  });

  if (!response?.ok) {
    setStatus(response?.error || "Save failed.", true);
    return;
  }

  const result = response.result;
  setStatus(result.message, result.mode !== "synced");
  metaEl.textContent = `Pending queue: ${result.pendingCount}`;
}

async function onSyncPending() {
  setStatus("Syncing pending queue...", false);

  const response = await chrome.runtime.sendMessage({ type: "addem-sync-pending" });
  if (!response?.ok) {
    setStatus(response?.error || "Sync failed.", true);
    return;
  }

  const result = response.result;
  setStatus(`${result.message} Synced ${result.synced}, failed ${result.failed}.`, result.failed > 0);
  metaEl.textContent = `Pending queue: ${result.pendingCount}`;
}

async function onOpenApp() {
  const capture = makeCaptureFromForm();
  const response = await chrome.runtime.sendMessage({
    type: "addem-open-app",
    capture
  });

  if (!response?.ok) {
    setStatus(response?.error || "Could not open AddEm app.", true);
    return;
  }

  setStatus("Tried opening AddEm app via deep link.", false);
}

async function refreshStatus() {
  const response = await chrome.runtime.sendMessage({ type: "addem-get-status" });
  if (!response?.ok) {
    setStatus(response?.error || "Status unavailable.", true);
    return;
  }

  const status = response.status;
  const endpointText = status.endpointConfigured ? "Endpoint configured" : "No endpoint configured";
  setStatus(endpointText, !status.endpointConfigured);
  metaEl.textContent = `Pending queue: ${status.pendingCount}`;
}

function setStatus(message, isWarning) {
  statusEl.textContent = message;
  statusEl.style.color = isWarning ? "#7a2450" : "#2e2558";
}
