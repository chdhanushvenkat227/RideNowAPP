namespace RideNowAPI.Services
{
    public class LocationService
    {
        public double CalculateDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
        {
            var R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians((double)(lat2 - lat1));
            var dLng = ToRadians((double)(lng2 - lng1));
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        public decimal CalculateFare(double distance, string vehicleType)
        {
            var rates = new Dictionary<string, decimal>
            {
                { "Bike", 5m },
                { "Scooty", 6m },
                { "Auto", 8m },
                { "Cab XL", 12m },
                { "Cab Premium", 15m }
            };

            var rate = rates.ContainsKey(vehicleType) ? rates[vehicleType] : 10m;
            return Math.Round((decimal)distance * rate, 2);
        }

        private double ToRadians(double degrees) => degrees * (Math.PI / 180);
    }
}
