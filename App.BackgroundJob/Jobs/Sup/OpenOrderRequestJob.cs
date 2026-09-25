using App.BackgroundJob.Jobs.Inv;
using Common.Attributes;
using Common.Entities;
using Common.Utilities;
using Data;
using Data.Contracts;
using Data.Services.Sup;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Entities.App.FIN;
using Entities.App.Inv;
using Entities.App.Inv.Enums;
using Entities.App.Pln;
using Entities.App.Sale;
using Entities.App.Sup;
using Entities.App.Sup.Enums;
using Entities.Auth;
using Entities.Base;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Entities.Hts.Edms;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using Services.Job;
using System.Data;
using System.Globalization;
using System.Text;

namespace App.BackgroundJob.Jobs.Sup
{
    public class OpenOrderRequestJob(
        ApplicationDbContext dbContext,
        RahkaranDbContext rdb,
        IUnitOfWork unitOfWork,
        HtsDbContext htsDb,
        IFileService fileService)
    {
        [JobHandler("هماهنگ کردن اطلاعات درخواست های باز از راهکاران")]
        public async Task SyncOpenOrderRequestJobFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
        {
            try
            {
                // HtsTaskService: BaseHamkaranTask فقط بین ۰۶:۰۰ تا ۲۱:۰۰ (ساعت ایران)
                var iranTz = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
                var iranNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, iranTz);
                var localHour = iranNow.Hour;
                if (localHour < 6 || localHour > 21)
                {
                    jobLogger?.LogInfoAsync($"SyncOpenOrderRequestJobFromRahkaran skipped (IranHour={localHour}; runs only 06:00–21:00 Iran).");
                    return;
                }

                jobLogger?.LogInfoAsync("Starting SyncOpenOrderRequestJobFromRahkaran...");

                // 1. Prepare Date Parameters
                var now = DateTime.Now;
                var persianYear = now.ToShamsiDate().Substring(0, 4);
                int currentYear = int.Parse(persianYear);
                var yearNumbers = $"{currentYear - 2},{currentYear - 1},{currentYear}";

                // 2. Load Lookups
                var partsDict = await unitOfWork.Repository<Part>().TableNoTracking
                    .Where(p => p.HamkaranId != null)
                    .Select(p => new { Id = p.Id!.Value, HamkaranId = p.HamkaranId!.Value })
                    .ToDictionaryAsync(x => x.HamkaranId, x => x.Id, cn);

                var dlsDict = await unitOfWork.Repository<DL>().TableNoTracking
                     .Where(d => d.HamkaranId != null)
                     .Select(d => new { Id = d.Id!.Value, HamkaranId = d.HamkaranId!.Value })
                     .ToDictionaryAsync(x => x.HamkaranId, x => x.Id, cn);

                // Load ProductionOrders with their related DL
                var productionOrdersDict = await unitOfWork.Repository<ProductionOrder>().TableNoTracking
                    .Where(po => po.ProjectDlId != null)
                    .Include(po => po.ProjectDl)
                    .Where(po => po.ProjectDl != null && po.ProjectDl.HamkaranId != null)
                    .GroupBy(po => po.ProjectDl!.HamkaranId!.Value)
                    .Select(g => new
                    {
                        DlHamkaranId = g.Key,
                        ProductionOrder = g.OrderByDescending(po => po.Id).FirstOrDefault()
                    })
                    .ToDictionaryAsync(x => x.DlHamkaranId, x => x.ProductionOrder, cn);

                // 3. Fetch Data from ERP
                jobLogger?.LogInfoAsync("Fetching Rahkaran Orders...");
                var hamkaranOrders = await GetRahkaranOrders(yearNumbers);
                jobLogger?.LogInfoAsync($"Fetched {hamkaranOrders.Count} orders.");

                // 4. Fetch Local Data
                var localOrders = await unitOfWork.Repository<OpenOrderRequest>().Table.ToListAsync(cn);
                var insertedThisRun = new List<OpenOrderRequest>();
                // HTS active pairs — never revive a soft-deleted local row unless this key is active in HTS
                var htsActiveKeysForSync = await LoadHtsActiveOpenOrderKeysAsync(cn);

                // 5. Sync Loop (Insert/Update)
                foreach (var remoteOrder in hamkaranOrders)
                {
                    try
                    {
                        // Find matching local record
                        OpenOrderRequest? localOrder = null;

                        if (remoteOrder.OrderRowId.HasValue)
                        {
                            localOrder = localOrders.FirstOrDefault(x =>
                                x.PurchaseRequestItemId == remoteOrder.PurchaseRequestItemId &&
                                x.OrderRowId == remoteOrder.OrderRowId);

                            if (localOrder == null)
                            {
                                localOrder = localOrders.FirstOrDefault(x => x.PurchaseRequestItemId == remoteOrder.PurchaseRequestItemId && x.OrderRowId == null);
                                if (localOrder != null)
                                {
                                    if (localOrder.PurchaseRequestMiladiDate != remoteOrder.PurchaseRequestDate)
                                        localOrder = null;
                                }
                            }
                        }
                        else
                        {
                            localOrder = localOrders.FirstOrDefault(x => x.PurchaseRequestItemId == remoteOrder.PurchaseRequestItemId);
                        }

                        // Resolve Foreign Keys
                        if (!remoteOrder.PartRef.HasValue || !partsDict.TryGetValue(remoteOrder.PartRef.Value, out var partId))
                        {
                            continue; // Part mandatory
                        }

                        long? dlId = null;
                        long? productionOrderId = null;
                        int? productionOrderNumber = null;

                        if (remoteOrder.DlRef.HasValue   && remoteOrder.DlRef > 0)
                        {
                            if (dlsDict.TryGetValue((long)remoteOrder.DlRef, out var dId))
                            {
                                dlId = dId;
                            }

                            // Map ProductionOrder from local DB based on DlRef
                            if (productionOrdersDict.TryGetValue((long)remoteOrder.DlRef, out var prodOrder) && prodOrder != null)
                            {
                                productionOrderId = prodOrder.Id;
                                if (int.TryParse(prodOrder.Number, out var poNum))
                                {
                                    productionOrderNumber = poNum;
                                }
                            }
                        }

                        if (localOrder == null)
                        {
                            // INSERT
                            var newOrder = new OpenOrderRequest
                            {
                                PurchaseRequestItemId = remoteOrder.PurchaseRequestItemId,
                                OrderRowId = remoteOrder.OrderRowId,
                                Year = remoteOrder.Year,
                                OrderNo = remoteOrder.OrderNo?.ToLong(),

                                OrderMiladiDate = remoteOrder.OrderDateInEurope,
                                DlId = dlId,
                                PartId = partId,
                                NeedDateShamsiDate = !remoteOrder.NeedDate.HasValue ? null : remoteOrder.NeedDate.Value.ToShamsiDate(),
                                NeedDateMiladiDate = remoteOrder.NeedDate,
						   SumSendQty = (long?)remoteOrder.SumSendQty,
                                FactoredCount = (int)remoteOrder.FactoredCount,
                                OrderQty = (int?)remoteOrder.OrderQty,
                                RequiredQty = (int)remoteOrder.ReqQty,
                                OrderItemComment = remoteOrder.OrdItmComment,
                                PurchaseRequestNumber = remoteOrder.PurchaseRequestNumber?.ToLong() ?? 0,

                                ProductionOrderNumber = productionOrderNumber,
                                ProductionOrderId = productionOrderId,
                                RelatedPartId = remoteOrder.RelatedPartId.HasValue && partsDict.TryGetValue(remoteOrder.RelatedPartId.Value, out var rpId) ? rpId : null,

                                IsDeleted = false,
                                IsForceDeletedByUser = false
                            };

                            if (remoteOrder.OrderDate.HasValue)
                            {
                                newOrder.OrderShamsiDate = remoteOrder.OrderDate.Value.ToShamsiDate();
                                newOrder.OrderMiladiDate = remoteOrder.OrderDate;

                            }

                                if (remoteOrder.FinalVoucherDate.HasValue)
                                {
                                    newOrder.FinalInventoryVoucherShamsiDate = remoteOrder.FinalVoucherDate.Value.ToShamsiDate();
                                    newOrder.FinalInventoryVoucherMiladiDate = remoteOrder.FinalVoucherDate;
                                }

                            if (remoteOrder.DeliveryDate.HasValue)
                            {
                                newOrder.DeliveryVoucherShamsiDate = remoteOrder.DeliveryDate.Value.ToShamsiDate();
                                newOrder.DeliveryVoucherMiladiDate = remoteOrder.DeliveryDate;
                            }

                            if (remoteOrder.TemporaryVoucherDate.HasValue)
                            {
                                newOrder.TemporaryInventoryVoucherShamsiDate = remoteOrder.TemporaryVoucherDate.Value.ToShamsiDate();
                                newOrder.TemporaryInventoryVoucherMiladiDate = remoteOrder.TemporaryVoucherDate;
                            }

                            if (remoteOrder.OrderConfirmDate.HasValue)
                            {
                                newOrder.OrderConfirmShamsiDate = remoteOrder.OrderConfirmDate.Value.ToShamsiDate();
							 newOrder.OrderConfirmMiladiDate = remoteOrder.OrderConfirmDate.Value;
							}

                            if (remoteOrder.PurchaseRequestDate.HasValue)
                            {
                                newOrder.PurchaseRequestShamsiDate = remoteOrder.PurchaseRequestDate.Value.ToShamsiDate();
                                newOrder.PurchaseRequestMiladiDate = remoteOrder.PurchaseRequestDate;
                            }

                            await unitOfWork.Repository<OpenOrderRequest>().AddAsync(newOrder, cn);
                            localOrders.Add(newOrder);
                            insertedThisRun.Add(newOrder);
                        }
                        else
                        {
                            // UPDATE
                            if (localOrder.IsForceDeletedByUser) continue;

                            bool updated = false;

                            if (localOrder.Year != remoteOrder.Year) updated = true;
                            if (localOrder.PurchaseRequestNumber != (remoteOrder.PurchaseRequestNumber?.ToLong() ?? 0)) updated = true;

                            if (localOrder.FactoredCount != (int)remoteOrder.FactoredCount) updated = true;
                            if (localOrder.PartId != partId) updated = true;

                            long? newRelatedPartId = remoteOrder.RelatedPartId.HasValue && partsDict.TryGetValue(remoteOrder.RelatedPartId.Value, out var nrpId) ? nrpId : null;
                            if (localOrder.RelatedPartId != newRelatedPartId) updated = true;
                            if (localOrder.DlId != dlId) updated = true;

                            var remNeedDate = !remoteOrder.NeedDate.HasValue ? null : remoteOrder.NeedDate.Value.ToShamsiDate();
                           
                           if ((localOrder.NeedDateShamsiDate ?? "").Trim() != (remNeedDate ?? "").Trim()) updated = true;

                            if (localOrder.SumSendQty != (int?)remoteOrder.SumSendQty) updated = true;
                            if (localOrder.OrderQty != (int?)remoteOrder.OrderQty) updated = true;
                            if (localOrder.RequiredQty != (int)remoteOrder.ReqQty) updated = true;
                            if (localOrder.OrderItemComment != remoteOrder.OrdItmComment) updated = true;
                            if (localOrder.ProductionOrderNumber != productionOrderNumber) updated = true;
                            if (localOrder.ProductionOrderId != productionOrderId) updated = true;
                            if (localOrder.DeliveryVoucherMiladiDate != remoteOrder.DeliveryDate) updated = true;
                            if (localOrder.TemporaryInventoryVoucherMiladiDate != remoteOrder.TemporaryVoucherDate) updated = true;
                            if (localOrder.PurchaseRequestMiladiDate != remoteOrder.PurchaseRequestDate) updated = true;
                            if (localOrder.FinalInventoryVoucherMiladiDate != remoteOrder.FinalVoucherDate) updated = true;
                            if (remoteOrder.OrderConfirmDate.HasValue && localOrder.OrderConfirmShamsiDate != remoteOrder.OrderConfirmDate.Value.ToShamsiDate()) updated = true;
                            if (localOrder.OrderRowId != remoteOrder.OrderRowId) updated = true;

                            if (updated)
                            {
                                // Do not reactivate when HTS has this (PR,OrderRow) deleted / absent from active set
                                var syncKey = (localOrder.PurchaseRequestItemId, remoteOrder.OrderRowId ?? localOrder.OrderRowId ?? 0L);
                                if (!localOrder.IsDeleted || htsActiveKeysForSync.Contains(syncKey))
                                {
                                    localOrder.IsDeleted = false;
                                    localOrder.IsDeletedMiladiDate = null;
                                    localOrder.IsDeletedShamsiDate = null;
                                }
                                localOrder.Year = remoteOrder.Year;
                                localOrder.PurchaseRequestNumber = remoteOrder.PurchaseRequestNumber?.ToLong() ?? 0;

                                localOrder.OrderRowId = remoteOrder.OrderRowId;
                                localOrder.OrderNo = remoteOrder.OrderNo?.ToLong();
                                if (remoteOrder.OrderDate.HasValue)
                                    localOrder.OrderShamsiDate = remoteOrder.OrderDate.Value.ToShamsiDate();
                                localOrder.OrderMiladiDate = remoteOrder.OrderDateInEurope;
                                localOrder.DlId = dlId;
                                localOrder.PartId = partId;


                                localOrder.SumSendQty = (int?)remoteOrder.SumSendQty;
                                localOrder.FactoredCount = (int)remoteOrder.FactoredCount;
                                localOrder.OrderQty = (int?)remoteOrder.OrderQty;
                                localOrder.RequiredQty = (int)remoteOrder.ReqQty;
                                localOrder.OrderItemComment = remoteOrder.OrdItmComment;
                                localOrder.ProductionOrderNumber = productionOrderNumber;
                                localOrder.ProductionOrderId = productionOrderId;
                                localOrder.RelatedPartId = newRelatedPartId;

                                localOrder.NeedDateShamsiDate = remNeedDate;
                                        localOrder.NeedDateMiladiDate = remoteOrder.NeedDate;

								if (remoteOrder.DeliveryDate.HasValue)
                                {
                                    localOrder.DeliveryVoucherShamsiDate = remoteOrder.DeliveryDate.Value.ToShamsiDate();
                                    localOrder.DeliveryVoucherMiladiDate = remoteOrder.DeliveryDate;
                                }

                                if (remoteOrder.TemporaryVoucherDate.HasValue)
                                {
                                    localOrder.TemporaryInventoryVoucherShamsiDate = remoteOrder.TemporaryVoucherDate.Value.ToShamsiDate();
                                    localOrder.TemporaryInventoryVoucherMiladiDate = remoteOrder.TemporaryVoucherDate;
                                }

                                if (remoteOrder.OrderConfirmDate.HasValue)
                                {
                                    localOrder.OrderConfirmShamsiDate = remoteOrder.OrderConfirmDate.Value.ToShamsiDate();
                                    localOrder.OrderConfirmMiladiDate = remoteOrder.OrderConfirmDate.Value;

                                }

                                if (remoteOrder.PurchaseRequestDate.HasValue)
                                {
                                    localOrder.PurchaseRequestShamsiDate = remoteOrder.PurchaseRequestDate.Value.ToShamsiDate();
                                    localOrder.PurchaseRequestMiladiDate = remoteOrder.PurchaseRequestDate;
                                }



                                if (remoteOrder.FinalVoucherDate.HasValue)
                                {
                                    localOrder.FinalInventoryVoucherShamsiDate = remoteOrder.FinalVoucherDate.Value.ToShamsiDate();
                                    localOrder.FinalInventoryVoucherMiladiDate = remoteOrder.FinalVoucherDate;
                                }

                                if(localOrder.Comment == null || !localOrder.Comment.Contains("|| بروزرسانی سیستمی"))
                                     localOrder.Comment += "|| بروزرسانی سیستمی";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        jobLogger?.LogInfoAsync($"Error processing item {remoteOrder.PurchaseRequestItemId}: {ex.Message}");
                    }
                }

                await unitOfWork.SaveChangesAsync(cn);

                // VPIS auto-insert (HTS «درج بصورت اتوماتیک توسط سیستم») — فقط برای درخواست‌های تازه‌درج‌شده
                if (insertedThisRun.Count > 0)
                {
                    var insertedIds = insertedThisRun.Where(o => o.Id.HasValue).Select(o => o.Id!.Value).ToList();
                    var insertedLoaded = await unitOfWork.Repository<OpenOrderRequest>().Table
                        .Include(o => o.ProductionOrder)
                        .Where(o => insertedIds.Contains(o.Id!.Value))
                        .ToListAsync(cn);
                    await AutoInsertVpisLinksForRequestsAsync(insertedLoaded, jobLogger, cn);
                }

                // 6. Compute Logic (Missing items = Deleted)
                var relevantYears = yearNumbers.Split(',').Select(long.Parse).ToList();
                var activeLocalOrders = localOrders.Where(x => !x.IsDeleted && !x.IsForceDeletedByUser && relevantYears.Contains(x.Year)).ToList();
                var remoteKeys = hamkaranOrders.Select(x => new { x.PurchaseRequestItemId, OrderRowId = x.OrderRowId ?? 0 }).ToHashSet();

                foreach (var local in activeLocalOrders)
                {
                    var key = new { local.PurchaseRequestItemId, OrderRowId = local.OrderRowId ?? 0 };
                    if (!remoteKeys.Contains(key))
                    {
                        local.IsDeleted = true;
						local.IsDeletedMiladiDate = now;
						local.IsDeletedShamsiDate = now.ToShamsiDateTime();
					}
                }

                await unitOfWork.SaveChangesAsync(cn);

                // 7. Compute Additional Logic (From Stored Procedure)
                await ComputeOpenOrderRequest(yearNumbers, jobLogger, cn);

                // 8. Notifications (رسید کالا / رد QC) — یک‌بار به ازای هر نفر، کالا و رسید
                await SendNotifications(yearNumbers, jobLogger, cn);

                // 9. تایید خودکار مهندسی برای درخواست‌های روتین دارای مدرک (معادل سیستم قدیم)
                await AutoAcceptEngineeringForRoutineParts(jobLogger, cn);

            }
            catch (Exception ex)
            {
                if (jobLogger != null)
                {
                    await jobLogger.LogInfoAsync(ex.Message, cn);
                }
                throw;
            }
        }

        private const string ArrivalNotificationTitle = "رسید کالای درخواستی در انبار";
        private const string PartialArrivalNotificationTitle = "رسید قسمتی از کالای درخواستی در انبار";
        private const string QcRejectionNotificationTitle = "عدم تایید کالا توسط QC";
        private const string AutoEngineeringAcceptTitle = "اعلان ثبت پیوست درخواست باز";
        private const string ReceiptQueryParam = "receipt=";
        private const string ReceiptBodyMarkerPrefix = "<!--receipt:";
        private const string LegacyReceiptKey = "*";
        private const string QcReceiptKey = "qc";

        private const string ArrivalWarehouseGroupCode = "Sup.OpenOrderRequest.ArrivalWarehouse";
        private const string ArrivalPartialGroupCode = "Sup.OpenOrderRequest.ArrivalPartial";
        private const string QcRejectionGroupCode = "Sup.OpenOrderRequest.QcRejection";

        private async Task SendNotifications(string yearNumbers, IJobLogger? logger, CancellationToken cn)
        {
            var newEvents = await GetNewInvVchForNotify(yearNumbers);
            if (newEvents.Count == 0)
            {
                logger?.LogInfoAsync("No inventory voucher events for notification.");
                return;
            }

            var users = await dbContext.Users.AsNoTracking().ToListAsync(cn);
            var userLookup = BuildUserLookup(users);
            var arrivalEmails = await LoadGroupEmailsAsync(ArrivalWarehouseGroupCode, cn);
            var partialEmails = await LoadGroupEmailsAsync(ArrivalPartialGroupCode, cn);
            var qcEmails = await LoadGroupEmailsAsync(QcRejectionGroupCode, cn);
            if (arrivalEmails.Length == 0)
                logger?.LogInfoAsync($"گروه اعلان «{ArrivalWarehouseGroupCode}» عضو ندارد (WP8).");
            if (partialEmails.Length == 0)
                logger?.LogInfoAsync($"گروه اعلان «{ArrivalPartialGroupCode}» عضو ندارد (WP8).");
            if (qcEmails.Length == 0)
                logger?.LogInfoAsync($"گروه اعلان «{QcRejectionGroupCode}» عضو ندارد (WP8).");

            var localOrders = await unitOfWork.Repository<OpenOrderRequest>().Table
                .Where(x => !x.IsForceDeletedByUser && !x.IsDeleted)
                .ToListAsync(cn);

            if (localOrders.Count == 0)
                return;

            var orderIds = localOrders.Where(o => o.Id.HasValue).Select(o => o.Id!.Value).ToList();
            var existingNotifications = new List<(long EntityId, long OwnerId, string Title, string? ViewPath, string? Body)>();
            foreach (var batch in orderIds.Chunk(1000))
            {
                var batchIds = batch.ToList();
                var batchRows = await unitOfWork.Repository<Notification>().TableNoTracking
                    .Where(n => n.EntityId != null
                                && batchIds.Contains(n.EntityId.Value)
                                && (n.Title == ArrivalNotificationTitle
                                    || n.Title == PartialArrivalNotificationTitle
                                    || n.Title == QcRejectionNotificationTitle))
                    .Select(n => new { n.EntityId, n.OwnerId, n.Title, n.ViewPath, n.Body })
                    .ToListAsync(cn);

                existingNotifications.AddRange(batchRows.Select(n => (n.EntityId!.Value, n.OwnerId, n.Title, n.ViewPath, n.Body)));
            }

            var sentKeys = existingNotifications
                .Select(n => (n.EntityId, n.OwnerId, n.Title, ReceiptKey: ExtractReceiptKey(n.ViewPath, n.Body) ?? LegacyReceiptKey))
                .ToHashSet();

            var eventsByPurchaseItem = newEvents
                .GroupBy(e => e.PurchaseRequestItemID)
                .ToDictionary(g => g.Key, g => g.ToList());

            var createdCount = 0;
            var skippedCount = 0;

            foreach (var order in localOrders)
            {
                try
                {
                    if (!eventsByPurchaseItem.TryGetValue(order.PurchaseRequestItemId, out var orderEvents))
                        continue;

                    orderEvents = FilterEventsForOrder(order, orderEvents);
                    if (orderEvents.Count == 0)
                        continue;

                    createdCount += await SendQcRejectionNotifications(order, orderEvents, userLookup, qcEmails, sentKeys, cn, incrementSkipped: () => skippedCount++);

                    createdCount += await SendArrivalNotifications(order, orderEvents, userLookup, arrivalEmails, partialEmails, sentKeys, cn, incrementSkipped: () => skippedCount++);
                }
                catch (Exception ex)
                {
                    if (logger != null)
                        await logger.LogInfoAsync($"Error creating notification for {order.PurchaseRequestItemId}: {ex.Message}", cn);
                }
            }

            await unitOfWork.SaveChangesAsync(cn);
            logger?.LogInfoAsync($"Notifications created={createdCount}, skipped(already sent)={skippedCount}");
        }

        private static List<HamkaranNotifyDto> FilterEventsForOrder(OpenOrderRequest order, List<HamkaranNotifyDto> events)
        {
            var prNumber = order.PurchaseRequestNumber.ToString();
            var matches = events.Where(e =>
                string.Equals((e.PurchaseRequestNumber ?? string.Empty).Trim(), prNumber, StringComparison.OrdinalIgnoreCase));

            if (order.OrderRowId.HasValue)
                matches = matches.Where(e => e.SendRefNo == order.OrderRowId);

            return matches.ToList();
        }

        private async Task<int> SendQcRejectionNotifications(
            OpenOrderRequest order,
            List<HamkaranNotifyDto> orderEvents,
            UserLookup userLookup,
            string[] qcExtraEmails,
            HashSet<(long EntityId, long OwnerId, string Title, string ReceiptKey)> sentKeys,
            CancellationToken cn,
            Action incrementSkipped)
        {
            if (order.IsRejectedByInspection)
                return 0;

            var rejectedItems = orderEvents.Where(e => e.InspctnFlag == 0).ToList();
            if (rejectedItems.Count == 0)
                return 0;

            var recipientIds = ResolveRecipientUserIds(
                order,
                userLookup,
                includeRequestedPersonnel: true,
                extraEmails: qcExtraEmails,
                includeSalesUnit: false,
                includePairedExtras: false);

            if (recipientIds.Count == 0)
            {
                order.IsRejectedByInspection = true;
                return 0;
            }

            var body = BuildQcRejectionBody(order, rejectedItems);
            var created = 0;

            foreach (var userId in recipientIds)
            {
                if (WasNotificationAlreadySent(sentKeys, order.Id!.Value, userId, QcRejectionNotificationTitle, QcReceiptKey))
                {
                    incrementSkipped();
                    continue;
                }

                await AddReceiptNotification(order, userId, QcRejectionNotificationTitle, body, QcReceiptKey, cn);
                MarkNotificationSent(sentKeys, order.Id!.Value, userId, QcRejectionNotificationTitle, QcReceiptKey);
                created++;
            }

            order.IsRejectedByInspection = true;
            return created;
        }

        private async Task<int> SendArrivalNotifications(
            OpenOrderRequest order,
            List<HamkaranNotifyDto> orderEvents,
            UserLookup userLookup,
            string[] arrivalExtraEmails,
            string[] partialExtraEmails,
            HashSet<(long EntityId, long OwnerId, string Title, string ReceiptKey)> sentKeys,
            CancellationToken cn,
            Action incrementSkipped)
        {
            var confirmedReceipts = orderEvents
                .Where(e => e.FinalVchItemId.HasValue)
                .GroupBy(e => e.FinalVchItemId!.Value)
                .Select(g => g.First())
                .ToList();

            if (confirmedReceipts.Count == 0)
                return 0;

            var trackedPaperIds = ParsePaperIds(order.NotifyEmailSendPaperIds);
            var newReceipts = confirmedReceipts
                .Where(r => !IsReceiptAlreadyTracked(trackedPaperIds, r))
                .ToList();

            var isFullyReceived = order.RequiredQty == order.FactoredCount && order.FactoredCount > 0;

            if (newReceipts.Count == 0)
            {
                if (isFullyReceived)
                    MarkOrderFullyReceived(order, confirmedReceipts);
                return 0;
            }

            var title = isFullyReceived ? ArrivalNotificationTitle : PartialArrivalNotificationTitle;

            if (!isFullyReceived && order.FactoredCount <= 0)
                return 0;

            var extraEmails = isFullyReceived
                ? arrivalExtraEmails.Concat(GetPairedExtraEmails(order)).ToArray()
                : partialExtraEmails;

            var recipientIds = ResolveRecipientUserIds(
                order,
                userLookup,
                includeRequestedPersonnel: true,
                extraEmails: extraEmails,
                includeSalesUnit: isFullyReceived,
                includePairedExtras: isFullyReceived);

            var created = 0;
            var allowLegacyWildcard = trackedPaperIds.Count == 0;
            var newlyTrackedKeys = new List<string>();

            foreach (var receipt in newReceipts)
            {
                var receiptKey = GetReceiptKey(receipt);
                var body = BuildArrivalBody(order, new[] { receipt });
                var sentToAnyone = false;

                foreach (var userId in recipientIds)
                {
                    if (WasArrivalAlreadySent(sentKeys, order.Id!.Value, userId, title, receiptKey, allowLegacyWildcard))
                    {
                        incrementSkipped();
                        sentToAnyone = true;
                        continue;
                    }

                    await AddReceiptNotification(order, userId, title, body, receiptKey, cn);
                    MarkNotificationSent(sentKeys, order.Id!.Value, userId, title, receiptKey);
                    sentToAnyone = true;
                    created++;
                }

                if (sentToAnyone || (isFullyReceived && recipientIds.Count == 0))
                    newlyTrackedKeys.AddRange(GetTrackablePaperIds(receipt));
            }

            if (newlyTrackedKeys.Count > 0)
            {
                foreach (var key in newlyTrackedKeys)
                    trackedPaperIds.Add(key);

                order.NotifyEmailSendPaperIds = TruncatePaperIds(string.Join("|", trackedPaperIds));
                order.NotifyEmailRequestedPersonel = recipientIds.Count > 0;
            }

            if (isFullyReceived)
                MarkOrderFullyReceived(order, newReceipts);

            return created;
        }

        private static void MarkOrderFullyReceived(OpenOrderRequest order, List<HamkaranNotifyDto> receipts)
        {
            var now = DateTime.Now;
            var factoredDate = receipts.Select(r => r.FactoredDate).FirstOrDefault(d => d.HasValue) ?? now;
            order.IsDeleted = true;
            order.IsDeletedMiladiDate = now;
            order.IsDeletedShamsiDate = now.ToShamsiDateTime();
            order.FactoredMiladiDate = factoredDate;
            order.FactoredShamsiDate = factoredDate.ToShamsiDate();
            if (string.IsNullOrEmpty(order.CompletionShamsiDate))
            {
                order.CompletionMiladiDate = factoredDate;
                order.CompletionShamsiDate = factoredDate.ToShamsiDate();
            }
        }

        private async Task AddReceiptNotification(
            OpenOrderRequest order,
            long userId,
            string title,
            string body,
            string receiptKey,
            CancellationToken cn)
        {
            await unitOfWork.Repository<Notification>().AddAsync(new Notification
            {
                Type = NotificationType.Email,
                Title = title,
                Body = $"{body}{ReceiptBodyMarkerPrefix}{receiptKey}-->",
                EntityId = order.Id,
                OwnerId = userId,
                ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={order.Id}&{ReceiptQueryParam}{receiptKey}",
                IsRead = false
            }, cn);
        }

        private static bool WasNotificationAlreadySent(
            HashSet<(long EntityId, long OwnerId, string Title, string ReceiptKey)> sentKeys,
            long entityId,
            long ownerId,
            string title,
            string receiptKey)
        {
            return sentKeys.Contains((entityId, ownerId, title, receiptKey));
        }

        private static bool WasArrivalAlreadySent(
            HashSet<(long EntityId, long OwnerId, string Title, string ReceiptKey)> sentKeys,
            long entityId,
            long ownerId,
            string title,
            string receiptKey,
            bool allowLegacyWildcard)
        {
            if (WasNotificationAlreadySent(sentKeys, entityId, ownerId, title, receiptKey)
                || WasNotificationAlreadySent(sentKeys, entityId, ownerId, ArrivalNotificationTitle, receiptKey)
                || WasNotificationAlreadySent(sentKeys, entityId, ownerId, PartialArrivalNotificationTitle, receiptKey))
                return true;

            if (!allowLegacyWildcard)
                return false;

            return sentKeys.Contains((entityId, ownerId, ArrivalNotificationTitle, LegacyReceiptKey))
                   || sentKeys.Contains((entityId, ownerId, PartialArrivalNotificationTitle, LegacyReceiptKey))
                   || sentKeys.Contains((entityId, ownerId, title, LegacyReceiptKey));
        }

        private static void MarkNotificationSent(
            HashSet<(long EntityId, long OwnerId, string Title, string ReceiptKey)> sentKeys,
            long entityId,
            long ownerId,
            string title,
            string receiptKey)
        {
            sentKeys.Add((entityId, ownerId, title, receiptKey));
        }

        private static HashSet<string> ParsePaperIds(string? paperIds)
        {
            if (string.IsNullOrWhiteSpace(paperIds))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return paperIds
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsReceiptAlreadyTracked(HashSet<string> trackedPaperIds, HamkaranNotifyDto receipt)
        {
            if (receipt.FinalVchItemId.HasValue && trackedPaperIds.Contains(receipt.FinalVchItemId.Value.ToString()))
                return true;

            return !string.IsNullOrWhiteSpace(receipt.SendNo) && trackedPaperIds.Contains(receipt.SendNo.Trim());
        }

        private static string GetReceiptKey(HamkaranNotifyDto receipt)
        {
            if (receipt.FinalVchItemId.HasValue)
                return receipt.FinalVchItemId.Value.ToString();

            return string.IsNullOrWhiteSpace(receipt.SendNo) ? LegacyReceiptKey : receipt.SendNo.Trim();
        }

        private static IEnumerable<string> GetTrackablePaperIds(HamkaranNotifyDto receipt)
        {
            if (receipt.FinalVchItemId.HasValue)
                yield return receipt.FinalVchItemId.Value.ToString();

            if (!string.IsNullOrWhiteSpace(receipt.SendNo))
                yield return receipt.SendNo.Trim();
        }

        private static string TruncatePaperIds(string value)
        {
            const int maxLength = 2048;
            if (value.Length <= maxLength)
                return value;

            var parts = value.Split('|');
            var builder = new StringBuilder();
            for (var i = parts.Length - 1; i >= 0; i--)
            {
                var next = parts[i];
                if (builder.Length == 0)
                {
                    if (next.Length <= maxLength)
                        builder.Insert(0, next);
                    continue;
                }

                if (builder.Length + 1 + next.Length > maxLength)
                    break;

                builder.Insert(0, next + "|");
            }

            return builder.ToString();
        }

        private static string? ExtractReceiptKey(string? viewPath, string? body)
        {
            if (!string.IsNullOrEmpty(viewPath))
            {
                var idx = viewPath.IndexOf(ReceiptQueryParam, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var start = idx + ReceiptQueryParam.Length;
                    var end = viewPath.IndexOf('&', start);
                    var key = end < 0 ? viewPath[start..] : viewPath[start..end];
                    if (!string.IsNullOrWhiteSpace(key))
                        return key.Trim();
                }
            }

            if (!string.IsNullOrEmpty(body))
            {
                var idx = body.IndexOf(ReceiptBodyMarkerPrefix, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var start = idx + ReceiptBodyMarkerPrefix.Length;
                    var end = body.IndexOf("-->", start, StringComparison.Ordinal);
                    if (end > start)
                        return body[start..end].Trim();
                }
            }

            return null;
        }

        private sealed class UserLookup
        {
            public Dictionary<string, User> ByEmail { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, User> ByUsername { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private static UserLookup BuildUserLookup(List<User> users)
        {
            var lookup = new UserLookup();
            foreach (var user in users.Where(u => u.Id.HasValue))
            {
                if (!string.IsNullOrWhiteSpace(user.Email) && !lookup.ByEmail.ContainsKey(user.Email))
                    lookup.ByEmail[user.Email.Trim()] = user;

                if (!string.IsNullOrWhiteSpace(user.Username) && !lookup.ByUsername.ContainsKey(user.Username))
                    lookup.ByUsername[user.Username.Trim()] = user;
            }

            return lookup;
        }

        private static User? FindUser(UserLookup lookup, string emailOrUsername)
        {
            var clean = emailOrUsername.Trim();
            if (clean.Length == 0)
                return null;

            if (lookup.ByEmail.TryGetValue(clean, out var byEmail))
                return byEmail;

            var username = clean.Contains('@') ? clean.Split('@')[0] : clean;
            if (lookup.ByUsername.TryGetValue(username, out var byUsername))
                return byUsername;

            if (!clean.Contains('@', StringComparison.Ordinal) && lookup.ByEmail.TryGetValue(username + "@havayar.com", out var byConstructedEmail))
                return byConstructedEmail;

            return null;
        }

        private static HashSet<long> ResolveRecipientUserIds(
            OpenOrderRequest order,
            UserLookup userLookup,
            bool includeRequestedPersonnel,
            IEnumerable<string>? extraEmails,
            bool includeSalesUnit,
            bool includePairedExtras)
        {
            var ids = new HashSet<long>();

            if (includeRequestedPersonnel)
            {
                AddUserIdsFromEmails(ids, userLookup, order.RequestedPersonelEmail);
                AddUserIdsFromEmails(ids, userLookup, order.RequestedEngineeringPersonelEmail);
                AddUserIds(ids, order.RequestedPersonelIds);
                AddUserIds(ids, order.RequestedEngineeringPersonelIds);
            }

            if (includeSalesUnit)
            {
                AddUserId(ids, order.SalesUnitSalesExpertId);
                AddUserId(ids, order.SalesUnitSalesManagerId);
                AddUserId(ids, order.SalesUnitProjectManagerId);
            }

            if (extraEmails != null)
                AddUserIdsFromEmails(ids, userLookup, string.Join(";", extraEmails));

            if (includePairedExtras)
                AddUserIdsFromEmails(ids, userLookup, string.Join(";", GetPairedExtraEmails(order)));

            return ids;
        }

        private async Task<string[]> LoadGroupEmailsAsync(string groupCode, CancellationToken cn)
        {
            var emails = await dbContext.NotificationGroupMembers
                .AsNoTracking()
                .Where(m => m.NotificationGroup.Code == groupCode && m.IsActive == IsActiveEnum.Active)
                .Select(m => m.Email)
                .ToListAsync(cn);

            return emails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static IEnumerable<string> GetPairedExtraEmails(OpenOrderRequest order)
        {
            var emails = $"{order.RequestedPersonelEmail};{order.RequestedEngineeringPersonelEmail}".ToLowerInvariant();
            if (emails.Contains("asadpour.h"))
                yield return "khodabandeh.f@havayar.com";
            else if (emails.Contains("khodabandeh.f"))
                yield return "asadpour.h@havayar.com";
            else if (emails.Contains("vahedian.s"))
                yield return "daneshmandi.m@havayar.com";
        }

        private static void AddUserIdsFromEmails(HashSet<long> ids, UserLookup lookup, string? emails)
        {
            if (string.IsNullOrWhiteSpace(emails))
                return;

            foreach (var part in emails.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var user = FindUser(lookup, part);
                if (user?.Id != null)
                    ids.Add(user.Id.Value);
            }
        }

        private static void AddUserIds(HashSet<long> ids, List<long>? source)
        {
            if (source == null)
                return;

            foreach (var id in source.Where(x => x > 0))
                ids.Add(id);
        }

        private static void AddUserId(HashSet<long> ids, long? id)
        {
            if (id.HasValue && id.Value > 0)
                ids.Add(id.Value);
        }

        private static string BuildArrivalBody(OpenOrderRequest order, IEnumerable<HamkaranNotifyDto> receipts)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style='text-align:center;direction:rtl'>");
            sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='100%'>");
            sb.AppendLine("<tr style='background: #000aa0'><td colspan='2'><div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center'>گروه صنعتی هوایار</div></td></tr>");
            sb.AppendLine("<tr><td colspan='2'><div style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
            sb.AppendLine("با سلام و احترام <br />");
            sb.AppendLine("کاربر گرامی کالای درخواستی شما طبق مشخصات ذیل وارد کارخانه شد</br></br>");

            foreach (var receipt in receipts)
            {
                var sendDate = receipt.SendDate.HasValue ? receipt.SendDate.Value.ToShamsiDate() : "-";
                sb.AppendLine("<ul style='margin-bottom:20px'>");
                sb.AppendLine($"<li>شماره درخواست : <strong>{receipt.PurchaseRequestNumber ?? order.PurchaseRequestNumber.ToString()}</strong></li>");
                sb.AppendLine($"<li>کد کالا : <strong>{receipt.PartCode}</strong></li>");
                sb.AppendLine($"<li>شرح کالا : <strong>{receipt.PartName}</strong></li>");
                sb.AppendLine($"<li>توضیح کلی : <strong>{order.OrderItemComment}</strong></li>");
                sb.AppendLine($"<li>تاریخ ارسال : <strong>{sendDate}</strong></li>");
                sb.AppendLine($"<li>تعداد/مقدار : <strong>{receipt.SendQty ?? receipt.FinalVchQty}</strong></li>");
                sb.AppendLine($"<li>توضیح قلم : <strong>{receipt.Comment}</strong></li>");
                sb.AppendLine("</ul>");
            }

            sb.AppendLine("</div></td></tr></table></div>");
            return sb.ToString();
        }

        private static string BuildQcRejectionBody(OpenOrderRequest order, List<HamkaranNotifyDto> rejectedItems)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style='direction:rtl;text-align:right;padding:5px; margin:5px; font-family:tahoma; font-size: 11pt'>");
            sb.AppendLine($"کاربر گرامی، کالا با مشخصات ذیل، مربوط به سفارش ({rejectedItems[0].OrdNo ?? order.OrderNo?.ToString()}) توسط واحد کیفیت رد شد<br>");

            foreach (var item in rejectedItems)
            {
                var sendDate = item.SendDate.HasValue ? item.SendDate.Value.ToShamsiDate() : "-";
                sb.AppendLine("<ul>");
                sb.AppendLine($"<li>کد کالا : <strong>{item.PartCode}</strong></li>");
                sb.AppendLine($"<li>شرح کالا : <strong>{item.PartName}</strong></li>");
                sb.AppendLine($"<li>تامین کننده : <strong>{item.DlTempTitle}</strong></li>");
                sb.AppendLine($"<li>شرح واحد کیفیت : <strong>{item.InspectionComment}</strong></li>");
                sb.AppendLine($"<li>شماره برگه ارسال : <strong>{item.SendNo}</strong></li>");
                sb.AppendLine($"<li>تاریخ ارسال : <strong>{sendDate}</strong></li>");
                sb.AppendLine($"<li>تعداد/مقدار : <strong>{item.SendQty}</strong></li>");
                sb.AppendLine($"<li>توضیح قلم : <strong>{item.Comment}</strong></li>");
                sb.AppendLine("</ul><br /><br /><br />");
            }

            sb.AppendLine("</div>");
            return sb.ToString();
        }

        private async Task AutoAcceptEngineeringForRoutineParts(IJobLogger? logger, CancellationToken cn)
        {
            try
            {
                logger?.LogInfoAsync("Starting auto engineering accept for routine parts...");

                var documentTypes = new[]
                {
                    PartDocumentTypeEnum.Data_Sheet,
                    PartDocumentTypeEnum.Detail_Drawing,
                    PartDocumentTypeEnum.Wiring_Diagram,
                    PartDocumentTypeEnum.Technical_Documents
                };

                var partIdsWithRequiredDocs = await unitOfWork.Repository<Part>().TableNoTracking
                    .Where(p => p.Documents.Any(d => d.Main == true && d.Type != null && documentTypes.Contains(d.Type.Value)))
                    .Select(p => p.Id!.Value)
                    .ToListAsync(cn);

                var partIdsNotNeedingDocs = await unitOfWork.Repository<Part>().TableNoTracking
                    .Where(p => p.DocumentsNotRequired)
                    .Select(p => p.Id!.Value)
                    .ToListAsync(cn);

                var eligiblePartIds = partIdsWithRequiredDocs.Union(partIdsNotNeedingDocs).ToHashSet();
                if (eligiblePartIds.Count == 0)
                {
                    logger?.LogInfoAsync("No eligible parts for auto engineering accept.");
                    return;
                }

                var orders = await unitOfWork.Repository<OpenOrderRequest>().Table
                    .Include(o => o.Part)
                    .ThenInclude(p => p!.BuyCategory)
                    .Where(o => !o.IsDeleted
                                && !o.IsForceDeletedByUser
                                && !o.EngineeringAccept
                                && eligiblePartIds.Contains(o.PartId))
                    .ToListAsync(cn);

                if (orders.Count == 0)
                {
                    logger?.LogInfoAsync("No open orders pending auto engineering accept.");
                    return;
                }

                var partIds = orders.Select(o => o.PartId).Distinct().ToList();
                var leadTimes = await unitOfWork.Repository<LeadTime>().TableNoTracking
                    .Where(lt => lt.PartId != null && partIds.Contains(lt.PartId.Value))
                    .ToListAsync(cn);
                var leadTimeByPart = leadTimes
                    .GroupBy(lt => lt.PartId!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                var lastDocumentCreators = await unitOfWork.Repository<Part>().TableNoTracking
                    .Where(p => partIds.Contains(p.Id!.Value))
                    .SelectMany(p => p.Documents
                        .Where(d => d.Main == true && d.Type != null && documentTypes.Contains(d.Type.Value))
                        .Select(d => new { PartId = p.Id!.Value, d.Id, d.CreatedById }))
                    .ToListAsync(cn);
                var lastCreatorByPart = lastDocumentCreators
                    .GroupBy(d => d.PartId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First().CreatedById);

                var industrialUserIds = await dbContext.NotificationGroupMembers
                    .AsNoTracking()
                    .Where(m => m.NotificationGroup.Code == "Sup.OpenOrderRequest.Industrial"
                                && m.IsActive == IsActiveEnum.Active)
                    .Select(m => m.UserId)
                    .ToListAsync(cn);

                var now = DateTime.Now;
                var acceptedCount = 0;

                foreach (var order in orders)
                {
                    var notNeedDocuments = order.Part?.DocumentsNotRequired == true;
                    long? engineeringUserId = notNeedDocuments ? 1 : lastCreatorByPart.GetValueOrDefault(order.PartId);
                    if (!notNeedDocuments && engineeringUserId is null or <= 0)
                        continue;

                    order.Changed = true;
                    order.EngineeringAccept = true;
                    order.EngineeringAcceptUserId = engineeringUserId;
                    order.EngineeringConfirmationMiladiDateTime = now;
                    order.EngineeringAcceptShamsiDateTime = now.ToShamsiDateTime();
                    order.IsAcceptedAutomaticallyByEngineering = true;

                    if (leadTimeByPart.TryGetValue(order.PartId, out var leadTime) && leadTime.LeadTimeDay > 0)
                    {
                        order.SupplyMiladiDate = now.AddDays(leadTime.LeadTimeDay);
                        order.SupplyShamsiDate = order.SupplyMiladiDate.Value.ToShamsiDate();
                    }

                    var recipientIds = new HashSet<long>();
                    if (order.Part?.BuyCategory?.PurchaseResponsibleId is > 0)
                        recipientIds.Add(order.Part.BuyCategory.PurchaseResponsibleId.Value);

                    foreach (var industrialId in industrialUserIds)
                        recipientIds.Add(industrialId);

                    var body = BuildAutoEngineeringAcceptBody(order);
                    foreach (var userId in recipientIds)
                    {
                        await unitOfWork.Repository<Notification>().AddAsync(new Notification
                        {
                            Type = NotificationType.Email,
                            Title = AutoEngineeringAcceptTitle,
                            Body = body,
                            EntityId = order.Id,
                            OwnerId = userId,
                            ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={order.Id}",
                            IsRead = false
                        }, cn);
                    }

                    acceptedCount++;
                }

                await unitOfWork.SaveChangesAsync(cn);
                logger?.LogInfoAsync($"Auto engineering accept completed. Accepted={acceptedCount}");
            }
            catch (Exception ex)
            {
                if (logger != null)
                    await logger.LogInfoAsync($"Error in AutoAcceptEngineeringForRoutineParts: {ex.Message}", cn);
            }
        }

        private static string BuildAutoEngineeringAcceptBody(OpenOrderRequest order)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div style='width: 100%;text-align:center;direction:rtl'>");
            sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' width='100%'>");
            sb.AppendLine("<tr style='background: #000aa0'><td colspan='2'><div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center'>گروه صنعتی هوایار</div></td></tr>");
            sb.AppendLine("<tr><td>");
            sb.AppendLine("بدینوسیله اعلام میگردد، مدرک/مدارک جدیدی جهت درخواست باز، با مشخصات ذیل آپلود گردید");
            sb.AppendLine("<br><br>شما می توانید با مراجعه به سیستم جامع / سیستم تدارکات / عملیات / درخواست های باز / پیوست مدارک از جزییات آن اطلاع یابید<br><br>");
            sb.AppendLine("<ul>");
            sb.AppendLine($"<li>شماره درخواست : <strong>{order.PurchaseRequestNumber}</strong></li>");
            sb.AppendLine($"<li>شماره سفارش : <strong>{order.OrderNo}</strong></li>");
            if (order.IsStop)
                sb.AppendLine("<li>وضعیت درخواست : <strong style='color:red'>متوقف شده</strong></li>");
            sb.AppendLine($"<li>کد کالا : <strong>{order.Part?.Code}</strong></li>");
            sb.AppendLine($"<li>عنوان کالا : <strong>{order.Part?.Name}</strong></li>");
            sb.AppendLine("</ul></td></tr></table></div>");
            return sb.ToString();
        }

        private async Task ComputeOpenOrderRequest(string yearNumbers, IJobLogger? logger, CancellationToken cn)
        {
            try
            {
                logger?.LogInfoAsync("Computing OpenOrderRequest updates...");

                var now = DateTime.Now;
                var currentDate = now.ToShamsiDate();
                var currentTime = now.ToString("HH:mm");

                // 1. Get Return From Temporary Receipt IDs
                var returnedOrderRowIds = await GetReturnFromTemporaryReceiptIds();

                // 2. Get All Order Items with States from Rahkaran
                var allOrderItemsWithState = await GetAllOrderItemsWithState(yearNumbers);

                // 3. Get Open Order Requests (State IN 3,4,7) from Rahkaran
                var openOrderRequests = await GetOpenOrderRequests(yearNumbers);

                // 4. Load local orders
                var localOrders = await unitOfWork.Repository<OpenOrderRequest>().Table
                    .Include(o => o.Comments)
                    .ToListAsync(cn);

                // 5. Update Completion Date for completed orders (State = 7)
                var completedOrderRowIds = openOrderRequests
                    .Where(x => x.OrderItemState == 7 && x.OrderRowId.HasValue)
                    .Select(x => x.OrderRowId!.Value)
                    .ToHashSet();

                foreach (var local in localOrders)
                {
                    if (local.OrderRowId.HasValue && completedOrderRowIds.Contains(local.OrderRowId.Value))
                    {
                        if (string.IsNullOrEmpty(local.CompletionShamsiDate))
                        {
                            // Check if not stopped
                            var lastComment = local.Comments.OrderByDescending(c => c.Id).FirstOrDefault();
                            if (lastComment == null || !lastComment.IsStop)
                            {
                                local.CompletionShamsiDate = currentDate;
                                local.CompletionMiladiDate = now;
                            }
                        }
                    }
                }

                // 6. Reactivation Logic - راه‌اندازی دوباره
                // Items that are deleted but now back in open requests
                // Never revive when (PR,OrderRow) is not an active HTS pair (deleted in HTS or PR has no active HTS row)
                var htsActiveKeys = await LoadHtsActiveOpenOrderKeysAsync(cn);
                var openRequestKeys = openOrderRequests.Select(x => x.PurchaseRequestItemId).ToHashSet();

                foreach (var local in localOrders)
                {
                    if (local.IsDeleted && !local.IsForceDeletedByUser && openRequestKeys.Contains(local.PurchaseRequestItemId))
                    {
                        var matchingRemote = openOrderRequests.FirstOrDefault(x => x.PurchaseRequestItemId == local.PurchaseRequestItemId);
                        if (matchingRemote != null)
                        {
                            var localKey = (local.PurchaseRequestItemId, local.OrderRowId ?? 0L);
                            if (!htsActiveKeys.Contains(localKey))
                                continue;

                            // Reactivate if not manually deleted and not contains "توسط" in comment
                            if (local.Comment?.Contains("توسط") != true)
                            {
                                // Request reactivation (no OrderRowId check)
                                if (local.OrderRowId == null)
                                {
                                    local.IsDeleted = false;
                                    local.IsDeletedMiladiDate = null;
                                    local.IsDeletedShamsiDate = null;
                                    local.Comment = (local.Comment ?? "") + (local.Comment?.Contains("راه اندازی دوباره") == true ? "" : "|| راه اندازی دوباره بجهت خروج از اختتام");
                                }
                                // Order reactivation (with OrderRowId check and quantity check)
                                else if (local.OrderRowId == matchingRemote.OrderRowId)
                                {
                                    if (local.FactoredCount < matchingRemote.RequestItemQuantity && !local.IsStop)
                                    {
                                        local.IsDeleted = false;
                                        local.IsDeletedMiladiDate = null;
                                        local.IsDeletedShamsiDate = null;
                                        local.Comment = (local.Comment ?? "") + (local.Comment?.Contains("راه اندازی دوباره") == true ? "" : "|| راه اندازی دوباره بجهت خروج از اختتام");
                                    }
                                }
                            }
                        }
                    }
                }

                // 7. State-based Deletion (PurchaseRequestItemStatus IN 5,6,9 = terminated/cancelled)
                //    and items that no longer exist in Rahkaran at all
                if (allOrderItemsWithState.Count > 0)
                {
                    var terminatedItemIds = allOrderItemsWithState
                        .Where(x => x.PurchaseRequestItemStatus != null && (x.PurchaseRequestItemStatus == 5 || x.PurchaseRequestItemStatus == 6 || x.PurchaseRequestItemStatus == 9))
                        .Select(x => x.PurchaseRequestItemId)
                        .ToHashSet();

                    var allItemIds = allOrderItemsWithState.Select(x => x.PurchaseRequestItemId).ToHashSet();
                    var cutoff = new DateTime(2023, 3, 20);

                    foreach (var local in localOrders)
                    {
                        if (local.IsDeleted || local.IsForceDeletedByUser)
                            continue;

                        if (!local.PurchaseRequestMiladiDate.HasValue || local.PurchaseRequestMiladiDate.Value <= cutoff)
                            continue;

                        if (terminatedItemIds.Contains(local.PurchaseRequestItemId) || !allItemIds.Contains(local.PurchaseRequestItemId))
                        {
                            local.IsDeleted = true;
                            local.IsDeletedMiladiDate = now;
                            local.IsDeletedShamsiDate = now.ToShamsiDateTime();
                        }
                    }
                }

                // 8. Return From Temporary Receipt Deletion
                if (returnedOrderRowIds.Any())
                {
                    foreach (var local in localOrders)
                    {
                        if (local.OrderRowId.HasValue && returnedOrderRowIds.Contains(local.OrderRowId.Value) && !local.IsDeleted)
                        {
                            if (local.OrderMiladiDate.HasValue && local.OrderMiladiDate.Value > new DateTime(2023, 3, 20))
                            {
                                local.IsDeleted = true;
								local.IsDeletedMiladiDate = now;
								local.IsDeletedShamsiDate = now.ToShamsiDateTime();
							}
                        }
                    }
                }

                // 8b. Soft-delete fully factored (HTS WriteData: FactoredCount == RequiredQty ⇒ IsDeleted)
                foreach (var local in localOrders)
                {
                    if (local.IsDeleted || local.IsForceDeletedByUser)
                        continue;

                    if (local.RequiredQty > 0 && local.FactoredCount >= local.RequiredQty)
                    {
                        local.IsDeleted = true;
                        local.IsDeletedMiladiDate = now;
                        local.IsDeletedShamsiDate = now.ToShamsiDateTime();
                        if (string.IsNullOrEmpty(local.CompletionShamsiDate))
                        {
                            local.CompletionShamsiDate = currentDate;
                            local.CompletionMiladiDate = now;
                        }
                    }
                }

                // 8c. Align IsDeleted with HTS by PurchaseRequestItemId+OrderRowId (active-set parity)
                await AlignIsDeletedWithHtsAsync(localOrders, htsActiveKeys, now, logger, cn);

                // 9. Update SalesUnit Info from ProductionOrder
                var ordersNeedingSalesUpdate = localOrders
                    .Where(o => o.ProductionOrderId.HasValue && o.SalesUnitSalesExpertId == null)
                    .ToList();

                if (ordersNeedingSalesUpdate.Any())
                {
                    var productionOrderIds = ordersNeedingSalesUpdate.Select(o => o.ProductionOrderId!.Value).Distinct().ToList();
                    var productionOrders = await unitOfWork.Repository<ProductionOrder>().TableNoTracking
                        .Where(po => productionOrderIds.Contains(po.Id!.Value))
                        .ToListAsync(cn);

                    var productionOrderDict = productionOrders.ToDictionary(po => po.Id!.Value);

                    foreach (var local in ordersNeedingSalesUpdate)
                    {
                        if (productionOrderDict.TryGetValue(local.ProductionOrderId!.Value, out var prodOrder))
                        {
                            local.SalesUnitSalesExpertId = prodOrder.SalesExpertId;
                            local.SalesUnitSalesManagerId = prodOrder.SalesManagerId;
                            local.SalesUnitProjectManagerId = prodOrder.ProjectManagerId;
                        }
                    }
                }

                // 10. Update IsRoutineRequest — معادل SP قدیم: بدون مدیر پروژه = روتین
                foreach (var local in localOrders)
                {
                    local.IsRoutineRequest = !local.SalesUnitProjectManagerId.HasValue;
                }

                await unitOfWork.SaveChangesAsync(cn);

                // 11. Calculate DelaysBuyDay
                await CalculateDelaysBuyDay(localOrders, cn);

                logger?.LogInfoAsync("Compute completed.");
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    await logger.LogInfoAsync($"Error in ComputeOpenOrderRequest: {ex.Message}", cn);
                }
                throw;
            }
        }

        /// <summary>
        /// Active (IsDeleted=0) HTS keys on (PurchaseRequestItemId, OrderRowId??0).
        /// </summary>
        private async Task<HashSet<(long PurchaseRequestItemId, long OrderRowId)>> LoadHtsActiveOpenOrderKeysAsync(CancellationToken cn)
        {
            var rows = await htsDb.Hts_Sup_OpenOrderRequests.AsNoTracking()
                .Where(o => !o.IsDeleted)
                .Select(o => new { o.PurchaseRequestItemId, o.OrderRowId })
                .ToListAsync(cn);

            return rows
                .Select(o => (o.PurchaseRequestItemId, OrderRowId: o.OrderRowId ?? 0L))
                .ToHashSet();
        }

        /// <summary>
        /// هم‌ترازی IsDeleted با TotalSystem روی کلید PurchaseRequestItemId+OrderRowId.
        /// هر ردیف فعال محلی که جفتش در HTS فعال نیست → soft-delete
        /// (شامل: همان کلید حذف‌شده در HTS، OrderRow اشتباه، یا PR بدون هیچ ردیف فعال HTS).
        /// نباید با همگام‌سازی Rahkaran دوباره فعال شود.
        /// </summary>
        private async Task AlignIsDeletedWithHtsAsync(
            List<OpenOrderRequest> localOrders,
            HashSet<(long PurchaseRequestItemId, long OrderRowId)> htsActiveKeys,
            DateTime now,
            IJobLogger? logger,
            CancellationToken cn)
        {
            htsActiveKeys ??= await LoadHtsActiveOpenOrderKeysAsync(cn);

            var softDeleted = 0;
            foreach (var local in localOrders)
            {
                if (local.IsForceDeletedByUser || local.IsDeleted)
                    continue;

                var key = (local.PurchaseRequestItemId, local.OrderRowId ?? 0L);
                if (htsActiveKeys.Contains(key))
                    continue;

                local.IsDeleted = true;
                local.IsDeletedMiladiDate = now;
                local.IsDeletedShamsiDate = now.ToShamsiDateTime();
                softDeleted++;
            }

            logger?.LogInfoAsync($"AlignIsDeletedWithHts soft-deleted={softDeleted}");
        }

        private async Task CalculateDelaysBuyDay(List<OpenOrderRequest> localOrders, CancellationToken cn)
        {
            try
            {
                // Load BuyCategory mapping
                var buyCategoryItems = await unitOfWork.Repository<BuyCategoryItem>().TableNoTracking
                    .Include(bc => bc.BuyCategory)
                    .Where(bc => bc.PartId != null)
                    .ToListAsync(cn);

                var buyCategoryDict = buyCategoryItems
                    .Where(bc => bc.PartId.HasValue)
                    .GroupBy(bc => bc.PartId!.Value)
                    .ToDictionary(g => g.Key, g => g.First().BuyCategory);

                // Load Comments for delay calculation
                var orderIds = localOrders.Where(o => o.CompletionShamsiDate != null).Select(o => o.Id!.Value).ToList();
                var allComments = await unitOfWork.Repository<OpenOrderRequestComment>().TableNoTracking
                    .Where(c => orderIds.Contains(c.OpenOrderRequestId))
                    .ToListAsync(cn);

                var commentsByOrderId = allComments.GroupBy(c => c.OpenOrderRequestId).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var order in localOrders.Where(o => o.CompletionShamsiDate != null && o.OrderMiladiDate > new DateTime(2023, 3, 20)))
                {
                    try
                    {
                        int delayDays = 0;

                        // Calculate stop/launch delay
                        int stopLaunchDelay = 0;
                        if (commentsByOrderId.TryGetValue(order.Id!.Value, out var comments))
                        {
                            var stopComments = comments.Where(c => c.IsStop).OrderBy(c => c.Id).ToList();
                            var launchComments = comments.Where(c => c.IsLaunched).OrderBy(c => c.Id).ToList();

                            for (int i = 0; i < Math.Min(stopComments.Count, launchComments.Count); i++)
                            {
                                if (!string.IsNullOrEmpty(stopComments[i].ShamsiDate) && !string.IsNullOrEmpty(launchComments[i].ShamsiDate))
                                {
                                    var stopDate = stopComments[i].ShamsiDate!.ToMiladiDate();
                                    var launchDate = launchComments[i].ShamsiDate!.ToMiladiDate();
                                    stopLaunchDelay += (launchDate - stopDate).Days;
                                }
                            }
                        }

                        // Determine expected delivery date
                        DateTime? expectedDeliveryDate = null;

                        if (!string.IsNullOrEmpty(order.SupplyShamsiDate))
                        {
                            expectedDeliveryDate = order.SupplyShamsiDate.ToMiladiDate();
                        }
                        else if (order.EngineeringConfirmationMiladiDateTime.HasValue && buyCategoryDict.TryGetValue(order.PartId, out var buyCategory))
                        {
                            // Calculate based on lead time
                            int leadTimeDays = 0;

                            if (order.DlId.HasValue && buyCategory.NonRoutineLeadTimeInDay.HasValue)
                            {
                                leadTimeDays = buyCategory.NonRoutineLeadTimeInDay.Value;
                            }
                            else if (!order.DlId.HasValue && buyCategory.RoutineLeadTimeInDay.HasValue)
                            {
                                leadTimeDays = buyCategory.RoutineLeadTimeInDay.Value;
                            }

                            if (leadTimeDays > 0)
                            {
                                expectedDeliveryDate = order.EngineeringConfirmationMiladiDateTime.Value.AddDays(leadTimeDays);
                            }
                        }
                        else if (order.NeedDateMiladiDate.HasValue)
                        {
                            // Use need date as fallback
                            expectedDeliveryDate = order.NeedDateMiladiDate;
                        }

                        // Calculate delay
                        if (expectedDeliveryDate.HasValue && order.CompletionMiladiDate.HasValue)
						{
                            var completionDate = order.CompletionMiladiDate ;
                            delayDays = (completionDate.Value - expectedDeliveryDate.Value).Days - stopLaunchDelay;
                        }
                        else
                        {
                            delayDays = stopLaunchDelay;
                        }

                        order.DelaysBuyDay = delayDays;
                    }
                    catch
                    {
                        // Skip calculation for this order if any error
                    }
                }

                await unitOfWork.SaveChangesAsync(cn);
            }
            catch
            {
                // Don't throw - این نباید کل job را متوقف کند
            }
        }

        private async Task<List<long>> GetReturnFromTemporaryReceiptIds()
        {
            var sql = @"
IF OBJECT_ID('tempdb..#ReceiptPermitItems') IS NOT NULL
	DROP TABLE #ReceiptPermitItems

SELECT ReceiptPermitItemID , ReferenceRef 
INTO #ReceiptPermitItems
FROM Erps.LGS3.ReceiptPermitItem

SELECT  oi.OrderItemID
FROM ERPS.Erps.PRC3.OrderItem oi
INNER JOIN Erps.Erps.PRC3.DeliveryItem AS DeliveryItem ON DeliveryItem.ReferenceRef = oi.OrderItemID
INNER JOIN #ReceiptPermitItems AS ReceiptPermitItem ON ReceiptPermitItem.ReferenceRef = DeliveryItem.DeliveryItemID 
INNER JOIN ERPS.Erps.lgs3.InventoryVoucherItem AS  InventoryVoucherItem ON InventoryVoucherItem.ReferenceRef=ReceiptPermitItem.ReceiptPermitItemID AND InventoryVoucherItem.ReferenceType=2 AND InventoryVoucheritem.InventoryVoucherSpecificationRef=9979
INNER JOIN ERPS.Erps.lgs3.InventoryVoucher InventoryVoucher ON InventoryVoucher.InventoryVoucherID=InventoryVoucherItem.InventoryVoucherRef AND InventoryVoucher.InventoryVoucherSpecificationRef=9979
JOIN ERPS.Erps.LGS3.InventoryVoucherItem AS  InventoryVoucherItem2 ON InventoryVoucherItem2.ReturnableVoucherItemRef = InventoryVoucherItem.InventoryVoucherItemID AND  InventoryVoucheritem.InventoryVoucherSpecificationRef = 9982				
GROUP BY oi.OrderItemID
";
            try
            {
                return await rdb.Database.SqlQueryRaw<long>(sql).ToListAsync();
            }
            catch
            {
                return new List<long>();
            }
        }

        private async Task<List<AllOrderItemDto>> GetAllOrderItemsWithState(string yearNumbers)
        {
            var sql = $@"
SELECT  PurchaseRequestItem.PurchaseRequestItemID,
        PurchaseRequest.State AS PurchaseRequestStatus,
        PurchaseRequestItem.State AS PurchaseRequestItemStatus,
        CmrOrdItm.OrderItemID AS OrderRowId, 
        CmrOrdItm.Quantity AS OrderQty, 
        PurchaseRequestItem.Quantity AS ReqQty , 
        OrderItemStatus.Code AS OrderStatus

FROM ERPs.Erps.PRC3.PurchaseRequestItem AS PurchaseRequestItem 
INNER JOIN ERPs.Erps.PRC3.PurchaseRequest AS PurchaseRequest ON PurchaseRequest.PurchaseRequestID = PurchaseRequestItem.PurchaseRequestRef
INNER JOIN Erps.Erps.SYS3.Lookup AS PurchaseRequestStatus ON PurchaseRequestStatus.Code = PurchaseRequest.State AND PurchaseRequestStatus.Type = 'PurchaseContractState'
INNER JOIN Erps.Erps.SYS3.Lookup AS PurchaseRequestItemStatus ON PurchaseRequestItemStatus.Code = PurchaseRequestItem.State AND PurchaseRequestItemStatus.Type = 'PurchaseContractState'

LEFT JOIN ERPs.Erps.PRC3.OrderItem AS CmrOrdItm ON PurchaseRequestItem.PurchaseRequestItemID = CmrOrdItm.PurchaseRequestItemRef
LEFT JOIN ERPs.Erps.PRC3.[Order] AS CmrOrder ON CmrOrder.OrderID = CmrOrdItm.OrderRef
LEFT JOIN Erps.Erps.SYS3.Lookup AS OrderStatus ON OrderStatus.Code = CmrOrder.State AND OrderStatus.Type = 'PurchaseContractState'
LEFT JOIN Erps.Erps.SYS3.Lookup AS OrderItemStatus ON OrderItemStatus.Code = CmrOrdItm.State AND OrderItemStatus.Type = 'PurchaseContractState'
";
            try
            {
                return await rdb.Database.SqlQueryRaw<AllOrderItemDto>(sql).ToListAsync();
            }
            catch
            {
                return new List<AllOrderItemDto>();
            }
        }

        private async Task<List<OpenOrderRequestDto>> GetOpenOrderRequests(string yearNumbers)
        {
            var sql = $@"
SELECT PurchaseRequestItem.PurchaseRequestItemID, 
       PurchaseRequest.State AS PurchaseRequestState,
       PurchaseRequestItem.State AS PurchaseRequestItemState,
       CmrOrdItm.OrderItemID AS OrderRowId , 
       CmrOrdItm.State AS OrderItemState,
       PurchaseRequestItem.Quantity AS RequestItemQuantity,
       CmrOrdItm.Quantity AS OrderItemQuantity

FROM ERPs.Erps.PRC3.PurchaseRequestItem AS PurchaseRequestItem 
INNER JOIN ERPs.Erps.PRC3.PurchaseRequest AS PurchaseRequest ON PurchaseRequest.PurchaseRequestID = PurchaseRequestItem.PurchaseRequestRef
INNER JOIN Erps.Erps.SYS3.Lookup AS PurchaseRequestStatus ON PurchaseRequestStatus.Code = PurchaseRequest.State AND PurchaseRequestStatus.Type = 'PurchaseContractState'
INNER JOIN Erps.Erps.SYS3.Lookup AS PurchaseRequestItemStatus ON PurchaseRequestItemStatus.Code = PurchaseRequestItem.State AND PurchaseRequestItemStatus.Type = 'PurchaseContractState'

LEFT JOIN ERPs.Erps.PRC3.OrderItem AS CmrOrdItm ON PurchaseRequestItem.PurchaseRequestItemID = CmrOrdItm.PurchaseRequestItemRef
LEFT JOIN ERPs.Erps.PRC3.[Order] AS CmrOrder ON CmrOrder.OrderID = CmrOrdItm.OrderRef
LEFT JOIN Erps.Erps.SYS3.Lookup AS OrderStatus ON OrderStatus.Code = CmrOrder.State AND OrderStatus.Type = 'PurchaseContractState'
LEFT JOIN Erps.Erps.SYS3.Lookup AS OrderItemStatus ON OrderItemStatus.Code = CmrOrdItm.State AND OrderItemStatus.Type = 'PurchaseContractState'

WHERE PurchaseRequestItem.State IN(3,4,7)
";
            try
            {
                return await rdb.Database.SqlQueryRaw<OpenOrderRequestDto>(sql).ToListAsync();
            }
            catch
            {
                return new List<OpenOrderRequestDto>();
            }
        }

        private async Task<List<HamkaranOrderDto>> GetRahkaranOrders(string yearNumbers)
        {
            var sql = $@"
IF object_id('tempdb..#ReceiptPermitItems') is not null
	DROP TABLE #ReceiptPermitItems

SELECT ReceiptPermitItemID , ReferenceRef 
INTO #ReceiptPermitItems
FROM Erps.LGS3.ReceiptPermitItem

SELECT  	 
	FiscalYear.FiscalYearID  AS Year,
	CmrReqPart.PurchaseRequestItemID,
	CmrOrdItm.OrderItemID AS OrderRowId,
	Part.Code  AS PartCode ,
	Part.Name  AS PartName,
	
	PurchaseRequest.Number AS PurchaseRequestNumber,
	PurchaseRequest.RequestDate AS PurchaseRequestDate,
	PurchaseRequestDate.SolarDate AS PurchaseRequestDateInText,
    CASE WHEN (CmrReqPart.Description IS NULL OR LEN(CmrReqPart.Description) < 1) THEN Dl.Title ELSE CmrReqPart.Description END AS OrdItmComment,
	CmrReqPart.RowNumber AS Seq,

	CmrOrder.Number AS OrderNo,

	Part.PartID AS PartRef,
	1.0 AS Ratio,
	1.0 AS upRatio,
	OrderStatus.Code AS OrderStatus,
	OrderItemStatus.Code  AS OrdItmStatus,
	CmrOrder.OrderDate AS OrderDate,
    CmrOrder.OrderDate AS OrderDateInEurope,
    
	(SELECT TOP (1) StateHistory.ChangeDate FROM Erps.Erps.SYS3.StateHistory AS StateHistory WHERE StateHistory.EntityCode = 725 AND StateHistory.RecordID = CmrOrder.OrderID AND StateHistory.TargetState = 3 ORDER BY StateHistory.StateHistoryID DESC) AS OrderConfirmDate,
	CmrReqPart.DemandDate AS NeedDate,
	Dl.DLID AS DlRef,
	RelatedPart.PartID AS RelatedPartId,
		
	CmrReqPart.Quantity AS ReqQty,
	CmrOrdItm.Quantity AS OrderQty,
    ISNULL(SendPart.Quantity,0) AS FinalSendQty,
	ISNULL(ReturnedPart.DeliveryReturnItemCount ,0) AS RetQty ,
	IIF((ISNULL(SendPart.OpenQuantity, 0) - ISNULL(ReturnedPart.DeliveryReturnItemCount,0) - ISNULL(SendPart.FinalVoucherItemCount,0)) > CmrReqPart.Quantity ,CmrReqPart.Quantity, ISNULL(SendPart.OpenQuantity, 0) - ISNULL(ReturnedPart.DeliveryReturnItemCount,0) - ISNULL(SendPart.FinalVoucherItemCount,0)) AS SumSendQty,	

	SendPart.DeliveryDate,
	SendPart.TemporaryVoucherDate,
	SendPart.FinalVoucherDate,

    ISNULL((
            SELECT SUM(InventoryVoucherFinal.MajorUnitQuantity)
					FROM ERPS.Erps.PRC3.OrderItem AS OrderItemTemp 
					INNER JOIN ERPS.Erps.PRC3.[ORDER] o ON o.OrderID=OrderItemTemp.OrderRef
	
					INNER  JOIN Erps.Erps.sys3.RecursiveEntityRelation as RecursiveEntityRelation ON RecursiveEntityRelation.SourceRef= OrderItemTemp.OrderItemID 
					INNER JOIN Erps.Erps.sys3.EntityLookup as OrderItemLookup on OrderItemLookup.Code = RecursiveEntityRelation.SourceType AND OrderItemLookup.EntityName = 'SystemGroup.Procurement.OrderManagement,OrderItem'
					INNER JOIN Erps.Erps.sys3.EntityLookup as InventoryVoucherItemLookup on InventoryVoucherItemLookup.Code = RecursiveEntityRelation.TargetType AND InventoryVoucherItemLookup.EntityName = 'SystemGroup.Logistics.VoucherProcessing,InventoryVoucherItem' 
	
					INNER JOIN Erps.Erps.lgs3.InventoryVoucherItem as InventoryVoucherItem on  InventoryVoucherItem.InventoryVoucherItemID = RecursiveEntityRelation.TargetRef
					INNER  JOIN Erps.Erps.lgs3.InventoryVoucher AS Inventory ON Inventory.InventoryVoucherID = InventoryVoucherItem.InventoryVoucherRef

					------ سند دائم انبار استعلامی
					 JOIN  ERPS.Erps.LGS3.InventoryVoucherItem as InventoryVoucherFinal on  InventoryVoucherFinal.ReferenceRef = InventoryVoucherItem.InventoryVoucherItemID and InventoryVoucherFinal.InventoryVoucherSpecificationRef=2 AND InventoryVoucherFinal.ReferenceType=1

					WHERE OrderItemTemp.OrderItemID = CmrOrdItm.OrderItemID
		),0) AS FactoredCount


	FROM   
		   	Erps.Erps.PRC3.PurchaseRequestItem AS CmrReqPart 
			INNER JOIN Erps.Erps.PRC3.PurchaseRequest AS PurchaseRequest ON PurchaseRequest.PurchaseRequestID = CmrReqPart.PurchaseRequestRef
			INNER JOIN Erps.Erps.PRC3.PurchaseItem AS PurchaseItem ON PurchaseItem.PurchaseItemID = CmrReqPart.PurchaseItemRef
			INNER JOIN Erps.Erps.LGS3.Part AS Part ON Part.PartID = PurchaseItem.PartRef
			INNER JOIN ERPS.Erps.GNR3.FiscalYear AS FiscalYear ON FiscalYear.FiscalYearID = PurchaseRequest.FiscalYearRef
			
			LEFT JOIN Erps.Erps.SYS3.FieldValueContainer AS FieldValueContainer ON FieldValueContainer.RecordID = PurchaseRequest.PurchaseRequestID AND FieldValueContainer.EntityCode = 736
			LEFT JOIN Erps.Erps.LGS3.Part AS RelatedPart ON RelatedPart.PartID = FieldValueContainer.Reference2

			LEFT JOIN ERPs.Erps.FIN3.DL AS Dl ON Dl.ReferenceID = PurchaseRequest.CounterpartRef AND Dl.DLTypeRef = CASE WHEN PurchaseRequest.CounterpartType = 1 THEN 1 WHEN PurchaseRequest.CounterpartType = 2 THEN 4 ELSE 5 END
			INNER JOIN ERPs.Erps.USR3.Gnr_DimDate  AS PurchaseRequestDate ON PurchaseRequestDate.Date = CAST(PurchaseRequest.RequestDate AS DATE)

		    LEFT JOIN  ERPs.Erps.PRC3.OrderItem AS CmrOrdItm ON CmrOrdItm.PurchaseRequestItemRef = CmrReqPart.PurchaseRequestItemID
			LEFT JOIN ERPs.Erps.PRC3.[Order] AS CmrOrder ON CmrOrder.OrderID = CmrOrdItm.OrderRef
			LEFT JOIN Erps.Erps.SYS3.Lookup AS OrderStatus ON OrderStatus.Code = CmrOrder.State AND OrderStatus.Type = 'PurchaseContractState'
			LEFT JOIN Erps.Erps.SYS3.Lookup AS OrderItemStatus ON OrderItemStatus.Code = CmrOrdItm.State AND OrderItemStatus.Type = 'PurchaseContractState'

			LEFT  JOIN (    
							SELECT OrderItemTemp.OrderItemID ,
								  -- IIF(SUM(DeliveryItem.Quantity) > OrderItemTemp.Quantity ,OrderItemTemp.Quantity, SUM(DeliveryItem.Quantity))  AS Quantity, 
								   SUM(DeliveryItem.Quantity) AS Quantity, 
								   SUM(DeliveryItem.Quantity) AS OpenQuantity, 
								   MAX(Delivery.DeliveryDate) AS DeliveryDate,
								   SUM(ISNULL(InventoryVoucherItem.Quantity, 0)) TempVoucherItemCount,
								   MAX(InventoryVoucherItem.Date) AS TemporaryVoucherDate,
								   MAX(InventoryVoucherItemFinal.Date) AS FinalVoucherDate,
								   SUM(ISNULL(InventoryVoucherItemFinal.Quantity, 0)) AS FinalVoucherItemCount

							FROM ERPS.Erps.PRC3.OrderItem AS OrderItemTemp 
							INNER JOIN ERPS.Erps.PRC3.[ORDER] o ON o.OrderID=OrderItemTemp.OrderRef
					
							INNER  JOIN Erps.Erps.sys3.RecursiveEntityRelation as RecursiveEntityRelation ON RecursiveEntityRelation.SourceRef= OrderItemTemp.OrderItemID 
							INNER JOIN Erps.Erps.sys3.EntityLookup as OrderItemLookup on OrderItemLookup.Code = RecursiveEntityRelation.SourceType AND OrderItemLookup.EntityName = 'SystemGroup.Procurement.OrderManagement,OrderItem'
							INNER JOIN Erps.Erps.sys3.EntityLookup as DeliveryItemLookup on DeliveryItemLookup.Code = RecursiveEntityRelation.TargetType AND DeliveryItemLookup.EntityName = 'SystemGroup.Procurement.DeliveryManagement,DeliveryItem' 
	
							INNER JOIN Erps.Erps.PRC3.DeliveryItem as DeliveryItem on  DeliveryItem.DeliveryItemID = RecursiveEntityRelation.TargetRef
							INNER JOIN Erps.Erps.PRC3.Delivery as Delivery on  Delivery.DeliveryID = DeliveryItem.DeliveryRef

							------ مجوز ورود
							LEFT JOIN #ReceiptPermitItems AS ReceiptPermitItem on ReceiptPermitItem.ReferenceRef = DeliveryItem.DeliveryItemID 
							------سند موقت انبار 
							LEFT JOIN ERPS.Erps.LGS3.InventoryVoucherItem InventoryVoucherItem on InventoryVoucherItem.ReferenceRef=ReceiptPermitItem.ReceiptPermitItemID AND InventoryVoucherItem.InventoryVoucherSpecificationRef=9979
							------ سند دائم انبار 
							LEFT JOIN  ERPS.Erps.LGS3.InventoryVoucherItem as InventoryVoucherItemFinal on  InventoryVoucherItemFinal.ReferenceRef=InventoryVoucherItem.InventoryVoucherItemID and InventoryVoucherItemFinal.InventoryVoucherSpecificationRef=2 AND InventoryVoucherItemFinal.ReferenceType=1

							GROUP BY OrderItemTemp.OrderItemID , OrderItemTemp.Quantity
			
						) AS SendPart ON SendPart.OrderItemID = CmrOrdItm.OrderItemID

		
			
			LEFT JOIN (  
						SELECT  DeliveryItem.ReferenceRef , 
								ISNULL(DeliveryReturnItem.Quantity,0) AS DeliveryReturnItemCount,
								DeliveryReturnItem.DeliveryReturnItemID,
							    RowNumber = ROW_NUMBER() OVER (PARTITION BY DeliveryItem.ReferenceRef ORDER BY DeliveryReturnItem.DeliveryReturnItemID DESC)

						FROM  ERPS.Erps.PRC3.DeliveryItem  AS DeliveryItem 
						
						------ مجوز ورود استعلامی
						INNER  JOIN #ReceiptPermitItems ri12 on ri12.ReferenceRef = DeliveryItem.DeliveryItemID 
						
						------سند موقت انبار 
						INNER JOIN ERPS.Erps.LGS3.InventoryVoucherItem InventoryVoucherItem on InventoryVoucherItem.ReferenceRef=ri12.ReceiptPermitItemID AND InventoryVoucheritem.InventoryVoucherSpecificationRef=9979
						
						--  سند مرجوعی
						LEFT JOIN ERPS.Erps.PRC3.DeliveryReturnItem AS DeliveryReturnItem on  DeliveryReturnItem.ReferenceRef = InventoryVoucherItem.InventoryVoucherItemID 
						------ تحویل   		
						LEFT JOIN ERPS.Erps.PRC3.DeliveryItem  AS DeliveryItemAgain ON DeliveryItemAgain.ReferenceRef = DeliveryReturnItem.DeliveryReturnItemID

						------ مجوز ورود
						LEFT JOIN #ReceiptPermitItems AS ReceiptPermitItemAgain on ReceiptPermitItemAgain.ReferenceRef = DeliveryItemAgain.DeliveryItemID 

						------سند موقت انبار 
						LEFT JOIN ERPS.Erps.LGS3.InventoryVoucherItem InventoryVoucherItemAgain on InventoryVoucherItemAgain.ReferenceRef=ReceiptPermitItemAgain.ReceiptPermitItemID AND InventoryVoucherItemAgain.InventoryVoucherSpecificationRef=9979

						------ سند دائم انبار 
						LEFT JOIN  ERPS.Erps.LGS3.InventoryVoucherItem as InventoryVoucherItemAgainFinal on  InventoryVoucherItemAgainFinal.ReferenceRef=InventoryVoucherItemAgain.InventoryVoucherItemID and InventoryVoucherItemAgainFinal.InventoryVoucherSpecificationRef=2 AND InventoryVoucherItemAgainFinal.ReferenceType=1
					  
					  ) AS ReturnedPart ON ReturnedPart.ReferenceRef = CmrOrdItm.OrderItemID AND ReturnedPart.DeliveryReturnItemID IS NOT NULL AND ReturnedPart.RowNumber = 1

	WHERE   
		CmrReqPart.PurchaseRequestItemID >= 1
	AND PurchaseRequest.PurchasingDepartmentRef = 1
	AND CmrReqPart.State IN (3,4,7)
	And PurchaseRequest.FiscalYearRef IN({yearNumbers.GetHamkaranYearNumber()}) 
	AND PurchaseRequest.CounterpartRef <> 24 
	AND LEN(Part.Code) >= 10 
	AND Part.Code NOT LIKE N'40%'
	AND Part.Code NOT LIKE N'60%'
	AND Part.Code NOT LIKE N'80%'
";
            return await rdb.Database.SqlQueryRaw<HamkaranOrderDto>(sql).ToListAsync();
        }

        private async Task<List<HamkaranNotifyDto>> GetNewInvVchForNotify(string yearNumbers)
        {
            var sql = $@"
IF OBJECT_ID('tempdb..#ReceiptPermitItems') IS NOT NULL
	DROP TABLE #ReceiptPermitItems

SELECT ReceiptPermitItemID , ReferenceRef 
INTO #ReceiptPermitItems
FROM Erps.LGS3.ReceiptPermitItem

SELECT 
PurchaseRequestItem.PurchaseRequestItemID,
PurchaseRequest.Number AS PurchaseRequestNumber,

InventoryVoucherItem.InventoryVoucherItemID AS VchItmID,
OrderItem.OrderItemID AS SendRefNo,
CAST(Delivery.Number AS NVARCHAR(80)) AS SendNo ,
[ORDER].Number AS OrdNo,
Delivery.DeliveryDate AS SendDate,

Part.Code AS PartCode,
Part.Name AS PartName,
DeliveryItem.Description AS Comment,

InventoryVoucherItem.Date AS VchDateLatin ,
InventoryVoucherItem.Number AS TempVchNum, 

InventoryVoucherItemFinal.Number AS FinalVchNum,
InventoryVoucherItem.InventoryVoucherItemID AS TempVchItemId,
InventoryVoucherItemFinal.InventoryVoucherItemID AS FinalVchItemId,

CASE WHEN QualityInspectionResultItem.RejectedQuantity > 0 AND OrderItem.Quantity <> (ISNULL(InventoryVoucherItemFinal.Quantity , 0)) THEN 0 ELSE NULL END AS InspctnFlag,
CASE WHEN QualityInspectionResultItem.RejectedQuantity > 0 AND OrderItem.Quantity <> (ISNULL(InventoryVoucherItemFinal.Quantity , 0)) THEN QualityInspectionResult.Description + ' | ' + QualityInspectionResultItem.Description ELSE NULL END AS InspectionComment,

DeliveryItem.DeliveryItemID AS RowSendId,
OrderItem.Quantity AS ordQty,
DeliveryItem.Quantity AS SendQty,
InventoryVoucherItem.Quantity AS TempVchQty,
InventoryVoucherItemFinal.Quantity AS FinalVchQty,
InventoryVoucherItemFinal.Date AS FactoredDate,
Dl.Title AS DlTitle,
DlTemp.Title as DlTempTitle,

ISNULL((
    SELECT SUM(InventoryVoucherFinal.MajorUnitQuantity)
			FROM ERPS.Erps.PRC3.OrderItem AS OrderItemTemp 
			INNER JOIN ERPS.Erps.PRC3.[ORDER] o ON o.OrderID=OrderItemTemp.OrderRef

			INNER  JOIN Erps.Erps.sys3.RecursiveEntityRelation as RecursiveEntityRelation ON RecursiveEntityRelation.SourceRef= OrderItemTemp.OrderItemID 
			INNER JOIN Erps.Erps.sys3.EntityLookup as OrderItemLookup on OrderItemLookup.Code = RecursiveEntityRelation.SourceType AND OrderItemLookup.EntityName = 'SystemGroup.Procurement.OrderManagement,OrderItem'
			INNER JOIN Erps.Erps.sys3.EntityLookup as InventoryVoucherItemLookup on InventoryVoucherItemLookup.Code = RecursiveEntityRelation.TargetType AND InventoryVoucherItemLookup.EntityName = 'SystemGroup.Logistics.VoucherProcessing,InventoryVoucherItem' 

			INNER JOIN Erps.Erps.lgs3.InventoryVoucherItem as InventoryVoucherItem on  InventoryVoucherItem.InventoryVoucherItemID = RecursiveEntityRelation.TargetRef
			INNER  JOIN Erps.Erps.lgs3.InventoryVoucher AS Inventory ON Inventory.InventoryVoucherID = InventoryVoucherItem.InventoryVoucherRef

			------ سند دائم انبار استعلامی
			 JOIN  ERPS.Erps.LGS3.InventoryVoucherItem as InventoryVoucherFinal on  InventoryVoucherFinal.ReferenceRef = InventoryVoucherItem.InventoryVoucherItemID and InventoryVoucherFinal.InventoryVoucherSpecificationRef=2 AND InventoryVoucherFinal.ReferenceType=1

			WHERE OrderItemTemp.OrderItemID = OrderItem.OrderItemID
),0) AS AllFactoredCount


FROM ERPS.Erps.PRC3.PurchaseRequestItem AS PurchaseRequestItem
INNER JOIN ERPs.Erps.PRC3.PurchaseRequest AS PurchaseRequest ON PurchaseRequest.PurchaseRequestID = PurchaseRequestItem.PurchaseRequestRef
INNER JOIN ERPs.Erps.PRC3.PurchaseItem AS PurchaseItem ON PurchaseItem.PurchaseItemID = PurchaseRequestItem.PurchaseItemRef
INNER JOIN ERPs.Erps.LGS3.Part AS Part ON Part.PartID = PurchaseItem.PartRef

INNER JOIN ERPS.Erps.PRC3.OrderItem AS OrderItem ON OrderItem.PurchaseRequestItemRef = PurchaseRequestItem.PurchaseRequestItemID
INNER JOIN ERPS.Erps.PRC3.[ORDER] AS [ORDER] ON [ORDER].OrderID = OrderItem.OrderRef
INNER JOIN ERPS.Erps.PRC3.DeliveryItem AS DeliveryItem ON	DeliveryItem.ReferenceRef = OrderItem.OrderItemID
INNER JOIN ERPS.Erps.PRC3.Delivery AS Delivery ON	Delivery.DeliveryID = DeliveryItem.DeliveryRef

-- مجوز ورود
INNER  JOIN #ReceiptPermitItems AS ReceiptPermitItem on ReceiptPermitItem.ReferenceRef = DeliveryItem.DeliveryItemID 

------سند موقت انبار 
INNER JOIN ERPS.Erps.LGS3.InventoryVoucherItem AS InventoryVoucherItem on InventoryVoucherItem.ReferenceRef = ReceiptPermitItem.ReceiptPermitItemID AND InventoryVoucherItem.ReferenceType = 2  AND InventoryVoucheritem.InventoryVoucherSpecificationRef=9979 
INNER JOIN ERPS.Erps.LGS3.InventoryVoucher InventoryVoucher ON InventoryVoucher.InventoryVoucherID=InventoryVoucherItem.InventoryVoucherRef AND InventoryVoucher.InventoryVoucherSpecificationRef=9979
INNER JOIN ERPS.Erps.SYS3.Lookup AS InventoryVoucherItemStatus ON InventoryVoucherItemStatus.Code = InventoryVoucherItem.State AND InventoryVoucherItemStatus.Type = 'InventoryVoucherState' AND InventoryVoucherItemStatus.System = 'LGS3'

-- سند رد / تایید کیفی
LEFT JOIN QCM3.QualityInspectionResultItem AS QualityInspectionResultItem  ON QualityInspectionResultItem.ReferenceRef = InventoryVoucherItem.InventoryVoucherItemID AND QualityInspectionResultItem.ReferenceType = 64
LEFT JOIN QCM3.QualityInspectionResult AS QualityInspectionResult ON QualityInspectionResultItem.QualityInspectionResultRef = QualityInspectionResult.QualityInspectionResultID

LEFT JOIN
(
	SELECT 
			OrderItem.OrderItemID,
			InventoryVoucherItemFinal.InventoryVoucherItemID,
			InventoryVoucherItemFinal.Number,
			InventoryVoucherItemFinal.Quantity,
			InventoryVoucherItemFinal.Date,
			InventoryVoucherItemFinal.CounterpartEntityRef,
			InventoryVoucherItemFinal.CounterpartEntityCode

	FROM  ERPS.Erps.PRC3.OrderItem AS OrderItem 
	INNER JOIN Erps.Erps.sys3.RecursiveEntityRelation as RecursiveEntityRelation ON RecursiveEntityRelation.SourceRef= OrderItem.OrderItemID 
	INNER JOIN Erps.Erps.sys3.EntityLookup as OrderItemLookup on OrderItemLookup.Code = RecursiveEntityRelation.SourceType AND OrderItemLookup.EntityName = 'SystemGroup.Procurement.OrderManagement,OrderItem'
	INNER JOIN Erps.Erps.sys3.EntityLookup as InventoryVoucherItemLookup on InventoryVoucherItemLookup.Code = RecursiveEntityRelation.TargetType AND InventoryVoucherItemLookup.EntityName = 'SystemGroup.Logistics.VoucherProcessing,InventoryVoucherItem' 
	
	INNER JOIN Erps.Erps.lgs3.InventoryVoucherItem as InventoryVoucherItemFinal on  InventoryVoucherItemFinal.InventoryVoucherItemID = RecursiveEntityRelation.TargetRef and InventoryVoucherItemFinal.InventoryVoucherSpecificationRef=2 AND InventoryVoucherItemFinal.ReferenceType=1
	INNER JOIN Erps.Erps.lgs3.InventoryVoucher AS Inventory ON Inventory.InventoryVoucherID = InventoryVoucherItemFinal.InventoryVoucherRef
	
) AS InventoryVoucherItemFinal ON InventoryVoucherItemFinal.OrderItemID = OrderItem.OrderItemID 


LEFT JOIN ERPS.Erps.FIN3.DL AS Dl ON Dl.ReferenceID = InventoryVoucherItemFinal.CounterpartEntityRef  AND Dl.EntityCode = InventoryVoucher.CounterpartEntityCode
LEFT JOIN ERPS.Erps.FIN3.DL AS DlTemp ON DlTemp.ReferenceID = InventoryVoucherItem.CounterpartEntityRef  AND DlTemp.EntityCode = InventoryVoucher.CounterpartEntityCode


WHERE   
		InventoryVoucherItem.InventoryVoucherItemID IS NOT NULL 
		AND
		Delivery.FiscalYearRef IN ({yearNumbers.GetHamkaranYearNumber()}) 
";
            return await rdb.Database.SqlQueryRaw<HamkaranNotifyDto>(sql).ToListAsync();
        }


        // DTOs
        private class HamkaranOrderDto
        {
            public long PurchaseRequestItemId { get; set; }
            public long? OrderRowId { get; set; }
            public long Year { get; set; }
            public string? OrderNo { get; set; }
            public DateTime? OrderDate { get; set; }
            public DateTime? OrderDateInEurope { get; set; }
            public long? PartRef { get; set; }
            public long? DlRef { get; set; }
            public DateTime? NeedDate { get; set; }
            public DateTime? OrderConfirmDate { get; set; }
            public decimal SumSendQty { get; set; }
            public decimal FactoredCount { get; set; }
            public decimal? OrderQty { get; set; }
            public decimal ReqQty { get; set; }
            public string? OrdItmComment { get; set; }
            public string? PurchaseRequestNumber { get; set; }
            public DateTime? PurchaseRequestDate { get; set; }
            public long? RelatedPartId { get; set; }
            public DateTime? DeliveryDate { get; set; }
            public DateTime? TemporaryVoucherDate { get; set; }
            public DateTime? FinalVoucherDate { get; set; }
        }

        private class HamkaranNotifyDto
        {
            public long PurchaseRequestItemID { get; set; }
            public string? PurchaseRequestNumber { get; set; }
            public long? FinalVchItemId { get; set; }
            public long? SendRefNo { get; set; }
            public string? SendNo { get; set; }
            public string? OrdNo { get; set; }
            public DateTime? SendDate { get; set; }
            public string? PartCode { get; set; }
            public string? PartName { get; set; }
            public string? Comment { get; set; }
            public int? InspctnFlag { get; set; }
            public string? InspectionComment { get; set; }
            public decimal? SendQty { get; set; }
            public decimal? FinalVchQty { get; set; }
            public DateTime? FactoredDate { get; set; }
            public string? DlTempTitle { get; set; }
        }

        private class AllOrderItemDto
        {
            public long PurchaseRequestItemId { get; set; }
            public int? PurchaseRequestStatus { get; set; }
            public int? PurchaseRequestItemStatus { get; set; }
            public long? OrderRowId { get; set; }
            public decimal? OrderQty { get; set; }
            public decimal ReqQty { get; set; }
            public int? OrderStatus { get; set; }
        }

        private class OpenOrderRequestDto
        {
            public long PurchaseRequestItemId { get; set; }
            public int PurchaseRequestState { get; set; }
            public int PurchaseRequestItemState { get; set; }
            public long? OrderRowId { get; set; }
            public int? OrderItemState { get; set; }
            public decimal RequestItemQuantity { get; set; }
            public decimal? OrderItemQuantity { get; set; }
        }

        #region Attachment Migration From HTS

        [JobHandler("انتقال پیوست‌های درخواست‌های باز فعال از HTS")]
        public async Task SyncOpenOrderRequestAttachmentsFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
        {
            try
            {
                if (jobLogger != null)
                    await jobLogger.LogInfoAsync("شروع انتقال پیوست‌های درخواست‌های باز از HTS...", cn);

                var htsAttachmentsMeta = await (
                    from a in htsDb.Hts_Sup_OpenOrderRequest_Attachments.AsNoTracking()
                    join o in htsDb.Hts_Sup_OpenOrderRequests.AsNoTracking()
                        on a.OpenOrderRequest_FK equals o.OpenOrderRequest_ID
                    where !o.IsDeleted
                    select new
                    {
                        a.OpenOrderRequest_Attachment_ID,
                        a.OpenOrderRequest_FK,
                        a.Attachment_FileName,
                        a.AttachmentFilePath,
                        a.Attachment_Comment,
                        a.DocumentTypeId,
                        HasDbContent = a.Attachment_FileContent != null,
                        o.PurchaseRequestItemId,
                        o.OrderRowId
                    }).ToListAsync(cn);

                if (jobLogger != null)
                    await jobLogger.LogInfoAsync($"تعداد پیوست‌های کاندید در HTS: {htsAttachmentsMeta.Count}", cn);

                if (htsAttachmentsMeta.Count == 0)
                    return;

                var localOrders = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
                    .Where(o => !o.IsDeleted)
                    .Select(o => new
                    {
                        Id = o.Id!.Value,
                        o.PurchaseRequestItemId,
                        o.OrderRowId
                    })
                    .ToListAsync(cn);

                var localByPrAndOrder = localOrders
                    .Where(o => o.OrderRowId.HasValue)
                    .GroupBy(o => (o.PurchaseRequestItemId, o.OrderRowId!.Value))
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var localByPrOnly = localOrders
                    .Where(o => !o.OrderRowId.HasValue)
                    .GroupBy(o => o.PurchaseRequestItemId)
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var localByPrAny = localOrders
                    .GroupBy(o => o.PurchaseRequestItemId)
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var existingAttachments = await unitOfWork.Repository<OpenOrderRequestAttachment>().Table
                    .Where(a => a.HtsId != 0)
                    .ToListAsync(cn);
                var existingByHtsId = existingAttachments.ToDictionary(a => a.HtsId);

                var migrated = 0;
                var skipped = 0;
                var failed = 0;
                var updated = 0;

                foreach (var pa in htsAttachmentsMeta)
                {
                    try
                    {
                        var localOrderId = ResolveLocalOpenOrderRequestId(
                            pa.PurchaseRequestItemId,
                            pa.OrderRowId,
                            localByPrAndOrder,
                            localByPrOnly,
                            localByPrAny);

                        if (localOrderId == null)
                        {
                            skipped++;
                            continue;
                        }

                        var fileType = MapDocumentType(pa.DocumentTypeId);
                        var fileName = string.IsNullOrWhiteSpace(pa.Attachment_FileName)
                            ? $"attachment_{pa.OpenOrderRequest_Attachment_ID}"
                            : pa.Attachment_FileName;

                        if (existingByHtsId.TryGetValue(pa.OpenOrderRequest_Attachment_ID, out var existing))
                        {
                            existing.Comment = pa.Attachment_Comment;
                            existing.FileType = fileType;
                            existing.OpenOrderRequestId = localOrderId.Value;
                            updated++;
                            continue;
                        }

                        var fileBytes = await ReadHtsAttachmentBytesAsync(
                            pa.OpenOrderRequest_Attachment_ID,
                            pa.AttachmentFilePath,
                            pa.HasDbContent,
                            cn);

                        if (fileBytes == null || fileBytes.Length == 0)
                        {
                            skipped++;
                            if (jobLogger != null)
                                await jobLogger.LogWarningAsync(
                                    $"فایل پیوست HTS یافت نشد. AttachmentId={pa.OpenOrderRequest_Attachment_ID}, Path={pa.AttachmentFilePath}",
                                    0,
                                    cn);
                            continue;
                        }

                        var uploaded = await fileService.UploadAsync(
                            fileBytes,
                            fileName,
                            null,
                            typeof(OpenOrderRequestAttachment).FullName,
                            nameof(OpenOrderRequestAttachment.Attachment),
                            null,
                            cn);

                        var newAttachment = new OpenOrderRequestAttachment
                        {
                            OpenOrderRequestId = localOrderId.Value,
                            AttachmentId = uploaded.Id,
                            Comment = pa.Attachment_Comment,
                            FileType = fileType,
                            HtsId = pa.OpenOrderRequest_Attachment_ID
                        };

                        await unitOfWork.Repository<OpenOrderRequestAttachment>().AddAsync(newAttachment, cn);
                        existingByHtsId[pa.OpenOrderRequest_Attachment_ID] = newAttachment;
                        migrated++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        if (jobLogger != null)
                            await jobLogger.LogErrorAsync(
                                $"خطا در انتقال پیوست HTS Id={pa.OpenOrderRequest_Attachment_ID}: {ex.Message}",
                                0,
                                cn);
                    }
                }

                await unitOfWork.SaveChangesAsync(cn);

                if (jobLogger != null)
                    await jobLogger.LogInfoAsync(
                        $"پایان انتقال پیوست‌ها. Migrated={migrated}, Updated={updated}, Skipped={skipped}, Failed={failed}",
                        cn);
            }
            catch (Exception ex)
            {
                if (jobLogger != null)
                    await jobLogger.LogErrorAsync($"خطای کلی SyncOpenOrderRequestAttachmentsFromHts: {ex.Message}", 0, cn);
                throw;
            }
        }

        private static long? ResolveLocalOpenOrderRequestId(
            long purchaseRequestItemId,
            long? orderRowId,
            Dictionary<(long PurchaseRequestItemId, long OrderRowId), long> localByPrAndOrder,
            Dictionary<long, long> localByPrOnly,
            Dictionary<long, long> localByPrAny)
        {
            if (orderRowId.HasValue)
            {
                if (localByPrAndOrder.TryGetValue((purchaseRequestItemId, orderRowId.Value), out var byBoth))
                    return byBoth;

                if (localByPrOnly.TryGetValue(purchaseRequestItemId, out var byPrNullOrder))
                    return byPrNullOrder;

                return null;
            }

            if (localByPrOnly.TryGetValue(purchaseRequestItemId, out var byPr))
                return byPr;

            if (localByPrAny.TryGetValue(purchaseRequestItemId, out var byAny))
                return byAny;

            return null;
        }

        private static OpenOrderRequestAttachmentFileTypeEnum? MapDocumentType(short? documentTypeId)
        {
            if (!documentTypeId.HasValue)
                return null;

            var value = (int)documentTypeId.Value;
            return Enum.IsDefined(typeof(OpenOrderRequestAttachmentFileTypeEnum), value)
                ? (OpenOrderRequestAttachmentFileTypeEnum)value
                : null;
        }

        private async Task<byte[]?> ReadHtsAttachmentBytesAsync(
            int attachmentId,
            string? attachmentFilePath,
            bool hasDbContent,
            CancellationToken cn)
        {
            if (!string.IsNullOrWhiteSpace(attachmentFilePath) && File.Exists(attachmentFilePath))
                return await File.ReadAllBytesAsync(attachmentFilePath, cn);

            if (!hasDbContent)
                return null;

            return await htsDb.Hts_Sup_OpenOrderRequest_Attachments.AsNoTracking()
                .Where(a => a.OpenOrderRequest_Attachment_ID == attachmentId)
                .Select(a => a.Attachment_FileContent)
                .FirstOrDefaultAsync(cn);
        }

        #endregion

        #region VPIS Revision Check

        [JobHandler("بررسی تغییر Revision مدارک VPIS و اعلام به واحد فروش/پروژه")]
        public async Task CheckVpisRevisionChangesAndNotify(IJobLogger? jobLogger = null, CancellationToken cn = default)
        {
            try
            {
                jobLogger?.LogInfoAsync("Starting CheckVpisRevisionChangesAndNotify...");

                var openOrderRequestVpisList = await unitOfWork.Repository<OpenOrderRequestVpis>()
                    .TableNoTracking
                    .Include(v => v.OpenOrderRequest)
                    .Where(v => v.IsLatest &&
                          !v.OpenOrderRequest.IsDeleted &&
                          !v.OpenOrderRequest.IsForceDeletedByUser &&
                          v.OpenOrderRequest.SalesUnitSalesExpertId != null)
                    .ToListAsync(cn);

                if (!openOrderRequestVpisList.Any())
                {
                    jobLogger?.LogInfoAsync("No VPIS links found.");
                    return;
                }

                var projectVpisIds = openOrderRequestVpisList.Select(v => v.ProjectVpisId).Distinct().ToList();
                var projectVpisList = await unitOfWork.Repository<ProjectVpis>()
                    .TableNoTracking
                    .Include(pv => pv.ProjectName)
                    .Where(pv => projectVpisIds.Contains(pv.Id!.Value))
                    .ToListAsync(cn);
                var projectVpisById = projectVpisList
                    .Where(pv => pv.Id.HasValue)
                    .ToDictionary(pv => pv.Id!.Value);

                var documents = await unitOfWork.Repository<Document>()
                    .TableNoTracking
                    .Include(d => d.Comments)
                    .Where(d => d.DocumentVpisId != null && projectVpisIds.Contains(d.DocumentVpisId.Value))
                    .ToListAsync(cn);

                var eligibleLatestDocuments = OpenOrderRequestVpisRules.SelectLatestEligiblePerVpis(
                    documents,
                    vpisId => projectVpisById.TryGetValue(vpisId, out var vpis) ? vpis.Title : null,
                    vpisId => projectVpisById.TryGetValue(vpisId, out var vpis)
                        && vpis.ProjectName?.ProjectIsVendoriType == true);

                var changedCount = await OpenOrderRequestVpisRevisionHelper.ApplyRevisionChangesAsync(
                    unitOfWork, eligibleLatestDocuments, cn);

                jobLogger?.LogInfoAsync($"Completed CheckVpisRevisionChangesAndNotify. UpdatedLinks={changedCount}");
            }
            catch (Exception ex)
            {
                if (jobLogger != null)
                {
                    await jobLogger.LogInfoAsync("Error in CheckVpisRevisionChangesAndNotify: " + ex.Message, cn);
                }
                throw;
            }
        }

        #endregion

        #region VPIS bridge from HTS + auto-insert

        /// <summary>
        /// پل روزانه از HTS تا خاموشی: کپی لینک‌های VPIS غایب.
        /// تطبیق درخواست با PurchaseRequestItemId+OrderRowId؛ مدرک/پروژه/VPIS با HtsId.
        /// </summary>
        [JobHandler("همگام‌سازی لینک VPIS درخواست‌های باز از HTS")]
        public async Task SyncOpenOrderRequestVpisFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
        {
            try
            {
                if (jobLogger != null)
                    await jobLogger.LogInfoAsync("شروع همگام‌سازی VPIS درخواست‌های باز از HTS...", cn);

                var htsLinks = await (
                    from v in htsDb.Hts_Sup_OpenOrderRequestVpis.AsNoTracking()
                    join o in htsDb.Hts_Sup_OpenOrderRequests.AsNoTracking()
                        on v.OpenOrderRequestId equals o.OpenOrderRequest_ID
                    select new
                    {
                        v.Id,
                        v.OpenOrderRequestId,
                        v.ProjectId,
                        v.ProjectVpisId,
                        v.DocumentId,
                        v.RevisionNumber,
                        v.IsLatest,
                        v.Comment,
                        o.PurchaseRequestItemId,
                        o.OrderRowId
                    }).ToListAsync(cn);

                if (jobLogger != null)
                    await jobLogger.LogInfoAsync($"تعداد لینک VPIS در HTS: {htsLinks.Count}", cn);

                if (htsLinks.Count == 0)
                {
                    await AutoInsertVpisLinksForActiveRequestsAsync(jobLogger, cn);
                    return;
                }

                var localOrders = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
                    .Select(o => new { Id = o.Id!.Value, o.PurchaseRequestItemId, o.OrderRowId })
                    .ToListAsync(cn);

                var localByPrAndOrder = localOrders
                    .Where(o => o.OrderRowId.HasValue)
                    .GroupBy(o => (o.PurchaseRequestItemId, o.OrderRowId!.Value))
                    .ToDictionary(g => g.Key, g => g.First().Id);
                var localByPrOnly = localOrders
                    .Where(o => !o.OrderRowId.HasValue)
                    .GroupBy(o => o.PurchaseRequestItemId)
                    .ToDictionary(g => g.Key, g => g.First().Id);
                var localByPrAny = localOrders
                    .GroupBy(o => o.PurchaseRequestItemId)
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var projectByHts = await unitOfWork.Repository<Project>().TableNoTracking
                    .Where(p => p.HtsId != 0)
                    .Select(p => new { Id = p.Id!.Value, p.HtsId })
                    .ToDictionaryAsync(p => p.HtsId, p => p.Id, cn);
                var vpisByHts = await unitOfWork.Repository<ProjectVpis>().TableNoTracking
                    .Where(p => p.HtsId != 0)
                    .Select(p => new { Id = p.Id!.Value, p.HtsId })
                    .ToDictionaryAsync(p => p.HtsId, p => p.Id, cn);
                var documentByHts = await unitOfWork.Repository<Document>().TableNoTracking
                    .Where(d => d.HtsId != 0)
                    .Select(d => new { Id = d.Id!.Value, d.HtsId })
                    .ToDictionaryAsync(d => d.HtsId, d => d.Id, cn);

                var existingPairs = (await unitOfWork.Repository<OpenOrderRequestVpis>().TableNoTracking
                    .Select(v => new { v.OpenOrderRequestId, v.DocumentId, v.ProjectVpisId })
                    .ToListAsync(cn))
                    .Select(v => (v.OpenOrderRequestId, v.DocumentId, v.ProjectVpisId))
                    .ToHashSet();

                var inserted = 0;
                var skipped = 0;
                var unmatchedRequest = 0;
                var unmatchedProject = 0;
                var unmatchedVpis = 0;
                var unmatchedDocument = 0;
                var documentIdsForFileCheck = new HashSet<long>();
                var documentIdToHtsId = new Dictionary<long, long>();

                foreach (var link in htsLinks)
                {
                    var localOrderId = ResolveLocalOpenOrderRequestId(
                        link.PurchaseRequestItemId,
                        link.OrderRowId,
                        localByPrAndOrder,
                        localByPrOnly,
                        localByPrAny);
                    if (localOrderId == null)
                    {
                        unmatchedRequest++;
                        skipped++;
                        continue;
                    }

                    if (!projectByHts.TryGetValue(link.ProjectId, out var localProjectId))
                    {
                        unmatchedProject++;
                        skipped++;
                        if (jobLogger != null)
                            await jobLogger.LogWarningAsync($"پروژه HTS {link.ProjectId} برای VPIS {link.Id} یافت نشد", 0, cn);
                        continue;
                    }

                    if (!vpisByHts.TryGetValue(link.ProjectVpisId, out var localVpisId))
                    {
                        unmatchedVpis++;
                        skipped++;
                        if (jobLogger != null)
                            await jobLogger.LogWarningAsync($"ProjectVpis HTS {link.ProjectVpisId} برای VPIS {link.Id} یافت نشد", 0, cn);
                        continue;
                    }

                    long? localDocumentId = null;
                    if (link.DocumentId.HasValue)
                    {
                        if (!documentByHts.TryGetValue(link.DocumentId.Value, out var mappedDocId))
                        {
                            unmatchedDocument++;
                            skipped++;
                            if (jobLogger != null)
                                await jobLogger.LogWarningAsync($"مدرک HTS {link.DocumentId} برای VPIS {link.Id} یافت نشد", 0, cn);
                            continue;
                        }
                        localDocumentId = mappedDocId;
                        documentIdsForFileCheck.Add(mappedDocId);
                        documentIdToHtsId[mappedDocId] = link.DocumentId.Value;
                    }

                    var key = (localOrderId.Value, localDocumentId, localVpisId);
                    if (existingPairs.Contains(key))
                    {
                        skipped++;
                        continue;
                    }

                    await unitOfWork.Repository<OpenOrderRequestVpis>().AddAsync(new OpenOrderRequestVpis
                    {
                        OpenOrderRequestId = localOrderId.Value,
                        ProjectId = localProjectId,
                        ProjectVpisId = localVpisId,
                        DocumentId = localDocumentId,
                        RevisionNumber = link.RevisionNumber,
                        IsLatest = link.IsLatest,
                        Comment = link.Comment
                    }, cn);

                    existingPairs.Add(key);
                    inserted++;
                }

                await unitOfWork.SaveChangesAsync(cn);

                var filesCopied = await EnsureDocumentMainFilesFromHtsAsync(
                    documentIdsForFileCheck,
                    documentIdToHtsId,
                    jobLogger,
                    cn);

                if (jobLogger != null)
                    await jobLogger.LogInfoAsync(
                        $"پایان پل VPIS از HTS. Inserted={inserted}, Skipped={skipped}, UnmatchedRequest={unmatchedRequest}, UnmatchedProject={unmatchedProject}, UnmatchedVpis={unmatchedVpis}, UnmatchedDocument={unmatchedDocument}, FilesCopied={filesCopied}",
                        cn);

                await AutoInsertVpisLinksForActiveRequestsAsync(jobLogger, cn);
            }
            catch (Exception ex)
            {
                if (jobLogger != null)
                    await jobLogger.LogErrorAsync($"خطای کلی SyncOpenOrderRequestVpisFromHts: {ex.Message}", 0, cn);
                throw;
            }
        }

        private const string PortalEdmsRoot = @"D:\PortalData\EDMS";
        private const string EdmsPathMarker = @"\EDMS\";

        /// <summary>
        /// اگر مدرک در سیستم جدید هست ولی MainFile ندارد، بایت فایل را از مسیر HTS/Portal می‌خواند
        /// و با همان الگوی UploadAsync پیوست‌ها در FileEntity ذخیره می‌کند.
        /// </summary>
        private async Task<int> EnsureDocumentMainFilesFromHtsAsync(
            HashSet<long> localDocumentIds,
            Dictionary<long, long> localIdToHtsId,
            IJobLogger? jobLogger,
            CancellationToken cn)
        {
            if (localDocumentIds.Count == 0)
                return 0;

            var documents = await unitOfWork.Repository<Document>().Table
                .Where(d => d.Id != null && localDocumentIds.Contains(d.Id.Value) && d.MainFileId == null)
                .ToListAsync(cn);

            if (documents.Count == 0)
                return 0;

            var copied = 0;
            foreach (var document in documents)
            {
                try
                {
                    if (!document.Id.HasValue)
                        continue;

                    if (!localIdToHtsId.TryGetValue(document.Id.Value, out var htsDocumentId))
                        htsDocumentId = document.HtsId;

                    if (htsDocumentId == 0)
                        continue;

                    var hts = await htsDb.Hts_Edms_Documents.AsNoTracking()
                        .Where(d => d.Document_ID == (int)htsDocumentId)
                        .Select(d => new { d.Document_FilePath, d.Document_FileName })
                        .FirstOrDefaultAsync(cn);

                    if (hts == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(hts.Document_FilePath) && string.IsNullOrWhiteSpace(hts.Document_FileName))
                        continue;

                    var fileBytes = ReadHtsDocumentFileBytes(hts.Document_FilePath);
                    if (fileBytes == null || fileBytes.Length == 0)
                    {
                        if (jobLogger != null)
                            await jobLogger.LogWarningAsync(
                                $"فایل مدرک HTS یافت نشد. DocumentId={htsDocumentId}, Path={hts.Document_FilePath}",
                                0,
                                cn);
                        continue;
                    }

                    var fileName = string.IsNullOrWhiteSpace(hts.Document_FileName)
                        ? $"document_{htsDocumentId}"
                        : hts.Document_FileName.Trim();

                    var uploaded = await fileService.UploadAsync(
                        fileBytes,
                        fileName,
                        null,
                        typeof(Document).FullName,
                        nameof(Document.MainFile),
                        document.Id,
                        cn);

                    document.MainFileId = uploaded.Id;
                    copied++;
                }
                catch (Exception ex)
                {
                    if (jobLogger != null)
                        await jobLogger.LogErrorAsync(
                            $"خطا در کپی فایل مدرک DocumentId={document.Id}: {ex.Message}",
                            0,
                            cn);
                }
            }

            if (copied > 0)
                await unitOfWork.SaveChangesAsync(cn);

            return copied;
        }

        private static byte[]? ReadHtsDocumentFileBytes(string? htsPath)
        {
            var sourcePath = LocatePortalEdmsFile(htsPath);
            if (string.IsNullOrWhiteSpace(sourcePath))
                return null;

            try
            {
                var info = new FileInfo(sourcePath);
                if (!info.Exists || info.Length == 0)
                    return null;
                return File.ReadAllBytes(sourcePath);
            }
            catch
            {
                return null;
            }
        }

        private static string? LocatePortalEdmsFile(string? htsPath)
        {
            if (string.IsNullOrWhiteSpace(htsPath))
                return null;

            var normalized = htsPath.Trim().Replace('/', '\\');
            if (File.Exists(normalized))
                return normalized;

            var markerIndex = normalized.IndexOf(EdmsPathMarker, StringComparison.OrdinalIgnoreCase);
            string? expected;
            if (markerIndex >= 0)
            {
                var relative = normalized[(markerIndex + EdmsPathMarker.Length)..].TrimStart('\\');
                expected = Path.Combine(PortalEdmsRoot, relative);
            }
            else
            {
                expected = normalized;
            }

            return File.Exists(expected) ? expected : null;
        }

        private async Task AutoInsertVpisLinksForActiveRequestsAsync(IJobLogger? jobLogger, CancellationToken cn)
        {
            var candidates = await unitOfWork.Repository<OpenOrderRequest>().Table
                .Include(o => o.ProductionOrder)
                .Where(o => !o.IsDeleted && !o.IsForceDeletedByUser
                    && o.ProductionOrderId != null
                    && o.ProductionOrder != null
                    && o.ProductionOrder.EdmsProject != null)
                .ToListAsync(cn);

            await AutoInsertVpisLinksForRequestsAsync(candidates, jobLogger, cn);
        }

        /// <summary>
        /// پورت شرط HTS «درج بصورت اتوماتیک توسط سیستم»:
        /// درخواست فعال با سفارش ساخت دارای EdmsProject و آخرین مدارک معتبر پروژه
        /// (همان فیلتر GetVpisListByProject). اگر لینک (درخواست + ProjectVpis) نبود درج می‌شود.
        /// </summary>
        private async Task AutoInsertVpisLinksForRequestsAsync(
            List<OpenOrderRequest> requests,
            IJobLogger? jobLogger,
            CancellationToken cn)
        {
            var withProject = requests
                .Where(o => o.Id.HasValue && o.ProductionOrder?.EdmsProject != null)
                .Select(o => new { Request = o, HtsProjectId = (long)o.ProductionOrder!.EdmsProject!.Value })
                .ToList();
            if (withProject.Count == 0)
                return;

            var htsProjectIds = withProject.Select(x => x.HtsProjectId).Distinct().ToList();
            var projects = await unitOfWork.Repository<Project>().TableNoTracking
                .Where(p => htsProjectIds.Contains(p.HtsId))
                .Select(p => new { Id = p.Id!.Value, p.HtsId })
                .ToListAsync(cn);
            var projectByHts = projects.ToDictionary(p => p.HtsId, p => p.Id);
            if (projectByHts.Count == 0)
            {
                if (jobLogger != null)
                    await jobLogger.LogInfoAsync("AutoInsert VPIS: هیچ پروژه‌ای با HtsId=EdmsProject یافت نشد.", cn);
                return;
            }

            var localProjectIds = projectByHts.Values.Distinct().ToList();
            var projectVpisList = await unitOfWork.Repository<ProjectVpis>().TableNoTracking
                .Include(pv => pv.ProjectName)
                .Where(pv => pv.ProjectNameId != null && localProjectIds.Contains(pv.ProjectNameId.Value))
                .Select(pv => new
                {
                    Id = pv.Id!.Value,
                    ProjectId = pv.ProjectNameId!.Value,
                    pv.Title,
                    IsVendor = pv.ProjectName != null && pv.ProjectName.ProjectIsVendoriType
                })
                .ToListAsync(cn);
            if (projectVpisList.Count == 0)
                return;

            var projectVpisIds = projectVpisList.Select(pv => pv.Id).ToList();
            var projectVpisById = projectVpisList.ToDictionary(pv => pv.Id);
            var documents = await unitOfWork.Repository<Document>().TableNoTracking
                .Include(d => d.Comments)
                .Where(d => d.DocumentVpisId != null && projectVpisIds.Contains(d.DocumentVpisId.Value))
                .ToListAsync(cn);

            var latestValidDocs = OpenOrderRequestVpisRules.SelectLatestEligiblePerVpis(
                documents,
                vpisId => projectVpisById.TryGetValue(vpisId, out var vpis) ? vpis.Title : null,
                vpisId => projectVpisById.TryGetValue(vpisId, out var vpis) && vpis.IsVendor);

            var vpisByProject = projectVpisList.GroupBy(pv => pv.ProjectId).ToDictionary(g => g.Key, g => g.ToList());
            var docsByVpis = latestValidDocs
                .Where(d => d.DocumentVpisId.HasValue)
                .GroupBy(d => d.DocumentVpisId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var requestIds = withProject.Select(x => x.Request.Id!.Value).ToList();
            var existing = (await unitOfWork.Repository<OpenOrderRequestVpis>().Table
                .Where(v => requestIds.Contains(v.OpenOrderRequestId))
                .Select(v => new { v.OpenOrderRequestId, v.ProjectVpisId })
                .ToListAsync(cn))
                .Select(v => (v.OpenOrderRequestId, v.ProjectVpisId))
                .ToHashSet();

            const string autoComment = "درج بصورت اتوماتیک توسط سیستم";
            var inserted = 0;

            foreach (var item in withProject)
            {
                if (!projectByHts.TryGetValue(item.HtsProjectId, out var localProjectId))
                    continue;
                if (!vpisByProject.TryGetValue(localProjectId, out var vpisRows))
                    continue;

                foreach (var vpis in vpisRows)
                {
                    if (!docsByVpis.TryGetValue(vpis.Id, out var doc))
                        continue;
                    var key = (item.Request.Id!.Value, vpis.Id);
                    if (existing.Contains(key))
                        continue;

                    await unitOfWork.Repository<OpenOrderRequestVpis>().AddAsync(new OpenOrderRequestVpis
                    {
                        OpenOrderRequestId = item.Request.Id.Value,
                        ProjectId = localProjectId,
                        ProjectVpisId = vpis.Id,
                        DocumentId = doc.Id,
                        RevisionNumber = doc.Revision,
                        IsLatest = true,
                        Comment = autoComment
                    }, cn);
                    existing.Add(key);
                    inserted++;
                }
            }

            if (inserted > 0)
                await unitOfWork.SaveChangesAsync(cn);

            if (jobLogger != null)
                await jobLogger.LogInfoAsync($"AutoInsert VPIS: {inserted} لینک برای {withProject.Count} درخواست.", cn);
        }

        #endregion

    }
}
