/**
 * filter-handler.js
 * ─────────────────────────────────────────────────────────────────────────
 * Accumulative sidebar filter for HDKTech product pages.
 *
 * Responsibilities:
 *   1. Maintain a live state object from current URL params on page load.
 *   2. Let the user toggle brands (checkboxes), spec chips (CPU/VGA/RAM),
 *      price presets, and custom price inputs without losing other filters.
 *   3. On "Áp dụng": serialise state → /Product/Filter query string → navigate.
 *   4. On "Xóa tất cả" / individual badge ×: clear specific or all params.
 *   5. Update active badges strip in real-time as user interacts.
 *
 * Brand handling:
 *   The server returns brand *names* in AvailableBrands.
 *   We track brands by name and join them as "brandIds" = name-joined with
 *   commas. The controller+repo resolve them by checking Brand.Name.
 *   (If you later store numeric IDs in data-id attributes, swap trivially.)
 * ─────────────────────────────────────────────────────────────────────────
 */

(function () {
  'use strict';

  // ── Helpers ──────────────────────────────────────────────────────────────

  /** Read a query-string param from the current URL */
  function getParam(name) {
    return new URLSearchParams(window.location.search).get(name) || '';
  }

  /** Build a URL for /Product/Filter from a state object */
  function buildUrl(state) {
    const base   = '/Product/Filter';
    const params = new URLSearchParams();

    if (state.categoryId) params.set('categoryId', state.categoryId);
    if (state.keyword)    params.set('keyword',    state.keyword);
    if (state.sortBy && state.sortBy !== 'featured') params.set('sortBy', state.sortBy);

    // brands → comma list
    if (state.brands && state.brands.length)
      params.set('brandIds', state.brands.join(','));

    if (state.minPrice) params.set('minPrice', state.minPrice);
    if (state.maxPrice) params.set('maxPrice', state.maxPrice);
    if (state.cpuLine)  params.set('cpuLine',  state.cpuLine);
    if (state.vgaLine)  params.set('vgaLine',  state.vgaLine);
    if (state.ramType)  params.set('ramType',  state.ramType);
    if (state.status !== null && state.status !== '') params.set('status', state.status);

    const qs = params.toString();
    return qs ? `${base}?${qs}` : base;
  }

  /** Humanise a decimal price for badges */
  function formatPrice(val) {
    if (!val) return '';
    const n = parseInt(val, 10);
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(n % 1_000_000 === 0 ? 0 : 1) + 'tr';
    if (n >= 1_000)     return (n / 1_000).toFixed(0) + 'k';
    return n.toString();
  }

  // ── State initialisation ─────────────────────────────────────────────────

  const $categoryId = document.getElementById('fsbCategoryId');
  const $keyword    = document.getElementById('fsbKeyword');
  const $initBrands = document.getElementById('fsbInitBrands');

  // Initial state from current URL
  const state = {
    categoryId : $categoryId ? $categoryId.value : getParam('categoryId'),
    keyword    : $keyword    ? $keyword.value    : getParam('keyword'),
    sortBy     : getParam('sortBy')   || 'featured',
    brands     : [],   // will be populated below
    minPrice   : getParam('minPrice') || '',
    maxPrice   : getParam('maxPrice') || '',
    cpuLine    : getParam('cpuLine')  || '',
    vgaLine    : getParam('vgaLine')  || '',
    ramType    : getParam('ramType')  || '',
    status     : getParam('status')   || '',
  };

  // Re-hydrate selected brands from init value (comma list of names)
  const initBrandsRaw = $initBrands ? $initBrands.value : getParam('brandIds');
  if (initBrandsRaw) {
    state.brands = initBrandsRaw.split(',').map(s => s.trim()).filter(Boolean);
  }

  // ── DOM refs ─────────────────────────────────────────────────────────────

  const sidebar      = document.getElementById('filterSidebar');
  if (!sidebar) return;  // not on a filter page

  const applyBtn     = document.getElementById('fsbApplyBtn');
  const clearBtn     = document.getElementById('fsbClearBtn');
  const resetAllBtn  = document.getElementById('fsbResetAll');
  const badgesWrap   = document.getElementById('fsbActiveBadges');
  const sortSelect   = document.getElementById('fsbSort');
  const minPriceIn   = document.getElementById('fsbMinPrice');
  const maxPriceIn   = document.getElementById('fsbMaxPrice');
  const brandCBs     = sidebar.querySelectorAll('.fsb-brand-cb');
  const specChips    = sidebar.querySelectorAll('.fsb-spec-chip');
  const pricePresets = sidebar.querySelectorAll('.fsb__price-preset');

  // ── Sync initial UI state ────────────────────────────────────────────────

  function syncUI() {
    // Sort
    if (sortSelect) sortSelect.value = state.sortBy || 'featured';

    // Price
    if (minPriceIn) minPriceIn.value = state.minPrice;
    if (maxPriceIn) maxPriceIn.value = state.maxPrice;

    // Brand checkboxes
    brandCBs.forEach(cb => {
      cb.checked = state.brands.includes(cb.value);
    });

    // Spec chips
    specChips.forEach(chip => {
      const param = chip.dataset.param;
      const val   = chip.dataset.value;
      const active = state[param] === val;
      chip.classList.toggle('active', active);
    });

    renderBadges();
  }

  // ── Badge rendering ──────────────────────────────────────────────────────

  function renderBadges() {
    if (!badgesWrap) return;
    const tags = [];

    if (state.brands.length) {
      state.brands.forEach(b => {
        tags.push({ label: b, action: () => { state.brands = state.brands.filter(x => x !== b); syncUI(); } });
      });
    }
    if (state.minPrice || state.maxPrice) {
      const label = `${state.minPrice ? formatPrice(state.minPrice) : '0'} – ${state.maxPrice ? formatPrice(state.maxPrice) : '∞'}`;
      tags.push({ label: '💰 ' + label, action: () => { state.minPrice = ''; state.maxPrice = ''; if (minPriceIn) minPriceIn.value = ''; if (maxPriceIn) maxPriceIn.value = ''; syncUI(); } });
    }
    if (state.cpuLine) {
      tags.push({ label: 'CPU: ' + state.cpuLine, action: () => { state.cpuLine = ''; syncUI(); } });
    }
    if (state.vgaLine) {
      tags.push({ label: 'VGA: ' + state.vgaLine, action: () => { state.vgaLine = ''; syncUI(); } });
    }
    if (state.ramType) {
      tags.push({ label: 'RAM: ' + state.ramType, action: () => { state.ramType = ''; syncUI(); } });
    }
    if (state.status !== '') {
      const lbl = state.status === '1' ? 'Còn hàng' : 'Hết hàng';
      tags.push({ label: lbl, action: () => { state.status = ''; syncUI(); } });
    }

    if (!tags.length) {
      badgesWrap.style.display = 'none';
      badgesWrap.innerHTML = '';
      return;
    }

    badgesWrap.style.display = 'flex';
    badgesWrap.innerHTML = tags.map((t, i) =>
      `<span class="fsb__active-tag">
        ${escHtml(t.label)}
        <button type="button" data-badge-idx="${i}" title="Xóa">×</button>
      </span>`
    ).join('');

    // Wire remove buttons
    badgesWrap.querySelectorAll('[data-badge-idx]').forEach(btn => {
      btn.addEventListener('click', () => {
        tags[parseInt(btn.dataset.badgeIdx, 10)].action();
      });
    });
  }

  function escHtml(str) {
    return str.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
  }

  // ── Event: Sort ──────────────────────────────────────────────────────────

  sortSelect && sortSelect.addEventListener('change', () => {
    state.sortBy = sortSelect.value;
    // Sort triggers immediate navigation (no need to click Apply)
    navigate();
  });

  // ── Event: Brand checkboxes ──────────────────────────────────────────────

  brandCBs.forEach(cb => {
    cb.addEventListener('change', () => {
      if (cb.checked) {
        if (!state.brands.includes(cb.value)) state.brands.push(cb.value);
      } else {
        state.brands = state.brands.filter(b => b !== cb.value);
      }
      renderBadges();
    });
  });

  // ── Event: Spec chips (CPU / VGA / RAM / Status) ─────────────────────────

  specChips.forEach(chip => {
    chip.addEventListener('click', () => {
      const param = chip.dataset.param;
      const val   = chip.dataset.value;
      // Toggle: click active chip → deselect
      if (state[param] === val) {
        state[param] = '';
        chip.classList.remove('active');
      } else {
        // Deactivate siblings in same group
        sidebar.querySelectorAll(`.fsb-spec-chip[data-param="${param}"]`)
               .forEach(c => c.classList.remove('active'));
        state[param] = val;
        chip.classList.add('active');
      }
      renderBadges();
    });
  });

  // ── Event: Price presets ─────────────────────────────────────────────────

  pricePresets.forEach(btn => {
    btn.addEventListener('click', () => {
      state.minPrice = btn.dataset.min || '';
      state.maxPrice = btn.dataset.max || '';
      if (minPriceIn) minPriceIn.value = state.minPrice;
      if (maxPriceIn) maxPriceIn.value = state.maxPrice;
      renderBadges();
    });
  });

  // ── Event: Manual price inputs (sync on blur/enter) ──────────────────────

  function syncPriceFromInputs() {
    state.minPrice = (minPriceIn && minPriceIn.value.trim()) ? minPriceIn.value.trim() : '';
    state.maxPrice = (maxPriceIn && maxPriceIn.value.trim()) ? maxPriceIn.value.trim() : '';
    renderBadges();
  }

  minPriceIn && minPriceIn.addEventListener('blur',   syncPriceFromInputs);
  maxPriceIn && maxPriceIn.addEventListener('blur',   syncPriceFromInputs);
  minPriceIn && minPriceIn.addEventListener('keydown', e => { if (e.key === 'Enter') syncPriceFromInputs(); });
  maxPriceIn && maxPriceIn.addEventListener('keydown', e => { if (e.key === 'Enter') syncPriceFromInputs(); });

  // ── Event: Apply button ──────────────────────────────────────────────────

  applyBtn && applyBtn.addEventListener('click', navigate);

  // ── Event: Clear (sidebar footer) ────────────────────────────────────────

  clearBtn && clearBtn.addEventListener('click', clearFilters);

  // ── Event: Reset all (header) ────────────────────────────────────────────

  resetAllBtn && resetAllBtn.addEventListener('click', clearFilters);

  // ── Navigation ───────────────────────────────────────────────────────────

  function navigate() {
    // Grab latest price inputs right before navigating
    syncPriceFromInputs();
    window.location.href = buildUrl(state);
  }

  function clearFilters() {
    state.brands   = [];
    state.minPrice = '';
    state.maxPrice = '';
    state.cpuLine  = '';
    state.vgaLine  = '';
    state.ramType  = '';
    state.status   = '';
    state.sortBy   = 'featured';
    // Keep category + keyword context
    syncUI();
    navigate();
  }

  // ── Initial sync ─────────────────────────────────────────────────────────

  syncUI();

})();
