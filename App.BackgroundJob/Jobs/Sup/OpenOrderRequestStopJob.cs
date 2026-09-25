using Common.Attributes;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.Hrm;
using Entities.App.Sup;
using Entities.App.Sup.Enums;
using Entities.Base;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Net;
using System.Text;

namespace App.BackgroundJob.Jobs.Sup
{
    /// <summary>
    /// جاب‌های روزانه توقف «درخواست‌های باز» (WP3 / D7).
    /// معادل <c>HtsTaskService.SupplysSystemTask</c> در HTS که هر روز ساعت ۰۹:۳۵ اجرا می‌شد:
    /// <list type="number">
    /// <item><see cref="SendStopDeadlineReminders"/> ⇐ <c>OpenOrderRequestCommentService.SendStopCommentsThatResponseDeadlineDateHasArrived</c></item>
    /// <item><see cref="EscalateStaleStops"/> ⇐ <c>OpenOrderRequestCommentService.SendStopCommentsThatNextStepOfStopOperatorDoseNotTriggered</c></item>
    /// <item><see cref="SendUnsentDispatchDigest"/> ⇐ <c>OpenOrderRequestCommentService.SendSupplyCommentNotifications</c></item>
    /// </list>
    /// هیچ ایمیلی به‌صورت مستقیم (SMTP) ارسال نمی‌شود؛ فقط ردیف <see cref="Notification"/> ثبت می‌شود
    /// (اعلان درون‌برنامه‌ای + صف ایمیل <c>System.EmailJob.SendPendingEmailsAsync</c>) — تصمیم Q10.
    /// وضعیت 2821 مستقل از موفقیت اعلان/ایمیل ثبت می‌شود — تصمیم Q6 (HTS فقط در صورت موفقیت ایمیل ثبت می‌کرد).
    /// </summary>
    public class OpenOrderRequestStopJob(ApplicationDbContext dbContext, IUnitOfWork unitOfWork)
    {
        #region Constants

        /// <summary>عنوان اعلان یادآوری مهلت — عین موضوع ایمیل HTS.</summary>
        public const string DeadlineReminderTitle = "هشدار پایان مهلت پاسخگویی به توقف درخواست خرید";

        /// <summary>
        /// عنوان اعلان ارسال خودکار به کارتابل تدارکات.
        /// در HTS همان موضوع یادآوری استفاده می‌شد؛ برای تمایز در کارتابل و جلوگیری از تداخل با منطق تکراری‌گیری، عنوان جدا شد.
        /// </summary>
        public const string EscalationTitle = "ارسال خودکار توقف درخواست خرید به کارتابل تدارکات (عدم بررسی در زمان مشخص)";

        /// <summary>متن کامنت سیستمی — عین متن <c>StopCheckingComment</c> در HTS.</summary>
        public const string EscalationCommentText = "ارسال خودکار درخواست توقف به کارتابل واحد تدارکات توسط مدیر سیستم (بعلت عدم بررسی در زمان مشخص)";

        private const string SupplyUnitGroupCode = "Sup.OpenOrderRequest.SupplyUnit";
        private const string UnsentDispatchDigestGroupCode = "Sup.OpenOrderRequest.UnsentDispatchDigest";
        private const string EditViewPath = "/Panel/Sup/OpenOrderRequest/Edit?id=";
        private const string ReminderQueryParam = "&stopReminder=";
        private const string DigestViewPathPrefix = "/Panel/Sup/OpenOrderRequest/List?digest=unsentDispatch&date=";

        /// <summary>عنوان دایجست روزانه — عین موضوع ایمیل HTS.</summary>
        public const string UnsentDispatchDigestTitle = "تامین درخواست های برگ ارسال نخورده";

        /// <summary>
        /// <c>true</c> = رفتار HTS: یادآوری فقط در روز مهلت (<c>ResponseDeadlineDate == today</c>).
        /// <c>false</c> = هر روز از روز مهلت تا اقدام عامل توقف (حداکثر یک بار در روز).
        /// </summary>
        private const bool RemindOnDeadlineDayOnly = true;

        /// <summary>بیش از این تعداد روز بدون اقدام پس از اعلام نتیجه عامل توقف ⇒ 2821 (HTS: ۱۴ روز).</summary>
        private const int StaleStopDays = 14;

        /// <summary>مهلت پاسخ عامل توقف (همان قاعده کنترلر: ۲ روز روتین / ۳ روز غیرروتین).</summary>
        private const int RoutineResponseDays = 2;
        private const int NonRoutineResponseDays = 3;

        /// <summary>کاربر سیستم (admin) — HTS هم با UpdatedUserId = 1 ثبت می‌کرد.</summary>
        private const long SystemUserId = 1;
        private const string SystemUserName = "مدیر سیستم";

