using Basic_Streaming_.NET.Models;
using Basic_Streaming_.NET.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Basic_Streaming.NET.Views
{
    /// <summary>
    /// Interaction logic for UserMessage.xaml
    /// </summary>
    public partial class UserMessage : UserControl
    {
        public UserMessageModel Model;

        public UserMessage()
        {
            InitializeComponent();
            UserMessageVM vm = new UserMessageVM();
            Model = new UserMessageModel();
            vm.UserMessage = Model;
            Message.DataContext = vm;
        }
    }
}
