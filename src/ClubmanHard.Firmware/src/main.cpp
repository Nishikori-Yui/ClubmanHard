#include <Arduino.h>
#include <BleGamepad.h>

// Configuration
#define SERIAL_BAUD 115200
#define PACKET_SIZE 5
#define START_BYTE 0xFF

// Pin Definitions (ESP32-S3)
#define RX_PIN 4 // Connect to RPi TX
#define TX_PIN 5 // Connect to RPi RX

// BLE Gamepad Configuration
BleGamepadConfiguration bleGamepadConfig;
BleGamepad bleGamepad("Xbox Wireless Controller", "Microsoft", 100);

// Serial Buffer
uint8_t buffer[PACKET_SIZE];

void setup() {
  // Initialize Serial1 on custom pins
  Serial1.begin(SERIAL_BAUD, SERIAL_8N1, RX_PIN, TX_PIN);

  // Configure BLE Gamepad
  bleGamepadConfig.setControllerType(CONTROLLER_TYPE_GAMEPAD);
  bleGamepadConfig.setVid(0x045E);
  bleGamepadConfig.setPid(0x02FD);
  bleGamepadConfig.setAxesMin(0x0000); // 0
  bleGamepadConfig.setAxesMax(0x7FFF); // 32767

  // Initialize BLE Gamepad
  bleGamepad.begin(&bleGamepadConfig);

  Serial1.println("ClubmanHard Firmware Started");
}

void processPacket() {
  uint8_t throttle = buffer[1];
  uint8_t brake = buffer[2];
  int8_t steering = (int8_t)buffer[3]; // -128 to 127
  uint8_t checksum = buffer[4];

  // Verify Checksum (Simple sum of payload)
  uint8_t calculatedChecksum = (throttle + brake + (uint8_t)steering) & 0xFF;

  if (calculatedChecksum == checksum) {
    if (bleGamepad.isConnected()) {
      // Map inputs to Xbox Controller
      // Throttle -> Z Axis (0-255 -> 0-32767)
      // Brake -> Rz Axis (0-255 -> 0-32767)
      // Steering -> Left Stick X (-128..127 -> -32767..32767)

      int16_t throttleMapped = map(throttle, 0, 255, 0, 32767);
      int16_t brakeMapped = map(brake, 0, 255, 0, 32767);
      int16_t steeringMapped = map(steering, -128, 127, -32767, 32767);

      bleGamepad.setZ(throttleMapped);
      bleGamepad.setRZ(brakeMapped);
      bleGamepad.setLeftThumb(steeringMapped, 0);
      bleGamepad.sendReport();
    }
  } else {
    // Serial1.println("Checksum Error");
  }
}

void loop() {
  if (Serial1.available() >= PACKET_SIZE) {
    if (Serial1.read() == START_BYTE) {
      buffer[0] = START_BYTE;
      // Read remaining bytes
      for (int i = 1; i < PACKET_SIZE; i++) {
        buffer[i] = Serial1.read();
      }
      processPacket();
    }
  }
}
