"""
Manages channels available for TriggerConfig instances
"""
import numpy as np
from PySide6.QtWidgets import QWidget, QGridLayout

from DataCollector.CentroTriggerConfig import CentroTriggerConfig

class CentroTriggerController(QWidget):
    def __init__(self, gui_update_func, parent: QWidget):
        QWidget.__init__(self, parent= parent)
        self.gui_update_func = gui_update_func
        # GUI elements
        self.start_input = CentroTriggerConfig("Start Input Trigger", 0, self.channel_choice_updated, self)
        self.stop_input = CentroTriggerConfig("Stop Input Trigger", 1, self.channel_choice_updated, self)
        self.stop_input.signal_edge_combo_box.setCurrentIndex(1) #set default to falling edge
        self.start_output = CentroTriggerConfig("Start Output Trigger", 2, self.channel_choice_updated, self)
        self.stop_output = CentroTriggerConfig("Stop Output Trigger", 3, self.channel_choice_updated, self)
        self.stop_output.signal_edge_combo_box.setCurrentIndex(1) #set default to falling edge
        self.sync_output = CentroTriggerConfig("Sync Output Beacon", 4, self.channel_choice_updated, self)
        self.layout = QGridLayout()

        self.config_panel = self.create_centro_trigger_panel()
        self.layout.addWidget(self.config_panel, 1, 0)

        self.setLayout(self.layout)

    def channel_choice_updated(self):
        #start and stop input triggers can share a channel,
        # all other trigger channel choices must be mutually exclusive
        rows, cols = (4,4)
        options = [[True for _ in range(cols)] for _ in range(rows)]
        options = np.array(options)
        if self.start_input.is_enabled and self.start_input.channel != 0:
            options[:, self.start_input.channel-1] = False
            options[0][self.start_input.channel-1] = True
        if self.stop_input.is_enabled and self.stop_input.channel != 0:
            options[:, self.stop_input.channel-1] = False
            options[0][self.stop_input.channel-1] = True
        if self.start_output.is_enabled and self.start_output.channel != 0:
            options[:, self.start_output.channel-1] = False
            options[1][self.start_output.channel-1] = True
        if self.stop_output.is_enabled and self.stop_output.channel != 0:
            options[:, self.stop_output.channel-1] = False
            options[2][self.stop_output.channel-1] = True
        if self.sync_output.is_enabled and self.sync_output.channel != 0:
            options[:, self.sync_output.channel-1] = False
            options[3][self.sync_output.channel-1] = True
        self.start_input.set_channel_choices(options[0].tolist())
        self.stop_input.set_channel_choices(options[0].tolist())
        self.start_output.set_channel_choices(options[1].tolist())
        self.stop_output.set_channel_choices(options[2].tolist())
        self.sync_output.set_channel_choices(options[3].tolist())
        self.gui_update_func()

    def on_connect(self):
        self.start_input.on_connect(True)
        self.start_output.on_connect(True)
        self.stop_input.on_connect(True)
        self.stop_output.on_connect(True)
        self.sync_output.on_connect(True)

    def get_centro_trigger_config(self):
        triggers = list()
        for t in [self.start_input, self.start_output, self.stop_input, self.stop_output]:
            triggers.append([t.is_enabled, t.channel, t.is_5V, t.is_rising, t.trigger_type])
        return triggers

    def get_sync_output(self):
        return [self.sync_output.is_enabled,self.sync_output.channel,self.sync_output.is_5V,self.sync_output.frequency]

    def create_centro_trigger_panel(self):
        widget = QWidget()
        widget.layout = QGridLayout()
        widget.layout.addWidget(self.start_input, 0, 0)
        widget.layout.addWidget(self.start_output, 0, 1)
        widget.layout.addWidget(self.stop_input, 1, 0)
        widget.layout.addWidget(self.stop_output, 1, 1)
        widget.layout.addWidget(self.sync_output, 2, 0, 1, 2)
        widget.setLayout(widget.layout)
        return widget
