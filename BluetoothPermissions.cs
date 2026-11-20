using Microsoft.Maui.ApplicationModel;

namespace AURA_APP_TEST;

public sealed class BluetoothPermissions : Permissions.BasePlatformPermission
{
#if ANDROID
	public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
		OperatingSystem.IsAndroidVersionAtLeast(31)
			? new[]
			{
				(global::Android.Manifest.Permission.BluetoothScan, true),
				(global::Android.Manifest.Permission.BluetoothConnect, true)
			}
			: new[]
			{
				(global::Android.Manifest.Permission.AccessFineLocation, true)
			};
#endif
}
