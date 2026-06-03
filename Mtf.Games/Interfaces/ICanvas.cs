using Mtf.Drawing;
using Mtf.Drawing.Interfaces;
using System.Drawing;

namespace Mtf.Games.Interfaces;

public interface ICanvas
{
    int ScreenWidth { get; }

    int ScreenHeight { get; }
    
    int HorizontalCenter { get; }
    
    int VerticalCenter { get; }

    void Clear();

    void Update();

    void Draw(IPrimitive primitive, Color color);

    void ShowOnMiddleOfScreen(string text, FontType fontType, int v2);
}