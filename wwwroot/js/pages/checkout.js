/* ── Checkout Page — Address Picker + Shipping Fee + Payment UI ───── */
$(document).ready(function () {
    var form       = document.getElementById('checkoutForm');
    var baseTotal  = parseFloat(form.dataset.total)    || 0;
    var currentFee = parseFloat(form.dataset.shipping) || 0;

    // ── Helpers ──────────────────────────────────────────────────────
    function formatVND(amount) {
        return amount.toLocaleString('vi-VN') + '₫';
    }

    function updateSummaryPanel(fee, feeFormatted) {
        currentFee = fee;
        $('#hiddenShipping').val(fee);
        $('#summary-shipping-fee').text(feeFormatted === 'Miễn phí' ? '🎉 Miễn phí' : feeFormatted);
        $('#summary-total-amount').text(formatVND(baseTotal + fee));
    }

    function showFeeResult(fee, feeFormatted, zone) {
        var noteMap = { A: '(Giao hàng nội thành Đà Nẵng)', B: '(Miền Trung)', C: '(Miền Nam)', D: '(Miền Bắc)' };
        var el = $('#shipping-fee-result');
        el.removeClass('zone-a zone-b zone-c zone-d').addClass('zone-' + zone.toLowerCase());
        $('#fee-value').text(feeFormatted === 'Miễn phí' ? '🎉 Miễn phí' : feeFormatted);
        $('#fee-note').text(noteMap[zone] || '');
        el.fadeIn(250);
    }

    function autoFillAddress() {
        var parts    = [];
        var ward     = $('#phuong option:selected').text();
        var district = $('#quan option:selected').text();
        var city     = $('#tinh option:selected').text();

        if (ward     && ward     !== '📍 Phường / Xã')       parts.push(ward);
        if (district && district !== '🏘️ Quận / Huyện')     parts.push(district);
        if (city     && city     !== '🏙️ Tỉnh / Thành phố') parts.push(city);

        if (parts.length > 0) {
            var current = $('#shippingAddress').val().trim();
            if (!current || $('#shippingAddress').data('auto') === true) {
                $('#shippingAddress').val(parts.join(', '));
                $('#shippingAddress').data('auto', true);
            }
        }
    }

    // ── Gọi API tính phí ship ────────────────────────────────────────
    function fetchShippingFee(cityId, cityName, districtId, districtName, wardId, wardName) {
        if (!cityName || cityName === '🏙️ Tỉnh / Thành phố') return;

        $.ajax({
            url: '/Shipping/CalculateFee',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                CityId: cityId, CityName: cityName,
                DistrictId: districtId || '', DistrictName: districtName || '',
                WardId: wardId || '', WardName: wardName || ''
            }),
            success: function (res) {
                if (res.success) {
                    updateSummaryPanel(res.fee, res.feeFormatted);
                    showFeeResult(res.fee, res.feeFormatted, res.zone);
                    $('#hiddenCityName').val(cityName);
                    $('#hiddenDistrictName').val(districtName || '');
                    $('#hiddenWardName').val(wardName || '');
                }
            }
        });
    }

    // ── Esgoo: Tải Tỉnh Thành ───────────────────────────────────────
    $.getJSON('https://esgoo.net/api-tinhthanh/1/0.htm', function (data) {
        if (data.error === 0) {
            $.each(data.data, function (k, v) {
                $('#tinh').append('<option value="' + v.id + '" data-name="' + v.full_name + '">' + v.full_name + '</option>');
            });
        }
    });

    // ── Khi chọn Tỉnh Thành ─────────────────────────────────────────
    $('#tinh').on('change', function () {
        var idTinh   = $(this).val();
        var nameTinh = $('option:selected', this).data('name') || $('option:selected', this).text();

        $('#quan').html('<option value="0">🏘️ Quận / Huyện</option>').prop('disabled', true);
        $('#phuong').html('<option value="0">📍 Phường / Xã</option>').prop('disabled', true);
        $('#shipping-fee-result').fadeOut(150);

        if (idTinh === '0') {
            updateSummaryPanel(0, '—');
            $('#summary-shipping-fee').text('Chưa chọn địa chỉ');
            $('#summary-total-amount').text(formatVND(baseTotal));
            return;
        }

        fetchShippingFee(idTinh, nameTinh, '', '', '', '');
        autoFillAddress();

        $.getJSON('https://esgoo.net/api-tinhthanh/2/' + idTinh + '.htm', function (data) {
            if (data.error === 0) {
                $.each(data.data, function (k, v) {
                    $('#quan').append('<option value="' + v.id + '" data-name="' + v.full_name + '">' + v.full_name + '</option>');
                });
                $('#quan').prop('disabled', false);
            }
        });
    });

    // ── Khi chọn Quận/Huyện ─────────────────────────────────────────
    $('#quan').on('change', function () {
        var idQuan   = $(this).val();
        var nameQuan = $('option:selected', this).data('name') || $('option:selected', this).text();
        var nameTinh = $('option:selected', $('#tinh')).data('name') || $('option:selected', $('#tinh')).text();
        var idTinh   = $('#tinh').val();

        $('#phuong').html('<option value="0">📍 Phường / Xã</option>').prop('disabled', true);
        if (idQuan === '0') return;

        fetchShippingFee(idTinh, nameTinh, idQuan, nameQuan, '', '');
        autoFillAddress();

        $.getJSON('https://esgoo.net/api-tinhthanh/3/' + idQuan + '.htm', function (data) {
            if (data.error === 0) {
                $.each(data.data, function (k, v) {
                    $('#phuong').append('<option value="' + v.id + '" data-name="' + v.full_name + '">' + v.full_name + '</option>');
                });
                $('#phuong').prop('disabled', false);
            }
        });
    });

    // ── Khi chọn Phường/Xã ──────────────────────────────────────────
    $('#phuong').on('change', function () {
        if ($(this).val() === '0') return;

        var namePhuong = $('option:selected', this).data('name') || $('option:selected', this).text();
        var nameQuan   = $('option:selected', $('#quan')).data('name')  || $('option:selected', $('#quan')).text();
        var nameTinh   = $('option:selected', $('#tinh')).data('name')  || $('option:selected', $('#tinh')).text();

        fetchShippingFee($('#tinh').val(), nameTinh, $('#quan').val(), nameQuan, $(this).val(), namePhuong);
        autoFillAddress();
        $('#hiddenWardName').val(namePhuong);
    });

    $('#shippingAddress').on('input', function () {
        $(this).data('auto', false);
    });

    // ── Payment method UI ────────────────────────────────────────────
    $('input[name="PaymentMethod"]').on('change', function () {
        var btn  = $('#btnSubmit');
        var icon = $('#btnIcon');
        var text = $('#btnText');

        $('.payment-method').removeClass('active');
        $(this).closest('.payment-method').addClass('active');

        if (this.value === 'VNPAY') {
            btn.css('background', '#005BAA');
            icon.attr('class', 'bi bi-credit-card-2-front');
            text.text('THANH TOÁN QUA VNPAY');
        } else if (this.value === 'Momo') {
            btn.css('background', '#A72930');
            icon.attr('class', 'bi bi-wallet2');
            text.text('THANH TOÁN QUA MOMO');
        } else {
            btn.css('background', '');
            icon.attr('class', 'bi bi-check-circle');
            text.text('XÁC NHẬN ĐẶT HÀNG');
        }
    });

    // ── Loading state khi submit ─────────────────────────────────────
    $('#checkoutForm').on('submit', function () {
        $('#btnSubmit').prop('disabled', true);
        $('#btnIcon').attr('class', 'bi bi-hourglass-split');
        $('#btnText').text('ĐANG XỬ LÝ...');
    });
});
