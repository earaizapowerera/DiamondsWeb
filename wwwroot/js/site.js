// Diamonds Web - Site JavaScript

// Format currency
function formatMXN(amount) {
    return new Intl.NumberFormat('es-MX', {
        style: 'currency',
        currency: 'MXN',
        minimumFractionDigits: 2
    }).format(amount);
}

// UTC date conversion
function convertUtcDatesToLocal() {
    document.querySelectorAll('.utc-date').forEach(function(el) {
        var utc = el.getAttribute('data-utc');
        if (utc) {
            var d = new Date(utc);
            el.textContent = d.toLocaleDateString('es-MX', {
                year: 'numeric', month: '2-digit', day: '2-digit'
            });
        }
    });
}

document.addEventListener('DOMContentLoaded', function() {
    convertUtcDatesToLocal();
});
