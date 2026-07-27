(function (window, $) {
    'use strict';

    function escapeHtml(text) {
        return String(text || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function initHomePage() {
        if (!$('#objectsList').length) return;

        var allObjects = [];

        function loadDatabaseObjects() {
            $('#loadingSpinner').show();
            $('#objectsList').empty();
            $('#statsBar').hide();

            $.ajax({
                type: 'GET',
                url: '/Home/GetDatabaseObjects'
            }).done(function (data) {
                allObjects = data || [];
                var tableCount = allObjects.filter(function (o) { return o.type === 'Table'; }).length;
                var viewCount = allObjects.filter(function (o) { return o.type === 'View'; }).length;
                $('#tableCount').text(tableCount);
                $('#viewCount').text(viewCount);
                $('#statsBar').show();
                displayObjects(allObjects);
            }).fail(function (_xhr, _status, error) {
                if (window.AppToast) AppToast.show('خطا در دریافت اطلاعات: ' + error, 'error');
                else alert('خطا در دریافت اطلاعات: ' + error);
            }).always(function () {
                $('#loadingSpinner').hide();
            });
        }

        function groupBySchema(objects) {
            return objects.reduce(function (groups, obj) {
                var schema = obj.schema || 'dbo';
                if (!groups[schema]) groups[schema] = [];
                groups[schema].push(obj);
                return groups;
            }, {});
        }

        function appendObjectItem(obj) {
            var iconClass = obj.type === 'Table' ? 'type-table' : 'type-view';
            var icon = obj.type === 'Table' ? 'fa-table' : 'fa-eye';
            var item = $(
                '<div class="table-item" data-name="' + escapeHtml(obj.name) + '" data-schema="' + escapeHtml(obj.schema) + '" data-type="' + escapeHtml(obj.type) + '">' +
                '<div class="table-item-content">' +
                '<div class="table-item-name">' +
                '<div class="table-icon ' + iconClass + '"><i class="fas ' + icon + '"></i></div>' +
                '<div><div></div><span class="table-item-schema"></span></div>' +
                '</div>' +
                '<span class="badge-' + String(obj.type || '').toLowerCase() + '"></span>' +
                '</div></div>'
            );
            item.find('.table-item-name div:last > div').text(obj.name);
            item.find('.table-item-schema').text(obj.schema);
            item.find('[class^="badge-"]').text(obj.type);

            item.on('click', function () {
                $('.table-item').removeClass('selected');
                $(this).addClass('selected');
                generateClass(obj.name, obj.type, obj.schema);
            });

            $('#objectsList').append(item);
        }

        function displayObjects(objects) {
            $('#objectsList').empty();
            if (!objects.length) {
                $('#objectsList').html('<div class="text-center text-muted p-3">موردی یافت نشد</div>');
                return;
            }

            var tables = objects.filter(function (o) { return o.type === 'Table'; });
            var views = objects.filter(function (o) { return o.type === 'View'; });

            if (tables.length) {
                $('#objectsList').append('<div class="section-title"><i class="fas fa-table me-2"></i>جداول</div>');
                var bySchema = groupBySchema(tables);
                Object.keys(bySchema).sort().forEach(function (schema) {
                    bySchema[schema].forEach(appendObjectItem);
                });
            }

            if (views.length) {
                $('#objectsList').append('<div class="section-title"><i class="fas fa-eye me-2"></i>ویوها</div>');
                var viewsBySchema = groupBySchema(views);
                Object.keys(viewsBySchema).sort().forEach(function (schema) {
                    viewsBySchema[schema].forEach(appendObjectItem);
                });
            }
        }

        function generateClass(tableName, objectType, schema) {
            $('#codeContainer').html('<div class="text-center p-5"><div class="spinner-border text-primary"></div></div>');
            $.ajax({
                type: 'POST',
                url: '/Home/GenerateClass',
                contentType: 'application/json',
                data: JSON.stringify({ tableName: tableName, objectType: objectType, schema: schema })
            }).done(function (data) {
                var fullName = schema ? schema + '.' + tableName : tableName;
                displayCode(data.code, fullName);
            }).fail(function (_xhr, _status, error) {
                $('#codeContainer').html('<div class="alert alert-danger m-3">خطا در تولید کلاس: ' + escapeHtml(error) + '</div>');
            });
        }

        function displayCode(code, tableName) {
            var html =
                '<div class="code-header">' +
                '<div class="code-filename"><i class="fas fa-file-code"></i><span></span></div>' +
                '<button class="btn-copy" type="button"><i class="fas fa-copy me-1"></i>کپی کد</button>' +
                '</div>' +
                '<pre id="codeOutput" class="p-3 mb-0"><code></code></pre>';
            var $wrap = $(html);
            $wrap.find('.code-filename span').text(tableName + '.cs');
            $wrap.find('code').text(code);
            $wrap.find('.btn-copy').on('click', function () {
                var text = $('#codeOutput').text();
                navigator.clipboard.writeText(text).then(function () {
                    var $btn = $('.btn-copy');
                    var original = $btn.html();
                    $btn.html('<i class="fas fa-check me-1"></i>کپی شد!');
                    setTimeout(function () { $btn.html(original); }, 2000);
                });
            });
            $('#codeContainer').empty().append($wrap);
        }

        $('#searchBox').off('keyup.homePage').on('keyup.homePage', function () {
            var searchTerm = ($(this).val() || '').toLowerCase().trim();
            var filtered = allObjects.filter(function (obj) {
                var tableName = (obj.name || '').toLowerCase();
                var schema = (obj.schema || '').toLowerCase();
                var fullName = schema + '.' + tableName;
                return tableName.indexOf(searchTerm) !== -1 ||
                    schema.indexOf(searchTerm) !== -1 ||
                    fullName.indexOf(searchTerm) !== -1;
            });
            displayObjects(filtered);
        });

        loadDatabaseObjects();
    }

    $(document).on('page:loaded', initHomePage);
})(window, jQuery);
