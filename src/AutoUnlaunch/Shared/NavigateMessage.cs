using Microsoft.UI.Xaml.Media.Animation;

namespace MrCapitalQ.AutoUnlaunch.Shared;

internal record NavigateMessage(Type SourcePageType, object? Parameter = null);

internal record SlideNavigateMessage(Type SourcePageType,
    SlideNavigationTransitionEffect SlideEffect,
    object? Parameter = null)
    : NavigateMessage(SourcePageType, Parameter);