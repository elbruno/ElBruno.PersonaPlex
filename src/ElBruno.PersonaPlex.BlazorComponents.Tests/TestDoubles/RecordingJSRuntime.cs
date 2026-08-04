using Microsoft.JSInterop;

namespace ElBruno.PersonaPlex.BlazorComponents.Tests.TestDoubles;

internal sealed class RecordingJSRuntime : IJSRuntime
{
    public List<InvocationRecord> Invocations { get; } = [];

    public RecordingJSObjectReference Module { get; } = new();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        Invocations.Add(new InvocationRecord(identifier, args ?? []));

        if (identifier == "import")
        {
            return ValueTask.FromResult((TValue)(object)Module);
        }

        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);

    internal sealed record InvocationRecord(string Identifier, IReadOnlyList<object?> Arguments);
}

internal sealed class RecordingJSObjectReference : IJSObjectReference
{
    public List<InvocationRecord> Invocations { get; } = [];

    public Dictionary<string, Queue<object?>> ResultsByIdentifier { get; } = new(StringComparer.Ordinal);

    public bool DisposeAsyncCalled { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        Invocations.Add(new InvocationRecord(identifier, args ?? []));

        if (ResultsByIdentifier.TryGetValue(identifier, out var results) && results.Count > 0)
        {
            var result = results.Dequeue();
            if (result is Task<TValue> taskResult)
            {
                return new ValueTask<TValue>(taskResult);
            }

            return ValueTask.FromResult((TValue)result!);
        }

        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);

    public ValueTask DisposeAsync()
    {
        DisposeAsyncCalled = true;
        return ValueTask.CompletedTask;
    }

    public void QueueResult(string identifier, object? value)
    {
        if (!ResultsByIdentifier.TryGetValue(identifier, out var results))
        {
            results = new Queue<object?>();
            ResultsByIdentifier[identifier] = results;
        }

        results.Enqueue(value);
    }

    public void QueueDeferredResult<TValue>(string identifier, Task<TValue> valueTask)
    {
        if (!ResultsByIdentifier.TryGetValue(identifier, out var results))
        {
            results = new Queue<object?>();
            ResultsByIdentifier[identifier] = results;
        }

        results.Enqueue(valueTask);
    }

    internal sealed record InvocationRecord(string Identifier, IReadOnlyList<object?> Arguments);
}
