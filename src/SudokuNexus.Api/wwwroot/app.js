const state = {
    presets: [],
    currentPuzzleString: '',
    appliedEliminations: [],
    currentBoardDto: null,
    pendingHintStep: null
};

const els = {
    presetSelect: document.getElementById('presetSelect'),
    btnLoadPreset: document.getElementById('btnLoadPreset'),
    puzzleInput: document.getElementById('puzzleInput'),
    btnInspect: document.getElementById('btnInspect'),
    sudokuGrid: document.getElementById('sudokuGrid'),
    btnHint: document.getElementById('btnHint'),
    btnApplyHint: document.getElementById('btnApplyHint'),
    btnSolveDlx: document.getElementById('btnSolveDlx'),
    btnAnalyze: document.getElementById('btnAnalyze'),
    genDifficulty: document.getElementById('genDifficulty'),
    btnGenerate: document.getElementById('btnGenerate'),
    statusBanner: document.getElementById('statusBanner'),
    statusTitle: document.getElementById('statusTitle'),
    statusMessage: document.getElementById('statusMessage'),
    metricDlxTime: document.getElementById('metricDlxTime'),
    metricUnique: document.getElementById('metricUnique'),
    metricDifficulty: document.getElementById('metricDifficulty'),
    metricTechnique: document.getElementById('metricTechnique'),
    stepCounter: document.getElementById('stepCounter'),
    traceList: document.getElementById('traceList')
};

function setStatus(type, title, message) {
    els.statusBanner.className = `status-card ${type}`;
    els.statusTitle.textContent = title;
    els.statusMessage.textContent = message;
}

async function apiCall(url, options = {}) {
    const response = await fetch(url, {
        headers: { 'Content-Type': 'application/json' },
        ...options
    });

    const data = await response.json().catch(() => null);
    if (!response.ok) {
        if (response.status === 501 && data && data.detail) {
            setStatus('todo-warning', 'Learning Module Stub Reached (501 Not Implemented)', data.detail);
        } else {
            const msg = (data && (data.detail || data.title)) || `HTTP ${response.status}`;
            setStatus('error', 'API Error', msg);
        }
        throw new Error('Handled API error');
    }
    return data;
}

function renderEmptyGrid(rawString = '') {
    els.sudokuGrid.innerHTML = '';
    const padded = rawString.padEnd(81, '.');

    for (let i = 0; i < 81; i++) {
        const r = Math.floor(i / 9);
        const c = i % 9;
        const ch = padded[i];
        const val = ch >= '1' && ch <= '9' ? parseInt(ch, 10) : 0;

        const cell = document.createElement('div');
        cell.className = 'cell' +
            (c === 2 || c === 5 ? ' box-right' : '') +
            (r === 2 || r === 5 ? ' box-bottom' : '') +
            (val !== 0 ? ' given' : '');

        const coord = document.createElement('span');
        coord.className = 'cell-coord';
        coord.textContent = `R${r + 1}C${c + 1}`;
        cell.appendChild(coord);

        if (val !== 0) {
            const span = document.createElement('span');
            span.className = 'cell-value';
            span.textContent = val;
            cell.appendChild(span);
        }

        els.sudokuGrid.appendChild(cell);
    }
}

