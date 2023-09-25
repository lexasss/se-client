using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SEClient.Cmd;
using TestCmd.Logging;

namespace TestCmd;

[ValueConversion(typeof(object), typeof(int))]
public class ObjectPresenceToBorderWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ((string)value)?.Length > 0 ? 2 : 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public partial class MainWindow : Window
{
    readonly DataSource _dataSource = new ();
    readonly Parser _parser;
    readonly FlowLogger _flowLogger = FlowLogger.Instance;

    public MainWindow()
    {
        InitializeComponent();

        DataContext = this;

        _dataSource.Data += DataSource_Data;
        _dataSource.Closed += DataSource_Closed;

        _parser = new ();
        _parser.PlaneEnter += Parser_PlaneEnter;
        _parser.PlaneExit += Parser_PlaneExit;
        _parser.Sample += Parser_Sample;

        KeyDown += MainWindow_KeyDown;
        Closing += MainWindow_Closing;

        var settings = Properties.Settings.Default;
        txbHost.Text = settings.Host;
        txbPort.Text = settings.Port;
    }

    private void SaveLoggedData()
    {
        if (_flowLogger.HasRecords)
        {
            _flowLogger.IsEnabled = false;
            _flowLogger.SaveTo($"selogger_{DateTime.Now:u}.txt".ToPath());
            _flowLogger.IsEnabled = true;
        }
    }

    // Handlers

    private void DataSource_Closed(object _, EventArgs e)
    {
        btnStartStop.Content = "Start";
        btnStartStop.IsEnabled = true;
        stpSettings.IsEnabled = true;

        SaveLoggedData();
    }

    private void DataSource_Data(object _, string e)
    {
        _parser.Feed(e);
    }

    private void Parser_PlaneEnter(object _, Intersection e)
    {
        _flowLogger.Add("enter", e.PlaneName);
    }

    private void Parser_PlaneExit(object _, string e)
    {
        _flowLogger.Add("exit", e);
    }

    private void Parser_Sample(object _, Sample e)
    {
        _flowLogger.Add("sample", e.ID.ToString(), e.TimeStamp.ToString(), e.GazeDirectionQuality.ToString("F3"));
    }

    // UI handlers

    private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F5)
        {
            _dataSource.Start(txbHost.Text, txbPort.Text, true);
        }
    }

    private void MainWindow_Closing(object _, CancelEventArgs e)
    {
        var settings = Properties.Settings.Default;
        settings.Host = txbHost.Text;
        settings.Port = txbPort.Text;
        settings.Save();
    }


    private async void StartStop_Click(object _, RoutedEventArgs e)
    {
        btnStartStop.IsEnabled = !_dataSource.IsRunning;
        btnStartStop.Content = _dataSource.IsRunning ? "Closing..." : "Interrupt";

        if (_dataSource.IsRunning)
        {
            await _dataSource.Stop();
        }
        else
        {
            _parser.Reset();
            _dataSource.Start(txbHost.Text, txbPort.Text);
        }
    }
}
