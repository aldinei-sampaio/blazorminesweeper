namespace BlazorMinesweeper.Models;

public sealed class Square(int row, int col, int neighborsCount) : IDisposable
{
    private bool _isOpenned = false;
    private bool _isRevealed = false;
    private SquareState _state = SquareState.Normal;
    private TipType _tipType = TipType.None;
    private readonly int _neighborsCount = neighborsCount;
    private int _neighborsClosed = neighborsCount;

    public event Action? OnUpdate;
    public bool HasMine { get; set; } = false;
    public bool HasExploded { get; set; } = false;
    public int DisplayNumber { get; set; } = 0;
    public bool HasNeighborsClosed => _neighborsClosed > 0;
    public SquareState State => _state;
    public bool IsOpenned => _isOpenned;
    public int Row => row;
    public int Col => col;
    public TipType TipType
    {
        get => _tipType;
        set 
        {
            if (_tipType != value) {
                _tipType = value;
                OnUpdate?.Invoke();
            }
        }
    }

    public void Reveal()
    {
        if (_isRevealed)
            return;
        _isRevealed = true;
        if (!_isOpenned)
            OnUpdate?.Invoke();
    }

    public SquareDisplayMode DisplayMode
    {
        get
        {
            if (_isOpenned)
            {
                if (HasExploded)
                    return SquareDisplayMode.Exploded;

                if (HasMine)
                    return SquareDisplayMode.Mine;

                if (DisplayNumber == 0)
                    return SquareDisplayMode.Empty;

                if (HasNeighborsClosed && !_isRevealed)
                    return SquareDisplayMode.NumberActive;
                
                return SquareDisplayMode.NumberInactive;
            }

            if (_isRevealed)
            {
                if (_state == SquareState.Flagged)
                {
                    if (HasMine)
                        return SquareDisplayMode.Flag;
                    
                    return SquareDisplayMode.WrongFlag;
                }

                if (HasMine)
                    return SquareDisplayMode.Mine;

                if (DisplayNumber == 0)
                    return SquareDisplayMode.Empty;

                return SquareDisplayMode.NumberInactive;
            }

            return _state switch
            {
                SquareState.Normal => SquareDisplayMode.Normal,
                SquareState.Flagged => SquareDisplayMode.Flag,
                SquareState.Unknown => SquareDisplayMode.Unknown,
                _ => SquareDisplayMode.Normal
            };
        }
    }

    public void Reset()
    {
        _isOpenned = false;
        _state = SquareState.Normal;
        HasMine = false;
        HasExploded = false;
        DisplayNumber = 0;
        _tipType = TipType.None;
        _neighborsClosed = _neighborsCount;
        OnUpdate?.Invoke();
    }

    public void Open()
    {
        if (_isOpenned) 
            return;
        
        _isOpenned = true;

        if (HasMine)
            HasExploded = true;
        
        OnUpdate?.Invoke();
    }

    public void ToggleState()
    {
        _state = _state switch
        {
            SquareState.Normal => SquareState.Flagged,
            SquareState.Flagged => SquareState.Unknown,
            SquareState.Unknown => SquareState.Normal,
            _ => _state
        };
        OnUpdate?.Invoke();
    }

    public void IncrementNeighborsClosed()
    {
        _neighborsClosed++;
        if (_neighborsClosed == 1)
            OnUpdate?.Invoke();
    }

    public void DecrementNeighborsClosed()
    {
        if (_neighborsClosed <= 0)
            return;

        _neighborsClosed--;

        if (_neighborsClosed == 0)
            OnUpdate?.Invoke();
    }

    public void Dispose()
    {
        OnUpdate = null;
    }
}
