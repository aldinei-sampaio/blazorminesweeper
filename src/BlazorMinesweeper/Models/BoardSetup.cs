namespace BlazorMinesweeper.Models;

public sealed record BoardSetup(int Rows, int Columns, int Mines)
{
    public static BoardSetup Begginer { get; } = new(Rows: 9, Columns: 9, Mines: 10);
    public static BoardSetup Intermediate { get; } = new(Rows: 16, Columns: 16, Mines: 40 );
    public static BoardSetup Expert { get; } = new(Rows: 16, Columns: 30, Mines: 99);
    public static BoardSetup Minimum { get; } = new(Rows: 8, Columns: 8, Mines: 10);
    public static BoardSetup Maximum { get; } = new(Rows: 24, Columns: 30, Mines: 667);
    public static int GetMaximumMines(int rows, int columns) => rows * columns - 1;
    public static bool IsValid(BoardSetup setup)
    {
        if (setup.Rows < Minimum.Rows || setup.Rows > Maximum.Rows)
            return false;

        if (setup.Columns < Minimum.Columns || setup.Columns > Maximum.Columns)
            return false;

        if (setup.Mines < Minimum.Mines || setup.Mines > GetMaximumMines(setup.Rows, setup.Columns))
            return false;

        return true;
    }
}
