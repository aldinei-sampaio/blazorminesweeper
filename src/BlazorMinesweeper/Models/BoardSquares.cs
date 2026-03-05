namespace BlazorMinesweeper.Models;

public sealed class BoardSquares : IDisposable
{
    private readonly Square[,] _squares;

    public int Rows { get; }
    public int Columns { get; }
    public int MinRow { get; } = 0;
    public int MaxRow => Rows - 1;
    public int MinColumn { get; } = 0;
    public int MaxColumn => Columns - 1;
    public int SquareCount => Rows * Columns;

    public BoardSquares(int rows, int columns)
    {
        if (rows <= 0 || columns <= 0)
            throw new ArgumentException("rows e cols precisam ser maiores que zero");

        Rows = rows;
        Columns = columns;
        _squares = new Square[rows, columns];

        for (var i = MinRow; i <= MaxRow; i++)
        {
            var rowCorner = (i == MinRow || i == MaxRow);
            for (var j = MinColumn; j <= MaxColumn; j++)
            {
                var colCorner = (j == MinColumn || j == MaxColumn);
                var neighborCount = rowCorner ? (colCorner ? 3 : 5) : (colCorner ? 5 : 8);
                _squares[i, j] = new Square(i, j, neighborCount);
            }
        }
    }

    public Square this[int row, int column]
    {
        get
        {
            if (row < MinRow || row > MaxRow || column < MinColumn || column > MaxColumn)
                throw new ArgumentException("Valor de row ou col inválido");
            return _squares[row, column];
        }
    }

    public void Reveal() => ForEach(i => i.Reveal());

    public void Reset() => ForEach(i => i.Reset());

    public void Dispose() => ForEach(i => i.Dispose());

    public void ForEach(Action<Square> callbackfn)
    {
        for (var i = MinRow; i <= MaxRow; i++)
        {
            for (var j = MinColumn; j <= MaxColumn; j++)
            {
                callbackfn(_squares[i, j]);
            }
        }
    }

    public void ForEachInVicinity(Square item, Func<Square, bool> callbackfn)
    {
        for (var i = Math.Max(item.Row - 1, MinRow); i <= Math.Min(item.Row + 1, MaxRow); i++)
        {
            for (var j = Math.Max(item.Column - 1, MinColumn); j <= Math.Min(item.Column + 1, MaxColumn); j++)
            {
                if (i == item.Row && j == item.Column)
                    continue;

                if (callbackfn(_squares[i, j]))
                    return;
            }
        }
    }

    public void ForEachInVicinity(Square item, Action<Square> callbackfn)
    {
        for (var i = Math.Max(item.Row - 1, MinRow); i <= Math.Min(item.Row + 1, MaxRow); i++)
        {
            for (var j = Math.Max(item.Column - 1, MinColumn); j <= Math.Min(item.Column + 1, MaxColumn); j++)
            {
                if (i == item.Row && j == item.Column)
                    continue;

                callbackfn(_squares[i, j]);
            }
        }
    }

    public Square? Find(int startingRow, int startingColumn, Func<Square, Square?> callback)
    {
        for (var i = startingRow; i <= MaxRow; i++)
        {
            for (var j = startingColumn; j <= MaxColumn; j++)
            {
                var square = callback(_squares[i, j]);
                if (square != null)
                    return square;
            }
        }

        for (var i = 0; i < startingRow; i++)
        {
            for (var j = 0; j < startingColumn; j++)
            {
                var square = callback(_squares[i, j]);
                if (square != null)
                    return square;
            }
        }

        return null;
    }
}
