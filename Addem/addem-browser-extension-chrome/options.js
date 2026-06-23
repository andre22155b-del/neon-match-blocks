const defaults = {
  endpointUrl: "",
  authToken: "",
  openAddEmAfterSave: false,
  addEmSchemeBase: "addem://capture"
};

const statusEl = document.getElementById("status");
const form = document.getElementById("settings-form");
const endpointEl = document.getElementById("endpointUrl");
const tokenEl = document.getElementById("authToken");
const openAfterSaveEl = document.getElementById("openAddEmAfterSave");
const deepLinkEl = document.getElementById("addEmSchemeBase");

init().catch((error) => setStatus(`Init failed: ${String(error)}`, true));

async function init() {
  const saved = await chrome.storage.sync.get(Object.keys(defaults));
  const settings = { ...defaults, ...saved };

  endpointEl.value = settings.endpointUrl;
  tokenEl.value = settings.authToken;
  openAfterSaveEl.checked = settings.openAddEmAfterSave;
  deepLinkEl.value = settings.addEmSchemeBase;

  form.addEventListener("submit", onSave);
}

async function onSave(event) {
  event.preventDefault();

  const payload = {
    endpointUrl: endpointEl.value.trim(),
    authToken: tokenEl.value.trim(),
    openAddEmAfterSave: openAfterSaveEl.checked,
    addEmSchemeBase: deepLinkEl.value.trim() || defaults.addEmSchemeBase
  };

  await chrome.storage.sync.set(payload);
  setStatus("Settings saved.", false);
}

function setStatus(message, isError) {
  statusEl.textContent = message;
  statusEl.style.color = isError ? "#8f254c" : "#3e2f7e";
}
