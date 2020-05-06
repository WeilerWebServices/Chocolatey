﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Chocolatey" file="ChocolateyDialog.xaml.cs">
//   Copyright 2017 - Present Chocolatey Software, LLC
//   Copyright 2014 - 2017 Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media;
using ChocolateyGui.Common.Controls;
using ChocolateyGui.Common.Models;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace ChocolateyGui.Common.Windows.Controls.Dialogs
{
    /// <summary>
    ///     Interaction logic for ChocolateyDialog.xaml
    /// </summary>
    public partial class ChocolateyDialog : CustomDialog
    {
        public static readonly DependencyProperty IsCancelableProperty = DependencyProperty.Register(
            "IsCancelable",
            typeof(bool),
            typeof(ChocolateyDialog),
            new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty NegativeButtonTextProperty =
            DependencyProperty.Register(
                "NegativeButtonText",
                typeof(string),
                typeof(ChocolateyDialog),
                new PropertyMetadata("Cancel"));

        public static readonly DependencyProperty OutputBufferCollectionProperty = DependencyProperty.Register(
            "OutputBufferCollectionCollection",
            typeof(ObservableRingBufferCollection<PowerShellOutputLine>),
            typeof(ChocolateyDialog),
            new PropertyMetadata(
                default(ObservableRingBufferCollection<PowerShellOutputLine>),
                (s, e) =>
                {
                    ((ChocolateyDialog)s).PART_Console.BufferCollection =
                        (ObservableRingBufferCollection<PowerShellOutputLine>)e.NewValue;
                }));

        public static readonly DependencyProperty ProgressBarForegroundProperty =
            DependencyProperty.Register(
                "ProgressBarForeground",
                typeof(Brush),
                typeof(ChocolateyDialog),
                new PropertyMetadata(Brushes.White));

        internal ChocolateyDialog(MetroWindow parentWindow, bool showConsoleOutput)
        {
            ShowOutputConsole = showConsoleOutput;

            InitializeComponent();

            if (parentWindow.MetroDialogOptions.ColorScheme == MetroDialogColorScheme.Theme)
            {
                ProgressBarForeground = FindResource("MahApps.Brushes.Accent") as Brush;
            }
            else
            {
                ProgressBarForeground = Brushes.White;
            }

            NegativeButtonText = Common.Properties.Resources.ChocolateyDialog_Cancel;
        }

        public bool IsCancelable
        {
            get { return (bool)GetValue(IsCancelableProperty); }
            set { SetValue(IsCancelableProperty, value); }
        }

        public string NegativeButtonText
        {
            get { return (string)GetValue(NegativeButtonTextProperty); }
            set { SetValue(NegativeButtonTextProperty, value); }
        }

        public bool ShowOutputConsole { get; set; }

        public ObservableRingBufferCollection<PowerShellOutputLine> OutputBufferCollection
        {
            get { return (ObservableRingBufferCollection<PowerShellOutputLine>)GetValue(OutputBufferCollectionProperty); }
            set { SetValue(OutputBufferCollectionProperty, value); }
        }

        public Brush ProgressBarForeground
        {
            get { return (Brush)GetValue(ProgressBarForegroundProperty); }
            set { SetValue(ProgressBarForegroundProperty, value); }
        }

        protected override void OnClose()
        {
            base.OnClose();
            OutputBufferCollection.Clear();
        }
    }
}