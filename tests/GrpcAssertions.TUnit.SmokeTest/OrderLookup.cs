using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace Smoke.Consumer;

/// <summary>The README's code under test: a wrapper over the generated client.</summary>
/// <param name="client">The generated client.</param>
internal sealed class OrderLookup(Orders.OrdersClient client)
{
    /// <summary>Returns the customer of an order, or <see langword="null"/> when the order does not exist.</summary>
    /// <param name="orderId">The order id.</param>
    /// <param name="cancellationToken">The call's cancellation token.</param>
    /// <returns>The customer, or <see langword="null"/>.</returns>
    public async Task<string?> FindCustomerAsync(int orderId, CancellationToken cancellationToken)
    {
        try
        {
            var reply = await client.GetOrderAsync(
                new OrderRequest { Id = orderId }, cancellationToken: cancellationToken);
            return reply.Customer;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}
