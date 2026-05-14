using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Esp32Controller.Models;

namespace Esp32Controller.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly SerialConnection _connection = new();

    [ObservableProperty]
    private ObservableCollection<string> _availablePorts = new();

    [ObservableProperty]
    private string? _selectedPort;

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private string _statusMessage = "尚未連線";

    [ObservableProperty]
    private string _ledState = "UNKNOWN";

    [ObservableProperty]
    private string _ledColor = "#808080";

    [ObservableProperty]
    private byte _customR = 255;

    [ObservableProperty]
    private byte _customG = 128;

    [ObservableProperty]
    private byte _customB = 0;

    public ObservableCollection<string> CommunicationLogs { get; } = new();

    public MainViewModel()
    {
        // 訂閱 Model 的事件
        _connection.MessageReceived += OnMessageReceived;
        _connection.ErrorOccurred += OnErrorOccurred;

        // 載入可用 COM Port
        RefreshPorts();
    }

    [RelayCommand]
    private void RefreshPorts()
    {
        AvailablePorts.Clear();
        foreach (var port in SerialConnection.GetAvailablePorts())
        {
            AvailablePorts.Add(port);
        }

        if (AvailablePorts.Count > 0 && string.IsNullOrEmpty(SelectedPort))
        {
            SelectedPort = AvailablePorts[0];
        }
    }

    [RelayCommand]
    private void Connect()
    {
        if (string.IsNullOrEmpty(SelectedPort))
        {
            StatusMessage = "請先選擇 COM Port";
            return;
        }

        try
        {
            _connection.Open(SelectedPort);
            IsConnected = true;
            StatusMessage = $"已連線到 {SelectedPort}";
            AddLog($"已連線到 {SelectedPort}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"連線失敗: {ex.Message}";
            AddLog($"連線失敗: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Disconnect()
    {
        try
        {
            _connection.Close();
            IsConnected = false;
            StatusMessage = "已中斷連線";
            AddLog("中斷連線");
        }
        catch (Exception ex)
        {
            StatusMessage = $"中斷失敗: {ex.Message}";
            AddLog($"中斷失敗: {ex.Message}");
        }
    }

    [RelayCommand]
    private void TurnOn() => SendCommand("ON");

    [RelayCommand]
    private void TurnOff() => SendCommand("OFF");

    [RelayCommand]
    private void SetRgbCycle() => SendCommand("RGB");

    [RelayCommand]
    private void ApplyCustomColor()
    {
        var color = new RgbColor(CustomR, CustomG, CustomB);
        SendCommand($"SET COLOR={color.ToProtocolString()}");
    }

    [RelayCommand]
    private void ClearLogs() => CommunicationLogs.Clear();

    private void SendCommand(string command)
    {
        if (!_connection.IsOpen)
        {
            StatusMessage = "尚未連線";
            AddLog($"⚠ 未連線，無法送出: {command}");
            return;
        }

        try
        {
            _connection.Send(command);
            AddLog($"→ {command}");
        }
        catch (Exception ex)
        {
            AddLog($"⚠ 送出失敗: {ex.Message}");
        }
    }

    private void OnMessageReceived(object? sender, string message)
    {
        // 序列埠事件不在 UI 執行緒，要切回去
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddLog($"← {message}");
            UpdateStateFromMessage(message);
        });
    }

    private void OnErrorOccurred(object? sender, string error)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AddLog($"⚠ {error}");
            StatusMessage = error;
        });
    }

    private void UpdateStateFromMessage(string message)
    {
        if (message == "READY")
        {
            StatusMessage = "ESP32 就緒";
            return;
        }

        if (message.StartsWith("STATE:"))
        {
            string state = message.Substring(6);
            LedState = state;
            UpdateLedColor(state);
        }
        else if (message.StartsWith("ERROR:"))
        {
            StatusMessage = $"ESP32 錯誤: {message.Substring(6)}";
        }
    }

    private void UpdateLedColor(string state)
    {
        if (state == "ON")
        {
            LedColor = "#FFFFFF";
        }
        else if (state == "OFF")
        {
            LedColor = "#808080";
        }
        else if (state == "RGB")
        {
            LedColor = "#800080";
        }
        else if (state.StartsWith("COLOR="))
        {
            string rgbPart = state.Substring(6);
            if (RgbColor.TryParse(rgbPart, out var color) && color != null)
            {
                LedColor = color.ToHexString();
            }
        }
    }

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        CommunicationLogs.Add($"[{timestamp}] {message}");
    }

    public void Dispose()
    {
        _connection.MessageReceived -= OnMessageReceived;
        _connection.ErrorOccurred -= OnErrorOccurred;
        _connection.Dispose();
    }
}