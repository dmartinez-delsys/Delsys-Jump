"""
Manages scan settings for Link sensors
"""
import re

from PySide6.QtGui import QDoubleValidator
from PySide6.QtWidgets import QPushButton, QWidget, QSizePolicy, QLabel, QComboBox, QListWidget, \
    QAbstractItemView, QVBoxLayout, QHBoxLayout, QCheckBox, QLineEdit

class LinkController(QWidget):

    def __init__(self, parent : QWidget):
        QWidget.__init__(self, parent = parent)

        #state variables:
        self.trigno_base = None
        self.use_ant = False #Scan for devices via ANT+ if True, BLE if False
        self.vo2_index = -1 #sensor index of a VO2 master. -1 if not present.
        self.get_current_mode_callback = None
        self.set_current_mode_callback = None

        #GUI elements
        self.layout = QVBoxLayout()

        # Use Bluetooth Low Energy (BLE) or ANT+ connection?
        network_layout = QHBoxLayout()
        self.network_label = QLabel("Scan Type:")
        self.network_label.setToolTip("The method used to connect sensors to this Link device.")
        self.network_combobox = QComboBox(self)
        self.network_combobox.insertItem(0, "Bluetooth")
        self.network_combobox.insertItem(1, "ANT+")
        self.network_combobox.currentIndexChanged.connect(self.set_network_type)
        network_layout.addWidget(self.network_label)
        network_layout.addWidget(self.network_combobox)
        self.layout.addLayout(network_layout)

        # Select Device Types included
        device_types_layout = QHBoxLayout()
        
        self.sensor_types_label = QLabel("Sensor Types", self)
        self.sensor_types_label.setToolTip("The types of sensors searched for during a scan")
        device_types_layout.addWidget(self.sensor_types_label)

        self.select_all_button = QPushButton("Select All", self)
        self.select_all_button.setProperty('class', 'secondaryButton')
        device_types_layout.addWidget(self.select_all_button)
        self.select_none_button = QPushButton("Select None")
        self.select_none_button.setProperty('class', 'secondaryButton')
        device_types_layout.addWidget(self.select_none_button)

        self.layout.addLayout(device_types_layout)
        self.device_type_list = QListWidget(self)
        self.device_type_list.setSelectionMode(QAbstractItemView.MultiSelection)
        self.device_type_list.setSizePolicy(QSizePolicy.Preferred, QSizePolicy.Expanding)
        self.select_all_button.pressed.connect(self.device_type_list.selectAll)
        self.select_none_button.pressed.connect(self.device_type_list.clearSelection)
        self.layout.addWidget(self.device_type_list)

        # User defined weight (For VO2 Master)
        VO2_layout = QHBoxLayout()
        self.VO2_weight_label = QLabel("VO2 Master: Weight (kg)", self)
        self.VO2_weight_label.setToolTip(
            "These changes will overwrite the configuration currently saved on this device. "
            "Recalibration will be required before running a data collection.")
        self.VO2_weight_label.setVisible(False)
        VO2_layout.addWidget(self.VO2_weight_label)
        self.VO2_weight_lineedit = QLineEdit(self)
        self.onlyDouble = QDoubleValidator()
        self.VO2_weight_lineedit.setValidator(self.onlyDouble)
        self.VO2_weight_lineedit.setVisible(False)
        self.VO2_weight_lineedit.setEnabled(False)
        VO2_layout.addWidget(self.VO2_weight_lineedit)
        self.layout.addLayout(VO2_layout)
        self.apply_VO2_weight_button = QPushButton("Apply Changes", self)
        self.apply_VO2_weight_button.setProperty("class", 'secondaryButton')
        self.apply_VO2_weight_button.setEnabled(False)
        self.apply_VO2_weight_button.setVisible(False)
        self.apply_VO2_weight_button.pressed.connect(self.on_vo2_weight_change)
        VO2_layout.addWidget(self.apply_VO2_weight_button)

        self.setLayout(self.layout)

    def on_select_all(self, is_checked: bool):
        if is_checked:
            self.device_type_list.selectAll()
        else:
            self.device_type_list.clearSelection()

    def set_device_type_panel(self):
        self.device_type_list.clear()
        self.device_type_list.addItems(self.trigno_base.get_link_scan_device_options(self.use_ant))

    def set_network_type(self, state: int):
        self.use_ant = state == 1
        self.set_device_type_panel()
        self.on_select_all(False) #deselect all

    def get_device_types(self):
        return [x.text() for x in self.device_type_list.selectedItems()]

    def on_scan(self, sensors: list):
        for sensor_index in range(len(sensors)):
            sensor = sensors[sensor_index]
            if sensor.name == "VO2 Master":
                self.vo2_index = sensor_index
                self.VO2_weight_label.setVisible(True)
                self.VO2_weight_lineedit.setVisible(True)
                self.VO2_weight_lineedit.setEnabled(True)
                self.apply_VO2_weight_button.setVisible(True)
                self.apply_VO2_weight_button.setEnabled(True)

    def on_vo2_weight_change(self):
        new_weight = self.VO2_weight_lineedit.text()
        current_mode = self.get_current_mode_callback(self.vo2_index)
        mode_as_list = re.split(r'[;]', current_mode) #split by semicolon
        mode_as_list[-1] = f" weight = {new_weight}"
        self.set_current_mode_callback(self.vo2_index, ";".join(mode_as_list)) #rejoin with semicolon
