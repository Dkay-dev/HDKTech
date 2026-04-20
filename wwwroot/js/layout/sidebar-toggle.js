/* ── Guest Layout — Mobile Sidebar Toggle ─────────────────────────── */
document.addEventListener('DOMContentLoaded', function () {
    const sidebarToggle  = document.getElementById('sidebarToggle');
    const sidebarClose   = document.getElementById('sidebarClose');
    const adminSidebar   = document.getElementById('adminSidebar');
    const sidebarOverlay = document.getElementById('sidebarOverlay');

    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function (e) {
            e.preventDefault();
            adminSidebar.classList.toggle('active');
            sidebarOverlay.classList.toggle('active');
        });
    }

    if (sidebarClose) {
        sidebarClose.addEventListener('click', function (e) {
            e.preventDefault();
            adminSidebar.classList.remove('active');
            sidebarOverlay.classList.remove('active');
        });
    }

    if (sidebarOverlay) {
        sidebarOverlay.addEventListener('click', function () {
            adminSidebar.classList.remove('active');
            sidebarOverlay.classList.remove('active');
        });
    }

    const sidebarItems = document.querySelectorAll('.hdk-sidebar-item');
    sidebarItems.forEach(function (item) {
        item.addEventListener('click', function () {
            adminSidebar.classList.remove('active');
            sidebarOverlay.classList.remove('active');
        });
    });
});
