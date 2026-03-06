"""
Data Collector GUI
This is the GUI that lets you connect to a base, scan via rf for sensors, and stream data from them in real time.
"""
from PySide6 import QtCore
from PySide6.QtCore import *
from PySide6.QtGui import *
from PySide6.QtWidgets import *

from DataCollector.AnalogInputController import AnalogInputController
from DataCollector.CollectDataController import *
from DataCollector.CollectionMetricsManagement import CollectionMetricsManagement
from DataCollector.LinkController import LinkController
from DataCollector.CentroTriggerController import CentroTriggerController
from Plotter import GenericPlot as gp


class CollectDataWindow(QWidget):
    plot_enabled = False

    def __init__(self, controller):
        QWidget.__init__(self)
        self.analog_input_controller : AnalogInputController
        self.analog_input_controller = None
        self.trigger_controller = None
        self.link_controller : LinkController = LinkController(self)
        self.are_triggers_ready = False
        self.trigger_channel_func = None
        self.pipelinetext = "Off"
        self.controller = controller
        self.buttonPanel = self.ButtonPanel()
        self.plotPanel = None
        self.collectionLabelPanel = self.CollectionLabelPanel()

        self.grid = QGridLayout(self)

        self.MetricsConnector = CollectionMetricsManagement()
        self.collectionLabelPanel.setFixedHeight(275)
        self.MetricsConnector.collectionmetrics.setFixedHeight(275)

        self.metricspanel = QWidget()
        self.metricspane = QHBoxLayout()
        self.metricspane.setAlignment(Qt.AlignmentFlag.AlignTop)
        self.metricspane.addWidget(self.collectionLabelPanel)
        self.metricspane.addWidget(self.MetricsConnector.collectionmetrics)
        self.metricspanel.setLayout(self.metricspane)
        self.metricspanel.setFixedWidth(400)
        self.grid.addWidget(self.metricspanel, 0, 1)
        style_sheet = """
                    QWidget { background-color: #1F253D; }
                    
                    QCheckbox { spacing: 0; }

                    QCheckBox::indicator {
                        width: 18px;
                        height: 18px;
                    }
                    QCheckBox::indicator:unchecked {
                        image: url(./Images/unchecked.png);
                    }
                   
                    QCheckBox::indicator:checked {
                        image: url(./Images/checked.png);
                    }
                    QPushButton.primaryButton{
                        border: none; 
                        background-color:#3D885C;
                        color: white;
                        border-radius:5px;
                        font-size: 12pt;
                    }
                    QPushButton.primaryButton:pressed {
                        border: none; 
                        background-color:#35734F;
                        color: white;
                        border-radius:5px;
                        font-size: 12pt;
                    }

                    QPushButton.secondaryButton{
                        border: 1px solid white; 
                        font-size: 12pt;
                        border-radius:5px;
                    }
                    
                    QPushButton.secondaryButton:pressed {
                        border: 1px solid white; 
                        font-size: 12pt;
                        border-radius:5px;
                    }

                    QListWidget {color: white; background:#4A5063; border-radius: 5px; outline: 0px;}

                    QListWidget::item:selected {color: white; border-left: 2px solid #3D885C;}

                    QComboBox {color: white;  }
                    
                    QComboBox:disabled { color : grey; }

                    QComboBox QAbstractItemView {
                        background-color: white;
                        border:none;
                        outline: 0px;      
                        color: black;        
                    }

                    QComboBox QAbstractItemView::item:hover {
                        background-color:  #B6D3C5;
                        color: black; 
                    }

                    QComboBox QAbstractItemView::item:selected {
                        background-color:  #B6D3C5;
                        color: black;
                    }
                    
                    QLabel { color: white; }
                    
                    QLabel:disabled {color : grey}
                """
        self.setStyleSheet(style_sheet)
        self.tabPanel = self.TabPanel()
        self.grid.addWidget(self.tabPanel, 0, 0)
        self.setLayout(self.grid)
        self.setWindowTitle("Python Demo")
        self.setWindowIcon(QIcon("./Images/window_icon.png"))
        self.pairing = False
        self.selectedSensor = None

    def AddPlotPanel(self):
        self.plotPanel = self.Plotter()
        self.grid.addWidget(self.plotPanel, 0, 2)

    def SetCallbackConnector(self):
        if self.plot_enabled:
            self.CallbackConnector = PlottingManagement(self, self.MetricsConnector, self.plotCanvas)
        else:
            self.CallbackConnector = PlottingManagement(self, self.MetricsConnector)

    # -----------------------------------------------------------------------
    # ---- GUI Components
    def TabPanel(self):
        # Initialize tab screen
        self.tabs = QTabWidget(self)
        self.tabs.autoFillBackground()
        self.main_control_tab = self.ButtonPanel()
        # Add tabs
        self.tabs.addTab(self.main_control_tab,    "Trial Controls")
        self.tabs.setSizePolicy(QSizePolicy.Preferred, QSizePolicy.Expanding)
        self.tabs.setStyleSheet(
                """
                QTabBar::tab {
                    color: white;                    
                    background: transparent;          
                    padding: 6px 12px;                
                    border: 1px solid transparent;   
                    border-top-left-radius: 6px;
                    border-top-right-radius: 6px;
                }
                
                QTabBar::tab:selected {
                    color: #1F253D;
                    background: #CFE8FF;              
                    border-color: #9CC9F5;           
                }
                
                QTabBar::tab:hover {
                    background: #D9EEFF;
                    color: #1F253D;
                }
                """
                )
        return self.tabs


    def ButtonPanel(self):
        buttonPanel = QWidget()
        buttonPanel.setSizePolicy(QSizePolicy.Preferred, QSizePolicy.Expanding)
        buttonPanel.layout = QVBoxLayout()

        buttonLayout = QVBoxLayout()
        buttonLayout.setSpacing(12)


        # ---- Include YT Data?
        yt_layout = QHBoxLayout()
        yt_label = QLabel('Stream Time Series Values?')
        self.yt_checkbox = QCheckBox()
        self.yt_checkbox.checkStateChanged.connect(self.update_stream_type)
        self.yt_checkbox.setEnabled(False)
        yt_layout.addWidget(yt_label)
        yt_layout.addWidget(self.yt_checkbox)
        buttonLayout.addLayout(yt_layout)

        findSensor_layout = QHBoxLayout()

        # ---- Pair Button
        self.pair_button = QPushButton('Pair', self)
        self.pair_button.setToolTip('Pair Sensors')
        self.pair_button.setSizePolicy(QSizePolicy.Preferred, QSizePolicy.Expanding)
        self.pair_button.setProperty("class", 'primaryButton')
        self.pair_button.clicked.connect(self.pair_callback)
        self.pair_button.setStyleSheet("")
        self.pair_button.setEnabled(True)
        self.pair_button.setFixedHeight(50)
        findSensor_layout.addWidget(self.pair_button)

        # ---- Scan Button
        self.scan_button = QPushButton('Scan', self)
        self.scan_button.setToolTip('Scan for Sensors')
        self.scan_button.setSizePolicy(QSizePolicy.Preferred, QSizePolicy.Expanding)
        self.scan_button.setProperty("class", 'primaryButton')
        self.scan_button.clicked.connect(self.Scan_Window)
        self.scan_button.setEnabled(False)
        self.scan_button.setFixedHeight(50)
        findSensor_layout.addWidget(self.scan_button)

        buttonLayout.addLayout(findSensor_layout)

        # ---- Base Station Specific Triggering
        self.baseStationTriggerWidget = QWidget(self)
        baseStationTriggerLayout = QHBoxLayout()
        self.baseStationTriggerWidget.layout = QHBoxLayout()
        baseStationTriggerLayout.setSpacing(0)

        self.starttriggercheckbox = QCheckBox()
        self.starttriggercheckbox.setSizePolicy(QSizePolicy.Fixed, QSizePolicy.Fixed)
        self.starttriggercheckbox.clicked.connect(self.on_trigger_config_change)
        baseStationTriggerLayout.addWidget(self.starttriggercheckbox)
        self.starttriggerlabel = QLabel('Start Trigger', self)
        baseStationTriggerLayout.addWidget(self.starttriggerlabel)
        self.stoptriggercheckbox = QCheckBox()
        self.stoptriggercheckbox.clicked.connect(self.on_trigger_config_change)
        self.stoptriggercheckbox.setSizePolicy(QSizePolicy.Fixed, QSizePolicy.Fixed)
        baseStationTriggerLayout.addWidget(self.stoptriggercheckbox)
        self.stoptriggerlabel = QLabel('Stop Trigger', self)
        baseStationTriggerLayout.addWidget(self.stoptriggerlabel)
        self.baseStationTriggerWidget.setLayout(baseStationTriggerLayout)

        self.baseStationTriggerWidget.setVisible(False)
        buttonLayout.addWidget(self.baseStationTriggerWidget)

        # ---- Start Button
        self.start_button = QPushButton('Start', self)
        self.start_button.setToolTip('Start Sensor Stream')
        self.start_button.setSizePolicy(QSizePolicy.Preferred, QSizePolicy.Expanding)
        self.start_button.setProperty("class", 'secondaryButton')
        self.start_button.clicked.connect(self.start_callback)
        self.start_button.setEnabled(False)
        self.start_button.setStyleSheet('border: 1px solid gray; color: gray;')
        self.start_button.setFixedHeight(50)
        buttonLayout.addWidget(self.start_button)

        # ---- Stop Button
        self.stop_button = QPushButton('Stop', self)
        self.stop_button.setToolTip('Stop Sensor Stream')
        self.stop_button.setSizePolicy(QSizePolicy.Preferred, QSizePolicy.Expanding)
        self.stop_button.setProperty("class", 'secondaryButton')
        self.stop_button.clicked.connect(self.stop_callback)
        self.stop_button.setEnabled(False)
        self.stop_button.setStyleSheet('border: 1px solid gray; color: gray;')
        self.stop_button.setFixedHeight(50)
        buttonLayout.addWidget(self.stop_button)

        # ---- Export CSV Button
        self.exportcsv_button = QPushButton('Export to CSV', self)
        self.exportcsv_button.setToolTip('Export collected data to project root - data.csv')
        self.exportcsv_button.setSizePolicy(QSizePolicy.Preferred, QSizePolicy.Expanding)
        self.exportcsv_button.setProperty("class", 'secondaryButton')
        self.exportcsv_button.clicked.connect(self.exportcsv_callback)
        self.exportcsv_button.setEnabled(False)
        self.exportcsv_button.setStyleSheet('border: 1px solid gray; color: gray;')
        self.exportcsv_button.setFixedHeight(50)
        buttonLayout.addWidget(self.exportcsv_button)

        # ---- Drop-down menu of sensor modes
        self.SensorModeList = QComboBox(self)
        self.sensor_mode_default_tooltip = 'Sensor Modes'
        self.SensorModeList.setToolTip(self.sensor_mode_default_tooltip)
        self.SensorModeList.objectName = 'PlaceHolder'
        self.SensorModeList.activated.connect(self.sensorModeList_callback)
        self.SensorModeList.setStyleSheet('QComboBox {color: white;background: #848482}')
        buttonLayout.addWidget(self.SensorModeList)

        # ---- List of detected sensors
        sensorListLabel = QLabel("Connected Sensors")
        sensorListLabel.setStyleSheet('QLabel { font-size: 12pt; color: white}')
        sensorListLabel.setAlignment(Qt.AlignmentFlag.AlignLeft)
        buttonLayout.addWidget(sensorListLabel)

        list_layout = QVBoxLayout()
        list_layout.setSpacing(0)

        self.list_container = QWidget(self)
        self.list_container.setToolTip('Sensor List')
        self.list_container.objectName = 'PlaceHolder'
        self.list_container.setStyleSheet(
            'QWidget {color: white; background:#4A5063; border-radius: 5px; margin: 0}')

        self.no_sensor_error = QLabel("No sensors found.")
        self.no_sensor_error.setStyleSheet('QLabel { font-size: 1pt; color: transparent; margin: 0}')
        self.no_sensor_error.setAlignment(Qt.AlignmentFlag.AlignLeft)
        list_layout.addWidget(self.no_sensor_error)

        self.SensorListBox = QListWidget(self)
        self.SensorListBox.setStyleSheet(
            'QListWidget {color: white; background: #4A5063; border-radius: 5px;}')
        self.SensorListBox.itemClicked.connect(self.sensorList_callback)
        self.SensorListBox.setToolTip('Sensor List')
        self.SensorListBox.currentItemChanged.connect(self.sensorList_callback)
        self.SensorListBox.setSizePolicy(QSizePolicy.Preferred, QSizePolicy.Expanding)
        list_layout.addWidget(self.SensorListBox)

        self.list_container.setLayout(list_layout)
        buttonLayout.addWidget(self.list_container)
        buttonPanel.setLayout(buttonLayout)
        buttonPanel.setFixedWidth(275)
        return buttonPanel

    def Plotter(self):
        widget = QWidget()
        widget.setLayout(QVBoxLayout())

        plot_mode = 'windowed'  # Select between 'scrolling' and 'windowed'
        pc = gp.GenericPlot(plot_mode)
        pc.native.objectName = 'vispyCanvas'
        pc.native.parent = self
        label = QLabel("* This Demo plots EMG Channels only")
        label.setStyleSheet('QLabel { font-size: 8pt; color: white}')
        label.setFixedHeight(20)
        label.setAlignment(Qt.AlignmentFlag.AlignRight)
        widget.layout().addWidget(pc.native)
        widget.layout().addWidget(label)
        self.plotCanvas = pc


        return widget

    def CollectionLabelPanel(self):
        collectionLabelPanel = QWidget()
        collectionlabelsLayout = QVBoxLayout()

        pipelinelabel = QLabel('Pipeline State')
        pipelinelabel.setAlignment(Qt.AlignLeft)
        collectionlabelsLayout.addWidget(pipelinelabel)

        sensorsconnectedlabel = QLabel('Connected Sensors', self)
        sensorsconnectedlabel.setAlignment(Qt.AlignLeft)
        collectionlabelsLayout.addWidget(sensorsconnectedlabel)

        totalchannelslabel = QLabel('Total Channels', self)
        totalchannelslabel.setAlignment(Qt.AlignLeft)
        collectionlabelsLayout.addWidget(totalchannelslabel)

        framescollectedlabel = QLabel('Frames Collected', self)
        framescollectedlabel.setAlignment(Qt.AlignLeft)
        collectionlabelsLayout.addWidget(framescollectedlabel)

        collectionLabelPanel.setFixedWidth(150)
        collectionLabelPanel.setLayout(collectionlabelsLayout)

        return collectionLabelPanel

    # -----------------------------------------------------------------------
    # ---- Callback Functions
    def getpipelinestate(self):
        self.pipelinetext = self.CallbackConnector.base.PipelineState_Callback()
        self.MetricsConnector.pipelinestatelabel.setText(self.pipelinetext)

    def connect_callback(self):
        self.CallbackConnector.base.Connect_Callback()
        self.set_station_type(self.CallbackConnector.base.trigno_station_type)
        self.pair_button.setEnabled(True)
        self.pair_button.setObjectName("pairButton")
        self.scan_button.setEnabled(True)
        self.scan_button.setObjectName("scanButton")
        self.starttriggerlabel.setStyleSheet("color : white;")
        self.stoptriggerlabel.setStyleSheet("color : white;")
        self.getpipelinestate()
        self.MetricsConnector.pipelinestatelabel.setText(self.pipelinetext + " (Base Connected)")
        self.yt_checkbox.setEnabled(True)
        self.yt_checkbox.setChecked(True)
        self.link_controller.trigno_base = self.CallbackConnector.base
        self.link_controller.set_device_type_panel()

    def pair_callback(self):
        """Pair button callback"""
        self.Pair_Window()
        self.getpipelinestate()
        self.exportcsv_button.setEnabled(False)
        self.exportcsv_button.setStyleSheet('border: 1px solid gray; color: gray;')

    def Pair_Window(self):
        """Open pair sensor window to set pair number and begin pairing process"""
        pairWindow = QInputDialog()
        pairWindow.setStyleSheet("background-color: #757A89; color: white")
        pair_number, pressed = pairWindow.getInt(QWidget(), "Pair Sensor", "Pair Number",
                                                   1, 0, 100, 1)
        if pressed:
            self.pairing = True
            self.pair_canceled = False
            self.CallbackConnector.base.pair_number = pair_number
            self.PairThreadManager()

    def PairThreadManager(self):
        """Start t1 thread to begin pairing operation in DelsysAPI
           Start t2 thread to await result of CheckPairStatus() to return False
           Once threads begin, display awaiting sensor pair request window/countdown"""

        self.t1 = threading.Thread(target=self.CallbackConnector.base.Pair_Callback)
        self.t1.start()

        self.t2 = threading.Thread(target=self.awaitPairThread)
        self.t2.start()

        self.BeginPairingUISequence()


    def BeginPairingUISequence(self):
        """The awaiting sensor window will stay open until either:
           A) The pairing countdown timer completes (The end of the countdown will send a CancelPair request to the DelsysAPI)
           or...
           B) A sensor has been paired to the base (via self.pairing flag set by DelsysAPI CheckPairStatus() bool)

           If a sensor is paired, ask the user if they want to pair another sensor (No = start a scan for all previously paired sensors)
        """

        pair_success = False
        self.pair_countdown_seconds = 15

        awaitingPairWindow = QDialog()
        awaitingPairWindow.setWindowTitle(
            "Awaiting sensor pair request... Auto-closing in " + str(self.pair_countdown_seconds) + " seconds.")
        awaitingPairWindow.setFixedWidth(500)
        awaitingPairWindow.setFixedHeight(80)
        awaitingPairWindow.show()

        while self.pair_countdown_seconds > 0:
            if self.pairing:
                time.sleep(1)
                self.pair_countdown_seconds -= 1
                self.UpdateTimerUI(awaitingPairWindow)
            else:
                pair_success = True
                break

        awaitingPairWindow.close()
        if not pair_success:
            self.CallbackConnector.base.TrigBase.CancelPair()
        else:
            self.ShowPairAnotherSensorDialog()

    def awaitPairThread(self):
        """ Wait for a sensor to be paired
        Once PairSensor() command is sent to the DelsysAPI, CheckPairStatus() will return True until a sensor has been paired to the base"""
        time.sleep(1)
        while self.pairing:
            pairstatus = self.CallbackConnector.base.CheckPairStatus()
            if not pairstatus:
                self.pairing = False

    def UpdateTimerUI(self, awaitingPairWindow):
        awaitingPairWindow.setWindowTitle(
            "Awaiting sensor pair request... Auto-closing in " + str(self.pair_countdown_seconds) + " seconds.")

    def ShowPairAnotherSensorDialog(self):
        messagebox = QMessageBox()
        messagebox.setWindowTitle("Pair Sensor")
        messagebox.setText("Pair another sensor?")
        messagebox.setStandardButtons(QMessageBox.Yes | QMessageBox.No)
        messagebox.setIcon(QMessageBox.Question)
        button = messagebox.exec_()

        if button == QMessageBox.Yes:
            self.Pair_Window()
        else:
            self.Scan_Window()

    def Scan_Window(self):
        self.scanning_dialog = QDialog()
        self.scanning_dialog.setAttribute(Qt.WA_TranslucentBackground)
        self.scanning_dialog.setWindowFlags(Qt.FramelessWindowHint | Qt.Dialog)
        self.scanning_dialog.resize(150,80)
        self.scanning_dialog.setStyleSheet("QDialog {background-color: transparent; border: none }")

        container = QWidget()
        container.setStyleSheet("QWidget { background-color: #4A5063; border: 4px solid #4A5063; border-radius: 5px; }")
        container_layout = QVBoxLayout(container)

        scanning_label = QLabel("Scanning...")
        scanning_label.setStyleSheet("color: white; font-size: 20px")
        scanning_label.setAlignment(Qt.AlignmentFlag.AlignCenter)

        container_layout.addWidget(scanning_label)

        dialog_layout = QHBoxLayout()
        dialog_layout.setContentsMargins(0,0,0,0)
        dialog_layout.setSpacing(0)
        self.scanning_dialog.setLayout(dialog_layout)
        dialog_layout.addWidget(container)

        parent_geometry = self.geometry()
        dialog_geometry = self.scanning_dialog.frameGeometry()
        center_point = parent_geometry.center()
        dialog_geometry.moveCenter(center_point)
        self.scanning_dialog.move(dialog_geometry.topLeft())

        self.scanning_dialog.open()

        QTimer.singleShot(2000, self.scan_callback)
        app.process_events()

    def scan_callback(self):
        self.CallbackConnector.base.scan_callback(self.link_controller)
        self.update_sensor_list()
        self.update_button_lock_and_metrics()
        self.scanning_dialog.close()
        self.update_button_lock_and_metrics()
        self.link_controller.on_scan(self.CallbackConnector.base.sensors)

    def update_button_lock_and_metrics(self):
        # streaming requires all the following to be met:
        # - triggering is in a valid state
        # - the pipeline is connected or armed
        # - there is at least one RF sensor connected (Link sensors do not count)
        num_sensors = len(self.CallbackConnector.base.sensors)
        can_start = self.CallbackConnector.base.is_ready_to_stream()
        style = "color : white" if can_start else "color : gray"
        self.start_button.setEnabled(can_start)
        self.start_button.setStyleSheet(style)
        self.stop_button.setEnabled(can_start)
        self.stop_button.setStyleSheet(style)
        if num_sensors > 0:
            self.MetricsConnector.sensorsconnected.setText(str(num_sensors))
            self.no_sensor_error.setStyleSheet('QLabel { font-size: 1pt; color: transparent; margin: 0}')
        else:
            self.MetricsConnector.sensorsconnected.setText("-")
            self.no_sensor_error.setStyleSheet('QLabel { font-size: 10pt; color: white; margin: 0}')
            self.SensorModeList.clear()
        self.getpipelinestate()
        self.exportcsv_button.setEnabled(False)
        self.exportcsv_button.setStyleSheet("border: 1px solid gray; color: gray;")
        self.resetModeList([])

    def on_trigger_config_change(self):
        self.update_button_lock_and_metrics()
        self.CallbackConnector.base.reset_before_config = True

    def set_station_type(self, station_type: TrignoType):
        self.station_type = station_type
        match station_type:
            case TrignoType.CENTRO:
                self.trigger_controller = CentroTriggerController(self.on_trigger_config_change, self)
                self.tabs.addTab(self.trigger_controller, "Centro Triggers")
                self.trigger_controller.on_connect()
                self.analog_input_controller = AnalogInputController(self)
                self.tabs.addTab(self.analog_input_controller, "Analog Inputs")
                self.analog_input_controller.set_update_func(self.on_analog_input_change)
                pass
            case TrignoType.BASESTATION:
                self.baseStationTriggerWidget.setVisible(True)
        if self.CallbackConnector.base.TrigBase.IsLinkConnected():
            self.tabs.addTab(self.link_controller, "Trigno Link")
            self.link_controller.get_current_mode_callback = self.CallbackConnector.base.getCurMode
            self.link_controller.set_current_mode_callback = self.update_sensor_mode

    def update_sensor_list(self, should_update: bool = False):
        self.SensorListBox.clear()
        self.CallbackConnector.base.set_sensors_list()
        number_and_names_str = []
        for sensor in self.CallbackConnector.base.sensors:
            str_pair_num = f"({sensor.pair_number}) " if sensor.pair_number > 0 else ""
            number_and_names_str.append(str_pair_num + sensor.name)
            for channel in sensor.channels:
                if channel.is_enabled and not str(channel.channel_type) == "SkinCheck":
                    str_sample_rate = f"({str(round(channel.sample_rate, 3))} Hz)" if channel.sample_rate > 0 else ""
                    channel_info = f"\n      -{channel.name} {str_sample_rate}"
                    number_and_names_str[-1] += channel_info
        self.SensorListBox.addItems(number_and_names_str)

    def start_callback(self):
        self.CallbackConnector.base.Start_Callback(self.starttriggercheckbox.isChecked(),
                                                   self.stoptriggercheckbox.isChecked(),
                                                   self.trigger_controller,
                                                   self.analog_input_controller)

        self.CallbackConnector.resetmetrics()
        self.stop_button.setEnabled(True)
        self.exportcsv_button.setEnabled(False)
        self.exportcsv_button.setStyleSheet("border: 1px solid gray; color: gray;")
        self.getpipelinestate()

    def stop_callback(self):
        self.CallbackConnector.base.Stop_Callback()
        self.getpipelinestate()
        self.exportcsv_button.setEnabled(True)
        self.exportcsv_button.setStyleSheet("color : white")

    def exportcsv_callback(self):
        export = None
        if self.CallbackConnector.streamYTData:
            export = self.CallbackConnector.base.csv_writer.exportYTCSV()
        else:
            export = self.CallbackConnector.base.csv_writer.exportCSV()
        self.getpipelinestate()
        print("CSV Export: " + str(export))

    def sensorList_callback(self):
        current_selected = self.SensorListBox.currentRow()
        if (self.selectedSensor is None or self.selectedSensor != current_selected) and current_selected != -1:
            self.selectedSensor = self.SensorListBox.currentRow()
            modeList = self.CallbackConnector.base.getSampleModes(self.selectedSensor)
            curMode = self.CallbackConnector.base.getCurMode(self.selectedSensor)
            self.resetModeList(modeList)
            if curMode is not None:
                self.SensorModeList.setCurrentText(curMode)
                self.SensorModeList.setToolTip(curMode)
            else:
                self.SensorModeList.setToolTip(self.sensor_mode_default_tooltip)

    def resetModeList(self, mode_list):
        self.SensorModeList.clear()
        for i in range(len(mode_list)):
            self.SensorModeList.addItem(mode_list[i])
            self.SensorModeList.setItemData(i, mode_list[i], QtCore.Qt.ToolTipRole)
        self.SensorModeList.setToolTip(self.sensor_mode_default_tooltip)

    def sensorModeList_callback(self):
        curItem = self.SensorListBox.currentRow()
        curMode = self.CallbackConnector.base.getCurMode(curItem)
        selMode = self.SensorModeList.currentText()
        if curMode != selMode and selMode != '':
            self.update_sensor_mode(curItem, selMode)

    def update_sensor_mode(self, curItem, selMode):
        self.CallbackConnector.base.setSampleMode(curItem, selMode)
        self.getpipelinestate()
        self.update_sensor_list(should_update=False)
        self.sensorList_callback()
        self.SensorModeList.setCurrentText(selMode)
        self.SensorListBox.setCurrentRow(curItem)
        self.SensorModeList.setToolTip(selMode)

    def on_analog_input_change(self):
        self.CallbackConnector.base.configure_analog_input(self.analog_input_controller.get_num_channels(),
                                                           self.analog_input_controller.is_mic_enabled(),
                                                           self.analog_input_controller.get_voltage_ranges(),
                                                           self.analog_input_controller.get_channel_names())
        self.update_sensor_list(should_update=True)
        self.update_button_lock_and_metrics()
        # reset pipeline to ensure that transforms are reapplied upon configuration to the analog input channels
        self.CallbackConnector.base.reset_before_config = True

    def update_stream_type(self):
        self.CallbackConnector.streamYTData = self.yt_checkbox.isChecked()