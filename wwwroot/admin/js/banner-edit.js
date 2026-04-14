/* ========== Banner Edit - Image Upload Handler ========== */

document.addEventListener('DOMContentLoaded', function () {
    const imageDropZone = document.getElementById('imageDropZone');
    const imageFile = document.getElementById('imageFile');
    const previewImage = document.getElementById('previewImage');
    const form = document.querySelector('#bannerEditForm');
    const imageUrlInput = document.querySelector('input[name="ImageUrl"]');

    if (!imageDropZone || !imageFile) return;

    // ========== Handle Drag & Drop ==========
    imageDropZone.addEventListener('click', () => imageFile.click());

    imageDropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        e.stopPropagation();
        imageDropZone.classList.add('active');
    });

    imageDropZone.addEventListener('dragleave', (e) => {
        e.preventDefault();
        e.stopPropagation();
        imageDropZone.classList.remove('active');
    });

    imageDropZone.addEventListener('drop', (e) => {
        e.preventDefault();
        e.stopPropagation();
        imageDropZone.classList.remove('active');

        const files = e.dataTransfer.files;
        if (files && files.length > 0) {
            imageFile.files = files;
            handleImageSelect();
        }
    });

    // ========== Handle File Input Change ==========
    imageFile.addEventListener('change', handleImageSelect);

    function handleImageSelect() {
        const file = imageFile.files[0];
        if (file) {
            // Validate file type
            const allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];
            if (!allowedTypes.includes(file.type)) {
                alert('Vui lòng chọn file ảnh hợp lệ (PNG, JPG, WebP)');
                imageFile.value = '';
                return;
            }

            // Validate file size (5MB)
            if (file.size > 5 * 1024 * 1024) {
                alert('Kích thước ảnh không vượt quá 5MB');
                imageFile.value = '';
                return;
            }

            // Read and preview image
            const reader = new FileReader();
            reader.onload = (e) => {
                previewImage.src = e.target.result;
            };
            reader.readAsDataURL(file);

            // ✅ FIX: Clear ImageUrl input when file is selected
            // Priority: File upload > URL input
            if (imageUrlInput) {
                imageUrlInput.value = '';
            }
        }
    }

    // ========== Live Preview Updates ==========
    const titleInput = document.querySelector('input[name="Title"]');
    if (titleInput) {
        titleInput.addEventListener('input', (e) => {
            document.getElementById('previewName').textContent = e.target.value;
        });
    }

    const bannerTypeSelect = document.querySelector('select[name="BannerType"]');
    if (bannerTypeSelect) {
        bannerTypeSelect.addEventListener('change', (e) => {
            const typeMap = {
                'Main': 'Trang Chủ (Main)',
                'Side': 'Sidebar',
                'Bottom': 'Footer (Bottom)',
                '': 'Chưa chọn'
            };
            document.getElementById('previewType').textContent = typeMap[e.target.value] || 'Chưa chọn';
        });
    }

    const displayOrderInput = document.querySelector('input[name="DisplayOrder"]');
    if (displayOrderInput) {
        displayOrderInput.addEventListener('input', (e) => {
            document.getElementById('previewOrder').textContent = e.target.value || '0';
        });
    }

    // ========== Form Submit - Ensure File is Sent ==========
    if (form) {
        form.addEventListener('submit', function (e) {
            // No need to prevent default - form will submit normally with multipart/form-data
            // The file is already in the input element, it will be sent automatically
            console.log('Form submitted with file:', imageFile.files[0] ? imageFile.files[0].name : 'No file');
        });
    }
});
