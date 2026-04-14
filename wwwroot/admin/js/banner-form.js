/**
 * banner-form.js
 * Dùng chung cho Create.cshtml và Edit.cshtml
 *
 * Tính năng:
 *  1. Dropzone click / drag-drop + file validation
 *  2. Live preview ảnh (file upload ưu tiên hơn URL text)
 *  3. Chuẩn hóa LinkUrl → Relative Path (client-side preview; server cũng normalize)
 *  4. Sync live preview metadata (tên, loại, trạng thái)
 */
(function () {
    'use strict';

    // ── DOM refs ──────────────────────────────────────────────────────
    const dropZone = document.getElementById('imageDropZone');
    const fileInput = document.getElementById('imageFile');
    const uploadWrap = document.getElementById('uploadPreviewWrap');
    const uploadImg = document.getElementById('uploadPreviewImg');
    const removeBtn = document.getElementById('removeUpload');
    const imageUrlInput = document.getElementById('imageUrlInput');
    const linkUrlInput = document.getElementById('linkUrlInput');
    const linkPreview = document.getElementById('linkPreview');
    const linkPreviewValue = document.getElementById('linkPreviewValue');

    // Preview pane refs
    const previewFinalImg = document.getElementById('previewFinalImg');
    const previewEmpty = document.getElementById('previewEmpty');
    const pvName = document.getElementById('pvName');
    const pvType = document.getElementById('pvType');
    const pvLink = document.getElementById('pvLink');
    const pvStatus = document.getElementById('pvStatus');

    // Form fields
    const titleInput = document.querySelector('[name="Title"]');
    const typeSelect = document.querySelector('[name="BannerType"]');
    const isActiveChk = document.querySelector('[name="IsActive"]');

    // ── 1. DROPZONE: click to open file picker ─────────────────────
    if (dropZone && fileInput) {
        dropZone.addEventListener('click', () => fileInput.click());

        dropZone.addEventListener('dragover', (e) => {
            e.preventDefault();
            dropZone.classList.add('dragover');
        });

        dropZone.addEventListener('dragleave', () => {
            dropZone.classList.remove('dragover');
        });

        dropZone.addEventListener('drop', (e) => {
            e.preventDefault();
            dropZone.classList.remove('dragover');
            const files = e.dataTransfer?.files;
            if (files?.length) {
                fileInput.files = files;
                handleFileSelect(files[0]);
            }
        });

        fileInput.addEventListener('change', () => {
            if (fileInput.files?.length) {
                handleFileSelect(fileInput.files[0]);
            }
        });
    }

    // ── 2. Handle file select: validate + preview ──────────────────
    function handleFileSelect(file) {
        const allowedTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
        const maxSize = 5 * 1024 * 1024; // 5 MB

        if (!allowedTypes.includes(file.type)) {
            alert('Định dạng không hợp lệ. Chỉ chấp nhận JPG, PNG, WebP, GIF.');
            clearFileInput();
            return;
        }

        if (file.size > maxSize) {
            alert('Kích thước ảnh vượt quá 5 MB. Vui lòng chọn ảnh nhỏ hơn.');
            clearFileInput();
            return;
        }

        const reader = new FileReader();
        reader.onload = (e) => {
            const dataUrl = e.target.result;
            // Show upload preview
            if (uploadImg && uploadWrap) {
                uploadImg.src = dataUrl;
                uploadWrap.classList.remove('d-none');
            }
            // Update final preview pane
            showPreviewImg(dataUrl);
        };
        reader.readAsDataURL(file);
    }

    // ── 3. Remove uploaded file ────────────────────────────────────
    if (removeBtn) {
        removeBtn.addEventListener('click', () => {
            clearFileInput();
            // Fallback to URL field if it has a value
            const urlVal = imageUrlInput?.value?.trim();
            if (urlVal) {
                showPreviewImg(urlVal);
            } else {
                hidePreviewImg();
            }
        });
    }

    function clearFileInput() {
        if (fileInput) {
            fileInput.value = '';
        }
        if (uploadWrap) uploadWrap.classList.add('d-none');
        if (uploadImg) uploadImg.src = '';
    }

    // ── 4. URL text input → live preview ──────────────────────────
    if (imageUrlInput) {
        imageUrlInput.addEventListener('input', () => {
            // Only update preview from URL if no file is selected
            if (!fileInput?.files?.length) {
                const urlVal = imageUrlInput.value.trim();
                if (urlVal) {
                    showPreviewImg(urlVal);
                } else {
                    hidePreviewImg();
                }
            }
        });
    }

    // ── 5. LinkUrl: chuẩn hóa thành Relative Path (client preview) ─
    if (linkUrlInput) {
        linkUrlInput.addEventListener('input', handleLinkNormalization);
        linkUrlInput.addEventListener('blur', handleLinkNormalization);

        // Run once on page load to show existing value
        if (linkUrlInput.value) handleLinkNormalization();
    }

    function handleLinkNormalization() {
        const raw = linkUrlInput.value.trim();
        if (!raw) {
            hideLinkPreview();
            updatePvLink('—');
            return;
        }

        const normalized = normalizeToRelativePath(raw);

        // Show preview if changed
        if (normalized !== raw) {
            if (linkPreview && linkPreviewValue) {
                linkPreviewValue.textContent = normalized;
                linkPreview.style.display = 'block';
            }
        } else {
            hideLinkPreview();
        }

        updatePvLink(normalized);
    }

    /**
     * Chuẩn hóa URL → Relative Path
     * "https://localhost:7215/Category/Index/1" → "/Category/Index/1"
     * "/Product/Details/5"                       → "/Product/Details/5"
     */
    function normalizeToRelativePath(raw) {
        raw = raw.trim();
        if (!raw) return '';
        if (raw.startsWith('/')) return raw;

        try {
            const url = new URL(raw);
            return url.pathname + url.search + url.hash;
        } catch {
            // Not a valid URL — return as-is
            return raw;
        }
    }

    function hideLinkPreview() {
        if (linkPreview) linkPreview.style.display = 'none';
    }

    // ── 6. Live metadata preview ───────────────────────────────────
    if (titleInput) {
        titleInput.addEventListener('input', () => {
            if (pvName) pvName.textContent = titleInput.value || '—';
        });
    }

    if (typeSelect) {
        typeSelect.addEventListener('change', () => {
            if (pvType) pvType.textContent = typeSelect.options[typeSelect.selectedIndex]?.text || '—';
        });
    }

    if (isActiveChk && pvStatus) {
        isActiveChk.addEventListener('change', () => {
            pvStatus.textContent = isActiveChk.checked ? 'Kích hoạt' : 'Vô hiệu';
            pvStatus.className = 'preview-meta-val ' +
                (isActiveChk.checked ? 'preview-status-active' : 'preview-status-inactive');
        });
    }

    function updatePvLink(val) {
        if (pvLink) {
            pvLink.textContent = val || '—';
            pvLink.title = val || '';
        }
    }

    // ── 7. Preview image helpers ───────────────────────────────────
    function showPreviewImg(src) {
        if (!previewFinalImg) return;
        previewFinalImg.src = src;
        previewFinalImg.classList.remove('d-none');
        if (previewEmpty) previewEmpty.classList.add('d-none');
    }

    function hidePreviewImg() {
        if (!previewFinalImg) return;
        previewFinalImg.src = '';
        previewFinalImg.classList.add('d-none');
        if (previewEmpty) previewEmpty.classList.remove('d-none');
    }

    // Handle broken image URL gracefully
    if (previewFinalImg) {
        previewFinalImg.addEventListener('error', () => {
            if (!fileInput?.files?.length) {
                // URL typed was invalid
                previewFinalImg.classList.add('d-none');
                if (previewEmpty) previewEmpty.classList.remove('d-none');
            }
        });
    }

})();