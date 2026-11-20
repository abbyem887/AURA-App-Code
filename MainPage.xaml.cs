using Microcharts;
using System.Diagnostics;

namespace AURA_APP_TEST;

public partial class MainPage : ContentPage
{

	private const int MaxDataPoints = 10;
	private readonly JsonStorageService jsonStorageService = new();
	private readonly BluetoothConnectionService bluetoothConnectionService;

	//Default Data
	private List<float> uvData = new() { 0,0,0,0,0,0,0,0,0,0 };
	private List<int> aqData = new() { 0,0,0,0,0,0,0,0,0,0 };

	// Properties for data binding
	public Chart UVChart { get; private set; } = new LineChart();
	public Chart AQChart { get; private set; } = new LineChart();
	public float UVCurrentValue { get; private set; }
	public int AQCurrentValue { get; private set; }

	public MainPage(BluetoothConnectionService bluetoothConnectionService)
	{
		this.bluetoothConnectionService = bluetoothConnectionService;
		InitializeComponent();

		uvData = LoadUVGraph();
		aqData = LoadAQGraph();
		UVCurrentValue = uvData.LastOrDefault();
		AQCurrentValue = aqData.LastOrDefault();
		UVChart = CreateChart(uvData);
		AQChart = CreateChart(aqData);
		BindingContext = this;
		this.bluetoothConnectionService.SensorDataReceived += OnSensorDataReceived;
	}	

	private static List<float> NormalizeData(List<float>? data)
	{
		var values = data ?? new List<float>();

		if (values.Count <= MaxDataPoints)
		{
			return values;
		}

		return values.Skip(values.Count - MaxDataPoints).ToList();
	}

	private static List<int> NormalizeData(List<int>? data)
	{
		var values = data ?? new List<int>();

		if (values.Count <= MaxDataPoints)
		{
			return values;
		}

		return values.Skip(values.Count - MaxDataPoints).ToList();
	}

	private void AddDataFromEsp(float UVvalue, int AQvalue)
	{
		uvData.Add(UVvalue);
		aqData.Add(AQvalue);
		uvData = NormalizeData(uvData);
		aqData = NormalizeData(aqData);
		jsonStorageService.SaveUVData(uvData);
		jsonStorageService.SaveAQData(aqData);

		UVCurrentValue = uvData.Last();
		OnPropertyChanged(nameof(UVCurrentValue));
		AQCurrentValue = aqData.Last();
		OnPropertyChanged(nameof(AQCurrentValue));
		UVChart = CreateChart(uvData);
		OnPropertyChanged(nameof(UVChart));
		AQChart = CreateChart(aqData);
		OnPropertyChanged(nameof(AQChart));
	}

	private static Chart CreateChart(IEnumerable<float> values) =>
		new LineChart
		{
			Entries = values.Select((value, index) =>
				new ChartEntry(value)
				{
					Label = (index + 1).ToString(),
					ValueLabel = value.ToString()
				}).ToArray()
		};

	private static Chart CreateChart(IEnumerable<int> values) =>
		new LineChart
		{
			Entries = values.Select((value, index) =>
				new ChartEntry(value)
				{
					Label = (index + 1).ToString(),
					ValueLabel = value.ToString()
				}).ToArray()
		};

	// Load graph data from JSON storage
	private List<float> LoadUVGraph()
	{
		var savedData = jsonStorageService.LoadUVData();
		if(savedData is null || savedData.Count == 0)
		{
			Debug.WriteLine("No UV data found in storage. Using default data.");
		}
		uvData = NormalizeData(savedData is { Count: > 0 } ? savedData : uvData);
		return uvData;
	}

	private List<int> LoadAQGraph()
	{
		var savedData = jsonStorageService.LoadAQData();
		if(savedData is null || savedData.Count == 0)
		{
			Debug.WriteLine("No AQ data found in storage. Using default data.");
		}
		aqData = NormalizeData(savedData is { Count: > 0 } ? savedData : aqData);
		return aqData;
	}

	private void OnSensorDataReceived(float uvValue, int aqValue)
	{
		MainThread.BeginInvokeOnMainThread(() => AddDataFromEsp(uvValue, aqValue));
	}

	// Button click event handlers
	private void OnUVButtonClicked(object? sender, EventArgs e)
	{
		MainGrid.IsVisible = false;
		UVBorder.IsVisible = true;
		AQBorder.IsVisible = false;

		UVChartGrid.IsVisible = true;
		AQChartGrid.IsVisible = false;
	}
	private void OnAQButtonClicked(object? sender, EventArgs e)
	{
		MainGrid.IsVisible = false;
		UVBorder.IsVisible = false;
		AQBorder.IsVisible = true;

		UVChartGrid.IsVisible = false;
		AQChartGrid.IsVisible = true;
	}
	private void OnDefaultButtonClicked(object? sender, EventArgs e)
	{
		MainGrid.IsVisible = true;
		UVBorder.IsVisible = false;
		AQBorder.IsVisible = false;

		UVChartGrid.IsVisible = true;
		AQChartGrid.IsVisible = true;
	}


}
