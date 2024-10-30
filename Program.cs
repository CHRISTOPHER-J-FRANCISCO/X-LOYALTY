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
                    //Console.WriteLine(url); Checking ur
                    // Make the custom request
                    HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url);
                    // Get the response body
                    string responseBody = await httpResponseMessage.Content.ReadAsStringAsync();
                    // Parse the response body into a json
                    var jsonDocument = JsonDocument.Parse(responseBody);
                    Console.Write("BODYYYY");
                    Console.Write(responseBody);
                    JsonElement rootJsonElement = jsonDocument.RootElement;
                    // Too many requests
                    if (rootJsonElement.TryGetProperty("title", out var property))
                    {
                            Console.WriteLine(property.ToString());
                            return Results.Ok(-1);
                
                    }
                    // Username exists
                    else if (rootJsonElement.TryGetProperty("data", out var prop))
                    {
                        return Results.Ok(1);
                    }
                    // Username doesn't exist
                    else
                    {

                        return Results.Ok(0);
                    }
                }
                catch (HttpRequestException)
                {
                    // Something went horribly wrong on our end
                    return Results.BadRequest();
                }
            }).WithName("GetLists");
        });
    });
});
// Setup method that tests endpoint to determine if a user exists
static async Task<string> TestEndpoint(string xUsername)
{
    // Create local http client
    var client = new HttpClient();
    // Get a request from a response sent
    var response = await client.GetAsync($"http://localhost:5115/GetLists?username={xUsername}");
    // Respond based on the response
    if (response.IsSuccessStatusCode)
    {
        string responseCode = await response.Content.ReadAsStringAsync();
        return responseCode;
    }
    // Bad request
    else
    {
        Console.WriteLine("Something went wrong!");
        client.Dispose();
        return "error";
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
// Create elon smiling to show the user when the username exists
var elonSmilingImage = new PictureBox
{
    Image = Image.FromFile("elonSmiling.jpg"),
    Location = new Point(0, 180),
    Size = new Size(420, 200),
    SizeMode = PictureBoxSizeMode.Zoom,
};
form.Controls.Add(elonSmilingImage);
elonSmilingImage.Visible = false;
// Create elon frowning to show the user when the username is non existant
var elonFrowningImage = new PictureBox
{
    Image = Image.FromFile("elonFrowning.jpeg"),
    Location = new Point(0, 180),
    Size = new Size(420, 200),
    SizeMode = PictureBoxSizeMode.Zoom,
};
form.Controls.Add(elonFrowningImage);
elonFrowningImage.Visible = false;
// Create elon saying go fuck yourself image for too many requests
var elonFYourselfImage = new PictureBox
{
    Image = Image.FromFile("elonFYourself.jpg"),
    Location = new Point(0, 180),
    Size = new Size(420, 200),
    SizeMode = PictureBoxSizeMode.Zoom,
};
form.Controls.Add(elonFYourselfImage);
elonFYourselfImage.Visible = false;
// When the username textbox is selected it hides the images
usernameTextBox.Click += async (sender, e) =>
{
    // Just hide the elon images
    elonFrowningImage.Visible = false;
    elonSmilingImage.Visible = false;
    elonFYourselfImage.Visible = false;
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
            else if (response == "1")
            {
                elonSmilingImage.Visible = true;
                elonFrowningImage.Visible = false;
                elonFYourselfImage.Visible = false;
            }
            // Username doesn't exist
            else if (response == "0")
            {
                elonFrowningImage.Visible = true;
                elonSmilingImage.Visible = false;
                elonFYourselfImage.Visible = false;
            }
            else if (response == "-1")
            {
                elonFYourselfImage.Visible = true;
                elonFrowningImage.Visible = false;
                elonSmilingImage.Visible = false;
   
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