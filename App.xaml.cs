using Microsoft.Extensions.DependencyInjection;

namespace AURA_APP_TEST;

public partial class App : Application
{
	private readonly IServiceProvider serviceProvider;

	public App(IServiceProvider serviceProvider)
	{
		this.serviceProvider = serviceProvider;
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(serviceProvider.GetRequiredService<AppShell>());
	}
}