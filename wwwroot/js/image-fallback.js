/**
 * Image Fallback Handler - Simple
 */
(function() {
    'use strict';

    window.handleImageError = function(img) {
        if (!img) return;
        if (img.tagName !== 'IMG') return;
        if (img.dataset.handled === 'true') return;
        
        img.dataset.handled = 'true';
        img.onerror = null;
        img.src = '/images/products/no-image.png';
    };

    window.handleCartImageError = function(img) {
        if (!img) return;
        if (img.tagName !== 'IMG') return;
        if (img.dataset.handled === 'true') return;
        
        img.dataset.handled = 'true';
        img.onerror = null;
        img.src = '/images/products/no-image.png';
    };

    console.log('✅ Image fallback loaded');
})();