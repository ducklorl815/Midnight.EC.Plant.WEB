(function () {
  "use strict";

  function esc(s) {
    if (!s) return "";
    return String(s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function ensureModal() {
    var el = document.getElementById("plant-effect-modal");
    if (el) return el;

    el = document.createElement("div");
    el.id = "plant-effect-modal";
    el.className = "plant-effect-modal d-none";
    el.setAttribute("role", "dialog");
    el.setAttribute("aria-modal", "true");
    el.innerHTML =
      '<div class="plant-effect-modal-backdrop" data-effect-close></div>' +
      '<div class="plant-effect-modal-dialog">' +
      '  <button type="button" class="plant-effect-modal-close" data-effect-close aria-label="關閉">&times;</button>' +
      '  <div class="plant-effect-stage" data-effect-stage>' +
      '    <img class="plant-effect-image" alt="植物效果圖" />' +
      "  </div>" +
      '  <div class="plant-effect-modal-actions">' +
      '    <button type="button" class="btn btn-sm btn-outline-secondary" data-effect-close>關閉</button>' +
      '    <button type="button" class="btn btn-sm btn-plant-primary" data-effect-regen>重新生成</button>' +
      "  </div>" +
      '  <p class="plant-effect-meta small text-muted mb-0"></p>' +
      "</div>";
    document.body.appendChild(el);

    el.addEventListener("click", function (e) {
      if (e.target && e.target.hasAttribute("data-effect-close")) {
        hideModal();
      }
    });

    return el;
  }

  var activePhotoId = null;
  var activeButton = null;
  var activeData = null;
  var activeOptions = {};

  function hideModal() {
    var modal = document.getElementById("plant-effect-modal");
    if (modal) modal.classList.add("d-none");
    document.body.classList.remove("plant-effect-modal-open");
  }

  function updateStageUi(data) {
    if (!data) return;
    var modal = ensureModal();
    var stage = modal.querySelector("[data-effect-stage]");
    var img = modal.querySelector(".plant-effect-image");
    var meta = modal.querySelector(".plant-effect-meta");

    img.src = data.generatedImageUrl || "";
    meta.textContent =
      (!!data.isComposedFinal ? "舊版合成圖" : "效果圖") +
      " · 風格 " +
      (data.style || "") +
      " · 版面 " +
      (data.layout || "") +
      " · " +
      (data.promptVersion || "");
  }

  function showResult(data, button, options) {
    activeData = data || null;
    activeButton = button || null;
    activeOptions = options || {};
    activePhotoId = data ? data.originalPhotoId : null;

    var modal = ensureModal();
    updateStageUi(activeData);
    modal.classList.remove("d-none");
    document.body.classList.add("plant-effect-modal-open");

    var regen = modal.querySelector("[data-effect-regen]");
    regen.onclick = function () {
      if (!activePhotoId) return;
      runGenerate(activePhotoId, true, activeButton || regen, activeOptions);
    };
  }

  function setButtonState(btn, state, url) {
    if (!btn) return;
    btn.disabled = state === "loading";
    if (state === "loading") {
      btn.textContent = "生成中...";
      return;
    }
    if (url) {
      btn.textContent = "查看效果圖片";
      btn.dataset.effectUrl = url;
      btn.dataset.effectMode = "view";
    } else if (state === "ready-regen") {
      btn.textContent = "重新生成";
      btn.dataset.effectMode = "regen";
    } else {
      btn.textContent = "生成效果圖片";
      btn.dataset.effectMode = "generate";
    }
  }

  async function runGenerate(photoId, force, btn, options) {
    options = options || {};
    var confirmText =
      options.confirmMessage ||
      (force
        ? "確定要重新生成效果圖嗎？這會再次消耗圖片 token。"
        : "確定要生成效果圖嗎？這會消耗圖片 token。");
    if (options.confirm !== false && !window.confirm(confirmText)) {
      return null;
    }

    setButtonState(btn, "loading");
    var statusEl = btn
      ? btn.closest(".photo-card, .pc-detail-wall-slide-media, .pc-detail-wall-card, .pc-frame-row")
      : null;
    var hint =
      (statusEl && statusEl.querySelector("[data-effect-status='" + photoId + "']")) ||
      (statusEl && statusEl.querySelector(".pc-frame-effect-status"));
    if (hint) {
      hint.classList.remove("d-none");
      hint.textContent = "正在製作效果圖…";
    }

    try {
      var endpoint = force
        ? "/PlantEffectImage/Regenerate?photoId=" + encodeURIComponent(photoId)
        : "/PlantEffectImage/Generate?photoId=" + encodeURIComponent(photoId);
      var response = await fetch(endpoint, { method: "POST" });
      var data = await response.json().catch(function () {
        return {};
      });
      if (!response.ok) {
        throw new Error(data.error || "效果圖生成失敗。");
      }
      setButtonState(btn, "idle", data.generatedImageUrl);
      if (hint) {
        hint.textContent = "效果圖已生成。";
      }
      showResult(data, btn, options);
      return data;
    } catch (err) {
      setButtonState(btn, "idle");
      if (hint) {
        hint.textContent = err.message || "效果圖生成失敗。";
      } else {
        window.alert(err.message || "效果圖生成失敗。");
      }
      throw err;
    }
  }

  async function onClick(btn) {
    var photoId = btn.getAttribute("data-photo-id");
    if (!photoId) return;

    var mode = btn.dataset.effectMode || "generate";
    var existingUrl = btn.dataset.effectUrl || "";

    if (mode === "view" && existingUrl) {
      try {
        var latest = await fetch("/PlantEffectImage/Latest?photoId=" + encodeURIComponent(photoId));
        if (latest.ok) {
          var data = await latest.json();
          showResult(data, btn);
          return;
        }
      } catch (_) {
        /* fall through */
      }
    }

    await runGenerate(photoId, mode === "regen", btn, {});
  }

  function bind(root) {
    (root || document)
      .querySelectorAll("[data-plant-effect-btn]")
      .forEach(function (btn) {
        if (btn.dataset.effectBound === "1") return;
        btn.dataset.effectBound = "1";
        var url = btn.getAttribute("data-effect-url") || "";
        if (url) {
          setButtonState(btn, "idle", url);
        } else {
          setButtonState(btn, "idle");
        }
        btn.addEventListener("click", function () {
          onClick(btn);
        });
      });
  }

  document.addEventListener("DOMContentLoaded", function () {
    bind(document);
  });

  window.PlantEffectImageUI = {
    bind: bind,
    showResult: showResult,
    runGenerate: runGenerate
  };
})();
