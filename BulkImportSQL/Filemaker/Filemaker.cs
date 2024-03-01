using Chase.CommonLib.Networking;
using Newtonsoft.Json.Linq;

namespace BulkImportSQL.Filemaker;

public class Filemaker
{
    public static async Task<JArray> GetRows(string username, string password, string database, string layout)
    {
        using (AdvancedNetworkClient client = new())
        {
            client.Timeout = TimeSpan.FromHours(23);
            using HttpRequestMessage request = new();
            request.RequestUri = new Uri($"https://lib.mardens.com/fmutil/databases/{database}/layouts/{layout}/records/all");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("X-Authentication-Options", $$"""{"username": "{{username}}",    "password": "{{password}}" }""");
            string content = "";
            try
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStringAsync();
                return JArray.Parse(content);
                // return await client.GetAsJsonArray(request) ?? [];
            }
            catch (Exception e)
            {
                await Console.Error.WriteAsync($"Failed to get content from filemaker: {e.Message}");
                if (!string.IsNullOrWhiteSpace(content))
                {
                    string path = Path.GetFullPath($"./{database}-{layout}-error.json");
                    await File.WriteAllTextAsync(path, content);
                    await Console.Error.WriteAsync($"Writing response to: {path}");
                }
            }
        }


        return [];
    }
}