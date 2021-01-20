using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Minecraft_Server_GUI
{
    /// <summary>
    /// Interaction logic for NumUpDown.xaml
    /// </summary>
    public partial class NumUpDown : UserControl
    {
        public NumUpDown()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(decimal), typeof(NumUpDown), new FrameworkPropertyMetadata(new PropertyChangedCallback(OnValueChanged), new CoerceValueCallback(CoerceValue)));

        public decimal Value
        {
            get { return (decimal)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private static object CoerceValue(DependencyObject element, object value)
        {
            decimal newValue = (decimal)value;
            NumUpDown control = (NumUpDown)element;

            return newValue;
        }

        private static void OnValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            NumUpDown control = (NumUpDown)obj;

            RoutedPropertyChangedEventArgs<decimal> e = new RoutedPropertyChangedEventArgs<decimal>(
                (decimal)args.OldValue, (decimal)args.NewValue, ValueChangedEvent);
            control.OnValueChanged(e);
        }

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<decimal>), typeof(NumUpDown));


        public event RoutedPropertyChangedEventHandler<decimal> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        private void OnValueChanged(RoutedPropertyChangedEventArgs<decimal> e)
        {
            RaiseEvent(e);
            ValueDisplay.Text = Value.ToString();
        }

        private void Increment_Click(object sender, RoutedEventArgs e)
        {
            Value++;
            ValueDisplay.Text = Value.ToString();
        }

        private void Decrement_Click(object sender, RoutedEventArgs e)
        {
            Value--;
            ValueDisplay.Text = Value.ToString();
        }

        private void ValueDisplay_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Regex.IsMatch(ValueDisplay.Text, "/^[0-9]+$/"))
            {
                ValueDisplay.Foreground = new SolidColorBrush(Colors.White);
                Value = Math.Round(Convert.ToDecimal(ValueDisplay.Text));
            }
            else
            {
                //ValueDisplay.Foreground = new SolidColorBrush(Colors.Red);
            }
            
        }

        private void ValueDisplay_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) { ValueDisplay.Text = Value++.ToString(); }
            if (e.Delta < 0) { ValueDisplay.Text = Value--.ToString(); }
        }

        private void ValueDisplay_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up) { ValueDisplay.Text = Value++.ToString(); }
            if (e.Key == Key.Down) { ValueDisplay.Text = Value--.ToString(); }
        }
    }
}
