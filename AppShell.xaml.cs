namespace AURA_APP_TEST;

public partial class AppShell : Shell
{
	public AppShell(BluetoothPage bluetoothPage, MainPage mainPage)
	{
		InitializeComponent();
		BluetoothShellContent.Content = bluetoothPage;
		MainShellContent.Content = mainPage;
	}
}
