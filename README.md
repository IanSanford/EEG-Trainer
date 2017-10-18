# EEG-Trainer
C# WinForms application for EEG-SMT hardware

This is a GUI application I created as part of my senior project. 
It communicates with a consumer-grade EEG-SMT headset from Olimex to take EEG samples relating to a specific desired machine output.
Each sample is taken while the user "thinks" an output like "up" or "click" which the user selects before recording.
The raw EEG data is stored along with the corresponding input in such a way that it can be used to train a neural network.
