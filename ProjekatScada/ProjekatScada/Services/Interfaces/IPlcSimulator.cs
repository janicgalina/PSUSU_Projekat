namespace ProjekatScada.Services.Interfaces
{
    public interface IPlcSimulator
    {
        void EnsureAddress(string address, double initialValue = 0d);
        double Read(string address);
        bool TryRead(string address, out double value);
        void Write(string address, double value);
    }
}
