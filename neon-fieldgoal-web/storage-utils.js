(function (root, factory) {
  const api = factory();
  if (typeof module !== "undefined" && module.exports) {
    module.exports = api;
  }
  root.NFGStorageUtils = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  function getStorage(storage) {
    if (storage) {
      return storage;
    }
    try {
      return typeof localStorage !== "undefined" ? localStorage : null;
    } catch (error) {
      return null;
    }
  }

  function loadJson(storage, key, fallback = null) {
    const target = getStorage(storage);
    if (!target || typeof target.getItem !== "function") {
      return fallback;
    }
    try {
      const raw = target.getItem(key);
      if (!raw) {
        return fallback;
      }
      const parsed = JSON.parse(raw);
      return parsed == null ? fallback : parsed;
    } catch (error) {
      return fallback;
    }
  }

  function saveJson(storage, key, value) {
    const target = getStorage(storage);
    if (!target || typeof target.setItem !== "function") {
      return false;
    }
    try {
      target.setItem(key, JSON.stringify(value));
      return true;
    } catch (error) {
      return false;
    }
  }

  function loadNumber(storage, key, fallback = 0) {
    const target = getStorage(storage);
    if (!target || typeof target.getItem !== "function") {
      return fallback;
    }
    try {
      const raw = target.getItem(key);
      const parsed = Number.parseInt(raw || "", 10);
      return Number.isFinite(parsed) ? parsed : fallback;
    } catch (error) {
      return fallback;
    }
  }

  function removeKey(storage, key) {
    const target = getStorage(storage);
    if (!target || typeof target.removeItem !== "function") {
      return false;
    }
    try {
      target.removeItem(key);
      return true;
    } catch (error) {
      return false;
    }
  }

  return {
    loadJson,
    saveJson,
    loadNumber,
    removeKey
  };
});