function renderBoardState(boardDto, highlightStep = null) {
    state.currentBoardDto = boardDto;
    els.sudokuGrid.innerHTML = '';

    const primarySet = new Set(highlightStep ? highlightStep.primaryCells || [] : []);
    const placedCell = highlightStep ? highlightStep.placedCellIndex : null;
    const placedDigit = highlightStep ? highlightStep.placedDigit : null;
    const elimMap = new Map();

    if (highlightStep && highlightStep.eliminations) {
        for (const e of highlightStep.eliminations) {
            if (!elimMap.has(e.cellIndex)) elimMap.set(e.cellIndex, new Set());
            elimMap.get(e.cellIndex).add(e.digit);
        }
    }

    for (const cellDto of boardDto.cells) {
        const i = cellDto.index;
        const r = cellDto.row;
        const c = cellDto.column;

        const cell = document.createElement('div');
        cell.className = 'cell' +
            (c === 2 || c === 5 ? ' box-right' : '') +
            (r === 2 || r === 5 ? ' box-bottom' : '') +
            (cellDto.isGiven ? ' given' : '') +
            (primarySet.has(i) ? ' highlight-primary' : '') +
            (placedCell === i ? ' highlight-placed' : '');

        const coord = document.createElement('span');
        coord.className = 'cell-coord';
        coord.textContent = `R${r + 1}C${c + 1}`;
        cell.appendChild(coord);

        if (cellDto.value !== 0) {
            const span = document.createElement('span');
            span.className = 'cell-value';
            span.textContent = cellDto.value;
            cell.appendChild(span);
        } else {
            const candGrid = document.createElement('div');
            candGrid.className = 'candidates-grid';
            const activeSet = new Set(cellDto.candidates || []);
            const cellElims = elimMap.get(i) || new Set();

            for (let d = 1; d <= 9; d++) {
                const candSpan = document.createElement('span');
                candSpan.className = 'cand';
                if (activeSet.has(d)) {
                    candSpan.textContent = d;
                    if (cellElims.has(d)) {
                        candSpan.classList.add('eliminated');
                    } else if (placedCell === i && placedDigit === d) {
                        candSpan.classList.add('target-digit');
                    }
                }
                candGrid.appendChild(candSpan);
            }
            cell.appendChild(candGrid);
        }

        els.sudokuGrid.appendChild(cell);
    }
}

async function inspectBoard() {
    const raw = els.puzzleInput.value.trim();
    state.currentPuzzleString = raw;
    state.pendingHintStep = null;
    els.btnApplyHint.disabled = true;

    try {
        const dto = await apiCall('/api/puzzles/inspect', {
            method: 'POST',
            body: JSON.stringify({
                puzzle: raw,
                appliedEliminations: state.appliedEliminations
            })
        });
        renderBoardState(dto);
        setStatus(
            dto.isValid ? 'success' : 'error',
            dto.isSolved ? 'Puzzle Solved!' : 'Board Inspected (Module 1 Verified)',
            `Filled Cells: ${dto.filledCellCount}/81 | Valid State: ${dto.isValid}`
        );
    } catch {
        // Fallback so grid still displays raw clues before Module 1.2 is implemented
        renderEmptyGrid(raw);
    }
}

async function requestNextHint() {
    const raw = els.puzzleInput.value.trim();
    try {
        const res = await apiCall('/api/puzzles/hint', {
            method: 'POST',
            body: JSON.stringify({
                puzzle: raw,
                appliedEliminations: state.appliedEliminations
            })
        });

        if (!res.hasHint || !res.step) {
            renderBoardState(res.currentBoard);
            setStatus('info', 'No Further Human Hint Available',
                res.currentBoard.isSolved
                    ? 'The board is already completely solved!'
                    : 'No registered strategy found a step. Implement more strategies in Module 3!');
            els.btnApplyHint.disabled = true;
            return;
        }

        state.pendingHintStep = res.step;
        renderBoardState(res.currentBoard, res.step);
        els.btnApplyHint.disabled = false;
        setStatus('success', `${res.step.technique} (SE Weight: ${res.step.difficultyWeight.toFixed(1)})`, res.step.description);
    } catch {
        // Error banner already updated
    }
}

async function applyPendingHint() {
    if (!state.pendingHintStep) return;
    const step = state.pendingHintStep;

    if (step.placedCellIndex !== null && step.placedDigit !== null) {
        const chars = els.puzzleInput.value.trim().split('');
        chars[step.placedCellIndex] = String(step.placedDigit);
        els.puzzleInput.value = chars.join('');
    }

    if (step.eliminations && step.eliminations.length > 0) {
        state.appliedEliminations.push(...step.eliminations);
    }

    state.pendingHintStep = null;
    els.btnApplyHint.disabled = true;
    await inspectBoard();
}

async function solveFastDlx() {
    const raw = els.puzzleInput.value.trim();
    try {
        const res = await apiCall('/api/puzzles/solve', {
            method: 'POST',
            body: JSON.stringify({ puzzle: raw })
        });

        els.metricDlxTime.textContent = `${res.elapsedMicroseconds} µs`;
        els.metricUnique.textContent = res.hasUniqueSolution ? 'Unique (1)' : `Multiple/None (${res.solutionCount})`;

        if (res.isSolved && res.solvedBoard) {
            els.puzzleInput.value = res.solvedBoardString;
            state.appliedEliminations = [];
            renderBoardState(res.solvedBoard);
            setStatus('success', 'Solved via Algorithm X / Dancing Links (Module 2 Verified)',
                `Solved in ${res.elapsedMicroseconds} µs. Unique solution: ${res.hasUniqueSolution}.`);
        } else {
            setStatus('error', 'Unsolvable Board', 'Algorithm X determined this puzzle has 0 valid solutions.');
        }
    } catch {
        // Error banner already updated
    }
}

