namespace Mtf.Games.Interfaces;

public interface IButtonStates
{
    bool IsUpButtonPressed();

    bool IsCenterButtonPressed();

    bool IsDownButtonPressed();

    bool IsRightButtonPressed();

    bool IsLeftButtonPressed();

    bool IsBackButtonPressed();

    bool IsAnyButtonPressed();
}
