// Flow editor: ported from the standalone wwwroot/index.html prototype into a
// Blazor-embeddable, multi-instance-safe module. Each call to initFlow(name, elName)
// owns its own state/closures; nothing is looked up by global document ids.

const instances = new Map();

const BLOCK_DEFS = {
    input: {
        color: '#22c55e',
        items: [
            { name: 'Data Source', icon: '🗄️', config: { source: '' } },
            { name: 'API Input', icon: '🌐', config: { url: '', method: 'GET' } },
            { name: 'File Reader', icon: '📖', config: { path: '', format: 'csv' } },
            { name: 'Sensor', icon: '📡', config: { sensorId: '', interval: 1000 } },
            { name: 'Manual Input', icon: '✏️', config: { fields: [] } }
        ]
    },
    process: {
        color: '#3b82f6',
        items: [
            { name: 'Transform', icon: '🔄', config: { mapping: {} } },
            { name: 'Filter', icon: '🔍', config: { condition: '' } },
            { name: 'Aggregate', icon: '📊', config: { function: 'sum', field: '' } },
            { name: 'Condition', icon: '🔀', config: { if: '', then: '', else: '' } },
            { name: 'NodeScript', icon: '📜', config: { code: '' } },
            { name: 'PySharp', icon: '🐍', config: { code: '' } }
        ]
    },
    output: {
        color: '#f97316',
        items: [
            { name: 'Data Output', icon: '💾', config: { destination: '' } },
            { name: 'API Output', icon: '📤', config: { url: '', method: 'POST' } },
            { name: 'File Writer', icon: '📝', config: { path: '', format: 'json' } },
            { name: 'Display', icon: '🖥️', config: { template: '' } },
            { name: 'Notification', icon: '🔔', config: { channel: 'email', message: '' } }
        ]
    }
};

const TYPE_LABELS = { input: 'Input', process: 'Process', output: 'Output' };
const GRID_SIZE = 20;

const YAML_CDN_URL = 'https://cdn.jsdelivr.net/npm/js-yaml@4.1.0/dist/js-yaml.min.js';
let yamlLoadPromise = null;

// Block config is edited as YAML in the properties panel. Loaded lazily and
// dynamically (rather than via a static <script> tag in the host page) because
// Blazor's DOM-diffing inserts elements imperatively, and browsers don't execute
// <script> tags inserted that way - only ones created via the DOM API below.
function ensureYaml() {
    if (window.jsyaml) return Promise.resolve();
    if (yamlLoadPromise) return yamlLoadPromise;
    yamlLoadPromise = new Promise((resolve, reject) => {
        // Monaco's AMD loader.js (used on the Config page) defines a global `define`
        // with `define.amd` set. js-yaml's UMD wrapper detects that and registers
        // itself as an AMD module instead of setting window.jsyaml. Hide `define`
        // while the script runs so the UMD wrapper falls through to the global branch.
        const savedDefine = window.define;
        window.define = undefined;
        const restore = () => { window.define = savedDefine; };
        const script = document.createElement('script');
        script.src = YAML_CDN_URL;
        script.onload = () => { restore(); resolve(); };
        script.onerror = () => { restore(); reject(new Error('Failed to load js-yaml from ' + YAML_CDN_URL)); };
        document.head.appendChild(script);
    });
    return yamlLoadPromise;
}

export async function initFlow(name, elName, stateJson, dotNetRef) {
    if (instances.has(name)) {
        disposeFlow(name);
    }

    const root = document.getElementById(elName);
    if (!root) return;

    await ensureYaml().catch((err) => console.error('flowUIInterop:', err));

    const inst = createInstance(name, root, dotNetRef);
    instances.set(name, inst);

    inst.initPalette();
    inst.updateCanvasTransform();

    if (stateJson) {
        try {
            inst.loadFlow(JSON.parse(stateJson));
        } catch (err) {
            console.error('flowUIInterop: invalid initial state JSON', err);
        }
    }
}

export function disposeFlow(name) {
    const inst = instances.get(name);
    if (!inst) return;
    inst.teardown();
    instances.delete(name);
}

