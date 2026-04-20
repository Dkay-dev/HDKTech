/**
 * CART PAGE - Logic (MERGED Huy 3f92de9 → Khoa's variant-aware schema)
 *
 * Tính năng giữ từ Huy:
 *   - Fallback ảnh thông minh (.jpg / .png / .webp) + log chi tiết
 *   - AJAX remove + update quantity
 *
 * Tính năng refactor cho Khoa:
 *   - Mọi thao tác đều cần (productId, productVariantId) vì
 *     CartItem được key theo cặp (ProductId, ProductVariantId).
 */

// ────────────────────────────────────────────────────────────────────
//  IMAGE FALLBACK — cascade .jpg → .png → .webp → no-image
// ────────────────────────────────────────────────────────────────────
function handleCartImageError(img) {
    if (!img || img.dataset.status === 'final') return;

    const category = img.getAttribute('data-category');
    const fileName = img.src.split('/').pop().split('?')[0];
    const cleanName = fileName.split('.')[0];

    // Map category tiếng Việt → folder (copy từ Utils/ImageHelper.cs)
    const folderMap = {
        "laptop": "laptops",
        "laptop gaming": "laptops-gaming",
        "main, cpu, vga": "components",
        "case, nguồn, tản": "components",
        "ổ cứng, ram, thẻ nhớ": "storage",
        "loa, micro, webcam": "audio",
        "màn hình": "monitor",
        "bàn phím": "peripherals",
        "chuột + lót chuột": "peripherals",
        "tai nghe": "audio",
        "ghế - bàn": "furniture",
        "handheld, console": "handheld",
        "dịch vụ và thông tin khác": "services",
        "pc gvn": "pc-builds"
    };

    const folder = folderMap[category?.toLowerCase().trim()] || "accessories";

    if (!img.dataset.status) {
        img.dataset.status = 'trying-folder';
        img.src = `/images/products/${folder}/${cleanName}.jpg`;
    } else if (img.dataset.status === 'trying-folder') {
        img.dataset.status = 'trying-png';
        img.src = `/images/products/${folder}/${cleanName}.png`;
    } else if (img.dataset.status === 'trying-png') {
        img.dataset.status = 'trying-webp';
        img.src = `/images/products/${folder}/${cleanName}.webp`;
    } else if (img.dataset.status === 'trying-webp') {
        img.dataset.status = 'final';
        img.src = '/images/products/no-image.png';
        img.onerror = null;
    }
}

// ────────────────────────────────────────────────────────────────────
//  HELPERS — build DOM ids nhất quán với Cart/Index.cshtml
// ────────────────────────────────────────────────────────────────────
function rowKey(productId, productVariantId) {
    return `${productId}-${productVariantId}`;
}

function csrfToken() {
    const t = document.querySelector('input[name="__RequestVerificationToken"]');
    return t ? t.value : '';
}

// ────────────────────────────────────────────────────────────────────
//  QUANTITY
// ────────────────────────────────────────────────────────────────────
function changeQty(productId, productVariantId, delta) {
    const key = rowKey(productId, productVariantId);
    const input = document.getElementById(`qty-${key}`);
    if (!input) return;

    const newVal = parseInt(input.value) + delta;
    if (newVal < 1) return;

    updateQuantityAjax(productId, productVariantId, newVal);
}

function updateQuantityAjax(productId, productVariantId, quantity) {
    fetch('/Cart/UpdateQuantity', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': csrfToken()
        },
        body: JSON.stringify({ productId, productVariantId, quantity })
    })
        .then(res => res.json())
        .then(data => {
            if (!data.success) {
                alert(data.message || 'Không thể cập nhật số lượng.');
                return;
            }
            const key = rowKey(productId, productVariantId);
            const qtyEl = document.getElementById(`qty-${key}`);
            const totEl = document.getElementById(`total-${key}`);
            const selEl = document.getElementById(`selected-total-${key}`);
            const rowEl = document.getElementById(`row-${key}`);
            const cbEl = rowEl ? rowEl.querySelector('.cart-item-checkbox') : null;

            const fmtLine = (data.itemTotal || 0).toLocaleString('vi-VN') + '₫';
            if (qtyEl) qtyEl.value = quantity;
            if (totEl) totEl.innerText = fmtLine;
            if (selEl) selEl.innerText = fmtLine;

            // Cập nhật data-quantity để selection tính tổng đúng
            if (cbEl) cbEl.dataset.quantity = quantity;
            if (rowEl) rowEl.dataset.qty = quantity;

            updateGlobalCartUI(data);
            if (typeof updateSelection === 'function') updateSelection();
        })
        .catch(err => console.error('Update quantity error:', err));
}

