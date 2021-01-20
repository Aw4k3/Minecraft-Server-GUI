using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Minecraft_Server_GUI
{
    /// <summary>
    /// Interaction logic for ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        public ColorPicker()
        {
            InitializeComponent();
        }

        //HSV TO RGB CONVERTER

        void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = ClampRGB((int)(R * 255.0));
            g = ClampRGB((int)(G * 255.0));
            b = ClampRGB((int)(B * 255.0));
        }

        int ClampRGB(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }

        //USERCONTROL SETUP

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Color", typeof(Color), typeof(ColorPicker), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnValueChanged), new CoerceValueCallback(CoerceValue)));

        public Color Value
        {
            get { return (Color)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private static object CoerceValue(DependencyObject element, object value)
        {
            Color newValue = (Color)value;
            ColorPicker control = (ColorPicker)element;

            return newValue;
        }

        private static void OnValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ColorPicker control = (ColorPicker)obj;

            RoutedPropertyChangedEventArgs<Color> e = new RoutedPropertyChangedEventArgs<Color>(
                (Color)args.OldValue, (Color)args.NewValue, ValueChangedEvent);
            control.OnValueChanged(e);
        }

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<Color>), typeof(ColorPicker));

        public event RoutedPropertyChangedEventHandler<Color> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        private void OnValueChanged(RoutedPropertyChangedEventArgs<Color> e) => RaiseEvent(e);

        //MAIN CODE

        double H, S, V;
        int R, G, B;

        private int Clamp(int value, int min, int max)
        {
            if (value > max)
            {
                return max;
            }
            else if (value < min)
            {
                return min;
            }
            else
            {
                return value;
            }
        }

        private void HueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            HueValue.Value = (decimal)HueSlider.Value;
            H = HueSlider.Value;
            UpdateHSVSliders();
        }

        private void SatSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SatValue.Value = (decimal)SatSlider.Value;
            S = SatSlider.Value;
            UpdateHSVSliders();
        }

        private void ValSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
           ValValue.Value = (decimal)ValSlider.Value;
           V = ValSlider.Value;
           UpdateHSVSliders();
        }

        private void HueValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            HueValue.Value = Clamp((int)HueValue.Value, 0, 360);
            HueSlider.Value = Clamp((int)HueValue.Value, 0, 360);
            H = Clamp((int)HueValue.Value, 0, 360);
            UpdateHSVSliders();
        }

        private void SatValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            SatValue.Value = Clamp((int)SatValue.Value, 0, 100);
            SatSlider.Value = Clamp((int)SatValue.Value, 0, 100);
            S = Clamp((int)SatValue.Value, 0, 100);
            UpdateHSVSliders();
        }

        private void ValValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            ValValue.Value = Clamp((int)ValValue.Value, 0, 100);
            ValSlider.Value = Clamp((int)ValValue.Value, 0, 100);
            V = Clamp((int)ValValue.Value, 0, 100);
            UpdateHSVSliders();
        }

        private void UpdateHSVSliders()
        {
            HsvToRgb(H, S, V, out R, out G, out B);

            //Setup Saturation Slider
            LinearGradientBrush SatSliderBrush = new LinearGradientBrush();
            SatSliderBrush.StartPoint = new Point(0, 0.5);
            SatSliderBrush.EndPoint = new Point(1, 0.5);
            SatSliderBrush.GradientStops.Add(new GradientStop(Color.FromRgb((byte)(V / 100 * 255), (byte)(V / 100 * 255), (byte)(V / 100 * 255)), 0));
            SatSliderBrush.GradientStops.Add(new GradientStop(Color.FromRgb((byte)(V / 100 * R), (byte)(V / 100 * G), (byte)(V / 100 * B)), 1));

            //Setup Value Slider
            LinearGradientBrush ValSliderBrush = new LinearGradientBrush();
            ValSliderBrush.StartPoint = new Point(0, 0.5);
            ValSliderBrush.EndPoint = new Point(1, 0.5);
            ValSliderBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 0), 0));
            ValSliderBrush.GradientStops.Add(new GradientStop(Color.FromRgb((byte)(S / 100 * R), (byte)(S / 100 * G), (byte)(S / 100 * B)), 1));

            //Apply Gradient Brushes
            SatSlider.Background = SatSliderBrush;
            ValSlider.Background = ValSliderBrush;
        }
    }
}
