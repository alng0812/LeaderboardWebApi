using LeaderboardWebApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register LeaderboardService
        builder.Services.AddSingleton<LeaderboardService>();

        var app = builder.Build();

        // Define POST route to update customer score
        app.MapPost("/customer/{customerId}/score/{score}", (long customerId, decimal score, LeaderboardService service) =>
        {
            var currentScore = service.UpdateScore(customerId, score);
            return Results.Ok(currentScore);
        });

        // Define GET route to get customer rankings
        app.MapGet("/leaderboard", (int start, int end, LeaderboardService service) =>
        {
            var customers = service.GetCustomersByRank(start, end);
            return Results.Ok(customers);
        });

        // Define GET route to get customer neighbors
        app.MapGet("/leaderboard/{customerId}", (long customerId, LeaderboardService service, int high = 0, int low = 0) =>
        {
            var neighbors = service.GetCustomerNeighbors(customerId, high, low);
            return Results.Ok(neighbors);
        });

        app.Run();
    }
}