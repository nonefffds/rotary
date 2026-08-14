using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace RotaryMonitor
{
    /// <summary>Read-only view of the accessory: shows the live angle report.
    /// It shares the main connection (fed by MainWindow's serial reader), so it
    /// never opens its own port or disconnects the app.</summary>
    public sealed partial class SensorWindow : Window
    {
        private bool _hasData;
        private string _port = "";
        private RotateTransform _needleRot;

        public SensorWindow()
        {
            InitializeComponent();
            Title = L.Get("SensorTitle.Title");
            NoteText.Text = L.Get("SensorNote.Text");
            BuildDial();
            StatusText.Text = L.Get("SensorConnecting");
        }

        public void SetPort(string port)
        {
            _port = port ?? "";
            StatusText.Text = string.Format(L.Get("SensorConnected"), _port);
        }

        public void SetConnected(bool connected)
        {
            DispatcherQueue.TryEnqueue(delegate
            {
                StatusText.Text = connected
                    ? string.Format(L.Get("SensorConnected"), _port)
                    : L.Get("SensorClosed");
                StatusText.Foreground = connected
                    ? new SolidColorBrush(UiColors.ForestGreen)
                    : new SolidColorBrush(UiColors.DarkRed);
            });
        }

        public void OnReading(double angle, float ax, float ay, float az)
        {
            DispatcherQueue.TryEnqueue(delegate
            {
                _hasData = true;
                AngleText.Text = angle.ToString("0.0", CultureInfo.InvariantCulture) + "\u00B0";
                RawText.Text = "ax=" + ax.ToString("0.00", CultureInfo.InvariantCulture)
                    + "   ay=" + ay.ToString("0.00", CultureInfo.InvariantCulture)
                    + "   az=" + az.ToString("0.00", CultureInfo.InvariantCulture);

                string st;
                if (angle >= -30 && angle <= 30) st = L.Get("SensorLandscape");
                else if (angle >= 60 && angle <= 120) st = L.Get("SensorPortrait90");
                else if (angle >= 150 || angle <= -150) st = L.Get("SensorFlipped");
                else if (angle >= -120 && angle <= -60) st = L.Get("SensorPortrait270");
                else st = L.Get("SensorBetween");
                StateText.Text = st;
                _needleRot.Angle = angle;
            });
        }

        private void BuildDial()
        {
            const int cx = 120, cy = 120, r = 108;

            var ring = new Ellipse
            {
                Width = r * 2,
                Height = r * 2,
                Stroke = new SolidColorBrush(UiColors.Gray),
                StrokeThickness = 2,
            };
            Canvas.SetLeft(ring, cx - r);
            Canvas.SetTop(ring, cy - r);
            Dial.Children.Add(ring);

            for (int deg = 0; deg < 360; deg += 30)
            {
                bool major = deg % 90 == 0;
                double rad = deg * Math.PI / 180.0;
                int len = major ? 12 : 6;
                var t = new Line
                {
                    X1 = cx + (r - 4) * Math.Sin(rad),
                    Y1 = cy - (r - 4) * Math.Cos(rad),
                    X2 = cx + (r - 4 - len) * Math.Sin(rad),
                    Y2 = cy - (r - 4 - len) * Math.Cos(rad),
                    Stroke = new SolidColorBrush(major ? UiColors.DimGray : UiColors.LightGray),
                    StrokeThickness = 1.5,
                };
                Dial.Children.Add(t);

                if (major)
                {
                    int label = deg == 0 ? 0 : (deg == 180 ? 180 : (deg == 90 ? 90 : 270));
                    var tb = new TextBlock
                    {
                        Text = label.ToString(CultureInfo.InvariantCulture) + "\u00B0",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(UiColors.Gray),
                        HorizontalAlignment = HorizontalAlignment.Center,
                    };
                    tb.Measure(new Size(60, 30));
                    double lx = cx + (r - 32) * Math.Sin(rad) - tb.DesiredSize.Width / 2;
                    double ly = cy - (r - 32) * Math.Cos(rad) - tb.DesiredSize.Height / 2;
                    Canvas.SetLeft(tb, lx);
                    Canvas.SetTop(tb, ly);
                    Dial.Children.Add(tb);
                }
            }

            _needleRot = new RotateTransform { Angle = 0, CenterX = cx, CenterY = cy };
            var needle = new Line
            {
                X1 = cx,
                Y1 = cy,
                X2 = cx,
                Y2 = cy - (r - 26),
                Stroke = new SolidColorBrush(UiColors.Red),
                StrokeThickness = 2.5,
                RenderTransform = _needleRot,
            };
            Dial.Children.Add(needle);

            var hub = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(UiColors.Red),
            };
            Canvas.SetLeft(hub, cx - 4);
            Canvas.SetTop(hub, cy - 4);
            Dial.Children.Add(hub);
        }
    }
}
