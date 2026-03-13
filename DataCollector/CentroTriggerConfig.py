"""
Manages the state for start and stop input/output triggers as well as sync outputs, including state and UI
"""
from PySide6.QtCore import Qt
from PySide6.QtGui import QColor, QStandardItem, QStandardItemModel
from PySide6.QtWidgets import QWidget, QGridLayout, QComboBox, QLabel, QPushButton, QDialog, QVBoxLayout, \
    QDialogButtonBox


class CentroTriggerConfig(QWidget):

    CHANNEL_OPTIONS: [list] = [1, 2, 3, 4]
    SYNC_BEACON_FREQ: [list] = [37, 74, 148]
    EDGE_OPTIONS: [list] = ["Rising Edge", "Falling Edge"]
    VOLTAGE_OPTIONS: [list] = ["3.3V", "5V"]
    ENABLED_STYLE = "color : white"
    DISABLED_STYLE = "color : grey"


    def __init__(self, label : str, trigger_type: int, update_func, controller):
        QWidget.__init__(self, parent= controller)
        self.update_func = update_func #manages channel availability
        self.controller = controller
        self.label: str = label
        self.is_enabled: bool = False
        self.is_rising: bool = True
        self.is_5V: bool = False
        self.channel: int = 0
        self.frequency: int = CentroTriggerConfig.SYNC_BEACON_FREQ[1] #default to 74Hz
        self.layout = QGridLayout()
        # 0-> start input, 1-> start output, 2-> stop input, 3-> stop output, 4-> sync output
        self.trigger_type: int = trigger_type

        # ---- Trigger active/inactive button with trigger name
        self.enabled_button = QPushButton(self.label)
        self.enabled_button.setEnabled(False)
        self.enabled_button.setProperty("class", 'primaryButton')
        self.enabled_button.setFixedHeight(50)
        self.enabled_button.setCheckable(True)
        self.enabled_button.clicked.connect(self.on_enable_button_click)
        self.layout.addWidget(self.enabled_button, 0, 0, 1, 2)

        # ---- Voltage
        self.signal_voltage_label = QLabel("Signal Voltage:")
        self.signal_voltage_label.setStyleSheet(CentroTriggerConfig.DISABLED_STYLE)
        self.layout.addWidget(self.signal_voltage_label, 1, 0)

        self.voltage_combo_box = QComboBox(self)
        self.voltage_combo_box.setEnabled(False)
        for i in range(len(CentroTriggerConfig.VOLTAGE_OPTIONS)):
            self.voltage_combo_box.insertItem(i, CentroTriggerConfig.VOLTAGE_OPTIONS[i])
        self.voltage_combo_box.currentIndexChanged.connect(self.on_voltage_select)
        self.layout.addWidget(self.voltage_combo_box, 1, 1)

        # ---- Rising or Falling Edge   /    Frequency
        self.signal_edge_label = QLabel("Signal Edge:")
        self.signal_edge_label.setStyleSheet(CentroTriggerConfig.DISABLED_STYLE)
        self.signal_edge_combo_box = QComboBox()
        self.signal_edge_combo_box.setEnabled(False)
        for i in range(len(CentroTriggerConfig.EDGE_OPTIONS)):
            self.signal_edge_combo_box.insertItem(i, CentroTriggerConfig.EDGE_OPTIONS[i])
        self.signal_edge_combo_box.currentIndexChanged.connect(self.on_edge_select)

        self.frequency_label = QLabel("Frequency:")
        self.frequency_label.setStyleSheet(CentroTriggerConfig.DISABLED_STYLE)
        self.frequency_combo_box = QComboBox()
        self.frequency_combo_box.setEnabled(False)
        for i in range(len(CentroTriggerConfig.SYNC_BEACON_FREQ)):
            self.frequency_combo_box.insertItem(i, str(CentroTriggerConfig.SYNC_BEACON_FREQ[i]) + " Hz")
        self.frequency_combo_box.setCurrentIndex(1)  # set default to 74Hz
        self.frequency_combo_box.currentIndexChanged.connect(self.on_frequency_select)
        if trigger_type == 4:
            self.layout.addWidget(self.frequency_label, 2, 0)
            self.layout.addWidget(self.frequency_combo_box, 2, 1)
        else:
            self.layout.addWidget(self.signal_edge_label, 2, 0)
            self.layout.addWidget(self.signal_edge_combo_box, 2, 1)

        # --- Channel
        self.channel_label = QLabel("Channel:", self)
        self.channel_label.setStyleSheet(CentroTriggerConfig.DISABLED_STYLE)
        self.layout.addWidget(self.channel_label, 3, 0)

        self.channel_combo_box = QComboBox(self)
        self.channel_combo_box.setEnabled(False)
        self.standard_item_model = QStandardItemModel()
        self.channel_options = [
            QStandardItem("1"), QStandardItem("2"), QStandardItem("3"), QStandardItem("4")]
        for i in range(len(self.channel_options)):
            self.standard_item_model.appendRow(self.channel_options[i])
        self.channel_combo_box.setModel(self.standard_item_model)
        self.channel_combo_box.setCurrentIndex(-1)  # <- set the initial value to a blank value before connecting
        self.channel_combo_box.currentIndexChanged.connect(self.on_channel_select)
        self.layout.addWidget(self.channel_combo_box, 3, 1)

        self.setLayout(self.layout)

    def on_connect(self, is_connected: bool):
        style = CentroTriggerConfig.ENABLED_STYLE if is_connected else CentroTriggerConfig.DISABLED_STYLE
        self.enabled_button.setStyleSheet(style)
        self.enabled_button.setEnabled(is_connected)

    def on_enable_button_click(self, is_enabled):
        self.is_enabled = is_enabled
        style = CentroTriggerConfig.ENABLED_STYLE if is_enabled else CentroTriggerConfig.DISABLED_STYLE

        # set the index to the first open channel
        enabled_options = [i for i in range(len(self.channel_options)) if self.channel_options[i].isEnabled()]
        if len(enabled_options) == 0:
            self.enabled_button.setChecked(False)
            dialog = QDialog(self)
            dialog.setWindowTitle("all channels in use")
            message = QLabel("All channels in use. \nSet the start input trigger and \nstop input trigger to the same channel to continue.")
            message.setAlignment(Qt.AlignCenter)

            buttonBox = QDialogButtonBox(QDialogButtonBox.Ok | QDialogButtonBox.Cancel)
            buttonBox.accepted.connect(dialog.accept)
            buttonBox.rejected.connect(dialog.reject)

            dialog.layout = QVBoxLayout()
            dialog.layout.addWidget(message)
            dialog.layout.addWidget(buttonBox)
            dialog.setLayout(dialog.layout)
            dialog.exec()
            self.is_enabled = False
            return
        elif is_enabled:
            self.channel_combo_box.setCurrentIndex(enabled_options[0])
        else:
            self.channel_combo_box.setCurrentIndex(-1)

        self.signal_voltage_label.setStyleSheet(style)
        self.voltage_combo_box.setEnabled(is_enabled)

        self.channel_label.setStyleSheet(style)
        self.channel_combo_box.setEnabled(is_enabled)

        self.signal_edge_label.setStyleSheet(style)
        self.signal_edge_combo_box.setEnabled(is_enabled)

        self.frequency_label.setStyleSheet(style)
        self.frequency_combo_box.setEnabled(is_enabled)

        self.update_func()

    def on_voltage_select(self, choice):
        self.is_5V = choice == 1

    def on_edge_select(self, choice):
        self.is_rising = choice == 0

    def on_channel_select(self, choice):
        self.channel = choice + 1
        self.update_func()

    def on_frequency_select(self, choice):
        self.frequency = CentroTriggerConfig.SYNC_BEACON_FREQ[choice]

    def set_channel_choices(self, available_channels : list):
        for i in range(len(available_channels)):
            is_enabled = available_channels[i]
            self.channel_options[i].setSelectable(is_enabled)
            self.channel_options[i].setForeground(QColor('black') if is_enabled else QColor('grey'))
            self.channel_options[i].setEnabled(is_enabled)
