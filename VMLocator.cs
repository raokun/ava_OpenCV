using System;
using ava_OpenCV.ViewModels;

namespace ava_OpenCV;

internal class VMLocator
{
    private static MainWindowViewModel _mainWindowViewModel;

    public static MainWindowViewModel MainWindowViewModel
    {
        get => _mainWindowViewModel ??=new MainWindowViewModel();
        set => _mainWindowViewModel = value;
    }
}