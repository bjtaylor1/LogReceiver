using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;

namespace LogReceiver
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly List<MessageData> eventList;
        private MessageData selectedMessage;
        private bool isPaused;

        public ICommand ClearCommand { get; }
        public ICommand ClearTreeCommand { get; }
        public ICommand TogglePauseCommand { get; }

        protected void BeginInvokePropertyChanged(string propertyName)
        {
            PropertyChanged?.BeginInvoke(this, new PropertyChangedEventArgs(propertyName), null, null);
        }

        public bool IsPaused
        {
            get => isPaused; set
            {
                if (isPaused != value)
                {
                    isPaused = value;
                    BeginInvokePropertyChanged(nameof(IsPaused));
                    BeginInvokePropertyChanged(nameof(TogglePauseCommandContent));
                }
            }
        }

        public MessageData SelectedMessage
        {
            get { return selectedMessage; }
            set
            {
                if(selectedMessage != value)
                {
                    selectedMessage = value;
                    BeginInvokePropertyChanged(nameof(SelectedMessage));
                }
            }
        }

        public string TogglePauseCommandContent => IsPaused ? "Resume" : "Pause";

        public ListCollectionView Events { get; }

        
        public MainViewModel()
        {
            App.EventAggregator.Value.GetEvent<MessageEvent>().Subscribe(AddMessage, ThreadOption.UIThread);
            App.EventAggregator.Value.GetEvent<InvalidateFilterCacheEvent>().Subscribe(InvalidateFilterCache, ThreadOption.UIThread);
            eventList = new List<MessageData>();
            Events = new ListCollectionView(eventList);
            ClearCommand = new DelegateCommand(Clear);
            TogglePauseCommand = new DelegateCommand(TogglePause);
        }

        private void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        private void InvalidateFilterCache(InvalidateFilterCacheEventArgs args)
        {
            foreach(var affectedLogger in args.AffectedLoggers)
            {
                filterCache.AddOrUpdate(affectedLogger, args.Value, (s, b) => args.Value);
            }
            Events.Refresh();
        }

        private void Clear()
        {
            eventList.Clear();
            Events.Refresh();
        }

        private static readonly ConcurrentDictionary<string, bool> filterCache = new ConcurrentDictionary<string, bool>();

        public event PropertyChangedEventHandler PropertyChanged;


        public void AddMessage(MessageData msgs)
        {
            if (!IsPaused)
            {
                eventList.Add(msgs);

                if (eventList.Count > 5000)
                {
                    eventList.RemoveRange(0, 2000);
                }
                Events.Refresh();
            }
        }
    }
}
