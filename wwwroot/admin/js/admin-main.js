// HDKTech Admin - Main JavaScript Module

document.addEventListener('DOMContentLoaded', function() {
    initializeAdmin();
});

function initializeAdmin() {
    setupTableInteractions();
    setupFormValidation();
    setupSearchFunctionality();
    setupNavigationHighlight();
}

/**
 * Setup table row interactions (hover, selection, etc.)
 */
function setupTableInteractions() {
    const tables = document.querySelectorAll('table');
    
    tables.forEach(table => {
        const rows = table.querySelectorAll('tbody tr');
        
        rows.forEach(row => {
            row.addEventListener('click', function() {
                rows.forEach(r => r.classList.remove('selected'));
                this.classList.add('selected');
            });
        });
    });
}

/**
 * Setup form validation
 */
function setupFormValidation() {
    const forms = document.querySelectorAll('form[data-validate]');
    
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            if (!validateForm(this)) {
                e.preventDefault();
            }
        });
    });
}

function validateForm(form) {
    const inputs = form.querySelectorAll('input[required], select[required], textarea[required]');
    let isValid = true;
    
    inputs.forEach(input => {
        if (!input.value.trim()) {
            input.classList.add('error');
            isValid = false;
        } else {
            input.classList.remove('error');
        }
    });
    
    return isValid;
}

/**
 * Setup search functionality
 */
function setupSearchFunctionality() {
    const searchInput = document.querySelector('header input[type="text"]');
    
    if (searchInput) {
        searchInput.addEventListener('keyup', debounce(function(e) {
            performSearch(e.target.value);
        }, 300));
    }
}

function performSearch(query) {
    // Implementation depends on current page
    const table = document.querySelector('table');
    
    if (table) {
        const rows = table.querySelectorAll('tbody tr');
        const lowercaseQuery = query.toLowerCase();
        
        rows.forEach(row => {
            const text = row.textContent.toLowerCase();
            row.style.display = text.includes(lowercaseQuery) ? '' : 'none';
        });
    }
}

/**
 * Highlight active navigation item
 */
function setupNavigationHighlight() {
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('aside nav a');
    
    navLinks.forEach(link => {
        const href = link.getAttribute('href');
        if (href && currentPath.includes(href)) {
            link.classList.add('active');
        }
    });
}

/**
 * Utility: Debounce function
 */
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

/**
 * Show notification toast
 */
function showNotification(message, type = 'success') {
    const toast = document.createElement('div');
    toast.className = `notification notification-${type}`;
    toast.textContent = message;
    document.body.appendChild(toast);
    
    setTimeout(() => {
        toast.classList.add('show');
    }, 100);
    
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

/**
 * Confirm action dialog
 */
function confirmAction(message) {
    return confirm(message);
}

// Export for use in other scripts
window.AdminUI = {
    showNotification,
    confirmAction,
    validateForm
};
