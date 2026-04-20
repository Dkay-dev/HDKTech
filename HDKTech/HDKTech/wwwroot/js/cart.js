/**
 * Cart Operations - AJAX Add to Cart
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