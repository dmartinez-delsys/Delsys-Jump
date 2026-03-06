using System.ComponentModel;

namespace Basic_Streaming.NET.Models
{
    /// <summary>
    /// Model for representing sensors in the UI
    /// </summary>
    public class SensorModel : INotifyPropertyChanged
    {
        private string _sensorName;
        public string SensorName
        {
            get { return _sensorName; }
            set
            {
                _sensorName = value;
                RaisePropertyChanged("SensorName");
            }
        }

        private int _sensorId;
        public int SensorId
        {
            get { return _sensorId; }
            set
            {
                _sensorId = value;
                RaisePropertyChanged("SensorId");
            }
        }

        private int _pairNum;
        public int PairNum
        {
            get { return _pairNum; }
            set
            {
                _pairNum = value;
                RaisePropertyChanged("PairNum");
            }
        }

        // Notifies UI when there's been a change
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