        #endregion

        #region (1) یادآوری پایان مهلت پاسخگویی عامل توقف

        /// <summary>
        /// شرط: درخواست فعال (IsDeleted=0, IsForceDeletedByUser=0, IsStop=1) با وضعیت توقف 2807 یا 2816
        /// که آخرین کامنت توقف آن عامل توقف دارد، نتیجه بررسی ندارد و مهلت پاسخ آن (ResponseDeadlineMiladiDate؛
        /// برای توقف مجدد بدون مهلت = تاریخ کامنت + ۲/۳ روز) امروز یا قبل از امروز است.
        /// اقدام: حداکثر یک اعلان در روز به ازای هر کامنت توقف — گیرنده: عامل توقف (To)، رونوشت: ثبت‌کننده توقف.
        /// تکراری‌گیری: وجود Notification با همین عنوان/EntityId/ViewPath (شامل stopReminder=&lt;commentId&gt;) در امروز.
        /// </summary>
        [JobHandler(
            "یادآوری پایان مهلت پاسخگویی عامل توقف (درخواست‌های باز)",
            "روزانه ۰۹:۳۵ — معادل SendStopCommentsThatResponseDeadlineDateHasArrived در HTS. برای هر توقف در انتظار بررسی عامل توقف (وضعیت 2807/2816) که مهلت پاسخ آن رسیده یا گذشته، حداکثر یک اعلان در روز به عامل توقف (رونوشت: ثبت‌کننده توقف) ثبت می‌کند. ایمیل از صف EmailJob ارسال می‌شود.")]
        public async Task SendStopDeadlineReminders(IJobLogger? jobLogger = null, CancellationToken cn = default)
        {
            var today = DateTime.Now.Date;
            await LogInfoAsync(jobLogger, "شروع یادآوری مهلت پاسخگویی توقف درخواست‌های باز...", cn);

            var orders = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
                .Include(o => o.Part)
                .Include(o => o.Comments)
                .Where(o => !o.IsDeleted && !o.IsForceDeletedByUser && o.IsStop
                            && (o.StopStatus == OpenOrderRequestStopStatusEnum.InitialSubmissionAndSendToStopBoard
                                || o.StopStatus == OpenOrderRequestStopStatusEnum.CheckStopFactorReRegisterStop))
                .ToListAsync(cn);

            var candidates = new List<(OpenOrderRequest Order, OpenOrderRequestComment Comment, DateTime Deadline)>();
            foreach (var order in orders)
            {
                var current = GetCurrentStopComment(order);
                if (current == null || current.StopOperatorId is not > 0 || current.StopCheckingResult.HasValue)
                    continue;

                var deadline = ResolveResponseDeadline(order, current);
                if (deadline == null)
                    continue;
                if (RemindOnDeadlineDayOnly)
                {
                    if (deadline.Value.Date != today)
                        continue;
                }
                else if (deadline.Value.Date > today)
                {
                    continue;
                }

                candidates.Add((order, current, deadline.Value));
            }

            await LogInfoAsync(jobLogger, $"درخواست‌های در انتظار بررسی عامل توقف: {orders.Count} — مهلت رسیده/گذشته: {candidates.Count}", cn);
            if (candidates.Count == 0)
                return;

            // تکراری‌گیری روزانه: اعلان‌هایی که امروز با همین عنوان برای همین درخواست‌ها ثبت شده‌اند
            var orderIds = candidates.Select(c => c.Order.Id!.Value).ToList();
            var sentTodayPaths = (await unitOfWork.Repository<Notification>().TableNoTracking
                    .Where(n => n.Title == DeadlineReminderTitle
                                && n.CreatedOnMiladiDateTime >= today
                                && n.EntityId != null && orderIds.Contains(n.EntityId.Value)
                                && n.ViewPath != null)
                    .Select(n => n.ViewPath!)
                    .ToListAsync(cn))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var users = await LoadUsersAsync(cn);

            int created = 0, skipped = 0, failed = 0;
            foreach (var (order, comment, deadline) in candidates)
            {
                cn.ThrowIfCancellationRequested();

                var viewPath = BuildReminderViewPath(order.Id!.Value, comment.Id!.Value);
                if (sentTodayPaths.Contains(viewPath))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    var operatorUser = ResolveUser(comment.StopOperatorId, users);
                    var creator = ResolveUser(comment.CreatedById, users);
                    var creatorName = comment.CreatedByName.HasValue() ? comment.CreatedByName! : DisplayName(creator);

                    var body = BuildDeadlineReminderBody(order, comment, deadline, creatorName);
                    var notifiedAnyone = false;

                    // گیرنده اصلی: عامل توقف (To) — رونوشت: ثبت‌کننده توقف (مطابق HTS)
                    if (operatorUser is { IsActive: true })
                    {
                        await AddNotificationAsync(operatorUser.Id, DeadlineReminderTitle, body, order.Id.Value, viewPath,
                            toEmails: EmailsOf(operatorUser), ccEmails: EmailsOf(creator), cn);
                        notifiedAnyone = true;
                    }
                    else
                    {
                        await LogWarningAsync(jobLogger,
                            $"درخواست {order.Id}: عامل توقف (UserId={comment.StopOperatorId}) در جدول کاربران یافت نشد یا غیرفعال است؛ فقط ثبت‌کننده توقف مطلع می‌شود.", cn);
                    }

                    // نسخه درون‌برنامه‌ای برای ثبت‌کننده توقف (ایمیل او در رونوشت رفته است؛ ایمیل تکراری ثبت نمی‌شود)
                    if (creator is { IsActive: true } && creator.Id != operatorUser?.Id)
                    {
                        await AddNotificationAsync(creator.Id, DeadlineReminderTitle, body, order.Id.Value, viewPath,
                            toEmails: null, ccEmails: null, cn);
                        notifiedAnyone = true;
                    }

                    if (notifiedAnyone)
                    {
                        sentTodayPaths.Add(viewPath);
                        created++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    await LogWarningAsync(jobLogger, $"خطا در ثبت یادآوری برای درخواست {order.Id} (کامنت {comment.Id}): {ex.Message}", cn);
                }
            }

            await LogInfoAsync(jobLogger, $"پایان یادآوری مهلت پاسخگویی — ثبت‌شده: {created}، تکراری امروز/بدون گیرنده: {skipped}، خطا: {failed}", cn);
        }

