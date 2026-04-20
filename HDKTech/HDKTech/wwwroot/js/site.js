// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// So sánh sản phẩm (tối đa 3)
const compareList = [];
window.addToCompare = function (productId, productName) {
    if (compareList.includes(productId)) {
        Swal.fire({ icon: 'info', title: 'Đã có trong danh sách so sánh', timer: 1500 });
        return;
    }
    if (compareList.length >= 3) {
        Swal.fire({ icon: 'warning', title: 'Chỉ so sánh tối đa 3 sản phẩm' });
        return;
    }
    compareList.push(productId);
    Swal.fire({ icon: 'success', title: `Đã thêm "${productName}" vào so sánh`, timer: 1200 });
};