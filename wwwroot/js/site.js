// Diamonds Web - Site JavaScript

// Format currency
function formatMXN(amount) {
    return new Intl.NumberFormat('es-MX', {
        style: 'currency',
        currency: 'MXN',
        minimumFractionDigits: 2
    }).format(amount);
}

// UTC date conversion — shows date + time in local timezone
function convertUtcDatesToLocal() {
    document.querySelectorAll('.utc-date').forEach(function(el) {
        var utc = el.getAttribute('data-utc');
        if (utc) {
            var d = new Date(utc);
            var dateStr = d.toLocaleDateString('es-MX', {
                year: 'numeric', month: '2-digit', day: '2-digit'
            });
            var timeStr = d.toLocaleTimeString('es-MX', {
                hour: '2-digit', minute: '2-digit', hour12: false
            });
            el.textContent = dateStr + ' ' + timeStr;
        }
    });
}

document.addEventListener('DOMContentLoaded', function() {
    convertUtcDatesToLocal();
});
