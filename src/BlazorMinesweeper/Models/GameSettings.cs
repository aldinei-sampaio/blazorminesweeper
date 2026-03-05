namespace BlazorMinesweeper.Models;

public sealed class GameSettings
{
    public BoardSetup BoardSetup { get; set; } = BoardSetup.Begginer;
    public bool PreventImediateWin { get; set; } = true;
    public bool AllowTips { get; set; } = true;
    public bool AutoPlay { get; set; } = false;
}