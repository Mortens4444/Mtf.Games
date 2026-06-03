using Mtf.Games.Interfaces;

namespace Snake.WinForms.Game;

public class WinFormsButtonStates(HashSet<Keys> keys) : IButtonStates
{
    private readonly HashSet<Keys> keys = keys;

    public bool IsUpButtonPressed() => keys.Contains(Keys.Up);
    
    public bool IsDownButtonPressed() => keys.Contains(Keys.Down);
    
    public bool IsLeftButtonPressed() => keys.Contains(Keys.Left);
    
    public bool IsRightButtonPressed() => keys.Contains(Keys.Right);
        
    public bool IsCenterButtonPressed() => keys.Contains(Keys.Space);

    public bool IsBackButtonPressed() => keys.Contains(Keys.Escape);

    public bool IsAnyButtonPressed() => keys.Count > 0;
}