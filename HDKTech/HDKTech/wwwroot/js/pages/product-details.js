/**
 * Product Details Page - JavaScript Logic
 * Handles image loading, quantity management, reviews, and cart operations
 */

// ===== IMAGE HANDLING =====
const folderTryOrder = ['laptops', 'laptops-gaming', 'components', 'peripherals',
    'accessories', 'audio', 'furniture', 'handheld', 'monitor',
    'pc-builds', 'services', 'storage'];

const foundImages = new Set();

/**
 * Handle image loading errors with smart fallback across folders
 * @param {HTMLImageElement} img - The image element that failed to load
 * @param {Event} event - The error event
 */
function handleImageError(img, event) {
    // Nếu đã là no-image, dừng lại
    if (img.src.includes('no-image.png')) {
        return;
    }

    // Lấy tên file
    const fileName = img.src.split('/').pop();
    const currentPath = img.src;

    // Lấy folder hiện tại từ path
    const pathParts = currentPath.split('/');
    const currentFolder = pathParts[4];

    console.log(`Image error - fileName: ${fileName}, currentFolder: ${currentFolder}`);

    // Tìm index folder hiện tại
    let currentIndex = folderTryOrder.indexOf(currentFolder);
    let foundAny = false;

    // Thử các folder tiếp theo
    for (let i = currentIndex + 1; i < folderTryOrder.length; i++) {
        const nextFolder = folderTryOrder[i];
        const newPath = `/images/products/${nextFolder}/${fileName}`;

        // Tạo hình ảnh test để kiểm tra xem file có tồn tại không
        const testImg = new Image();
        testImg.onload = function() {
            img.src = newPath;
            foundImages.add(fileName);
            foundAny = true;
            if (img.id === 'mainImg' && img.parentElement && img.parentElement.classList.contains('glightbox')) {
                img.parentElement.href = newPath;
                if (typeof lightbox !== 'undefined') {
                    lightbox.reload();
                }
            }
            console.log(`✓ Found at: ${newPath}`);
        };
        testImg.onerror = function() {
            // File không tồn tại, tiếp tục thử folder tiếp theo
        };
        testImg.src = newPath;
    }

    // Nếu không tìm thấy ở đâu, chỉ lúc đó mới ẩn ảnh (sau 200ms để chắc chắn)
    setTimeout(() => {
        if (!foundImages.has(fileName) && !img.src.includes('no-image.png')) {
            img.style.display = 'none';
            // Ẩn card wrapper nếu là related products
            if (img.classList.contains('related-img')) {
                const cardWrapper = img.closest('.related-card');
                if (cardWrapper) {
                    cardWrapper.style.opacity = '0.5';
                    cardWrapper.style.pointerEvents = 'none';
                }
            }
            console.log(`✗ All folders failed, hiding image`);
        }
    }, 200);
}

/**
 * Update main product image when clicking thumbnail
 * @param {string} url - New image URL
 * @param {HTMLElement} el - The thumbnail element clicked
 */
function updateMainImage(url, el) {
    var mainImg = document.getElementById('mainImg');
    var glightboxLink = mainImg.parentElement;
    console.log('Updating image to:', url);

    // Restore image display nếu nó bị ẩn trước đó
    mainImg.style.display = 'block';

    // Cập nhật src
    mainImg.src = url;
    glightboxLink.href = url;

    // Cập nhật active state
    document.querySelectorAll('.thumb-item').forEach(t => t.classList.remove('active'));
    el.classList.add('active');

    // Reinitialize lightbox
    if (typeof lightbox !== 'undefined') {
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

    // Áp dụng error handler cho tất cả ảnh
    document.querySelectorAll('img').forEach(img => {
        if (img.className.includes('main-img') || img.className.includes('thumb-item') || img.className.includes('related-img')) {
            img.addEventListener('error', function(e) {
                handleImageError(this, e);
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
 * Retrieves maSanPham from the page and quantity from input
 */
function buyNow() {
    // Lấy mã sản phẩm từ model
    const maSanPham = document.querySelector('input[name="maSanPham"]')?.value ||
        window.productId || parseInt(window.location.pathname.split('/').pop());
    const qty = parseInt(document.getElementById('qtyInput').value) || 1;

    // Gửi AJAX để thêm vào giỏ hàng
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
                // Nếu thành công, chuyển sang Checkout
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
 * @param {number} productId - Product ID
 * @param {string} productName - Product name (for display)
 * @param {number} quantity - Quantity to add
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
                // Update cart badge if exists
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

/**
 * Toggle like/unlike on review
 * @param {HTMLElement} btn - The like button element
 */
function toggleLike(btn) {
    btn.classList.toggle('liked');
    const countSpan = btn.querySelector('span');
    let count = parseInt(countSpan.textContent);
    countSpan.textContent = btn.classList.contains('liked') ? count + 1 : Math.max(0, count - 1);
}

/**
 * Toggle dislike/undislike on review
 * @param {HTMLElement} btn - The dislike button element
 */
function toggleDislike(btn) {
    btn.classList.toggle('liked');
    const countSpan = btn.querySelector('span');
    let count = parseInt(countSpan.textContent);
    countSpan.textContent = btn.classList.contains('liked') ? count + 1 : Math.max(0, count - 1);
}

/**
 * Load more reviews via AJAX
 * TODO: Implement full AJAX loading with pagination
 */
function loadMoreReviews() {
    // TODO: Implement load more reviews via AJAX
    alert('Chức năng tải thêm nhận xét sẽ được implement sớm');
}
