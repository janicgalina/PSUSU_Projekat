using ProjekatScada.Models.Enums;

namespace ProjekatScada.Services.Interfaces
{
    public interface IAlarmSoundService
    {
        double Volume { get; set; }
        AlarmSoundProfile SelectedProfile { get; set; }

        void Start();
        void Stop();
        void Preview();
    }
}