        #endregion

        #region (2) ارسال خودکار توقف‌های بدون اقدام به کارتابل تدارکات (2821)

        /// <summary>
        /// شرط: درخواست فعال و متوقف با وضعیت توقف 2808/2810/2811/2812/2813 (عامل توقف نتیجه را اعلام کرده، گام بعدی انجام نشده)
        /// که از تاریخ اعلام نتیجه (StopCheckingMiladiDateTime؛ در نبود آن تاریخ ویرایش/تاریخ کامنت) بیش از ۱۴ روز گذشته باشد.
        /// اقدام (به ترتیب): ۱) StopStatus = 2821 + کامنت توقف سیستمی (یک SaveChanges؛ همیشه — Q6)،
        /// ۲) اعلان به گروه تدارکات (Sup.OpenOrderRequest.SupplyUnit)، متولی خرید دسته کالا، ثبت‌کننده توقف و عامل توقف.
        /// ایدم‌پوتنت: پس از تغییر وضعیت به 2821 درخواست دیگر در شرط انتخاب نیست.
        /// </summary>
        [JobHandler(
            "ارسال خودکار توقف‌های بررسی‌نشده درخواست‌های باز به کارتابل تدارکات (۱۴ روز ⇐ 2821)",
            "روزانه ۰۹:۳۶ — معادل SendStopCommentsThatNextStepOfStopOperatorDoseNotTriggered در HTS. اگر بیش از ۱۴ روز از اعلام نتیجه عامل توقف (وضعیت 2808/2810/2811/2812/2813) گذشته و اقدامی نشده باشد، وضعیت توقف همیشه به 2821 تغییر می‌کند (تصمیم Q6)، کامنت سیستمی ثبت و به گروه تدارکات، متولی خرید، ثبت‌کننده و عامل توقف اعلان داده می‌شود.")]
        public async Task EscalateStaleStops(IJobLogger? jobLogger = null, CancellationToken cn = default)
        {
            var now = DateTime.Now;
            var today = now.Date;
            await LogInfoAsync(jobLogger, $"شروع بررسی توقف‌های تعیین‌تکلیف‌شده بدون اقدام (بیش از {StaleStopDays} روز)...", cn);

            var orders = await unitOfWork.Repository<OpenOrderRequest>().Table
                .Include(o => o.Part).ThenInclude(p => p.BuyCategory)
                .Include(o => o.Comments)
                .Where(o => !o.IsDeleted && !o.IsForceDeletedByUser && o.IsStop
                            && (o.StopStatus == OpenOrderRequestStopStatusEnum.StopCauseRequiresRestart
                                || o.StopStatus == OpenOrderRequestStopStatusEnum.CheckStopFactorNeedToFixBom
                                || o.StopStatus == OpenOrderRequestStopStatusEnum.NeedsDocumentRevision
                                || o.StopStatus == OpenOrderRequestStopStatusEnum.IndustrialUnitStopCauseDeletionRequestCheck
                                || o.StopStatus == OpenOrderRequestStopStatusEnum.ProjectManagerPendingStopReason))
                .ToListAsync(cn);

            await LogInfoAsync(jobLogger, $"درخواست‌های متوقف با نتیجه اعلام‌شده عامل توقف: {orders.Count}", cn);
            if (orders.Count == 0)
                return;

            var supplyUnitUserIds = await dbContext.NotificationGroupMembers
                .AsNoTracking()
                .Where(m => m.NotificationGroup.Code == SupplyUnitGroupCode && m.IsActive == IsActiveEnum.Active)
                .Select(m => m.UserId)
                .Distinct()
                .ToListAsync(cn);

            if (supplyUnitUserIds.Count == 0)
                await LogWarningAsync(jobLogger,
                    $"گروه اعلان «{SupplyUnitGroupCode}» عضو فعالی ندارد؛ اعلان فقط به متولی خرید / ثبت‌کننده / عامل توقف می‌رسد (D24).", cn);

            var users = await LoadUsersAsync(cn);
            var orderRepo = unitOfWork.Repository<OpenOrderRequest>();
            var commentRepo = unitOfWork.Repository<OpenOrderRequestComment>();

            int escalated = 0, notified = 0, notStale = 0, skipped = 0, failed = 0;
            foreach (var order in orders)
            {
                cn.ThrowIfCancellationRequested();

                var current = GetCurrentStopComment(order);
                if (current == null)
                {
                    skipped++;
                    await LogWarningAsync(jobLogger,
                        $"درخواست {order.Id}: وضعیت توقف {(int?)order.StopStatus} است ولی کامنت توقف ندارد؛ نادیده گرفته شد.", cn);
                    continue;
                }

                var referenceDate = current.StopCheckingMiladiDateTime
                                    ?? current.ModifiedDateMiladiDateTime
                                    ?? current.MiladiDate
                                    ?? current.CreatedOnMiladiDateTime;

                // HTS: DbFunctions.AddDays(UpdatedDate, 14) < today  (بیش از ۱۴ روز کامل)
                if (referenceDate == null || referenceDate.Value.Date.AddDays(StaleStopDays) >= today)
                {
                    notStale++;
                    continue;
                }

                var previousStatus = order.StopStatus!.Value;
                var previousStatusTitle = previousStatus.ToDisplay();
                var systemComment = BuildEscalationComment(order, current, previousStatusTitle, now);

                try
                {
                    // ۱) وضعیت 2821 + کامنت سیستمی در یک SaveChanges — مستقل از موفقیت اعلان (Q6)
                    await commentRepo.AddAsync(systemComment, cn, saveNow: false);
                    order.StopStatus = OpenOrderRequestStopStatusEnum.SendToProcurementDueToMissingReview;
                    await orderRepo.UpdateAsync(order, cn);
                    escalated++;

                    await LogInfoAsync(jobLogger,
                        $"درخواست {order.Id} (درخواست خرید {order.PurchaseRequestNumber}): {previousStatusTitle} ⇐ 2821؛ مرجع تاریخ {referenceDate.Value:yyyy-MM-dd}", cn);
                }
                catch (Exception ex)
                {
                    failed++;
                    // تغییرات معلق همین درخواست را از Context خارج کن تا SaveChanges درخواست‌های بعدی را خراب نکند
                    dbContext.Entry(systemComment).State = EntityState.Detached;
                    order.StopStatus = previousStatus;
                    dbContext.Entry(order).State = EntityState.Unchanged;

                    await LogWarningAsync(jobLogger, $"خطا در تغییر وضعیت درخواست {order.Id} به 2821: {ex.Message}", cn);
                    continue;
                }

                // ۲) اعلان‌ها — خطا در این بخش وضعیت ثبت‌شده را برنمی‌گرداند
                try
                {
                    notified += await NotifyEscalationAsync(order, current, previousStatusTitle, supplyUnitUserIds, users, cn);
                }
                catch (Exception ex)
                {
                    await LogWarningAsync(jobLogger,
                        $"وضعیت درخواست {order.Id} به 2821 تغییر کرد ولی ثبت اعلان ناموفق بود: {ex.Message}", cn);
                }
            }

            await LogInfoAsync(jobLogger,
                $"پایان — ارسال به کارتابل تدارکات: {escalated}، اعلان ثبت‌شده: {notified}، هنوز در مهلت: {notStale}، بدون کامنت توقف: {skipped}، خطا: {failed}", cn);
        }

