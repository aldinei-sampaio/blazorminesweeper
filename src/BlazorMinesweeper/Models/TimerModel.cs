using System.Diagnostics;

namespace BlazorMinesweeper.Models;

public sealed class TimerModel : IDisposable
{
    public event Action? OnTick;
    public TimeSpan ElapsedTime => Stopwatch.GetElapsedTime(startTimestamp);
    public bool IsRunning => _isRunning;

    private readonly Timer _timer;
    private readonly TimeSpan _interval = TimeSpan.FromMilliseconds(500);
    private bool _isRunning;
    private long startTimestamp = Stopwatch.GetTimestamp();

    public TimerModel()
    {
        _timer = new Timer(_ =>
        {
            try
            {
                OnTick?.Invoke();
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
        OnTick?.Invoke();
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        OnTick?.Invoke();
    }

    public void Reset()
    {
        startTimestamp = Stopwatch.GetTimestamp();
        if (_isRunning)
            Stop();
        else
            OnTick?.Invoke();
    }

    public void Dispose()
    {
        OnTick = null;
        _timer.Dispose();
    } 
}
