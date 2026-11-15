// Declarations the README samples use without declaring them: the stand-ins of protoc-generated gRPC code,
// code under test, and the fields every sample body can read. assertions-ci's readme_samples.py compiles this file
// with the samples; every wrapping class derives from Prelude.
global using Grpc.Core;
global using GrpcAssertions;

namespace ReadmeSamples;

/// <summary>Stand-in for a generated request message.</summary>
public sealed class OrderRequest
{
    /// <summary>Gets or sets the order id.</summary>
    public int Id { get; set; }
}

/// <summary>Stand-in for a generated reply message.</summary>
public sealed class OrderReply
{
    /// <summary>Gets or sets the customer of the order.</summary>
    public string Customer { get; set; } = string.Empty;
}

/// <summary>Stand-in for a generated request message of the greeter service.</summary>
public sealed class MyRequest
{
}

/// <summary>Stand-in for a generated response message of the greeter service.</summary>
public sealed class MyResponse
{
}

/// <summary>Stand-in for the protoc-generated service class of the orders service.</summary>
public static class Orders
{
    /// <summary>Stand-in for the generated client: the constructors and overload shape of generated code.</summary>
    public class OrdersClient : ClientBase<OrdersClient>
    {
        /// <summary>The parameterless constructor generated clients provide for test doubles.</summary>
        protected OrdersClient()
        {
        }

        /// <summary>The configuration constructor generated clients provide.</summary>
        /// <param name="configuration">The client configuration.</param>
        protected OrdersClient(ClientBaseConfiguration configuration)
            : base(configuration)
        {
        }

        /// <summary>The generated convenience overload: forwards to the <see cref="CallOptions"/> overload.</summary>
        /// <param name="request">The request.</param>
        /// <param name="headers">The request headers.</param>
        /// <param name="deadline">The call deadline.</param>
        /// <param name="cancellationToken">The call's cancellation token.</param>
        /// <returns>The call.</returns>
        public virtual AsyncUnaryCall<OrderReply> GetOrderAsync(
            OrderRequest request, Metadata? headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
            => GetOrderAsync(request, new CallOptions(headers, deadline, cancellationToken));

        /// <summary>The generated <see cref="CallOptions"/> overload; a real client invokes the channel here.</summary>
        /// <param name="request">The request.</param>
        /// <param name="options">The call options.</param>
        /// <returns>The call.</returns>
        public virtual AsyncUnaryCall<OrderReply> GetOrderAsync(OrderRequest request, CallOptions options)
            => throw new NotSupportedException("The stand-in has no channel; the samples use a fake.");

        /// <inheritdoc/>
        protected override OrdersClient NewInstance(ClientBaseConfiguration configuration) => new(configuration);
    }
}

/// <summary>Stand-in for the protoc-generated service class of the greeter service.</summary>
public static class Greeter
{
    /// <summary>Stand-in for the generated client of the greeter service.</summary>
    public class GreeterClient : ClientBase<GreeterClient>
    {
        /// <summary>The parameterless constructor generated clients provide for test doubles.</summary>
        protected GreeterClient()
        {
        }

        /// <summary>The configuration constructor generated clients provide.</summary>
        /// <param name="configuration">The client configuration.</param>
        protected GreeterClient(ClientBaseConfiguration configuration)
            : base(configuration)
        {
        }

        /// <summary>The generated <see cref="CallOptions"/> overload; a real client invokes the channel here.</summary>
        /// <param name="request">The request.</param>
        /// <param name="options">The call options.</param>
        /// <returns>The call.</returns>
        public virtual AsyncUnaryCall<MyResponse> SayHelloAsync(MyRequest request, CallOptions options)
            => throw new NotSupportedException("The stand-in has no channel; the samples use a fake.");

        /// <inheritdoc/>
        protected override GreeterClient NewInstance(ClientBaseConfiguration configuration) => new(configuration);
    }
}

/// <summary>Stand-in for the code under test of the greeter samples: a wrapper over the generated client.</summary>
/// <param name="client">The generated client to wrap.</param>
public sealed class GreeterWrapper(Greeter.GreeterClient client)
{
    /// <summary>Awaits the generated client's call.</summary>
    /// <param name="request">The request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    public async Task<MyResponse> SayHelloAsync(object request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await client.SayHelloAsync(new MyRequest(), new CallOptions(cancellationToken: cancellationToken));
    }
}

/// <summary>Stand-in for a client wrapper whose methods return a <see cref="Task"/>, which a delegate assertion awaits.</summary>
public sealed class OrdersApi
{
    /// <summary>Fetches an order.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reply.</returns>
    public Task<OrderReply> GetOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new OrderReply());

    /// <summary>Fetches the server info.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The info.</returns>
    public Task<string> GetServerInfoAsync(OrderRequest request, CancellationToken cancellationToken)
        => Task.FromResult(string.Empty);

    /// <summary>Cancels an order.</summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the order is canceled.</returns>
    public Task CancelOrderAsync(OrderRequest request, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>Stand-in for the code under test of the worked example.</summary>
/// <param name="client">The client to wrap.</param>
public sealed class OrderLookup(Orders.OrdersClient client)
{
    /// <summary>Finds the customer of an order.</summary>
    /// <param name="orderId">The order id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The customer, or <see langword="null"/> when the order does not exist.</returns>
    public async Task<string?> FindCustomerAsync(int orderId, CancellationToken cancellationToken)
    {
        try
        {
            var reply = await client.GetOrderAsync(new OrderRequest { Id = orderId }, cancellationToken: cancellationToken);
            return reply.Customer;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}

/// <summary>Stand-in for the fake client of the worked example.</summary>
/// <param name="call">The call every <c>GetOrderAsync</c> returns.</param>
public sealed class FakeOrdersClient(AsyncUnaryCall<OrderReply> call) : Orders.OrdersClient
{
    /// <inheritdoc/>
    public override AsyncUnaryCall<OrderReply> GetOrderAsync(OrderRequest request, CallOptions options) => call;
}

/// <summary>A generated client without a channel, for the samples that call one directly.</summary>
public sealed class StubOrdersClient : Orders.OrdersClient
{
}

/// <summary>What the sample bodies read: the clients, a request and a cancellation token.</summary>
public partial class Prelude
{
    /// <summary>Gets or sets the client wrapper whose methods return a task.</summary>
    protected OrdersApi client { get; set; } = new();

    /// <summary>Gets or sets a generated client, called directly.</summary>
    protected Orders.OrdersClient generatedClient { get; set; } = new StubOrdersClient();

    /// <summary>Gets or sets the request the samples send.</summary>
    protected OrderRequest request { get; set; } = new();

    /// <summary>Gets or sets the cancellation token the samples pass on.</summary>
    protected CancellationToken ct { get; set; }
}
