using Mtf.Games.Interfaces;

namespace Mtf.Games.General;

public class MovingHandler(IMoveable moveable)
{
    private readonly IMoveable moveable = moveable;

    public IButtonStates HandleKeyPress(IInputContext canvasContext)
    {
        var buttonStates = canvasContext.GetButtonStates();
        moveable.MoveRight(buttonStates.IsRightButtonPressed());
        moveable.MoveLeft(buttonStates.IsLeftButtonPressed());
        moveable.MoveDown(buttonStates.IsDownButtonPressed());
        moveable.MoveUp(buttonStates.IsUpButtonPressed());
        return buttonStates;
    }
}
