"""
Single Widget for one channel in the analog input config tab.
"""
from PySide6.QtWidgets import QLabel, QComboBox, QWidget, QGridLayout, QLineEdit


class AnalogInput(QWidget):
    def __init__(self, label: str, voltage_options: list, frequency: int, parent : QWidget):
        QWidget.__init__(self, parent= parent)
        #State Variables
        self.voltage_index = 0
        self.voltage_options: list
        self.channel_name = label
        self.update_func = None
        self.layout = QGridLayout()

        self.label : QLineEdit = QLineEdit(self)
        self.label.setText(self.channel_name)
        self.label.editingFinished.connect(self.on_channel_label_change)
        self.layout.addWidget(self.label, 0, 0)

        self.freq_label: QLabel
        self.update_frequency_options(frequency)
        self.layout.addWidget(self.freq_label, 0, 1)

        voltage_label = QLabel("Input Range:", self)
        self.layout.addWidget(voltage_label, 1, 0)

        self.voltage_combobox = QComboBox(self)
        self.voltage_combobox.activated.connect(self.on_voltage_change)
        self.update_voltage_options(voltage_options)
        self.layout.addWidget(self.voltage_combobox, 1, 1)

        self.setLayout(self.layout)
        self.setMaximumHeight(80)

    def update_frequency_options(self, frequency: int):
        label = "Frequency: " + str(frequency) + "KHz" if frequency > 0 else "Frequency: None"
        self.freq_label = QLabel(label, self)
        self.layout.addWidget(self.freq_label, 0, 1)

    def on_voltage_change(self, new_index: int):
        self.voltage_index = new_index
        self.update_func()

    def set_update_func(self, update_func):
        self.update_func = update_func

    def update_voltage_options(self, voltage_options: list):
        self.voltage_options = voltage_options
        self.voltage_combobox.clear()
        self.voltage_combobox.addItems(["± "+ str(x)+"V" for x in self.voltage_options])
        self.voltage_combobox.setCurrentIndex(0)

    def get_voltage_index(self):
        return self.voltage_index

    def update_label(self, new_label):
        self.channel_name = new_label
        self.label.setText(self.channel_name)

    def on_channel_label_change(self):
        self.channel_name = self.label.text()
        self.update_func()
