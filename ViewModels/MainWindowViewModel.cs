using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using ava_OpenCV.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using OpenCvSharp.Extensions;
using ReactiveUI;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

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
        // WorkMat.Resize(2);
        // var bmp = WorkMat.ToMemoryStream();
        // OutPutImageSource = new Bitmap(bmp);
        HandPose();
    }
    /// <summary>
    /// 灰白色调
    /// </summary>
    private void GrayColor()
    {
        Cv2.CvtColor(WorkMat,WorkMat,ColorConversionCodes.BGR2GRAY, 0);
        var bmp = WorkMat.ToMemoryStream();
        OutPutImageSource = new Bitmap(bmp);
    }

    const string model = "pose_iter_102000.caffemodel";
    const string modelTxt = "pose_deploy.prototxt";
    const string sampleImage = "hand.jpg";
    const string outputLoc = "Output_Hand.jpg";
    const int nPoints = 22;
    const double thresh = 0.01;
    
    private void HandPose()
    {
        if (WorkMat == null)
        {
            WorkMat = new Mat(FilePath, ImreadModes.Unchanged);
        }
        int[][] posePairs =
        {
            new[] {0, 1}, new[] {1, 2}, new[] {2, 3}, new[] {3, 4}, //thumb
            new[] {0, 5}, new[] {5, 6}, new[] {6, 7}, new[] {7, 8}, //index
            new[] {0, 9}, new[] {9, 10}, new[] {10, 11}, new[] {11, 12}, //middle
            new[] {0, 13}, new[] {13, 14}, new[] {14, 15}, new[] {15, 16}, //ring
            new[] {0, 17}, new[] {17, 18}, new[] {18, 19}, new[] {19, 20}, //small
        };
        int frameWidth = WorkMat.Cols;
        int frameHeight = WorkMat.Rows;
        float aspectRatio = frameWidth / (float) frameHeight;
        int inHeight = 368;
        int inWidth = ((int) (aspectRatio * inHeight) * 8) / 8;
        using var net = CvDnn.ReadNetFromCaffe(modelTxt, model);
        using var inpBlob = CvDnn.BlobFromImage(_workMat, 1.0 / 255, new OpenCvSharp.Size(inWidth, inHeight),
            new Scalar(0, 0, 0), false, false);

        net.SetInput(inpBlob);
        using var output = net.Forward();
        int H = output.Size(2);
        int W = output.Size(3);

        var points = new List<OpenCvSharp.Point>();

        for (int n = 0; n < nPoints; n++)
        {
            // Probability map of corresponding body's part.
            using var probMap = new Mat(H, W, MatType.CV_32F, output.Ptr(0, n));
            Cv2.Resize(probMap, probMap, new OpenCvSharp.Size(frameWidth, frameHeight));
            Cv2.MinMaxLoc(probMap, out _, out var maxVal, out _, out OpenCvSharp.Point maxLoc);

            if (maxVal > thresh)
            {
                Cv2.Circle(WorkMat, maxLoc.X, maxLoc.Y, 8, new Scalar(0, 255, 255), -1,
                    LineTypes.Link8);
                Cv2.PutText(WorkMat, Cv2.Format(n), new OpenCvSharp.Point(maxLoc.X, maxLoc.Y),
                    HersheyFonts.HersheyComplex, 1, new Scalar(0, 0, 255), 2, LineTypes.AntiAlias);
            }

            points.Add(maxLoc);
        }

        int nPairs = 20; //(POSE_PAIRS).Length / POSE_PAIRS[0].Length;

        for (int n = 0; n < nPairs; n++)
        {
            // lookup 2 connected body/hand parts
            OpenCvSharp.Point partA = points[posePairs[n][0]];
            OpenCvSharp.Point partB = points[posePairs[n][1]];

            if (partA.X <= 0 || partA.Y <= 0 || partB.X <= 0 || partB.Y <= 0)
                continue;

            Cv2.Line(WorkMat, partA, partB, new Scalar(0, 255, 255), 8);
            Cv2.Circle(WorkMat, partA.X, partA.Y, 8, new Scalar(0, 0, 255), -1);
            Cv2.Circle(WorkMat, partB.X, partB.Y, 8, new Scalar(0, 0, 255), -1);
        }
        var bmp = WorkMat.ToMemoryStream();
        OutPutImageSource = new Bitmap(bmp);
    }
    #endregion
}