        private static OpenOrderRequestComment BuildEscalationComment(
            OpenOrderRequest order,
            OpenOrderRequestComment current,
            string previousStatusTitle,
            DateTime now)
        {
            // مانند مسیر «ادامه روند توقف» در کنترلر (2813): کامنت توقف جدید با انتقال داده‌های توقف جاری،
            // تا این ردیف «کامنت توقف جاری» برای گام بعدی (تصمیم تدارکات در 2821) باشد و در تاریخچه کامنت‌ها دیده شود.
            return new OpenOrderRequestComment
            {
                OpenOrderRequestId = order.Id!.Value,
                MiladiDate = now,
                ShamsiDate = now.ToShamsiDate(),
                IsStop = true,
                CommentValue = EscalationCommentText,
                StopType = current.StopType,
                StopOperatorId = current.StopOperatorId,
                BeneficiariesIds = current.BeneficiariesIds,
                BeneficiariesNames = current.BeneficiariesNames,
                StopCheckingResult = current.StopCheckingResult,
                StopCheckingMiladiDateTime = current.StopCheckingMiladiDateTime,
                StopCheckingShamsiDateTime = current.StopCheckingShamsiDateTime,
                StopCheckingComment = current.StopCheckingComment,
                StopCheckingDelayReasonText = current.StopCheckingDelayReasonText,
                IsNeedToUpdateBom = current.IsNeedToUpdateBom,
                IsNeedToDeleteBom = current.IsNeedToDeleteBom,
                ProjectManagerApproximateCommentMiladiDate = current.ProjectManagerApproximateCommentMiladiDate,
                ProjectManagerApproximateCommentShamsiDate = current.ProjectManagerApproximateCommentShamsiDate,
                AdditionalDescription =
                    $"تغییر خودکار وضعیت توقف از «{previousStatusTitle}» به «{OpenOrderRequestStopStatusEnum.SendToProcurementDueToMissingReview.ToDisplay()}» پس از بیش از {StaleStopDays} روز بدون اقدام",
                CreatedById = SystemUserId,
                CreatedByName = SystemUserName,
                ModifiedById = SystemUserId,
                ModifiedByName = SystemUserName
            };
        }

