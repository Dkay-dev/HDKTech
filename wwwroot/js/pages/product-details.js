/**
 * Product Details Page - JavaScript Logic
 * Handles image loading, quantity management, reviews, and cart operations
 */

// ===== IMAGE HANDLING =====
const folderTryOrder = ['laptops', 'laptops-gaming', 'components', 'peripherals',
    'accessories', 'audio', 'furniture', 'handheld', 'monitor',
    'pc-builds', 'services', 'storage'];

const foundImages = new Map();

/**
 * Handle image loading errors with smart fallback across folders
 * @param {HTMLImageElement} img - The image element that failed to load
 */
function handleImageError(img) {
    // Nếu đã là no-image, dừng lại
    if (img.src.includes('no-image.png')) {
        return;
    }

    // Lấy tên file gốc (không có path)
    const fileName = img.src.split('/').pop();
    
    // Nếu đã thử tất cả các folder rồi
    if (foundImages.has(fileName) && foundImages.get(fileName) === 'failed') {
        img.src = '/images/products/no-image.png';
        img.onerror = null;
        return;
    }

    // Nếu đã tìm thấy ở folder nào đó
    if (foundImages.has(fileName)) {
        return;
    }

    // Tìm folder hiện tại từ path
    const pathParts = img.src.split('/');
    let currentFolder = '';
    for (let i = 0; i < pathParts.length; i++) {
        if (pathParts[i] === 'products' && pathParts[i + 1]) {
            currentFolder = pathParts[i + 1];
            break;
        }
    }

    let found = false;
    
    // Thử các folder theo thứ tự
    const foldersToTry = currentFolder 
        ? [...folderTryOrder.slice(folderTryOrder.indexOf(currentFolder) + 1), ...folderTryOrder]
        : folderTryOrder;

    const uniqueFolders = [...new Set(foldersToTry)];

    for (const folder of uniqueFolders) {
        if (found) break;
        
        // Thử .jpg
        const testJpg = new Image();
        testJpg.onload = function() {
            if (!found) {
                found = true;
                foundImages.set(fileName, `/images/products/${folder}/${fileName}`);
                img.src = `/images/products/${folder}/${fileName}`;
                updateLightboxLink(img, `/images/products/${folder}/${fileName}`);
                console.log(`✓ Found image: /images/products/${folder}/${fileName}`);
            }
        };
        testJpg.onerror = function() {
            // Thử .png
            const testPng = new Image();
            testPng.onload = function() {
                if (!found) {
                    found = true;
                    foundImages.set(fileName, `/images/products/${folder}/${fileName}`);
                    img.src = `/images/products/${folder}/${fileName}`;
                    updateLightboxLink(img, `/images/products/${folder}/${fileName}`);
                    console.log(`✓ Found image: /images/products/${folder}/${fileName}`);
                }
            };
            testPng.onerror = function() {
                // Thử .webp
                const testWebp = new Image();
                testWebp.onload = function() {
                    if (!found) {
                        found = true;
                        foundImages.set(fileName, `/images/products/${folder}/${fileName}`);
                        img.src = `/images/products/${folder}/${fileName}`;
                        updateLightboxLink(img, `/images/products/${folder}/${fileName}`);
                        console.log(`✓ Found image: /images/products/${folder}/${fileName}`);
                    }
                };
                testWebp.onerror = function() {
                    // Không tìm thấy, tiếp tục folder tiếp theo
                };
                testWebp.src = `/images/products/${folder}/${fileName.replace(/\.[^.]+$/, '.webp')}`;
            };
            testPng.src = `/images/products/${folder}/${fileName.replace(/\.[^.]+$/, '.png')}`;
        };
        testJpg.src = `/images/products/${folder}/${fileName}`;
    }

    // Đánh dấu là failed sau 3 giây nếu không tìm thấy
    setTimeout(() => {
        if (!foundImages.has(fileName)) {
            foundImages.set(fileName, 'failed');
            img.src = '/images/products/no-image.png';
            img.onerror = null;
            updateLightboxLink(img, '/images/products/no-image.png');
        }
    }, 3000);
}

/**
 * Update GLightbox link when image changes
 */
