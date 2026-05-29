namespace WebApp.ViewModels.NotificationViewModels
{
	public class SendNotificationViewModel
	{
		public string Title { get; set; }
		public string Body { get; set; }
		public string Description { get; set; }
		public long[]? OwnerIds { get; set; }
		public bool AllUsers { get; set; }
		public bool SaveNotification { get; set; }
	}
}
