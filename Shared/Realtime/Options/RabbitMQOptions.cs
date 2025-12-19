namespace Shared.Realtime.Options;

/// <summary>
/// Strongly-typed options for RabbitMQ connectivity.
/// </summary>
public class RabbitMQOptions
{
	public const string SectionName = "RabbitMQ";
	public string HostName { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public string Exchange { get; set; } = "app.events";
    public string ExchangeType { get; set; } = "topic";
    public bool ExchangeDurable { get; set; } = true;
    public bool ExchangeAutoDelete { get; set; } = false;

    public bool PersistentMessages { get; set; } = true;
    public bool MandatoryPublish { get; set; } = false;

    // Publisher confirms
    public bool EnablePublisherConfirms { get; set; } = true;
    public bool EnablePublisherConfirmTracking { get; set; } = false;

    // reconnect
    public int ConnectRetryCount { get; set; } = 5;
    public int ConnectRetryBaseDelayMs { get; set; } = 500;
    public int ConnectRetryMaxDelayMs { get; set; } = 5000;

    // optional
    public bool HandleBasicReturns { get; set; } = false;
}


