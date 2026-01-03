namespace ClubmanHard.Logic
{
    public interface IControllerOutput
    {
        void Open();
        void SendControl(byte throttle, byte brake, sbyte steering, bool boost, bool cross = false, bool circle = false, bool dpadUp = false, bool dpadDown = false, bool dpadLeft = false, bool dpadRight = false);
        void Close();
    }
}
