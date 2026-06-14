using System;
using System.Collections.Generic;
using System.Linq;
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
        => Success(response, responseHeaders: null, trailers: null);

    /// <summary>
    /// Builds a successful <see cref="AsyncUnaryCall{TResponse}"/> that completes with
    /// <paramref name="response"/> and the supplied response headers and trailers, and a terminal
    /// <see cref="StatusCode.OK"/> status. A null <paramref name="responseHeaders"/> or
    /// <paramref name="trailers"/> is treated as empty metadata. The trailers accessor returns the
    /// same <see cref="Metadata"/> instance on every call, matching a real client.
    /// </summary>
    /// <typeparam name="TResponse">The gRPC response message type.</typeparam>
    /// <param name="response">The response the call completes with. Must not be null.</param>
    /// <param name="responseHeaders">The response headers, or null for empty headers.</param>
    /// <param name="trailers">The response trailers, or null for empty trailers.</param>
    /// <returns>A completed, successful <see cref="AsyncUnaryCall{TResponse}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is null.</exception>
    public static AsyncUnaryCall<TResponse> Success<TResponse>(TResponse response, Metadata? responseHeaders, Metadata? trailers)
    {
        ArgumentNullException.ThrowIfNull(response);

        var headers = responseHeaders ?? new Metadata();
        var callTrailers = trailers ?? new Metadata();
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(headers),
            static () => new Status(StatusCode.OK, string.Empty),
            () => callTrailers,
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

    /// <summary>
    /// Builds a faulted <see cref="AsyncUnaryCall{TResponse}"/> from a status code, detail, and
    /// response <paramref name="trailers"/>. The trailers are attached to the constructed
    /// <see cref="RpcException"/>, so they surface through the call's trailers accessor and through
    /// <see cref="RpcException.Trailers"/> on the thrown exception.
    /// </summary>
    /// <typeparam name="TResponse">The gRPC response message type.</typeparam>
    /// <param name="statusCode">The gRPC status code to fault with.</param>
    /// <param name="detail">The status detail message. A null value is treated as an empty string.</param>
    /// <param name="trailers">The response trailers to attach. Must not be null.</param>
    /// <returns>A faulted <see cref="AsyncUnaryCall{TResponse}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="trailers"/> is null.</exception>
    public static AsyncUnaryCall<TResponse> Faulted<TResponse>(StatusCode statusCode, string? detail, Metadata trailers)
    {
        ArgumentNullException.ThrowIfNull(trailers);
        return Faulted<TResponse>(new RpcException(new Status(statusCode, detail ?? string.Empty), trailers));
    }

    /// <summary>
    /// Builds a successful <see cref="AsyncServerStreamingCall{TResponse}"/> whose response stream
    /// yields <paramref name="responses"/> in order and then ends cleanly with a terminal
    /// <see cref="StatusCode.OK"/> status: the server-streaming shape a generated gRPC client
    /// returns on success.
    /// </summary>
    /// <typeparam name="TResponse">The gRPC response message type.</typeparam>
    /// <param name="responses">The responses the stream yields, in order. Must not be null; an empty
    /// sequence produces a stream that ends immediately.</param>
    /// <returns>A successful <see cref="AsyncServerStreamingCall{TResponse}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="responses"/> is null.</exception>
    public static AsyncServerStreamingCall<TResponse> ServerStreaming<TResponse>(IEnumerable<TResponse> responses)
    {
        ArgumentNullException.ThrowIfNull(responses);
        return BuildServerStreaming(responses, fault: null);
    }

    /// <summary>
    /// Builds an <see cref="AsyncServerStreamingCall{TResponse}"/> whose response stream yields
    /// <paramref name="responses"/> in order and then throws an <see cref="RpcException"/> on the
    /// next read: the partial-stream-then-error shape that exercises a wrapper's mid-stream fault
    /// handling.
    /// </summary>
    /// <typeparam name="TResponse">The gRPC response message type.</typeparam>
    /// <param name="statusCode">The gRPC status code the stream faults with after the responses.</param>
    /// <param name="responses">The responses yielded before the fault, in order. Must not be null; an
    /// empty sequence faults on the first read.</param>
    /// <param name="detail">The status detail message. A null value is treated as an empty string.</param>
    /// <returns>A faulted-after-<paramref name="responses"/> <see cref="AsyncServerStreamingCall{TResponse}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="responses"/> is null.</exception>
    public static AsyncServerStreamingCall<TResponse> ServerStreamingFaulted<TResponse>(
        StatusCode statusCode,
        IEnumerable<TResponse> responses,
        string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(responses);
        return BuildServerStreaming(responses, new RpcException(new Status(statusCode, detail ?? string.Empty)));
    }

    /// <summary>Constructs the server-streaming call from a materialized response list and an optional
    /// terminal fault, mirroring the five-parameter shape a real client exposes.</summary>
    /// <typeparam name="TResponse">The gRPC response message type.</typeparam>
    /// <param name="responses">The responses to yield.</param>
    /// <param name="fault">The exception to throw after the responses, or null to end cleanly.</param>
    /// <returns>The constructed call.</returns>
    private static AsyncServerStreamingCall<TResponse> BuildServerStreaming<TResponse>(IEnumerable<TResponse> responses, RpcException? fault)
    {
        var items = responses as IReadOnlyList<TResponse> ?? responses.ToList();
        var reader = new FakeAsyncStreamReader<TResponse>(items, fault);
        Func<Status> getStatus = fault is not null
            ? () => fault.Status
            : static () => new Status(StatusCode.OK, string.Empty);
        Func<Metadata> getTrailers = fault is not null
            ? () => fault.Trailers
            : static () => new Metadata();

        return new AsyncServerStreamingCall<TResponse>(
            reader,
            Task.FromResult(new Metadata()),
            getStatus,
            getTrailers,
            static () => { });
    }
}