        private async Task<int> NotifyEscalationAsync(
            OpenOrderRequest order,
            OpenOrderRequestComment current,
            string previousStatusTitle,
            List<long> supplyUnitUserIds,
            Dictionary<long, UserInfo> users,
            CancellationToken cn)
        {
            var recipientIds = new HashSet<long>(supplyUnitUserIds);

            if (order.Part?.BuyCategory?.PurchaseResponsibleId is long purchaseResponsibleId && purchaseResponsibleId > 0)
                recipientIds.Add(purchaseResponsibleId);

            if (current.CreatedById is long creatorId && creatorId > 0)
                recipientIds.Add(creatorId);

            if (current.StopOperatorId is long operatorId && operatorId > 0)
                recipientIds.Add(operatorId);

            var operatorName = DisplayName(ResolveUser(current.StopOperatorId, users));
            var creatorName = current.CreatedByName.HasValue() ? current.CreatedByName! : DisplayName(ResolveUser(current.CreatedById, users));
            var body = BuildEscalationBody(order, current, operatorName, creatorName, previousStatusTitle);
            var viewPath = $"{EditViewPath}{order.Id}";

            var count = 0;
            foreach (var userId in recipientIds)
            {
                if (!users.TryGetValue(userId, out var user) || !user.IsActive)
                    continue;

                await AddNotificationAsync(userId, EscalationTitle, body, order.Id!.Value, viewPath,
                    toEmails: EmailsOf(user), ccEmails: null, cn);
                count++;
            }

            return count;
        }

        #endregion

        #region (3) دایجست «تامین درخواست های برگ ارسال نخورده»

