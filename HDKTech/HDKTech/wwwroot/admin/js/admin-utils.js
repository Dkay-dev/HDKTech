/**
 * admin-utils.js — HDKTech Admin Utility Library
 * =================================================
 * Tập trung toàn bộ logic:
 *   1. SweetAlert2 Gold/Black Luxury — xác nhận xoá
 *   2. Toastr — thông báo nhanh góc phải
 *   3. TempData bridge — tự động hiển thị toast từ TempData server-side
 *   4. AJAX helpers — gọi API với CSRF token
 *   5. Utility functions — format tiền, debounce, v.v.
 *
 * Gọi file này từ _AdminLayout.cshtml (đã được nhúng sẵn)
 * Không import bất kỳ class Tailwind nào — chỉ dùng Bootstrap 5 + inline style
 */

'use strict';

// ═══════════════════════════════════════════════════════════════════════════
// 1. MÀU SẮC LUXURY — Gold / Black / Red HDKTech
// ═══════════════════════════════════════════════════════════════════════════
const HDK_COLORS = {
    gold:        '#c9a84c',
    goldDark:    '#a07a2f',
    goldLight:   '#f0d080',
    black:       '#1a1a1a',
    darkGray:    '#2c2c2c',
    red:         '#bc000c',
    redDark:     '#8b0000',
    white:       '#ffffff',
    surface:     '#fcf9f8'
};

// ═══════════════════════════════════════════════════════════════════════════
// 2. SWEETALERT2 — XÁC NHẬN XOÁ (Gold/Black Luxury)
// ═══════════════════════════════════════════════════════════════════════════

/**
 * Hiển thị dialog xác nhận xoá kiểu Luxury với màu Gold/Black
 * @param {object} options - Tuỳ chọn cấu hình
 * @param {string} options.title        - Tiêu đề (mặc định: 'Xác nhận xoá')
 * @param {string} options.text         - Nội dung mô tả
 * @param {string} options.itemName     - Tên item đang xoá (hiển thị in đậm)
 * @param {Function} options.onConfirm  - Callback khi user xác nhận
 * @param {Function} options.onCancel   - Callback khi user huỷ (optional)
 */
function hdkConfirmDelete(options = {}) {
    const {
        title     = 'Xác nhận xoá',
        text      = null,
        itemName  = null,
        onConfirm = null,
        onCancel  = null
    } = options;

    const htmlContent = itemName
        ? `Bạn có chắc muốn xoá <strong style="color:${HDK_COLORS.goldLight}">${itemName}</strong>?<br>
           <small style="color:rgba(255,255,255,0.6)">Hành động này không thể hoàn tác.</small>`
        : (text || 'Bạn có chắc muốn xoá mục này? Hành động này không thể hoàn tác.');

    return Swal.fire({
        title:              title,
        html:               htmlContent,
        icon:               'warning',
        background:         HDK_COLORS.black,
        color:              HDK_COLORS.white,
        showCancelButton:   true,
        confirmButtonText:  '🗑 Xoá',
        cancelButtonText:   'Huỷ',
        reverseButtons:     true,

        // Nút xoá: đỏ sang trọng
        confirmButtonColor: HDK_COLORS.red,
        cancelButtonColor:  HDK_COLORS.darkGray,

        // Icon warning màu Gold
        iconColor:          HDK_COLORS.gold,

        // Custom CSS cho luxury feel
        customClass: {
            popup:          'hdk-swal-luxury-popup',
            title:          'hdk-swal-luxury-title',
            confirmButton:  'hdk-swal-confirm-btn',
            cancelButton:   'hdk-swal-cancel-btn'
        }
    }).then(result => {
        if (result.isConfirmed && typeof onConfirm === 'function') {
            onConfirm(result);
        } else if (!result.isConfirmed && typeof onCancel === 'function') {
            onCancel(result);
        }
        return result;
    });
}

