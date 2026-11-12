# ProjectPPIE

Unity first-person kitchen scene driven by a **physical ESP32 controller** (sensors, joystick, buttons) over OSC.

Firmware for the controller: [UnityController](https://github.com/Z1CC4/UnityController).

Course project for **Prototyping Physical Interactive Experiences** (PPIE) at the Free University of Bozen-Bolzano.

## What it is

A small interactive environment (kitchen / apartment) where you move, look around, and trigger water and fire effects from real sensors — not only from keyboard. Input is abstracted so the same scene can run with:

- **ESP32** (`ESP32InputProvider`, `ESP32OSCReceiver`, move/look bridges, heartbeat)
- **Keyboard** (`KeyboardInputProvider`) for development without hardware

Custom gameplay scripts live under `Assets/Scripts/` (input, interaction, kitchen state, water/fire visuals). Third-party packs (kitchen models, VFX, Starter Assets) are used as the scene art.

## Requirements

- Unity (URP; open the project with the version stored in `ProjectSettings/ProjectVersion.txt`)
- Optional: flashed [UnityController](https://github.com/Z1CC4/UnityController) on an Adafruit ESP32 Feather V2, PC joined to the board’s Wi-Fi AP

## Run

1. Clone and open this folder as a Unity project.
2. For keyboard-only testing, use the keyboard input provider in the scene.
3. For the physical setup: start the ESP32 firmware, connect to `ESP32-AP`, then play the scene so OSC receivers can bind.

## Related repo

[UnityController](https://github.com/Z1CC4/UnityController) — ESP32 firmware, OSC messages, OLED status.

## Author

Andrea Zicarelli ([@Z1CC4](https://github.com/Z1CC4))
