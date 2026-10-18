using Mtf.Drawing;
using Mtf.Drawing.Interfaces;
using Mtf.Games.Interfaces;
using System.Drawing;

namespace Mtf.Games.Tests.Fakes;

public sealed class FakeGameContext : IGameContext
{
    public int ScreenWidth { get; init; } = 100;
    public int ScreenHeight { get; init; } = 100;
    public int HorizontalCenter => ScreenWidth / 2;
    public int VerticalCenter => ScreenHeight / 2;

    public List<IPrimitive> Drawn { get; } = [];
    public List<string> Texts { get; } = [];
    public List<string> Calls { get; } = [];
    public Queue<IButtonStates> ButtonStateQueue { get; } = new();
    public int MaxButtonStateReads { get; init; } = 1000;
    private int buttonStateReads;
    public FakeButtonStates DefaultButtonStates { get; set; } = new();

    public void Clear() => Calls.Add(nameof(Clear));
    public void Update() => Calls.Add(nameof(Update));
    public void Draw(IPrimitive primitive, Color color) => Drawn.Add(primitive);

    public void ShowOnMiddleOfScreen(string text, FontType fontType, int v2)
    {
        Calls.Add(nameof(ShowOnMiddleOfScreen));
        Texts.Add(text);
    }

    public IButtonStates GetButtonStates()
    {
        Calls.Add(nameof(GetButtonStates));
        if (++buttonStateReads > MaxButtonStateReads)
        {
            throw new InvalidOperationException($"GetButtonStates called more than {MaxButtonStateReads} times; game loop is probably stuck.");
        }
        return ButtonStateQueue.Count > 0 ? ButtonStateQueue.Dequeue() : DefaultButtonStates;
    }

    public void PlayGameOver() => Calls.Add(nameof(PlayGameOver));
    public void PlayYouWon() => Calls.Add(nameof(PlayYouWon));
    public void Start(string destinationAppName) => Calls.Add(nameof(Start));
    public void StopApplication() => Calls.Add(nameof(StopApplication));
    public void Finish() => Calls.Add(nameof(Finish));
    public void StartGame() => Calls.Add(nameof(StartGame));
}