/**
 * Shorthand: Xác nhận xoá → submit form nếu user đồng ý
 * Dùng trực tiếp trong onclick của nút Xoá trong bảng
 * @param {Event}  event    - Event từ onclick
 * @param {string} itemName - Tên item hiển thị trong dialog
 */
function hdkDeleteForm(event, itemName) {
    event.preventDefault();
    const form = event.target.closest('form') || event.currentTarget.closest('form');

    hdkConfirmDelete({
        itemName:  itemName,
        onConfirm: () => { if (form) form.submit(); }
    });
}

/**
 * Tương thích ngược với code cũ dùng confirmDelete()
 * Bây giờ dùng SweetAlert2 thay vì confirm() native
 */
function confirmDelete(message) {
    // Trả về false để chặn form submit ngay lập tức
    // Dùng hdkDeleteForm() hoặc hdkConfirmDelete() thay thế
    const itemName = message || 'mục này';
    hdkConfirmDelete({ text: message });
    return false; // Luôn chặn form — SweetAlert2 sẽ submit sau khi confirm
}

// ═══════════════════════════════════════════════════════════════════════════
// 3. TOASTR — THÔNG BÁO NHANH (Toast notifications)
// ═══════════════════════════════════════════════════════════════════════════

/**
 * Hiển thị toast bằng Toastr (nếu có) hoặc fallback dùng SweetAlert2 toast
 * @param {string} message  - Nội dung thông báo
 * @param {'success'|'error'|'warning'|'info'} type - Loại thông báo
 * @param {string} title    - Tiêu đề (optional)
 * @param {number} duration - Thời gian hiển thị ms (mặc định 4000)
 */
function hdkToast(message, type = 'success', title = null, duration = 4000) {
    // Ưu tiên Toastr nếu đã load
    if (typeof toastr !== 'undefined') {
        toastr.options = {
            closeButton:       true,
            progressBar:       true,
            positionClass:     'toast-top-right',
            timeOut:           duration,
            extendedTimeOut:   1000,
            showEasing:        'swing',
            hideEasing:        'linear',
            showMethod:        'fadeIn',
            hideMethod:        'fadeOut'
        };

        switch (type) {
            case 'success': toastr.success(message, title || 'Thành công'); break;
            case 'error':   toastr.error(message,   title || 'Lỗi');        break;
            case 'warning': toastr.warning(message, title || 'Cảnh báo');   break;
            default:        toastr.info(message,    title || 'Thông báo');  break;
        }
        return;
    }

    // Fallback: SweetAlert2 toast
    if (typeof Swal !== 'undefined') {
        const iconMap = { success: 'success', error: 'error', warning: 'warning', info: 'info' };
        Swal.mixin({
            toast:             true,
            position:          'top-end',
            showConfirmButton: false,
            timer:             duration,
            timerProgressBar:  true,
            showCloseButton:   true
        }).fire({
            icon:  iconMap[type] || 'info',
            title: title || (type === 'success' ? 'Thành công' : type === 'error' ? 'Lỗi' : 'Thông báo'),
            text:  message
        });
        return;
    }

    // Ultimate fallback: Bootstrap toast tự build
    _showBootstrapToast(message, type, title, duration);
}

/**
 * Tạo Bootstrap toast thủ công (fallback khi không có Toastr/SweetAlert2)
 */
