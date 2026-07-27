(function (window, $) {
    'use strict';

    function filterJobLogsTable() {
        var $table = $('#jobLogsTable');
        if (!$table.length) return;

        var generalSearchTerm = ($('#generalSearch').val() || '').toLowerCase().trim();
        var jobNameValue = $('#jobNameFilter').val() || '';
        var isActiveValue = $('#isActiveFilter').val() || '';
        var scheduleTypeValue = $('#scheduleTypeFilter').val() || '';
        var statusValue = $('#statusFilter').val() || '';

        var $rows = $table.find('tbody tr').filter(function () {
            return $(this).data('job-name') !== undefined;
        });

        $table.find('tbody tr.no-results').remove();
        var visibleCount = 0;

        $rows.each(function () {
            var $row = $(this);
            var rowText = $row.text().toLowerCase();
            var jobName = $row.data('job-name') || '';
            var isActive = String($row.data('is-active') || '');
            var scheduleType = String($row.data('schedule-type') || '');
            var status = String($row.data('status') || '');

            var match =
                (generalSearchTerm === '' || rowText.indexOf(generalSearchTerm) !== -1) &&
                (jobNameValue === '' || jobName === jobNameValue) &&
                (isActiveValue === '' || isActive === isActiveValue.toLowerCase()) &&
                (scheduleTypeValue === '' || scheduleType === scheduleTypeValue) &&
                (statusValue === '' || status === statusValue);

            $row.toggle(match);
            if (match) visibleCount++;
        });

        if (visibleCount === 0 && $rows.length > 0) {
            $table.find('tbody').append('<tr class="no-results"><td colspan="7" class="text-center text-muted">نتیجه‌ای یافت نشد</td></tr>');
        }

        $('#resultCount').text('نمایش ' + visibleCount + ' از ' + $rows.length + ' رکورد');
    }

    function initJobLogsIndex() {
        if (!$('#jobLogsTable').length) return;
        filterJobLogsTable();
    }

    function initDetailsPage() {
        var $stream = $('#liveLogStream');
        if (!$stream.length) return;
        $stream.data('follow', true);
        $stream.scrollTop($stream[0].scrollHeight);
    }

    function renderQuickLogs(data, historyId, isRunning) {
        var content = '<div id="logModalContent" data-live-history="' + (isRunning ? historyId : '') + '">' +
            '<div class="d-flex justify-content-between align-items-center mb-2">' +
            '<div class="btn-group btn-group-sm" role="group">' +
            '<button type="button" class="btn btn-outline-secondary btn-filter-level active" data-level="all">همه</button>' +
            '<button type="button" class="btn btn-outline-info btn-filter-level" data-level="info">Info</button>' +
            '<button type="button" class="btn btn-outline-warning btn-filter-level" data-level="warning">Warning</button>' +
            '<button type="button" class="btn btn-outline-danger btn-filter-level" data-level="error">Error</button>' +
            '</div>' +
            (isRunning ? '<span class="badge bg-warning text-dark status-running"><span class="status-pulse"></span> زنده</span>' : '') +
            '</div>' +
            '<div class="live-log-stream" id="quickLogStream" data-follow="true">';

        if (data && data.length) {
            data.forEach(function (log) {
                var level = (log.logLevel || 'Info').toLowerCase();
                content += '<div class="log-line log-level-' + level + '" data-level="' + level + '">' +
                    '<span class="log-time" dir="ltr">' + (window.JobRealtime ? window.JobRealtime.formatDate(log.timestamp) : log.timestamp) + '</span>' +
                    '<span class="log-level">[' + (log.logLevel || 'Info') + ']</span>' +
                    '<span class="log-msg">' + $('<div>').text(log.message || '').html() + '</span>' +
                    '</div>';
            });
        } else {
            content += '<p class="text-center text-muted mb-0">لاگی یافت نشد</p>';
        }

        content += '</div></div>';
        return content;
    }

    window.loadDetailedLogs = function (historyId, isRunning) {
        isRunning = !!isRunning;
        $.ajax({
            url: '/JobLogs/GetDetailedLogs',
            type: 'GET',
            data: { historyId: historyId }
        }).done(function (data) {
            $('#logModalBody').html(renderQuickLogs(data, historyId, isRunning));
            var modalEl = document.getElementById('logModal');
            if (modalEl) {
                bootstrap.Modal.getOrCreateInstance(modalEl).show();
            }
            if (isRunning && window.JobRealtime) {
                window.JobRealtime.joinHistory(historyId);
            }
        }).fail(function () {
            $('#logModalBody').html('<p class="text-danger">خطا در دریافت لاگ‌ها</p>');
            var modalEl = document.getElementById('logModal');
            if (modalEl) bootstrap.Modal.getOrCreateInstance(modalEl).show();
        });
    };

    $(document).on('job:logAdded job:logsBatch', function (_e, payload) {
        var entries = Array.isArray(payload) ? payload : [payload];
        var $quick = $('#quickLogStream');
        if (!$quick.length) return;

        var liveId = parseInt($('#logModalContent').attr('data-live-history') || '0', 10);
        entries.forEach(function (log) {
            if (liveId && log.historyId && log.historyId !== liveId) return;
            var level = (log.level || 'Info').toLowerCase();
            var $row = $(
                '<div class="log-line log-level-' + level + '" data-level="' + level + '">' +
                '<span class="log-time" dir="ltr"></span>' +
                '<span class="log-level">[' + (log.level || 'Info') + ']</span>' +
                '<span class="log-msg"></span></div>'
            );
            $row.find('.log-time').text(window.JobRealtime ? window.JobRealtime.formatDate(log.timestamp) : log.timestamp);
            $row.find('.log-msg').text(log.message || '');
            $quick.append($row);
        });

        if ($quick.data('follow') !== false) {
            $quick.scrollTop($quick[0].scrollHeight);
        }
    });

    $(document).on('click', '.btn-filter-level', function () {
        var level = ($(this).data('level') || 'all').toLowerCase();
        $(this).siblings().removeClass('active');
        $(this).addClass('active');
        $('#quickLogStream .log-line, #liveLogStream .log-line').each(function () {
            var lineLevel = ($(this).data('level') || '').toLowerCase();
            $(this).toggle(level === 'all' || lineLevel === level);
        });
    });

    $(document).on('keyup', '#generalSearch', filterJobLogsTable);
    $(document).on('change', '#jobNameFilter, #isActiveFilter, #scheduleTypeFilter, #statusFilter', filterJobLogsTable);
    $(document).on('click', '#clearFilters', function () {
        $('#generalSearch').val('');
        $('#jobNameFilter, #isActiveFilter, #scheduleTypeFilter, #statusFilter').val('');
        filterJobLogsTable();
    });

    $(document).on('page:loaded', function () {
        initJobLogsIndex();
        initDetailsPage();
    });
})(window, jQuery);