        /// <summary>
        /// معادل <c>SendSupplyCommentNotifications</c> در HTS:
        /// کامنت‌های غیرتوقف امروز روی درخواست فعال، یک کامنت آخر به ازای هر درخواست، یک ایمیل HTML به گروه
        /// <c>Sup.OpenOrderRequest.UnsentDispatchDigest</c> (در نبود عضو: <c>SupplyUnit</c>).
        /// فیلتر واحد سازمانی عین HTS: <c>CreatedOrgUnit_FK == 51</c> («تامین و خرید») —
        /// در Havayar همان واحد با عنوان <c>تامین و خرید</c> (Id فعلی ۹۸) نگاشت می‌شود.
        /// </summary>
        [JobHandler(
            "دایجست تامین درخواست های برگ ارسال نخورده",
            "روزانه ۰۹:۳۷ — معادل SendSupplyCommentNotifications در HTS. کامنت‌های غیرتوقف امروز درخواست‌های فعال را در یک اعلان HTML به گروه UnsentDispatchDigest می‌فرستد.")]
        public async Task SendUnsentDispatchDigest(IJobLogger? jobLogger = null, CancellationToken cn = default)
        {
            var today = DateTime.Now.Date;
            await LogInfoAsync(jobLogger, "شروع دایجست «تامین درخواست های برگ ارسال نخورده»...", cn);

            var digestDateKey = today.ToString("yyyyMMdd");
            var viewPath = DigestViewPathPrefix + digestDateKey;
            var alreadySent = await unitOfWork.Repository<Notification>().TableNoTracking
                .AnyAsync(n => n.Title == UnsentDispatchDigestTitle
                               && n.CreatedOnMiladiDateTime >= today
                               && n.ViewPath == viewPath, cn);
            if (alreadySent)
            {
                await LogInfoAsync(jobLogger, "دایجست امروز قبلاً ثبت شده؛ رد شد.", cn);
                return;
            }

            // HTS CreatedOrgUnit_FK == 51 («تامین و خرید») → Hrm.OrgUnit با همان عنوان
            var supplyOrgUnitId = await unitOfWork.Repository<OrgUnit>().TableNoTracking
                .Where(o => o.Title == "تامین و خرید")
                .Select(o => o.Id)
                .FirstOrDefaultAsync(cn);

            if (supplyOrgUnitId is null or 0)
            {
                await LogWarningAsync(jobLogger, "واحد سازمانی «تامین و خرید» یافت نشد؛ دایجست رد شد.", cn);
                return;
            }

            var comments = await unitOfWork.Repository<OpenOrderRequestComment>().TableNoTracking
                .Include(c => c.OpenOrderRequest).ThenInclude(o => o.Part)
                .Include(c => c.OpenOrderRequest).ThenInclude(o => o.ManCompany).ThenInclude(s => s.Party)
                .Where(c => !c.IsStop
                            && c.CreatedOrgUnitId == supplyOrgUnitId
                            && c.MiladiDate.HasValue && c.MiladiDate.Value.Date == today
                            && c.OpenOrderRequest != null
                            && !c.OpenOrderRequest.IsDeleted
                            && !c.OpenOrderRequest.IsForceDeletedByUser)
                .ToListAsync(cn);

            var latest = comments
                .GroupBy(c => c.OpenOrderRequestId)
                .Select(g => g.OrderByDescending(c => c.Id).First())
                .ToList();

            await LogInfoAsync(jobLogger, $"کامنت غیرتوقف امروز (واحد تامین و خرید/{supplyOrgUnitId}): {comments.Count} — درخواست یکتا: {latest.Count}.", cn);
            if (latest.Count == 0)
                return;

            var recipientIds = await dbContext.NotificationGroupMembers
                .AsNoTracking()
                .Where(m => m.NotificationGroup.Code == UnsentDispatchDigestGroupCode && m.IsActive == IsActiveEnum.Active)
                .Select(m => m.UserId)
                .Distinct()
                .ToListAsync(cn);

            if (recipientIds.Count == 0)
            {
                await LogWarningAsync(jobLogger,
                    $"گروه «{UnsentDispatchDigestGroupCode}» عضو ندارد؛ fallback به «{SupplyUnitGroupCode}».", cn);
                recipientIds = await dbContext.NotificationGroupMembers
                    .AsNoTracking()
                    .Where(m => m.NotificationGroup.Code == SupplyUnitGroupCode && m.IsActive == IsActiveEnum.Active)
                    .Select(m => m.UserId)
                    .Distinct()
                    .ToListAsync(cn);
            }

            if (recipientIds.Count == 0)
            {
                await LogWarningAsync(jobLogger, "هیچ گیرنده‌ای برای دایجست برگ ارسال‌نخورده نیست (WP8 اعضا را پر می‌کند).", cn);
                return;
            }

            var users = await LoadUsersAsync(cn);
            var body = BuildUnsentDispatchDigestBody(latest);
            var created = 0;
            foreach (var userId in recipientIds)
            {
                if (!users.TryGetValue(userId, out var user) || !user.IsActive)
                    continue;

                await AddNotificationAsync(userId, UnsentDispatchDigestTitle, body, latest[0].OpenOrderRequestId, viewPath,
                    toEmails: EmailsOf(user), ccEmails: null, cn);
                created++;
            }

            await LogInfoAsync(jobLogger, $"پایان دایجست برگ ارسال‌نخورده — اعلان: {created}، درخواست: {latest.Count}", cn);
        }

