namespace Direnix.Service.Configuration;

public sealed class ProductHostOptions
{
    public const string SectionName = "Direnix";

    public string ListenAddress { get; init; } = "127.0.0.1";

    public int Port { get; init; } = 8787;

    public string InstanceMode { get; init; } = "LocalSecure";
}