async function analyzeFull() {
    const raw = els.puzzleInput.value.trim();
    try {
        const res = await apiCall('/api/puzzles/analyze', {
            method: 'POST',
            body: JSON.stringify({ puzzle: raw })
        });

        els.metricDlxTime.textContent = `${res.dlxResult.elapsedMicroseconds} µs`;
        els.metricUnique.textContent = res.dlxResult.hasUniqueSolution ? 'Unique (1)' : 'Non-Unique';
        els.metricDifficulty.textContent = res.grading.rating;
        els.metricTechnique.textContent = `${res.grading.maxTechniqueUsed} (${res.grading.maxStepWeight.toFixed(1)})`;

        renderBoardState(res.initialBoard);
        renderTraceSteps(res.grading.steps, res.initialBoard);

        setStatus('success', `Full Analysis Complete — Rated ${res.grading.rating}`,
            `Total Logical Steps: ${res.grading.steps.length} | Total SE Weight: ${res.grading.totalWeightScore} | Solved by Logic: ${res.grading.solvedEntirelyByLogic}`);
    } catch {
        // Error banner already updated
    }
}

function renderTraceSteps(steps, initialBoardDto) {
    els.stepCounter.textContent = `${steps.length} steps`;
    els.traceList.innerHTML = '';

    if (!steps || steps.length === 0) {
        els.traceList.innerHTML = '<p class="muted-placeholder">No logical steps recorded.</p>';
        return;
    }

    steps.forEach((step, idx) => {
        const div = document.createElement('div');
        div.className = 'trace-item';
        div.innerHTML = `
            <div class="trace-item-top">
                <span>#${idx + 1} — ${step.technique}</span>
                <span>SE ${step.difficultyWeight.toFixed(1)}</span>
            </div>
            <div class="trace-item-desc">${step.description}</div>
        `;
        div.addEventListener('click', () => {
            renderBoardState(initialBoardDto, step);
            setStatus('info', `Step #${idx + 1}: ${step.technique}`, step.description);
        });
        els.traceList.appendChild(div);
    });
}

async function generateNewPuzzle() {
    const difficulty = els.genDifficulty.value;
    try {
        const res = await apiCall(`/api/puzzles/generate?difficulty=${encodeURIComponent(difficulty)}`);
        els.puzzleInput.value = res.puzzleString;
        state.appliedEliminations = [];
        await inspectBoard();
        setStatus('success', `Generated ${res.difficulty} Puzzle (Module 4 Verified)`,
            `Clues: ${res.givenClueCount}/81 | Max Technique: ${res.maxTechnique}`);
    } catch {
        // Error banner already updated
    }
}

async function init() {
    try {
        state.presets = await apiCall('/api/puzzles/presets');
        els.presetSelect.innerHTML = '';
        for (const p of state.presets) {
            const opt = document.createElement('option');
            opt.value = p.id;
            opt.textContent = `[${p.expectedDifficulty}] ${p.name} — ${p.subtitle}`;
            els.presetSelect.appendChild(opt);
        }

        if (state.presets.length > 0) {
            els.puzzleInput.value = state.presets[0].puzzleString;
            renderEmptyGrid(state.presets[0].puzzleString);
        }
    } catch {
        renderEmptyGrid();
    }

    els.btnLoadPreset.addEventListener('click', () => {
        const chosen = state.presets.find(p => p.id === els.presetSelect.value);
        if (chosen) {
            els.puzzleInput.value = chosen.puzzleString;
            state.appliedEliminations = [];
            inspectBoard();
        }
    });

    els.btnInspect.addEventListener('click', inspectBoard);
    els.btnHint.addEventListener('click', requestNextHint);
    els.btnApplyHint.addEventListener('click', applyPendingHint);
    els.btnSolveDlx.addEventListener('click', solveFastDlx);
    els.btnAnalyze.addEventListener('click', analyzeFull);
    els.btnGenerate.addEventListener('click', generateNewPuzzle);
}

init();
