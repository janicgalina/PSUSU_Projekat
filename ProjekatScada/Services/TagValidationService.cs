using System;
using System.Collections.Generic;
using System.Linq;
using ProjekatScada.Models;
using ProjekatScada.Models.Enums;
using ProjekatScada.Services.Interfaces;

namespace ProjekatScada.Services
{
    public class TagValidationService : ITagValidationService
    {
        public IEnumerable<string> ValidateTag(TagBase tag, IEnumerable<TagBase> existingTags)
        {
            var errors = new List<string>();
            var currentTags = existingTags == null ? Enumerable.Empty<TagBase>() : existingTags;

            if (tag == null)
            {
                errors.Add("Tag mora biti definisan.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(tag.TagName))
            {
                errors.Add("Tag name je obavezan.");
            }
            else if (currentTags.Any(t => string.Equals(t.TagName, tag.TagName, StringComparison.OrdinalIgnoreCase) && t.Id != tag.Id))
            {
                errors.Add("Tag name mora biti jedinstven.");
            }

            if (string.IsNullOrWhiteSpace(tag.Description))
            {
                errors.Add("Description je obavezan.");
            }

            if (string.IsNullOrWhiteSpace(tag.IOAddress))
            {
                errors.Add("I/O address je obavezna.");
            }
            else if (currentTags.Any(t => string.Equals(t.IOAddress, tag.IOAddress, StringComparison.OrdinalIgnoreCase) && t.Id != tag.Id))
            {
                errors.Add("I/O address mora biti jedinstvena.");
            }

            var inputTag = tag as InputTag;
            if (inputTag != null && inputTag.ScanTime <= 0)
            {
                errors.Add("Scan time mora biti veći od nule za ulazne tagove.");
            }

            var analogInputTag = tag as AnalogInputTag;
            if (analogInputTag != null)
            {
                ValidateAnalogLimits(analogInputTag.LowLimit, analogInputTag.HighLimit, analogInputTag.Units, errors);

                if (analogInputTag.Deadband < 0)
                {
                    errors.Add("Deadband ne može biti negativan.");
                }

                if (analogInputTag.Hysteresis < 0)
                {
                    errors.Add("Hysteresis ne može biti negativna.");
                }
            }

            var analogOutputTag = tag as AnalogOutputTag;
            if (analogOutputTag != null)
            {
                ValidateAnalogLimits(analogOutputTag.LowLimit, analogOutputTag.HighLimit, analogOutputTag.Units, errors);

                if (analogOutputTag.InitialValue < analogOutputTag.LowLimit || analogOutputTag.InitialValue > analogOutputTag.HighLimit)
                {
                    errors.Add("Initial value za AO mora biti unutar zadatih granica.");
                }
            }

            var digitalOutputTag = tag as DigitalOutputTag;
            if (digitalOutputTag != null && digitalOutputTag.InitialValue != 0d && digitalOutputTag.InitialValue != 1d)
            {
                errors.Add("Initial value za DO mora biti 0 ili 1.");
            }

            return errors;
        }

        public IEnumerable<string> ValidateAlarm(Alarm alarm, AnalogInputTag analogInputTag, IEnumerable<Alarm> existingAlarms)
        {
            var errors = new List<string>();
            var currentAlarms = existingAlarms == null ? Enumerable.Empty<Alarm>() : existingAlarms;

            if (alarm == null)
            {
                errors.Add("Alarm mora biti definisan.");
                return errors;
            }

            if (analogInputTag == null)
            {
                errors.Add("Alarm može biti vezan samo za postojeći AI tag.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(alarm.Message))
            {
                errors.Add("Poruka alarma je obavezna.");
            }

            if (alarm.Threshold < analogInputTag.LowLimit || alarm.Threshold > analogInputTag.HighLimit)
            {
                errors.Add("Threshold alarma mora biti unutar granica analognog ulaza.");
            }

            if (currentAlarms.Any(a => a.Id != alarm.Id && a.AnalogInputTagId == analogInputTag.Id && a.TriggerType == alarm.TriggerType && Math.Abs(a.Threshold - alarm.Threshold) < 0.0001))
            {
                errors.Add("Alarm sa istim pragom i tipom okidanja već postoji za izabrani AI tag.");
            }

            if (!Enum.IsDefined(typeof(AlarmTriggerType), alarm.TriggerType))
            {
                errors.Add("Trigger type alarma nije validan.");
            }

            return errors;
        }

        private static void ValidateAnalogLimits(double lowLimit, double highLimit, string units, ICollection<string> errors)
        {
            if (lowLimit >= highLimit)
            {
                errors.Add("Low limit mora biti manji od high limit vrednosti.");
            }

            if (string.IsNullOrWhiteSpace(units))
            {
                errors.Add("Units je obavezno polje za analogne tagove.");
            }
            else if (units.Any(char.IsDigit))
            {
                errors.Add("Units ne sme sadržati brojeve. Izaberite jedinicu iz liste.");
            }
        }
    }
}
