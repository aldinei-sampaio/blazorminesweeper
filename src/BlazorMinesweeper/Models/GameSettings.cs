namespace BlazorMinesweeper.Models;

public sealed class GameSettings
{
    public BoardSetup BoardSetup { get; set; } = BoardSetup.Begginer;
    public bool PreventImmediateGameOver { get; set; } = true;
    public bool AllowTips { get; set; } = true;
    public bool AllowAutoPlay { get; set; } = false;
}