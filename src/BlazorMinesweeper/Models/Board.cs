namespace BlazorMinesweeper.Models;

public sealed class Board : IDisposable
{
    private BoardSetup _setup;
    private bool _preventImediateWin;
    private BoardSquares _boardSquares;

    private int _openCount = 0;
    private GameState _state = GameState.Ready;
    private int _flaggedCount = 0;
    private int _mineCount = 0;
    private int _putMinesLater = 0;

    public event Action? OnStateChange;
    public event Action? OnFlagCountChange;
    public event Action? OnBoardChange;

    public BoardTimer Timer { get; } = new();
    public GameState State => _state;

    public Board(BoardSetup setup, bool preventImediateWin)
    {
        _setup = setup;
        _preventImediateWin = preventImediateWin;
        _boardSquares = new(setup.Rows, setup.Columns);
        Initialize();
    }

    public void Reset()
    {
        _boardSquares.Reset();
        Initialize();
    }

    public void Reset(BoardSetup setup, bool preventImediateWin)
    {
        if (setup.Rows != _boardSquares.Rows || setup.Columns != _boardSquares.Columns)
        {
            _boardSquares.Dispose();
            _boardSquares = new BoardSquares(setup.Rows, setup.Columns);
        }
        else
        {
            _boardSquares.Reset();
        }
        _setup = setup;
        _preventImediateWin = preventImediateWin;
        Initialize();
    }

    private void Initialize()
    {
        _openCount = 0;
        _flaggedCount = 0;
        _putMinesLater = 0;
        _mineCount = 0;
        PutMines();
        if (_state != GameState.Ready)
        {
            _state = GameState.Ready;
            OnStateChange?.Invoke();
        }
    }

    public void Dispose()
    {
        Timer.Dispose();
        OnStateChange = null;
        _boardSquares.Dispose();
    }

    public int RemainingFlags => _mineCount + _putMinesLater - _flaggedCount;    
    public int MinRow => _boardSquares.MinRow;
    public int MaxRow => _boardSquares.MaxRow;
    public int MinColumn => _boardSquares.MinColumn;
    public int MaxColumn => _boardSquares.MaxColumn;

    public Square this[int row, int column] => _boardSquares[row, column];

    private int OpenableCount => _boardSquares.SquareCount - _mineCount;
        
    private void PutMines()
    {
        var maximumMines = BoardSetup.GetMaximumMines(_setup.Rows, _setup.Columns);
        var mineCount = Math.Clamp(_setup.Mines, BoardSetup.Minimum.Mines, maximumMines);

        if (_preventImediateWin) 
            _putMinesLater += mineCount;
        else
            PutAllMines(mineCount);
    }

    private void PutAllMines(int mineCount, Square? exceptSquare = null)
    { 
        for (var n = 1; n <= mineCount; n++)
        {
            Square square;
            do
            {
                var (row, col) = GetRandomCoordinates();
                square = _boardSquares[row, col];
            } while (!ValidateSquare(square, exceptSquare));
            PutMine(square);
        }
    }

    private static (int Row, int Column) GetRandomCoordinates()
    {
        var row = Random.Shared.Next(0, 100);
        var column = Random.Shared.Next(0, 100);
        return (row, column);
    }

    private static bool ValidateSquare(Square square, Square? exceptSquare)
    {
        if (square.HasMine)
            return false;

        if (exceptSquare is not null && AreNeighbors(exceptSquare, square))
            return false;

        return true;
    }

    private void PutMine(Square square)
    {
        if (square.HasMine)
            return;

        square.HasMine = true;
        _mineCount++;
        _boardSquares.ForEachInVicinity(square, i => i.DisplayNumber++);
    }

    private static bool AreNeighbors(Square s1, Square s2)
    {
        return s2.Row >= s1.Row - 1
            && s2.Row <= s1.Row + 1
            && s2.Column >= s1.Column - 1
            && s2.Column <= s1.Column + 1;
    }
        
    private void RegisterStart()
    {
        if (_state != GameState.Ready)
            return;
            
        _state = GameState.Started;
        OnStateChange?.Invoke();

        Timer.Start();
    }

