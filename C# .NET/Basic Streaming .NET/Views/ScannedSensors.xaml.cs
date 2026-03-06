using Basic_Streaming.NET.Views.SensorListItems.LinkSensorListItems;
using Basic_Streaming.NET.Views.SensorListItems.LinkSensorListItems.SpecializedLinkSensorListItems;
using Basic_Streaming.NET.Views.SensorListItems.RfSensorListItems;
using DelsysAPI.Components.TrignoLink;
using DelsysAPI.Components.TrignoRf;
using DelsysAPI.Contracts;
using DelsysAPI.Pipelines;
using DelsysAPI.Utils;
using MathNet.Numerics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Basic_Streaming.NET.DeviceStreaming;

namespace Basic_Streaming.NET.Views
{
    /// <summary>
    /// Interaction logic for ScannedSensors.xaml
    /// </summary>
    public partial class ScannedSensors : UserControl
    {
        private Pipeline _pipeline;
        private DeviceStreaming _uc;
        public readonly LoadingIcon _loadingIconRf;
        public readonly LoadingIcon _loadingIconLink;

        public ScannedSensors(DeviceStreaming uc, Pipeline pipeline, bool link)
        {
            InitializeComponent();
            _uc = uc;
            _pipeline = pipeline;
            _loadingIconRf = new LoadingIcon("Scanning for all connected Rf sensors...");
            MainPanel.Children.Add(_loadingIconRf);

            if (link)
            {
                _loadingIconLink = new LoadingIcon("Scanning for new Link sensors...");
                MainPanel.Children.Add(_loadingIconLink);
                Grid.SetColumn(_loadingIconLink, 1);
            }
            else
            {
                // Remove Link from UI
                LabelPanel.Children.Remove(LabelPanel.Children[1]);
                MainPanel.Children.Remove(MainPanel.Children[1]);

                _loadingIconRf.LoadingIconSensorList.Width = _loadingIconRf.LoadingIconSensorList.Width * 2;
                Grid.SetColumn(_loadingIconRf, 0);
                Grid.SetColumnSpan(_loadingIconRf, 2);
                _loadingIconRf.SetMessageColumnSpan(2);
                RfLabel.Width = RfLabel.Width * 2;
                ScannedRfSensorsList.Width = ScannedRfSensorsList.Width * 2;
            }

            // Remove trigger capability if Lite is connected
            if (_uc.IsLiteConnected())
            {
                TriggerPanelBorder.ToolTip = "Triggers not available for Lite";
                TriggerPanel.IsEnabled = false;
            }
        }

        public void ScanComplete(List<SensorTrignoRf> rfs)
        {
            // Remove all existing sensors
            foreach (var item in ScannedRfSensorsList.Items)
            {
                ScannedRfSensorsList.Items.Remove(item);
            }

            MainPanel.Children.Remove(_loadingIconRf);
            ScannedRfSensorsList.Visibility = Visibility.Visible;

            foreach (SensorTrignoRf sensor in rfs)
            {
                RfSensorListItem item = new RfSensorListItem(sensor);
                ScannedRfSensorsList.Items.Add(item);
                if (!_uc.IsLinkConnected())
                {
                    item.ListItem.Width = item.ListItem.Width * 2;
                }
            }
        }

