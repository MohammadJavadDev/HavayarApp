/*
JobStep_Rahkaran_ProductionOrder_InsertCommentAndSendEmail.sql
================================================================================
SQL Agent job STEP batch for Erps / Rahkaran (NOT a HavayarApp BackgroundJob).
Do not run sp_add_job here — paste this as the job step command.

Replaces legacy HTS_ProductionOrder_InsertCommentAndSendEmailJob:
  - Was: read/write TMS.TotalSystem (Pln_ProductionOrder / Pln_ProductionOrderComment / Gnr_User)
  - Now: read/write linked server [Portal.HavayarApp] catalog HavayarApp
    (Sale.ProductionOrder / Sale.ProductionOrderComment / system.[User])

Match key: Sale.ProductionOrder.HamkaranId = USR3.Sale_ProductionOrder.Sale_ProductionOrderID
State map: Rahkaran 2→2 (FinancialUnitReview), 3→3 (FinancialApproval), else→4 (Obsolete)
User map: system.[User].HamkaranId = Sale_ProductionOrder.Creator, else CreatedById=1

Encoding: UTF-8 with BOM (Persian subject/body)
Catalog confirmed via MCP user-mssql-havayar: HavayarApp

Note: four-part DML is used (same style as original TMS.TotalSystem.dbo...).
If the linked-server provider rejects INSERT/UPDATE, wrap remote DML in OPENQUERY.
*/

SET NOCOUNT ON;

-- همه متغیرها بیرون از WHILE. DECLARE داخل حلقه در جاب Agent خطای 137 می‌دهد
-- و در تکرار بعدی هم «already been declared» می‌شود.
DECLARE @RowsCount INT;
DECLARE @ID BIGINT;
DECLARE @StateId INT;
DECLARE @ProductionOrderNumber INT;
DECLARE @ContractID BIGINT;
DECLARE @ContarctNumber NVARCHAR(20);
DECLARE @ContractPrice DECIMAL(28, 6);
DECLARE @ManagementConfirmationAttachmentId BIGINT;
DECLARE @DepositFactorAttachmentId BIGINT;
DECLARE @PreFactorAttachmentId BIGINT;
DECLARE @ContractAttachmentId BIGINT;
DECLARE @ContractTitle NVARCHAR(1024);
DECLARE @CreatorFullname NVARCHAR(255);
DECLARE @CreatorEmail NVARCHAR(255);
DECLARE @SalesExpertUserEmail NVARCHAR(255);
DECLARE @AlternativeSalesExpertUserEmail NVARCHAR(255);
DECLARE @SalesManagerUserEmail NVARCHAR(255);
DECLARE @SalesProjectManagerUserEmail NVARCHAR(255);
DECLARE @AttachmentPaths NVARCHAR(MAX);
DECLARE @AttachmentFilePath NVARCHAR(1024);
DECLARE @RootPath NVARCHAR(100) = N'D:\HtsTemp\';
DECLARE @AttachmentsCount INT;
DECLARE @AttachmentId INT;
DECLARE @Subject NVARCHAR(1024);
DECLARE @Body NVARCHAR(MAX);
DECLARE @Recipients NVARCHAR(200);
DECLARE @CopyRecipients NVARCHAR(200);
DECLARE @Path NVARCHAR(1024);

IF OBJECT_ID(N'tempdb..#TempTable') IS NOT NULL
	DROP TABLE #TempTable;

-- Target app state from Rahkaran State (enum ints, NOT old HTS lookup 2084/2057/2207)
SELECT
	AppProductionOrder.Id AS AppProductionOrderId,
	(CASE
		WHEN SaleProductionOrder.State = 2 THEN 2  -- FinancialUnitReview
		WHEN SaleProductionOrder.State = 3 THEN 3  -- FinancialApproval
		ELSE 4                                      -- Obsolete
	END) AS TargetState,
	AppProductionOrder.State AS PreviousState,
	ISNULL(AppUser.Id, 1) AS CreatedById,
	GETDATE() AS CreatedOnMiladiDateTime,
	SYS3.fn_DateToShamsiDate(GETDATE()) + N' ' + CONVERT(VARCHAR(5), GETDATE(), 108) AS CreatedOnShamsiDateTime,
	CAST(NULL AS NVARCHAR(2048)) AS Comment,
	SaleProductionOrder.Sale_ProductionOrderID,
	SaleProductionOrder.ProductionOrderNumber
