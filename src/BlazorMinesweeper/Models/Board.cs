using System.Diagnostics;

namespace BlazorMinesweeper.Models;

public sealed class Board : IDisposable
{
    private readonly Square[,] _squares;
    private readonly int _rows = 0;
    private readonly int _cols = 0;
    private readonly int _minRow = 0;
    private readonly int _minCol = 0;
    private readonly int _maxRow = 0;
    private readonly int _maxCol = 0;
    private readonly int _squareCount = 0;

    private int _openedCount = 0;
    private GameState _state = GameState.Ready;
    private int _flaggedCount = 0;
    private int _mineCount = 0;
    private int _putMinesLater = 0;

    public event Action? OnStateChange;
    public event Action? OnFlagCountChange;

    public BoardTimer Timer { get; } = new();
    public GameState State => _state;

    public Board(int rows, int cols)
    {
        if (rows <= 0 || cols <= 0)
            throw new ArgumentException("rows e cols precisam ser maiores que zero");
        
        _squares = new Square[rows, cols];

        _rows = rows;
        _cols = cols;
        _minRow = 0;
        _maxRow = rows - 1;
        _minCol = 0;
        _maxCol = cols - 1;

        _squareCount = rows * cols;

        for (var i = _minRow; i <= _maxRow; i++)
        {
            var rowCorner = (i == _minRow || i == _maxRow);
            for (var j = _minCol; j <= _maxCol; j++)
            {
                var colCorner = (j == _minCol || j == _maxCol);
                var neighborCount = rowCorner ? (colCorner ? 3 : 5) : (colCorner ? 5 : 8);
                _squares[i, j] = new Square(i, j, neighborCount);
            }
        }
    }

    public void Reset()
    {
        Timer.Stop();
        _openedCount = 0;
        _flaggedCount = 0;
        _putMinesLater = _mineCount;
        _mineCount = 0;
        for (var i = _minRow; i <= _maxRow; i++)
        {
            for (var j = _minCol; j <= _maxCol; j++)
            {
                _squares[i, j].Reset();
            }
        }
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
        for (var i = _minRow; i <= _maxRow; i++)
        {
            for (var j = _minCol; j <= _maxCol; j++)
            {
                _squares[i, j].Dispose();
            }
        }
    }

    public int RemainingFlags => _mineCount + _putMinesLater - _flaggedCount;    
    public int MinRow => _minRow;
    public int MaxRow => _maxRow;
    public int MinCol => _minCol;
    public int MaxCol => _maxCol;
    public int ColCount => _cols;

    public Square GetSquare(int row, int col)
    {
        if (row < _minRow || row > _maxRow || col < _minCol || col > _maxCol)
            throw new ArgumentException("Valor de row ou col inválido");

        return _squares[row, col];
    }

    private int OpenableCount => _squareCount - _mineCount;
        
