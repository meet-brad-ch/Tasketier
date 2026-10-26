using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Tasketier.App;

/// <summary>One seven-segment LED digit, drawn from polygons, as on a bedside alarm clock.</summary>
[SupportedOSPlatform("windows")]
public sealed class SevenSegmentDigit : Canvas
{
    // Which segments (a top, b upper right, c lower right, d bottom, e lower left, f upper left, g middle) each digit lights.
    private static readonly string[] Lit = ["abcdef", "bc", "abdeg", "abcdg", "bcfg", "acdfg", "acdefg", "abc", "abcdefg", "abcdfg"];

    private readonly Dictionary<char, Polygon> _segments = new();

    /// <summary>Draws the unlit digit at the given size.</summary>
    /// <param name="width">The digit width; the height is twice that.</param>
    /// <param name="lit">The colour of a lit segment.</param>
    /// <param name="unlit">The colour of an unlit segment, the faint ghost a real display shows.</param>
    public SevenSegmentDigit(double width, Brush lit, Brush unlit)
    {
        ArgumentNullException.ThrowIfNull(lit);
        ArgumentNullException.ThrowIfNull(unlit);
        LitBrush = lit;
        UnlitBrush = unlit;
        Width = width;
        Height = width * 2;
        double t = width * 0.18;   // segment thickness
        double h = width * 2;
        double g = t * 0.25;       // gap between segments
        // Horizontal segments are hexagons; vertical ones too, so the corners meet cleanly.
        Add('a', Horizontal(t, g, 0, width, t));
        Add('g', Horizontal(t, g, (h - t) / 2, width, t));
        Add('d', Horizontal(t, g, h - t, width, t));
        Add('f', Vertical(t, g, 0, 0, h / 2));
        Add('b', Vertical(t, g, width - t, 0, h / 2));
        Add('e', Vertical(t, g, 0, h / 2, h / 2));
        Add('c', Vertical(t, g, width - t, h / 2, h / 2));
    }

    /// <summary>The colour of a lit segment.</summary>
    public Brush LitBrush { get; }

    /// <summary>The colour of an unlit segment.</summary>
    public Brush UnlitBrush { get; }

    /// <summary>Shows a digit, or nothing lit for a space.</summary>
    /// <param name="digit">'0' to '9', or ' '.</param>
    public void Show(char digit)
    {
        string on = digit is >= '0' and <= '9' ? Lit[digit - '0'] : string.Empty;
        foreach ((char name, Polygon segment) in _segments)
        {
            segment.Fill = on.Contains(name, StringComparison.Ordinal) ? LitBrush : UnlitBrush;
        }
    }

    private void Add(char name, Polygon segment)
    {
        segment.Fill = UnlitBrush;
        _segments[name] = segment;
        Children.Add(segment);
    }

    private static Polygon Horizontal(double t, double gap, double top, double width, double thickness)
    {
        double half = thickness / 2;
        return new Polygon
        {
            Points =
            [
                new Point(gap + half, top + half), new Point(gap + thickness, top), new Point(width - gap - thickness, top),
                new Point(width - gap - half, top + half), new Point(width - gap - thickness, top + thickness), new Point(gap + thickness, top + thickness),
            ],
        };
    }

    private static Polygon Vertical(double t, double gap, double left, double top, double height)
    {
        double half = t / 2;
        return new Polygon
        {
            Points =
            [
                new Point(left + half, top + gap + half), new Point(left + t, top + gap + t), new Point(left + t, top + height - gap - t),
                new Point(left + half, top + height - gap - half), new Point(left, top + height - gap - t), new Point(left, top + gap + t),
            ],
        };
    }
}
