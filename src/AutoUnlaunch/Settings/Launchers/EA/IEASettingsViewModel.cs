﻿using CommunityToolkit.Mvvm.Input;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.ComponentModel;

namespace MrCapitalQ.AutoUnlaunch.Settings.Launchers.EA;

public interface IEASettingsViewModel : INotifyPropertyChanged
{
    bool IsEnabled { get; set; }
    IEnumerable<ComboBoxOption<int>> DelayOptions { get; }
    ComboBoxOption<int> SelectedDelay { get; set; }
    IEnumerable<ComboBoxOption<LauncherStopMethod>> StopMethodOptions { get; }
    ComboBoxOption<LauncherStopMethod> SelectedStopMethod { get; set; }
    bool MinimizesOnActivityEnd { get; set; }
    IRelayCommand MoreCommand { get; }
    IAsyncRelayCommand OpenEACommand { get; }
}

