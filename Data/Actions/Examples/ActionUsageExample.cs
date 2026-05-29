using System;
using System.Threading.Tasks;

namespace HavayarApp.Data.Actions.Examples
{
    /// <summary>
    /// Example entity for demonstration purposes.
    /// </summary>
    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public decimal Amount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Notes { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Processing,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Example 1: Simple action class marked with ActionAttribute for Create operations
    /// This action validates and initializes an order when it's created.
    /// </summary>
    [Action("InitializeOrder", "Initializes a new order with default values", typeof(Order), ActionType.Create, true, 0)]
    public class InitializeOrderAction : IEntityAction<Order>
    {
        public string ActionName => "InitializeOrder";

        public async Task ExecuteAsync(Order entity)
        {
            await Task.Run(() =>
            {
                entity.Status = OrderStatus.Pending;
                entity.CreatedAt = DateTime.UtcNow;
                Console.WriteLine($"Order {entity.OrderNumber} initialized");
            });
        }
    }

    /// <summary>
    /// Example 2: Action for validating order before confirmation
    /// This action runs second in the sequence (ExecutionOrder = 1)
    /// </summary>
    [Action("ValidateOrder", "Validates order data before confirmation", typeof(Order), ActionType.Update, true, 1)]
    public class ValidateOrderAction : IEntityAction<Order>
    {
        public string ActionName => "ValidateOrder";

        public async Task ExecuteAsync(Order entity)
        {
            await Task.Run(() =>
            {
                if (entity.Amount <= 0)
                    throw new InvalidOperationException("Order amount must be greater than zero");

                if (string.IsNullOrEmpty(entity.OrderNumber))
                    throw new InvalidOperationException("Order number is required");

                Console.WriteLine($"Order {entity.OrderNumber} validated successfully");
            });
        }
    }

    /// <summary>
    /// Example 3: Action for updating order status
    /// This action runs third (ExecutionOrder = 2)
    /// </summary>
    [Action("UpdateOrderStatus", "Updates order status to Confirmed", typeof(Order), ActionType.Update, true, 2)]
    public class UpdateOrderStatusAction : IEntityAction<Order>
    {
        public string ActionName => "UpdateOrderStatus";

        public async Task ExecuteAsync(Order entity)
        {
            await Task.Run(() =>
            {
                entity.Status = OrderStatus.Confirmed;
                Console.WriteLine($"Order {entity.OrderNumber} status updated to {entity.Status}");
            });
        }
    }

    /// <summary>
    /// Example 4: Action for logging order changes
    /// This action runs last (ExecutionOrder = 3)
    /// </summary>
    [Action("LogOrderChange", "Logs order changes to audit trail", typeof(Order), ActionType.Update, true, 3)]
    public class LogOrderChangeAction : IEntityAction<Order>
    {
        public string ActionName => "LogOrderChange";

        public async Task ExecuteAsync(Order entity)
        {
            await Task.Run(() =>
            {
                var logMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Order {entity.OrderNumber} - Status: {entity.Status}, Amount: {entity.Amount}";
                Console.WriteLine($"LOG: {logMessage}");
                // In real implementation, this would write to a database or file
            });
        }
    }

    /// <summary>
    /// Example 5: Method-level action attribute
    /// </summary>
    public class OrderProcessingService
    {
        [Action("ProcessOrderPayment", "Processes payment for the order", typeof(Order), ActionType.Execute, true, 0)]
        public async Task ProcessPaymentAsync(Order order)
        {
            await Task.Run(() =>
            {
                Console.WriteLine($"Processing payment for order {order.OrderNumber}");
                order.Notes += " [Payment processed]";
            });
        }

        [Action("SendOrderConfirmation", "Sends confirmation email to customer", typeof(Order), ActionType.Execute, true, 1)]
        public async Task SendConfirmationAsync(Order order)
        {
            await Task.Run(() =>
            {
                Console.WriteLine($"Confirmation email sent for order {order.OrderNumber}");
                order.Notes += " [Email sent]";
            });
        }
    }

    /// <summary>
    /// Example 6: Property-level action attribute
    /// </summary>
    public class OrderMetadata
    {
        [Action("OrderNumberProperty", "Action for OrderNumber property changes")]
        public string OrderNumber { get; set; }

        [Action("AmountProperty", "Action for Amount property changes")]
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Example 7: Usage demonstration
    /// </summary>
    public class ActionUsageExample
    {
        public static async Task RunExample()
        {
            // Initialize the action registry
            ActionRegistry.Initialize();

            // Create a sample order
            var order = new Order
            {
                Id = 1,
                OrderNumber = "ORD-2026-001",
                Amount = 1500.00m,
                Status = OrderStatus.Pending
            };

            Console.WriteLine("=== Executing Actions on Order ===\n");

            // Get all actions for Order type
            var actions = ActionRegistry.GetActionsForEntity(typeof(Order));
            Console.WriteLine($"Found {actions.Count} registered actions for Order entity\n");

            // Execute all actions in sequence
            var success = await ActionRegistry.ExecuteActionsAsync(order);

            Console.WriteLine($"\n=== Execution Result: {(success ? "SUCCESS" : "FAILED")} ===");
            Console.WriteLine($"Order Status: {order.Status}");
            Console.WriteLine($"Final Notes: {order.Notes}");

            // Get specific actions
            var createActions = ActionRegistry.GetActionsForEntity(typeof(Order), ActionType.Create);
            Console.WriteLine($"\n=== Create Actions: {createActions.Count} ===");

            var updateActions = ActionRegistry.GetActionsForEntity(typeof(Order), ActionType.Update);
            Console.WriteLine($"=== Update Actions: {updateActions.Count} ===");

            // Get all registered actions
            var allActions = ActionRegistry.GetAllActions();
            Console.WriteLine($"\n=== Total Registered Actions: {allActions.Count} ===");

            foreach (var action in allActions)
            {
                Console.WriteLine($"- {action.Name} (Entity: {action.EntityType?.Name ?? "Unknown"}, Order: {action.ExecutionOrder})");
            }
        }
    }
}
