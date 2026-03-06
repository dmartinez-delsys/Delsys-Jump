using System.Windows.Controls;

namespace Basic_Streaming_.NET.Views
{
    /// <summary>
    /// Interaction logic for SensorTypesButton.xaml
    /// </summary>
    public partial class SensorTypesButton : UserControl
    {
        public SensorTypesButton(string content)
        {
            InitializeComponent();
            Button.Content = content;
        }
    }
}
