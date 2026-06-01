using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace GrpcAssertions;

/// <summary>
/// Builds <see cref="AsyncUnaryCall{TResponse}"/> instances for gRPC client test doubles,
/// eliminating the five-parameter constructor that every hand-rolled fake repeats. These are
/// test-infrastructure helpers, not assertions, so they live in the framework-agnostic core
/// <c>GrpcAssertions</c> package rather than the TUnit adapter.
/// </summary>
public static class GrpcCallBuilder
{
    /// <summary>
    /// Builds a successful <see cref="AsyncUnaryCall{TResponse}"/> that completes with
    /// <paramref name="response"/>, empty response headers and trailers, and a terminal
    /// <see cref="StatusCode.OK"/> status: the unary-call shape a generated gRPC client
    /// returns on success.
    /// </summary>
    /// <typeparam name="TResponse">The gRPC response message type.</typeparam>
    /// <param name="response">The response the call completes with. Must not be null.</param>
    /// <returns>A completed, successful <see cref="AsyncUnaryCall{TResponse}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is null.</exception>
    public static AsyncUnaryCall<TResponse> Success<TResponse>(TResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            static () => new Status(StatusCode.OK, string.Empty),
            static () => new Metadata(),
            static () => { });
    }

    /// <summary>
    /// Builds a faulted <see cref="AsyncUnaryCall{TResponse}"/> whose response task throws
    /// <paramref name="exception"/>, surfacing the exception's <see cref="RpcException.Status"/>
    /// and <see cref="RpcException.Trailers"/> through the call's status and trailers accessors.
    /// </summary>
    /// <typeparam name="TResponse">The gRPC response message type.</typeparam>
    /// <param name="exception">The exception the call faults with. Must not be null.</param>
    /// <returns>A faulted <see cref="AsyncUnaryCall{TResponse}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
    public static AsyncUnaryCall<TResponse> Faulted<TResponse>(RpcException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new AsyncUnaryCall<TResponse>(
            Task.FromException<TResponse>(exception),
            Task.FromResult(new Metadata()),
            () => exception.Status,
            () => exception.Trailers,
            static () => { });
    }

    /// <summary>
    /// Builds a faulted <see cref="AsyncUnaryCall{TResponse}"/> from a status code and detail,
    /// the most common faulted-call shape in tests: a convenience over
    /// <see cref="Faulted{TResponse}(RpcException)"/> that constructs the
    /// <see cref="RpcException"/> for the caller.
    /// </summary>
    /// <typeparam name="TResponse">The gRPC response message type.</typeparam>
    /// <param name="statusCode">The gRPC status code to fault with.</param>
    /// <param name="detail">The status detail message. A null value is treated as an empty string.</param>
    /// <returns>A faulted <see cref="AsyncUnaryCall{TResponse}"/>.</returns>
    public static AsyncUnaryCall<TResponse> Faulted<TResponse>(StatusCode statusCode, string? detail = null)
        => Faulted<TResponse>(new RpcException(new Status(statusCode, detail ?? string.Empty)));
}
