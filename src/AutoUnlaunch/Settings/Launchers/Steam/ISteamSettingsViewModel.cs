﻿using CommunityToolkit.Mvvm.Input;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.ComponentModel;

namespace MrCapitalQ.AutoUnlaunch.Settings.Launchers.Steam;

public interface ISteamSettingsViewModel : INotifyPropertyChanged
{
    bool IsEnabled { get; set; }
    IEnumerable<ComboBoxOption<int>> DelayOptions { get; }
    ComboBoxOption<int> SelectedDelay { get; set; }
    IEnumerable<ComboBoxOption<LauncherStopMethod>> StopMethodOptions { get; }
    ComboBoxOption<LauncherStopMethod> SelectedStopMethod { get; set; }
    bool HidesShutdownScreen { get; set; }
    bool HidesOnActivityStart { get; set; }
    bool HidesOnActivityEnd { get; set; }
    bool ShowUnnestedInStartMenu { get; set; }
    IRelayCommand MoreCommand { get; }
    IAsyncRelayCommand OpenSteamSettingsCommand { get; }
}

