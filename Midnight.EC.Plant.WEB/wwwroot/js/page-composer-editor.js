(function () {
  "use strict";

  var seedEl = document.getElementById("pc-layout-seed");
  var listEl = document.getElementById("pc-module-list");
  var previewEl = document.getElementById("pc-live-preview");
  var formEl = document.getElementById("pc-save-form");
  var jsonInput = document.getElementById("pc-layout-json");
  var addTypeEl = document.getElementById("pc-add-type");
  var addBtn = document.getElementById("pc-add-module");
  var detailEmpty = document.getElementById("pc-detail-empty");
  var detailBanner = document.getElementById("pc-detail-banner");
  var detailCarousel = document.getElementById("pc-detail-carousel");
  var frameListEl = document.getElementById("pc-frame-list");
  var detailRoot = document.getElementById("pc-module-detail");

  if (!seedEl || !listEl || !formEl || !jsonInput) return;

  var TYPE_LABELS = {
    Banner: "Banner",
    LeftImageRightText: "左圖右文",
    RightImageLeftText: "右圖左文",
    MultiImageMultiText: "多圖多文",
    MultiImageStyle: "多圖風格",
    PlantDetailWallCarousel: "植栽細節牆·輪播",
    NotificationReminders: "通知提醒"
  };

  var seed;
  try {
    seed = JSON.parse(seedEl.textContent || "{}");
  } catch (e) {
    seed = { version: 2, modules: [], previewSlides: {}, previewNotifications: [] };
  }

  var layout = {
    version: seed.version || 2,
    modules: Array.isArray(seed.modules) ? seed.modules : []
  };
  layout.modules.forEach(function (m) {
    if (!Array.isArray(m.slideFrames)) m.slideFrames = [];
  });

  /** Base plant catalog from server (per module id). Framing applied from module.slideFrames. */
  var plantCatalogByModule = seed.previewSlides || {};
  var previewNotifications = Array.isArray(seed.previewNotifications) ? seed.previewNotifications : [];
  var photoCatalog = Array.isArray(seed.photoCatalog) ? seed.photoCatalog : [];
  var bannerUploadUrl = seed.uploadUrl || "/Settings/UploadPageComposerBanner";
  var selectedModuleId = null;
  var selectedPlantId = null;

  function antiforgeryToken() {
    var el = formEl.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : "";
  }

  function uid() {
    if (window.crypto && crypto.randomUUID) return crypto.randomUUID().replace(/-/g, "");
    return "m" + Date.now().toString(36) + Math.random().toString(36).slice(2, 8);
  }

  function moduleType(m) {
    if (!m) return "";
    var t = m.type != null ? m.type : m.Type;
    return t == null ? "" : String(t);
  }

  function isPlantDetailWall(m) {
    var t = moduleType(m);
    return (
      t === "PlantDetailWallCarousel" ||
      t === "5" ||
      t === "左圖右文·植栽細節牆·輪播"
    );
  }

  function typeLabel(t) {
    return TYPE_LABELS[t] || TYPE_LABELS[String(t)] || t;
  }

  function syncOrders() {
    layout.modules.forEach(function (m, i) {
      m.order = i;
    });
  }

  /** Banner 釘死第一位、永遠啟用、僅保留一個。 */
  function pinBannerFirst() {
    var banners = layout.modules.filter(function (m) {
      return m.type === "Banner";
    });
    var others = layout.modules.filter(function (m) {
      return m.type !== "Banner";
    });
    var keep = banners[0] || createModule("Banner");
    keep.enabled = true;
    keep.type = "Banner";
    layout.modules = [keep].concat(others);
    syncOrders();
  }

  function findModule(id) {
    return layout.modules.find(function (m) {
      return m.id === id;
    });
  }

  function catalogFor(moduleId) {
    var list = plantCatalogByModule[moduleId];
    if (Array.isArray(list) && list.length) return list;
    var anyKey = Object.keys(plantCatalogByModule)[0];
    if (anyKey && Array.isArray(plantCatalogByModule[anyKey])) return plantCatalogByModule[anyKey];
    return [];
  }

  function samePlantId(a, b) {
    return String(a || "").toLowerCase() === String(b || "").toLowerCase();
  }

  function ensureFrames(module) {
    if (!module.slideFrames) module.slideFrames = [];
    var catalog = catalogFor(module.id);
    catalog.forEach(function (s) {
      if (!s.plantId || s.plantId === "00000000-0000-0000-0000-000000000000") return;
      var exists = module.slideFrames.some(function (f) {
        return samePlantId(f.plantId, s.plantId);
      });
      if (!exists) {
        module.slideFrames.push(createDefaultFrame(s.plantId));
      }
    });
    module.slideFrames.forEach(normalizeFrame);
  }

  function createDefaultFrame(plantId) {
    return {
      plantId: plantId,
      zoom: 1,
      focusX: 50,
      focusY: 50,
      sourceZoom: 1,
      sourceFocusX: 50,
      sourceFocusY: 50,
      cardX: 6,
      cardY: 22,
      effectImageId: null,
      effectImageIsComposedFinal: false,
      effectImageUrl: null,
      pendingEffectId: null,
      pendingEffectIsComposedFinal: false,
      pendingEffectUrl: null,
      pendingBaseId: null,
      pendingBaseUrl: null
    };
  }

  function normalizeFrame(frame) {
    if (!frame) return frame;
    if (frame.zoom == null || frame.zoom <= 0) frame.zoom = 1;
    if (frame.focusX == null) frame.focusX = 50;
    if (frame.focusY == null) frame.focusY = 50;
    if (frame.sourceZoom == null || frame.sourceZoom <= 0) frame.sourceZoom = 1;
    if (frame.sourceFocusX == null) frame.sourceFocusX = 50;
    if (frame.sourceFocusY == null) frame.sourceFocusY = 50;
    if (frame.cardX == null) frame.cardX = 6;
    if (frame.cardY == null) frame.cardY = 22;
    if (frame.effectImageIsComposedFinal == null) frame.effectImageIsComposedFinal = false;
    if (frame.pendingEffectId === undefined) frame.pendingEffectId = null;
    if (frame.pendingEffectIsComposedFinal === undefined) frame.pendingEffectIsComposedFinal = false;
    if (frame.pendingEffectUrl === undefined) frame.pendingEffectUrl = null;
    if (frame.pendingBaseId === undefined) frame.pendingBaseId = null;
    if (frame.pendingBaseUrl === undefined) frame.pendingBaseUrl = null;
    return frame;
  }

  function getFrame(module, plantId) {
    ensureFrames(module);
    var frame = module.slideFrames.find(function (f) {
      return samePlantId(f.plantId, plantId);
    });
    if (!frame) {
      frame = createDefaultFrame(plantId);
      module.slideFrames.push(frame);
    }
    return normalizeFrame(frame);
  }

  function resetFrameView(frame) {
    frame.zoom = 1;
    frame.focusX = 50;
    frame.focusY = 50;
  }

  function resetFrameSource(frame) {
    frame.sourceZoom = 1;
    frame.sourceFocusX = 50;
    frame.sourceFocusY = 50;
  }

  function hydrateLatestEffect(frame, slide) {
    if (!frame || !slide) return;
    if (frame.effectImageId || frame.effectImageUrl || frame.pendingEffectId || frame.pendingEffectUrl) return;
    if (!slide.latestEffectImageId || !slide.latestEffectImageUrl) return;
    frame.pendingEffectId = slide.latestEffectImageId;
    frame.pendingEffectUrl = slide.latestEffectImageUrl;
    frame.pendingEffectIsComposedFinal = !!slide.latestEffectIsComposedFinal;
  }

  function createModule(type) {
    var base = {
      id: uid(),
      type: type,
      enabled: true,
      order: layout.modules.length,
      title: "新模組",
      subtitle: "佔位副標",
      body: "請之後替換成真實內容。",
      theme: "theme-default",
      images: [{ url: "", alt: "佔位圖" }],
      items: [],
      slideFrames: []
    };
    if (type === "Banner") {
      base.title = "探索每一盆綠意";
      base.subtitle = "由上而下，一區一景——模組拼圖組成的植栽故事首頁。";
      base.body = "";
      base.theme = "theme-banner";
      base.ctaText = "新增植栽";
      base.ctaHref = "/Plant/Create";
      base.images = [{ url: "", alt: "Banner" }];
    } else if (type === "PlantDetailWallCarousel") {
      base.title = "植栽細節牆 - 輪播";
      base.subtitle = "";
      base.body = "";
      base.theme = "theme-detail-wall";
      base.images = [];
      base.items = [];
      base.slideFrames = [];
    } else if (type === "NotificationReminders") {
      base.title = "通知提醒";
      base.subtitle = "";
      base.body = "";
      base.theme = "theme-default";
      base.images = [];
      base.items = [];
      base.slideFrames = [];
    } else if (type === "MultiImageMultiText") {
      base.title = "多圖多文牆";
      base.theme = "theme-fern";
      base.items = [
        { title: "故事一", body: "佔位說明文字 A。", imageUrl: "" },
        { title: "故事二", body: "佔位說明文字 B。", imageUrl: "" },
        { title: "故事三", body: "佔位說明文字 C。", imageUrl: "" }
      ];
      base.images = [];
    } else if (type === "MultiImageStyle") {
      base.title = "多圖風格";
      base.subtitle = "以影像為主";
      base.body = "";
      base.theme = "theme-stone";
      base.images = [
        { url: "", alt: "圖一" },
        { url: "", alt: "圖二" },
        { url: "", alt: "圖三" },
        { url: "", alt: "圖四" }
      ];
    } else if (type === "RightImageLeftText") {
      base.title = "新右圖左文";
      base.theme = "theme-clay";
    } else {
      base.title = "新左圖右文";
      base.theme = "theme-moss";
    }
    return base;
  }

  function selectModule(id) {
    selectedModuleId = id;
    selectedPlantId = null;
    renderList();
    renderDetail();
    refreshPreview();
  }

  function renderList() {
    listEl.innerHTML = "";
    pinBannerFirst();
    layout.modules.forEach(function (m) {
      var isBanner = m.type === "Banner";
      var li = document.createElement("li");
      li.className =
        "pc-module-row" +
        (m.enabled ? "" : " is-disabled") +
        (m.id === selectedModuleId ? " is-selected" : "") +
        (isBanner ? " is-pinned" : "");
      li.dataset.id = m.id;

      li.innerHTML =
        (isBanner
          ? '<span class="pc-module-handle is-locked" title="Banner 固定於頂部" aria-hidden="true">◉</span>'
          : '<span class="pc-module-handle" draggable="true" title="拖移排序" aria-label="拖移排序">⠿</span>') +
        '<div class="pc-module-reorder">' +
        '<button type="button" class="btn btn-sm btn-outline-secondary pc-move-up" title="上移"' +
        (isBanner ? " disabled" : "") +
        ">↑</button>" +
        '<button type="button" class="btn btn-sm btn-outline-secondary pc-move-down" title="下移"' +
        (isBanner ? " disabled" : "") +
        ">↓</button>" +
        "</div>" +
        '<div class="pc-module-meta">' +
        "<strong></strong>" +
        "<span></span>" +
        "</div>" +
        (isBanner
          ? '<span class="small text-muted pc-banner-lock">必開</span>'
          : '<label class="small mb-0"><input type="checkbox" class="pc-toggle form-check-input" /> 開</label>') +
        (isBanner
          ? ""
          : '<button type="button" class="btn btn-sm btn-outline-danger pc-remove" title="刪除">刪</button>');

      li.querySelector("strong").textContent = m.title || "(無標題)";
      li.querySelector("span").textContent = typeLabel(m.type);

      li.addEventListener("click", function (e) {
        if (
          e.target.closest(".pc-toggle") ||
          e.target.closest(".pc-remove") ||
          e.target.closest(".pc-move-up") ||
          e.target.closest(".pc-move-down") ||
          e.target.closest(".pc-module-handle")
        )
          return;
        selectModule(m.id);
      });

      li.querySelector(".pc-move-up").addEventListener("click", function () {
        if (isBanner) return;
        var idx = layout.modules.findIndex(function (x) {
          return x.id === m.id;
        });
        if (idx <= 1) return;
        var tmp = layout.modules[idx - 1];
        layout.modules[idx - 1] = layout.modules[idx];
        layout.modules[idx] = tmp;
        pinBannerFirst();
        renderList();
        refreshPreview();
      });
      li.querySelector(".pc-move-down").addEventListener("click", function () {
        if (isBanner) return;
        var idx = layout.modules.findIndex(function (x) {
          return x.id === m.id;
        });
        if (idx < 0 || idx >= layout.modules.length - 1) return;
        var tmp = layout.modules[idx + 1];
        layout.modules[idx + 1] = layout.modules[idx];
        layout.modules[idx] = tmp;
        pinBannerFirst();
        renderList();
        refreshPreview();
      });

      var handle = li.querySelector(".pc-module-handle");
      if (!isBanner) {
        handle.addEventListener("dragstart", function (e) {
          li.classList.add("is-dragging");
          e.dataTransfer.setData("text/plain", m.id);
          e.dataTransfer.effectAllowed = "move";
        });
        handle.addEventListener("dragend", function () {
          li.classList.remove("is-dragging");
          listEl.querySelectorAll(".pc-module-row.is-drop-target").forEach(function (n) {
            n.classList.remove("is-drop-target");
          });
        });
      }
      li.addEventListener("dragover", function (e) {
        if (isBanner) return;
        e.preventDefault();
        e.dataTransfer.dropEffect = "move";
        listEl.querySelectorAll(".pc-module-row.is-drop-target").forEach(function (n) {
          n.classList.remove("is-drop-target");
        });
        li.classList.add("is-drop-target");
      });
      li.addEventListener("dragleave", function () {
        li.classList.remove("is-drop-target");
      });
      li.addEventListener("drop", function (e) {
        if (isBanner) return;
        e.preventDefault();
        li.classList.remove("is-drop-target");
        var fromId = e.dataTransfer.getData("text/plain");
        var toId = m.id;
        if (!fromId || fromId === toId) return;
        var fromIdx = layout.modules.findIndex(function (x) {
          return x.id === fromId;
        });
        var toIdx = layout.modules.findIndex(function (x) {
          return x.id === toId;
        });
        if (fromIdx < 0 || toIdx < 0) return;
        if (layout.modules[fromIdx].type === "Banner") return;
        var moved = layout.modules.splice(fromIdx, 1)[0];
        layout.modules.splice(toIdx, 0, moved);
        pinBannerFirst();
        renderList();
        refreshPreview();
      });

      if (!isBanner) {
        var toggle = li.querySelector(".pc-toggle");
        toggle.checked = !!m.enabled;
        toggle.addEventListener("change", function () {
          m.enabled = toggle.checked;
          li.classList.toggle("is-disabled", !m.enabled);
          refreshPreview();
        });
        li.querySelector(".pc-remove").addEventListener("click", function () {
          layout.modules = layout.modules.filter(function (x) {
            return x.id !== m.id;
          });
          if (selectedModuleId === m.id) {
            selectedModuleId = null;
            selectedPlantId = null;
          }
          pinBannerFirst();
          renderList();
          renderDetail();
          refreshPreview();
        });
      }

      listEl.appendChild(li);
    });
  }

  function renderDetail() {
    if (!detailRoot || !detailEmpty || !detailCarousel || !frameListEl) return;
    var module = selectedModuleId ? findModule(selectedModuleId) : null;
    if (detailBanner) {
      detailBanner.classList.add("d-none");
      detailBanner.innerHTML = "";
    }
    if (!module) {
      detailRoot.classList.add("is-empty");
      detailEmpty.classList.remove("d-none");
      detailEmpty.textContent = "點選上方模組以編輯細項。";
      detailCarousel.classList.add("d-none");
      frameListEl.innerHTML = "";
      return;
    }

    detailRoot.classList.remove("is-empty");

    if (module.type === "Banner" && detailBanner) {
      detailEmpty.classList.add("d-none");
      detailCarousel.classList.add("d-none");
      frameListEl.innerHTML = "";
      detailBanner.classList.remove("d-none");
      if (!module.images || !module.images.length)
        module.images = [{ url: "", alt: "Banner", source: "url" }];
      var img = module.images[0];
      if (!img.source) img.source = img.plantId && img.imageId ? "plant" : "url";

      var previewHtml = img.missing
        ? '<p class="small text-warning mb-2">原圖已不在，已改用占位。請重新上傳或選圖。</p>'
        : "";
      if (img.url && !img.missing) {
        previewHtml +=
          '<div class="pc-banner-thumb-wrap mb-2"><img class="pc-banner-thumb" src="' +
          esc(img.url) +
          '" alt="" /></div>';
      }

      var pickerHtml = "";
      if (!photoCatalog.length) {
        pickerHtml = '<p class="small text-muted mb-2">尚無植栽照片可選。</p>';
      } else {
        pickerHtml = photoCatalog
          .map(function (group) {
            var thumbs = (group.photos || [])
              .map(function (ph) {
                var selected =
                  img.source === "plant" &&
                  String(img.plantId || "").toLowerCase() === String(group.plantId || "").toLowerCase() &&
                  String(img.imageId || "").toLowerCase() === String(ph.imageId || "").toLowerCase();
                return (
                  '<button type="button" class="pc-banner-pick' +
                  (selected ? " is-selected" : "") +
                  '" data-plant-id="' +
                  esc(group.plantId) +
                  '" data-image-id="' +
                  esc(ph.imageId) +
                  '" data-url="' +
                  esc(ph.url) +
                  '" title="' +
                  (ph.isCover ? "封面" : "照片") +
                  '">' +
                  '<img src="' +
                  esc(ph.url) +
                  '" alt="" />' +
                  (ph.isCover ? '<span class="pc-banner-pick-badge">封</span>' : "") +
                  "</button>"
                );
              })
              .join("");
            return (
              '<div class="pc-banner-plant-group">' +
              '<div class="small fw-semibold mb-1">' +
              esc(group.displayName) +
              "</div>" +
              '<div class="pc-banner-pick-grid">' +
              thumbs +
              "</div></div>"
            );
          })
          .join("");
      }

      detailBanner.innerHTML =
        '<div class="fw-semibold small mb-2">Banner 主視覺</div>' +
        '<p class="small text-muted">上傳站點圖，或從植栽照片選一張。</p>' +
        previewHtml +
        '<label class="form-label small mb-1">上傳圖片</label>' +
        '<input type="file" accept="image/jpeg,image/png,image/webp,image/gif" class="form-control form-control-sm mb-2 pc-banner-file" />' +
        '<p class="small text-muted pc-banner-upload-status mb-2"></p>' +
        '<div class="fw-semibold small mb-1">從植栽照片選</div>' +
        '<div class="pc-banner-picker mb-3">' +
        pickerHtml +
        "</div>" +
        '<label class="form-label small mb-1">替代文字</label>' +
        '<input type="text" class="form-control form-control-sm mb-3 pc-banner-alt" />' +
        '<details class="pc-banner-advanced">' +
        '<summary class="small">進階：外連網址與文案</summary>' +
        '<div class="pt-2">' +
        '<label class="form-label small mb-1">圖片網址</label>' +
        '<input type="url" class="form-control form-control-sm mb-2 pc-banner-img" placeholder="https://…" />' +
        '<label class="form-label small mb-1">標題</label>' +
        '<input type="text" class="form-control form-control-sm mb-2 pc-banner-title" />' +
        '<label class="form-label small mb-1">副標</label>' +
        '<textarea class="form-control form-control-sm mb-2 pc-banner-sub" rows="2"></textarea>' +
        '<label class="form-label small mb-1">按鈕文字</label>' +
        '<input type="text" class="form-control form-control-sm mb-2 pc-banner-cta" />' +
        '<label class="form-label small mb-1">按鈕連結</label>' +
        '<input type="text" class="form-control form-control-sm pc-banner-href" placeholder="/Plant/Create" />' +
        "</div></details>";

      var fileInput = detailBanner.querySelector(".pc-banner-file");
      var statusEl = detailBanner.querySelector(".pc-banner-upload-status");
      var imgInput = detailBanner.querySelector(".pc-banner-img");
      var altInput = detailBanner.querySelector(".pc-banner-alt");
      var titleInput = detailBanner.querySelector(".pc-banner-title");
      var subInput = detailBanner.querySelector(".pc-banner-sub");
      var ctaInput = detailBanner.querySelector(".pc-banner-cta");
      var hrefInput = detailBanner.querySelector(".pc-banner-href");
      imgInput.value = img.source === "url" ? img.url || "" : "";
      altInput.value = img.alt || "";
      titleInput.value = module.title || "";
      subInput.value = module.subtitle || "";
      ctaInput.value = module.ctaText || "";
      hrefInput.value = module.ctaHref || "";

      function pushBannerMeta() {
        img.alt = altInput.value.trim() || "Banner";
        module.title = titleInput.value;
        module.subtitle = subInput.value;
        module.ctaText = ctaInput.value.trim();
        module.ctaHref = hrefInput.value.trim();
        var row = listEl.querySelector('.pc-module-row[data-id="' + module.id + '"] strong');
        if (row) row.textContent = module.title || "(無標題)";
        refreshPreview();
      }

      function applyBannerImage(next) {
        img.source = next.source;
        img.url = next.url || "";
        img.plantId = next.plantId || null;
        img.imageId = next.imageId || null;
        img.missing = false;
        pushBannerMeta();
        renderDetail();
      }

      fileInput.addEventListener("change", function () {
        var file = fileInput.files && fileInput.files[0];
        if (!file) return;
        statusEl.textContent = "上傳中…";
        var fd = new FormData();
        fd.append("file", file);
        fd.append("__RequestVerificationToken", antiforgeryToken());
        fetch(bannerUploadUrl, {
          method: "POST",
          body: fd,
          credentials: "same-origin"
        })
          .then(function (res) {
            return res.json().then(function (body) {
              if (!res.ok) throw new Error((body && body.error) || "上傳失敗");
              return body;
            });
          })
          .then(function (body) {
            statusEl.textContent = "已上傳。";
            applyBannerImage({ source: "upload", url: body.url });
          })
          .catch(function (err) {
            statusEl.textContent = err.message || "上傳失敗";
          });
      });

      detailBanner.querySelectorAll(".pc-banner-pick").forEach(function (btn) {
        btn.addEventListener("click", function () {
          applyBannerImage({
            source: "plant",
            url: btn.getAttribute("data-url") || "",
            plantId: btn.getAttribute("data-plant-id"),
            imageId: btn.getAttribute("data-image-id")
          });
        });
      });

      imgInput.addEventListener("input", function () {
        var url = imgInput.value.trim();
        img.source = "url";
        img.url = url;
        img.plantId = null;
        img.imageId = null;
        img.missing = false;
        pushBannerMeta();
      });
      [altInput, titleInput, subInput, ctaInput, hrefInput].forEach(function (el) {
        el.addEventListener("input", pushBannerMeta);
      });
      return;
    }

    if (!isPlantDetailWall(module)) {
      detailEmpty.classList.remove("d-none");
      detailEmpty.textContent = "此模組類型尚無細項編輯（目前僅 Banner 與植栽細節牆·輪播）。";
      detailCarousel.classList.add("d-none");
      frameListEl.innerHTML = "";
      return;
    }

    detailEmpty.classList.add("d-none");
    detailCarousel.classList.remove("d-none");
    ensureFrames(module);
    // Share catalog under this module id for newly added modules
    if (!plantCatalogByModule[module.id]) {
      var shared = catalogFor(module.id);
      plantCatalogByModule[module.id] = shared.slice();
    }

    var catalog = catalogFor(module.id);
    frameListEl.innerHTML = "";
    if (!catalog.length) {
      frameListEl.innerHTML = '<p class="small text-muted mb-0">尚無照片牆盆栽可調。</p>';
      return;
    }

    catalog.forEach(function (slide, idx) {
      if (!slide.plantId || slide.plantId.indexOf("00000000") === 0) return;
      var frame = getFrame(module, slide.plantId);
      hydrateLatestEffect(frame, slide);
      var row = document.createElement("div");
      row.className = "pc-frame-row" + (samePlantId(selectedPlantId, slide.plantId) ? " is-active" : "");
      row.dataset.plantId = slide.plantId;

      var thumbSrc = frame.effectImageUrl || frame.pendingEffectUrl || resolveOriginalCoverPath(slide) || slide.coverImagePath;
      var thumb = thumbSrc
        ? '<img class="pc-frame-thumb" src="' + esc(thumbSrc) + '" alt="" />'
        : '<div class="pc-frame-thumb pc-frame-thumb-empty"></div>';

      var hasApplied = !!(frame.effectImageUrl && frame.effectImageId);
      var hasPending = !!(frame.pendingEffectUrl && frame.pendingEffectId);
      var photoId = slide.coverImageId || "";
      var pendingLabel = hasApplied ? "重新置換" : "置換";
      var previewLabel = hasPending
        ? "已找到已生成效果圖，可直接置換。"
        : "";

      row.innerHTML =
        '<button type="button" class="pc-frame-pick">' +
        thumb +
        '<span class="pc-frame-name"></span></button>' +
        '<div class="pc-frame-fields">' +
        '<div class="pc-frame-effect-actions">' +
        '<button type="button" class="btn btn-sm btn-plant-primary pc-f-effect-gen"' +
        (photoId ? "" : " disabled") +
        ">生成效果圖</button>" +
        '<button type="button" class="btn btn-sm btn-emphasis pc-f-effect-apply"' +
        (hasPending || hasApplied ? "" : " disabled") +
        ">" +
        (hasApplied && !hasPending ? "已置換" : pendingLabel) +
        "</button>" +
        '<button type="button" class="btn btn-sm btn-outline-secondary pc-f-effect-reset"' +
        (hasApplied || hasPending ? "" : " disabled") +
        ">還原原圖</button>" +
        "</div>" +
        '<div class="pc-frame-effect-status small text-muted">' +
        esc(previewLabel) +
        "</div>" +
        (hasPending
          ? '<div class="pc-frame-effect-preview"><img src="' +
            esc(frame.pendingEffectUrl) +
            '" alt="待置換效果圖" /></div>'
          : hasApplied
            ? '<div class="pc-frame-effect-preview is-applied"><img src="' +
              esc(frame.effectImageUrl) +
              '" alt="已置換效果圖" /></div>'
            : "") +
        "</div>";

      row.querySelector(".pc-frame-name").textContent =
        idx +
        1 +
        ". " +
        (slide.displayName || "植栽") +
        (hasApplied ? " · 效果圖" : "");
      var statusEl = row.querySelector(".pc-frame-effect-status");
      var genBtn = row.querySelector(".pc-f-effect-gen");
      var applyBtn = row.querySelector(".pc-f-effect-apply");
      var resetBtn = row.querySelector(".pc-f-effect-reset");

      row.querySelector(".pc-frame-pick").addEventListener("click", function () {
        selectedPlantId = slide.plantId;
        renderDetail();
        refreshPreview();
      });

      genBtn.addEventListener("click", async function () {
        if (!photoId) {
          statusEl.textContent = "此盆尚無封面照片，無法生成。";
          return;
        }
        var force = !!(frame.effectImageUrl || frame.pendingEffectUrl || frame.pendingBaseUrl);
        var confirmText = force
          ? "確定要重新生成效果圖嗎？這會再次消耗圖片 token。"
          : "確定要生成效果圖嗎？這會消耗圖片 token。";
        if (!window.confirm(confirmText)) return;
        selectedPlantId = slide.plantId;
        genBtn.disabled = true;
        applyBtn.disabled = true;
        statusEl.textContent = "正在製作效果圖，請稍候…";
        try {
          var endpoint = force
            ? "/PlantEffectImage/Regenerate?photoId=" +
              encodeURIComponent(photoId)
            : "/PlantEffectImage/Generate?photoId=" +
              encodeURIComponent(photoId);
          var response = await fetch(endpoint, { method: "POST" });
          var data = await response.json().catch(function () {
            return {};
          });
          if (!response.ok) throw new Error(data.error || "效果圖生成失敗。");
          frame.pendingBaseId = null;
          frame.pendingBaseUrl = null;
          frame.pendingEffectId = data.id;
          frame.pendingEffectUrl = data.generatedImageUrl;
          frame.pendingEffectIsComposedFinal = !!data.isComposedFinal;
          statusEl.textContent = "效果圖已生成，可直接查看或置換。";
          if (window.PlantEffectImageUI && data.generatedImageUrl) {
            window.PlantEffectImageUI.showResult(data, genBtn, {
              confirm: false
            });
          }
          genBtn.disabled = false;
          applyBtn.disabled = false;
          renderDetail();
          refreshPreview();
        } catch (err) {
          statusEl.textContent = err.message || "效果圖生成失敗。";
          genBtn.disabled = false;
          applyBtn.disabled = !(frame.pendingEffectUrl || frame.effectImageUrl);
        }
      });

      applyBtn.addEventListener("click", function () {
        if (!frame.pendingEffectUrl && !frame.effectImageUrl) {
          statusEl.textContent = "請先生成效果圖。";
          return;
        }
        if (frame.pendingEffectUrl) {
          frame.effectImageId = frame.pendingEffectId;
          frame.effectImageIsComposedFinal = !!frame.pendingEffectIsComposedFinal;
          frame.effectImageUrl = frame.pendingEffectUrl;
          frame.pendingEffectId = null;
          frame.pendingEffectIsComposedFinal = false;
          frame.pendingEffectUrl = null;
          frame.pendingBaseId = null;
          frame.pendingBaseUrl = null;
        }
        selectedPlantId = slide.plantId;
        statusEl.textContent = "已置換到編輯預覽。記得按左側「儲存」寫入首頁。";
        renderDetail();
        refreshPreview();
      });

      resetBtn.addEventListener("click", function () {
        frame.effectImageId = null;
        frame.effectImageIsComposedFinal = false;
        frame.effectImageUrl = null;
        frame.pendingEffectId = null;
        frame.pendingEffectIsComposedFinal = false;
        frame.pendingEffectUrl = null;
        frame.pendingBaseId = null;
        frame.pendingBaseUrl = null;
        selectedPlantId = slide.plantId;
        statusEl.textContent = "已還原原圖。記得按「儲存」。";
        renderDetail();
        refreshPreview();
      });

      frameListEl.appendChild(row);
    });
  }

  function clamp(n, min, max) {
    return Math.min(max, Math.max(min, n));
  }

  function esc(s) {
    return String(s == null ? "" : s)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function placeholder(extraClass) {
    return '<div class="pc-placeholder-visual ' + (extraClass || "") + '"></div>';
  }

  function slidesFor(module) {
    var catalog = catalogFor(module.id);
    ensureFrames(module);
    return catalog.map(function (s) {
      var frame = getFrame(module, s.plantId);
      hydrateLatestEffect(frame, s);
      var originalCover = resolveOriginalCoverPath(s);
      var displayPath = frame.effectImageUrl || frame.pendingEffectUrl || originalCover;
      return Object.assign({}, s, {
        zoom: 1,
        focusX: 50,
        focusY: 50,
        sourceZoom: 1,
        sourceFocusX: 50,
        sourceFocusY: 50,
        cardX: frame.cardX,
        cardY: frame.cardY,
        coverImagePath: displayPath,
        originalCoverImagePath: originalCover,
        effectImageUrl: frame.effectImageUrl || frame.pendingEffectUrl || null,
        effectImageIsComposedFinal:
          frame.effectImageUrl
            ? !!frame.effectImageIsComposedFinal
            : frame.pendingEffectUrl
              ? !!frame.pendingEffectIsComposedFinal
              : !!s.latestEffectIsComposedFinal,
        coverImageId: s.coverImageId || null
      });
    });
  }

  /** Catalog must keep real cover; never treat /effects/ path as 原圖. */
  function resolveOriginalCoverPath(s) {
    if (!s) return null;
    var plantId = s.plantId;
    var fromCatalog = coverUrlFromPhotoCatalog(plantId);
    if (fromCatalog) return fromCatalog;

    var cover = s.coverImagePath;
    if (!cover) return null;
    var coverStr = String(cover);
    if (coverStr.indexOf("/effects/") >= 0) return null;
    return cover;
  }

  function coverUrlFromPhotoCatalog(plantId) {
    if (!plantId || !photoCatalog.length) return null;
    var want = String(plantId).toLowerCase();
    for (var i = 0; i < photoCatalog.length; i++) {
      var group = photoCatalog[i];
      if (String(group.plantId || "").toLowerCase() !== want) continue;
      var photos = group.photos || [];
      var cover = null;
      for (var j = 0; j < photos.length; j++) {
        var ph = photos[j];
        if (!ph || !ph.url) continue;
        if (String(ph.url).indexOf("/effects/") >= 0) continue;
        if (ph.isCover) return ph.url;
        if (!cover) cover = ph.url;
      }
      return cover;
    }
    return null;
  }

  // 修正舊 seed：若 previewSlides 的 cover 已被效果圖覆寫，改回相簿封面
  Object.keys(plantCatalogByModule).forEach(function (key) {
    var list = plantCatalogByModule[key];
    if (!Array.isArray(list)) return;
    plantCatalogByModule[key] = list.map(function (s) {
      var fixed = Object.assign({}, s);
      var realCover = coverUrlFromPhotoCatalog(fixed.plantId);
      if (realCover) fixed.coverImagePath = realCover;
      return fixed;
    });
  });

  function renderDetailWall(m) {
    var slides = slidesFor(m);
    var activeIdx = 0;
    slides.forEach(function (s, i) {
      if (
        selectedModuleId === m.id &&
        selectedPlantId &&
        samePlantId(s.plantId, selectedPlantId)
      ) {
        activeIdx = i;
      }
    });
    var dotsHtml =
      slides.length > 1
        ? '<div class="pc-detail-wall-dots" role="tablist" aria-label="投影片">' +
          slides
            .map(function (_s, i) {
              var on = i === activeIdx;
              return (
                '<button type="button" class="pc-detail-wall-dot' +
                (on ? " is-active" : "") +
                '" data-slide-to="' +
                i +
                '" role="tab" aria-selected="' +
                (on ? "true" : "false") +
                '" aria-label="第 ' +
                (i + 1) +
                ' 張"></button>'
              );
            })
            .join("") +
          "</div>"
        : "";
    var slidesHtml = slides
      .map(function (s, i) {
        var imgPath = s.coverImagePath;
        var isEffect = !!(s.effectImageUrl || (imgPath && String(imgPath).indexOf("/effects/") >= 0));
        var facts = Array.isArray(s.careFacts) ? s.careFacts : [];
        var img = imgPath
          ? '<img src="' + esc(imgPath) + '" alt="' + esc(s.displayName) + '" draggable="false" />'
          : placeholder("pc-detail-wall-ph");
        var active =
          selectedModuleId === m.id && selectedPlantId
            ? samePlantId(s.plantId, selectedPlantId)
            : i === 0;
        var mediaClass = isEffect ? " is-effect" : "";
        var hasPlant =
          s.plantId && String(s.plantId).indexOf("00000000") !== 0;
        var nameHtml = hasPlant
          ? '<a class="pc-detail-wall-name-link" href="/Plant/Details/' +
            esc(s.plantId) +
            '">' +
            esc(s.displayName) +
            "</a>"
          : esc(s.displayName);
        var factsHtml =
          facts.length > 0
            ? '<div class="pc-detail-wall-facts care-facts">' +
              facts
                .map(function (f) {
                  return (
                    "<div><span>" +
                    esc(f.label || "") +
                    "</span>" +
                    esc(f.value || "未設定") +
                    "</div>"
                  );
                })
                .join("") +
              "</div>"
            : "";
        return (
          '<article class="pc-detail-wall-slide' +
          (active ? " is-active" : "") +
          '" data-slide-index="' +
          i +
          '" data-plant-id="' +
          esc(s.plantId || "") +
          '">' +
          '<div class="pc-detail-wall-media pc-frame-stage' +
          mediaClass +
          '">' +
          '<div class="pc-detail-wall-slide-media is-active' +
          (isEffect ? " is-effect" : "") +
          '" data-plant-id="' +
          esc(s.plantId || "") +
          '">' +
          '<div class="pc-detail-wall-view">' +
          img +
          "</div>" +
          "</div>" +
          dotsHtml +
          "</div>" +
          '<div class="pc-detail-wall-copy">' +
          '<div class="pc-detail-wall-slide-copy is-active">' +
          '<h3 class="pc-detail-wall-name">' +
          nameHtml +
          "</h3>" +
          (s.scientificName
            ? '<p class="pc-detail-wall-sci">' + esc(s.scientificName) + "</p>"
            : "") +
          '<p class="pc-detail-wall-intro">' +
          esc(s.intro || "尚無介紹") +
          "</p>" +
          factsHtml +
          "</div></div></article>"
        );
      })
      .join("");

    return (
      '<section class="pc-module pc-detail-wall" data-module-id="' +
      esc(m.id) +
      '">' +
      '<div class="pc-detail-wall-carousel" data-pc-carousel data-interval="8500" data-module-id="' +
      esc(m.id) +
      '">' +
      '<div class="pc-detail-wall-slides">' +
      slidesHtml +
      "</div>" +
      "</div></section>"
    );
  }

  function renderNotificationReminders(m) {
    if (!previewNotifications.length) return "";
    var rows = previewNotifications
      .map(function (item) {
        var action = "";
        if (item.showCompleteWater) {
          action = '<span class="btn btn-sm btn-emphasis disabled">已澆水</span>';
        } else if (item.showCompleteFertilize) {
          action = '<span class="btn btn-sm btn-emphasis disabled">已施肥</span>';
        }
        var msg =
          item.message && !item.showCompleteWater && !item.showCompleteFertilize
            ? '<span class="pc-notify-msg">' + esc(item.message) + "</span>"
            : "";
        return (
          '<li class="pc-notify-row">' +
          '<div class="pc-notify-link">' +
          '<span class="pc-notify-headline">' +
          esc(item.headline || "") +
          "</span>" +
          msg +
          "</div>" +
          (action ? '<div class="pc-notify-action">' + action + "</div>" : "") +
          "</li>"
        );
      })
      .join("");
    return (
      '<section class="pc-module pc-notify" data-module-id="' +
      esc(m.id) +
      '">' +
      '<h2 class="pc-notify-title">' +
      esc(m.title || "通知提醒") +
      "</h2>" +
      '<ul class="pc-notify-list">' +
      rows +
      "</ul></section>"
    );
  }

  function renderModuleHtml(m) {
    var theme = esc(m.theme || "theme-default");
    if (isPlantDetailWall(m)) return renderDetailWall(m);
    if (m.type === "NotificationReminders") return renderNotificationReminders(m);
    if (m.type === "Banner") {
      var bImg = m.images && m.images[0];
      var bUrl = bImg && bImg.url && !bImg.missing ? bImg.url : "";
      var bCopy =
        (m.title ? '<h1 class="pc-banner-title">' + esc(m.title) + "</h1>" : "") +
        (m.subtitle ? '<p class="pc-banner-subtitle">' + esc(m.subtitle) + "</p>" : "") +
        (m.ctaText
          ? '<a class="pc-banner-cta" href="' + esc(m.ctaHref || "#") + '">' + esc(m.ctaText) + "</a>"
          : "");
      return (
        '<section class="pc-module pc-banner ' +
        theme +
        '" data-module-type="Banner" aria-label="' +
        esc(m.title || "Banner") +
        '">' +
        '<div class="pc-banner-media">' +
        (bUrl
          ? '<img src="' + esc(bUrl) + '" alt="" loading="eager" />'
          : placeholder("pc-placeholder-banner")) +
        "</div>" +
        (bCopy ? '<div class="pc-banner-content">' + bCopy + "</div>" : "") +
        "</section>"
      );
    }
    if (m.type === "LeftImageRightText" || m.type === "RightImageLeftText") {
      var splitClass = m.type === "LeftImageRightText" ? "pc-split-left" : "pc-split-right";
      var copy =
        '<div class="pc-split-copy">' +
        (m.subtitle ? '<p class="pc-eyebrow">' + esc(m.subtitle) + "</p>" : "") +
        '<h2 class="pc-heading">' +
        esc(m.title) +
        "</h2>" +
        (m.body ? '<p class="pc-body">' + esc(m.body) + "</p>" : "") +
        "</div>";
      var mediaWrap = '<div class="pc-split-media">' + placeholder() + "</div>";
      var inner = m.type === "LeftImageRightText" ? mediaWrap + copy : copy + mediaWrap;
      return (
        '<section class="pc-module pc-split ' + splitClass + " " + theme + '">' + inner + "</section>"
      );
    }
    if (m.type === "MultiImageMultiText") {
      var items = (m.items || [])
        .map(function (it) {
          return (
            '<article class="pc-multi-text-item">' +
            '<div class="pc-multi-text-media">' +
            placeholder("pc-placeholder-sm") +
            "</div>" +
            '<h3 class="pc-subheading">' +
            esc(it.title) +
            "</h3>" +
            (it.body ? '<p class="pc-body-sm">' + esc(it.body) + "</p>" : "") +
            "</article>"
          );
        })
        .join("");
      return (
        '<section class="pc-module pc-multi-text ' +
        theme +
        '">' +
        '<div class="pc-section-intro">' +
        (m.subtitle ? '<p class="pc-eyebrow">' + esc(m.subtitle) + "</p>" : "") +
        '<h2 class="pc-heading">' +
        esc(m.title) +
        "</h2>" +
        (m.body ? '<p class="pc-body">' + esc(m.body) + "</p>" : "") +
        "</div>" +
        '<div class="pc-multi-text-grid">' +
        items +
        "</div></section>"
      );
    }
    var cells = (m.images || [])
      .map(function (img) {
        return (
          '<figure class="pc-multi-image-cell">' +
          placeholder() +
          (img.alt ? '<figcaption class="pc-caption">' + esc(img.alt) + "</figcaption>" : "") +
          "</figure>"
        );
      })
      .join("");
    return (
      '<section class="pc-module pc-multi-image ' +
      theme +
      '">' +
      '<div class="pc-section-intro">' +
      (m.subtitle ? '<p class="pc-eyebrow">' + esc(m.subtitle) + "</p>" : "") +
      '<h2 class="pc-heading">' +
      esc(m.title) +
      "</h2></div>" +
      '<div class="pc-multi-image-grid">' +
      cells +
      "</div></section>"
    );
  }

  function bindCarousels(root) {
    root.querySelectorAll("[data-pc-carousel]").forEach(function (el) {
      if (el.dataset.pcBound) return;
      el.dataset.pcBound = "1";
      var slides = Array.prototype.slice.call(el.querySelectorAll(".pc-detail-wall-slide"));
      var dots = Array.prototype.slice.call(el.querySelectorAll(".pc-detail-wall-dot"));
      var count = slides.length;
      var moduleId = el.getAttribute("data-module-id");
      var pauseAuto =
        moduleId && moduleId === selectedModuleId && selectedPlantId;

      if (count === 0) return;
      if (count <= 1) return;

      var index = 0;
      function plantIdOf(slide) {
        return String((slide && slide.getAttribute("data-plant-id")) || "").toLowerCase();
      }

      function currentIndex() {
        if (moduleId === selectedModuleId && selectedPlantId) {
          var want = String(selectedPlantId).toLowerCase();
          var byPlant = slides.findIndex(function (s) {
            return plantIdOf(s) === want;
          });
          if (byPlant >= 0) return byPlant;
        }
        var byActive = slides.findIndex(function (s) {
          return s.classList.contains("is-active");
        });
        return byActive >= 0 ? byActive : index;
      }

      function syncDots(active) {
        dots.forEach(function (dot) {
          var on = parseInt(dot.getAttribute("data-slide-to") || "-1", 10) === active;
          dot.classList.toggle("is-active", on);
          dot.setAttribute("aria-selected", on ? "true" : "false");
        });
      }

      function show(i) {
        index = ((i % count) + count) % count;
        slides.forEach(function (node, n) {
          node.classList.toggle("is-active", n === index);
        });
        syncDots(index);
      }

      function goTo(nextIndex) {
        nextIndex = ((nextIndex % count) + count) % count;
        var slide = slides[nextIndex];
        var plantId = slide && slide.getAttribute("data-plant-id");
        if (moduleId === selectedModuleId && plantId) {
          selectedPlantId = plantId;
          renderDetail();
          refreshPreview();
          return;
        }
        show(nextIndex);
      }

      dots.forEach(function (dot) {
        dot.addEventListener("click", function (e) {
          e.preventDefault();
          e.stopPropagation();
          goTo(parseInt(dot.getAttribute("data-slide-to") || "0", 10));
        });
      });

      // 進頁先對齊作用中投影片（避免多張同時佔高）
      show(currentIndex());

      if (pauseAuto) return;

      var intervalMs = parseInt(el.getAttribute("data-interval") || "8500", 10) || 8500;
      var timer = setInterval(function () {
        index = (currentIndex() + 1) % count;
        show(index);
      }, intervalMs);
      el.addEventListener("mouseenter", function () {
        clearInterval(timer);
      });
      el.addEventListener("mouseleave", function () {
        clearInterval(timer);
        timer = setInterval(function () {
          index = (currentIndex() + 1) % count;
          show(index);
        }, intervalMs);
      });
    });
  }

  function refreshPreview() {
    if (!previewEl) return;
    var shell = previewEl.querySelector(".pc-shell");
    var scrollTop = shell ? shell.scrollTop : 0;
    pinBannerFirst();
    var html = layout.modules
      .filter(function (m) {
        return m.enabled;
      })
      .map(renderModuleHtml)
      .join("");
    var header =
      '<header class="pc-site-header">' +
      '<a class="pc-brand" href="/Plant/List">我的植栽</a>' +
      '<nav class="pc-nav" aria-label="主選單">' +
      '<a href="/Plant">首頁</a>' +
      '<a href="/Plant/List">我的植栽</a>' +
      '<a href="/Plant/Create">新增植栽</a>' +
      '<a href="/Settings">設定</a>' +
      "</nav></header>";
    previewEl.innerHTML = '<div class="pc-shell is-preview">' + header + html + "</div>";
    var newShell = previewEl.querySelector(".pc-shell");
    if (newShell) newShell.scrollTop = scrollTop;
    bindCarousels(previewEl);
    // 預覽頂欄改靜態，不綁首頁 chrome（避免 sticky／絕對定位干擾）
  }

  if (addBtn && addTypeEl) {
    addBtn.addEventListener("click", function () {
      if (addTypeEl.value === "Banner") return;
      var mod = createModule(addTypeEl.value);
      layout.modules.push(mod);
      pinBannerFirst();
      if (isPlantDetailWall(mod)) {
        plantCatalogByModule[mod.id] = catalogFor(mod.id).slice();
        ensureFrames(mod);
        selectModule(mod.id);
      } else {
        renderList();
        refreshPreview();
      }
    });
  }

  formEl.addEventListener("submit", function () {
    pinBannerFirst();
    layout.modules.forEach(function (m) {
      // 統一 type 字串，避免數字／大小寫導致 slideFrames 被清空
      if (m.type == null && m.Type != null) m.type = m.Type;
      m.type = moduleType(m);

      if (isPlantDetailWall(m)) {
        m.type = "PlantDetailWallCarousel";
        ensureFrames(m);
        m.slideFrames = (m.slideFrames || [])
          .map(function (f) {
            var pid = String(f.plantId || "").trim();
            if (!pid || pid.indexOf("00000000") === 0) return null;
            return {
              plantId: pid,
              zoom: 1,
              focusX: 50,
              focusY: 50,
              sourceZoom: 1,
              sourceFocusX: 50,
              sourceFocusY: 50,
              effectImageId: f.effectImageId || null,
              effectImageIsComposedFinal: !!f.effectImageIsComposedFinal,
              effectImageUrl: f.effectImageUrl || null,
              cardX: clamp(parseFloat(f.cardX) || 6, 0, 72),
              cardY: clamp(parseFloat(f.cardY) || 22, 0, 78)
            };
          })
          .filter(Boolean);
      } else {
        m.slideFrames = [];
      }
      if (m.type === "Banner") m.enabled = true;
    });
    layout.version = 2;
    jsonInput.value = JSON.stringify({
      version: layout.version,
      modules: layout.modules
    });
  });

  // Normalize module types once at boot (numeric enum → name)
  layout.modules.forEach(function (m) {
    if (isPlantDetailWall(m)) m.type = "PlantDetailWallCarousel";
    else if (moduleType(m) === "6" || moduleType(m) === "NotificationReminders")
      m.type = "NotificationReminders";
    else if (moduleType(m) === "0" || moduleType(m) === "Banner") m.type = "Banner";
  });

  pinBannerFirst();
  renderList();
  renderDetail();
  refreshPreview();
})();
