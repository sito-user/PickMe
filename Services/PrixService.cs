using Newtonsoft.Json.Linq;

namespace DeliveryWeb.Services
{
    public class PrixService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public PrixService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        public async Task<decimal> CalculerAsync(
    double latDep, double lngDep,
    double latArr, double lngArr,
    string typeVehicule,
    bool estExpress = false)
        {
            decimal tarifKm;
            decimal prixBase;

            switch (typeVehicule)
            {
                case "Voiture": tarifKm = 350m; prixBase = 500m; break;
                case "Camionnette": tarifKm = 500m; prixBase = 800m; break;
                default: tarifKm = 150m; prixBase = 200m; break;
            }

            // Haversine direct — plus de dépendance OSRM instable
            double distanceKm = HaversineKm(latDep, lngDep, latArr, lngArr);

            // Si coords invalides (0,0), estimer 5 km par défaut
            if (distanceKm < 0.1) distanceKm = 5.0;

            decimal prix = prixBase + (decimal)distanceKm * tarifKm;

            // Météo (optionnel)
            try
            {
                var coeff = await GetCoeffMeteo(latDep, lngDep);
                prix *= coeff;
            }
            catch { /* météo optionnelle, on ignore */ }

            var heure = DateTime.Now.Hour;
            if (heure < 7 || heure >= 20) prix *= 1.10m;
            if (estExpress) prix *= 1.20m;

            return Math.Round(prix / 100, 0) * 100;
        }

        private async Task<double> GetDistanceOSRM(
            double latD, double lngD, double latA, double lngA)
        {
            try
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var url = $"http://router.project-osrm.org/route/v1/driving/" +
                          $"{lngD.ToString(inv)},{latD.ToString(inv)};" +
                          $"{lngA.ToString(inv)},{latA.ToString(inv)}" +
                          $"?overview=false";

                var response = await _http.GetStringAsync(url);
                var json = JObject.Parse(response);
                var metres = json["routes"]![0]!["distance"]!.Value<double>();
                return metres / 1000.0;
            }
            catch
            {
                return HaversineKm(latD, lngD, latA, lngA);
            }
        }

        private async Task<decimal> GetCoeffMeteo(double lat, double lng)
        {
            try
            {
                var apiKey = _config["OpenWeatherMap:ApiKey"];
                if (string.IsNullOrEmpty(apiKey)) return 1.0m;

                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var url = $"https://api.openweathermap.org/data/2.5/weather" +
                          $"?lat={lat.ToString(inv)}&lon={lng.ToString(inv)}&appid={apiKey}";

                var response = await _http.GetStringAsync(url);
                var json = JObject.Parse(response);
                var weatherId = json["weather"]![0]!["id"]!.Value<int>();

                if ((weatherId >= 200 && weatherId <= 232) ||
                    (weatherId >= 500 && weatherId <= 531))
                    return 1.15m;

                return 1.0m;
            }
            catch { return 1.0m; }
        }

        private static double HaversineKm(
            double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}