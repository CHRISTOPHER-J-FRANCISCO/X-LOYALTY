using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Windows.Forms;


// Create and configure the host builder
var builder = Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
{
    // Configure application configuration
    webBuilder.ConfigureAppConfiguration((hostingContext, config) =>
    {
        // Add external file
        config.AddJsonFile("secrets.json", optional: false, reloadOnChange: true);

    });
    // Configure application's HTTP request pipeline
    webBuilder.Configure(app =>
    {
        // Add routing
        app.UseRouting();
        // Add endpoints
        app.UseEndpoints(endpoints =>
        {
            // Define Get Lists of Followers and Following
            endpoints.MapGet("/GetLists", async (string username, IConfiguration configuration, HttpResponse response) =>
            {
                try
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
                    // Convert the response to json data
                    var jsonDataDictionary = await getJsonDataFromResponse(httpResponseMessage);
                    // Validate the username
                    if (isXUsername(jsonDataDictionary))
                    {
                        Console.WriteLine("Username exists!");
                    }
                    else
                    {
                        Console.WriteLine("Username nonexistant!");
                    }
                    return Results.Ok(jsonDataDictionary);
                }
                catch (HttpRequestException e)
                {
                    // Console.WriteLine(e.GetBaseException()); // Print exception 
                    return Results.Problem(e.StackTrace);
                }
            }).WithName("GetLists");
        });
    });
});

// Converts string response body to a json document dictionary
static async Task<Dictionary<string, object>> getJsonDataFromResponse(HttpResponseMessage httpResponseMessage)
{
    // Ensure success
    httpResponseMessage.EnsureSuccessStatusCode();
    // Return response, part of the blob we care about
    string responseBody = await httpResponseMessage.Content.ReadAsStringAsync();
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
    return jsonDataDictionary;
}

// Determines if a username exists provided the json data dictionary
static bool isXUsername(Dictionary<string, object> jsonDataDictionary)
{
    return jsonDataDictionary.ContainsKey("id") && jsonDataDictionary.ContainsKey("name") && jsonDataDictionary.ContainsKey("username");
}

// Setup method that tests endpoint
static async Task TestEndpoint()
{
    // Create local http client
    using var client = new HttpClient();
    // Get a request from a response sent
    var response = await client.GetAsync("http://localhost:5115/GetLists?username=marckuban69"); // Assuming 5115 is your actual port
    // Respond based on the response
    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response: {content}");
    }
    else
    {
        Console.WriteLine($"Request failed with status code: {response.StatusCode}");
    }
}

// Build the custom host builder
var host = builder.Build();

// Start the host in the background
var hostTask = host.RunAsync();

// // Run delay to make sure you call an endpoint after the host builder is run
// await Task.Delay(1000);

// // Test the endpoint
// await TestEndpoint();

// Create XLoyalty Gui
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
// Create the form
var form = new Form
{
    Text = "X LOYALTY"
};
// Create the label
var label = new Label 
{
    Text = "Welcome to X LOYALTY!",
    AutoSize = true,
    Location = new System.Drawing.Point(12, 12)
};
// Add the label to the form
form.Controls.Add(label);
// Kill the host when you kill the form
form.FormClosed += async (sender, e) => await host.StopAsync();
// Run the form
Application.Run(form);