using System;
using System.Globalization;
using System.IO.Ports;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace RotaryMonitor
{
    public sealed partial class SensorWindow : Window
    {
        private readonly string _port;
        private volatile bool _closing;
        private Thread? _thread;

        private bool _hasData;
        private double _angle;
        private float _ax, _ay, _az;
        private RotateTransform _needleRot;

        public SensorWindow(string port)
        {
            _port = port;
            InitializeComponent();
            Title = L.Get("SensorTitle.Title");
            NoteText.Text = L.Get("SensorNote.Text");
            BuildDial();
            StatusText.Text = string.Format(L.Get("SensorConnecting"), port);
            Closed += (s, e) => { _closing = true; };
            _thread = new Thread(Loop) { IsBackground = true };
            _thread.Start();
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

        private void Loop()
        {
            while (!_closing)
            {
                try
                {
                    using (var sp = new SerialPort(_port, 115200))
                    {
                        sp.ReadTimeout = 2000;
                        sp.Open();
                        SetStatus(string.Format(L.Get("SensorConnected"), _port), true);
                        while (!_closing)
                        {
                            string line;
                            try { line = sp.ReadLine(); }
                            catch (TimeoutException) { continue; }
                            if (line == null) continue;
                            Parse(line.Trim());
                        }
                    }
                }
                catch (Exception ex)
                {
                    SetStatus(string.Format(L.Get("SensorError"), ex.Message), false);
                }
                for (int i = 0; i < 10 && !_closing; i++)
                    Thread.Sleep(500);
            }
            SetStatus(L.Get("SensorClosed"), false);
        }

        private void Parse(string line)
        {
            if (!line.StartsWith("A="))
                return;
            string[] p = line.Substring(2).Split(' ');
            double a; float x, y, z;
            if (p.Length >= 4 && double.TryParse(p[0], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out a)
                && float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                && float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                && float.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
            {
                _angle = a; _ax = x; _ay = y; _az = z; _hasData = true;
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            DispatcherQueue.TryEnqueue(delegate
            {
                if (!_hasData)
                    return;
                AngleText.Text = _angle.ToString("0.0", CultureInfo.InvariantCulture) + "\u00B0";
                RawText.Text = "ax=" + _ax.ToString("0.00", CultureInfo.InvariantCulture)
                    + "   ay=" + _ay.ToString("0.00", CultureInfo.InvariantCulture)
                    + "   az=" + _az.ToString("0.00", CultureInfo.InvariantCulture);

                string st;
                if (_angle >= -30 && _angle <= 30) st = L.Get("SensorLandscape");
                else if (_angle >= 60 && _angle <= 120) st = L.Get("SensorPortrait90");
                else if (_angle >= 150 || _angle <= -150) st = L.Get("SensorFlipped");
                else if (_angle >= -120 && _angle <= -60) st = L.Get("SensorPortrait270");
                else st = L.Get("SensorBetween");
                StateText.Text = st;
                _needleRot.Angle = _angle;
            });
        }

        private void SetStatus(string text, bool connected)
        {
            DispatcherQueue.TryEnqueue(delegate
            {
                StatusText.Text = text;
                StatusText.Foreground = connected
                    ? new SolidColorBrush(UiColors.ForestGreen)
                    : new SolidColorBrush(UiColors.DarkRed);
            });
        }
    }
}
