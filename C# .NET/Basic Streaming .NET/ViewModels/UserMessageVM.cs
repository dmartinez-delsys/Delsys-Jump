using Basic_Streaming_.NET.Models;

namespace Basic_Streaming_.NET.ViewModels
{
    /// <summary>
    /// View Model for representing a message for the user in the UI
    /// </summary>
    public class UserMessageVM
    {
        public UserMessageModel UserMessage { get; set; }

        public UserMessageVM()
        {
            UserMessage = new UserMessageModel();
        }
    }
}
