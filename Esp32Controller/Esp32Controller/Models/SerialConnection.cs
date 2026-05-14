using System.IO.Ports;
using System.Text;

namespace Esp32Controller.Models;

public class SerialConnection : IDisposable
{
    private SerialPort? _port;
    private readonly StringBuilder _receiveBuffer = new();
    private readonly object _bufferLock = new();

    public event EventHandler<string>? MessageReceived;
    public event EventHandler<string>? ErrorOccurred;

    public bool IsOpen => _port?.IsOpen ?? false;

    public static string[] GetAvailablePorts() => SerialPort.GetPortNames();

    public void Open(string portName, int baudRate = 115200)
    {
        if (_port?.IsOpen == true)
        {
            throw new InvalidOperationException("Serial port is already open.");
        }

        _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            NewLine = "\n",
            ReadTimeout = 500,
            WriteTimeout = 500,
            Encoding = Encoding.ASCII
        };

        _port.DataReceived += OnDataReceived;
        _port.ErrorReceived += OnErrorReceived;

        _port.Open();
    }

    public void Close()
    {
        if (_port == null) return;

        try
        {
            _port.DataReceived -= OnDataReceived;
            _port.ErrorReceived -= OnErrorReceived;

            if (_port.IsOpen)
            {
                _port.Close();
            }
        }
        finally
        {
            _port.Dispose();
            _port = null;
        }
    }

    public void Send(string command)
    {
        if (_port == null || !_port.IsOpen)
        {
            throw new InvalidOperationException("Serial port is not open.");
        }

        _port.WriteLine(command);
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_port == null) return;

        try
        {
            string data = _port.ReadExisting();

            lock (_bufferLock)
            {
                _receiveBuffer.Append(data);

                // 從 buffer 抽出所有完整訊息（以 \n 結尾）
                while (true)
                {
                    string bufferContent = _receiveBuffer.ToString();
                    int newlineIndex = bufferContent.IndexOf('\n');
                    if (newlineIndex == -1) break;

                    string message = bufferContent.Substring(0, newlineIndex).TrimEnd('\r');
                    _receiveBuffer.Remove(0, newlineIndex + 1);

                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        MessageReceived?.Invoke(this, message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Read error: {ex.Message}");
        }
    }

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        ErrorOccurred?.Invoke(this, $"Serial error: {e.EventType}");
    }

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}