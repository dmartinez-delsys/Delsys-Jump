using Basic_Streaming.NET.ViewModels;
using Basic_Streaming.NET.Models;
using System.Collections.Generic;
using System.Windows.Controls;
using DelsysAPI.Components.TrignoLink;
using System;
using System.Windows;
using DelsysAPI.Components;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace Basic_Streaming.NET.Views.SensorListItems.LinkSensorListItems.SpecializedLinkSensorListItems
{
    /// <summary>
    /// Interaction logic for SensorListItem.xaml
    /// </summary>
    public partial class VO2SensorListItem : UserControl
    {
        private readonly DeviceStreaming _ds;
        public readonly int Sid;

        public VO2SensorListItem(SensorTrignoLinkVO2 sensor, DeviceStreaming ds)
        {
            InitializeComponent();
            _ds = ds;
            Sid = sensor.Sid;

            // Instantiate the ViewModel to hold the sensor information
            SensorListItemVM slvm = new SensorListItemVM();
            slvm.Sensor = new SensorModel
            {
                SensorName = sensor.FriendlyName,
                SensorId = sensor.Sid
            };

            // Get all the modes for the sensor 
            // Calibration Volume
            ModesListVM calVolModes = new ModesListVM();
            calVolModes.Modes = new List<Mode>();
            int modeIndex = 0;
            foreach (var mode in Enum.GetValues(typeof(SensorTrignoLinkVO2.CALIBRATION_VOLUME)))
            {
                Mode modeModel = new Mode
                {
                    ModeName = mode.ToString(),
                    ModeIndex = modeIndex
                };
                calVolModes.Modes.Add(modeModel);
                modeIndex++;
            }

            // Mask Size
            ModesListVM maskSizeModes = new ModesListVM();
            maskSizeModes.Modes = new List<Mode>();
            modeIndex = 0;
            foreach (var mode in Enum.GetValues(typeof(SensorTrignoLinkVO2.MASK_SIZE)))
            {
                Mode modeModel = new Mode
                {
                    ModeName = mode.ToString(),
                    ModeIndex = modeIndex
                };
                maskSizeModes.Modes.Add(modeModel);
                modeIndex++;
            }

            // Vent Size
            ModesListVM ventSizeModes = new ModesListVM();
            ventSizeModes.Modes = new List<Mode>();
            modeIndex = 0;
            foreach (var mode in Enum.GetValues(typeof(SensorTrignoLinkVO2.VENT_SIZE)))
            {
                Mode modeModel = new Mode
                {
                    ModeName = mode.ToString(),
                    ModeIndex = modeIndex
                };
                ventSizeModes.Modes.Add(modeModel);
                modeIndex++;
            }

            // Set the data contexts
            ListItem.DataContext = slvm;
            ModeListCalVol.DataContext = calVolModes;
            ModeListMaskSize.DataContext = maskSizeModes;
            ModeListVentSize.DataContext = ventSizeModes;
        }

        private void ValidateTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex reg = new Regex(@"^[0-9]*$");
            e.Handled = !reg.IsMatch(e.Text);
        }

        private void clk_Remove(object sender, RoutedEventArgs e)
        {
            _ds.RemoveLinkSensor(Sid);
            ListBox parent = (ListBox)this.Parent;
            parent.Items.Remove(this);
        }
    }
}
