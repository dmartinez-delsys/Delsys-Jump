using Basic_Streaming.NET.ViewModels;
using Basic_Streaming.NET.Models;
using System.Collections.Generic;
using System.Windows.Controls;
using DelsysAPI.Components.TrignoLink;
using System;
using System.Windows;
namespace Basic_Streaming.NET.Views.SensorListItems.LinkSensorListItems.SpecializedLinkSensorListItems
{
    /// <summary>
    /// Interaction logic for SensorListItem.xaml
    /// </summary>
    public partial class MoxySensorListItem : UserControl
    {

        private readonly DeviceStreaming _ds;
        public readonly int Sid;

        public MoxySensorListItem(SensorTrignoLinkMoxyBT sensor, DeviceStreaming ds)
        {
            InitializeComponent();
            _ds = ds;
            Sid = sensor.Sid;

            // Instantiate the ViewModel to hold the sensor information
            SensorListItemVM slvm = new SensorListItemVM();
            slvm.Sensor = new SensorModel
            {
                SensorName = sensor.FriendlyName,
                SensorId = sensor.StickerNumber
            };

            // Get all the modes for the sensor 
            ModesListVM  mlvm = new ModesListVM();
            mlvm.Modes = new List<Mode>();
            int modeIndex = 0;
            foreach (var mode in Enum.GetValues(typeof(SensorTrignoLinkMoxyBT.UPDATE_TYPE)))
            {
                Mode modeModel = new Mode
                {
                    ModeName = mode.ToString(),
                    ModeIndex = modeIndex
                };

                mlvm.Modes.Add(modeModel);
                modeIndex++;
            }

            // Set the view's data context
            ListItem.DataContext = slvm;
            
            // Set the mode's data context
            ModeList.DataContext = mlvm;
        }

        private void clk_Remove(object sender, RoutedEventArgs e)
        {
            _ds.RemoveLinkSensor(Sid);
            ListBox parent = (ListBox)this.Parent;
            parent.Items.Remove(this);
        }
    }
}
