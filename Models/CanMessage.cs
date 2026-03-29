using System;
using System.ComponentModel;

namespace VeraCom.Models
{
    public class CanMessage : INotifyPropertyChanged
    {
        public uint CanID { get; set; }
        public byte DLC { get; set; }
        public byte[] Payload { get; set; }
        public int CycleTimeMs { get; set; }

        private int _txFrameCounter;
        public int TxFrameCounter
        {
            get => _txFrameCounter;
            set { _txFrameCounter = value; OnPropertyChanged(nameof(TxFrameCounter)); }
        }

        // Empfang
        public DateTime Timestamp { get; set; }
        public DateTime LastTimestamp { get; set; }

        private int _rxCycleTime;
        public int RxCycleTime
        {
            get => _rxCycleTime;
            set { _rxCycleTime = value; OnPropertyChanged(nameof(RxCycleTime)); }
        }

        public string PayloadString => Payload != null ? BitConverter.ToString(Payload) : "";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(DLC));
            OnPropertyChanged(nameof(PayloadString));
            OnPropertyChanged(nameof(Timestamp));
            OnPropertyChanged(nameof(RxCycleTime));
        }
    }
}