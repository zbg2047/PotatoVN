using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GalgameManager.Contracts.Services;
using GalgameManager.Contracts.ViewModels;
using GalgameManager.Models;
using KnowledgePicker.WordCloud;
using KnowledgePicker.WordCloud.Coloring;
using KnowledgePicker.WordCloud.Drawing;
using KnowledgePicker.WordCloud.Layouts;
using KnowledgePicker.WordCloud.Primitives;
using KnowledgePicker.WordCloud.Sizers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.UI;

namespace GalgameManager.ViewModels;
public partial class ReportSubPage2ViewModel : ObservableObject, INavigationAware
{
    private Dictionary<string, int> wordFrequencies = null!;
    private DispatcherTimer tipCloseTimer = null!;

    [ObservableProperty]
    private bool isWordTipOpen;

    [ObservableProperty]
    private FrameworkElement? wordTipTarget;

    [ObservableProperty]
    private TextBlock wordTipContent = new();

    [ObservableProperty]
    private Canvas? wordCloudCanvas;

    public ReportSubPage2ViewModel()
    {
        InitializeWordFrequencies();
        InitializeTimer();
    }

    public void OnNavigatedTo(object parameter)
    {

    }

    public void OnNavigatedFrom()
    {

    }

    private void InitializeWordFrequencies()
        {
            wordFrequencies = new Dictionary<string, int>
            {
                { "WinUI", 8 },
                { "Windows", 7 },
                { "开发", 10 },
                { "界面", 6 },
                { "控件", 5 },
                { "XAML", 8 },
                { "C#", 9 },
                { "编程", 7 },
                { "微软", 6 },
                { "应用程序", 8 },
                // ... 其他词频数据 ...
            };
        }

        private void InitializeTimer()
        {
            tipCloseTimer = new DispatcherTimer();
            tipCloseTimer.Interval = TimeSpan.FromMilliseconds(200);
            tipCloseTimer.Tick += (s, e) =>
            {
                IsWordTipOpen = false;
                tipCloseTimer.Stop();
            };
        }

        private void GenerateWordCloud()
        {
            if (WordCloudCanvas == null) return;
            
            WordCloudCanvas.Children.Clear();

            var wordCloud = new WordCloudInput(
                wordFrequencies.Select(p => new WordCloudEntry(p.Key, p.Value)))
            {
                Width = 800,
                Height = 400,
                MinFontSize = 16,
                MaxFontSize = 64
            };

            var sizer = new LogSizer(wordCloud);
            var typeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal);
            using var engine = new SkGraphicEngine(sizer, wordCloud, typeface);
            var layout = new SpiralLayout(wordCloud);
            var colorizer = new RandomColorizer();
            var wcg = new WordCloudGenerator<SKBitmap>(wordCloud, engine, layout, colorizer);

            foreach (var (item, fontSize) in wcg.Arrange())
            {
                var textBlock = new TextBlock
                {
                    Text = item.Entry.Word,
                    FontSize = fontSize,
                    Foreground = new SolidColorBrush(GetWinUIColor(colorizer.GetColor(item))),
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Microsoft YaHei")
                };

                Canvas.SetLeft(textBlock, item.Location.X);
                Canvas.SetTop(textBlock, item.Location.Y);

                textBlock.PointerEntered += (s, args) =>
                {
                    tipCloseTimer.Stop();
                    WordTipContent.Text = $"词语：{item.Entry.Word}\n频率：{item.Entry.Count}";
                    WordTipTarget = textBlock;
                    IsWordTipOpen = true;
                };

                textBlock.PointerExited += (s, args) =>
                {
                    tipCloseTimer.Start();
                };

                WordCloudCanvas.Children.Add(textBlock);
            }
        }

        private Windows.UI.Color GetWinUIColor(System.Drawing.Color? color) =>
            color.HasValue ? Windows.UI.Color.FromArgb(color.Value.A, color.Value.R, color.Value.G, color.Value.B) 
                          : Colors.Black;
    
    partial void OnWordCloudCanvasChanged(Canvas? value)
    {
        if (value != null)
        {
            GenerateWordCloud();
        }
    }
}
