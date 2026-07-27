(function (window, $) {
    'use strict';

    var connection = null;
    var joinedStatuses = false;
    var joinedHistoryId = null;
    var pausedForVisibility = false;
    var connecting = false;

    function formatDate(value) {
        if (!value) return '—';
        try {
            if (typeof moment === 'function') {
                return moment(new Date(value)).format('jYYYY/jMM/jDD HH:mm:ss');
            }
            return new Date(value).toLocaleString('fa-IR');
        } catch (e) {
            return String(value);
        }
    }

    function statusBadgeHtml(status) {
        var s = (status || '').toString();
        if (s === 'Running') {
            return '<span class="badge bg-warning text-dark status-badge status-running"><span class="status-pulse"></span>در حال اجرا...</span>';
        }
        if (s === 'Waiting') {
            return '<span class="badge bg-info text-dark status-badge">در صف اجرا</span>';
        }
        if (s === 'Error' || s === 'Failed') {
            return '<span class="badge bg-danger status-badge">خطا</span>';
        }
        if (s === 'NoSchedule') {
            return '<span class="badge bg-dark status-badge">بدون زمان‌بندی</span>';
        }
        return '<span class="badge bg-secondary status-badge">آماده‌باش</span>';
    }

    function updateStatusSummary() {
        var $rows = $('#jobsTableBody tr[id^="row-"]');
        if (!$rows.length) return;

        var running = 0, error = 0, idle = 0, waiting = 0;
        $rows.each(function () {
            var status = ($(this).attr('data-status') || '').toLowerCase();
            if (status === 'running') running++;
            else if (status === 'error' || status === 'failed') error++;
            else if (status === 'waiting') waiting++;
            else idle++;
        });

        $('#summary-running').text(running);
        $('#summary-error').text(error);
        $('#summary-idle').text(idle);
        $('#summary-waiting').text(waiting);
    }

    function applyStatusToRow(payload) {
        var id = payload.scheduleId;
        var $row = $('#row-' + id);
        if (!$row.length) return;

        $row.attr('data-status', payload.status || '');
        $row.find('.js-status-cell').html(statusBadgeHtml(payload.status));
        if (payload.lastRun !== undefined) {
            $row.find('.js-last-run').text(formatDate(payload.lastRun));
        }
        if (payload.nextRun !== undefined) {
            $row.find('.js-next-run').text(formatDate(payload.nextRun));
        }
        updateStatusSummary();
    }

    function appendLogEntries(entries) {
        var $stream = $('#liveLogStream');
        if (!$stream.length || !entries || !entries.length) return;

        var follow = $stream.data('follow') !== false;
        entries.forEach(function (log) {
            var level = (log.level || 'Info').toLowerCase();
            var levelClass = 'log-level-' + level;
            var row =
                '<div class="log-line ' + levelClass + '" data-level="' + level + '">' +
                '<span class="log-time" dir="ltr">' + formatDate(log.timestamp) + '</span>' +
                '<span class="log-level">[' + (log.level || 'Info') + ']</span>' +
                '<span class="log-msg"></span>' +
                '</div>';
            var $row = $(row);
            $row.find('.log-msg').text(log.message || '');
            $stream.append($row);
        });

        applyLogLevelFilter();
        if (follow) {
            $stream.scrollTop($stream[0].scrollHeight);
        }
    }

    function applyLogLevelFilter() {
        var level = ($('#logLevelFilter').val() || 'all').toLowerCase();
        $('#liveLogStream .log-line').each(function () {
            var lineLevel = ($(this).data('level') || '').toLowerCase();
            $(this).toggle(level === 'all' || lineLevel === level);
        });
    }

    function isJobsPage(ctx) {
        return /\/Jobs(\/Index)?\/?$/i.test(ctx.pathname) || $('#jobsTableBody').length > 0;
    }

    function isDetailsPage(ctx) {
        return /\/JobLogs\/Details/i.test(ctx.pathname) || $('#liveLogStream').length > 0;
    }

    function getDetailsHistoryId() {
        var id = $('#liveLogStream').data('history-id');
        return id ? parseInt(id, 10) : null;
    }

    async function ensureConnection() {
        if (typeof signalR === 'undefined') {
            console.warn('SignalR client library not loaded');
            return;
        }
        if (connecting) return;
        if (connection && connection.state === signalR.HubConnectionState.Connected) return;

        connecting = true;
        try {
            if (!connection) {
                connection = new signalR.HubConnectionBuilder()
                    .withUrl('/hubs/jobs')
                    .withAutomaticReconnect([0, 2000, 5000, 10000])
                    .build();

                connection.on('statusChanged', function (payload) {
                    applyStatusToRow(payload);
                    $(document).trigger('job:statusChanged', [payload]);
                });

                connection.on('logAdded', function (payload) {
                    appendLogEntries([payload]);
                    $(document).trigger('job:logAdded', [payload]);
                });

                connection.on('logsBatch', function (payload) {
                    appendLogEntries(payload || []);
                    $(document).trigger('job:logsBatch', [payload]);
                });

                connection.on('historyCompleted', function (payload) {
                    $(document).trigger('job:historyCompleted', [payload]);
                    if (payload && payload.historyId && getDetailsHistoryId() === payload.historyId) {
                        $('#history-running-badge').addClass('d-none');
                        $('#history-end-time').text(formatDate(payload.endTime));
                        var badge = payload.success
                            ? '<span class="badge bg-success">موفق</span>'
                            : '<span class="badge bg-danger">ناموفق</span>';
                        $('#history-status').html(badge);
                    }
                });

                connection.onreconnected(async function () {
                    joinedStatuses = false;
                    joinedHistoryId = null;
                    await syncAfterReconnect();
                });
            }

            if (connection.state === signalR.HubConnectionState.Disconnected) {
                await connection.start();
            }
        } catch (err) {
            console.warn('SignalR connect failed', err);
        } finally {
            connecting = false;
        }
    }

    async function leaveAll() {
        if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
            joinedStatuses = false;
            joinedHistoryId = null;
            return;
        }
        try {
            if (joinedStatuses) {
                await connection.invoke('LeaveStatuses');
                joinedStatuses = false;
            }
            if (joinedHistoryId) {
                await connection.invoke('LeaveHistory', joinedHistoryId);
                joinedHistoryId = null;
            }
        } catch (e) {
            console.warn('SignalR leave failed', e);
        }
    }

    async function syncStatuses() {
        try {
            var data = await $.getJSON('/Jobs/GetStatuses');
            (data || []).forEach(applyStatusToRow);
            updateStatusSummary();
        } catch (e) {
            console.warn('GetStatuses sync failed', e);
        }
    }

    async function syncAfterReconnect() {
        var ctx = { pathname: window.location.pathname };
        if (isJobsPage(ctx)) {
            await joinStatuses();
            await syncStatuses();
        }
        if (isDetailsPage(ctx)) {
            var historyId = getDetailsHistoryId();
            if (historyId) await joinHistory(historyId);
        }
    }

    async function joinStatuses() {
        await ensureConnection();
        if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;
        if (!joinedStatuses) {
            await connection.invoke('JoinStatuses');
            joinedStatuses = true;
        }
    }

    async function joinHistory(historyId) {
        await ensureConnection();
        if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;
        if (joinedHistoryId && joinedHistoryId !== historyId) {
            await connection.invoke('LeaveHistory', joinedHistoryId);
            joinedHistoryId = null;
        }
        if (!joinedHistoryId) {
            await connection.invoke('JoinHistory', historyId);
            joinedHistoryId = historyId;
        }
    }

    async function onPageLoaded(_e, ctx) {
        ctx = ctx || { pathname: window.location.pathname };

        if (document.visibilityState === 'hidden') {
            pausedForVisibility = true;
            await leaveAll();
            return;
        }

        pausedForVisibility = false;
        var needRealtime = isJobsPage(ctx) || isDetailsPage(ctx) || $('#logModalContent[data-live-history]').length > 0;

        if (!needRealtime) {
            await leaveAll();
            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                try { await connection.stop(); } catch (e) { /* ignore */ }
            }
            return;
        }

        if (isJobsPage(ctx)) {
            await joinStatuses();
            await syncStatuses();
            updateStatusSummary();
        } else if (joinedStatuses) {
            try { await connection.invoke('LeaveStatuses'); } catch (e) { /* ignore */ }
            joinedStatuses = false;
        }

        if (isDetailsPage(ctx)) {
            var historyId = getDetailsHistoryId();
            if (historyId) await joinHistory(historyId);
        } else if (joinedHistoryId) {
            try { await connection.invoke('LeaveHistory', joinedHistoryId); } catch (e) { /* ignore */ }
            joinedHistoryId = null;
        }
    }

    document.addEventListener('visibilitychange', async function () {
        if (document.visibilityState === 'hidden') {
            pausedForVisibility = true;
            await leaveAll();
            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                try { await connection.stop(); } catch (e) { /* ignore */ }
            }
            return;
        }

        if (pausedForVisibility) {
            pausedForVisibility = false;
            await onPageLoaded(null, { pathname: window.location.pathname });
        }
    });

    $(document).on('page:loaded', onPageLoaded);

    $(document).on('change', '#logLevelFilter', applyLogLevelFilter);

    $(document).on('click', '#btnToggleFollow', function () {
        var $stream = $('#liveLogStream');
        var follow = $stream.data('follow') !== false;
        follow = !follow;
        $stream.data('follow', follow);
        $(this).text(follow ? 'توقف اسکرول' : 'دنبال کردن');
        if (follow && $stream.length) {
            $stream.scrollTop($stream[0].scrollHeight);
        }
    });

    window.JobRealtime = {
        applyStatusToRow: applyStatusToRow,
        statusBadgeHtml: statusBadgeHtml,
        formatDate: formatDate,
        joinHistory: joinHistory,
        updateStatusSummary: updateStatusSummary
    };
})(window, jQuery);
