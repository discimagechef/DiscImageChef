﻿using System.ComponentModel;
using Aaru.Gui.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Aaru.Gui.Views
{
    public class MediaScanWindow : Window
    {
        public MediaScanWindow()
        {
            InitializeComponent();
        #if DEBUG
            this.AttachDevTools();
        #endif
        }

        void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override void OnClosing(CancelEventArgs e)
        {
            (DataContext as MediaScanViewModel)?.ExecuteStopCommand();
            base.OnClosing(e);
        }
    }
}