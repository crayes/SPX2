using System.Collections.Concurrent;

namespace Spx.DeltaWorker.Infrastructure;

/// <summary>
/// Rate limiter adaptativo para controle de requisições ao Graph API.
/// Portado do Python: sharepoint_ultra/rate_limiter.py
/// </summary>
public sealed class AdaptiveRateLimiter
{
    private readonly int _maxRequestsPerSecond;
    private int _currentRate;
    private readonly ConcurrentQueue<DateTime> _requestTimes = new();
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<AdaptiveRateLimiter> _logger;
    private readonly object _rateLock = new();

    public AdaptiveRateLimiter(ILogger<AdaptiveRateLimiter> logger, int maxRequestsPerSecond = 20)
    {
        _logger = logger;
        _maxRequestsPerSecond = maxRequestsPerSecond;
        _currentRate = maxRequestsPerSecond;
        _semaphore = new SemaphoreSlim(maxRequestsPerSecond, maxRequestsPerSecond);
    }

    public int CurrentRate => _currentRate;

    /// <summary>
    /// Ajusta a taxa de requests dinamicamente.
    /// Chamado quando recebe HTTP 429 (reduce=true) ou quando requests estão fluindo bem (reduce=false).
    /// </summary>
    public void AdjustRate(bool reduce)
    {
        lock (_rateLock)
        {
            if (reduce)
            {
                _currentRate = Math.Max(5, _currentRate / 2);
                _logger.LogWarning("RATE_ADJUST: Reduzindo para {rate}/s devido a throttling", _currentRate);
            }
            else
            {
                _currentRate = Math.Min(_maxRequestsPerSecond * 2, _currentRate + 5);
                _logger.LogDebug("RATE_ADJUST: Aumentando para {rate}/s", _currentRate);
            }
        }
    }

    /// <summary>
    /// Adquire permissão para fazer uma requisição, respeitando o rate limit.
    /// </summary>
    public async Task AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        try
        {
            // Limpa timestamps antigos (mais de 1 segundo)
            var cutoff = DateTime.UtcNow.AddSeconds(-1);
            while (_requestTimes.TryPeek(out var oldest) && oldest < cutoff)
            {
                _requestTimes.TryDequeue(out _);
            }

            // Se atingiu o limite, espera
            while (_requestTimes.Count >= _currentRate)
            {
                if (_requestTimes.TryPeek(out var oldest))
                {
                    var waitTime = oldest.AddSeconds(1) - DateTime.UtcNow;
                    if (waitTime > TimeSpan.Zero)
                    {
                        _logger.LogDebug("RATE_LIMIT: Aguardando {ms}ms...", waitTime.TotalMilliseconds);
                        await Task.Delay(waitTime, cancellationToken);
                    }
                }
                
                // Limpa novamente após esperar
                cutoff = DateTime.UtcNow.AddSeconds(-1);
                while (_requestTimes.TryPeek(out var old) && old < cutoff)
                {
                    _requestTimes.TryDequeue(out _);
                }
            }

            _requestTimes.Enqueue(DateTime.UtcNow);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Retorna estatísticas do rate limiter.
    /// </summary>
    public (int CurrentRate, int QueuedRequests) GetStats()
    {
        return (_currentRate, _requestTimes.Count);
    }
}
