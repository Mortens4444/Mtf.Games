using Mtf.Drawing;
using System.Drawing;

namespace Mtf.Games.Interfaces;

/// <summary>
/// Optional capability of a game context: the size a text takes on the screen.
/// The menu uses it to center texts and stays with a rough estimate when the context does not implement it.
/// </summary>
public interface ITextMeasureContext
{
    SizeF MeasureText(string text, FontType fontType);
}
