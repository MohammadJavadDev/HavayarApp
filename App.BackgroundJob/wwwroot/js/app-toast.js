(function (window, $) {
    'use strict';

    function showToast(message, type) {
        type = type || 'success';
        var bg = {
            success: 'text-bg-success',
            error: 'text-bg-danger',
            warning: 'text-bg-warning',
            info: 'text-bg-info'
        }[type] || 'text-bg-secondary';

        var id = 'toast-' + Date.now();
        var html =
            '<div id="' + id + '" class="toast align-items-center ' + bg + ' border-0" role="alert" aria-live="assertive" aria-atomic="true">' +
            '<div class="d-flex">' +
            '<div class="toast-body">' + $('<div>').text(message || '').html() + '</div>' +
            '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>' +
            '</div></div>';

        var $container = $('#toast-container');
        if (!$container.length) {
            $container = $('<div id="toast-container" class="toast-container position-fixed bottom-0 start-0 p-3" style="z-index:1090;"></div>').appendTo('body');
        }

        var $el = $(html).appendTo($container);
        var toast = bootstrap.Toast.getOrCreateInstance($el[0], { delay: 3500 });
        $el.on('hidden.bs.toast', function () { $el.remove(); });
        toast.show();
    }

    window.AppToast = { show: showToast };
})(window, jQuery);
