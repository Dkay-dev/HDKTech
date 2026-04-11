/**
 * HDKTech Admin Utilities
 * Common functions for admin area
 */

// Confirm delete action
function confirmDelete(message = 'Bạn chắc chắn muốn xóa mục này?') {
    return confirm(message);
}

// Format currency (VNĐ)
function formatCurrency(value) {
    return parseInt(value).toLocaleString('vi-VN');
}

// Format percentage
function formatPercentage(value) {
    return value + '%';
}

// Toggle loading state on button
function toggleButtonLoading(button, isLoading = true) {
    if (isLoading) {
        button.disabled = true;
        button.classList.add('opacity-50', 'cursor-not-allowed');
        button.setAttribute('data-loading', 'true');
    } else {
        button.disabled = false;
        button.classList.remove('opacity-50', 'cursor-not-allowed');
        button.removeAttribute('data-loading');
    }
}

// Show notification
function showNotification(message, type = 'success', duration = 3000) {
    const notificationId = 'notification-' + Date.now();
    const bgColor = type === 'success' ? 'bg-green-100' : type === 'error' ? 'bg-red-100' : 'bg-blue-100';
    const textColor = type === 'success' ? 'text-green-700' : type === 'error' ? 'text-red-700' : 'text-blue-700';
    const icon = type === 'success' ? 'check_circle' : type === 'error' ? 'error' : 'info';

    const notification = document.createElement('div');
    notification.id = notificationId;
    notification.className = `fixed top-6 right-6 ${bgColor} ${textColor} px-6 py-4 rounded-lg shadow-lg flex items-center gap-3 animate-slideIn z-50`;
    notification.innerHTML = `
        <span class="material-symbols-outlined">${icon}</span>
        <span>${message}</span>
    `;

    document.body.appendChild(notification);

    setTimeout(() => {
        notification.classList.add('animate-slideOut');
        setTimeout(() => notification.remove(), 300);
    }, duration);
}

// Make API call with CSRF token
async function apiCall(url, method = 'POST', data = null) {
    const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    
    const options = {
        method: method,
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': csrfToken
        }
    };

    if (data) {
        options.body = JSON.stringify(data);
    }

    return fetch(url, options);
}

// Debounce function
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Add CSS animation rules if not present
if (!document.getElementById('admin-animations')) {
    const style = document.createElement('style');
    style.id = 'admin-animations';
    style.textContent = `
        @keyframes slideIn {
            from {
                transform: translateX(400px);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        @keyframes slideOut {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(400px);
                opacity: 0;
            }
        }
        .animate-slideIn {
            animation: slideIn 0.3s ease-out;
        }
        .animate-slideOut {
            animation: slideOut 0.3s ease-out;
        }
    `;
    document.head.appendChild(style);
}
