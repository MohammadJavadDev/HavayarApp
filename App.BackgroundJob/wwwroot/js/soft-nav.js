(function (window, $) {
    'use strict';

    var SoftNav = {
        contentSelector: '#app-content',
        navigating: false
    };

    function isInternalLink(anchor) {
        if (!anchor || !anchor.href) return false;
        if (anchor.target && anchor.target !== '_self') return false;
        if (anchor.hasAttribute('download')) return false;
        if (anchor.getAttribute('href') === '#') return false;

        var url;
        try {
            url = new URL(anchor.href, window.location.origin);
        } catch (e) {
            return false;
        }

        if (url.origin !== window.location.origin) return false;
        if (url.pathname.indexOf('/Authenticate/') === 0) return false;
        return true;
    }

    function updateActiveNav(url) {
        var path = url.pathname.replace(/\/$/, '') || '/';
        if (path === '/') path = '/Jobs';
        $('.navbar-nav .nav-link').removeClass('active');
        $('.navbar-nav .nav-link').each(function () {
            var href = this.getAttribute('href');
            if (!href) return;
            try {
                var linkPath = new URL(href, window.location.origin).pathname.replace(/\/$/, '') || '/';
                if (linkPath === '/') linkPath = '/Jobs';
                if (path === linkPath || (linkPath !== '/Jobs' && path.indexOf(linkPath) === 0)) {
                    $(this).addClass('active');
                } else if (linkPath === '/Jobs' && (path === '/Jobs' || path.indexOf('/Jobs/') === 0)) {
                    $(this).addClass('active');
                }
            } catch (e) { /* ignore */ }
        });
    }

    function setDocumentTitle(html, fallbackTitle) {
        var $header = $(html).filter('.page-header').add($(html).find('.page-header')).first();
        var title = $header.attr('data-page-title') || fallbackTitle;
        if (title) {
            document.title = title + ' - App.BackgroundJob';
        }
    }

    function triggerPageLoaded(url) {
        $(document).trigger('page:loaded', [{
            url: url.href,
            pathname: url.pathname,
            search: url.search
        }]);
    }

    SoftNav.load = function (url, options) {
        options = options || {};
        if (SoftNav.navigating) return $.Deferred().reject().promise();

        var targetUrl;
        try {
            targetUrl = typeof url === 'string' ? new URL(url, window.location.origin) : url;
        } catch (e) {
            window.location.href = url;
            return $.Deferred().reject().promise();
        }

        SoftNav.navigating = true;
        var $content = $(SoftNav.contentSelector);
        $content.addClass('is-loading');

        return $.ajax({
            url: targetUrl.pathname + targetUrl.search,
            method: 'GET',
            headers: { 'X-Partial': '1' },
            dataType: 'html'
        }).done(function (html) {
            $content.html(html);
            setDocumentTitle(html, options.title);
            updateActiveNav(targetUrl);

            if (options.push !== false) {
                window.history.pushState({ softNav: true }, '', targetUrl.pathname + targetUrl.search + targetUrl.hash);
            }

            triggerPageLoaded(targetUrl);
        }).fail(function (xhr) {
            if (xhr.status === 401 || xhr.status === 403) {
                window.location.href = '/Authenticate/Login';
                return;
            }
            // Fallback to full navigation
            window.location.href = targetUrl.href;
        }).always(function () {
            SoftNav.navigating = false;
            $content.removeClass('is-loading');
        });
    };

    SoftNav.reload = function () {
        return SoftNav.load(window.location.href, { push: false });
    };

    $(document).on('click', 'a.js-soft-nav, #app-content a[href]', function (e) {
        var anchor = this;
        if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey || e.which === 2) return;
        if ($(anchor).closest('[data-soft-nav="false"]').length) return;
        if (!isInternalLink(anchor)) return;

        e.preventDefault();
        SoftNav.load(anchor.href, { title: $(anchor).data('title') });
    });

    $(document).on('submit', '#app-content form[method="get"], #app-content form:not([method])', function (e) {
        var form = this;
        if ($(form).attr('data-soft-nav') === 'false') return;
        if ((form.method || 'get').toLowerCase() !== 'get') return;

        e.preventDefault();
        var action = form.getAttribute('action') || window.location.pathname;
        var url = new URL(action, window.location.origin);
        url.search = $(form).serialize();
        SoftNav.load(url);
    });

    window.addEventListener('popstate', function () {
        SoftNav.load(window.location.href, { push: false });
    });

    $(function () {
        updateActiveNav(window.location);
        triggerPageLoaded(window.location);
    });

    window.SoftNav = SoftNav;
})(window, jQuery);
