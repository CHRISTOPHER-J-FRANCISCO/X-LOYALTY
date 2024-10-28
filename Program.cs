using System.Text.Json;
// Build the web application
var builder = WebApplication.CreateBuilder(args); // Exception thrown: 'System.IO.DirectoryNotFoundException' in Microsoft.Extensions.FileProviders.Physical.dll

// Loading configuration settings
builder.Configuration.AddJsonFile("secrets.json", optional: false, reloadOnChange: true);
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Build the application
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

// Converts string response body to a json document dictionary
static Task<Dictionary<string, object>> getJsonDataFromResponse(string responseBody)
{
        // Parse the response body into a json
        var jsonDocument = JsonDocument.Parse(responseBody);
        // Get the root element of the json document
        var jsonRoot = jsonDocument.RootElement;
        // Create a nested dictionary to store the json response
        Dictionary<string, object> jsonDataDictionary = new Dictionary<string, object>();
        // Extract the first property of the json object because it's the actual data
        var jsonElementData = jsonRoot.EnumerateObject().First(); 
        // Parse the first value of the json object as a new json object
        jsonDocument = JsonDocument.Parse(jsonElementData.Value.ToString());
        // Update the json object variable to point to the new json object
        jsonRoot = jsonDocument.RootElement;
        // Now print the key values of the actual data
        foreach (var property in jsonRoot.EnumerateObject())
        {
            jsonDataDictionary.Add(property.Name, property.Value.ToString());
            //Console.WriteLine($"Key: {property.Name}, Value: {property.Value}"); // Print key-value pair
        }
        return Task.FromResult(jsonDataDictionary);
}

// Determines if a username exists provided the json data dictionary
static bool isXUsername(Dictionary<string, object> jsonDataDictionary)
{
    return jsonDataDictionary.ContainsKey("id") && jsonDataDictionary.ContainsKey("name") && jsonDataDictionary.ContainsKey("username");
}

// Get Lists of Followers and Following
app.MapGet("/GetLists", async (string username, IConfiguration configuration) =>
{
    // Setup http client
    using var httpClient = new HttpClient();
    // Save the bearer token
    string bearerToken = configuration["BearerToken"];
    // Add the authorization header
    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
    // Build the url to request the existence of the username provided
    string url = $"https://api.twitter.com/2/users/by/username/{username}";
    //Console.WriteLine(bearerToken); Checking configuration settings
    //Console.WriteLine(url); Checking url
    // Make the custom request
    HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url);
    // Console.WriteLine(response); The entire blob
    try
    {
         // Ensure success
        httpResponseMessage.EnsureSuccessStatusCode();
        
        // Return response, part of the blob we care about
        string responseBody = await httpResponseMessage.Content.ReadAsStringAsync();
        // Convert the response to json data
        var jsonDataDictionary = await getJsonDataFromResponse(responseBody);
        // Validate the username
        if (isXUsername(jsonDataDictionary))
        {
            Console.WriteLine("Username exists!");
        }
        return responseBody;
    }
    catch (HttpRequestException)
    {
        Console.WriteLine("Username nonexistant!");
        // Console.WriteLine(e.GetBaseException()); Print exception
        return await httpResponseMessage.Content.ReadAsStringAsync();
    }
})
.WithName("GetLists")
.WithOpenApi();
app.Run();