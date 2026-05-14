using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PerfMonitor.App.Views;

public partial class Sparkline : UserControl
{
    private readonly List<double> _values = [];
    private const int MaxPoints = 60;

    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(Sparkline),
            new PropertyMetadata(new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x89, 0xB4, 0xFA)),
                (d, _) => ((Sparkline)d).Redraw()));

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public void AddValue(double value)
    {
        _values.Add(value);
        while (_values.Count > MaxPoints)
            _values.RemoveAt(0);
        Redraw();
    }

    public void Reset()
    {
        _values.Clear();
        ChartCanvas.Children.Clear();
    }

    private void Redraw()
    {
        ChartCanvas.Children.Clear();

        if (_values.Count < 2) return;

        var width = ChartCanvas.Width;
        var height = ChartCanvas.Height;
        var min = _values.Min();
        var max = _values.Max();
        var range = max - min;

        if (range < 0.001) range = 1; // Avoid divide by zero for flat lines

        var stepX = width / (MaxPoints - 1);

        // Build the path geometry
        var geometry = new PathGeometry();
        var figure = new PathFigure();

        // Start from the oldest point
        var startIndex = _values.Count < MaxPoints ? 0 : 0;
        var xOffset = _values.Count < MaxPoints ? (MaxPoints - _values.Count) * stepX : 0;

        var x = xOffset;
        var y = height - ((_values[0] - min) / range * height);
        figure.StartPoint = new Point(x, Math.Clamp(y, 1, height - 1));

        for (int i = 1; i < _values.Count; i++)
        {
            x = xOffset + i * stepX;
            y = height - ((_values[i] - min) / range * height);
            figure.Segments.Add(new LineSegment(
                new Point(x, Math.Clamp(y, 1, height - 1)),
                isStroked: true));
        }

        geometry.Figures.Add(figure);

        var path = new Path
        {
            Stroke = Stroke,
            StrokeThickness = 1.2,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Data = geometry,
            SnapsToDevicePixels = true
        };

        ChartCanvas.Children.Add(path);
    }
}
