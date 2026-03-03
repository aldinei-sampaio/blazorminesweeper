using System.Diagnostics;

namespace BlazorMinesweeper.Models;

public sealed class BoardTimer : IDisposable
{
    public event Action? OnElapsedTime;
    public TimeSpan ElapsedTime => Stopwatch.GetElapsedTime(startTimestamp);

    private readonly Timer _timer;
    private readonly TimeSpan _interval = TimeSpan.FromMilliseconds(0);
    private bool _isRunning;
    private long startTimestamp = Stopwatch.GetTimestamp();

    public BoardTimer()
    {
        _timer = new Timer(_ =>
        {
            try
            {
                OnElapsedTime?.Invoke();
                if (ElapsedTime.TotalSeconds >= 5999)
                    Stop();
            }
            catch
            {
                // Evita que exceções internas derrubem o timer
            }
        });
    }

    public void Start()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        startTimestamp = Stopwatch.GetTimestamp();
        _timer.Change(_interval, _interval);
        OnElapsedTime?.Invoke();
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
        OnElapsedTime = null;
        _timer.Dispose();
    } 
}
