using Microsoft.Maui.Devices;

namespace trovagiocatoriApp
{
    public static class ApiConfig
    {
        public static string BaseUrl =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:8080"    // loopback dell'emulatore Android per auth-service
                : "http://localhost:8080";  // Windows, macOS, iOS, ecc.

        public static string PythonApiUrl =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:8000"    // loopback dell'emulatore Android per backend Python
                : "http://localhost:8000";  // Windows, macOS, iOS, ecc.
    }
}