        private static string BuildUnsentDispatchDigestBody(List<OpenOrderRequestComment> latestComments)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<div style='direction:rtl;text-align:right;padding:5px; margin:5px; font-family:tahoma; font-size: 11pt'>");
            sb.AppendLine("کاربر گرامی، لیست زیر نمایانگر درخواست هایی است که زمان تامین آن ها فرا رسیده اما برگ ارسالی جهت آنها صادر نشده است");
            sb.AppendLine("<br /><br />");

            foreach (var comment in latestComments)
            {
                var order = comment.OpenOrderRequest;
                var company = order?.ManCompany?.Party?.FullName;
                sb.AppendLine("<ul>");
                AppendItem(sb, "شماره درخواست خرید", order?.PurchaseRequestNumber.ToString());
                AppendItem(sb, "شماره سفارش", order?.OrderNo?.ToString());
                AppendItem(sb, "کد کالا", order?.Part?.Code);
                AppendItem(sb, "عنوان کالا", order?.Part?.Name);
                AppendItem(sb, "عنوان شرکت", company);
                AppendItem(sb, "تعداد درخواست", order?.RequiredQty.ToString());
                AppendItem(sb, "تعداد خریداری", order?.SumSendQty?.ToString());
                AppendItem(sb, "تاریخ تامین", comment.ShamsiDate.HasValue() ? comment.ShamsiDate : comment.MiladiDate?.ToShamsiDate());
                AppendItem(sb, "توضیحات", comment.CommentValue);
                sb.AppendLine("</ul>");
                sb.AppendLine("<br />");
            }

            sb.AppendLine("</div></body></html>");
            return sb.ToString();
        }

        #endregion

        #region Helpers — انتخاب کامنت / مهلت / کاربر

        private static OpenOrderRequestComment? GetCurrentStopComment(OpenOrderRequest order)
        {
            // همان قاعده کنترلر StopOperation: آخرین کامنت توقف بر اساس Id
            return order.Comments
                .Where(c => c.IsStop)
                .OrderByDescending(c => c.Id)
                .FirstOrDefault();
        }

        private static DateTime? ResolveResponseDeadline(OpenOrderRequest order, OpenOrderRequestComment comment)
        {
            if (comment.ResponseDeadlineMiladiDate.HasValue)
                return comment.ResponseDeadlineMiladiDate;

            // کنترلر برای «ثبت توقف مجدد» (2816) مهلت ثبت نمی‌کند ⇒ همان قاعده ۲/۳ روز از تاریخ کامنت
            var start = comment.MiladiDate ?? comment.CreatedOnMiladiDateTime;
            return start?.AddDays(order.IsRoutineRequest ? RoutineResponseDays : NonRoutineResponseDays);
        }

        private static string BuildReminderViewPath(long orderId, long commentId)
            => $"{EditViewPath}{orderId}{ReminderQueryParam}{commentId}";

        private sealed record UserInfo(long Id, string Name, string? NameFa, string? Email, bool IsActive);

        private async Task<Dictionary<long, UserInfo>> LoadUsersAsync(CancellationToken cn)
        {
            var rows = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id != null)
                .Select(u => new { Id = u.Id!.Value, u.Name, u.NameFa, u.Email, u.IsActive })
                .ToListAsync(cn);

