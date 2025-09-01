using Microsoft.Maui.Devices;

namespace trovagiocatoriApp
{
    public static class ApiConfig
    {
        /// <summary>
        /// Restituisce "http://10.0.2.2:8080" quando gira su Android Emulator,
        /// altrimenti "http://localhost:8080".
        /// </summary>
        public static string BaseUrl =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:8080"
                : "http://localhost:8080";


        public static string SpecificPostUrl =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:8000"
                : "http://localhost:8000";

    }
}
