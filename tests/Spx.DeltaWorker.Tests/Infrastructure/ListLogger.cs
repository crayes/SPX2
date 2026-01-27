using Microsoft.Extensions.Logging;

namespace Spx.DeltaWorker.Tests.Infrastructure;

public sealed class ListLogger<T> : ILogger<T>
{
    public readonly List<LogEntry> Entries = [];

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
        => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
    }

    public sealed record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
