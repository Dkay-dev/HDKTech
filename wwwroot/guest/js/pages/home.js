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
