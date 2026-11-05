using Grpc.Core;

namespace Smoke.Consumer;

/// <summary>The README's fake: overrides the <see cref="CallOptions"/> overload and returns a prepared call.</summary>
/// <param name="call">The call every request returns.</param>
internal sealed class FakeOrdersClient(AsyncUnaryCall<OrderReply> call) : Orders.OrdersClient
{
    /// <inheritdoc/>
    public override AsyncUnaryCall<OrderReply> GetOrderAsync(OrderRequest request, CallOptions options)
        => call;
}
