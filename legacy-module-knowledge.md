# Legacy Module Knowledge: Production Order Item

## 1. Module Overview

**Purpose:**
The `ProductionOrderItem` module manages the lifecycle of individual items within a manufacturing order (`Pln_ProductionOrder`). It tracks the item from its initial import from the ERP system (Rahkaran) through financial confirmation, engineering approval, production execution, testing, quality control, and final packaging.

**Business Problem Solved:**
It bridges the gap between the sales/financial initiation of an order and the physical execution on the factory floor. It solves the problem of:
- Routing items to the correct departments (Engineering vs. Project Management vs. Direct Production) based on item type.
- Tracking specific dates for production stages (Start/End of Production, Testing, Packaging).
- Enforcing financial and technical approvals before manufacturing begins.
- Providing visibility into the status of each component in a complex order.

## 2. Processes & Workflows

### 2.1. Ingestion & Initialization
1.  **Import:** Data is imported from the external ERP (Rahkaran) via scheduled tasks (`HtsTaskService`).
2.  **Creation:** New `Pln_ProductionOrderItem` records are created or updated based on the ERP data.

### 2.2. Approval Routing (The "BuyStatus" Workflow)
Once an item exists, it undergoes a routing check triggered by the `CheckFinancialConfirmsOrders` job:
1.  **Financial Check:** The system checks if the parent `ProductionOrder` has `Financial_Approval`.
2.  **Routing Decision:**
    *   **IF** Item is "Routine" (`IsRoutine`) **OR** Type is "CNG Equipment" / "Spare Parts":
        *   → Route to **Internal/Industries** (Status: `Internal` / 1904).
    *   **ELSE IF** Order has a Project Manager (`ProjectManagerId` is set):
        *   → Route to **Project Manager** (Status: `SendToProjectManager` / 2258).
    *   **ELSE**:
        *   → Route to **Engineering** (Status: `SeenToMechanicalEngineering` / 2195).

### 2.3. Approval Actions
*   **Engineering Approval:**
    *   User with Engineering permission reviews the item.
    *   Action: Sets `Enginnering_Approval` = `true`.
    *   Captures `Enginnering_Approval_Date` and `User`.
*   **Industrial/Planning Approval:**
    *   User with Industrial permission reviews the item.
    *   Action: Sets `Industrial_Approval` = `true`.
    *   This often signals the item is ready for the production floor.

### 2.4. Production Execution (State Machine)
Operators update the status via the UI (`DoChangeProductionStatusOperation`):
1.  **Start Production:** Status set to `1792`.
2.  **End Production:** Status set to `1789`.
    *   *System Action:* Updates `ProductionEndDate`.
3.  **Start Test:** Status set to `2209`.
    *   *System Action:* If a Project Manager is assigned, an IT Support notification is triggered (for test setup).
4.  **End Test:** Status set to `1843` (QC) or `2210`.
    *   *System Action:* Updates `TestingEndDate`.
5.  **Packaging:** Status set to `1791`.
    *   *System Action:* Updates `PreparationDate`.

## 3. Data Model

**Entity:** `Pln_ProductionOrderItem`

### Core Fields
| Field Name | Type | Nullable | Meaning |
| :--- | :--- | :--- | :--- |
| `Id` | int | No | Primary Key |
| `ProductionOrderId` | int | No | FK to Parent Order Header |
| `PartId` | long | No | FK to Inventory Part (`Inv_Part`) |
| `Serial` | string | Yes | Unique Serial Number for the item |
| `StatusId` | short | Yes | Current Workflow Status (Enum) |
| `BuyStatusId` | short | Yes | Routing Status (Internal, Engineering, PM) |
| `MetrePrice` | decimal | Yes | Calculated price (often hidden/protected) |
| `Mount` | decimal | No | Quantity/Count |
| `IsRoutine` | bool | No | Flag determining auto-routing |
| `DeviceTypeId` | short | Yes | Categorization (e.g., CNG, Spare Parts) |

