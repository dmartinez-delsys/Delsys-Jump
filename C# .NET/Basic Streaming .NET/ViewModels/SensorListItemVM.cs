using Basic_Streaming.NET.Models;

namespace Basic_Streaming.NET.ViewModels
{
    /// <summary>
    /// View Model for representing sensors in the UI
    /// </summary>
    public class SensorListItemVM
    {
        public SensorModel Sensor { get; set; }

        public SensorListItemVM() { }
    }
}
