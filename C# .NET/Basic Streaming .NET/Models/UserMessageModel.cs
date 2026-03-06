using System.ComponentModel;

namespace Basic_Streaming_.NET.Models
{
    /// <summary>
    /// Model for representing a message for the user in the UI
    /// </summary>
    public class UserMessageModel : INotifyPropertyChanged
    {
        private string _message;
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                RaisePropertyChanged("Message");
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