### Approval Fields
| Field Name | Type | Nullable | Meaning |
| :--- | :--- | :--- | :--- |
| `Industrial_Approval` | bool | No | Planning/Industrial Unit Approval |
| `Enginnering_Approval` | bool | No | Engineering Unit Approval |
| `Enginnering_Approval_Date` | string | Yes | Date of Engineering Approval |
| `Financial_Approval` | bool | No | (Usually on Header, but checked here) |

### Execution Dates
| Field Name | Type | Nullable | Meaning |
| :--- | :--- | :--- | :--- |
| `ProductionStartDate` | string | Yes | Production Start Date (Persian) |
| `ProductionEndDate` | string | Yes | Production End Date (Persian) |
| `TestingStartDate` | string | Yes | Testing Start Date (Persian) |
| `TestingEndDate` | string | Yes | Testing End Date (Persian) |
| `PackingStartDate` | string | Yes | Packaging Start Date (Persian) |
| `PackingEndDate` | string | Yes | Packaging End Date (Persian) |
| `DeliveryDate` | datetime | Yes | Final Delivery Date |
| `PreparationDate` | datetime | Yes | Ready for Delivery Date |

### Relationships
*   **Parent:** `Pln_ProductionOrder` (1:N)
*   **Part:** `Inv_Part` (1:1)
*   **Sub-Items:** Self-referencing via `Parent_FK` (for Packages/Kits).
*   **BOM:** `Pln_ProductionOrderItemBom` (1:N) - Bill of Materials specific to this order item.

## 4. Business Rules

### Explicit Rules
1.  **Financial Gate:** Items cannot proceed to Engineering/Production routing until the parent order is financially confirmed.
2.  **Status Locking:** Users cannot change the status to the current existing status (prevents duplicate log entries).
3.  **Permission Gates:**
    *   Only users with `Accept_Industrial` can perform Industrial Approval.
    *   Only users with `Accept_Engineering` (or specific Org Units 17, 143, 30) can perform Engineering Approval.
    *   Price editing (`MetrePrice`) requires `EditPrice` permission.
4.  **Package Validation:** If adding a "Package" item, `PackageType` and `Part` are mandatory.

### Implicit Rules (Inferred)
1.  **Date Auto-Filling:** When specific "End" statuses are selected (Production Completed, Test Completed, Packaging Completed), the system automatically fills the corresponding End Date fields with the current date/user input date.
2.  **Routine Bypass:** "Routine" items bypass the Engineering approval queue and go straight to the Industrial inbox.
3.  **IT Notification Logic:** The system assumes that if a Project Manager is involved, the "Test" phase requires IT resources, triggering an automatic email.

## 5. Triggers / Jobs / Scheduled Tasks

The module relies heavily on `HtsTaskService`:

| Task Name | Schedule | Function |
| :--- | :--- | :--- |
| `PlanningSystemTask` | Periodic (e.g., every 20 min) | Runs `CheckFinancialConfirmsOrders` to route new items. Applies unconfirmed orders. |
| `SendHoldedItemsToIndustrialUnitThatWasInProjectManagerCartable` | Scheduled | Moves items stuck in Project Manager's inbox (> 2 days) to Industrial Unit. |
| `SpecialHamkaranTask` | Scheduled | Imports Production Orders and Items from Rahkaran (ERP). |
| `SendProductionOrderItemsWithNearDeliveryDateNotifications` | Daily (09:45) | Emails stakeholders about items approaching their delivery deadline (15 days out). |
| `SendExpiredOrdersNotification` | Periodic | Notifies about orders stuck in Engineering/PM queues for > 1 day. |

## 6. External Dependencies

*   **Rahkaran (ERP):** The upstream source of truth for Order Headers and initial Item definitions.
*   **Bale Messenger:** Used for sending alerts (e.g., Roll Call, Work Deficit) - likely used for some production alerts too.
*   **Email Server:** SMTP server for sending notifications (IT Support, Expired Items, Near Delivery).
*   **TGJU (External API):** Used to fetch Exchange Rates (Dollar/Euro) which may influence `MetrePrice` calculations (though handled in a separate task).

## 7. Edge Cases & Constraints

