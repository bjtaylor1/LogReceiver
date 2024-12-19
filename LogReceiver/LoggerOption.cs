using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LogReceiver
{
    public class LoggerOption : INotifyPropertyChanged
    {
        public string Logger { get; }

        public LoggerOption(string logger) // shouldn't get cloned
        {
            Logger = logger;
        }
        
        private bool _isOn;

        public bool IsOn
        {
            get => _isOn;
            set
            {
                if (value == _isOn) return;
                _isOn = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}