            return rows.ToDictionary(
                r => r.Id,
                r => new UserInfo(r.Id, r.Name, r.NameFa, r.Email, r.IsActive == IsActiveEnum.Active));
        }

        private static UserInfo? ResolveUser(long? userId, Dictionary<long, UserInfo> users)
            => userId is long id && users.TryGetValue(id, out var user) ? user : null;

        private static string DisplayName(UserInfo? user)
            => user == null ? "-" : (user.NameFa.HasValue() ? user.NameFa! : user.Name);

        private static List<string>? EmailsOf(UserInfo? user)
            => user?.Email.HasValue() == true ? new List<string> { user.Email!.Trim() } : null;

        #endregion

        #region Helpers — اعلان و متن‌ها (متن فارسی از HTS پورت شده)

        /// <summary>
        /// ثبت اعلان با همان مکانیزم OpenOrderRequestJob / کنترلر: ردیف Notification برای مالک (اعلان درون‌برنامه‌ای).
        /// اگر گیرنده ایمیل دارد Type=Email (در صف EmailJob) وگرنه فقط درون‌برنامه‌ای تا ردیف خطادار در صف ایمیل نماند.
        /// </summary>
        private async Task AddNotificationAsync(
            long ownerId,
            string title,
            string body,
            long entityId,
            string viewPath,
            List<string>? toEmails,
            List<string>? ccEmails,
            CancellationToken cn)
        {
            var hasTo = toEmails is { Count: > 0 };

            await unitOfWork.Repository<Notification>().AddAsync(new Notification
            {
                Type = hasTo ? NotificationType.Email : NotificationType.Appliaction,
                Title = title,
                Body = body,
                EntityId = entityId,
                OwnerId = ownerId,
                ViewPath = viewPath,
                IsRead = false,
                IsSend = false,
                ToEmails = hasTo ? toEmails : null,
                CcEmails = hasTo && ccEmails is { Count: > 0 } ? ccEmails : null
            }, cn);
        }

        private static string BuildDeadlineReminderBody(OpenOrderRequest order, OpenOrderRequestComment comment, DateTime deadline, string creatorName)
        {
            var sb = BeginBody();
            sb.AppendLine("کاربر گرامی، لیست زیر نمایانگر درخواست هایی است که توقفی جهت آنها صادر شده و <strong style='color:red'>مهلت پاسخگویی به آنها فرا رسیده است</strong> و تا این لحظه اقدامی از جانب شما صورت نگردیده است");
            sb.AppendLine("<br /><br />");
            sb.AppendLine("<ul>");
            AppendItem(sb, "شماره درخواست خرید", order.PurchaseRequestNumber.ToString());
            AppendItem(sb, "شماره سفارش", order.OrderNo?.ToString());
            AppendItem(sb, "کد کالا", order.Part?.Code);
            AppendItem(sb, "عنوان کالا", order.Part?.Name);
            AppendItem(sb, "ایجادکننده", creatorName);
            AppendItem(sb, "توضیحات", comment.CommentValue);
            AppendItem(sb, "مهلت پاسخ", comment.ResponseDeadlineShamsiDate.HasValue() ? comment.ResponseDeadlineShamsiDate : deadline.ToShamsiDate());
            sb.AppendLine("</ul>");
            return EndBody(sb);
        }

        private static string BuildEscalationBody(
            OpenOrderRequest order,
            OpenOrderRequestComment current,
            string operatorName,
            string creatorName,
            string previousStatusTitle)
        {
            var sb = BeginBody();
            sb.AppendLine($"کاربر گرامی، لیست زیر نمایانگر درخواست هایی است که توسط عامل توقف ({WebUtility.HtmlEncode(operatorName)}) وبیش از 2 هفته است که تعیین تکلیف شده ولی اقدامی جهت ادامه روند آن صورت نپذیرفته است.");
            sb.AppendLine("لذا توقف مذکور به صورت اتوماتيك از كارتابل عامل توقف خارج شده و به كارتابل تامين و خريد، تحت عنوان 'درخواست بررسی حذف' ارسال مي گردد. ");
            sb.AppendLine("<br /><br />");
            sb.AppendLine("<ul>");
            AppendItem(sb, "شماره درخواست خرید", order.PurchaseRequestNumber.ToString());
            AppendItem(sb, "شماره سفارش", order.OrderNo?.ToString());
            AppendItem(sb, "کد کالا", order.Part?.Code);
            AppendItem(sb, "عنوان کالا", order.Part?.Name);
            AppendItem(sb, "ایجادکننده", creatorName);
            AppendItem(sb, "توضیحات", current.CommentValue);
            AppendItem(sb, "نتیجه بررسی عامل توقف", current.StopCheckingResult?.ToDisplay());
            AppendItem(sb, "آخرین وضعیت قبلی", previousStatusTitle);
            AppendItem(sb, "وضعیت جدید", OpenOrderRequestStopStatusEnum.SendToProcurementDueToMissingReview.ToDisplay());
            sb.AppendLine("</ul>");
            return EndBody(sb);
        }

        private static StringBuilder BeginBody()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style='text-align:center;direction:rtl'>");
            sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='100%'>");
            sb.AppendLine("<tr style='background: #000aa0'><td colspan='2'><div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>گروه صنعتی هوایار</div></td></tr>");
            sb.AppendLine("<tr><td colspan='2'><div style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
            sb.AppendLine("با سلام و احترام <br />");
            return sb;
        }

        private static string EndBody(StringBuilder sb)
        {
            sb.AppendLine("</div></td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private static void AppendItem(StringBuilder sb, string label, string? value)
            => sb.AppendLine($"<li>{label} : <strong>{WebUtility.HtmlEncode(value.HasValue() ? value!.Trim() : "-")}</strong></li>");

        #endregion

        #region Helpers — لاگ

        private static Task LogInfoAsync(IJobLogger? logger, string message, CancellationToken cn)
            => logger?.LogInfoAsync(message, cn) ?? Task.CompletedTask;

        private static Task LogWarningAsync(IJobLogger? logger, string message, CancellationToken cn)
            => logger?.LogWarningAsync(message, 0, cn) ?? Task.CompletedTask;

        #endregion
    }
}