*   **CNG/Spare Parts:** These specific `DeviceTypeId`s have hardcoded routing logic that mimics "Routine" items (bypassing standard engineering flow).
*   **Legacy Data:** The code handles `RahkaranId` and `RahkaranVersion`, implying a need to sync with legacy ERP records without overwriting local changes blindly.
*   **Part-to-Part vs. Packages:** The system distinguishes between standard items and "Packages" (`Is_Package_Itm`). Packages have a parent-child relationship logic (`Parent_FK`) and specific UI validation.
*   **Hardcoded IDs:** The code contains hardcoded Status IDs (e.g., 1789, 1792, 2209) and User Group IDs (e.g., 567 for email recipients). This makes the system brittle to configuration changes in the database.

## 8. Example Scenarios

### Scenario A: Routine Order
1.  **Input:** Order imported from ERP. `IsRoutine` = true. Financial Approval = Yes.
2.  **Process:** `CheckFinancialConfirmsOrders` runs.
3.  **Output:** Item Status set to `1904` (In Industries Internal Inbox). Engineering approval is skipped.

### Scenario B: Project Manager Order with Test
1.  **Input:** Order imported. `ProjectManagerId` is set. Financial Approval = Yes.
2.  **Process:** Routing logic sends item to `SendToProjectManager` status.
3.  **Action:** PM approves. Item moves to Production.
4.  **Action:** Production user changes status to `2209` (Start Test).
5.  **Output:** System automatically sends an email to IT Support to prepare test environment.

### Scenario C: Engineering Rejection
1.  **Input:** Non-routine item sent to Engineering.
2.  **Action:** Engineer reviews and rejects.
3.  **Output:** Status changes to `EngineeringRejectedNeedsRevision` (2201). Item likely loops back for revision (logic for re-submission is implied but handled via status updates).

### Scenario D: Project Manager Timeout
1.  **Input:** Item in `AwaitingProjectManagerApproval` (1380) status for > 2 days.
2.  **Process:** `SendHoldedItemsToIndustrialUnitThatWasInProjectManagerCartable` runs.
3.  **Action:** System automatically moves item to `AwaitingIndustry` (2744).
4.  **Output:** Status updated, comment added ("Auto-moved due to timeout"), Email sent.

## 9. Migration Mapping (Current Implementation)
*   **Status Mapping:**
    *   Legacy `BuyStatus` -> Current `ProductionOrderItem.CheckStatus` (Enum: `ProductionOrderItemCheckStatusEnum`).
    *   Legacy `Internal` -> `IndustrialDashboardInternal` (1904).
    *   Legacy `SendToProjectManager` -> `AwaitingProjectManagerApproval` (2258).
    *   Legacy `SeenToMechanicalEngineering` -> `MechanicalEngineeringApprovalPending` (2195).
    *   Legacy `PreRegister` -> `InitialRegistration` (1903).
*   **Entity Mapping:**
    *   Legacy `Pln_ProductionOrder` -> `ProductionOrder`.
    *   Legacy `Pln_ProductionOrderItem` -> `ProductionOrderItem`.
    *   Legacy `IsRoutine` (on Item) -> `Part.EngineeringRoutine` (on related Part).
*   **Missing Entities/Fields (as of implementation):**
    *   `Pln_ProductionOrderComment` (Parent Order Comments) not found. Skipped creation.
    *   `ProductionOrderItemComment` implemented with `ProductionStatus` mapped from `CheckStatus` (Status IDs match). 
    *   Creates a new `ProductionOrderItemComment` record for each status change with `CreatedById = 1` (System) and current timestamp.

## 10. Data Synchronization Strategy
*   **ProductionOrder:** Synchronized from Rahkaran. Updates are applied in-place (Snapshot) except when status changes to Obsolete (triggers cascading updates to items).
*   **ProductionOrderItem:** Implements "Insert-on-Update" versioning strategy to match legacy system behavior:
    *   Items are identified by `HamkaranId` (Persistent ID).
    *   If `Revision` changes in source, the existing latest item in app is marked `IsLatestVersion = false`.
    *   A new item record is inserted with updated data and `IsLatestVersion = true`.
    *   Deleted items in source are marked as `IsActive = Deleted` and `Status = Invalid` in app.
