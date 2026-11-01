using System;
using System.Threading;
using Grpc.Core;

namespace Smoke.Consumer;

/// <summary>Stand-in for the protoc-generated service class of the README's worked example.</summary>
internal static class Orders
{
    /// <summary>Stand-in for the generated client: the same constructors and overload shape, without
    /// a channel behind it.</summary>
    internal class OrdersClient : ClientBase<OrdersClient>
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
            => throw new NotSupportedException("The stand-in has no channel; tests use a fake.");

        /// <inheritdoc/>
        protected override OrdersClient NewInstance(ClientBaseConfiguration configuration) => new(configuration);
    }
}
