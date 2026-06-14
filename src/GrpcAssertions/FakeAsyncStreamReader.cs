using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace GrpcAssertions;

/// <summary>
/// An <see cref="IAsyncStreamReader{T}"/> over a fixed, in-memory list of responses, optionally
/// faulting with an <see cref="RpcException"/> once the responses are exhausted. Backs the
/// server-streaming call builders on <see cref="GrpcCallBuilder"/>; it is internal because consumers
/// build streaming calls through those builders rather than constructing readers directly.
/// </summary>
/// <typeparam name="T">The streamed response message type.</typeparam>
internal sealed class FakeAsyncStreamReader<T> : IAsyncStreamReader<T>
{
    private readonly IReadOnlyList<T> _items;
    private readonly RpcException? _faultAfterItems;
    private int _index = -1;

    /// <summary>Initializes the reader.</summary>
    /// <param name="items">The responses to yield in order.</param>
    /// <param name="faultAfterItems">The exception to throw on the read that follows the last item,
    /// or <see langword="null"/> to end the stream cleanly.</param>
    public FakeAsyncStreamReader(IReadOnlyList<T> items, RpcException? faultAfterItems)
    {
        _items = items;
        _faultAfterItems = faultAfterItems;
    }

    /// <inheritdoc/>
    public T Current => _index >= 0 && _index < _items.Count
        ? _items[_index]
        : throw new InvalidOperationException("No current element; call MoveNext and check its result first.");

    /// <inheritdoc/>
    public Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_index < _items.Count)
        {
            _index++;
        }

        if (_index < _items.Count)
        {
            return Task.FromResult(true);
        }

        return _faultAfterItems is not null
            ? Task.FromException<bool>(_faultAfterItems)
            : Task.FromResult(false);
    }
}
