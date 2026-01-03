# ClubmanHard 部署手册

🔙 **[返回主页](README.zh-CN.md)** | 🇺🇸 **[English Guide](DEPLOY.md)**

## 1. 硬件清单 (BOM)
*   **Raspberry Pi 5** (运行 Linux，推荐 Raspberry Pi OS Lite)
*   **ESP32-S3 开发板** (具体型号: **DNESP32S3M**)
*   **USB 接收器** (通用 HID 或兼容 Xbox 的接收器，用于主机系统)
*   杜邦线若干

## 2. 硬件接线
请按照以下引脚定义连接 Raspberry Pi 和 ESP32：

| Raspberry Pi 5 (GPIO) | ESP32-S3 (Pin) | 说明 |
| :--- | :--- | :--- |
| **5V** (Pin 2 或 4) | **5V** | 供电 |
| **GND** (Pin 6) | **GND** | 共地 |
| **GPIO 14** (TX) | **IO 4** (RX) | 数据发送 |
| **GPIO 15** (RX) | **IO 5** (TX) | 数据接收 |
| **GPIO 17** | **EN / RST** | 硬件复位控制 |

## 3. 系统配置 (RPi)
启用 UART 并禁用串口控制台，以确保数据传输纯净。

1.  **修改配置文件**:
    ```bash
    sudo nano /boot/firmware/config.txt
    ```
    添加: `enable_uart=1`

2.  **禁用控制台**:
    ```bash
    sudo raspi-config
    ```
    *   **Interface Options** -> **Serial Port**.
    *   Login Shell (登录 Shell): **NO**.
    *   Hardware Enabled (启用硬件): **YES**.
    *   **重启**.

3.  **权限设置**:
    ```bash
    sudo chmod 666 /dev/serial0
    ```

## 4. 安装与构建

### 4.1 ESP32 固件
1.  将 ESP32 连接到电脑或 RPi。
2.  进入固件目录：
    ```bash
    cd src/ClubmanHard.Firmware
    ```
3.  烧录固件：
    ```bash
    pio run -t upload
    ```

### 4.2 控制逻辑 (RPi)
构建适用于 Linux ARM64 的独立可执行文件：

```bash
dotnet publish src/ClubmanHard.Logic/ClubmanHard.Logic.csproj -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true -o ./deploy
```

将生成的 `deploy` 文件夹传输到 Raspberry Pi。

## 5. 运行操作

### 步骤 1: 诊断模式 (安全检查)
在正式运行前，验证硬件链路和控制器映射。

```bash
cd ~/ClubmanHard
sudo ./ClubmanHard.Logic --test-hardware
```
*   使用菜单测试轴和按键。
*   在主机系统的控制器设置中验证输入是否被识别。

### 步骤 2: 生产模式
启动遥测处理循环：

```bash
sudo ./ClubmanHard.Logic --port=/dev/serial0
```

*   **安全机制 (Fail-Safe)**: 系统包含安全功能。如果计算出的速度低于 1.0 单位持续超过 60 秒，将触发 **紧急停止 (Emergency Stop)**。
*   **日志**: 事件记录在 `clubman_log.txt` 中。
