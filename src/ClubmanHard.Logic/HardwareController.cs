using System;
using System.Device.Gpio;
using System.IO.Ports;
using System.Threading;

namespace ClubmanHard.Logic
{
    public class HardwareController : IControllerOutput
    {
        private SerialPort _serialPort;
        private const int BaudRate = 115200;
        private readonly string _portName;
        private const int ResetPin = 17;

        public HardwareController(string portName)
        {
            _portName = portName;
            _serialPort = new SerialPort(portName, BaudRate);
        }

        public void Open()
        {
            // GPIO Reset Sequence
            try
            {
                Console.WriteLine("Initializing ESP32 via GPIO...");
                using (var controller = new GpioController())
                {
                    controller.OpenPin(ResetPin, PinMode.Output);
                    
                    // Pull Low (Reset)
                    controller.Write(ResetPin, PinValue.Low);
                    Thread.Sleep(100);
                    
                    // Pull High (Boot)
                    controller.Write(ResetPin, PinValue.High);
                    Console.WriteLine("ESP32 Reset Complete. Waiting for boot...");
                    Thread.Sleep(2000); // Wait for BLE Init
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GPIO Error (Are you root?): {ex.Message}");
            }

            // Open Serial
            try
            {
                _serialPort.Open();
                Console.WriteLine($"Connected to ESP32 on {_serialPort.PortName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening serial port: {ex.Message}");
            }
        }

        public void SendControl(byte throttle, byte brake, sbyte steering, bool boost, bool cross = false, bool circle = false, bool dpadUp = false, bool dpadDown = false, bool dpadLeft = false, bool dpadRight = false)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            // Packet Format: [Start(0xFF), Throttle, Brake, Steering, Buttons, Checksum]
            byte buttons = 0;
            if (cross) buttons |= 0x01;
            if (circle) buttons |= 0x02;
            if (dpadRight) buttons |= 0x04;
            if (dpadLeft) buttons |= 0x08;
            if (dpadUp) buttons |= 0x10;
            if (dpadDown) buttons |= 0x20;

            byte checksum = (byte)((throttle + brake + (byte)steering + buttons) & 0xFF);
            byte[] packet = { 0xFF, throttle, brake, (byte)steering, buttons, checksum };

            try
            {
                _serialPort.Write(packet, 0, packet.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Hardware] Serial Write Error: {ex.Message}");
            }
        }

        public void Close()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }
    }
}
