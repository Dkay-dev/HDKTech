/**
 * Home Page - JavaScript Logic
 * Handles carousel auto-play and page initialization
 */

// Force Bootstrap carousel auto-play with interval check
document.addEventListener('DOMContentLoaded', function() {
    var carouselEl = document.getElementById('categoryBannerCarousel');
    if (!carouselEl) return;

    // Initialize Bootstrap Carousel
    var carousel = new bootstrap.Carousel(carouselEl, {
        interval: 4000,
        pause: 'hover',
        ride: 'carousel'
    });

    // Force start cycle (redundant but ensures auto-play)
    carousel.cycle();

    // Safety timer: if carousel stops, restart it
    setInterval(function() {
        if (carousel._config && !carousel._isSliding) {
            // Carousel is idle, ensure it cycles
            try {
                carousel.cycle();
            } catch(e) {}
        }
    }, 5000);

    console.log("✅ Banner: Carousel auto-play enabled (4s interval)");
});

// ── Banner click tracking ────────────────────────────────────────────
window.trackBannerClick = function (bannerId) {
    if (!bannerId) return;
    fetch('/api/banneranalytics/track-click/' + bannerId, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
    }).catch(function () {});
};

// ── Flash Sale Countdown — endTime from data-end-time attribute ──────
(function initCountdown() {
    var cdH = document.getElementById('cd-h');
    var cdM = document.getElementById('cd-m');
    var cdS = document.getElementById('cd-s');
    if (!cdH) return;

    var countdownEl = document.getElementById('flashCountdown');
    var endTimeStr  = countdownEl ? countdownEl.dataset.endTime : '';
    var endTime     = endTimeStr ? new Date(endTimeStr) : null;

    if (!endTime || isNaN(endTime.getTime())) {
        endTime = new Date();
        endTime.setHours(23, 59, 59, 999);
    }

    function pad(n) { return n < 10 ? '0' + n : n; }

    function tick() {
        var now  = new Date();
        var diff = Math.max(0, Math.floor((endTime - now) / 1000));
        if (diff === 0) { setTimeout(function () { location.reload(); }, 1500); }
        cdH.textContent = pad(Math.floor(diff / 3600));
        cdM.textContent = pad(Math.floor((diff % 3600) / 60));
        cdS.textContent = pad(diff % 60);
    }

    tick();
    setInterval(tick, 1000);
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
