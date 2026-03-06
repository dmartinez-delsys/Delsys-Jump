using Basic_Streaming.NET.Models;

namespace Basic_Streaming.NET.ViewModels
{
    /// <summary>
    /// View Model for representing sensor stream information in the UI
    /// </summary>
    public class StreamInfoVM
    {
        public StreamInfoModel StreamInfo { get; set; }

        public StreamInfoVM() { }
    }
}
