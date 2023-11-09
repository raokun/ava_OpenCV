using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ava_OpenCV.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ReactiveUI;

namespace ava_OpenCV.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        CheckFileCommand = ReactiveCommand.CreateFromTask(CheckFile);
        ColorChangeCommand = ReactiveCommand.Create(ColorChange);
        RefreshCommand= ReactiveCommand.Create(Refresh);
        TestCommand= ReactiveCommand.Create(Test);
    }

    #region 字段

    private string _filePath;

    public string FilePath
    {
        get => _filePath;
        set =>this.RaiseAndSetIfChanged(ref _filePath, value);
    }

    private bool _buttonVisible =false;

    public bool ButtonVisible
    {
        get => _buttonVisible;
        set =>this.RaiseAndSetIfChanged(ref _buttonVisible, value);
    }

    private IImage _imageSource;
    public IImage ImageSource
    {
        get => _imageSource; 
        set=>this.RaiseAndSetIfChanged(ref _imageSource, value);
        
    }
    
    private IImage _outPutImageSource;
    public IImage OutPutImageSource
    {
        get => _outPutImageSource; 
        set=>this.RaiseAndSetIfChanged(ref _outPutImageSource, value);
        
    }

    private Mat _workMat;

    public Mat WorkMat
    {
        get => _workMat;
        set => this.RaiseAndSetIfChanged(ref _workMat, value);
    }

    #endregion
    
    #region 事件
    public ICommand CheckFileCommand { get; }
    public ICommand ColorChangeCommand { get; }
    public ICommand RefreshCommand { get; }
    
    public ICommand TestCommand { get; }
    #endregion
    #region 方法

    private async Task CheckFile()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.AllowMultiple = false; // 只允许选择单个文件
        openFileDialog.Filters.Add(new FileDialogFilter() { Name = "Images", Extensions = { "jpg", "jpeg", "png", "gif" } });

        // 显示文件选择对话框，并等待用户选择文件
        var selectedFiles = await openFileDialog.ShowAsync((MainWindow)(Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);

        // 处理选定的文件
        if (selectedFiles != null && selectedFiles.Length > 0)
        {
            ButtonVisible = true;
            FilePath = selectedFiles[0];
            Bitmap bitmap = new Bitmap(FilePath);
            ImageSource = bitmap;
            OutPutImageSource = bitmap;
        }
    }
    private void ColorChange(){
        try
        {
            if (WorkMat == null)
            {
                WorkMat = new Mat(FilePath, ImreadModes.Unchanged);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        for (var y = 0; y < WorkMat.Height; y++)
        {
            for (var x = 0; x < WorkMat.Width; x++)
            {
                Vec3b color = WorkMat.Get<Vec3b>(y, x);
                var temp = color.Item0;
                color.Item0 = color.Item2; // B <- R
                color.Item2 = temp;        // R <- B
                WorkMat.Set(y, x, color);
            }
        }
        var mem = WorkMat.ToMemoryStream();
        OutPutImageSource = new Bitmap(mem);

    }
    
    private void Refresh()
    {
        WorkMat = new Mat(FilePath, ImreadModes.Unchanged);
        var bmp = WorkMat.ToMemoryStream();
        OutPutImageSource = new Bitmap(bmp);
    }

    private void Test()
    {
        WorkMat.Resize(2);
        var bmp = WorkMat.ToMemoryStream();
        OutPutImageSource = new Bitmap(bmp);
    }
    #endregion
}