export function getFlowJson(name) {
    const inst = instances.get(name);
    if (!inst) return null;
    return JSON.stringify(inst.serialize());
}

function createInstance(name, root, dotNetRef) {
    const state = {
        blocks: [],
        connections: [],
        selectedBlockId: null,
        selectedConnectionId: null,
        zoom: 1,
        panX: 0,
        panY: 0,
        isDirty: false,
        flowName: 'Untitled Flow',
        nextBlockNum: 1,
        nextConnNum: 1
    };

    const q = (role) => root.querySelector(`[data-role="${role}"]`);

    const $canvasWrapper = q('canvas-wrapper');
    const $canvas = q('canvas');
    const $blocksContainer = q('blocks-container');
    const $svg = q('connections-svg');
    const $zoomDisplay = q('zoom-display');
    const $flowName = q('flow-name');
    const $fileInput = q('file-input');

    const $propsEmpty = q('props-empty');
    const $propsContent = q('props-content');
    const $propId = q('prop-id');
    const $propType = q('prop-type-badge');
    const $propName = q('prop-name');
    const $propColor = q('prop-color');
    const $propColorHex = q('prop-color-hex');
    const $propX = q('prop-x');
    const $propY = q('prop-y');
    const $propConfig = q('prop-config');
    const $propConfigError = q('prop-config-error');

    const blockEls = new Map(); // blockId -> element
    const cleanups = [];
    function on(target, type, handler, opts) {
        target.addEventListener(type, handler, opts);
        cleanups.push(() => target.removeEventListener(type, handler, opts));
    }

    let pointerInside = false;
    on($canvasWrapper, 'mouseenter', () => { pointerInside = true; });
    on($canvasWrapper, 'mouseleave', () => { pointerInside = false; });

    // =====================================================================
    // PALETTE
    // =====================================================================
    function initPalette() {
        for (const [type, def] of Object.entries(BLOCK_DEFS)) {
            const container = root.querySelector(`[data-role="palette-${type}"]`);
            if (!container) continue;
            for (const item of def.items) {
                const el = document.createElement('div');
                el.className = 'palette-item flex items-center gap-2 px-2.5 py-1.5 rounded text-xs font-medium text-gray-200';
                el.style.background = hexToRgba(def.color, 0.15);
                el.style.borderLeft = `3px solid ${def.color}`;
                el.innerHTML = `<span class="text-sm">${item.icon}</span><span>${escapeHtml(item.name)}</span>`;
                el.draggable = true;
                on(el, 'dragstart', (e) => {
                    e.dataTransfer.setData('application/flow-block', JSON.stringify({ type, name: item.name, icon: item.icon, config: item.config, color: def.color }));
                    e.dataTransfer.effectAllowed = 'copy';
                    const ghost = document.createElement('div');
                    ghost.className = 'drag-ghost';
                    ghost.style.background = def.color;
                    ghost.textContent = `${item.icon} ${item.name}`;
                    document.body.appendChild(ghost);
                    e.dataTransfer.setDragImage(ghost, 60, 18);
                    requestAnimationFrame(() => ghost.remove());
                });
                container.appendChild(el);
            }
        }
    }

    // =====================================================================
    // CANVAS - DROP, PAN, ZOOM
    // =====================================================================
    on($canvasWrapper, 'dragover', (e) => {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'copy';
    });

    on($canvasWrapper, 'drop', (e) => {
        e.preventDefault();
        const raw = e.dataTransfer.getData('application/flow-block');
        if (!raw) return;
        const data = JSON.parse(raw);
        const rect = $canvasWrapper.getBoundingClientRect();
        const x = snapToGrid((e.clientX - rect.left - state.panX) / state.zoom);
        const y = snapToGrid((e.clientY - rect.top - state.panY) / state.zoom);
        addBlock(data.type, data.name, data.icon, x, y, data.config, data.color);
    });

    let isPanning = false, panStartX = 0, panStartY = 0;
    on($canvasWrapper, 'mousedown', (e) => {
        if (e.button === 1 || (e.button === 0 && e.altKey) || e.button === 2) {
            isPanning = true;
            panStartX = e.clientX - state.panX;
            panStartY = e.clientY - state.panY;
            $canvasWrapper.style.cursor = 'grabbing';
            e.preventDefault();
        }
    });
    on($canvasWrapper, 'contextmenu', (e) => e.preventDefault());

    on(window, 'mousemove', (e) => {
        if (isPanning) {
            state.panX = e.clientX - panStartX;
            state.panY = e.clientY - panStartY;
            updateCanvasTransform();
        }
        if (isDraggingBlock) dragBlockMove(e);
        if (isDrawingConnection) drawTempConnection(e);
    });

    on(window, 'mouseup', (e) => {
        if (isPanning) {
            isPanning = false;
            $canvasWrapper.style.cursor = '';
        }
        if (isDraggingBlock) dragBlockEnd();
        if (isDrawingConnection) endConnection(e);
    });

    on($canvasWrapper, 'wheel', (e) => {
        if (e.ctrlKey || e.metaKey) {
            e.preventDefault();
            const delta = e.deltaY > 0 ? -0.1 : 0.1;
            const newZoom = Math.max(0.2, Math.min(3, state.zoom + delta));
            const rect = $canvasWrapper.getBoundingClientRect();
            const mx = e.clientX - rect.left;
            const my = e.clientY - rect.top;
            state.panX = mx - (mx - state.panX) * (newZoom / state.zoom);
            state.panY = my - (my - state.panY) * (newZoom / state.zoom);
            state.zoom = newZoom;
            updateCanvasTransform();
        }
    }, { passive: false });

    function updateCanvasTransform() {
        $canvas.style.transform = `translate(${state.panX}px, ${state.panY}px) scale(${state.zoom})`;
        $zoomDisplay.textContent = `${Math.round(state.zoom * 100)}%`;
    }

    function zoomIn() {
        state.zoom = Math.min(3, state.zoom + 0.15);
        updateCanvasTransform();
    }
    function zoomOut() {
        state.zoom = Math.max(0.2, state.zoom - 0.15);
        updateCanvasTransform();
    }
    function zoomReset() {
        state.zoom = 1;
        state.panX = 0;
        state.panY = 0;
        updateCanvasTransform();
    }

    on($canvasWrapper, 'mousedown', (e) => {
        if (e.target === $canvasWrapper || e.target === $canvas || e.target === $blocksContainer) {
            if (e.button === 0 && !e.altKey) deselectAll();
        }
    });

    // =====================================================================
    // BLOCKS
    // =====================================================================
    function addBlock(type, name_, icon, x, y, config, color) {
        const id = `block_${state.nextBlockNum++}`;
        const block = { id, type, name: name_, icon, x, y, config: JSON.parse(JSON.stringify(config || {})), color };
        state.blocks.push(block);
        state.isDirty = true;
        renderBlock(block);
        selectBlock(id);
        return block;
    }

    function renderBlock(block) {
        const old = blockEls.get(block.id);
        if (old) old.remove();

        const el = document.createElement('div');
        el.className = 'flow-block';
        el.style.left = block.x + 'px';
        el.style.top = block.y + 'px';
        el.style.background = hexToRgba(block.color, 0.2);
        el.style.borderColor = hexToRgba(block.color, 0.4);

        el.innerHTML = `
            <div class="connector connector-in" data-dir="in"></div>
            <div class="connector connector-out" data-dir="out"></div>
            <div class="flex items-center gap-2">
                <span class="text-lg">${block.icon}</span>
                <div>
                    <div class="text-xs font-bold text-gray-100 block-name">${escapeHtml(block.name)}</div>
                    <div class="text-[10px] text-gray-400 uppercase">${TYPE_LABELS[block.type]}</div>
                </div>
            </div>
        `;

        if (state.selectedBlockId === block.id) el.classList.add('selected');

        el.addEventListener('mousedown', (e) => {
            if (e.target.classList.contains('connector')) return;
            if (e.button === 0) {
                selectBlock(block.id);
                dragBlockStart(block.id, e);
                e.stopPropagation();
            }
        });

        el.querySelectorAll('.connector').forEach(conn => {
            conn.addEventListener('mousedown', (e) => {
                if (e.button === 0) {
                    e.stopPropagation();
                    startConnection(block.id, conn.dataset.dir, e);
                }
            });
        });

        blockEls.set(block.id, el);
        $blocksContainer.appendChild(el);
    }

    function removeBlock(id) {
        const idx = state.blocks.findIndex(b => b.id === id);
        if (idx === -1) return;
        state.blocks.splice(idx, 1);
        state.connections = state.connections.filter(c => c.from !== id && c.to !== id);
        const el = blockEls.get(id);
        if (el) el.remove();
        blockEls.delete(id);
        if (state.selectedBlockId === id) {
            state.selectedBlockId = null;
            updatePropsPanel();
        }
        state.isDirty = true;
        renderConnections();
    }

    // =====================================================================
    // BLOCK DRAG ON CANVAS
    // =====================================================================
    let isDraggingBlock = false, dragBlockId = null, dragOffsetX = 0, dragOffsetY = 0;

    function dragBlockStart(id, e) {
        isDraggingBlock = true;
        dragBlockId = id;
        const block = state.blocks.find(b => b.id === id);
        const rect = $canvasWrapper.getBoundingClientRect();
        dragOffsetX = (e.clientX - rect.left - state.panX) / state.zoom - block.x;
        dragOffsetY = (e.clientY - rect.top - state.panY) / state.zoom - block.y;
        const el = blockEls.get(id);
        if (el) el.classList.add('dragging');
    }

    function dragBlockMove(e) {
        if (!isDraggingBlock) return;
        const block = state.blocks.find(b => b.id === dragBlockId);
        if (!block) return;
        const rect = $canvasWrapper.getBoundingClientRect();
        let nx = (e.clientX - rect.left - state.panX) / state.zoom - dragOffsetX;
        let ny = (e.clientY - rect.top - state.panY) / state.zoom - dragOffsetY;
        block.x = snapToGrid(Math.max(0, nx));
        block.y = snapToGrid(Math.max(0, ny));
        const el = blockEls.get(dragBlockId);
        if (el) {
            el.style.left = block.x + 'px';
            el.style.top = block.y + 'px';
        }
        renderConnections();
        updatePropsPosition(block);
        state.isDirty = true;
    }

    function dragBlockEnd() {
        if (!isDraggingBlock) return;
        const el = blockEls.get(dragBlockId);
        if (el) el.classList.remove('dragging');
        isDraggingBlock = false;
        dragBlockId = null;
    }

    // =====================================================================
    // CONNECTIONS
    // =====================================================================
    let isDrawingConnection = false, connFromId = null, tempLine = null;

    function startConnection(blockId, dir) {
        if (dir !== 'out') return;
        isDrawingConnection = true;
        connFromId = blockId;
        tempLine = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        tempLine.setAttribute('stroke', '#60a5fa');
        tempLine.setAttribute('stroke-width', '2');
        tempLine.setAttribute('fill', 'none');
        tempLine.setAttribute('stroke-dasharray', '6 4');
        tempLine.classList.add('temp-conn');
        $svg.appendChild(tempLine);
    }

    function drawTempConnection(e) {
        if (!isDrawingConnection || !tempLine) return;
        const fromEl = blockEls.get(connFromId);
        if (!fromEl) return;
        const fromConn = fromEl.querySelector('.connector-out');
        const fromRect = fromConn.getBoundingClientRect();
        const wrapperRect = $canvasWrapper.getBoundingClientRect();

        const x1 = (fromRect.left + fromRect.width / 2 - wrapperRect.left - state.panX) / state.zoom;
        const y1 = (fromRect.top + fromRect.height / 2 - wrapperRect.top - state.panY) / state.zoom;
        const x2 = (e.clientX - wrapperRect.left - state.panX) / state.zoom;
        const y2 = (e.clientY - wrapperRect.top - state.panY) / state.zoom;

        const cpOffset = Math.abs(x2 - x1) * 0.5 + 30;
        const d = `M ${x1} ${y1} C ${x1 + cpOffset} ${y1}, ${x2 - cpOffset} ${y2}, ${x2} ${y2}`;
        tempLine.setAttribute('d', d);
    }

    function endConnection(e) {
        if (!isDrawingConnection) return;
        isDrawingConnection = false;
        if (tempLine) { tempLine.remove(); tempLine = null; }

        const target = document.elementFromPoint(e.clientX, e.clientY);
        if (target && target.classList.contains('connector') && target.dataset.dir === 'in' && root.contains(target)) {
            const toEl = target.closest('.flow-block');
            const toId = toEl ? [...blockEls.entries()].find(([, el]) => el === toEl)?.[0] : null;
            if (toId && toId !== connFromId) {
                const exists = state.connections.some(c => c.from === connFromId && c.to === toId);
                if (!exists) {
                    const connId = `conn_${state.nextConnNum++}`;
                    state.connections.push({ id: connId, from: connFromId, to: toId });
                    state.isDirty = true;
                    renderConnections();
                }
            }
        }
        connFromId = null;
    }

    function renderConnections() {
        $svg.querySelectorAll('path:not(.temp-conn)').forEach(p => p.remove());

        for (const conn of state.connections) {
            const fromEl = blockEls.get(conn.from);
            const toEl = blockEls.get(conn.to);
            if (!fromEl || !toEl) continue;

            const x1 = fromEl.offsetLeft + fromEl.offsetWidth;
            const y1 = fromEl.offsetTop + fromEl.offsetHeight / 2;
            const x2 = toEl.offsetLeft;
            const y2 = toEl.offsetTop + toEl.offsetHeight / 2;

            const cpOffset = Math.abs(x2 - x1) * 0.4 + 40;
            const d = `M ${x1} ${y1} C ${x1 + cpOffset} ${y1}, ${x2 - cpOffset} ${y2}, ${x2} ${y2}`;

            const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
            path.setAttribute('d', d);
            path.setAttribute('stroke', '#64748b');
            path.setAttribute('stroke-width', '2.5');
            path.setAttribute('fill', 'none');
            path.setAttribute('marker-end', 'url(#arrowhead-' + name + ')');
            path.dataset.connId = conn.id;

            if (state.selectedConnectionId === conn.id) path.classList.add('selected');

            path.addEventListener('click', (e) => {
                e.stopPropagation();
                selectConnection(conn.id);
            });

            $svg.appendChild(path);
        }

        const markerId = 'arrowhead-' + name;
        if (!$svg.querySelector('#' + CSS.escape(markerId))) {
            const defs = document.createElementNS('http://www.w3.org/2000/svg', 'defs');
            defs.innerHTML = `
                <marker id="${markerId}" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto">
                    <polygon points="0 0, 10 3.5, 0 7" fill="#64748b" />
                </marker>
            `;
            $svg.insertBefore(defs, $svg.firstChild);
        }
    }

    function selectConnection(id) {
        state.selectedConnectionId = id;
        state.selectedBlockId = null;
        deselectAllBlocks();
        renderConnections();
        updatePropsPanel();
    }

    // =====================================================================
    // SELECTION
    // =====================================================================
    function selectBlock(id) {
        state.selectedBlockId = id;
        state.selectedConnectionId = null;
        deselectAllBlocks();
        const el = blockEls.get(id);
        if (el) el.classList.add('selected');
        renderConnections();
        updatePropsPanel();
    }

    function deselectAll() {
        state.selectedBlockId = null;
        state.selectedConnectionId = null;
        deselectAllBlocks();
        renderConnections();
        updatePropsPanel();
    }

    function deselectAllBlocks() {
        blockEls.forEach(el => el.classList.remove('selected'));
    }

    function deleteSelected() {
        if (state.selectedBlockId) {
            removeBlock(state.selectedBlockId);
        } else if (state.selectedConnectionId) {
            state.connections = state.connections.filter(c => c.id !== state.selectedConnectionId);
            state.selectedConnectionId = null;
            state.isDirty = true;
            renderConnections();
            updatePropsPanel();
        }
    }

    // =====================================================================
    // PROPERTIES PANEL
    // =====================================================================
    function updatePropsPanel() {
        if (state.selectedBlockId) {
            const block = state.blocks.find(b => b.id === state.selectedBlockId);
            if (!block) return;
            $propsEmpty.classList.add('hidden');
            $propsContent.classList.remove('hidden');
            $propId.textContent = block.id;
            $propType.textContent = TYPE_LABELS[block.type];
            $propType.style.background = hexToRgba(block.color, 0.3);
            $propType.style.color = block.color;
            $propName.value = block.name;
            $propColor.value = block.color;
            $propColorHex.textContent = block.color;
            $propX.textContent = block.x;
            $propY.textContent = block.y;
            $propConfig.value = dumpYaml(block.config);
            $propConfigError.classList.add('hidden');
        } else {
            $propsEmpty.classList.remove('hidden');
            $propsContent.classList.add('hidden');
        }
    }

    function updatePropsPosition(block) {
        if (state.selectedBlockId === block.id) {
            $propX.textContent = block.x;
            $propY.textContent = block.y;
        }
    }

    on($propName, 'input', () => {
        const block = state.blocks.find(b => b.id === state.selectedBlockId);
        if (!block) return;
        block.name = $propName.value;
        const el = blockEls.get(block.id);
        if (el) el.querySelector('.block-name').textContent = block.name;
        state.isDirty = true;
    });

    on($propColor, 'input', () => {
        const block = state.blocks.find(b => b.id === state.selectedBlockId);
        if (!block) return;
        block.color = $propColor.value;
        $propColorHex.textContent = block.color;
        const el = blockEls.get(block.id);
        if (el) {
            el.style.background = hexToRgba(block.color, 0.2);
            el.style.borderColor = hexToRgba(block.color, 0.4);
        }
        $propType.style.background = hexToRgba(block.color, 0.3);
        $propType.style.color = block.color;
        state.isDirty = true;
    });

    on($propConfig, 'input', () => {
        const block = state.blocks.find(b => b.id === state.selectedBlockId);
        if (!block) return;
        try {
            block.config = parseYaml($propConfig.value);
            $propConfigError.classList.add('hidden');
            state.isDirty = true;
        } catch (err) {
            $propConfigError.textContent = '⚠ Invalid YAML';
            $propConfigError.classList.remove('hidden');
        }
    });

    on(q('delete-block-btn'), 'click', () => deleteSelected());

    // =====================================================================
    // FILE OPERATIONS
    // =====================================================================
    function flowNewAction() {
        if (state.isDirty) {
            if (!confirm('There are unsaved changes. Continue anyway?')) return;
        }
        flowNew();
    }

    function flowNew() {
        state.blocks = [];
        state.connections = [];
        state.selectedBlockId = null;
        state.selectedConnectionId = null;
        state.isDirty = false;
        state.flowName = 'Untitled Flow';
        state.nextBlockNum = 1;
        state.nextConnNum = 1;
        state.zoom = 1;
        state.panX = 0;
        state.panY = 0;
        blockEls.forEach(el => el.remove());
        blockEls.clear();
        renderConnections();
        updateCanvasTransform();
        updatePropsPanel();
        $flowName.textContent = state.flowName;
    }

    function serialize() {
        return {
            name: state.flowName,
            version: '1.0',
            blocks: state.blocks.map(b => ({
                id: b.id, type: b.type, name: b.name, icon: b.icon,
                x: b.x, y: b.y, config: b.config, color: b.color
            })),
            connections: state.connections.map(c => ({ id: c.id, from: c.from, to: c.to }))
        };
    }

    async function flowSave() {
        const data = serialize();
        const json = JSON.stringify(data, null, 2);
        if (dotNetRef) {
            await dotNetRef.invokeMethodAsync('NotifySaveRequested', json);
        }
        state.isDirty = false;
    }

    function flowOpen() {
        if (state.isDirty) {
            if (!confirm('There are unsaved changes. Continue anyway?')) return;
        }
        $fileInput.click();
    }

    on($fileInput, 'change', (e) => {
        const file = e.target.files[0];
        if (!file) return;
        const reader = new FileReader();
        reader.onload = () => {
            try {
                const data = JSON.parse(reader.result);
                loadFlow(data);
            } catch (err) {
                alert('Error loading file: ' + err.message);
            }
        };
        reader.readAsText(file);
        $fileInput.value = '';
    });

    function loadFlow(data) {
        flowNew();

        state.flowName = data.name || 'Untitled Flow';
        $flowName.textContent = state.flowName;

        let maxBlockNum = 0, maxConnNum = 0;

        if (data.blocks) {
            for (const b of data.blocks) {
                state.blocks.push({ ...b });
                renderBlock(b);
                const num = parseInt((b.id || '').replace('block_', ''), 10);
                if (num > maxBlockNum) maxBlockNum = num;
            }
        }
        if (data.connections) {
            for (const c of data.connections) {
                state.connections.push({ ...c });
                const num = parseInt((c.id || '').replace('conn_', ''), 10);
                if (num > maxConnNum) maxConnNum = num;
            }
        }

        state.nextBlockNum = maxBlockNum + 1;
        state.nextConnNum = maxConnNum + 1;
        state.isDirty = false;

        renderConnections();
        updatePropsPanel();
    }

    // =====================================================================
    // TOOLBAR ACTIONS
    // =====================================================================
    root.querySelectorAll('[data-action]').forEach(btn => {
        on(btn, 'click', () => {
            switch (btn.dataset.action) {
                case 'new': flowNewAction(); break;
                case 'open': flowOpen(); break;
                case 'save': flowSave(); break;
                case 'zoom-in': zoomIn(); break;
                case 'zoom-out': zoomOut(); break;
                case 'zoom-reset': zoomReset(); break;
            }
        });
    });

    // =====================================================================
    // KEYBOARD SHORTCUTS (only when this instance's canvas is active)
    // =====================================================================
    function keydownHandler(e) {
        const withinInstance = pointerInside || root.contains(document.activeElement);
        if (e.key === 'Delete' || e.key === 'Backspace') {
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;
            if (!withinInstance) return;
            deleteSelected();
        }
        if (!withinInstance) return;
        if (e.ctrlKey && e.key === 'n') { e.preventDefault(); flowNewAction(); }
        if (e.ctrlKey && e.key === 'o') { e.preventDefault(); flowOpen(); }
        if (e.ctrlKey && e.key === 's') { e.preventDefault(); flowSave(); }
        if (e.ctrlKey && e.key === '0') { e.preventDefault(); zoomReset(); }
        if (e.key === 'Escape') deselectAll();
    }
    on(document, 'keydown', keydownHandler);

    function beforeUnloadHandler(e) {
        if (state.isDirty) {
            e.preventDefault();
            e.returnValue = '';
        }
    }
    on(window, 'beforeunload', beforeUnloadHandler);

    function teardown() {
        cleanups.forEach(fn => fn());
        blockEls.forEach(el => el.remove());
        blockEls.clear();
    }

    return { initPalette, updateCanvasTransform, loadFlow, serialize, teardown };
}

// =====================================================================
// UTILITIES
// =====================================================================
function snapToGrid(val) {
    return Math.round(val / GRID_SIZE) * GRID_SIZE;
}

function hexToRgba(hex, alpha) {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    return `rgba(${r},${g},${b},${alpha})`;
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

function dumpYaml(value) {
    if (!window.jsyaml) return JSON.stringify(value, null, 2);
    return window.jsyaml.dump(value ?? {});
}

function parseYaml(text) {
    if (!window.jsyaml) return JSON.parse(text);
    return window.jsyaml.load(text) ?? {};
}
