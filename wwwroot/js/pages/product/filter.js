/* ── Product Filter — Client-side State + Navigation ──────────────── */
(function () {
    var el = document.getElementById('filter-state');
    if (!el) return;

    var state = {
        categoryId : el.dataset.categoryId  || '',
        keyword    : el.dataset.keyword     || '',
        brandNames : JSON.parse(el.dataset.brandNames || '[]'),
        minPrice   : el.dataset.minPrice    || '',
        maxPrice   : el.dataset.maxPrice    || '',
        sortBy     : el.dataset.sortBy      || 'featured',
        cpuLine    : el.dataset.cpuLine     || '',
        vgaLine    : el.dataset.vgaLine     || '',
        ramType    : el.dataset.ramType     || '',
    };
    var pendingBrands = state.brandNames.slice();

    function navigate(overrides) {
        var s = Object.assign({}, state, overrides);
        var p = new URLSearchParams();
        if (s.categoryId)                       p.set('categoryId', s.categoryId);
        if (s.keyword)                          p.set('keyword',    s.keyword);
        if (s.brandNames && s.brandNames.length) p.set('brandNames', s.brandNames.join(','));
        if (s.minPrice)                         p.set('minPrice',   s.minPrice);
        if (s.maxPrice)                         p.set('maxPrice',   s.maxPrice);
        if (s.sortBy && s.sortBy !== 'featured') p.set('sortBy',    s.sortBy);
        if (s.cpuLine)                          p.set('cpuLine',    s.cpuLine);
        if (s.vgaLine)                          p.set('vgaLine',    s.vgaLine);
        if (s.ramType)                          p.set('ramType',    s.ramType);
        var qs = p.toString();
        window.location.href = '/Product/Filter' + (qs ? '?' + qs : '');
    }

    window.toggleBrand = function (cb) {
        if (cb.checked) {
            if (!pendingBrands.includes(cb.value)) pendingBrands.push(cb.value);
            cb.closest('label').classList.add('selected');
        } else {
            pendingBrands = pendingBrands.filter(function (b) { return b !== cb.value; });
            cb.closest('label').classList.remove('selected');
        }
    };
    window.applyBrands       = function ()          { navigate({ brandNames: pendingBrands }); };
    window.clearBrands       = function ()          {
        pendingBrands = [];
        document.querySelectorAll('.hdk-brand-cb').forEach(function (c) {
            c.checked = false;
            c.closest('label').classList.remove('selected');
        });
    };
    window.removeBrand       = function (name)      { navigate({ brandNames: state.brandNames.filter(function (b) { return b !== name; }) }); };
    window.applyPrice        = function (min, max)  { navigate({ minPrice: min ? String(min) : '', maxPrice: max ? String(max) : '' }); };
    window.applyCustomPrice  = function ()          {
        navigate({
            minPrice: (document.getElementById('priceMin')  || {}).value || '',
            maxPrice: (document.getElementById('priceMax')  || {}).value || '',
        });
    };
    window.clearPrice        = function ()          { navigate({ minPrice: '', maxPrice: '' }); };
    window.removePrice       = function ()          { navigate({ minPrice: '', maxPrice: '' }); };
    window.toggleSpec        = function (param, val) {
        var o = {};
        o[param] = state[param] === val ? '' : val;
        navigate(o);
    };
    window.removeSpec        = function (param)     { var o = {}; o[param] = ''; navigate(o); };
    window.applySort         = function (v)         { navigate({ sortBy: v }); };
    window.clearAllFilters   = function ()          { navigate({ brandNames: [], minPrice: '', maxPrice: '', cpuLine: '', vgaLine: '', ramType: '', sortBy: 'featured' }); };
    window.filterBrandSearch = function (q)         {
        var rows = document.querySelectorAll('#brandCheckboxList .hdk-brand-row');
        var lq   = q.toLowerCase();
        rows.forEach(function (r) { r.style.display = r.textContent.toLowerCase().includes(lq) ? '' : 'none'; });
    };
})();
