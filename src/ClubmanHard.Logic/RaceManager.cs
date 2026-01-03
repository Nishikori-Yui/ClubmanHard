// Ported and Adapted from ClubmanSharp
// Original Author: ddm999
// Source: https://github.com/ddm999/ClubmanSharp
// License: EUPL-1.2
// Modifications: Adapted for Headless RPi + ESP32 architecture.

using System;
using System.Threading.Tasks;

namespace ClubmanHard.Logic
{
    public enum RaceState
    {
        Racing,
        Finished,
        MenuNavigation
    }

    public class RaceManager
    {
        public RaceState CurrentState { get; private set; } = RaceState.Racing;
        private DateTime _lastMovementTime = DateTime.Now;
        private bool _isNavigating = false;

        // Delays from Bot.cs
        private const int ShortDelay = 250;
        private const int ButtonHold = 50;

        public async Task Update(TelemetryData data, IControllerOutput controller)
        {
            if (_isNavigating) return;

            float speedKmh = data.SpeedMeter * 3.6f;

            // Race Finish Detection
            // If speed is near 0 for > 15 seconds, assume race finished
            if (speedKmh > 1.0f)
            {
                _lastMovementTime = DateTime.Now;
                CurrentState = RaceState.Racing;
            }
            else
            {
                if ((DateTime.Now - _lastMovementTime).TotalSeconds > 15)
                {
                    CurrentState = RaceState.Finished;
                    await StartMenuSequence(controller);
                }
            }
        }

        private async Task StartMenuSequence(IControllerOutput controller)
        {
            _isNavigating = true;
            CurrentState = RaceState.MenuNavigation;
            Console.WriteLine("[RaceManager] Starting Blind Menu Sequence...");

            // 1. Wait for Loading Screen / Post Race (35s safe buffer)
            await Task.Delay(35000);

            // --- PostRaceInputRunner Logic ---
            Console.WriteLine("[RaceManager] Running PostRace Macros...");
            
            // Circle x 5 (Cancel/Exit)
            for (int i = 0; i < 5; i++)
            {
                controller.SendControl(0, 0, 0, false, circle: true);
                await Task.Delay(ButtonHold);
                controller.SendControl(0, 0, 0, false, circle: false);
                await Task.Delay(ShortDelay);
            }

            // Left x 1 (Quit -> Retry)
            controller.SendControl(0, 0, 0, false, dpadLeft: true);
            await Task.Delay(ButtonHold);
            controller.SendControl(0, 0, 0, false, dpadLeft: false);
            await Task.Delay(ShortDelay);

            // Cross x 1 (Retry)
            controller.SendControl(0, 0, 0, false, cross: true);
            await Task.Delay(ButtonHold);
            controller.SendControl(0, 0, 0, false, cross: false);
            await Task.Delay(ShortDelay);

            // --- Race Loading ---
            Console.WriteLine("[RaceManager] Waiting for Race Load (45s)...");
            await Task.Delay(45000);

            // --- PreRaceInputRunner Logic ---
            Console.WriteLine("[RaceManager] Running PreRace Macros...");

            // Circle x 6 (Ensure we are at top level)
            for (int i = 0; i < 6; i++)
            {
                controller.SendControl(0, 0, 0, false, circle: true);
                await Task.Delay(ButtonHold);
                controller.SendControl(0, 0, 0, false, circle: false);
                await Task.Delay(ShortDelay);
            }

            // Left x 8 (Move to Weather Radar?)
            for (int i = 0; i < 8; i++)
            {
                controller.SendControl(0, 0, 0, false, dpadLeft: true);
                await Task.Delay(ButtonHold);
                controller.SendControl(0, 0, 0, false, dpadLeft: false);
                await Task.Delay(ShortDelay);
            }

            // Right x 1 (Move to Start)
            controller.SendControl(0, 0, 0, false, dpadRight: true);
            await Task.Delay(ButtonHold);
            controller.SendControl(0, 0, 0, false, dpadRight: false);
            await Task.Delay(ShortDelay);

            // Cross x 1 (Start Race)
            controller.SendControl(0, 0, 0, false, cross: true);
            await Task.Delay(ButtonHold);
            controller.SendControl(0, 0, 0, false, cross: false);
            await Task.Delay(ShortDelay);

            // Reset
            CurrentState = RaceState.Racing;
            _lastMovementTime = DateTime.Now;
            _isNavigating = false;
            Console.WriteLine("[RaceManager] Race Started!");
        }
    }
}
