/**
 * Banner Analytics - Click Tracking
 * Logs banner clicks to the analytics API
 */

(function() {
    'use strict';

    // Initialize banner analytics tracking
    function initBannerTracking() {
        const bannerLinks = document.querySelectorAll('.banner-link, [data-banner-id]');
        
        bannerLinks.forEach(link => {
            link.addEventListener('click', function(e) {
                trackBannerClick(this);
            });
        });
    }

    // Track banner click
    function trackBannerClick(element) {
        const bannerId = element.getAttribute('data-banner-id') || extractBannerIdFromParent(element);
        
        if (bannerId) {
            logClickToServer(bannerId);
        }
    }

    // Extract banner ID from parent carousel item
    function extractBannerIdFromParent(element) {
        const carouselItem = element.closest('.carousel-item');
        if (carouselItem) {
            return carouselItem.getAttribute('data-banner-id');
        }
        return null;
    }

    // Log click to server via API
    async function logClickToServer(bannerId) {
        try {
            const response = await fetch('/api/banneranalytics/click', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    bannerId: parseInt(bannerId)
                })
            });

            if (response.ok) {
                const data = await response.json();
                console.log('Banner click tracked:', data);
            } else {
                console.warn('Failed to log banner click:', response.statusText);
            }
        } catch (error) {
            console.error('Error logging banner click:', error);
            // Don't block banner navigation on tracking error
        }
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initBannerTracking);
    } else {
        initBannerTracking();
    }

    // Reinitialize when page content is dynamically loaded (SPA scenarios)
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.addedNodes.length) {
                initBannerTracking();
            }
        });
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true
    });

    // Expose function globally for manual tracking if needed
    window.trackBannerClick = function(bannerId) {
        logClickToServer(bannerId);
    };
})();