// ────────────────────────────────────────────────────────────────────
//  REMOVE
// ────────────────────────────────────────────────────────────────────
function removeItem(productId, productVariantId) {
    if (!confirm('Xoá sản phẩm này khỏi giỏ hàng?')) return;

    fetch('/Cart/Remove', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': csrfToken()
        },
        body: JSON.stringify({ productId, productVariantId })
    })
        .then(res => res.json())
        .then(data => {
            if (!data.success) {
                alert(data.message || 'Không thể xoá sản phẩm.');
                return;
            }
            const key = rowKey(productId, productVariantId);
            const row = document.getElementById(`row-${key}`);
            if (row) {
                row.style.transition = '0.3s';
                row.style.opacity = '0';
                setTimeout(() => {
                    row.remove();
                    updateGlobalCartUI(data);
                    if (typeof updateSelection === 'function') updateSelection();
                    if (data.totalItems === 0) location.reload();
                }, 300);
            } else {
                updateGlobalCartUI(data);
                if (data.totalItems === 0) location.reload();
            }
        })
        .catch(err => console.error('Remove item error:', err));
}

// ────────────────────────────────────────────────────────────────────
//  UI toàn cục (badge giỏ trên header, total ở summary card)
// ────────────────────────────────────────────────────────────────────
function updateGlobalCartUI(data) {
    if (!data) return;
    const fmt = (typeof data.totalPrice === 'number'
        ? data.totalPrice.toLocaleString('vi-VN') + '₫'
        : data.totalPrice) || '';

    const subEl = document.getElementById('summary-subtotal');
    const totalEl = document.getElementById('summary-total');
    const selEl = document.getElementById('summary-selected');
    const countEl = document.getElementById('cart-count');
    const badgeEl = document.getElementById('cartBadge');

    if (subEl) subEl.innerText = fmt;
    if (totalEl) totalEl.innerText = fmt;
    if (selEl) selEl.innerText = fmt;
    if (countEl) countEl.innerText = data.totalItems;
    if (badgeEl) badgeEl.innerText = data.totalItems;
}

// ── Cart Selection Logic ─────────────────────────────────────────────
function updateSelection() {
    const checkboxes   = document.querySelectorAll('.cart-item-checkbox');
    const checkedBoxes = document.querySelectorAll('.cart-item-checkbox:checked');
    let selectedTotal  = 0;

    checkedBoxes.forEach(cb => {
        selectedTotal += (parseFloat(cb.dataset.price) || 0) * (parseInt(cb.dataset.quantity) || 1);
    });

    document.querySelectorAll('.cart-item-row').forEach(row => {
        const cb     = row.querySelector('.cart-item-checkbox');
        const key    = cb.dataset.rowKey;
        const price  = parseFloat(cb.dataset.price)    || 0;
        const qty    = parseInt(cb.dataset.quantity)   || 1;
        const selTot = document.getElementById(`selected-total-${key}`);

        if (cb.checked) {
            row.classList.add('selected');
            if (selTot) selTot.textContent = (price * qty).toLocaleString('vi-VN') + '₫';
        } else {
            row.classList.remove('selected');
        }
    });

    const selectAll = document.getElementById('selectAll');
    if (selectAll) {
        if (checkedBoxes.length === checkboxes.length && checkboxes.length > 0) {
            selectAll.checked = true;  selectAll.indeterminate = false;
        } else if (checkedBoxes.length > 0) {
            selectAll.checked = false; selectAll.indeterminate = true;
        } else {
            selectAll.checked = false; selectAll.indeterminate = false;
        }
    }

    const fmt    = selectedTotal.toLocaleString('vi-VN') + '₫';
    const selSum = document.getElementById('summary-selected');
    const totSum = document.getElementById('summary-total');
    if (selSum) selSum.textContent = fmt;
    if (totSum) totSum.textContent = fmt;

    const btn = document.getElementById('btn-checkout');
    if (btn) btn.disabled = checkedBoxes.length === 0;
}

function toggleSelectAll() {
    const selectAll  = document.getElementById('selectAll');
    const checkboxes = document.querySelectorAll('.cart-item-checkbox');
    checkboxes.forEach(cb => cb.checked = selectAll.checked);
    updateSelection();
}

function goToCheckout() {
    const picked = Array.from(document.querySelectorAll('.cart-item-checkbox:checked'))
        .map(cb => ({
            productId:        parseInt(cb.dataset.productId),
            productVariantId: parseInt(cb.dataset.variantId)
        }));

    if (picked.length === 0) {
        alert('Vui lòng chọn ít nhất một sản phẩm để đặt hàng.');
        return;
    }

    window.location.href = '/Checkout/Index?selectedItems=' + encodeURIComponent(JSON.stringify(picked));
}

document.addEventListener('DOMContentLoaded', updateSelection);
