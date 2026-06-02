using System;
using Grpc.Core;
using GrpcAssertions.TUnit;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// TUnit entry points for asserting on gRPC call outcomes. These extend the delegate assertion
/// source produced by <c>Assert.That(() =&gt; client.Method(...))</c>, so the call is executed and
/// its thrown <see cref="RpcException"/> (if any) is inspected by the returned assertion. The
/// methods live in <c>TUnit.Assertions.Extensions</c> so they are auto-imported alongside TUnit's
/// own assertions.
/// </summary>
public static class GrpcDelegateAssertionExtensions
{
    /// <summary>
    /// Asserts that the delegate throws a gRPC <see cref="RpcException"/> of any status. Chain
    /// <c>WithDetail</c> / <c>WithDetailContaining</c> or a status shorthand (<c>IsUnavailable()</c>,
    /// ...) to narrow the assertion.
    /// </summary>
    /// <typeparam name="TValue">The evaluated value type of the delegate under assertion.</typeparam>
    /// <param name="source">The delegate assertion source.</param>
    /// <returns>A <see cref="GrpcExceptionAssertion"/> for further chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static GrpcExceptionAssertion ThrowsGrpcException<TValue>(this IDelegateAssertionSource<TValue> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.Context.ExpressionBuilder.Append(".ThrowsGrpcException()");
        return new GrpcExceptionAssertion(source.Context.MapException<RpcException>(), expectedStatus: null);
    }

    /// <summary>
    /// Asserts that the delegate throws a gRPC <see cref="RpcException"/> whose
    /// <see cref="Status.StatusCode"/> equals <paramref name="expected"/>.
    /// </summary>
    /// <typeparam name="TValue">The evaluated value type of the delegate under assertion.</typeparam>
    /// <param name="source">The delegate assertion source.</param>
    /// <param name="expected">The expected gRPC status code.</param>
    /// <returns>A <see cref="GrpcExceptionAssertion"/> for further chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static GrpcExceptionAssertion ThrowsGrpcException<TValue>(this IDelegateAssertionSource<TValue> source, StatusCode expected)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.Context.ExpressionBuilder.Append(string.Concat(".ThrowsGrpcException(StatusCode.", expected.ToString(), ")"));
        return new GrpcExceptionAssertion(source.Context.MapException<RpcException>(), expected);
    }

    /// <summary>
    /// Asserts that the delegate completes without throwing a gRPC <see cref="RpcException"/>.
    /// </summary>
    /// <typeparam name="TValue">The evaluated value type of the delegate under assertion.</typeparam>
    /// <param name="source">The delegate assertion source.</param>
    /// <returns>A <see cref="GrpcDoesNotThrowAssertion{TValue}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static GrpcDoesNotThrowAssertion<TValue> DoesNotThrowGrpcException<TValue>(this IDelegateAssertionSource<TValue> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.Context.ExpressionBuilder.Append(".DoesNotThrowGrpcException()");
        return new GrpcDoesNotThrowAssertion<TValue>(source.Context);
    }
}
