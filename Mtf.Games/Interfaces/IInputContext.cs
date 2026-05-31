namespace Mtf.Games.Interfaces;

public interface IInputContext
{
    void ChangeTopLine(object enable);

    public IButtonStates GetButtonStates();
 }
