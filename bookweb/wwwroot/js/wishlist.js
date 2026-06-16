$(document).ready(function () {
    $('.wishlist-btn').on('click', function (e) {
        e.preventDefault();
        var btn = $(this);
        var productId = btn.data('product-id');

        $.ajax({
            url: '/Customer/Home/ToggleWishlist',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ productId: productId }),
            success: function (data) {
                if (data.success) {
                    var icon = btn.find('i');
                    var card = btn.closest('.book-card');
                    if (data.added) {
                        icon.removeClass('bi-heart text-muted').addClass('bi-heart-fill text-danger');
                        // Add "Want to Read" badge if not present
                        if (card.find('.badge.bg-warning, .badge.bg-primary, .badge.bg-success').length === 0) {
                            card.prepend('<span class="position-absolute top-0 start-0 m-2 badge bg-warning text-dark" style="z-index: 2; font-size: 0.6rem;">Want to Read</span>');
                        }
                    } else {
                        icon.removeClass('bi-heart-fill text-danger').addClass('bi-heart text-muted');
                        // Remove status badge
                        card.find('.badge.bg-warning, .badge.bg-primary, .badge.bg-success').filter(function() {
                            return $(this).closest('.card-body').length === 0;
                        }).remove();
                    }
                    toastr.success(data.message);
                }
            },
            error: function (xhr) {
                if (xhr.status === 401) {
                    window.location.href = '/Identity/Account/Login';
                } else {
                    toastr.error('Something went wrong.');
                }
            }
        });
    });
});
