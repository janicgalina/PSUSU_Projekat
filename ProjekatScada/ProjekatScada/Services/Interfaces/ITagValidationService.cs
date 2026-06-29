using System.Collections.Generic;
using ProjekatScada.Models;

namespace ProjekatScada.Services.Interfaces
{
    public interface ITagValidationService
    {
        IEnumerable<string> ValidateTag(TagBase tag, IEnumerable<TagBase> existingTags);
        IEnumerable<string> ValidateAlarm(Alarm alarm, AnalogInputTag analogInputTag, IEnumerable<Alarm> existingAlarms);
    }
}
