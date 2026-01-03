# ClubmanHard: UDP 遥测控制器接口

![License: EUPL-1.2](https://img.shields.io/badge/license-EUPL--1.2-blue.svg)
![Platform: RPi 5 + ESP32](https://img.shields.io/badge/platform-RPi%205%20%2B%20ESP32-green.svg)

🇺🇸 **[English Readme](README.md)** | 📖 **[部署手册 (Deployment Guide)](DEPLOY.zh-CN.md)**

> [!IMPORTANT]
> **免责声明：仅供教育研究使用**
>
> 本软件仅作为嵌入式系统通信和硬件接口编程的 **概念验证 (PoC)**。
> 它演示了如何处理加密的 UDP 遥测数据流并将其映射到物理硬件控制信号。
> 作者对硬件损坏、非预期行为或在第三方应用程序中的滥用**概不负责**。
> **使用风险自负。**

## 项目概述
ClubmanHard 是一个探索 **硬件环回控制 (Hardware Loopback Control)** 的技术研究项目。
它的工作原理是接收通用的 UDP 遥测数据包（通过 Salsa20 解密），在 Raspberry Pi 5 上通过 PID 控制回路进行处理，并通过 UART 串口发送控制指令给 ESP32-S3 微控制器。随后，ESP32 模拟标准的低功耗蓝牙 (BLE) 游戏手柄。

### 系统架构
```mermaid
graph LR
    Source["UDP 遥测源"] -- "加密数据流" --> RPi["Raspberry Pi 5 (逻辑层)"]
    RPi -- "串口 (UART)" --> ESP32["ESP32-S3 (HID 模拟器)"]
    ESP32 -- "BLE" --> Receiver["主机系统"]
```

### 核心特性
*   **无头运行 (Headless)**：专为无显示器的 Raspberry Pi 5 设计。
*   **硬件模拟**：使用 ESP32 模拟标准蓝牙手柄 (HID)，确保与主机软件完全隔离。
*   **实时处理**：实时解密和分析 UDP 遥测数据包。
*   **启发式转向控制**：基于相对方向向量实现 PID 转向算法。
*   **自动化输入序列**：支持“盲操作”状态机逻辑，用于用户界面导航的自动化测试。

## 使用说明
请参阅 **[部署手册 (DEPLOY.zh-CN.md)](DEPLOY.zh-CN.md)** 了解详细的硬件接线、固件安装和操作说明。

## 许可证
本项目采用 **EUPL-1.2** (欧盟公共许可证) 开源。
