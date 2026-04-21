/**
 * Home Page - JavaScript Logic
 * Handles carousel auto-play and page initialization
 */

document.addEventListener('DOMContentLoaded', function() {
    // Hero banner carousel
    var heroBannerEl = document.getElementById('categoryBannerCarousel');
    if (heroBannerEl) {
        var heroBannerCarousel = new bootstrap.Carousel(heroBannerEl, {
            interval: 4000,
            pause: 'hover',
            ride: 'carousel'
        });
        heroBannerCarousel.cycle();
    }

    // Flash sale carousel — tự lướt mỗi 5s qua 6 sản phẩm tiếp theo
    var flashEl = document.getElementById('flashSaleCarousel');
    if (flashEl) {
        var flashCarousel = new bootstrap.Carousel(flashEl, {
            interval: 5000,
            pause: 'hover',
            ride: 'carousel',
            wrap: true
        });
        flashCarousel.cycle();
    }
});

// ── Banner click tracking ────────────────────────────────────────────
window.trackBannerClick = function (bannerId) {
    if (!bannerId) return;
    fetch('/api/banneranalytics/track-click/' + bannerId, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
    }).catch(function () {});
};

// ── Category Strip — auto-scroll left/right ──────────────────────────
(function initCategoryStrip() {
    var inner = document.getElementById('catStripInner');
    var btnPrev = document.getElementById('catStripPrev');
    var btnNext = document.getElementById('catStripNext');
    if (!inner) return;

    var scrollAmount = 220; // px per step
    var autoInterval = null;
    var direction    = 1;   // 1 = right, -1 = left

    function scrollStep(dir) {
        var maxScroll = inner.scrollWidth - inner.clientWidth;
        var next = inner.scrollLeft + dir * scrollAmount;

        if (next >= maxScroll) { direction = -1; }
        if (next <= 0)         { direction =  1; }

        inner.scrollBy({ left: dir * scrollAmount, behavior: 'smooth' });
    }

    function startAuto() {
        autoInterval = setInterval(function() { scrollStep(direction); }, 2500);
    }
    function stopAuto() {
        clearInterval(autoInterval);
    }

    // Pause on hover
    inner.addEventListener('mouseenter', stopAuto);
    inner.addEventListener('mouseleave', startAuto);

    // Manual buttons
    if (btnPrev) {
        btnPrev.addEventListener('click', function() {
            stopAuto(); scrollStep(-1); startAuto();
        });
    }
    if (btnNext) {
        btnNext.addEventListener('click', function() {
            stopAuto(); scrollStep(1); startAuto();
        });
    }

    startAuto();
})();

// ── Flash Sale Countdown + Progress Bar + Card Countdowns ────────────
(function initFlashSale() {
    var cdH = document.getElementById('cd-h');
    var cdM = document.getElementById('cd-m');
    var cdS = document.getElementById('cd-s');
    if (!cdH) return;

    // Đọc thời gian từ data-end / data-start trên .flash-sale-section
    var sectionEl  = document.querySelector('.flash-sale-section');
    var endTime    = sectionEl && sectionEl.dataset.end   ? new Date(sectionEl.dataset.end)   : null;
    var startTime  = sectionEl && sectionEl.dataset.start ? new Date(sectionEl.dataset.start) : null;

    // Fallback
    if (!endTime || isNaN(endTime)) {
        endTime = new Date(); endTime.setHours(23, 59, 59, 999);
    }
    if (!startTime || isNaN(startTime)) {
        startTime = new Date(); startTime.setHours(0, 0, 0, 0);
    }

    var totalDuration = Math.max(1, endTime - startTime);
    var progressBar   = document.getElementById('flashProgressBar');
    var progressText  = document.getElementById('flashProgressText');

    function pad(n) { return n < 10 ? '0' + n : '' + n; }

    function formatRemaining(diff) {
        var h = Math.floor(diff / 3600);
        var m = Math.floor((diff % 3600) / 60);
        var s = diff % 60;
        if (h > 0) return 'Còn ' + h + 'g ' + pad(m) + 'p';
        if (m > 0) return 'Còn ' + pad(m) + ' phút ' + pad(s) + 'g';
        return 'Còn ' + pad(s) + ' giây';
    }

    function tick() {
        var now  = new Date();
        var diff = Math.max(0, Math.floor((endTime - now) / 1000));

        // Đồng hồ header
        cdH.textContent = pad(Math.floor(diff / 3600));
        cdM.textContent = pad(Math.floor((diff % 3600) / 60));
        cdS.textContent = pad(diff % 60);

        // Progress bar — đầy khi mới bắt đầu, cạn khi sắp hết
        if (progressBar) {
            var pct = ((endTime - now) / totalDuration) * 100;
            progressBar.style.width = Math.min(100, Math.max(0, pct)).toFixed(2) + '%';
        }

        // Nhãn thời gian còn lại
        if (progressText) {
            progressText.textContent = diff > 0 ? formatRemaining(diff) : 'Đã kết thúc';
        }

        if (diff === 0) { setTimeout(function () { location.reload(); }, 2000); return; }
    }

    tick();
    setInterval(tick, 1000);

    // ── Countdown mini trên từng card flash sale ──────────────────────
    document.querySelectorAll('.pc__countdown[data-end]').forEach(function (el) {
        var cardEnd = new Date(el.dataset.end);
        if (isNaN(cardEnd)) return;
        var hEl = el.querySelector('[data-unit="h"]');
        var mEl = el.querySelector('[data-unit="m"]');
        var sEl = el.querySelector('[data-unit="s"]');
        if (!hEl || !mEl || !sEl) return;
        function tickCard() {
            var d = Math.max(0, Math.floor((cardEnd - new Date()) / 1000));
            hEl.textContent = pad(Math.floor(d / 3600));
            mEl.textContent = pad(Math.floor((d % 3600) / 60));
            sEl.textContent = pad(d % 60);
        }
        tickCard();
        setInterval(tickCard, 1000);
    });
})();

// ── Tab Filter — client-side (no page reload) ────────────────────────
window.filterProducts = function (gridKey, filterValue, btn) {
    var tabContainer = btn.closest('.section-tabs');
    if (tabContainer) {
        tabContainer.querySelectorAll('.section-tab').forEach(function (t) {
            t.classList.remove('active');
        });
    }
    btn.classList.add('active');

    var key  = gridKey.charAt(0).toUpperCase() + gridKey.slice(1);
    var grid = document.getElementById('grid' + key);
    if (!grid) return;

    grid.querySelectorAll('[data-brand], [data-category]').forEach(function (card) {
        if (filterValue === 'all') {
            card.style.display = '';
        } else {
            var brandMatch    = card.dataset.brand    && card.dataset.brand.toLowerCase()    === filterValue.toLowerCase();
            var categoryMatch = card.dataset.category && card.dataset.category.toLowerCase() === filterValue.toLowerCase();
            card.style.display = (brandMatch || categoryMatch) ? '' : 'none';
        }
    });
};
