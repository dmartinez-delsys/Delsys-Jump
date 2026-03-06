"""
Manages Analog input and Mic input configuration options for Trigno Centros
"""
from PySide6.QtWidgets import QWidget, QComboBox, QVBoxLayout, QHBoxLayout, QLabel

from DataCollector.AnalogInput import AnalogInput

class AnalogInputController(QWidget):
    # index indicates the byte value the API designates for the given voltage option
    MIC_VOLTAGE_OPTIONS = [0.09]
    ANALOG_IN_VOLTAGE_OPTIONS = [10 , 5, 1, 0.1]
    CHANNEL_OPTIONS = [
        # label, frequency (in KiloHertz), and number of channels the option is applicable to
        ("None"        ,  0, 0),
        ("Mic Input"   , 48, 1),
        ("Channel 1"   , 48, 1),
        ("Channels 1-2", 24, 2),
        ("Channels 1-4", 12, 4),
        ("Channels 1-6",  6, 6)
    ]

    def __init__(self, parent : QWidget):
        QWidget.__init__(self, parent= parent)
        #State Variables
        self.channel_card_widget = None
        self.channel_cards = None
        self.channel_choice = 0 #index of channel options selected
        self.voltage_choices = list() #voltages selected
        self.call_back_func = None
        #Create GUI
        self.layout = QVBoxLayout()
        # Channel input type selection
        channel_choice_layout = QHBoxLayout()
        input_channels_label = QLabel("Input Channels:", self)
        channel_choice_layout.addWidget(input_channels_label)
        channel_choice_combobox = QComboBox()
        for i in range(len(self.CHANNEL_OPTIONS)):
            channel_choice_combobox.insertItem(i, self.CHANNEL_OPTIONS[i][0])
        channel_choice_combobox.currentIndexChanged.connect(self.on_channel_count_update)
        channel_choice_layout.addWidget(channel_choice_combobox)
        self.layout.addLayout(channel_choice_layout)

        # Create each channel config card
        self.analog_input_channels = []
        for i in range(0, 6):
            self.analog_input_channels.append(AnalogInput("Analog_In_" + str(i + 1),
                                                          self.ANALOG_IN_VOLTAGE_OPTIONS,
                                                          self.CHANNEL_OPTIONS[self.channel_choice][1],
                                                          self))
            self.layout.addWidget(self.analog_input_channels[i])
        self.on_channel_count_update(self.channel_choice)
        self.setLayout(self.layout)

    def get_voltage_ranges(self):
        return [a.get_voltage_index() for a in self.analog_input_channels]

    def get_channel_names(self):
        return [a.channel_name for a in self.analog_input_channels]

    def get_num_channels(self):
        return self.CHANNEL_OPTIONS[self.channel_choice][2]

    def is_mic_enabled(self):
        return self.channel_choice == 1

    def set_update_func(self, update_func):
        self.call_back_func = update_func
        for analog_input in self.analog_input_channels:
            analog_input.set_update_func(update_func)

    # update drop down selects for applicable channels
    def on_channel_count_update(self, new_index):
        self.channel_choice = new_index
        for i in range(len(self.analog_input_channels)):
            is_enabled = i < self.CHANNEL_OPTIONS[self.channel_choice][2]
            self.analog_input_channels[i].setEnabled(is_enabled)
            self.analog_input_channels[i].update_frequency_options(
                self.CHANNEL_OPTIONS[self.channel_choice][1] if is_enabled else 0)
            self.analog_input_channels[i].update_voltage_options(self.ANALOG_IN_VOLTAGE_OPTIONS)
        if self.channel_choice == 1:
            self.analog_input_channels[0].update_voltage_options(self.MIC_VOLTAGE_OPTIONS)
            self.analog_input_channels[0].update_label("Mic_In")
        else:
            self.analog_input_channels[0].update_label("Analog_In_1")
        if self.call_back_func is not None:
            self.call_back_func()
