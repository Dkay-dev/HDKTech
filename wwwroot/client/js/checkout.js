/**
 * checkout.js — JS dành riêng cho trang Xác nhận Thanh toán (Client)
 * Tách file riêng theo yêu cầu kỹ thuật - gọi qua @section Scripts
 * Sử dụng SweetAlert2 cho các thông báo thành công/thất bại
 */

document.addEventListener('DOMContentLoaded', function () {

    // ── Hiển thị thông báo SweetAlert2 từ TempData ────────────────────────
    // TempData được render vào hidden input bởi View
    const successMsg = document.getElementById('swal-success-msg')?.value;
    const errorMsg   = document.getElementById('swal-error-msg')?.value;

    if (successMsg && typeof Swal !== 'undefined') {
        Swal.fire({
            icon: 'success',
            title: 'Đặt hàng thành công!',
            html: successMsg,
            confirmButtonText: 'Xem đơn hàng',
            confirmButtonColor: '#e31837',
            allowOutsideClick: false
        });
    }

    if (errorMsg && typeof Swal !== 'undefined') {
        Swal.fire({
            icon: 'error',
            title: 'Đặt hàng thất bại',
            text: errorMsg,
            confirmButtonText: 'Thử lại',
            confirmButtonColor: '#e31837'
        });
    }

    // ── Disable nút submit sau khi click để tránh double-submit ─────────
    const form = document.getElementById('checkoutForm');
    if (form) {
        form.addEventListener('submit', function (e) {
            const btn = form.querySelector('[type="submit"]');
            if (btn) {
                btn.disabled = true;
                btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang xử lý...';
            }
        });
    }

    // ── Định dạng số điện thoại tự động ─────────────────────────────────
    const phoneInput = document.getElementById('soDienThoai');
    if (phoneInput) {
        phoneInput.addEventListener('input', function () {
            this.value = this.value.replace(/[^0-9]/g, '').substring(0, 11);
        });
    }

});
