using Basic_Streaming.NET.ViewModels;
using Basic_Streaming.NET.Models;
using System.Windows.Controls;
using DelsysAPI.Components.TrignoLink;
using System.Windows;
using DelsysAPI.Components;

namespace Basic_Streaming.NET.Views.SensorListItems.LinkSensorListItems
{
    /// <summary>
    /// Interaction logic for SensorListItem.xaml
    /// </summary>
    public partial class BTSensorListItem : UserControl
    {
        private readonly DeviceStreaming _ds;
        public readonly int Sid;

        public BTSensorListItem(SensorTrignoLinkBT sensor, DeviceStreaming ds)
        {
            InitializeComponent();
            _ds = ds;
            Sid = sensor.Sid;
            SensorListItemVM vm = new SensorListItemVM();

            // Instantiate the ViewModel to hold the sensor information
            vm.Sensor = new SensorModel
            {
                SensorName = sensor.FriendlyName,
                SensorId = sensor.Sid
            };
            ListItem.DataContext = vm;
        }

        private void clk_Remove(object sender, RoutedEventArgs e)
        {
            _ds.RemoveLinkSensor(Sid);
            ListBox parent = (ListBox)this.Parent;
            parent.Items.Remove(this);
        }
    }
}
