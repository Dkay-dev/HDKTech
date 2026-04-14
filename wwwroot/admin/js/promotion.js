/**
 * promotion.js — JS dành riêng cho trang Quản lý Khuyến mãi (Admin)
 * Tách ra file riêng theo yêu cầu kỹ thuật chung
 * Sử dụng SweetAlert2 cho các thông báo xác nhận/thành công/thất bại
 */

/**
 * Xác nhận xoá chiến dịch khuyến mãi bằng SweetAlert2
 * @param {string} tenChienDich - Tên chiến dịch cần xoá
 * @returns {boolean} true nếu người dùng xác nhận, false nếu huỷ
 */
function confirmXoa(tenChienDich) {
    // SweetAlert2 không hỗ trợ chặn form submit đồng bộ bằng return false
    // => Dùng event.preventDefault + Swal.fire async
    // Kỹ thuật: sử dụng confirm() native làm fallback nếu SweetAlert2 chưa load
    if (typeof Swal === 'undefined') {
        return confirm(`Bạn có chắc muốn xoá chiến dịch "${tenChienDich}" không?`);
    }

    // Không dùng await ở đây vì function cần trả về bool đồng bộ
    // => Sử dụng pattern: chặn submit, show Swal, rồi submit lại nếu confirm
    return false; // Chặn submit mặc định, để Swal xử lý
}

/**
 * Xử lý xoá với SweetAlert2 đầy đủ (async pattern)
 * Gọi từ onclick trên nút Xoá với data-form-id
 */
document.addEventListener('DOMContentLoaded', function () {

    // ── Xoá chiến dịch với SweetAlert2 ──────────────────────────────
    document.querySelectorAll('[data-confirm-delete]').forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            var tenChienDich = this.dataset.confirmDelete;
            var form = this.closest('form');

            Swal.fire({
                title: 'Xác nhận xoá',
                html: `Bạn có chắc muốn xoá chiến dịch <strong>"${tenChienDich}"</strong>?<br><small class="text-muted">Hành động này không thể hoàn tác.</small>`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc3545',
                cancelButtonColor: '#6c757d',
                confirmButtonText: '<i class="fas fa-trash me-1"></i>Xoá',
                cancelButtonText: 'Huỷ',
                reverseButtons: true
            }).then(function (result) {
                if (result.isConfirmed) {
                    form.submit();
                }
            });
        });
    });

    // ── Tự động ẩn alert sau 5 giây ─────────────────────────────────
    setTimeout(function () {
        document.querySelectorAll('.alert.alert-success, .alert.alert-danger').forEach(function (el) {
            var bsAlert = bootstrap.Alert.getOrCreateInstance(el);
            if (bsAlert) bsAlert.close();
        });
    }, 5000);

});

/**
 * Export danh sách khuyến mãi ra CSV
 * Đọc từ bảng HTML hiện tại trong DOM
 */
function exportCSV() {
    var rows = [];
    var headers = [];

    // Lấy headers
    document.querySelectorAll('table thead th').forEach(function (th) {
        headers.push('"' + th.innerText.trim() + '"');
    });
    rows.push(headers.join(','));

    // Lấy data rows
    document.querySelectorAll('table tbody tr').forEach(function (tr) {
        var cells = [];
        tr.querySelectorAll('td').forEach(function (td) {
            // Lấy text thuần, bỏ HTML tags
            var text = td.innerText.trim().replace(/"/g, '""');
            cells.push('"' + text + '"');
        });
        if (cells.length > 0) rows.push(cells.join(','));
    });

    // Tạo file và download
    var csvContent = '\uFEFF' + rows.join('\n'); // BOM cho Excel đọc UTF-8
    var blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    var url = URL.createObjectURL(blob);
    var link = document.createElement('a');
    link.href = url;
    link.download = 'khuyenmai_' + new Date().toISOString().slice(0, 10) + '.csv';
    link.click();
    URL.revokeObjectURL(url);
}