INTO #TempTable
FROM USR3.Sale_ProductionOrder AS SaleProductionOrder
INNER JOIN [Portal.HavayarApp].[HavayarApp].[Sale].[ProductionOrder] AS AppProductionOrder
	ON AppProductionOrder.HamkaranId = SaleProductionOrder.Sale_ProductionOrderID
LEFT JOIN [Portal.HavayarApp].[HavayarApp].[system].[User] AS AppUser
	ON AppUser.HamkaranId = SaleProductionOrder.Creator
WHERE SaleProductionOrder.IsShouldUpdateHts = 1
	AND AppProductionOrder.State <> (CASE
		WHEN SaleProductionOrder.State = 2 THEN 2
		WHEN SaleProductionOrder.State = 3 THEN 3
		ELSE 4
	END);

-- Insert status comments on HavayarApp (raw INSERT does not run ProductionOrderCommentAction)
INSERT INTO [Portal.HavayarApp].[HavayarApp].[Sale].[ProductionOrderComment]
(
	ProductionOrderId,
	PreviousState,
	NewState,
	Comment,
	CreatedById,
	CreatedOnMiladiDateTime,
	CreatedOnShamsiDateTime,
	IsActive,
	HtsId
)
SELECT
	AppProductionOrderId,
	PreviousState,
	TargetState,
	Comment,
	CreatedById,
	CreatedOnMiladiDateTime,
	CreatedOnShamsiDateTime,
	1,   -- IsActiveEnum.Active
	0    -- not from HTS; column NOT NULL with default ((0))
FROM #TempTable;

-- Equivalent of HTS trigger UpdateProductionOrderStatus: sync header State
UPDATE AppProductionOrder
SET AppProductionOrder.State = T.TargetState
FROM [Portal.HavayarApp].[HavayarApp].[Sale].[ProductionOrder] AS AppProductionOrder
INNER JOIN #TempTable AS T
	ON T.AppProductionOrderId = AppProductionOrder.Id
WHERE AppProductionOrder.State <> T.TargetState;

