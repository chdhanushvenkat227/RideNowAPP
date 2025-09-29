namespace RideNowAPP.Services
{
    public interface ILocationService
    {
        double CalculateDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2);


         decimal CalculateFare(double distance, string vehicleType); 

    }
}
