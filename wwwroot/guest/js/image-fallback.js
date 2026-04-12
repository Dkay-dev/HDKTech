/**
 * Image Fallback Handler - HDKTech
 * Robust onerror handler với anti-infinite-loop protection
 * 
 * Features:
 * - Retry limit (max 2 retries) to prevent infinite loops
 * - Auto-detect missing /images/products/ prefix
 * - Convert .png/.jpeg to .jpg (most products are .jpg)
 * - Final fallback to no-image.png
 */

(function() {
    'use strict';

    /**
     * Handle image load errors with intelligent fallback strategy
     * @param {HTMLImageElement} img - The image element that failed
     * @param {Event} [event] - Optional error event
     */
    window.handleImageError = function(img, event) {
        if (!img || !(img instanceof HTMLImageElement)) {
            console.warn('handleImageError: Invalid image element', img);
            return;
        }

        try {
            // ===== ANTI-INFINITE-LOOP PROTECTION =====
            var retryKey = 'imgRetries';
            var maxRetries = 2;
            var retries = parseInt(img.dataset[retryKey] || '0', 10);

            if (retries >= maxRetries) {
                // Max retries reached => use fallback and prevent further onerror
                img.removeAttribute('onerror');
                img.dataset[retryKey] = '99'; // Mark as exhausted
                img.src = '/images/products/no-image.png';
                console.warn('handleImageError: Max retries reached for', img.src);
                return;
            }

            // Increment retry count
            img.dataset[retryKey] = (retries + 1).toString();

            var src = img.getAttribute('src') || '';
            if (!src) {
                img.src = '/images/products/no-image.png';
                return;
            }

            // ===== STRATEGY 1: Add missing /images/products/ prefix =====
            var strategy1 = src;
            if (!src.match(/^https?:\/\//i) && !src.startsWith('/')) {
                strategy1 = '/images/products/' + src;
            }

            // If strategy1 is already current, skip to strategy2
            if (strategy1 !== img.src) {
                img.src = strategy1;
                console.log('handleImageError: Trying with /images/products/ prefix', strategy1);
                return; // Wait for load/error event on this attempt
            }

            // ===== STRATEGY 2: Convert extension to .jpg =====
            // (because most real images are .jpg despite DB may store .png)
            var strategy2 = src.replace(/\.(png|jpeg|jpg)$/i, '') + '.jpg';

            // Add prefix if missing
            if (!strategy2.match(/^https?:\/\//i) && !strategy2.startsWith('/')) {
                strategy2 = '/images/products/' + strategy2;
            }

            if (strategy2 !== img.src) {
                img.src = strategy2;
                console.log('handleImageError: Trying with .jpg extension', strategy2);
                return;
            }

            // ===== STRATEGY 3: Fallback to no-image =====
            img.removeAttribute('onerror'); // Prevent further onerror triggers
            img.src = '/images/products/no-image.png';
            console.warn('handleImageError: All strategies failed, using no-image fallback');

        } catch (error) {
            console.error('handleImageError: Exception caught', error);
            try {
                img.removeAttribute('onerror');
                img.src = '/images/products/no-image.png';
            } catch (e2) {
                console.error('handleImageError: Failed to set fallback', e2);
            }
        }
    };

    console.log('✅ Image Fallback Handler loaded');
})();