    public void PutMines(int mineCount, bool onFirstOpening)
    {
        if (mineCount + _mineCount > OpenableCount - 9)
            throw new ArgumentException("Não há espaço suficiente para o número de minas informado.");
    
        if (onFirstOpening) 
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
                square = GetSquare(row, col);
            } while (!ValidateSquare(square, exceptSquare));
            PutMine(square);
        }
    }

    private static (int Row, int Col) GetRandomCoordinates()
    {
        var row = Random.Shared.Next(0, 100);
        var col = Random.Shared.Next(0, 100);
        return (row, col);
    }

    private static bool ValidateSquare(Square square, Square? exceptSquare)
    {
        if (square.HasMine)
            return false;

        if (exceptSquare is not null && AreNeighbors(exceptSquare, square))
            return false;

        return true;
    }

    public void PutMine(Square square)
    {
        if (square.HasMine)
            return;

        square.HasMine = true;
        _mineCount++;
        ForEachInVicinity(square, (item) => { item.DisplayNumber++; return false; });
    }

    private static bool AreNeighbors(Square s1, Square s2)
    {
        return s2.Row >= s1.Row - 1
            && s2.Row <= s1.Row + 1
            && s2.Col >= s1.Col - 1
            && s2.Col <= s1.Col + 1;
    }

    private void ForEachInVicinity(Square item, Func<Square, bool> callbackfn)
    {
        for (var i = Math.Max(item.Row - 1, _minRow); i <= Math.Min(item.Row + 1, _maxRow); i++) 
        {
            for (var j = Math.Max(item.Col - 1, _minCol); j <= Math.Min(item.Col + 1, _maxCol); j++)
            {
                if (i == item.Row && j == item.Col)
                    continue;
                
                if (callbackfn(GetSquare(i, j)))
                    return;
            }
        }
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

        RegisterStart();

        OpenSquare(square);

        if (square.HasMine)
        {
            Timer.Stop();
            _state = GameState.Lost;
            Reveal();
            OnStateChange?.Invoke();
            return;
        }

        _openedCount++;

        if (square.DisplayNumber == 0)
            OpenAllNeighbors(square);
        else
            OpenEmptyNeighbors(square);

        if (_openedCount == OpenableCount)
        {
            Timer.Stop();
            _state = GameState.Won;
            OnStateChange?.Invoke();
        }
    }

    private void Reveal()
    {
        for (var i = _minRow; i <= _maxRow; i++)
        {
            for (var j = _minCol; j <= _maxCol; j++)
            {
                GetSquare(i, j).Reveal();
            }
        }
    }

    private void OpenSquare(Square square)
    {
        square.Open();
        ForEachInVicinity(square, (i) => { i.DecrementNeighborsClosed(); return false; });
    }

    private void OpenEmptyNeighbors(Square square) 
    {
        ForEachInVicinity(square, (item) => 
        {
            if (!item.IsOpenned && item.State == SquareState.Normal && !item.HasMine && item.DisplayNumber == 0)
            {
                OpenSquare(item);
                _openedCount++;
                OpenAllNeighbors(item);
            }
            return (_state == GameState.Won || _state == GameState.Lost);
        });
    }

    private void OpenAllNeighbors(Square square)
    {
        ForEachInVicinity(square, (item) => 
        {
            if (!item.IsOpenned && item.State == SquareState.Normal && !item.HasMine) 
            {
                OpenSquare(item);
                _openedCount++;
                if (item.DisplayNumber == 0)
                    OpenAllNeighbors(item);
            }
            return (_state == GameState.Won || _state == GameState.Lost);
        });
    }

    public void OpenNeighborhood(Square square)
    {
        var flagsFound = 0;
        ForEachInVicinity(square, (item) => 
        {
            if (!item.IsOpenned && item.State == SquareState.Flagged)
                flagsFound++;
            return false;
        });

        if (flagsFound < square.DisplayNumber)
            return;

        ForEachInVicinity(square, (item) => 
        {
            if (!item.IsOpenned && item.State == SquareState.Normal)
                Open(item);
            return false;
        });
    }

    public void ToggleState(Square square)
    {
        if (square.State == SquareState.Flagged)
        {
            _flaggedCount--;
            ForEachInVicinity(square, (i) => { i.IncrementNeighborsClosed(); return false; });
            OnFlagCountChange?.Invoke();
        }
        else if (square.State == SquareState.Normal)
        {
            if (RemainingFlags <= 0)
                return;
            _flaggedCount++;
            ForEachInVicinity(square, (i) => { i.DecrementNeighborsClosed(); return false; });
            OnFlagCountChange?.Invoke();
        }
        RegisterStart();
        square.ToggleState();
    }

    public Square? CreateTip()
    {
        if (_openedCount == 0)
            return null;

        var (row, col) = GetRandomCoordinates();
        return TraverseFrom(row, col, CheckIfCanBeATip);
    }

    private Square? CheckIfCanBeATip(Square square)
    {
        if (!square.IsOpenned)
            return null;

        var flagCount = 0;
        var closedCount = 0;
        Square? candidate = null;

        ForEachInVicinity(square, (square) => {
            if (!square.IsOpenned)
            {
                closedCount++;
                if (square.State == SquareState.Flagged)
                    flagCount++;
                else if (square.TipType == TipType.None)
                    candidate = square;
            }
            return false;
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

    private Square? TraverseFrom(int row, int col, Func<Square, Square?> callback)
    {
        for (var i = row; i <= _maxRow; i++)
        {
            for (var j = col; j <= _maxCol; j++)
            {
                var square = callback(GetSquare(i, j));
                if (square != null)
                    return square;
            }
        }

        for (var i = 0; i < row; i++)
        {
            for (var j = 0; j < col; j++)
            {
                var square = callback(GetSquare(i, j));
                if (square != null)
                    return square;
            }
        }

        return null;
    }
}
