using Basic_Streaming_.NET.Views;
using Basic_Streaming_.NET.Views.SensorTypes;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static DelsysAPI.Components.TrignoLink.SensorTrignoLinkANT;
using static DelsysAPI.Components.TrignoLink.SensorTrignoLinkBT;

namespace Basic_Streaming.NET.Views
{
    /// <summary>
    /// Interaction logic for SensorTypesPopup.xaml
    /// </summary>
    public partial class SensorTypesPopup : UserControl
    {
        private readonly DeviceStreaming _ds;

        public SensorTypesPopup(DeviceStreaming deviceStreaming, BT_DEVICE_TYPE[] devicesBT)
        {
            InitializeComponent();
            List<BT_DEVICE_TYPE> selectedTypesBT = new List<BT_DEVICE_TYPE>();
            _ds = deviceStreaming;
            int height = (50 * devicesBT.Length) + 150;
            this.Height = height;
            SelectTypesGrid.Height = height;

            // Add all the device options
            int gridRowInd = 1;
            foreach (BT_DEVICE_TYPE type in devicesBT)
            {
                // Designate a row
                SelectTypesGrid.RowDefinitions.Add(GenerateRow());

                // Add a toggle button
                SensorTypesButton btn = new SensorTypesButton(type.ToString());
                btn.Button.Checked += (object sender, RoutedEventArgs e) =>
                {
                    selectedTypesBT.Add(type);
                };
                btn.Button.Unchecked += (object sender, RoutedEventArgs e) =>
                {
                    selectedTypesBT.Remove(type);
                };
                Grid.SetRow(btn, gridRowInd++);
                SelectTypesGrid.Children.Add(btn);
            }

            // Add the footer (Scan and Go Back buttons)
            SensorTypesFooter footer = new SensorTypesFooter();
            footer.btn_Scan.Click += (object sender, RoutedEventArgs e) =>
            {
                if (selectedTypesBT.Count > 0)
                {
                    (this.Parent as Grid).Children.Remove(this);
                    _ds.LoadDevicesBT(selectedTypesBT.ToArray());
                }
                else
                {
                    DisplayNoneSelected();
                }
            };
            footer.btn_GoBack.Click += clk_GoBack;
            SelectTypesGrid.RowDefinitions.Add(GenerateRow(100));
            Grid.SetRow(footer, gridRowInd++);
            SelectTypesGrid.Children.Add(footer);
        }

        public SensorTypesPopup(DeviceStreaming deviceStreaming, ANT_DEVICE_TYPE[] devicesANT)
        {
            InitializeComponent();
            List<ANT_DEVICE_TYPE> selectedTypesANT = new List<ANT_DEVICE_TYPE>();
            _ds = deviceStreaming;
            int height = (50 * devicesANT.Length) + 150;
            this.Height = height;
            SelectTypesGrid.Height = height;

            // Add all the device options
            int gridRowInd = 1;
            foreach (ANT_DEVICE_TYPE type in devicesANT)
            {
                // Designate a row
                SelectTypesGrid.RowDefinitions.Add(GenerateRow());

                // Add a toggle button
                SensorTypesButton btn = new SensorTypesButton(type.ToString());
                btn.Button.Checked += (object sender, RoutedEventArgs e) =>
                {
                    selectedTypesANT.Add(type);
                };
                btn.Button.Unchecked += (object sender, RoutedEventArgs e) =>
                {
                    selectedTypesANT.Remove(type);
                };
                Grid.SetRow(btn, gridRowInd++);
                SelectTypesGrid.Children.Add(btn);
            }

            // Add the footer (Scan and Go Back buttons)
            SensorTypesFooter footer = new SensorTypesFooter();
            footer.btn_Scan.Click += (object sender, RoutedEventArgs e) =>
            {
                if (selectedTypesANT.Count > 0)
                {
                    (this.Parent as Grid).Children.Remove(this);
                    _ds.LoadDevicesANT(selectedTypesANT.ToArray());
                } else
                {
                    DisplayNoneSelected();
                }
            };
            footer.btn_GoBack.Click += clk_GoBack;
            SelectTypesGrid.RowDefinitions.Add(GenerateRow(100));
            Grid.SetRow(footer, gridRowInd++);
            SelectTypesGrid.Children.Add(footer);
        }

        private RowDefinition GenerateRow(int height = 50)
        {
            RowDefinition row = new RowDefinition();
            row.Height = new GridLength(height);
            return row;
        }

        private void DisplayNoneSelected()
        {
            _ds.UserMessage.Model.Message = "Please select at least one sensor type";
            _ds.UserMessage.Visibility = Visibility.Visible;
        }

        private void clk_GoBack(object sender, RoutedEventArgs e)
        {
            (this.Parent as Grid).Children.Remove(this);
            _ds.btn_PairSensors.IsEnabled = true;
            _ds.btn_ScanSensors.IsEnabled = true;
            _ds.UserMessage.Visibility = Visibility.Hidden;
        }
    }
}
