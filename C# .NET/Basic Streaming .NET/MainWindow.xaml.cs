using DelsysAPI.Events;
using DelsysAPI.Pipelines;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;


namespace Basic_Streaming.NET
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DeviceStreaming _deviceSteamingUC;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void clk_DeviceStreaming(object sender, RoutedEventArgs e)
        {
            MainPanel.Children.Clear();
            _deviceSteamingUC = new DeviceStreaming(this);
            MainPanel.Children.Add(_deviceSteamingUC);
        }

        public void clk_BackButton(object sender, RoutedEventArgs e)
        {
            CloseAllPipelines();
            MainPanel.Children.Clear();
            MainPanel.Children.Add(MainWindowBorder);
        }

        private async void CloseAllPipelines()
        {
            Debug.WriteLine("Simulating Stream # of pipeline Ids before removal: " + PipelineController.Instance.PipelineIds.Count);
            if (PipelineController.Instance.PipelineIds.Count > 0)
            {
                await ShutdownSequence();
            }
            Debug.WriteLine("Simulating Stream # of pipeline Ids after removal: " + PipelineController.Instance.PipelineIds.Count);
        }

        private async Task ShutdownSequence()
        {
            // Running -> Finished -> Armed -> Connected -> Off
            Debug.WriteLine("Shutdown sequence");
            Pipeline pipeline = PipelineController.Instance.PipelineIds[0];

            // Callback for stopping the pipeline to automatically continue shutdown process after stream has been stopped
            pipeline.CollectionComplete += async (object sender, CollectionCompleteEvent e) =>
            {
                DisarmAndRemovePipeline(pipeline);
            };

            if (pipeline.CurrentState == Pipeline.ProcessState.Running)
            {
                Debug.WriteLine("Stopping stream");
                await pipeline.Stop();
            } else if (pipeline.CurrentState == Pipeline.ProcessState.Armed)
            {
                DisarmAndRemovePipeline(pipeline);
            } else if (pipeline.CurrentState  != Pipeline.ProcessState.Finished)
            {
                PipelineController.Instance.RemovePipeline(0);
            }
        }

        private async void DisarmAndRemovePipeline(Pipeline pipeline)
        {
            Debug.WriteLine("Disarming pipeline");
            await pipeline.DisarmPipeline();
            Debug.WriteLine("Removing pipeline");
            PipelineController.Instance.RemovePipeline(0);
        }
    }
}
