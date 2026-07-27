(function (window, $) {
    'use strict';

    function convertTimeToTimeSpan(timeString) {
        var parts = (timeString || '00:00').split(':');
        var hours = parseInt(parts[0], 10) || 0;
        var minutes = parseInt(parts[1], 10) || 0;
        return hours + ':' + minutes + ':00';
    }

    function collectScheduleParams($row) {
        var scheduleType = $row.find('.schedule-type').val();
        var intervalSeconds = 0;
        var dailyIntervalDays = 1;
        var weeklyDays = '';
        var dailyTime = null;
        var hourlyMinute = 0;

        switch (scheduleType) {
            case 'interval':
                intervalSeconds = parseInt($row.find('.job-interval').val(), 10) || 0;
                break;
            case 'daily':
                dailyIntervalDays = parseInt($row.find('.job-daily-interval').val(), 10) || 1;
                dailyTime = convertTimeToTimeSpan($row.find('.job-daily-time').val());
                intervalSeconds = dailyIntervalDays * 86400;
                break;
            case 'weekly':
                var selectedDays = [];
                $row.find('.job-weekly-day:checked').each(function () {
                    selectedDays.push($(this).val());
                });
                weeklyDays = selectedDays.join(',');
                dailyTime = convertTimeToTimeSpan($row.find('.job-weekly-time').val());
                intervalSeconds = 604800;
                break;
            case 'hourly':
                hourlyMinute = parseInt($row.find('.job-hourly-minute').val(), 10) || 0;
                intervalSeconds = 3600;
                break;
            case 'custom':
                var customInterval = parseInt($row.find('.job-custom-interval').val(), 10) || 0;
                var customUnit = $row.find('.job-custom-unit').val();
                switch (customUnit) {
                    case 'seconds': intervalSeconds = customInterval; break;
                    case 'minutes': intervalSeconds = customInterval * 60; break;
                    case 'hours': intervalSeconds = customInterval * 3600; break;
                    case 'days': intervalSeconds = customInterval * 86400; break;
                }
                break;
        }

        return {
            scheduleType: scheduleType,
            intervalSeconds: intervalSeconds,
            dailyIntervalDays: dailyIntervalDays,
            weeklyDays: weeklyDays,
            dailyTime: dailyTime,
            hourlyMinute: hourlyMinute
        };
    }

    function filterJobsTable() {
        var $tbody = $('#jobsTableBody');
        if (!$tbody.length) return;

        var term = ($('#jobsSearch').val() || '').toLowerCase().trim();
        var $rows = $tbody.children('tr').not('.jobs-no-results');
        $tbody.children('tr.jobs-no-results').remove();
        var visibleCount = 0;

        $rows.each(function () {
            var $row = $(this);
            var haystack = String($row.attr('data-search') || $row.children('td').first().text() || '').toLowerCase();
            var match = !term || haystack.indexOf(term) !== -1;
            $row.toggle(match);
            if (match) visibleCount++;
        });

        if (visibleCount === 0 && $rows.length > 0) {
            $tbody.append(
                '<tr class="jobs-no-results">' +
                '<td colspan="6">' +
                '<div class="empty-state" style="padding:40px 16px">' +
                '<i class="fas fa-search"></i>' +
                '<h5>نتیجه‌ای یافت نشد</h5>' +
                '<p class="mb-0">سرویسی با این نام یا توضیحات پیدا نشد.</p>' +
                '</div>' +
                '</td>' +
                '</tr>'
            );
        }
    }

    function initJobsPage() {
        if (!$('#jobsTableBody').length) return;

        $('.schedule-type').each(function () {
            $(this).trigger('change');
        });

        filterJobsTable();

        if (window.JobRealtime) {
            window.JobRealtime.updateStatusSummary();
        }
    }

    $(document).on('input', '#jobsSearch', filterJobsTable);

    $(document).on('change', '.schedule-type', function () {
        var scheduleType = $(this).val();
        var $row = $(this).closest('tr');
        var $config = $row.find('.schedule-config');
        $config.find('> div').hide();
        switch (scheduleType) {
            case 'interval': $config.find('.interval-config').show(); break;
            case 'daily': $config.find('.daily-config').show(); break;
            case 'weekly': $config.find('.weekly-config').show(); break;
            case 'hourly': $config.find('.hourly-config').show(); break;
            case 'custom': $config.find('.custom-config').show(); break;
        }
    });

    $(document).on('change', '#newScheduleType', function () {
        var scheduleType = $(this).val();
        $('#intervalConfig, #dailyConfig, #weeklyConfig, #hourlyConfig').addClass('d-none');
        switch (scheduleType) {
            case 'interval': $('#intervalConfig').removeClass('d-none'); break;
            case 'daily': $('#dailyConfig').removeClass('d-none'); break;
            case 'weekly': $('#weeklyConfig').removeClass('d-none'); break;
            case 'hourly': $('#hourlyConfig').removeClass('d-none'); break;
        }
    });

    $(document).on('click', '.btn-create-schedule', function () {
        $('#newScheduleJobId').val($(this).data('id'));
        var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('createScheduleModal'));
        modal.show();
    });

    $(document).on('click', '#btnConfirmCreateSchedule', function () {
        var jobId = $('#newScheduleJobId').val();
        var scheduleType = $('#newScheduleType').val();
        var isActive = $('#newScheduleIsActive').is(':checked');
        var intervalSeconds = 0;
        var dailyIntervalDays = 1;
        var weeklyDays = '';
        var dailyTime = '00:00:00';
        var hourlyMinute = 0;

        switch (scheduleType) {
            case 'interval':
                intervalSeconds = parseInt($('#newScheduleInterval').val(), 10) || 60;
                break;
            case 'daily':
                dailyIntervalDays = parseInt($('#newScheduleDailyInterval').val(), 10) || 1;
                dailyTime = convertTimeToTimeSpan($('#newScheduleDailyTime').val());
                intervalSeconds = dailyIntervalDays * 86400;
                break;
            case 'weekly':
                var selectedDays = [];
                $('#weeklyConfig input[type="checkbox"]:checked').each(function () {
                    selectedDays.push($(this).val());
                });
                weeklyDays = selectedDays.join(',');
                dailyTime = convertTimeToTimeSpan($('#newScheduleWeeklyTime').val());
                intervalSeconds = 604800;
                break;
            case 'hourly':
                hourlyMinute = parseInt($('#newScheduleHourlyMinute').val(), 10) || 0;
                intervalSeconds = 3600;
                break;
        }

        $.post('/Jobs/CreateSchedule', {
            jobId: jobId,
            isActive: isActive,
            interval: intervalSeconds,
            scheduleType: scheduleType,
            dailyIntervalDays: dailyIntervalDays,
            weeklyDays: weeklyDays,
            dailyTime: dailyTime,
            hourlyMinute: hourlyMinute
        }).done(function (response) {
            var modal = bootstrap.Modal.getInstance(document.getElementById('createScheduleModal'));
            if (modal) modal.hide();
            if (window.AppToast) AppToast.show(response.message || 'زمان‌بندی ایجاد شد', 'success');
            if (window.SoftNav) SoftNav.reload();
        }).fail(function (xhr) {
            var errorMessage = (xhr.responseJSON && xhr.responseJSON.message) || xhr.responseText || 'خطای ناشناخته';
            if (window.AppToast) AppToast.show('خطا در ایجاد زمان‌بندی: ' + errorMessage, 'error');
            else alert('خطا در ایجاد زمان‌بندی: ' + errorMessage);
        });
    });

    $(document).on('click', '.btn-save', function () {
        var $btn = $(this);
        var id = $btn.data('id');
        var $row = $('#row-' + id);
        var isActive = $row.find('.job-active').is(':checked');
        var params = collectScheduleParams($row);

        if (isActive && params.intervalSeconds <= 0 && params.scheduleType === 'interval') {
            if (window.AppToast) AppToast.show('لطفا یک فاصله زمانی معتبر وارد کنید.', 'warning');
            else alert('لطفا یک فاصله زمانی معتبر وارد کنید.');
            return;
        }

        var originalText = $btn.text();
        $btn.text('در حال ذخیره...').prop('disabled', true);

        $.post('/Jobs/UpdateSettings', {
            scheduleId: id,
            isActive: isActive,
            interval: params.intervalSeconds,
            scheduleType: params.scheduleType,
            dailyIntervalDays: params.dailyIntervalDays,
            weeklyDays: params.weeklyDays,
            dailyTime: params.dailyTime,
            hourlyMinute: params.hourlyMinute
        }).done(function (response) {
            if (window.AppToast) AppToast.show(response.message || 'ذخیره شد', 'success');
            if (window.JobRealtime && response.scheduleId) {
                window.JobRealtime.applyStatusToRow(response);
            }
        }).fail(function () {
            if (window.AppToast) AppToast.show('خطا در ذخیره تنظیمات', 'error');
            else alert('خطا در ذخیره تنظیمات');
        }).always(function () {
            $btn.text(originalText).prop('disabled', false);
        });
    });

    $(document).on('click', '.btn-run-now', function () {
        var $btn = $(this);
        var id = $btn.data('id');
        if (!confirm('آیا مطمئن هستید که می‌خواهید این سرویس را فوراً اجرا کنید؟')) return;

        var originalText = $btn.text();
        $btn.addClass('is-loading').text('در حال برنامه‌ریزی...').prop('disabled', true);

        $.post('/Jobs/RunNow', { scheduleId: id })
            .done(function (response) {
                if (window.AppToast) AppToast.show(response.message || 'برنامه‌ریزی شد', 'success');
                if (window.JobRealtime && response.scheduleId) {
                    window.JobRealtime.applyStatusToRow(response);
                }
            })
            .fail(function () {
                if (window.AppToast) AppToast.show('خطا در اجرای سرویس', 'error');
                else alert('خطا در اجرای سرویس');
            })
            .always(function () {
                $btn.removeClass('is-loading').text(originalText).prop('disabled', false);
            });
    });

    $(document).on('click', '.btn-logs', function () {
        var id = $(this).data('id');
        var $tbody = $('#logTableBody');
        var $loader = $('#loadingLogs');
        $tbody.empty();
        $loader.show();

        $.get('/Jobs/GetHistory', { scheduleId: id })
            .done(function (data) {
                $loader.hide();
                if (!data || data.length === 0) {
                    $tbody.append('<tr><td colspan="4" class="text-center">هیچ لاگی یافت نشد.</td></tr>');
                    return;
                }

                $.each(data, function (_i, item) {
                    var statusBadge = '<span class="badge bg-warning">تکمیل نشده</span>';
                    if (item.isSuccess) statusBadge = '<span class="badge bg-success">موفق</span>';
                    else if (!item.isSuccess && item.endTime) statusBadge = '<span class="badge bg-danger">شکست</span>';

                    var startTime = window.JobRealtime
                        ? window.JobRealtime.formatDate(item.startTime)
                        : item.startTime;
                    var endTime = item.endTime
                        ? (window.JobRealtime ? window.JobRealtime.formatDate(item.endTime) : item.endTime)
                        : '—';

                    $tbody.append(
                        '<tr>' +
                        '<td dir="ltr">' + startTime + '</td>' +
                        '<td dir="ltr">' + endTime + '</td>' +
                        '<td>' + statusBadge + '</td>' +
                        '<td class="text-wrap" style="max-width:300px;"></td>' +
                        '</tr>'
                    );
                    $tbody.find('tr:last td:last').text(item.message || '');
                });
            })
            .fail(function () {
                $loader.hide();
                $tbody.append('<tr><td colspan="4" class="text-danger">خطا در دریافت اطلاعات.</td></tr>');
            });
    });

    $(document).on('page:loaded', initJobsPage);
})(window, jQuery);