IF ((SELECT COUNT(*) FROM #TempTable) > 0)
BEGIN

	IF OBJECT_ID(N'tempdb..#ShouldUpdateHtsTemp') IS NOT NULL
		DROP TABLE #ShouldUpdateHtsTemp;

	-- Quirk preserved: walk ALL IsShouldUpdateHts=1 rows (not only #TempTable)
	SELECT
		RowNum = (ROW_NUMBER() OVER (ORDER BY SaleProductionOrder.Sale_ProductionOrderID)),
		SaleProductionOrder.*
	INTO #ShouldUpdateHtsTemp
	FROM USR3.Sale_ProductionOrder AS SaleProductionOrder
	WHERE SaleProductionOrder.IsShouldUpdateHts = 1;

	SET @RowsCount = (SELECT COUNT(*) FROM #ShouldUpdateHtsTemp);

	WHILE @RowsCount > 0
	BEGIN

		SELECT
			@ID = Sale_ProductionOrderID,
			@StateId = State
		FROM #ShouldUpdateHtsTemp
		WHERE RowNum = @RowsCount;

		IF (@StateId IN (2, 3))
		BEGIN

			SET @ProductionOrderNumber = NULL;
			SET @ContractID = NULL;
			SET @ContarctNumber = NULL;
			SET @ContractPrice = NULL;
			SET @ManagementConfirmationAttachmentId = NULL;
			SET @DepositFactorAttachmentId = NULL;
			SET @PreFactorAttachmentId = NULL;
			SET @ContractAttachmentId = NULL;
			SET @ContractTitle = NULL;
			SET @CreatorFullname = NULL;
			SET @CreatorEmail = NULL;
			SET @SalesExpertUserEmail = NULL;
			SET @AlternativeSalesExpertUserEmail = NULL;
			SET @SalesManagerUserEmail = NULL;
			SET @SalesProjectManagerUserEmail = NULL;
			SET @AttachmentPaths = N'';
			SET @AttachmentFilePath = N'';
			SET @Subject = NULL;
			SET @Body = NULL;
			SET @Recipients = N'Bayat.fa@havayar.com';
			SET @CopyRecipients = N'Ahrarnejad.h@havayar.com;';
			SET @Path = N'';

			SELECT
				@ProductionOrderNumber = ProductionOrderNumber,
				@ContractID = ContractIdRef,
				@CreatorFullname = CreatorParty.FullName,
				@CreatorEmail = CreatorParty.Email,
				@SalesExpertUserEmail = SalesExpertUserParty.Email,
				@AlternativeSalesExpertUserEmail = AlternativeSalesExpertUserParty.Email,
				@SalesManagerUserEmail = SalesManagerUserParty.Email,
				@SalesProjectManagerUserEmail = SalesProjectManagerUserParty.Email,
				@ManagementConfirmationAttachmentId = ManagementConfirmationAttachment,
				@DepositFactorAttachmentId = DepositFactorAttachment,
				@PreFactorAttachmentId = PreFactorAttachment,
				@ContractAttachmentId = ContractAttachment,
				@ContractTitle = CustomerParty.FullName
			FROM USR3.Sale_ProductionOrder AS SaleProductionOrder
			JOIN SLS3.Contract AS ContractHeader ON ContractHeader.ContractID = SaleProductionOrder.ContractIdRef
			JOIN SLS3.Customer AS ContractCustomer ON ContractCustomer.CustomerID = ContractHeader.CustomerRef
			JOIN GNR3.Party AS CustomerParty ON CustomerParty.PartyID = ContractCustomer.PartyRef
			JOIN SYS3.[User] AS Creator ON Creator.UserID = SaleProductionOrder.Creator
			JOIN GNR3.Party AS CreatorParty ON CreatorParty.PartyID = Creator.PartyRef
			LEFT JOIN HCM3.Employee AS SalesExpertEmployee ON SalesExpertEmployee.EmployeeID = SaleProductionOrder.SalesExpertIdRef
			LEFT JOIN GNR3.Party AS SalesExpertUserParty ON SalesExpertUserParty.PartyID = SalesExpertEmployee.PartyRef
			LEFT JOIN HCM3.Employee AS AlternativeSalesExpertEmployee ON AlternativeSalesExpertEmployee.EmployeeID = SaleProductionOrder.AlternativeExpertIdRef
			LEFT JOIN GNR3.Party AS AlternativeSalesExpertUserParty ON AlternativeSalesExpertUserParty.PartyID = AlternativeSalesExpertEmployee.PartyRef
			LEFT JOIN HCM3.Employee AS SalesManagerEmployee ON SalesManagerEmployee.EmployeeID = SaleProductionOrder.SalesManagerIdRef
			LEFT JOIN GNR3.Party AS SalesManagerUserParty ON SalesManagerUserParty.PartyID = SalesManagerEmployee.PartyRef
			LEFT JOIN HCM3.Employee AS SalesProjectManagerEmployee ON SalesProjectManagerEmployee.EmployeeID = SaleProductionOrder.ProjectManagerIdRef
			LEFT JOIN GNR3.Party AS SalesProjectManagerUserParty ON SalesProjectManagerUserParty.PartyID = SalesProjectManagerEmployee.PartyRef
			WHERE SaleProductionOrder.Sale_ProductionOrderID = @ID;

			SELECT
				@ContarctNumber = Number,
				@ContractPrice = NetPrice
			FROM SLS3.Contract
			WHERE ContractID = @ContractID;

			IF OBJECT_ID(N'tempdb..#AttachmentsTemp') IS NOT NULL
				DROP TABLE #AttachmentsTemp;

			SELECT
				RowNum = (ROW_NUMBER() OVER (ORDER BY AttachmentID)),
				AttachmentID,
				EntityCode,
				RecordID
			INTO #AttachmentsTemp
			FROM SYS3.Attachment
			WHERE EntityCode = 1469 AND RecordID = @ID;

			SET @AttachmentsCount = (
				SELECT COUNT(*) FROM #AttachmentsTemp WHERE EntityCode = 1469 AND RecordID = @ID
			);

			WHILE @AttachmentsCount > 0
			BEGIN
				SET @AttachmentId = (
					SELECT AttachmentID FROM #AttachmentsTemp WHERE RowNum = @AttachmentsCount
				);

				BEGIN
					EXEC [dbo].[Hts_WriteImageToDisk]
						@AttachmentID = @AttachmentId,
						@FilePath = @RootPath,
						@Result = @AttachmentFilePath OUTPUT;
					SET @AttachmentPaths += @AttachmentFilePath + N';';
				END

				SET @AttachmentsCount = @AttachmentsCount - 1;
			END

			IF (LEN(@AttachmentPaths) > 0)
			BEGIN
				SET @AttachmentPaths = SUBSTRING(@AttachmentPaths, 1, (LEN(@AttachmentPaths) - 1));
			END

			SET @Subject = N'سفارش ساخت_' + LTRIM(RTRIM(STR(@ProductionOrderNumber) + N'_' + @ContractTitle));

			SET @Body = N'بدینوسیله اطلاعات سفارش ساخت شماره<strong> ' + STR(@ProductionOrderNumber) + N' </strong> به پیوست ارسال گردید <br /><br />
		  <ul>

			<li>
            شماره قرارداد  <strong>' + STR(@ContarctNumber) + N'</strong>
            </li>

			<li>
            مبلغ قرارداد  <strong>' + CAST(@ContractPrice AS NVARCHAR(20)) + N'</strong>
            </li>

			<li>
            ایجاد کننده  <strong>' + @CreatorFullname + N'</strong>
            </li>

		  </ul>';

			IF (@StateId = 2)
			BEGIN
				IF (LEN(@CreatorEmail) > 0)
					SET @CopyRecipients = @CopyRecipients + N';' + @CreatorEmail;
				IF (LEN(@SalesExpertUserEmail) > 0)
					SET @CopyRecipients = @CopyRecipients + N';' + @SalesExpertUserEmail;
				IF (LEN(@AlternativeSalesExpertUserEmail) > 0)
					SET @CopyRecipients = @CopyRecipients + N';' + @AlternativeSalesExpertUserEmail;
				IF (LEN(@SalesManagerUserEmail) > 0)
					SET @CopyRecipients = @CopyRecipients + N';' + @SalesManagerUserEmail;
				IF (LEN(@SalesProjectManagerUserEmail) > 0)
					SET @CopyRecipients = @CopyRecipients + N';' + @SalesProjectManagerUserEmail;
			END

			IF (@StateId = 3)
			BEGIN
				SET @Recipients = N'Lalouni.m@havayar.com';
				IF (LEN(@CreatorEmail) > 0)
					SET @CopyRecipients = @CopyRecipients + N';' + @CreatorEmail;
				IF (LEN(@SalesExpertUserEmail) > 0)
					SET @CopyRecipients = @CopyRecipients + N';' + @SalesExpertUserEmail;
				IF (LEN(@AlternativeSalesExpertUserEmail) > 0)
					SET @CopyRecipients = @CopyRecipients + N';' + @AlternativeSalesExpertUserEmail;
				IF (LEN(@SalesManagerUserEmail) > 0)
					SET @CopyRecipients = @CopyRecipients + N';' + @SalesManagerUserEmail;
				IF (LEN(@SalesProjectManagerUserEmail) > 0)
					SET @CopyRecipients = @CopyRecipients + N';' + @SalesProjectManagerUserEmail;
			END

			EXEC dbo.Hts_SendEmail
				@recipients = @Recipients,
				@copy_recipients = @CopyRecipients,
				@body = @Body,
				@subject = @Subject,
				@file_attachments = @AttachmentPaths;

			WHILE LEN(@AttachmentPaths) > 0
			BEGIN
				SET @Path = LEFT(@AttachmentPaths, CHARINDEX(N';', @AttachmentPaths + N';') - 1);
				EXEC ('xp_cmdshell ''del "' + @Path + '"''');
				SET @AttachmentPaths = STUFF(@AttachmentPaths, 1, CHARINDEX(N';', @AttachmentPaths + N';'), N'');
			END

			ALTER TABLE USR3.Sale_ProductionOrder DISABLE TRIGGER AddProductionOrderToHts_UpdateRevisionAndApplySomeChanges;

			UPDATE Sale_ProductionOrder
			SET Sale_ProductionOrder.IsShouldUpdateHts = 0
			FROM USR3.Sale_ProductionOrder AS Sale_ProductionOrder
			WHERE Sale_ProductionOrder.Sale_ProductionOrderID = @ID;

			ALTER TABLE USR3.Sale_ProductionOrder ENABLE TRIGGER AddProductionOrderToHts_UpdateRevisionAndApplySomeChanges;

		END

		SET @RowsCount = @RowsCount - 1;
	END

END
