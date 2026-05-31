using Mtf.Games.Interfaces;

namespace Mtf.Games.General;

public class DirectionHandler(IDirectable directable)
{
    private readonly IDirectable directable = directable;

    public IButtonStates HandleKeyPress(IInputContext canvasContext)
    {
        var buttonStates = canvasContext.GetButtonStates();
        if (buttonStates.IsRightButtonPressed())
        {
            directable.ChangeDirection(Direction.East);
        }
        else if (buttonStates.IsLeftButtonPressed())
        {
            directable.ChangeDirection(Direction.West);
        }
        else if (buttonStates.IsDownButtonPressed())
        {
            directable.ChangeDirection(Direction.South);
        }
        else if (buttonStates.IsUpButtonPressed())
        {
            directable.ChangeDirection(Direction.North);
        }
        return buttonStates;
    }
}
