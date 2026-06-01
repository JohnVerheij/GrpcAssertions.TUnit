using System;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcAssertions;
using TUnit.Assertions.Core;

namespace GrpcAssertions.TUnit;

/// <summary>
/// Fluent TUnit assertion that a delegate completes without throwing a gRPC
/// <see cref="RpcException"/>: the shape of the "benign error is swallowed" tests. Constructed by
/// the <c>DoesNotThrowGrpcException()</c> entry point on a delegate assertion source.
/// </summary>
/// <remarks>
/// The assertion runs over the raw delegate evaluation, so the thrown exception (if any) is in the
/// metadata exception field. A thrown <see cref="RpcException"/> fails with the full gRPC outcome
/// rendered; any other thrown exception fails naming its type and message (the call faulted, which
/// the swallow tests do not expect); no exception passes.
/// </remarks>
/// <typeparam name="TValue">The evaluated value type of the delegate under assertion.</typeparam>
public sealed class GrpcDoesNotThrowAssertion<TValue> : Assertion<TValue>
{
    /// <summary>Initializes the assertion. Called by the <c>DoesNotThrowGrpcException()</c> entry point.</summary>
    /// <param name="context">The assertion context supplied by TUnit.</param>
    public GrpcDoesNotThrowAssertion(AssertionContext<TValue> context) : base(context)
    {
    }

    /// <inheritdoc/>
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var exception = metadata.Exception;
        if (exception is RpcException rpc)
        {
            return Task.FromResult(AssertionResult.Failed(string.Concat("it threw ", GrpcOutcomeRendering.Describe(rpc))));
        }

        if (exception is not null)
        {
            return Task.FromResult(AssertionResult.Failed(string.Concat(
                "it threw ", exception.GetType().Name, ": ", exception.Message)));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    /// <inheritdoc/>
    protected override string GetExpectation() => "the gRPC call not to throw an RpcException";
}