        public void ScanComplete(List<SensorTrignoRf> rfs, List<SensorTrignoLink> links)
        {
            ScanComplete(rfs);
            // Remove all existing sensors in the UI
            foreach (var item in ScannedLinkSensorsList.Items)
            {
                ScannedLinkSensorsList.Items.Remove(item);
            }
            ScannedLinkSensorsList.Visibility = Visibility.Visible;
            MainPanel.Children.Remove(_loadingIconLink);

            Debug.WriteLine("Scanned Links found:");
            foreach (var sensor in links)
            {
                Debug.WriteLine(" - " + sensor.FriendlyName);
                // Add UI element representing sensor depending on its type
                // See comments at the top of each .xaml file for difference in UI representations of sensors
                switch (sensor)
                {
                    case SensorTrignoLinkANT antComp:
                        ScannedLinkSensorsList.Items.Add(new ANTSensorListItem(antComp, _uc));
                        break;
                    case SensorTrignoLinkVO2 vO2Comp:
                        ScannedLinkSensorsList.Items.Add(new VO2SensorListItem(vO2Comp, _uc));
                        break;
                    case SensorTrignoLinkMoxyBT bleMoxyComp:
                        ScannedLinkSensorsList.Items.Add(new MoxySensorListItem(bleMoxyComp, _uc));
                        break;
                    case SensorTrignoLinkBT bleComp:
                        ScannedLinkSensorsList.Items.Add(new BTSensorListItem(bleComp, _uc));
                        break;
                    default:
                        // Should never reach here
                        break;
                }
            }
        }

        public void NoRfSensorsDetected()
        {
            Dispatcher.Invoke(() =>
            {
                _loadingIconRf.JustShowMessage("No Rf sensors detected, please try again.");
                if (_uc.IsLinkConnected())
                {
                    _loadingIconLink.JustShowMessage("Please connect a Rf sensor to use Link.");
                }
            });
        }

        public async void clk_ArmPipeline(object sender, RoutedEventArgs e)
        {
            _uc.UserMessage.Visibility = Visibility.Hidden;

            // To arm the pipeline:
            // 1) Select/deselect desired sensors
            // 2) Set active data sources (Rf, Link)
            // 3) Configure pipeline 


            if (!SetSensorSelections())
            {
                return;
            }

            _uc.UserMessage.Model.Message = "Arming...";
            _uc.UserMessage.Visibility = Visibility.Visible;
            await Task.Delay(2); // Allows message to show

            ConfigureAndUpdateUI();
        }

        /// <summary>
        /// Selects and deselects sensors according to their checkbox selection status.
        /// </summary>
        /// <returns>
        /// Returns true if selection was successful and false otherwise (ex: no Rf sensors selected)
        /// </returns>
        private bool SetSensorSelections()
        {
            SelectRfSensors();

            // Return false if no rf sensors in that list are selected
            int selectedRfCount = _pipeline.TrignoRfManager.Components.Count(x => x.State == SelectionState.Allocated);
            if (selectedRfCount <= 0)
            {
                _uc.UserMessage.Model.Message = "Please connect and select at least one Rf Sensor.";
                _uc.UserMessage.Visibility = Visibility.Visible;
                return false;
            }

            SelectLinkSensors();

            return true;
        }

        private void SelectRfSensors()
        {
            foreach (RfSensorListItem item in ScannedRfSensorsList.Items)
            {
                bool selected = (bool)item.SelectCheckBox.IsChecked;
                ConfigureSensorSelectionRf(item.Sid, selected);
                if (selected)
                {
                    // Get index of selected mode in combobox
                    int modeIndex = item.ModeList.SelectedIndex;
                    // Use that index to set sensor mode 
                    SensorTrignoRf comp = _pipeline.TrignoRfManager.Components.Where(x => x.Properties.Sid == item.Sid).FirstOrDefault();
                    comp.SelectSampleMode(comp.Configuration.SampleModes[modeIndex]);
                }
            }
        }

