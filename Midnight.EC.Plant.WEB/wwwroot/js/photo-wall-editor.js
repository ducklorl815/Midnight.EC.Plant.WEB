(function () {
    const root = document.getElementById("wall-editor");
    if (!root) return;

    const grid = document.getElementById("wall-editor-grid");
    const gapX = document.getElementById("wall-gap-x");
    const gapY = document.getElementById("wall-gap-y");
    const seamTool = document.getElementById("wall-seam-tool");
    const seamGap = document.getElementById("wall-seam-gap");
    const seamLabel = document.getElementById("wall-seam-label");
    const colSpanInput = document.getElementById("wall-col-span");
    const rowSpanInput = document.getElementById("wall-row-span");
    const zoomInput = document.getElementById("wall-zoom");
    const selectedName = document.getElementById("wall-selected-name");
    const form = document.getElementById("wall-save-form");
    const hiddenJson = document.getElementById("wall-layout-json");

    let layout;
    try {
        const seed = document.getElementById("wall-layout-seed");
        layout = JSON.parse(seed ? seed.textContent : "{}");
    } catch {
        layout = { version: 1, columns: 4, gapX: 14, gapY: 14, tiles: [], colGaps: [], rowGaps: [] };
    }

    layout.columns = layout.columns || 4;
    layout.gapX = Number(layout.gapX ?? 14);
    layout.gapY = Number(layout.gapY ?? 14);
    layout.tiles = Array.isArray(layout.tiles) ? layout.tiles : [];
    layout.colGaps = Array.isArray(layout.colGaps) ? layout.colGaps.map(Number) : [];
    layout.rowGaps = Array.isArray(layout.rowGaps) ? layout.rowGaps.map(Number) : [];

    let selected = null;
    let selectedSeam = null; // { type: 'col'|'row', index }
    let dragEl = null;
    let panState = null;

    function tiles() {
        return Array.from(grid.querySelectorAll(".wall-tile"));
    }

    function contentLine(i) {
        return i * 2 + 1;
    }
    function spanTracks(span) {
        return span * 2 - 1;
    }
    function resolveColGap(i) {
        return layout.colGaps[i] != null ? Number(layout.colGaps[i]) : layout.gapX;
    }
    function resolveRowGap(i) {
        return layout.rowGaps[i] != null ? Number(layout.rowGaps[i]) : layout.gapY;
    }

    function ensureGapArrays(rowCount) {
        const colN = Math.max(0, layout.columns - 1);
        while (layout.colGaps.length < colN) layout.colGaps.push(layout.gapX);
        layout.colGaps = layout.colGaps.slice(0, colN).map((g, i) => (g == null ? layout.gapX : Number(g)));

        const rowN = Math.max(0, rowCount - 1);
        while (layout.rowGaps.length < rowN) layout.rowGaps.push(layout.gapY);
        layout.rowGaps = layout.rowGaps.slice(0, rowN).map((g, i) => (g == null ? layout.gapY : Number(g)));
    }

    function pack(tileList) {
        let columns = Math.max(2, Number(layout.columns) || 4);
        for (const el of tileList) {
            columns = Math.max(columns, Math.max(1, Number(el.dataset.colSpan || 1)));
        }
        layout.columns = columns;

        const occupied = new Set();
        const key = (r, c) => `${r}:${c}`;
        const result = [];

        for (const el of tileList) {
            const colSpan = Math.max(1, Number(el.dataset.colSpan || 1));
            const rowSpan = Math.max(1, Number(el.dataset.rowSpan || 1));
            let placed = false;
            for (let row = 0; !placed; row++) {
                for (let col = 0; col <= columns - colSpan; col++) {
                    let fits = true;
                    for (let r = row; r < row + rowSpan && fits; r++) {
                        for (let c = col; c < col + colSpan; c++) {
                            if (occupied.has(key(r, c))) {
                                fits = false;
                                break;
                            }
                        }
                    }
                    if (!fits) continue;
                    for (let r = row; r < row + rowSpan; r++) {
                        for (let c = col; c < col + colSpan; c++) occupied.add(key(r, c));
                    }
                    result.push({ el, col, row, colSpan, rowSpan });
                    placed = true;
                    break;
                }
            }
        }
        return result;
    }

    function applyFocus(el) {
        const zoom = Number(el.dataset.zoom || 1);
        const fx = Number(el.dataset.focusX ?? 0);
        const fy = Number(el.dataset.focusY ?? 100);
        el.style.setProperty("--zoom", String(zoom));
        el.style.setProperty("--focus-x", `${fx}%`);
        el.style.setProperty("--focus-y", `${fy}%`);
    }

    function rebuildSeams(rowCount) {
        grid.querySelectorAll(".wall-seam").forEach((n) => n.remove());
        for (let i = 0; i < layout.columns - 1; i++) {
            const btn = document.createElement("button");
            btn.type = "button";
            btn.className = "wall-seam wall-seam-col";
            btn.dataset.seam = "col";
            btn.dataset.index = String(i);
            btn.style.gridColumn = String(i * 2 + 2);
            btn.style.gridRow = "1 / -1";
            btn.title = `直縫 ${i + 1}`;
            btn.setAttribute("aria-label", `直縫 ${i + 1}`);
            btn.addEventListener("click", (e) => {
                e.stopPropagation();
                selectSeam("col", i);
            });
            grid.appendChild(btn);
        }
        for (let i = 0; i < Math.max(0, rowCount - 1); i++) {
            const btn = document.createElement("button");
            btn.type = "button";
            btn.className = "wall-seam wall-seam-row";
            btn.dataset.seam = "row";
            btn.dataset.index = String(i);
            btn.style.gridColumn = "1 / -1";
            btn.style.gridRow = String(i * 2 + 2);
            btn.title = `橫縫 ${i + 1}`;
            btn.setAttribute("aria-label", `橫縫 ${i + 1}`);
            btn.addEventListener("click", (e) => {
                e.stopPropagation();
                selectSeam("row", i);
            });
            grid.appendChild(btn);
        }
    }

    function applyLayout() {
        const packed = pack(tiles());
        const rowCount = packed.reduce((m, p) => Math.max(m, p.row + p.rowSpan), 1);
        ensureGapArrays(rowCount);

        const colParts = [];
        for (let i = 0; i < layout.columns; i++) {
            colParts.push("1fr");
            if (i < layout.columns - 1) colParts.push(`${resolveColGap(i)}px`);
        }
        const rowParts = [];
        for (let i = 0; i < rowCount; i++) {
            rowParts.push("140px");
            if (i < rowCount - 1) rowParts.push(`${resolveRowGap(i)}px`);
        }
        grid.style.gridTemplateColumns = colParts.join(" ");
        grid.style.gridTemplateRows = rowParts.join(" ");

        for (const p of packed) {
            p.el.style.gridColumn = `${contentLine(p.col)} / span ${spanTracks(p.colSpan)}`;
            p.el.style.gridRow = `${contentLine(p.row)} / span ${spanTracks(p.rowSpan)}`;
            applyFocus(p.el);
        }

        rebuildSeams(rowCount);

        if (selectedSeam) {
            const still =
                (selectedSeam.type === "col" && selectedSeam.index < layout.columns - 1) ||
                (selectedSeam.type === "row" && selectedSeam.index < rowCount - 1);
            if (still) selectSeam(selectedSeam.type, selectedSeam.index);
            else clearSeamSelection();
        }
    }

    function clearSeamSelection() {
        selectedSeam = null;
        grid.querySelectorAll(".wall-seam.is-selected").forEach((n) => n.classList.remove("is-selected"));
        seamTool.classList.add("is-disabled");
        seamGap.disabled = true;
        seamLabel.textContent = "選中縫";
        seamGap.value = "";
    }

    function selectSeam(type, index) {
        selected = null;
        tiles().forEach((t) => t.classList.remove("is-selected"));
        colSpanInput.disabled = true;
        rowSpanInput.disabled = true;
        zoomInput.disabled = true;
        selectedName.textContent = "尚未選取格子";

        selectedSeam = { type, index };
        grid.querySelectorAll(".wall-seam.is-selected").forEach((n) => n.classList.remove("is-selected"));
        const btn = grid.querySelector(`.wall-seam[data-seam="${type}"][data-index="${index}"]`);
        if (btn) btn.classList.add("is-selected");

        const value = type === "col" ? resolveColGap(index) : resolveRowGap(index);
        seamTool.classList.remove("is-disabled");
        seamGap.disabled = false;
        seamGap.value = String(value);
        seamLabel.textContent = type === "col" ? `直縫 ${index + 1}` : `橫縫 ${index + 1}`;
    }

    function selectTile(el) {
        clearSeamSelection();
        tiles().forEach((t) => {
            t.classList.remove("is-selected");
            t.draggable = true;
            t.classList.remove("is-panning");
        });
        selected = el;
        if (!el) {
            colSpanInput.disabled = true;
            rowSpanInput.disabled = true;
            zoomInput.disabled = true;
            selectedName.textContent = "尚未選取格子（直接拖格＝排序）";
            return;
        }
        el.classList.add("is-selected");
        // Selected tile: no HTML5 reorder; Alt+drag pans inside.
        el.draggable = false;
        colSpanInput.disabled = false;
        rowSpanInput.disabled = false;
        zoomInput.disabled = false;
        colSpanInput.value = el.dataset.colSpan || "1";
        rowSpanInput.value = el.dataset.rowSpan || "1";
        const zoom = Number(el.dataset.zoom || 1);
        zoomInput.value = String(Number(zoom.toFixed(3)));
        const name = el.querySelector(".wall-tile-name");
        selectedName.textContent = name
            ? `選中：${name.textContent}（Alt+拖＝取景；滾輪＝縮放）`
            : "已選取（Alt+拖＝取景）";
    }

    function clearTileSelection() {
        selectTile(null);
    }

    function parsePositiveInt(raw, fallback) {
        const n = Number.parseInt(String(raw), 10);
        if (!Number.isFinite(n) || n < 1) return fallback;
        return n;
    }

    function parseNonNegInt(raw, fallback) {
        const n = Number.parseInt(String(raw), 10);
        if (!Number.isFinite(n) || n < 0) return fallback;
        return n;
    }

    function setSpan(el, col, row) {
        el.dataset.colSpan = String(Math.max(1, col));
        el.dataset.rowSpan = String(Math.max(1, row));
        applyLayout();
    }

    function buildPayload() {
        const orderTiles = tiles().map((el, index) => ({
            plantId: el.dataset.plantId,
            order: index,
            colSpan: Number(el.dataset.colSpan || 1),
            rowSpan: Number(el.dataset.rowSpan || 1),
            zoom: Number(el.dataset.zoom || 1),
            focusX: Number(el.dataset.focusX ?? 0),
            focusY: Number(el.dataset.focusY ?? 100)
        }));
        const packed = pack(tiles());
        const rowCount = packed.reduce((m, p) => Math.max(m, p.row + p.rowSpan), 1);
        ensureGapArrays(rowCount);
        return {
            version: 1,
            columns: layout.columns,
            gapX: layout.gapX,
            gapY: layout.gapY,
            colGaps: layout.colGaps.slice(),
            rowGaps: layout.rowGaps.slice(),
            tiles: orderTiles
        };
    }

    gapX.addEventListener("change", () => {
        layout.gapX = parseNonNegInt(gapX.value, layout.gapX);
        gapX.value = String(layout.gapX);
        layout.colGaps = layout.colGaps.map(() => layout.gapX);
        while (layout.colGaps.length < layout.columns - 1) layout.colGaps.push(layout.gapX);
        applyLayout();
        if (selectedSeam && selectedSeam.type === "col") {
            seamGap.value = String(layout.gapX);
        }
    });
    gapY.addEventListener("change", () => {
        layout.gapY = parseNonNegInt(gapY.value, layout.gapY);
        gapY.value = String(layout.gapY);
        layout.rowGaps = layout.rowGaps.map(() => layout.gapY);
        applyLayout();
        if (selectedSeam && selectedSeam.type === "row") {
            seamGap.value = String(layout.gapY);
        }
    });

    seamGap.addEventListener("change", () => {
        if (!selectedSeam) return;
        const v = parseNonNegInt(seamGap.value, 0);
        seamGap.value = String(v);
        if (selectedSeam.type === "col") layout.colGaps[selectedSeam.index] = v;
        else layout.rowGaps[selectedSeam.index] = v;
        applyLayout();
    });

    function onSpanChange() {
        if (!selected) return;
        const col = parsePositiveInt(colSpanInput.value, Number(selected.dataset.colSpan || 1));
        const row = parsePositiveInt(rowSpanInput.value, Number(selected.dataset.rowSpan || 1));
        colSpanInput.value = String(col);
        rowSpanInput.value = String(row);
        setSpan(selected, col, row);
    }
    colSpanInput.addEventListener("change", onSpanChange);
    rowSpanInput.addEventListener("change", onSpanChange);
    function parsePositiveNumber(raw, fallback) {
        const n = Number(raw);
        if (!Number.isFinite(n) || n <= 0) return fallback;
        return n;
    }

    zoomInput.addEventListener("change", () => {
        if (!selected) return;
        const zoom = parsePositiveNumber(zoomInput.value, Number(selected.dataset.zoom || 1));
        zoomInput.value = String(zoom);
        selected.dataset.zoom = String(zoom);
        applyFocus(selected);
    });

    function bindTile(el) {
        el.addEventListener("click", (e) => {
            if (e.target.closest(".wall-seam")) return;
            e.preventDefault();
            // Toggle off if clicking the already-selected tile (without Alt).
            if (selected === el && !e.altKey) {
                clearTileSelection();
                return;
            }
            selectTile(el);
        });

        el.addEventListener("dragstart", (e) => {
            if (el.classList.contains("is-selected") || panState || e.altKey) {
                e.preventDefault();
                return;
            }
            dragEl = el;
            el.classList.add("is-dragging");
            e.dataTransfer.effectAllowed = "move";
            e.dataTransfer.setData("text/plain", el.dataset.plantId || "");
        });
        el.addEventListener("dragend", () => {
            el.classList.remove("is-dragging");
            tiles().forEach((t) => t.classList.remove("wall-tile-drop-target"));
            dragEl = null;
            applyLayout();
        });
        el.addEventListener("dragover", (e) => {
            e.preventDefault();
            if (!dragEl || dragEl === el) return;
            el.classList.add("wall-tile-drop-target");
            e.dataTransfer.dropEffect = "move";
        });
        el.addEventListener("dragleave", () => el.classList.remove("wall-tile-drop-target"));
        el.addEventListener("drop", (e) => {
            e.preventDefault();
            el.classList.remove("wall-tile-drop-target");
            if (!dragEl || dragEl === el) return;
            const all = tiles();
            const from = all.indexOf(dragEl);
            const to = all.indexOf(el);
            if (from < 0 || to < 0) return;
            if (from < to) el.after(dragEl);
            else el.before(dragEl);
            clearTileSelection();
            applyLayout();
        });

        el.addEventListener("wheel", (e) => {
            if (!el.classList.contains("is-selected")) return;
            e.preventDefault();
            let zoom = Number(el.dataset.zoom || 1);
            const factor = e.deltaY < 0 ? 1.08 : 1 / 1.08;
            zoom = Math.max(0.01, zoom * factor);
            el.dataset.zoom = String(zoom);
            zoomInput.value = String(Number(zoom.toFixed(3)));
            applyFocus(el);
        }, { passive: false });

        el.addEventListener("pointerdown", (e) => {
            if (!el.classList.contains("is-selected")) return;
            if (e.button !== 0) return;
            // Pan only with Alt to avoid fighting reorder.
            if (!e.altKey) return;
            panState = {
                el,
                startX: e.clientX,
                startY: e.clientY,
                focusX: Number(el.dataset.focusX ?? 0),
                focusY: Number(el.dataset.focusY ?? 100)
            };
            el.setPointerCapture(e.pointerId);
            el.classList.add("is-panning");
            e.preventDefault();
        });
        el.addEventListener("pointermove", (e) => {
            if (!panState || panState.el !== el) return;
            const dx = e.clientX - panState.startX;
            const dy = e.clientY - panState.startY;
            const rect = el.getBoundingClientRect();
            const nextX = Math.min(100, Math.max(0, panState.focusX - (dx / Math.max(rect.width, 1)) * 100));
            const nextY = Math.min(100, Math.max(0, panState.focusY - (dy / Math.max(rect.height, 1)) * 100));
            el.dataset.focusX = nextX.toFixed(2);
            el.dataset.focusY = nextY.toFixed(2);
            // Update baseline so continuous drag stays smooth.
            panState.startX = e.clientX;
            panState.startY = e.clientY;
            panState.focusX = nextX;
            panState.focusY = nextY;
            applyFocus(el);
        });
        const endPan = (e) => {
            if (!panState || panState.el !== el) return;
            panState = null;
            el.classList.remove("is-panning");
            try { el.releasePointerCapture(e.pointerId); } catch { /* ignore */ }
        };
        el.addEventListener("pointerup", endPan);
        el.addEventListener("pointercancel", endPan);
    }

    // Click empty canvas to clear selection so reorder is available again.
    document.querySelector(".wall-editor-canvas")?.addEventListener("click", (e) => {
        if (e.target.closest(".wall-tile") || e.target.closest(".wall-seam")) return;
        clearTileSelection();
    });

    tiles().forEach(bindTile);
    grid.querySelectorAll(".wall-seam").forEach((btn) => {
        btn.addEventListener("click", (e) => {
            e.stopPropagation();
            selectSeam(btn.dataset.seam, Number(btn.dataset.index));
        });
    });

    form.addEventListener("submit", () => {
        hiddenJson.value = JSON.stringify(buildPayload());
    });

    applyLayout();
})();
