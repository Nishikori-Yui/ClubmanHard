// Ported and Adapted from ClubmanSharp
// Original Author: ddm999
// Source: https://github.com/ddm999/ClubmanSharp
// License: EUPL-1.2
// Modifications: Adapted for Headless RPi + ESP32 architecture.

using System;
using ClubmanHard.Logic.TrackData;

namespace ClubmanHard.Logic
{
    public enum DrivingState
    {
        Idle,
        Driving,
        Pitbox,
        Lost,
        EmergencyStop
    }

    public class DrivingLogic
    {
        public DrivingState CurrentState { get; private set; } = DrivingState.Idle;
        private TrackDataBase _trackData;
        
        // Logic Constants from Bot.cs
        private const byte LateRaceMaxThrottle = 240;
        private bool _buttonmashToggler = false;
        
        // Fail-Safe
        private DateTime _lastMovementTime = DateTime.Now;
        private bool _isStuck = false;

        public DrivingLogic()
        {
            _trackData = new TokyoClubmanPlusTrackData();
            _lastMovementTime = DateTime.Now;
        }

        public (byte Throttle, byte Brake, sbyte Steering, bool Boost, bool Cross, bool DPadLeft) Update(TelemetryData data)
        {
            if (data.Magic == 0) // No data
            {
                CurrentState = DrivingState.Idle;
                return (0, 0, 0, false, false, false);
            }

            // Convert units to match Bot.cs logic (MPH, Degrees)
            double mph = data.SpeedMeter * 2.23694;
            double rotn = (1 - data.RelativeOrientationToNorth) * 180;
            
            // Fail-Safe Check
            if (mph > 1.0)
            {
                _lastMovementTime = DateTime.Now;
                _isStuck = false;
            }
            else
            {
                if ((DateTime.Now - _lastMovementTime).TotalSeconds > 60)
                {
                    CurrentState = DrivingState.EmergencyStop;
                    return (0, 255, 0, false, false, false); // Full Brake
                }
            }

            // Get Targets
            var (targetMph, targetOrientation) = _trackData.GetTargets(data.Position.X, data.Position.Z, data.LapCount);

            byte throttle = 0;
            byte brake = 0;
            sbyte steering = 0; // -128 to 127 (0 Center)
            bool boost = false;
            bool cross = false;
            bool dpadLeft = false;

            if (targetMph == -1 && targetOrientation == -1)
            {
                // Pitbox Logic
                CurrentState = DrivingState.Pitbox;
                _buttonmashToggler = !_buttonmashToggler;
                if (_buttonmashToggler)
                {
                    dpadLeft = true; // West
                }
                else
                {
                    cross = true; // Confirm
                }
                return (0, 0, 0, false, cross, dpadLeft);
            }

            CurrentState = DrivingState.Driving;

            // --- Throttle / Brake Logic ---
            targetMph += 2; // needed bc of how the acceleration decrease scales

            if (mph > targetMph * 1.2)
            {
                // Full Brake
                brake = 255;
            }
            else if (mph > targetMph)
            {
                // Partial Brake
                var diff = mph - targetMph;
                brake = (byte)(255 - (255 / (targetMph * 0.2) * diff));
                if (brake < 0) brake = 0; // Safety
            }
            else if (mph > targetMph * 0.9)
            {
                // Partial Accel
                var diff = targetMph - mph;
                throttle = (byte)(255 / (targetMph * 0.1) * diff);
            }
            else
            {
                // Full Accel
                if (mph < 150)
                {
                    boost = true; // NOS
                }
                throttle = 255;

                if (data.LapCount > 2)
                {
                    throttle = LateRaceMaxThrottle;
                }
            }

            // --- Steering Logic ---
            // Map 0-255 (Bot.cs) to -128-127 (Our Output)
            // Bot.cs: 0=Left, 128=Center, 255=Right
            // Ours: -128=Left, 0=Center, 127=Right
            // Formula: Our = Bot - 128

            int steeringInput = 128; // Center

            if (targetOrientation == 360.0)
            {
                // Lost / No Turning
                steeringInput = 128;
            }
            else if (targetOrientation < 0.0)
            {
                // Heading West (Left?)
                if (-rotn < targetOrientation - 5.0)
                {
                    // Full Right
                    steeringInput = 255;
                    boost = false; // Override NOS
                }
                else if (-rotn < targetOrientation)
                {
                    // Partial Right
                    var diff = rotn - (-targetOrientation);
                    steeringInput = (int)(128 + (127 / 5.0 * diff));
                }
                else if (-rotn > targetOrientation + 5.0)
                {
                    // Full Left
                    steeringInput = 0;
                    boost = false;
                }
                else if (-rotn > targetOrientation)
                {
                    // Partial Left
                    var diff = (-targetOrientation) - rotn;
                    steeringInput = (int)(128 - (127 / 5.0 * diff));
                }
                else
                {
                    steeringInput = 128;
                }
            }
            else
            {
                // Heading East
                if (rotn < targetOrientation - 5.0)
                {
                    // Full Right
                    steeringInput = 255;
                    boost = false;
                }
                else if (rotn < targetOrientation)
                {
                    // Partial Right
                    var diff = rotn - targetOrientation;
                    steeringInput = (int)(128 - (127 / 5.0 * diff)); // Wait, Bot.cs says 128 - ... for Right?
                    // Let's check Bot.cs line 392: 128 - (127 / 5.0 * diff)
                    // If rotn < target, we need to turn Right (increase angle?)
                    // If Bot.cs says 128-..., that means Left?
                    // Let's trust the source code logic exactly.
                    // Bot.cs Line 394: _ds4.SetAxisValue(DualShock4Axis.LeftThumbX, input);
                    // If input < 128, it's Left. If input > 128, it's Right.
                    // So Bot.cs logic seems to be steering Left when it says "Partial Right"?
                    // Or maybe my understanding of "Right" in Bot.cs context is inverted?
                    // Actually, let's just copy the math exactly.
                    
                    // Re-reading Bot.cs:
                    // Line 392: input = 128 - ...
                    // Line 393: Log "partial right"
                    // So 128 - ... is Right? That would mean 0 is Right?
                    // Standard DS4: 0 is Left, 255 is Right.
                    // Maybe `rotn` logic implies inverted steering?
                    // I will copy the math EXACTLY.
                    steeringInput = (int)(128 - (127 / 5.0 * diff));
                }
                else if (rotn > targetOrientation + 5.0)
                {
                    // Full Left
                    steeringInput = 0;
                    boost = false;
                }
                else if (rotn > targetOrientation)
                {
                    // Partial Left
                    var diff = targetOrientation - rotn;
                    steeringInput = (int)(128 + (127 / 5.0 * diff));
                }
                else
                {
                    steeringInput = 128;
                }
            }

            // Clamp and Convert
            steeringInput = Math.Clamp(steeringInput, 0, 255);
            steering = (sbyte)(steeringInput - 128);

            return (throttle, brake, steering, boost, cross, dpadLeft);
        }
    }
}
