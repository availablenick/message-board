namespace MessageBoard.Tests;

public class Utilities
{
	public static async Task<string> GetCSRFToken(HttpClient client)
    {
        var response = await client.GetAsync("/csrf-token");
        return response.Headers.GetValues("Set-Cookie")
            .Single(c => c.StartsWith("XSRF-TOKEN"))
            .Substring("XSRF-TOKEN=".Length)
            .Split(';')[0];
    }
}