function _showBootstrapToast(message, type, title, duration) {
    const bgMap = {
        success: 'bg-success',
        error:   'bg-danger',
        warning: 'bg-warning text-dark',
        info:    'bg-info text-dark'
    };
    const iconMap = {
        success: 'bi-check-circle-fill',
        error:   'bi-x-circle-fill',
        warning: 'bi-exclamation-triangle-fill',
        info:    'bi-info-circle-fill'
    };

    const toastId = 'hdk-toast-' + Date.now();
    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-white ${bgMap[type] || 'bg-secondary'} border-0 mb-2"
             role="alert" aria-live="assertive" data-bs-delay="${duration}">
            <div class="d-flex">
                <div class="toast-body d-flex align-items-center gap-2">
                    <i class="bi ${iconMap[type] || 'bi-bell'}"></i>
                    <span>${message}</span>
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>`;

    let container = document.getElementById('hdk-toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'hdk-toast-container';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '9999';
        document.body.appendChild(container);
    }

    container.insertAdjacentHTML('beforeend', toastHtml);
    const toastEl = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastEl);
    toast.show();
    toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
}

// ═══════════════════════════════════════════════════════════════════════════
// 4. TEMPDATA BRIDGE — Tự động hiển thị toast khi page load
//    Đọc từ <span id="toast-success|error|warning|info" data-msg="...">
//    (được render bởi _AdminLayout.cshtml từ TempData server-side)
// ═══════════════════════════════════════════════════════════════════════════
document.addEventListener('DOMContentLoaded', function () {

    const toastMap = [
        { id: 'toast-success', type: 'success' },
        { id: 'toast-error',   type: 'error'   },
        { id: 'toast-warning', type: 'warning' },
        { id: 'toast-info',    type: 'info'    }
    ];

    toastMap.forEach(({ id, type }) => {
        const el = document.getElementById(id);
        if (el && el.dataset.msg) {
            hdkToast(el.dataset.msg, type);
        }
    });

    // ── Gắn SweetAlert2 delete confirmation cho tất cả nút có data-confirm-item
    // Thay thế onclick="return confirmDelete(...)" cũ
    // Dùng: <button data-confirm-item="Tên sản phẩm" onclick="hdkDeleteForm(event, this.dataset.confirmItem)">
    document.querySelectorAll('[data-hdk-delete]').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            const itemName = this.dataset.hdkDelete || 'mục này';
            const form     = this.closest('form');
            hdkConfirmDelete({
                itemName:  itemName,
                onConfirm: () => { if (form) form.submit(); }
            });
        });
    });

});

// ═══════════════════════════════════════════════════════════════════════════
// 5. AJAX HELPER — Gọi API với CSRF Token tự động
// ═══════════════════════════════════════════════════════════════════════════

/**
 * Gọi API với method POST, tự động đính kèm Anti-Forgery Token
 * @param {string} url    - Endpoint URL
 * @param {object|null} data - Dữ liệu gửi đi (JSON)
 * @param {string} method - HTTP method (mặc định 'POST')
 * @returns {Promise<Response>}
 */
async function hdkApiCall(url, data = null, method = 'POST') {
    const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value
                   || document.querySelector('meta[name="csrf-token"]')?.content
                   || '';

    const options = {
        method:  method,
        headers: {
            'Content-Type':              'application/json',
            'RequestVerificationToken':  csrfToken,
            'X-Requested-With':          'XMLHttpRequest'
        }
    };

    if (data && method !== 'GET') {
        options.body = JSON.stringify(data);
    }

    return fetch(url, options);
}

/**
 * Gọi API và tự động hiển thị toast kết quả
 * @param {string}   url          - Endpoint URL
 * @param {object}   data         - Request data
 * @param {Function} onSuccess    - Callback khi success (nhận JSON response)
 * @param {string}   successMsg   - Thông báo thành công (override response.message)
 */
async function hdkApiCallWithFeedback(url, data, onSuccess, successMsg = null) {
    try {
        const response = await hdkApiCall(url, data);
        const json     = await response.json();

        if (json.success || response.ok) {
            hdkToast(successMsg || json.message || 'Thao tác thành công', 'success');
            if (typeof onSuccess === 'function') onSuccess(json);
        } else {
            hdkToast(json.message || 'Thao tác thất bại', 'error');
        }
    } catch (err) {
        console.error('[HDKTech] API Error:', err);
        hdkToast('Lỗi kết nối. Vui lòng thử lại.', 'error');
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// 6. UTILITY FUNCTIONS
// ═══════════════════════════════════════════════════════════════════════════

/** Format số thành tiền VNĐ */
function formatCurrency(value) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value);
}

/** Format phần trăm */
function formatPercentage(value, decimals = 1) {
    return parseFloat(value).toFixed(decimals) + '%';
}

/** Debounce — giới hạn tần suất gọi hàm */
function debounce(func, wait = 300) {
    let timeout;
    return function (...args) {
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(this, args), wait);
    };
}

/**
 * Toggle loading state cho button
 * @param {HTMLElement} btn       - Nút cần toggle
 * @param {boolean}     isLoading - true = hiển thị spinner
 * @param {string}      loadText  - Text khi loading
 */
function toggleButtonLoading(btn, isLoading = true, loadText = 'Đang xử lý...') {
    if (isLoading) {
        btn._originalHtml = btn.innerHTML;
        btn.disabled      = true;
        btn.innerHTML     = `<span class="spinner-border spinner-border-sm me-1"></span>${loadText}`;
    } else {
        btn.disabled  = false;
        btn.innerHTML = btn._originalHtml || btn.innerHTML;
    }
}

/** Copy text vào clipboard */
async function hdkCopyToClipboard(text, successMsg = 'Đã sao chép!') {
    try {
        await navigator.clipboard.writeText(text);
        hdkToast(successMsg, 'success');
    } catch {
        hdkToast('Không thể sao chép. Vui lòng copy thủ công.', 'warning');
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// 7. LUXURY CSS INJECTION — Style cho SweetAlert2 popup
// ═══════════════════════════════════════════════════════════════════════════
(function injectLuxuryStyles() {
    if (document.getElementById('hdk-swal-luxury-styles')) return;

    const style = document.createElement('style');
    style.id    = 'hdk-swal-luxury-styles';
    style.textContent = `
        /* SweetAlert2 Luxury Gold/Black Theme */
        .hdk-swal-luxury-popup {
            border: 1px solid ${HDK_COLORS.gold} !important;
            border-radius: 12px !important;
            box-shadow: 0 20px 60px rgba(0,0,0,0.5), 0 0 30px rgba(201,168,76,0.15) !important;
        }
        .hdk-swal-luxury-title {
            color: ${HDK_COLORS.gold} !important;
            font-weight: 700 !important;
            font-size: 1.3rem !important;
        }
        .hdk-swal-confirm-btn {
            border-radius: 8px !important;
            font-weight: 600 !important;
            padding: 10px 24px !important;
            letter-spacing: 0.03em !important;
        }
        .hdk-swal-cancel-btn {
            border-radius: 8px !important;
            font-weight: 600 !important;
            padding: 10px 24px !important;
            border: 1px solid rgba(255,255,255,0.2) !important;
            color: rgba(255,255,255,0.8) !important;
        }
        .hdk-swal-cancel-btn:hover {
            background-color: rgba(255,255,255,0.1) !important;
        }
        /* Toastr custom style phù hợp với luxury theme */
        #toast-container .toast {
            border-radius: 8px !important;
            box-shadow: 0 4px 20px rgba(0,0,0,0.3) !important;
        }
        #toast-container .toast-success {
            background-color: #1a3d1a !important;
            border-left: 4px solid #4caf50 !important;
        }
        #toast-container .toast-error {
            background-color: #3d1a1a !important;
            border-left: 4px solid ${HDK_COLORS.red} !important;
        }
        #toast-container .toast-warning {
            background-color: #3d301a !important;
            border-left: 4px solid ${HDK_COLORS.gold} !important;
            color: #fff !important;
        }
        #toast-container .toast-info {
            background-color: #1a2a3d !important;
            border-left: 4px solid #2196f3 !important;
        }
    `;
    document.head.appendChild(style);
})();

// Expose public API
window.HDKAdmin = {
    confirmDelete:        hdkConfirmDelete,
    deleteForm:           hdkDeleteForm,
    toast:                hdkToast,
    api:                  hdkApiCall,
    apiWithFeedback:      hdkApiCallWithFeedback,
    formatCurrency:       formatCurrency,
    formatPercentage:     formatPercentage,
    debounce:             debounce,
    toggleButtonLoading:  toggleButtonLoading,
    copyToClipboard:      hdkCopyToClipboard
};