    public void Open(Square square)
    {
        if (_putMinesLater > 0) {
            PutAllMines(_putMinesLater, square);
            _putMinesLater = 0;
        }

        ProcessOpen(square);
        OnBoardChange?.Invoke();
    }

    private void ProcessOpen(Square square)
    {
        RegisterStart();
        OpenSquare(square);

        if (square.HasMine)
        {
            Timer.Stop();
            _state = GameState.Lost;
            _boardSquares.Reveal();
            OnStateChange?.Invoke();
            return;
        }

        _openCount++;

        if (square.DisplayNumber == 0)
            OpenAllNeighbors(square);
        else
            OpenEmptyNeighbors(square);

        if (_openCount == OpenableCount)
        {
            Timer.Stop();
            _state = GameState.Won;
            OnStateChange?.Invoke();
            return;
        }
    }

    private void OpenSquare(Square square)
    {
        square.Open();
        _boardSquares.ForEachInVicinity(square, i => i.DecrementNeighborsClosed());
    }

    private void OpenEmptyNeighbors(Square square) 
    {
        _boardSquares.ForEachInVicinity(square, (item) => 
        {
            if (!item.IsOpen && item.State == SquareState.Normal && item.DisplayNumber == 0)
            {
                OpenSquare(item);
                _openCount++;
                OpenAllNeighbors(item);
            }
            return (_state == GameState.Won || _state == GameState.Lost);
        });
    }

    private void OpenAllNeighbors(Square square)
    {
        _boardSquares.ForEachInVicinity(square, (item) => 
        {
            if (!item.IsOpen && item.State == SquareState.Normal) 
            {
                OpenSquare(item);
                _openCount++;
                if (item.DisplayNumber == 0)
                    OpenAllNeighbors(item);
            }
            return (_state == GameState.Won || _state == GameState.Lost);
        });
    }

    public void OpenNeighborhood(Square square)
    {
        var flagsFound = 0;
        _boardSquares.ForEachInVicinity(square, (item) => 
        {
            if (!item.IsOpen && item.State == SquareState.Flagged)
                flagsFound++;
        });

        if (flagsFound < square.DisplayNumber)
            return;

        var found = false;
        _boardSquares.ForEachInVicinity(square, (item) => 
        {
            if (!item.IsOpen && item.State == SquareState.Normal)
            {
                found = true;
                ProcessOpen(item);
            }
        });

        if (found)
            OnBoardChange?.Invoke();
    }

    public void ToggleState(Square square)
    {
        if (square.State == SquareState.Flagged)
        {
            _flaggedCount--;
            _boardSquares.ForEachInVicinity(square, i => i.IncrementNeighborsClosed());
            OnFlagCountChange?.Invoke();
        }
        else if (square.State == SquareState.Normal)
        {
            if (RemainingFlags <= 0)
                return;
            _flaggedCount++;
            _boardSquares.ForEachInVicinity(square, i => i.DecrementNeighborsClosed());
            OnFlagCountChange?.Invoke();
        }
        else
        {
            return;
        }
        RegisterStart();
        square.ToggleState();
        OnBoardChange?.Invoke();
    }

    public bool TryCreateTip()
    {
        if (_openCount == 0)
            return false;

        var (row, column) = GetRandomCoordinates();
        var square = _boardSquares.Find(row, column, TryCreateTipInVincinity);
        if (square is null)
            return false;

        OnBoardChange?.Invoke();
        return true;
    }

    private Square? TryCreateTipInVincinity(Square square)
    {
        if (!square.IsOpen)
            return null;

        var flagCount = 0;
        var closedCount = 0;
        Square? candidate = null;

        _boardSquares.ForEachInVicinity(square, i => {
            if (!square.IsOpen)
            {
                closedCount++;
                if (i.State == SquareState.Flagged)
                    flagCount++;
                else if (i.TipType == TipType.None)
                    candidate = i;
            }
        });

        if (candidate is null)
            return null;

        if (square.DisplayNumber == closedCount)
        {
            candidate.TipType = TipType.Mine;
            return candidate;
        }

        if (square.DisplayNumber == flagCount)
        {
            candidate.TipType = TipType.Safe;
            return candidate;
        }

        return null;
    }
}
