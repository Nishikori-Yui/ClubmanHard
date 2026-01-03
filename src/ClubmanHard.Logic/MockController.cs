using System;

namespace ClubmanHard.Logic
{
    public class MockController : IControllerOutput
    {
        public void Open()
        {
            Console.WriteLine("[Mock] Controller Opened");
        }

        public void SendControl(byte throttle, byte brake, sbyte steering, bool boost, bool cross = false, bool circle = false, bool dpadUp = false, bool dpadDown = false, bool dpadLeft = false, bool dpadRight = false)
        {
            // Do nothing or log if verbose
            // Console.WriteLine($"[Mock] T:{throttle} B:{brake} S:{steering} X:{cross} O:{circle}");
        }

        public void Close()
        {
            Console.WriteLine("[Mock] Controller Closed");
        }
    }
}
