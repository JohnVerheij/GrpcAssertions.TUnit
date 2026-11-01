namespace Smoke.Consumer;

/// <summary>Stand-in for a protoc-generated reply message of the README's worked example.</summary>
internal sealed class OrderReply
{
    /// <summary>The customer who placed the order.</summary>
    public string Customer { get; init; } = string.Empty;
}
