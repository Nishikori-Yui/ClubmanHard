using System;
using System.IO.Ports;

namespace ClubmanHard.Logic
{
    public class SerialController
    {
        private SerialPort _serialPort;
        private const int BaudRate = 115200;

        public SerialController(string portName)
        {
            _serialPort = new SerialPort(portName, BaudRate);
        }

        public void Open()
        {
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

        public void SendControl(byte throttle, byte brake, sbyte steering)
        {
            if (!_serialPort.IsOpen) return;

            byte startByte = 0xFF;
            byte steeringByte = (byte)steering;
            
            // Simple checksum: Throttle + Brake + Steering
            byte checksum = (byte)(throttle + brake + steeringByte);

            byte[] packet = { startByte, throttle, brake, steeringByte, checksum };
            
            try
            {
                _serialPort.Write(packet, 0, packet.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending packet: {ex.Message}");
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
