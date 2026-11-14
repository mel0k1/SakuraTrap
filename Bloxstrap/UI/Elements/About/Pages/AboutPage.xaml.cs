using Bloxstrap.UI.ViewModels.About;

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

using Path = System.Windows.Shapes.Path;

namespace Bloxstrap.UI.Elements.About.Pages
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage
    {
        private class Petal
        {
            public Path Shape = null!;

            public double BaseX;

            public double Y;

            public double Speed;

            public double SwaySpeed;

            public double SwayAmplitude;

            public double Phase;

            public double SpinSpeed;
        }

        private const string PetalGeometry = "M 8,0 C 13,4 15,10 12,16 C 10,19 6,19 4,16 C 1,10 3,4 8,0 Z";

        private const int PetalCount = 14;

        private readonly List<Petal> _petals = new();

        private readonly DispatcherTimer _petalTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };

        private readonly Random _random = new();

        private double _petalTime;

        private readonly Queue<Key> _keys = new();

        private readonly List<Key> _expectedKeys = new() { Key.M, Key.A, Key.T, Key.T, Key.LeftShift, Key.D1 };

        private bool _triggered = false;

        public AboutPage()
        {
            DataContext = new AboutViewModel();
            InitializeComponent();

            InitializePetals();
        }

        private void InitializePetals()
        {
            var lightPetal = new SolidColorBrush(Color.FromArgb(120, 0xFF, 0xB7, 0xD5));
            var deepPetal = new SolidColorBrush(Color.FromArgb(90, 0xFF, 0x7F, 0xAC));

            for (int i = 0; i < PetalCount; i++)
            {
                var petal = new Petal
                {
                    Speed = 0.4 + _random.NextDouble() * 0.6,
                    SwaySpeed = 0.02 + _random.NextDouble() * 0.03,
                    SwayAmplitude = 8 + _random.NextDouble() * 14,
                    Phase = _random.NextDouble() * Math.PI * 2,
                    SpinSpeed = (_random.NextDouble() - 0.5) * 1.2
                };

                double size = 0.5 + _random.NextDouble() * 0.7;

                petal.Shape = new Path
                {
                    Data = Geometry.Parse(PetalGeometry),
                    Fill = _random.Next(2) == 0 ? lightPetal : deepPetal,
                    RenderTransform = new TransformGroup
                    {
                        Children =
                        {
                            new ScaleTransform(size, size),
                            new RotateTransform()
                        }
                    }
                };

                petal.Y = -_random.NextDouble() * 160;

                _petals.Add(petal);
                PetalCanvas.Children.Add(petal.Shape);
            }

            PetalCanvas.SizeChanged += (_, _) => RedistributeX();
            _petalTimer.Tick += (_, _) => UpdatePetals();
            Loaded += (_, _) => _petalTimer.Start();
            Unloaded += (_, _) => _petalTimer.Stop();
        }

        private void RedistributeX()
        {
            foreach (var petal in _petals)
                petal.BaseX = _random.NextDouble() * Math.Max(PetalCanvas.ActualWidth - 20, 40);
        }

        private void UpdatePetals()
        {
            _petalTime += 0.033;

            double width = PetalCanvas.ActualWidth;
            double height = PetalCanvas.ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            foreach (var petal in _petals)
            {
                petal.Y += petal.Speed;

                if (petal.Y > height + 24)
                {
                    petal.Y = -24;
                    petal.BaseX = _random.NextDouble() * Math.Max(width - 20, 40);
                }

                double x = petal.BaseX + Math.Sin(_petalTime * petal.SwaySpeed + petal.Phase) * petal.SwayAmplitude;

                Canvas.SetLeft(petal.Shape, x);
                Canvas.SetTop(petal.Shape, petal.Y);

                if (petal.Shape.RenderTransform is TransformGroup group && group.Children[1] is RotateTransform rotate)
                    rotate.Angle += petal.SpinSpeed;
            }
        }

        private void UiPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (_triggered)
                return;

            if (_keys.Count >= 6)
                _keys.Dequeue();

            var key = e.Key;

            if (key == Key.RightShift)
                key = Key.LeftShift;

            _keys.Enqueue(key);

            if (_keys.SequenceEqual(_expectedKeys))
            {
                _triggered = true;
                var storyboard = Resources["EggStoryboard"] as Storyboard;
                storyboard!.Begin();
            }
        }
    }
}
