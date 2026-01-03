# ClubmanHard Deployment Guide

🔙 **[Back to README](README.md)** | 🇨🇳 **[中文部署手册](DEPLOY.zh-CN.md)**

## 1. Bill of Materials (BOM)
*   **Raspberry Pi 5** (Running Linux, e.g., Raspberry Pi OS Lite)
*   **ESP32-S3 Development Board** (Specific Model: **DNESP32S3M**)
*   **USB Receiver** (Generic HID or Xbox-compatible receiver for the host system)
*   Jumper Wires

## 2. Hardware Wiring
Connect the Raspberry Pi and ESP32 using the following pinout configuration:

| Raspberry Pi 5 (GPIO) | ESP32-S3 (Pin) | Description |
| :--- | :--- | :--- |
| **5V** (Pin 2 or 4) | **5V** | Power Supply |
| **GND** (Pin 6) | **GND** | Ground |
| **GPIO 14** (TX) | **IO 4** (RX) | Data Transmit |
| **GPIO 15** (RX) | **IO 5** (TX) | Data Receive |
| **GPIO 17** | **EN / RST** | Hardware Reset Control |

## 3. System Configuration (RPi)
Enable UART and disable the serial console to ensure clean data transmission.

1.  **Edit Config**:
    ```bash
    sudo nano /boot/firmware/config.txt
    ```
    Add: `enable_uart=1`

2.  **Disable Console**:
    ```bash
    sudo raspi-config
    ```
    *   **Interface Options** -> **Serial Port**.
    *   Login Shell: **NO**.
    *   Hardware Enabled: **YES**.
    *   **Reboot**.

3.  **Permissions**:
    ```bash
    sudo chmod 666 /dev/serial0
    ```

## 4. Installation & Build

### 4.1 ESP32 Firmware
1.  Connect ESP32 to PC/RPi.
2.  Navigate to firmware directory:
    ```bash
    cd src/ClubmanHard.Firmware
    ```
3.  Flash firmware:
    ```bash
    pio run -t upload
    ```

### 4.2 Control Logic (RPi)
Build the standalone executable for Linux ARM64:

```bash
dotnet publish src/ClubmanHard.Logic/ClubmanHard.Logic.csproj -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true -o ./deploy
```

Transfer the `deploy` folder to your Raspberry Pi.

## 5. Operation

### Step 1: Diagnostic Mode (Safety Check)
Before full operation, verify the hardware link and controller mapping.

```bash
cd ~/ClubmanHard
sudo ./ClubmanHard.Logic --test-hardware
```
*   Use the menu to test axes and buttons.
*   Verify inputs are registered on the host system's controller settings.

### Step 2: Production Mode
Start the telemetry processing loop:

```bash
sudo ./ClubmanHard.Logic --port=/dev/serial0
```

*   **Fail-Safe**: The system includes a safety feature. If the calculated speed is < 1.0 unit for > 60 seconds, it triggers an **Emergency Stop**.
*   **Logging**: Events are recorded in `clubman_log.txt`.
