using Mtf.Games.Interfaces;

namespace Mtf.Games.Tests.Fakes;

public sealed class FakeButtonStates : IButtonStates, ISecondaryButtonStates
{
    public bool Up { get; init; }
    public bool Center { get; init; }
    public bool Down { get; init; }
    public bool Right { get; init; }
    public bool Left { get; init; }
    public bool Back { get; init; }
    public bool Secondary { get; init; }

    public bool IsUpButtonPressed() => Up;
    public bool IsCenterButtonPressed() => Center;
    public bool IsDownButtonPressed() => Down;
    public bool IsRightButtonPressed() => Right;
    public bool IsLeftButtonPressed() => Left;
    public bool IsBackButtonPressed() => Back;
    public bool IsSecondaryButtonPressed() => Secondary;
    public bool IsAnyButtonPressed() => Up || Center || Down || Right || Left || Back || Secondary;
}
