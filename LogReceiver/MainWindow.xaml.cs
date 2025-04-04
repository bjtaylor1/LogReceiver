﻿using Prism.Events;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LogReceiver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ScrollViewer dataGridScrollViewer;
        private readonly Task listenTask;
        private readonly MainViewModel mainViewModel;

        public MainWindow()
        {
            InitializeComponent();
            mainViewModel = new MainViewModel();
            DataContext = mainViewModel;
            listenTask = Task.Run(() => LogListener.Listen());
            dataGrid.Loaded += HandleDataGridLoaded;
            Closing += HandleClosing;
        }

        private void HandleClosing(object sender, CancelEventArgs e)
        {
            LogListener.Stop();
            if (!LogListener.StoppedEvent.Wait(TimeSpan.FromSeconds(2)))
            {
                MessageBox.Show(this, "The listener didn't report having stopped. Please check the process has fully exited.",
                    "LogReceiver", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else Debug.WriteLine("The listener reported stopped.");
        }

        private void HandleDataGridLoaded(object sender, RoutedEventArgs e)
        {
            dataGridScrollViewer = GetScrollViewer(dataGrid);
            var items = (INotifyCollectionChanged)dataGrid.Items;
            items.CollectionChanged += HandleDataGridCollectionChanged;
        }

        private void HandleDataGridCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            dataGridScrollViewer?.ScrollToEnd();
        }



        public static ScrollViewer GetScrollViewer(UIElement element)
        {
            if (element == null) return null;

            ScrollViewer sv = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element) && sv == null; i++)
            {
                if (VisualTreeHelper.GetChild(element, i) is ScrollViewer)
                {
                    sv = (ScrollViewer)(VisualTreeHelper.GetChild(element, i));
                }
                else
                {
                    sv = GetScrollViewer(VisualTreeHelper.GetChild(element, i) as UIElement);
                }
            }
            return sv;
        }

        private void HandleRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

    }
}
