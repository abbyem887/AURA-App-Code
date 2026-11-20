namespace AURA_APP_TEST;
public partial class BluetoothPage : ContentPage
{
    private readonly BluetoothConnectionService bluetoothConnectionService;

    public string ConnectionStatus { get; private set; } = "Not connected";
    public Color ConnectionStatusColor { get; private set; } = Colors.LightCoral;

    public BluetoothPage(BluetoothConnectionService bluetoothConnectionService)
    {
        this.bluetoothConnectionService = bluetoothConnectionService;
        InitializeComponent();
        BindingContext = this;
        UpdateConnectionStatus();
        this.bluetoothConnectionService.ConnectionStateChanged += OnConnectionStateChanged;
    }

    private async void OnConnectButtonClicked(object? sender, EventArgs e)
    {
        ConnectButton.IsEnabled = false;
        ConnectButton.Text = "Connecting...";
        ConnectionStatus = "Connecting...";
        ConnectionStatusColor = Colors.LightGoldenrodYellow;
        OnPropertyChanged(nameof(ConnectionStatus));
        OnPropertyChanged(nameof(ConnectionStatusColor));
        try
        {
            await bluetoothConnectionService.ConnectAsync();
            UpdateConnectionStatus();
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus();
            await DisplayAlertAsync("Bluetooth connection failed", ex.Message, "OK");
        }
        finally
        {
            ConnectButton.IsEnabled = true;
            ConnectButton.Text = "Connect";
        }
    }

    private void OnConnectionStateChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(UpdateConnectionStatus);
    }

    private void UpdateConnectionStatus()
    {
        ConnectionStatus = bluetoothConnectionService.IsConnected ? "Connected" : "Not Connected";
        ConnectionStatusColor = bluetoothConnectionService.IsConnected ? Colors.LightGreen : Colors.LightCoral;
        OnPropertyChanged(nameof(ConnectionStatus));
        OnPropertyChanged(nameof(ConnectionStatusColor));
    }
}