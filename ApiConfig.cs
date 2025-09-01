using Microsoft.Maui.Devices;

namespace trovagiocatoriApp
{
    public static class ApiConfig
    {
        /// <summary>
        /// URL per il servizio di autenticazione Go
        /// </summary>
        public static string BaseUrl =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:8080"  // Android Emulator
                : "http://localhost:8080";  // Sviluppo locale - quando Docker non è usato

        /// <summary>
        /// URL per il backend Python (FastAPI)
        /// </summary>
        public static string PythonApiUrl =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:8000"  // Android Emulator
                : "http://localhost:8000";  // Sviluppo locale - quando Docker non è usato
    }
}