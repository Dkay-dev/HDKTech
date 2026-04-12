/**
 * CART PAGE - Logic
 * Handles quantity changes, item removal, image error recovery
 */

/**
 * Xử lý lỗi ảnh với fallback thông minh
 * @param {HTMLImageElement} img - Image element
 */
function handleCartImageError(img) {
    if (img.dataset.status === 'final') return;

    const category = img.getAttribute('data-category');
    const fileName = img.src.split('/').pop().split('?')[0];
    const cleanName = fileName.split('.')[0];

    console.log('🖼️ handleCartImageError:', {
        src: img.src,
        category: category,
        fileName: fileName,
        cleanName: cleanName,
        productId: img.dataset.productId,
        status: img.dataset.status
    });

    const folderMap = {
        "laptop": "laptops",
        "laptop gaming": "laptops-gaming",
        "main, cpu, vga": "components",
        "case, nguồn, tản": "components",
        "ổ cứng, ram, thẻ nhớ": "storage",
        "màn hình": "monitor",
        "bàn phím": "peripherals",
        "chuột + lót chuột": "peripherals",
        "tai nghe": "audio"
    };

    const folder = folderMap[category?.toLowerCase().trim()] || "accessories";

    console.log('📁 Folder mapping:', {
        category: category,
        normalized: category?.toLowerCase().trim(),
        folder: folder,
        newSrc: `/images/products/${folder}/${cleanName}.jpg`
    });

    if (!img.dataset.status) {
        img.dataset.status = 'trying-folder';
        img.src = `/images/products/${folder}/${cleanName}.jpg`;
    } else if (img.dataset.status === 'trying-folder') {
        img.dataset.status = 'final';
        img.src = '/images/products/no-image.png';
        img.onerror = null;
    }
}

/**
 * Thay đổi số lượng sản phẩm
 * @param {number} productId - ID sản phẩm
 * @param {number} delta - Thay đổi (+1 hoặc -1)
 */
function changeQty(productId, delta) {
    const input = document.getElementById(`qty-${productId}`);
    let newVal = parseInt(input.value) + delta;
    if (newVal < 1) return;
    updateQuantityAjax(productId, newVal);
}

/**
 * Cập nhật số lượng qua AJAX
 * @param {number} productId - ID sản phẩm
 * @param {number} quantity - Số lượng mới
 */
function updateQuantityAjax(productId, quantity) {
    fetch('/Cart/UpdateQuantity', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ productId, quantity })
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                document.getElementById(`qty-${productId}`).value = quantity;
                document.getElementById(`total-${productId}`).innerText = 
                    data.itemTotal.toLocaleString('vi-VN') + '₫';
                updateGlobalCartUI(data);
            }
        })
        .catch(err => console.error('❌ Update quantity error:', err));
}

/**
 * Xoá sản phẩm khỏi giỏ
 * @param {number} productId - ID sản phẩm
 */
function removeItem(productId) {
    if (!confirm('Xoá sản phẩm này khỏi giỏ hàng?')) return;

    fetch('/Cart/Remove', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ productId })
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                const row = document.getElementById(`row-${productId}`);
                row.style.transition = '0.3s';
                row.style.opacity = '0';
                setTimeout(() => {
                    row.remove();
                    updateGlobalCartUI(data);
                    if (data.totalItems === 0) location.reload();
                }, 300);
            }
        })
        .catch(err => console.error('❌ Remove item error:', err));
}

/**
 * Cập nhật giao diện giỏ hàng toàn cục
 * @param {Object} data - Dữ liệu từ API
 */
function updateGlobalCartUI(data) {
    const formattedPrice = data.totalPrice.toLocaleString('vi-VN') + '₫';
    document.getElementById('summary-subtotal').innerText = formattedPrice;
    document.getElementById('summary-total').innerText = formattedPrice;
    document.getElementById('cart-count').innerText = data.totalItems;
    
    const badge = document.getElementById('cartBadge');
    if (badge) badge.innerText = data.totalItems;
}