        private void SelectLinkSensors()
        {
            foreach (var SensorListItem in ScannedLinkSensorsList.Items)
            {
                bool selected;
                int modeIndex;
                switch (SensorListItem)
                {
                    case MoxySensorListItem moxySLI:
                        ConfigureSensorSelectionLink(moxySLI.Sid, true);
                        // Get index of selected mode in combobox
                        modeIndex = moxySLI.ModeList.SelectedIndex;
                        // Use that index to set sensor mode 
                        SensorTrignoLinkMoxyBT moxyComp = (SensorTrignoLinkMoxyBT)_pipeline.TrignoLinkManager.Components.Where(x => x.Sid == moxySLI.Sid).FirstOrDefault();
                        moxyComp.UpdateType = (SensorTrignoLinkMoxyBT.UPDATE_TYPE)modeIndex;
                        break;
                    case VO2SensorListItem vo2SLI:
                        ConfigureSensorSelectionLink(vo2SLI.Sid, true);
                        // Get index of selected modes in combobox
                        int calVolIndex = vo2SLI.ModeListCalVol.SelectedIndex;
                        int maskSizeIndex = vo2SLI.ModeListMaskSize.SelectedIndex;
                        int ventSizeIndex = vo2SLI.ModeListVentSize.SelectedIndex;
                        string weightStr = vo2SLI.ModeListWeight.Text;

                        // Use that index to set sensor mode 
                        SensorTrignoLinkVO2 vo2Comp = (SensorTrignoLinkVO2)_pipeline.TrignoLinkManager.Components.Where(x => x.Sid == vo2SLI.Sid).FirstOrDefault();
                        vo2Comp.CalibrationVolume = (SensorTrignoLinkVO2.CALIBRATION_VOLUME)calVolIndex;
                        vo2Comp.MaskSize = (SensorTrignoLinkVO2.MASK_SIZE)maskSizeIndex;
                        vo2Comp.VentSize = (SensorTrignoLinkVO2.VENT_SIZE)ventSizeIndex;
                        vo2Comp.Weight = float.Parse(weightStr);
                        break;
                    case BTSensorListItem btSLI:
                        ConfigureSensorSelectionLink(btSLI.Sid, true);
                        break;
                    case ANTSensorListItem antSLI:
                        ConfigureSensorSelectionLink(antSLI.Sid, true);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Sets a Link sensor to desired selected/deselected if not already
        /// </summary>
        /// <param name="sid"> Sensor ID to set selection</param>
        /// <param name="selected">Whether the sensor should be selected or not</param>
        private void ConfigureSensorSelectionLink(int sid, bool selected)
        {
            SensorTrignoLink comp = _pipeline.TrignoLinkManager.Components.Where(x => x.Sid == sid).FirstOrDefault();
            if (comp == null)
            {
                Debug.WriteLine("Null");
            }

            // If we want to select the sensor and the sensor is not selected
            if (selected && comp.State != SelectionState.Allocated)
            {
                _pipeline.TrignoLinkManager.SelectComponentAsync(comp).Wait();
            }
            // If we want to deselect the sensor and the sensor is selected
            else if (!selected && comp.State == SelectionState.Allocated)
            {
                _pipeline.TrignoLinkManager.DeselectComponentAsync(comp).Wait();
            }
        }

        /// <summary>
        /// Sets a Rf sensor to desired selected/deselected if not already
        /// </summary>
        /// <param name="sid"> Sensor ID to set selection</param>
        /// <param name="selected">Whether the sensor should be selected or not</param>
        private void ConfigureSensorSelectionRf(int sid, bool selected)
        {
            // Get the sensor from the components
            SensorTrignoRf comp = _pipeline.TrignoRfManager.Components.Where(x => x.Properties.Sid == sid).FirstOrDefault();

            // If we want to select the sensor and the sensor is not selected
            if (selected && comp.State != SelectionState.Allocated)
            {
                _pipeline.TrignoRfManager.SelectComponentAsync(comp).Wait();
            }
            // If we want to deselect the sensor and the sensor is selected
            else if (!selected && comp.State == SelectionState.Allocated)
            {
                _pipeline.TrignoRfManager.DeselectComponentAsync(comp).Wait();
            }
        }

        private async void ConfigureAndUpdateUI()
        {
            _uc.ConfigurePipeline((bool)startTriggerCheckbox.IsChecked, (bool)stopTriggerCheckbox.IsChecked);

            this.Dispatcher.Invoke(() =>
            {
                _uc.btn_PairSensors.IsEnabled = false;
                _uc.btn_ScanSensors.IsEnabled = false;
                _uc.btn_Start.IsEnabled = true;
                _uc.btn_Disarm.IsEnabled = true;
                IsEnabled = false;
                _uc.UserMessage.Visibility = Visibility.Hidden;
            });
        }
    }
}