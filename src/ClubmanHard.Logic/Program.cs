// Ported and Adapted from ClubmanSharp
// Original Author: ddm999
// Source: https://github.com/ddm999/ClubmanSharp
// License: EUPL-1.2
// Modifications: Adapted for Headless RPi + ESP32 architecture.

// DISCLAIMER: EDUCATIONAL USE ONLY
// This software is a Proof of Concept (PoC) for hardware interface programming (RPi + ESP32).
// It is NOT intended to be used for violating the Terms of Service of any game or platform.
// The author takes NO responsibility for any bans, account suspensions, or hardware damage.
// Use at your own risk.

using System;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace ClubmanHard.Logic
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Argument Parsing
            bool debugMode = false;
            string portName = "/dev/serial0";

            foreach (var arg in args)
            {
                if (arg == "--debug") debugMode = true;
                if (arg.StartsWith("--port=")) portName = arg.Substring(7);
            }

            // UI Initialization
            AnsiConsole.Write(
                new FigletText("ClubmanHard")
                    .Color(Color.Red));

            var rule = new Rule(debugMode ? "[yellow]DEBUG MODE[/]" : "[green]HARDWARE MODE[/]");
            rule.Justification = Justify.Left;
            AnsiConsole.Write(rule);

            // Controller Setup
            IControllerOutput controller;
            if (debugMode)
            {
                controller = new MockController();
            }
            else
            {
                controller = new HardwareController(portName);
            }

            try
            {
                controller.Open();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to open controller: {ex.Message}[/]");
                if (!debugMode) return;
            }

            // Sim Interface Setup
            var sim = new SimInterface();
            sim.Start();

            // Driving Logic Setup
            var logic = new DrivingLogic();
            var raceManager = new RaceManager();

            // Main Loop
            await AnsiConsole.Live(GetGrid(sim, logic, raceManager, debugMode))
                .AutoClear(false)
                .Overflow(VerticalOverflow.Ellipsis)
                .Cropping(VerticalOverflowCropping.Bottom)
                .StartAsync(async ctx => 
                {
                    while (true)
                    {
                        // Update Race Manager (Menu Navigation)
                        await raceManager.Update(sim.CurrentTelemetry, controller);

                        if (raceManager.CurrentState == RaceState.Racing)
                        {
                            // Update Driving Logic
                            var (throttle, brake, steering, boost, cross, dpadLeft) = logic.Update(sim.CurrentTelemetry);
                            controller.SendControl(throttle, brake, steering, boost, cross: cross, dpadLeft: dpadLeft);
                        }
                        else
                        {
                            // In Menu/Finished state, Logic is paused
                            // RaceManager handles inputs (if implemented)
                        }

                        // Update UI
                        ctx.UpdateTarget(GetGrid(sim, logic, raceManager, debugMode));
                        
                        await Task.Delay(50);
                    }
                });

            controller.Close();
            sim.Stop();
        }

        static Grid GetGrid(SimInterface sim, DrivingLogic logic, RaceManager raceManager, bool debugMode)
        {
            var telemetry = sim.CurrentTelemetry;
            var connected = sim.IsConnected;

            var grid = new Grid();
            grid.AddColumn();
            grid.AddColumn();

            // Telemetry Panel
            var telemetryTable = new Table().Border(TableBorder.Rounded).Title("Telemetry");
            telemetryTable.AddColumn("Metric");
            telemetryTable.AddColumn("Value");
            telemetryTable.AddRow("Status", connected ? "[green]CONNECTED[/]" : "[red]WAITING[/]");
            telemetryTable.AddRow("Race State", $"[yellow]{raceManager.CurrentState}[/]");
            telemetryTable.AddRow("Drive State", $"[blue]{logic.CurrentState}[/]");
            telemetryTable.AddRow("Speed", $"{telemetry.SpeedMeter * 3.6f:F1} km/h"); // m/s to km/h
            telemetryTable.AddRow("RPM", $"{telemetry.EngineRPM:F0}");
            telemetryTable.AddRow("Gear", $"{telemetry.Gear}");

            // Input Panel (Visualizing what we are sending)
            // We need to capture the last sent values, or just re-calculate for display (minor overhead)
            var (t, b, s, boost, cross, dpadLeft) = logic.Update(telemetry); 

            var inputTable = new Table().Border(TableBorder.Rounded).Title("Output");
            inputTable.AddColumn("Control");
            inputTable.AddColumn("Value");
            inputTable.AddRow(new Text("Throttle"), new BarChart().Width(20).AddItem("T", t, Color.Green));
            inputTable.AddRow(new Text("Brake"), new BarChart().Width(20).AddItem("B", b, Color.Red));
            inputTable.AddRow("Steering", $"{s}");
            inputTable.AddRow("Boost", boost ? "[green]ON[/]" : "[grey]OFF[/]");
            inputTable.AddRow("Buttons", $"{(cross ? "X " : "")}{(dpadLeft ? "L " : "")}");

            grid.AddRow(telemetryTable, inputTable);
            
            if (debugMode)
            {
                grid.AddRow(new Panel("Debug Mode Active - No Hardware IO").BorderColor(Color.Yellow));
            }

            return grid;
        }
    }
}
