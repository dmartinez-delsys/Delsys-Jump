using Aero.PipeLine;
using Aero.TrignoComms;
using Basic_Streaming.NET.Views;
using DelsysAPI.Components.TrignoLink;
using DelsysAPI.Components.TrignoRf;
using DelsysAPI.DataConverters;
using DelsysAPI.DelsysDevices;
using DelsysAPI.Events;
using DelsysAPI.Exceptions;
using DelsysAPI.Pipelines;
using DelsysAPI.Utils;
using DelsysAPI.Utils.Trigno;
using DelsysAPI.Utils.TrignoConfiguration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Basic_Streaming.NET
{
    /// <summary>
    /// Interaction logic for DeviceStreamUserControl.xaml
    /// </summary>
    public partial class DeviceStreaming : UserControl
    {
        // Add your API key & license here 
        string key = "";
        string license = "";

        public List<SensorTrignoRf> SelectedSensors;

        // Pipeline fields
        private IDelsysDevice _deviceSource;
        private Pipeline _pipeline;

        // Holds collection data throughout the stream
        private List<List<double>> _data;

        // Metadata fields
        private int _totalFrames;
        private int _totalLostPackets;
        private int _frameThroughput;
        private double _packetInterval;
        private double _streamTime = 0.0;

        // User Controls
        private StreamInfo _streamInfo;
        private PairSensor _pairSensor;
        private ScannedSensors _scannedSensors;
        public UserMessage UserMessage;

        private bool _startTriggerEnabled, _stopTriggerEnabled, _waitingForStartTrigger, _waitingForStopTrigger;

        // Tracks scanning callbacks
        private int _scanCallbacksReceived;

        MainWindow _mainWindow;

        public DeviceStreaming(MainWindow mainWindowPanel)
        {
            InitializeComponent();
            InitializeDataSource();
            _streamInfo = new StreamInfo();
            UserMessage = new UserMessage();
            MessagePanel.Children.Add(UserMessage);
            MainPanel.Children.Add(_streamInfo);
            _mainWindow = mainWindowPanel;
        }

        #region Device Configuration

        private void InitializeDataSource()
        {
            Debug.WriteLine("Initializing data source...");
            // The API uses a factory method to create the data source of your application.
            // This creates the factory method, which will then give the data source for your platform.
            // In this case, the platform is RF.
            var deviceSourceCreator = new DeviceSourcePortable(key, license);
            deviceSourceCreator.SetDebugOutputStream((str, args) => Trace.WriteLine(string.Format(str, args)));

            // Here is where we tell the factory method what type of data source we want to receive,
            // which we then set a reference to for future use.
            SourceType[] st = new SourceType[2] { SourceType.TRIGNO_RF, SourceType.TRIGNO_LINK }; 
            _deviceSource = deviceSourceCreator.GetDataSource(st);

            // Here we use the key and license we previously loaded.
            _deviceSource.Key = key;
            _deviceSource.License = license;
        }

        private bool LoadDataSource()
        {
            // Attempts to load device
            try
            {
                // Create a Pipeline based on the datasource.
                Debug.WriteLine("Creating pipeline...");
                var instance = PipelineController.Instance;
                instance.AddPipeline(_deviceSource);
                Debug.WriteLine("Pipeline created");
            }
            // Catches exception if no base is detected
            catch (BaseDetectionFailedException)
            {
                Debug.WriteLine("Error: No base detected");
                UserMessage.Model.Message = "Warning: No Rf base detected. Check USB and power connections.";
                UserMessage.Visibility = Visibility.Visible;
                return false;
            }

            // Create a reference to this Pipeline
            _pipeline = PipelineController.Instance.PipelineIds[0];

            // Define the time (in seconds) we want to spend scanning for paired sensors.
            // TODO, may be deprecated soon
            // _pipeline.TrignoRfManager.InformationScanTime = 5;

            // Register handlers for API component (sensor specific) events
            _pipeline.TrignoRfManager.ComponentAdded += ComponentAddedRf;
            _pipeline.TrignoRfManager.ComponentLost += ComponentLostRf;
            _pipeline.TrignoRfManager.ComponentRemoved += ComponentRemovedRf;
            _pipeline.TrignoRfManager.ComponentScanComplete += ComponentScanCompleteRf;

            // Register handlers for API collection events
            _pipeline.CollectionStarted += CollectionStarted;
            _pipeline.CollectionDataReady += CollectionDataReadyStreamInfo;
            _pipeline.CollectionComplete += CollectionComplete;

            // Get YT Data converter for time series parallel to data packets
            LiveDataYTConverter liveDataYT = new LiveDataYTConverter(_pipeline);
            liveDataYT.DataReady += CollectionDataReadyYT;

            // Check if Link is connected
            if (IsLinkConnected())
            {
                _pipeline.TrignoLinkManager.ComponentAdded += ComponentAddedLink;
                _pipeline.TrignoLinkManager.ComponentLost += ComponentLostLink;
                _pipeline.TrignoLinkManager.ComponentRemoved += ComponentRemovedLink;
                _pipeline.TrignoLinkManager.ComponentScanComplete += ComponentScanCompleteLink;

                // Enable radio button to select BT or ANT and set default mode to previously used mode
                btn_SelectLinkMode.Visibility = Visibility.Visible;
                if(_pipeline.TrignoLinkManager.GetLinkMode(0) == LINK_TRANSMISSION_MODE.ANT)
                {
                    rdo_btn_ANT.IsChecked = true;
                } else
                {
                    rdo_btn_BT.IsChecked = true;
                }
            }

            return true;
        }

        public void ConfigurePipeline(bool startTrigger, bool stopTrigger)
        {
            // Set active data sources to either just RF or RF and Link
            if (IsLinkConnected())
            {
                _pipeline.SetActiveDataSources(new List<SourceType> { SourceType.TRIGNO_RF, SourceType.TRIGNO_LINK });
            }
            else
            {
                _pipeline.SetActiveDataSources(new List<SourceType> { SourceType.TRIGNO_RF });
            }

            // Configure pipeline
            DataLine dataLine = new DataLine(_pipeline);
            _startTriggerEnabled = startTrigger;
            _stopTriggerEnabled = stopTrigger;

            bool isCentro = _pipeline.DataSourceInfo[SourceType.TRIGNO_RF][0].ContainsKey("Centro ID");
            dataLine.ConfigurePipeline(new (), startTrigger, stopTrigger, 2, isCentro);

            // Set stream info
            // Get the frame throughput (the number of Trigno frames passed from the API at a time)
            _frameThroughput = PipelineController.Instance.GetFrameThroughput();

            int totalChannels = _pipeline.TrignoRfManager.Components.Sum(comp => comp.TrignoChannels.Count);
            int numSensorsConnected = _pipeline.TrignoRfManager.Components.Count();

            if(IsLinkConnected())
            {
                totalChannels += _pipeline.TrignoLinkManager.Components.Sum(comp => comp.LinkChannels.Count);
                numSensorsConnected += _pipeline.TrignoLinkManager.Components.Count();
            }

            int selectedSensors = 0;
            int selectedChannels = 0;
            foreach (var sensor in _pipeline.TrignoRfManager.Components.Where(x => x.State == SelectionState.Allocated))
            {
                selectedSensors++;
                foreach (var channel in sensor.TrignoChannels)
                {
                    selectedChannels++;
                }
            }

            _streamInfo.Model.PipelineStatus = _pipeline.CurrentState.ToString();

            _streamInfo.PipelineStatus.Foreground = Brushes.Red;
            _streamInfo.Model.SensorsConnected = numSensorsConnected;
            _streamInfo.Model.TotalChannels = totalChannels;
            _streamInfo.Model.SelectedSensors = selectedSensors;
            _streamInfo.Model.SelectedChannels = selectedChannels;

            return;
        }

        public async Task ResetPipeline()
        {
            // Start reset by disarming pipeline
            await _pipeline.DisarmPipeline();

            _totalFrames = 0;
            _totalLostPackets = 0;
            _streamTime = 0.0;
            _streamInfo.Model.SelectedSensors = 0;
            _streamInfo.Model.SelectedChannels = 0;

            // Removes components from pipeline
            for (int i = 0; i < _pipeline.TrignoRfManager.Components.Count; i++)
            {
                Debug.WriteLine("Removing component...");
                _pipeline.TrignoRfManager.RemoveTrignoComponent(_pipeline.TrignoRfManager.Components[i]);
            }


        }

        #endregion

        #region API Component Event Handlers

        public void ComponentAddedRf(object sender, ComponentAddedEventArgs e)
        {
            Debug.WriteLine("Rf ComponentAdded");
        }

        public void ComponentLostRf(object sender, ComponentLostEventArgs e)
        {
            Debug.WriteLine("Rf ComponentLost");
        }

        public void ComponentRemovedRf(object sender, ComponentRemovedEventArgs e)
        {
            Debug.WriteLine("Rf ComponentRemoved");
        }

        public void ComponentAddedLink(object sender, ComponentAddedEventArgs e)
        {
            Debug.WriteLine("Link ComponentAdded");
        }

        public void ComponentLostLink(object sender, ComponentLostEventArgs e)
        {
            Debug.WriteLine("Link ComponentLost");
        }

        public void ComponentRemovedLink(object sender, ComponentRemovedEventArgs e)
        {
            Debug.WriteLine("Link ComponentRemoved");
        }

        public void ComponentScanCompleteRf(object sender, ComponentScanCompletedEventArgs e)
        {
            if(e.IsRecovering)
            {
                Debug.WriteLine("Waiting for callback from running recovery sequence...");
                UserMessage.Model.Message = "Warning: Unsafe close detected. Running recovery sequence.";
                return;
            } else if (e.RepairSensors)
            {
                Debug.WriteLine("Waiting for call from repairing sensors...");
                UserMessage.Model.Message = "Warning: Re-Pairing Sensors";
                return;
            }
            _scanCallbacksReceived++;

            Debug.WriteLine("Number of RF sensors found: " + _pipeline.TrignoRfManager.Components.Count);

            // Once all scans are complete, continue
            if (_scanCallbacksReceived == _pipeline.GetDataSourceTypes().Count())
            {
                AllScansComplete();
            }
        }

        public void ComponentScanCompleteLink(object sender, ComponentScanCompletedEventArgs e)
        {
            _scanCallbacksReceived++;

            Debug.WriteLine("Number of Link sensors found: " + _pipeline.TrignoLinkManager.Components.Count);

            // Once all scans are complete, continue
            if (_scanCallbacksReceived == _pipeline.GetDataSourceTypes().Count())
            {
                AllScansComplete();
            }
        }

        private void AllScansComplete()
        {
            this.Dispatcher.Invoke(() => {
                // _mainWindow.btn_backToMainPageButton.IsEnabled = true;
                _scannedSensors.btn_ArmPipeline.IsEnabled = true;
                btn_ScanSensors.IsEnabled = true;
                btn_PairSensors.IsEnabled = true;
                _streamInfo.Model.PipelineStatus = GetPipelineState();
                _streamInfo.PipelineStatus.Foreground = Brushes.SpringGreen;
                UserMessage.Visibility = Visibility.Hidden;
            });

            // Check if no Rf sensors were detected in scan
            if (_pipeline.TrignoRfManager.Components.Count <= 0)
            {
                // Prompt user to try again if none found
                _scannedSensors.NoRfSensorsDetected();
                return;
            }

            // Populated scanned sensors list.
            if (IsLinkConnected())
            {
                this.Dispatcher.Invoke(() => { _scannedSensors.ScanComplete(_pipeline.TrignoRfManager.Components, _pipeline.TrignoLinkManager.Components); });
            }
            else
            {
                this.Dispatcher.Invoke(() => { _scannedSensors.ScanComplete(_pipeline.TrignoRfManager.Components); });
            }
        }

        #endregion

        #region API Data Collection Event Handlers
        public void CollectionStarted(object sender, CollectionStartedEvent e)
        {
            if (_startTriggerEnabled)
            {
                _waitingForStartTrigger = true;
                _streamInfo.Model.PipelineStatus = "Waiting For Start Trigger...";
                Debug.WriteLine("Waiting for start trigger");
            }
            else
            {
                _streamInfo.Model.PipelineStatus = GetPipelineState();
            }

            _data = new List<List<double>>();
            _totalFrames = 0;
            _totalLostPackets = 0;
            _streamTime = 0.0;


            // Recreate the list of data channels for recording.
            int totalChannels = 0;
            // Start with Rf
            // First, iterate across all components
            for (int i = 0; i < _pipeline.TrignoRfManager.Components.Count; i++)
            {
                // then across all channels within each component.
                for (int j = 0; j < _pipeline.TrignoRfManager.Components[i].TrignoChannels.Count; j++)
                {
                    // Need two columns per channel (one for time, one for data)
                    _data.Add(new List<double>());
                    _data.Add(new List<double>());
                    if (_packetInterval == 0)
                    {
                        _packetInterval = _pipeline.TrignoRfManager.Components[i].TrignoChannels[j].FrameInterval * _frameThroughput;
                    }
                    totalChannels++;
                }
            }

            // Continue with Link if connected
            if(IsLinkConnected())
            {
                // First, iterate across all components
                for (int i = 0; i < _pipeline.TrignoLinkManager.Components.Count; i++)
                {
                    // then across all channels within each component.
                    for (int j = 0; j < _pipeline.TrignoLinkManager.Components[i].LinkChannels.Count; j++)
                    {
                        // Need two columns per channel (one for time, one for data)
                        _data.Add(new List<double>());
                        _data.Add(new List<double>());
                        if (_packetInterval == 0)
                        {
                            _packetInterval = _pipeline.TrignoLinkManager.Components[i].LinkChannels[j].FrameInterval * _frameThroughput;
                        }
                        totalChannels++;
                    }
                }
            }
        }

        /// <summary>
        /// Collection callback called each time a packet is ready, dedicated towards updating the stream info.
        /// This includes updating the pipeline status, stream time, number of lost packets, and number of frames.
        /// </summary>
        public void CollectionDataReadyStreamInfo(object sender, ComponentDataReadyEventArgs e)
        {
            if (_startTriggerEnabled)
            {
                _waitingForStartTrigger = false;
                if (_stopTriggerEnabled)
                {
                    _waitingForStopTrigger = true;
                    this.Dispatcher.Invoke(() =>
                    {
                        _streamInfo.Model.PipelineStatus = "Waiting For Stop Trigger...";
                        _streamInfo.PipelineStatus.Foreground = Brushes.Red;
                    });
                }
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    _streamInfo.Model.PipelineStatus = GetPipelineState();
                    _streamInfo.PipelineStatus.Foreground = Brushes.Green;
                });
            }

            // Checks to see if any of the packets were lost during the stream.
            // We need to check all frames to see if any data is lost from any of them.
            int lostPackets = 0;
            for (int k = 0; k < e.Data.Count(); k++)
            {
                // Loops through each sensor.l
                for (int i = 0; i < e.Data[k].SensorData.Count(); i++)
                {
                    // Checks to see if any of the data in a sensor is lost.
                    if (e.Data[k].SensorData[i].IsDroppedPacket)
                    {
                        lostPackets++;
                    }
                }
            }

            // Update stream info
            _totalLostPackets += lostPackets;
            _streamTime += _packetInterval;
            _totalFrames += _frameThroughput * e.Data[0].SensorData.Count();
            // Update model for stream info
            _streamInfo.Model.PacketsLost = _totalLostPackets;
            _streamInfo.Model.StreamTime = _streamTime.ToString("#.##") + " seconds";
            _streamInfo.Model.FramesCollected = _totalFrames;
        }

        /// <summary>
        /// Collection callback called each time a packet is ready, dedicated towards storing all data received
        /// along with its associated time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CollectionDataReadyYT(object sender, YTDataReadyEventArgs e)
        {
            // Loops through each packet received
            for (int k = 0; k < e.YTData.Count(); k++)
            {
                // Determines the column that the data will be inserted into.
                int columnIndex = 0;

                // Loops through each channel for all the sensors.
                foreach (Guid guid in e.YTData[k].Keys)
                {
                    // Loops through the data of each channel.
                    foreach (var val in e.YTData[k][guid])
                    {
                        // Item1 is the time, Item2 is the data
                        // Adds the data at the current column index.
                        _data[columnIndex].Add(val.Item1);
                        _data[columnIndex+1].Add(val.Item2);
                    }
                    columnIndex += 2;
                }
            }
        }

        public void CollectionComplete(object sender, CollectionCompleteEvent e)
        {
            if (_waitingForStopTrigger)
            {
                _waitingForStopTrigger = false;
            }

            // TODO
            // This is a temporary fix to a bug causing multiple CollectionComplete callbacks
            // when stopping then starting the stream immediately due to an error
            // in handling stop calls in Trigno Link.
            Thread.Sleep(1000);

            this.Dispatcher.Invoke(() => {
                // _mainWindow.btn_backToMainPageButton.IsEnabled = true;
                btn_Export.IsEnabled = true;
                btn_Start.IsEnabled = true;
                btn_Disarm.IsEnabled = true;
                btn_Stop.IsEnabled = false; // TODO, Shouldn't be needed but part of temporary fix for above bug
                _streamInfo.PipelineStatus.Foreground = Brushes.Red;
                _streamInfo.Model.PipelineStatus = GetPipelineState();
            });
        }

        #endregion

        #region Button Events Handlers

        public void clk_LoadDevice(object sender, RoutedEventArgs e)
        {
            InitializeDataSource();
            if (LoadDataSource())
            {
                this.Dispatcher.Invoke(() =>
                {
                    btn_LoadDevice.IsEnabled = false;
                    btn_PairSensors.IsEnabled = true;
                    btn_ScanSensors.IsEnabled = true;
                    UserMessage.Visibility = Visibility.Hidden;
                    _streamInfo.Model.DeviceName = DeviceConnectedName();
                    _streamInfo.Model.PipelineStatus = GetPipelineState();
                    _streamInfo.DeviceName.Foreground = Brushes.Green;
                });
            }
        }

        public async void clk_Scan(object sender, RoutedEventArgs e)
        {
            _scanCallbacksReceived = 0;

            this.Dispatcher.Invoke(() =>
            {
                // _mainWindow.btn_backToMainPageButton.IsEnabled = false;
                btn_ScanSensors.IsEnabled = false;
                btn_PairSensors.IsEnabled = false;
                UserMessage.Visibility = Visibility.Hidden;
            });

            SelectLinkModesPopup();
        }

        public void SelectLinkModesPopup()
        {
            // All supported ANT devices
            SensorTrignoLinkANT.ANT_DEVICE_TYPE[] devicesANT = new SensorTrignoLinkANT.ANT_DEVICE_TYPE[]
            {
                SensorTrignoLinkANT.ANT_DEVICE_TYPE.BIKE_POWER,
                SensorTrignoLinkANT.ANT_DEVICE_TYPE.MOXY,
                SensorTrignoLinkANT.ANT_DEVICE_TYPE.HEART_RATE_STRAP,
                SensorTrignoLinkANT.ANT_DEVICE_TYPE.SPEED_AND_CADENCE_SENSOR,
                SensorTrignoLinkANT.ANT_DEVICE_TYPE.CADENCE_SENSOR,
                SensorTrignoLinkANT.ANT_DEVICE_TYPE.SPEED_SENSOR
            };

            // All Supported BT devices
            SensorTrignoLinkBT.BT_DEVICE_TYPE[] devicesBT = new SensorTrignoLinkBT.BT_DEVICE_TYPE[]
            {
                SensorTrignoLinkBT.BT_DEVICE_TYPE.MOXY,
                SensorTrignoLinkBT.BT_DEVICE_TYPE.DYNAMOMETER,
                SensorTrignoLinkBT.BT_DEVICE_TYPE.VO2_MASK,
                SensorTrignoLinkBT.BT_DEVICE_TYPE.HEART_RATE_STRAP,
                SensorTrignoLinkBT.BT_DEVICE_TYPE.CADENCE_SENSOR,
                SensorTrignoLinkBT.BT_DEVICE_TYPE.SPEED_SENSOR,
                SensorTrignoLinkBT.BT_DEVICE_TYPE.SPEED_AND_CADENCE_SENSOR
            };

            SecondaryPanel.Children.Clear();
            if(!IsLinkConnected())
            {
                StartScan();
            } else if (rdo_btn_ANT.IsChecked == true)
            {
                SecondaryPanel.Children.Add(new SensorTypesPopup(this, devicesANT));
            }
            else
            {
                SecondaryPanel.Children.Add(new SensorTypesPopup(this, devicesBT));
            }
        }

        public void LoadDevicesBT(SensorTrignoLinkBT.BT_DEVICE_TYPE[] devicesBT)
        {
            for (int i = 0; i < _pipeline.TrignoLinkManager.LinkCount(); i++)
            {
                _pipeline.TrignoLinkManager.SetLinkMode(i, LINK_TRANSMISSION_MODE.BLE_Low_Data_Rate);
                _pipeline.TrignoLinkManager.SetBTDevices(i, devicesBT);
            }
            StartScan();
        }

        public void LoadDevicesANT(SensorTrignoLinkANT.ANT_DEVICE_TYPE[] devicesANT)
        {
            for (int i = 0; i < _pipeline.TrignoLinkManager.LinkCount(); i++)
            {
                _pipeline.TrignoLinkManager.SetLinkMode(i, LINK_TRANSMISSION_MODE.ANT);
                _pipeline.TrignoLinkManager.SetANTDevices(i, devicesANT);
            }
            StartScan();
        }

        private async void StartScan()
        {
            // Display scanned sensors user control.
            SecondaryPanel.Children.Clear();
            _scannedSensors = new ScannedSensors(this, _pipeline, IsLinkConnected());
            SecondaryPanel.Children.Add(_scannedSensors);

            this.Dispatcher.Invoke(() =>
            {
                UserMessage.Model.Message = "Scanning for sensors...";
                UserMessage.Visibility = Visibility.Visible;
            });

            await _pipeline.Scan();
        }

        public void clk_Pair(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("# of components before pair: " + _pipeline.TrignoRfManager.Components.Count);

            // Display pairing user control
            SecondaryPanel.Children.Clear();
            _pairSensor = new PairSensor(_pipeline, _mainWindow, this);
            SecondaryPanel.Children.Add(_pairSensor);

            this.Dispatcher.Invoke(() =>
            {
                btn_PairSensors.IsEnabled = false;
                btn_ScanSensors.IsEnabled = false;
                UserMessage.Visibility = Visibility.Hidden;
            });
        }

        public void clk_Export(object sender, RoutedEventArgs e)
        {
            List<string> lines = new List<string>();

            int nSensors = _pipeline.TrignoRfManager.Components.Count;
            if(IsLinkConnected())
            {
                nSensors += _pipeline.TrignoLinkManager.Components.Count;
            }
            int nChannels = _data.Count;

            string labelRow = "";

            foreach (var sensor in _pipeline.TrignoRfManager.Components.Where(x => x.State == SelectionState.Allocated))
            {
                foreach (var channel in sensor.TrignoChannels)
                {
                    Debug.WriteLine("Channel Name: " + channel.Name);
                    labelRow += channel.Name + " Time Series ," + channel.Name + ",";
                }
            }

            if(IsLinkConnected())
            {
                foreach (var sensor in _pipeline.TrignoLinkManager.Components.Where(x => x.State == SelectionState.Allocated))
                {
                    foreach (var channel in sensor.LinkChannels)
                    {
                        Debug.WriteLine("Channel Name: " + channel.Name);
                        labelRow += channel.Name + " Time Series ," + channel.Name + ",";
                    }
                }
            }

            int largestChannel = 0;
            for (int i = 1; i < nChannels; i++)
            {
                if (_data[i].Count > _data[largestChannel].Count)
                {
                    largestChannel = i;
                }
            }

            for (int i = 0; i < _data[largestChannel].Count; i++)
            {
                string dataRow = "";

                if (i == 0)
                {
                    dataRow += labelRow;
                    dataRow += "\n";
                }

                for (int j = 0; j < nChannels; j++)
                {
                    if (i < _data[j].Count) 
                    {
                        dataRow += _data[j].ElementAt(i).ToString() + ",";
                    }
                    else 
                    {
                        dataRow += ",";
                    }
                }

                lines.Add(dataRow);
            }

            string dataDir = "./sensor_data";
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            string fileName = DateTime.Now.ToString("yyy-dd-MM--HH-mm-ss");
            string path = dataDir + "/" + fileName + ".csv";
            using (StreamWriter outputFile = new StreamWriter(path))
            {
                foreach (string line in lines)
                {
                    outputFile.WriteLine(line);
                }
            }

            // Open the file
            Process.Start(new ProcessStartInfo()
            {
                FileName = "sensor_data",
                UseShellExecute = true,
                Verb = "open"
            });


            this.Dispatcher.Invoke(() =>
            {
                btn_Export.IsEnabled = false;
            });
        }

        public async void clk_Start(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                btn_Start.IsEnabled = false;
                btn_Stop.IsEnabled = true;
                btn_Disarm.IsEnabled = false;
                btn_Export.IsEnabled = false;
                _streamInfo.PipelineStatus.Foreground = Brushes.Green;
            });

            await _pipeline.Start();
        }

        public async void clk_Stop(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                btn_Stop.IsEnabled = false;
            });

            await StopStreamAsync();

            this.Dispatcher.Invoke(() =>
            {
                _streamInfo.Model.PipelineStatus = GetPipelineState();
                _streamInfo.PipelineStatus.Foreground = Brushes.Blue;
            });
        }

        public async Task StopStreamAsync()
        {
            await _pipeline.Stop();
        }

        public async void clk_Disarm(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                btn_Disarm.IsEnabled = false;
                btn_Start.IsEnabled = false;
                btn_Export.IsEnabled = false;
                UserMessage.Model.Message = "Disarming pipeline...";
                UserMessage.Visibility = Visibility.Visible;
            });

            await Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                       new Action(() => { })); // Finishes all dispatcher tasks 


            //await Task.Delay(2); // Give time to let UI update

            // Disarm the pipeline
            await _pipeline.DisarmPipeline();

            // Reset metrics
            resetStreamInfo();

            this.Dispatcher.Invoke(() =>
            {
                UserMessage.Visibility = Visibility.Hidden;
                btn_ScanSensors.IsEnabled = true;
                btn_PairSensors.IsEnabled = true;
                _scannedSensors.IsEnabled = true;
            });
            
        }

        private void resetStreamInfo()
        {
            _totalFrames = 0;
            _totalLostPackets = 0;
            _streamTime = 0.0;

            _streamInfo.Model.SensorsConnected = 0;
            _streamInfo.Model.TotalChannels = 0;
            _streamInfo.Model.PipelineStatus = GetPipelineState();
            _streamInfo.Model.StreamTime = "0.0 seconds";
            _streamInfo.Model.PacketsLost = 0;
            _streamInfo.Model.FramesCollected = 0;
            _streamInfo.PipelineStatus.Foreground = Brushes.SpringGreen;
        }

        #endregion

        public string GetPipelineState()
        {
            return _pipeline.CurrentState.ToString();
        }

        public bool IsLinkConnected()
        {
            var sources = _pipeline.GetDataSourceTypes();
            return _pipeline.GetDataSourceTypes().Contains(SourceType.TRIGNO_LINK);
        }

        /// <summary>
        /// Checks to see if the Rf source connected is Lite or not.
        /// We know Lite is connected if the source info has a Dongle ID (as opposed to a Centro or Base ID)
        /// </summary>
        /// <returns>Whether a Lite is connected</returns>
        public bool IsLiteConnected()
        {
            var dataSourceInfo = _pipeline.DataSourceInfo[SourceType.TRIGNO_RF];
            return dataSourceInfo[0].ContainsKey("Dongle ID");
        }

        public bool IsCentroConnected()
        {
            var dataSourceInfo = _pipeline.DataSourceInfo[SourceType.TRIGNO_RF];
            return dataSourceInfo[0].ContainsKey("Centro ID");
        }

        public string DeviceConnectedName()
        {
            string retName;

            var dataSourceInfo = _pipeline.DataSourceInfo[SourceType.TRIGNO_RF];
            string name = dataSourceInfo[0]["Hardware Name"];
            if (IsCentroConnected())
            {
                retName = name + " " + dataSourceInfo[0]["Centro ID"];
            } else if (name.Contains("base"))
            {
                retName = "Trigno Base Station " + name.Split("_")[1];
            } else if (IsLiteConnected()) {
                retName = name + " " + dataSourceInfo[0]["Dongle ID"];
            }  else
            {
                Console.WriteLine("Device not supported.");
                retName = "Trigno Device";
            }

            return retName;
        }

        public void RemoveLinkSensor(int sensorID)
        {
            SensorTrignoLink comp = _pipeline.TrignoLinkManager.Components.Where(x => x.Sid == sensorID).FirstOrDefault();
            _pipeline.TrignoLinkManager.RemoveLinkComponent(comp);
        }
    }
}
