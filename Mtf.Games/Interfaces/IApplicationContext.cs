namespace Mtf.Games.Interfaces;

public interface IApplicationContext
{
    void Start(string destinationAppName);

    void StopApplication();
}