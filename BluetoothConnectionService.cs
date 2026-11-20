using System.Diagnostics;
using System.Globalization;
using System.Text;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;

namespace AURA_APP_TEST;

public sealed class BluetoothConnectionService
{
	private static readonly Guid Esp32ServiceId = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
	private static readonly Guid Esp32CharacteristicId = Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8");
	private const string Esp32DeviceName = "ESP32_MAUI_Demo";
	private static readonly TimeSpan ScanTimeout = TimeSpan.FromSeconds(12);

	private readonly IBluetoothLE _bluetooth = CrossBluetoothLE.Current;
	private readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;
	private readonly SemaphoreSlim _connectionLock = new(1, 1);
	private readonly HashSet<string> _discoveredDeviceNames = new(StringComparer.OrdinalIgnoreCase);
	private IDevice? _device;
	private ICharacteristic? _characteristic;

	public event Action<float, int>? SensorDataReceived;
	public event EventHandler? ConnectionStateChanged;

	public bool IsConnecting { get; private set; }
	public bool IsConnected => _device?.State == DeviceState.Connected;

	public async Task ConnectAsync()
	{
		await _connectionLock.WaitAsync();
		try
		{
			if (IsConnected && _characteristic is not null)
			{
				return;
			}

			IsConnecting = true;
			await EnsureBluetoothPermissionsAsync();

			if (_bluetooth.State != BluetoothState.On)
			{
				throw new InvalidOperationException("Bluetooth is off. Turn it on and try again.");
			}

			if (!IsConnected)
			{
				_device = null;
				_discoveredDeviceNames.Clear();
				_adapter.DeviceDiscovered += OnDeviceDiscovered;
				using var scanTimeout = new CancellationTokenSource(ScanTimeout);
				try
				{
					await _adapter.StartScanningForDevicesAsync(cancellationToken: scanTimeout.Token);
				}
				catch (OperationCanceledException) when (scanTimeout.IsCancellationRequested)
				{
				}
				finally
				{
					_adapter.DeviceDiscovered -= OnDeviceDiscovered;
				}

				if (_device is null)
				{
					var discoveredDevices = _discoveredDeviceNames.Count == 0
						? "No BLE devices were discovered."
						: $"Nearby BLE devices: {string.Join(", ", _discoveredDeviceNames)}.";
					throw new InvalidOperationException(
						$"No device named {Esp32DeviceName} was found within {ScanTimeout.TotalSeconds:0} seconds. " +
						$"{discoveredDevices} Check the ESP32's advertised name.");
				}

				await _adapter.ConnectToDeviceAsync(_device);
			}

			if (_characteristic is not null)
			{
				_characteristic.ValueUpdated -= OnCharacteristicValueUpdated;
				_characteristic = null;
			}

			var device = _device
				?? throw new InvalidOperationException("The ESP32 device is no longer available.");
			var service = await device.GetServiceAsync(Esp32ServiceId)
				?? throw new InvalidOperationException("The connected device does not expose the expected ESP32 service.");
			var characteristic = await service.GetCharacteristicAsync(Esp32CharacteristicId)
				?? throw new InvalidOperationException("The connected device does not expose the expected sensor characteristic.");
			_characteristic = characteristic;

			characteristic.ValueUpdated += OnCharacteristicValueUpdated;
			await characteristic.StartUpdatesAsync();
			ConnectionStateChanged?.Invoke(this, EventArgs.Empty);

			var (initialValue, resultCode) = await characteristic.ReadAsync();
			if (resultCode != 0)
			{
				throw new InvalidOperationException(
					$"Unable to read the ESP32's initial sensor value (BLE result {resultCode}).");
			}

			ProcessCharacteristicValue(initialValue);
		}
		finally
		{
			IsConnecting = false;
			_connectionLock.Release();
		}
	}

	private static async Task EnsureBluetoothPermissionsAsync()
	{
#if ANDROID
		var status = await Permissions.CheckStatusAsync<BluetoothPermissions>();
		if (status != PermissionStatus.Granted)
		{
			status = await Permissions.RequestAsync<BluetoothPermissions>();
		}

		if (status != PermissionStatus.Granted)
		{
			throw new UnauthorizedAccessException("Bluetooth permissions are required to find and connect to the ESP32.");
		}
#endif
	}

	private async void OnDeviceDiscovered(object? sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs args)
	{
		var name = args.Device.Name?.Trim();
		if (!string.IsNullOrEmpty(name))
		{
			_discoveredDeviceNames.Add(name);
		}

		if (_device is not null ||
			!string.Equals(name, Esp32DeviceName, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		_device = args.Device;
		try
		{
			await _adapter.StopScanningForDevicesAsync();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Unable to stop BLE scanning after finding the ESP32: {ex.Message}");
		}
	}

	private void OnCharacteristicValueUpdated(object? sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs args)
	{
		ProcessCharacteristicValue(args.Characteristic.Value);
	}

	private void ProcessCharacteristicValue(byte[] value)
	{
		var payload = Encoding.UTF8.GetString(value);
		var values = payload.Split(',');
		if (values.Length != 2 ||
			!float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var uvValue) ||
			!int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var aqValue))
		{
			Debug.WriteLine($"Ignoring malformed ESP32 sensor payload: '{payload}'.");
			return;
		}

		Debug.WriteLine($"ESP32 sensor values received: UV={uvValue}, AQ={aqValue}.");
		SensorDataReceived?.Invoke(uvValue, aqValue);
	}
}
