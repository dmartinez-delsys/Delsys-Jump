using Basic_Streaming.NET.ViewModels;
using Basic_Streaming.NET.Models;
using System.Collections.Generic;
using System.Windows.Controls;
using System;
using System.Windows;
using DelsysAPI.Components;
using DelsysAPI.Components.TrignoRf;

namespace Basic_Streaming.NET.Views.SensorListItems.RfSensorListItems
{
    /// <summary>
    /// Interaction logic for SensorListItem.xaml
    /// </summary>
    public partial class RfSensorListItem : UserControl
    {
        public readonly int Sid;
        
        public RfSensorListItem(SensorTrignoRf sensor)
        {
            InitializeComponent();
            Sid = sensor.Properties.Sid;

            SensorListItemVM vm = new SensorListItemVM();
            vm.Sensor = new SensorModel
            {
                SensorName = sensor.FriendlyName,
                PairNum = sensor.PairNumber,
                SensorId = sensor.Properties.Sid
            };

            ModesListVM modeVM = new ModesListVM();
            modeVM.Modes = new List<Mode>();
            int modeIndex = 0;
            foreach (var mode in sensor.Configuration.SampleModes)
            {
                Mode modeModel = new Mode
                {
                    ModeName = mode,
                    ModeIndex = modeIndex
                };
                modeVM.Modes.Add(modeModel);
                modeIndex++;
            }

            // Set the DataContexts 
            ListItem.DataContext = vm;
            ModeList.DataContext = modeVM;

            SetCheckStatus(sensor);

            // Get the index of the already configured mode and set it to default
            ModeList.SelectedIndex = Array.IndexOf(sensor.Configuration.SampleModes, sensor.Configuration.ModeString);
        }

        private void SetCheckStatus(Component sensor)
        {
            if (sensor.State == DelsysAPI.Utils.SelectionState.Allocated)
            {
                SelectCheckBox.IsChecked = true;
            }
            else
            {
                SelectCheckBox.IsChecked = false;
            }
        }

        public void clk_SensorListItem(object sender, RoutedEventArgs e)
        {
            SelectCheckBox.IsChecked = !SelectCheckBox.IsChecked;
        }
    }
}