function updateLightboxLink(img, newSrc) {
    const glightboxLink = img.closest('a.glightbox');
    if (glightboxLink) {
        glightboxLink.href = newSrc;
        if (typeof lightbox !== 'undefined' && lightbox.reload) {
            lightbox.reload();
        }
    }
}

/**
 * Update main product image when clicking thumbnail
 * @param {string} url - New image URL
 * @param {HTMLElement} el - The thumbnail element clicked
 */
function updateMainImage(url, el) {
    var mainImg = document.getElementById('mainImg');
    var glightboxLink = mainImg.parentElement;
    
    // Restore image display nếu nó bị ẩn trước đó
    mainImg.style.display = 'block';
    mainImg.style.opacity = '1';

    // Cập nhật src
    mainImg.src = url;
    glightboxLink.href = url;

    // Cập nhật active state
    document.querySelectorAll('.thumb-item').forEach(t => t.classList.remove('active'));
    el.classList.add('active');

    // Reinitialize lightbox
    if (typeof lightbox !== 'undefined' && lightbox.reload) {
        lightbox.reload();
    }
}

/**
 * Debug: Log all image paths on page load
 */
window.addEventListener('DOMContentLoaded', function() {
    console.log('=== IMAGE PATHS DEBUG ===');
    document.querySelectorAll('img').forEach((img, idx) => {
        if (img.className.includes('main-img') || img.className.includes('thumb-item') || img.className.includes('related-img')) {
            console.log(`Image ${idx}: ${img.src}`);
        }
    });

    // Áp dụng error handler cho tất cả ảnh sản phẩm
    document.querySelectorAll('.main-img, .thumb-item, .related-img').forEach(img => {
        if (!img.hasAttribute('data-error-bound')) {
            img.setAttribute('data-error-bound', 'true');
            img.addEventListener('error', function() {
                handleImageError(this);
            });
        }
    });
});

// ===== QUANTITY MANAGEMENT =====

/**
 * Update product quantity (increment/decrement)
 * @param {number} v - Value to add (positive or negative)
 */
function updateQty(v) {
    let i = document.getElementById('qtyInput');
    let n = parseInt(i.value) + v;
    if (n >= 1) i.value = n;
}

// ===== CART OPERATIONS =====

/**
 * Add product to cart and navigate to checkout
 */
function buyNow() {
    const maSanPham = document.querySelector('input[name="maSanPham"]')?.value ||
        window.productId || parseInt(window.location.pathname.split('/').pop());
    const qty = parseInt(document.getElementById('qtyInput').value) || 1;

    fetch('/Cart/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: JSON.stringify({
            productId: maSanPham,
            quantity: qty
        })
    })
        .then(response => {
            if (response.ok) {
                window.location.href = '/Checkout';
            } else {
                return response.json().then(data => {
                    alert(data.message || 'Lỗi khi thêm vào giỏ hàng. Vui lòng thử lại.');
                });
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Lỗi khi thêm vào giỏ hàng. Vui lòng thử lại.');
        });
}

/**
 * Add product to cart via AJAX
 */
function addToCartAjax(productId, productName, quantity) {
    quantity = parseInt(quantity) || 1;

    fetch('/Cart/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: JSON.stringify({
            productId: productId,
            quantity: quantity
        })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                alert(`${productName} đã được thêm vào giỏ hàng!`);
                const cartBadge = document.querySelector('[id*="cart-count"], [id*="cartBadge"]');
                if (cartBadge && data.cartCount) {
                    cartBadge.textContent = data.cartCount;
                }
            } else {
                alert(data.message || 'Lỗi khi thêm vào giỏ hàng');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Lỗi khi thêm vào giỏ hàng. Vui lòng thử lại.');
        });
}

// ===== REVIEW INTERACTIONS =====

function toggleLike(btn) {
    btn.classList.toggle('liked');
    const countSpan = btn.querySelector('span');
    let count = parseInt(countSpan.textContent);
    countSpan.textContent = btn.classList.contains('liked') ? count + 1 : Math.max(0, count - 1);
}

function toggleDislike(btn) {
    btn.classList.toggle('liked');
    const countSpan = btn.querySelector('span');
    let count = parseInt(countSpan.textContent);
    countSpan.textContent = btn.classList.contains('liked') ? count + 1 : Math.max(0, count - 1);
}

function loadMoreReviews() {
    alert('Chức năng tải thêm nhận xét sẽ được implement sớm');
}
