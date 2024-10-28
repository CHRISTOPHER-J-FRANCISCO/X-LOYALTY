using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
// Create and configure the host builder
var builder = Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
{
    // Explicitly bind to port 5115
    webBuilder.UseUrls("http://localhost:5115");
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
                    if (isXUsername(jsonDataDictionary))
                    {
                        return Results.Ok(true);
                    }
                    else
                    {
                        return Results.Ok(false);
                    }

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
static async Task<string> TestEndpoint(string xUsername)
{
    // Create local http client
    using var client = new HttpClient();
    {
        // Get a request from a response sent
        var response = await client.GetAsync($"http://localhost:5115/GetLists?username={xUsername}");
        // Respond based on the response
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
        else
        {
            return "error";
        }
    }
}
// Build the custom host builder
var host = builder.Build();
// Start the host in the background
var hostTask = host.RunAsync();
// Create XLoyalty Gui
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
// Create the form
var form = new Form
{
    Text = "X LOYALTY",
    MaximizeBox = false,
    MinimizeBox = false,
    FormBorderStyle = FormBorderStyle.FixedSingle,
    BackColor = System.Drawing.Color.Black,
    Size = new System.Drawing.Size(420, 420)
};
// Create the label
var label = new Label
{
    Text = "\nWelcome to X LOYALTY!\nEnter an X account username (alphanumeric) to determine if it exists or not!",
    Location = new System.Drawing.Point(0, 0),
    Size = new System.Drawing.Size(420, 60),
    BackColor = System.Drawing.Color.Black,
    ForeColor = System.Drawing.Color.White,
    TextAlign = ContentAlignment.TopCenter
};
// Add the label to the form
form.Controls.Add(label);
// Add the textfield to the form
var usernameTextBox = new TextBox
{
    Text = "Enter Username Here!",
    Location = new System.Drawing.Point(105, 60),
    Size = new System.Drawing.Size(210, 180),
    BackColor = System.Drawing.Color.White,
    ForeColor = System.Drawing.Color.Black,
    TextAlign = HorizontalAlignment.Center
};
// Add the textbox to the form
form.Controls.Add(usernameTextBox);
// Add a button to submit username and to try again
var submitTryAgainButton = new Button
{
    Text = "Go",
    Location = new Point(180, 120),
    Size = new Size(60, 30),
    BackColor = Color.White,
    ForeColor = Color.Black,
    TextAlign = ContentAlignment.MiddleCenter
};
// Add an asynchronous event handler
submitTryAgainButton.Click += async (sender, e) =>
{
    // Disable the button right away to not lodge the pipeline
    submitTryAgainButton.Enabled = false;
    try
    {
        // X usernames are only supposed to be alphanumeric
        if (usernameTextBox.Text.All(char.IsLetterOrDigit))
        {
            string response = await TestEndpoint(usernameTextBox.Text);
            // Something went wrong
            if (response == "error")
            {
                // Just flash the error for 5 seconds and quit
                label.Text = "Something went wrong on our end!";
                Thread.Sleep(5000);
                Application.Exit();
            }
            // Username exists
            else if (response == "true")
            {
                Console.WriteLine("Username Exists!");
            }
            // Username doesn't exist
            else
            {
                Console.WriteLine("Username Doesn't Exists!");
            }
        }
        else
        {
            submitTryAgainButton.Text = "Try Again";
        }
    }
    catch (Exception ex)
    {
        // Just flash the error for 5 seconds and quit
        label.Text = ex.Message;
        Thread.Sleep(5000);
        Application.Exit();
    }
    submitTryAgainButton.Enabled = true;
};
// Add the button to the form
form.Controls.Add(submitTryAgainButton);
// Kill the host when you kill the form
form.FormClosed += async (sender, e) => await host.StopAsync();
// Run the form
Application.Run(form);