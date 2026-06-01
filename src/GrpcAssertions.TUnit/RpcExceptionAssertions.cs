using System;
using GrpcAssertions;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace GrpcAssertions.TUnit;

/// <summary>
/// TUnit-native fluent entry points for asserting on gRPC exceptions. The v0.0.1 surface ships the
/// <c>IsRpcException()</c> discriminator only; the full outcome-assertion surface
/// (<c>ThrowsGrpcException</c> and the <c>StatusCode</c> shorthands, plus the <c>GrpcCallBuilder</c>
/// test infrastructure) ships in v0.1.0.
/// </summary>
/// <remarks>
/// Source methods carry the <c>[GenerateAssertion]</c> attribute; TUnit's source generator emits
/// the fluent <c>Assert.That(...).&lt;Method&gt;()</c> entry point at consumer build time. The
/// generated chain is AOT-clean (no runtime reflection in the assertion path).
/// </remarks>
public static class RpcExceptionAssertions
{
    /// <summary>
    /// Asserts that the exception is a gRPC <c>RpcException</c> (the exception type a failed gRPC
    /// call surfaces). Use it as a lightweight discriminator when a test has caught an exception
    /// and wants to confirm it originated from a gRPC call before inspecting its status.
    /// </summary>
    /// <param name="value">The exception under test.</param>
    /// <returns>A passing assertion when <paramref name="value"/> is a gRPC <c>RpcException</c>;
    /// otherwise a failing assertion naming the actual exception type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    [GenerateAssertion]
    public static AssertionResult IsRpcException(this Exception value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return GrpcExceptions.IsRpcException(value)
            ? AssertionResult.Passed
            : AssertionResult.Failed(string.Concat(
                "the exception to be a gRPC RpcException\n  but got: ",
                value.GetType().FullName));
    }
}
