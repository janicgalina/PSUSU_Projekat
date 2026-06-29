using System;
using System.Collections.Generic;
using ProjekatScada.Services.Interfaces;

namespace ProjekatScada.Services
{
    public class PlcSimulator : IPlcSimulator
    {
        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, double> _valuesByAddress = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        public void EnsureAddress(string address, double initialValue = 0d)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("I/O address je obavezna.", nameof(address));
            }

            lock (_syncRoot)
            {
                if (!_valuesByAddress.ContainsKey(address))
                {
                    _valuesByAddress[address] = initialValue;
                }
            }
        }

        public double Read(string address)
        {
            lock (_syncRoot)
            {
                if (!_valuesByAddress.ContainsKey(address))
                {
                    throw new KeyNotFoundException(string.Format("Adresa '{0}' nije mapirana u PLC simulatoru.", address));
                }

                return _valuesByAddress[address];
            }
        }

        public bool TryRead(string address, out double value)
        {
            lock (_syncRoot)
            {
                return _valuesByAddress.TryGetValue(address, out value);
            }
        }

        public void Write(string address, double value)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("I/O address je obavezna.", nameof(address));
            }

            lock (_syncRoot)
            {
                _valuesByAddress[address] = value;
            }
        }
    }
}
