using App.BackgroundJob.Jobs.Inv;
using Common.Attributes;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.FIN;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.App.Sup;
using Entities.Base.Notification;
using Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Data;
using System.Globalization;
using System.Text;
using Entities.App.Edms;
using Entities.Base.Enums;

namespace App.BackgroundJob.Jobs.Sup
{
    public class OpenOrderRequestJob(ApplicationDbContext dbContext, RahkaranDbContext rdb, IUnitOfWork unitOfWork)
    {
        [JobHandler("هماهنگ کردن اطلاعات درخواست های باز از راهکاران")]
        public async Task SyncOpenOrderRequestJobFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
        {
            try
            {
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
                                localOrder.IsDeleted = false;
                                localOrder.IsDeletedMiladiDate = null;
                                 localOrder.IsDeletedShamsiDate = null;
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
                                    localOrder.FinalInventoryVoucherShamsiDate = remoteOrder.FinalVoucherDate.Value.ToShamsiDate();
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

                // 8. Notifications
                await SendNotifications(yearNumbers, jobLogger, cn);

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

        private async Task SendNotifications(string yearNumbers, IJobLogger? logger, CancellationToken cn)
        {
            var newEvents = await GetNewInvVchForNotify(yearNumbers);
            var users = await dbContext.Users.ToListAsync(cn);
            var localOrders = await unitOfWork.Repository<OpenOrderRequest>().Table.ToListAsync(cn);

            foreach (var evt in newEvents)
            {
                try
                {
                    var order = localOrders.FirstOrDefault(x => x.PurchaseRequestItemId == evt.PurchaseRequestItemID && x.PurchaseRequestNumber.ToString() == evt.PurchaseRequestNumber && x.OrderRowId  == evt.SendRefNo);
                    if (order == null) continue;

                    string? title = null;
                    string? body = null;

                    if (evt.InspctnFlag == 0 && !order.IsRejectedByInspection)
                    {
                        title = "عدم تایید کالا توسط QC";
                        body = $"کالا با کد {evt.PartCode} مربوط به سفارش {evt.OrdNo} توسط واحد کیفیت رد شد. <br/> شرح: {evt.InspectionComment}";
                        order.IsRejectedByInspection = true;
                    }
                    else if (evt.FinalVchItemId.HasValue)
                    {
                        if (order.RequiredQty == order.FactoredCount)
                        {
                            title = "رسید کالای درخواستی در انبار";
                            body = $"کالای {evt.PartName} (سفارش {evt.OrdNo}) به مقدار {evt.FinalVchQty} رسید شد.";
                        }
                    }

                    if (title != null)
                    {
                        var recipients = new List<string>();
                        if (!string.IsNullOrEmpty(order.RequestedPersonelEmail)) recipients.AddRange(order.RequestedPersonelEmail.Split(';'));
                        if (!string.IsNullOrEmpty(order.RequestedEngineeringPersonelEmail)) recipients.AddRange(order.RequestedEngineeringPersonelEmail.Split(';'));

                        foreach (var email in recipients.Where(e => !string.IsNullOrWhiteSpace(e)))
                        {
                            var cleanEmail = email.Trim();
                            var user = users.FirstOrDefault(u => u.Email != null && u.Email.Equals(cleanEmail, StringComparison.OrdinalIgnoreCase));
                            if (user != null)
                            {
                                await unitOfWork.Repository<Notification>().AddAsync(new Notification
                                {
                                    Title = title,
                                    Body = body,
                                    EntityId = order.Id,
                                    OwnerId = user.Id!.Value,
                                    ViewPath = $"Panel/Sup/OpenOrderRequest/Edit/{order.Id}",
                                    IsRead = false
                                }, cn);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (logger != null)
                    {
                        await logger.LogInfoAsync($"Error creating notification for {evt.PurchaseRequestItemID}: {ex.Message}", cn);
                    }
                }
            }
            await unitOfWork.SaveChangesAsync(cn);
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
                var openRequestKeys = openOrderRequests.Select(x => x.PurchaseRequestItemId).ToHashSet();

                foreach (var local in localOrders)
                {
                    if (local.IsDeleted && !local.IsForceDeletedByUser && openRequestKeys.Contains(local.PurchaseRequestItemId))
                    {
                        var matchingRemote = openOrderRequests.FirstOrDefault(x => x.PurchaseRequestItemId == local.PurchaseRequestItemId);
                        if (matchingRemote != null)
                        {
                            // Reactivate if not manually deleted and not contains "توسط" in comment
                            if (local.Comment?.Contains("توسط") != true)
                            {
                                // Request reactivation (no OrderRowId check)
                                if (local.OrderRowId == null)
                                {
                                    local.IsDeleted = false;
								local.IsDeletedMiladiDate = now;
								local.IsDeletedShamsiDate = now.ToShamsiDateTime();
									local.Comment = (local.Comment ?? "") + (local.Comment?.Contains("راه اندازی دوباره") == true ? "" : "|| راه اندازی دوباره بجهت خروج از اختتام");
                                }
                                // Order reactivation (with OrderRowId check and quantity check)
                                else if (local.OrderRowId == matchingRemote.OrderRowId)
                                {
                                    if (local.FactoredCount < matchingRemote.RequestItemQuantity && !local.IsStop)
                                    {
                                        local.IsDeleted = false;
							     local.IsDeletedMiladiDate = now;
							     local.IsDeletedShamsiDate = now.ToShamsiDateTime();
							     local.Comment = (local.Comment ?? "") + (local.Comment?.Contains("راه اندازی دوباره") == true ? "" : "|| راه اندازی دوباره بجهت خروج از اختتام");
                                    }
                                }
                            }
                        }
                    }
                }

                // 7. State-based Deletion (PurchaseRequestItemStatus IN 5,6,9 = terminated/cancelled)
                var terminatedItemIds = allOrderItemsWithState
                    .Where(x => x.PurchaseRequestItemStatus != null && (x.PurchaseRequestItemStatus == 5 || x.PurchaseRequestItemStatus == 6 || x.PurchaseRequestItemStatus == 9))
                    .Select(x => x.PurchaseRequestItemId)
                    .ToHashSet();

                foreach (var local in localOrders)
                {
                    if (terminatedItemIds.Contains(local.PurchaseRequestItemId) && !local.IsDeleted && !local.IsForceDeletedByUser)
                    {
                        // Check date filter
                        if (local.PurchaseRequestMiladiDate.HasValue && local.PurchaseRequestMiladiDate.Value > new DateTime(2023, 3, 20))
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

                // 10. Update IsRoutineRequest
                // Note: IsRoutineRequest is non-nullable bool with default false in new system
                // Legacy logic: IsRoutineRequest = (SalesUnitProjectManagerId IS NULL)
                // Skipping as it's already set to default false in entity

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
SELECT ReceiptPermitItemID , ReferenceRef 
INTO #ReceiptPermitItems
FROM Erps.LGS3.ReceiptPermitItem

SELECT 
PurchaseRequestItem.PurchaseRequestItemID,
PurchaseRequest.Number AS PurchaseRequestNumber,

InventoryVoucherItem.InventoryVoucherItemID AS VchItmID,
OrderItem.OrderItemID AS SendRefNo,
Delivery.Number AS SendNo ,
[ORDER].Number AS OrdNo,

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
            public string? OrdNo { get; set; }
            public string? PartCode { get; set; }
            public string? PartName { get; set; }
            public int? InspctnFlag { get; set; }
            public string? InspectionComment { get; set; }
            public decimal? FinalVchQty { get; set; }
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

        #region VPIS Revision Check

        [JobHandler("بررسی تغییر Revision مدارک VPIS و اعلام به واحد فروش/پروژه")]
        public async Task CheckVpisRevisionChangesAndNotify(IJobLogger? jobLogger = null, CancellationToken cn = default)
        {
            try
            {
                jobLogger?.LogInfoAsync("Starting CheckVpisRevisionChangesAndNotify...");

                // دریافت تمام OpenOrderRequestVpis که IsLatest هستند
                var openOrderRequestVpisList = await unitOfWork.Repository<OpenOrderRequestVpis>()
                    .Table
                    .Include(v => v.OpenOrderRequest)
                    .Include(v => v.Document)
                    .Include(v => v.ProjectVpis)
                    .Include(v => v.Project)
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

                // دریافت تمام Documents مرتبط
                var projectVpisIds = openOrderRequestVpisList.Select(v => v.ProjectVpisId).Distinct().ToList();
                var latestDocuments = (await unitOfWork.Repository<Document>()
                    .TableNoTracking
                    .Where(d => projectVpisIds.Contains(d.DocumentVpisId!.Value))
                    .GroupBy(d => d.DocumentVpisId)
                    .Select(g => g.OrderByDescending(d => d.Id).FirstOrDefault())
                    .ToListAsync(cn))
                    .Where(d => d != null)
                    .Select(d => d!)
                    .ToList();

                var changedLinks = new List<OpenOrderRequestVpis>();

                foreach (var vpis in openOrderRequestVpisList)
                {
                    var latestDoc = latestDocuments.FirstOrDefault(d => d.DocumentVpisId == vpis.ProjectVpisId);
                    if (latestDoc == null) continue;

                    // اگر Revision تغییر کرده باشد
                    if (vpis.RevisionNumber != latestDoc.Revision)
                    {
                        changedLinks.Add(vpis);
                        jobLogger?.LogInfoAsync($"Revision changed for OpenOrderRequestId={vpis.OpenOrderRequestId}, Old={vpis.RevisionNumber}, New={latestDoc.Revision}");
                    }
                }

                if (!changedLinks.Any())
                {
                    jobLogger?.LogInfoAsync("No revision changes found.");
                    return;
                }

                // گروه‌بندی بر اساس OpenOrderRequestId
                var groupedByRequest = changedLinks.GroupBy(v => v.OpenOrderRequestId);

                foreach (var group in groupedByRequest)
                {
                    var openOrderRequestId = group.Key;
                    var vpisList = group.ToList();

                    // دریافت OpenOrderRequest
                    var openOrderRequest = vpisList.First().OpenOrderRequest;

                    // غیرفعال کردن تایید فروش
                    openOrderRequest.HasSalesUnitConfirmation = false;

                    // غیرفعال کردن لینک‌های قبلی
                    foreach (var vpis in vpisList)
                    {
                        vpis.IsLatest = false;
                    }

                    await unitOfWork.SaveChangesAsync(cn);

                    // ایجاد لینک‌های جدید با Revision بروز
                    var now = DateTime.Now;
                    foreach (var vpis in vpisList)
                    {
                        var latestDoc = latestDocuments.FirstOrDefault(d => d.DocumentVpisId == vpis.ProjectVpisId);
                        if (latestDoc == null) continue;

                        var newLink = new OpenOrderRequestVpis
                        {
                            OpenOrderRequestId = vpis.OpenOrderRequestId,
                            ProjectId = vpis.ProjectId,
                            ProjectVpisId = vpis.ProjectVpisId,
                            DocumentId = latestDoc.Id,
                            RevisionNumber = latestDoc.Revision,
                            IsLatest = true,
                            Comment = $"بروزرسانی خودکار Revision از {vpis.RevisionNumber} به {latestDoc.Revision}"
                        };

                        await unitOfWork.Repository<OpenOrderRequestVpis>().AddAsync(newLink, cn);
                    }

                    // ثبت کامنت
                    var comment = new OpenOrderRequestComment
                    {
                        OpenOrderRequestId = openOrderRequestId,
                        MiladiDate = now,
                        ShamsiDate = now.ToShamsiDate(),
                        HasSalesUnitConfirmation = false,
                        SalesUnitConfirmationComment = "منتظر تایید پروژه/فروش بعلت تغییر در رویژن مدرک مهندسی",
                        CommentValue = $"تغییر Revision مدارک VPIS - نیاز به تایید مجدد واحد فروش/پروژه"
                    };

                    await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);
                    await unitOfWork.SaveChangesAsync(cn);

                    // ارسال Notification به واحد فروش/پروژه
                    await SendVpisRevisionChangeNotification(openOrderRequest, vpisList, latestDocuments, cn);

                    jobLogger?.LogInfoAsync($"Processed OpenOrderRequestId={openOrderRequestId}");
                }

                jobLogger?.LogInfoAsync("Completed CheckVpisRevisionChangesAndNotify.");
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

        private async Task SendVpisRevisionChangeNotification(OpenOrderRequest openOrderRequest, 
            List<OpenOrderRequestVpis> changedVpis, List<Document> latestDocuments, CancellationToken cn)
        {
            try
            {
                var recipientUserIds = new List<long>();

                if (openOrderRequest.SalesUnitSalesExpertId.HasValue)
                    recipientUserIds.Add(openOrderRequest.SalesUnitSalesExpertId.Value);
                
                if (openOrderRequest.SalesUnitSalesManagerId.HasValue)
                    recipientUserIds.Add(openOrderRequest.SalesUnitSalesManagerId.Value);
                
                if (openOrderRequest.SalesUnitProjectManagerId.HasValue)
                    recipientUserIds.Add(openOrderRequest.SalesUnitProjectManagerId.Value);

                if (!recipientUserIds.Any())
                    return;

                var firstVpis = changedVpis.FirstOrDefault();
                if (firstVpis == null) return;

                var project = firstVpis.Project;
                var projectName = project?.ProjectName ?? "نامشخص";
                var projectCode = project?.Code ?? "-";

                var title = "تغییر Revision مدرک مهندسی - نیاز به تایید مجدد";
                var body = $@"
                    <div style='text-align:center;direction:rtl'>
                        <table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='100%'>
                            <tr style='background: #000aa0'>
                                <td><div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center'>گروه صنعتی هوایار</div></td>
                            </tr>
                            <tr><td>
                                <div style='font-size:12pt;text-align:right;direction:rtl'>
                                    <p>با سلام و احترام</p>
                                    <p>رویژن جدیدی از مدارک پروژه در سامانه بارگذاری گردید. لذا خواهشمند است نسبت به بررسی مجدد درخواست های باز اقدام فرمایید.</p>
                                    <ul>
                                        <li>کد پروژه: <strong>{projectCode}</strong></li>
                                        <li>عنوان پروژه: <strong>{projectName}</strong></li>
                                        <li>شماره درخواست خرید: <strong>{openOrderRequest.PurchaseRequestNumber}</strong></li>
                                        <li>شماره سفارش خرید: <strong>{openOrderRequest.OrderNo}</strong></li>
                                    </ul>
                                </div>
                            </td></tr>
                        </table>
                    </div>
                ";

                foreach (var userId in recipientUserIds.Distinct())
                {
                    await unitOfWork.Repository<Notification>().AddAsync(new Notification
                    {
                        Type = NotificationType.Email,
                        Title = title,
                        Body = body,
                        EntityId = openOrderRequest.Id,
                        OwnerId = userId,
                        ViewPath = $"Panel/Sup/OpenOrderRequest/Edit/{openOrderRequest.Id}",
                        IsRead = false
                    }, cn);
                }

                await unitOfWork.SaveChangesAsync(cn);
            }
            catch
            {
                // Log error but don't throw - این نباید کل Job را متوقف کند
            }
        }

        #endregion

    }
}
