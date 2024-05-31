namespace Hai.Views
{
	using Hai.Platforms;
	using Microsoft.Maui;
	using Microsoft.Maui.Controls;
	using Newtonsoft.Json;
	using System.Diagnostics;
	using System.Globalization;
	using System.Net.Http.Headers;
	using System.Text;
	using System.Text.Json;
	using System.Threading;
	using System.Threading.Tasks;


	public partial class ChatGeminiPage : ContentPage
	{
		private readonly HttpClient _httpClient;
		private readonly string _apiKey;
		private readonly string _apiEndpoint = "https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent";

		private readonly ISpeechToText speechToText;
		private readonly CancellationTokenSource tokenSource = new();


		public ChatGeminiPage()
		{
			InitializeComponent();

			// Initialize the HttpClient and set the API key
			_httpClient = new HttpClient();
			_apiKey = "AIzaSyALlRI9lfKHJHdtdAD3jxuZ2T-5-HFFsMQ"; // Replace with your actual API key

			// Initialize the speech-to-text implementation
			speechToText = new SpeechToTextImplementation();
		}

		private void OnTextInputChangedGemini(object sender, TextChangedEventArgs e)
		{
			// Ensure sendButtonGemini and micButtonGemini are not null
			if (sendButtonGemini != null)
			{
				// Handle text input change for Gemini
				sendButtonGemini.IsVisible = !string.IsNullOrWhiteSpace(e.NewTextValue);
			}

			if (micButtonGemini != null)
			{
				// Check if the text change is through the keyboard
				if (!string.IsNullOrEmpty(e.NewTextValue) && string.IsNullOrEmpty(e.OldTextValue))
				{
					// Text added via keyboard, hide the mic button
					micButtonGemini.IsVisible = false;
				}
				else if (string.IsNullOrEmpty(e.NewTextValue) && !string.IsNullOrEmpty(e.OldTextValue))
				{
					// Text removed via keyboard, show the mic button
					micButtonGemini.IsVisible = true;
				}
			}
		}


		private void OnTapOnDownArrowGemini(object sender, EventArgs e)
		{
			// Your implementation here
			chatScrollViewGemini.ScrollToAsync(0, chatScrollViewGemini.ContentSize.Height, true);
		}


		private async void OnMicButtonClickedGemini(object sender, EventArgs e)
		{
			// Handle mic button click for Gemini
			await StartListening();
		}

		private async void OnMicStopButtonClickedGemini(object sender, EventArgs e)
		{
			// Handle mic stop button click for Gemini
			await StopListening();
		}

		public string RecognitionText { get; private set; }
		private async Task StartListening()
		{
			micButtonGemini.IsEnabled = false;
			sendButtonGemini.IsVisible = false;
			micStopButtonGemini.IsVisible = true;

			// Request microphone permissions
			var isAuthorized = await speechToText.RequestPermissions();

			if (isAuthorized)
			{
				try
				{
					RecognitionText = await speechToText.Listen(CultureInfo.GetCultureInfo("en-us"),
					new Progress<string>(partialText =>
					{
						if (DeviceInfo.Platform == DevicePlatform.Android ||
																							DeviceInfo.Platform == DevicePlatform.iOS ||
																							DeviceInfo.Platform == DevicePlatform.WinUI)
						{
							Device.BeginInvokeOnMainThread(() =>
							{
								textInputGemini.Text = partialText;
							});
						}

						OnPropertyChanged(nameof(RecognitionText));
					}), tokenSource.Token);

					// Process the final recognized text as needed
					textInputGemini.Text = string.Empty;
					await AddMessageToChat(RecognitionText, isUser: true);

					string responseText = await SendMessageToGeminiAsync(RecognitionText);
					await AddMessageToChat(responseText, isUser: false);
				}
				catch (Exception ex)
				{
					// Handle any errors that occurred during speech recognition
					await DisplayAlert("Error", ex.Message, "OK");
				}
			}
			else
			{
				// Display an alert if microphone access is not granted
				await DisplayAlert("Permission Error", "No microphone access", "OK");
			}

			micButtonGemini.IsVisible = true;
			micStopButtonGemini.IsVisible = false;
		}

		private Task StopListening()
		{
			tokenSource?.Cancel();
			// Update button visibility
			micButtonGemini.IsVisible = true;
			textInputGemini.Text = string.Empty;
			micStopButtonGemini.IsVisible = false;
			return Task.CompletedTask;
		}

		private async void OnSendButtonClickedGemini(object sender, EventArgs e)
		{
			try
			{
				// Handle send button click for Gemini
				var userMessage = textInputGemini.Text;
				if (string.IsNullOrWhiteSpace(userMessage))
					return;

				await AddMessageToChat(userMessage, isUser: true);

				// Ensure this is run on the main thread
				MainThread.BeginInvokeOnMainThread(() =>
				{
					textInputGemini.Text = string.Empty;
				});

				var response = await SendMessageToGeminiAsync(userMessage);
				if (response != null)
				{
					await AddMessageToChat(response, isUser: false);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error in OnSendButtonClickedGemini: {ex.Message}");
				await DisplayAlert("Error", "An error occurred while sending the message.", "OK");
			}
		}

		private Task AddMessageToChat(string message, bool isUser)
		{
			return MainThread.InvokeOnMainThreadAsync(() =>
			{
				var messageLabel = new Label
				{
					Text = message,
					BackgroundColor = isUser ? Color.FromArgb("#075e54") : Color.FromArgb("#01204E"), // LightBlue and LightGray
					TextColor = isUser ? Color.FromArgb("#dcf8c6") : Color.FromArgb("#dcf8c6"),
					HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
					Margin = new Thickness(5),
					Padding = new Thickness(6)
				};

				chatLayoutGemini.Children.Add(messageLabel);
			});
		}


		private async Task<string> SendMessageToGeminiAsync(string userMessage, byte[] imageData = null)
		{
			try
			{
				// Construct the request body based on the input parameters
				dynamic requestBody = null;

				if (imageData != null)
				{
					requestBody = new
					{
						contents = new[]
							{
										new
										{
												parts = new object[]
												{
														new { text = userMessage },
														new
														{
																inlineData = new
																{
																		mimeType = "image/png",
																		data = Convert.ToBase64String(imageData)
																}
														}
												}
										}
								}
					};
				}
				else
				{
					requestBody = new
					{
						contents = new object[]
							{
										new
										{
												parts = new[]
												{
														new { text = userMessage }
												}
										}
								}
					};
				}

				// Serialize the request body
				var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

				// Construct the URL with the API key
				string url = _apiEndpoint + "?key=" + _apiKey;

				// Send the POST request
				var response = await _httpClient.PostAsync(url, jsonContent);

				// Read and parse the response
				var jsonResponse = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<GeminiResponse>(jsonResponse);
				var jsonResponseParse = JsonDocument.Parse(jsonResponse);

				if (response.IsSuccessStatusCode)
				{
					var content = result?.Candidates?.FirstOrDefault()?.Content;
					if (content != null && content.Parts.Any())
					{
						return content.Parts[0].Text;
					}
					else
					{
						return "No response content available.";
					}
				}
				else
				{
					string errorMessage = jsonResponseParse.RootElement.GetProperty("error").GetProperty("message").GetString();
					Debug.WriteLine($"API request failed with status code {response.StatusCode}");
					Debug.WriteLine($"{errorMessage}");
					if (response.ReasonPhrase == "Unauthorized" || response.ReasonPhrase == "Too Many Requests")
					{
						await DisplayAlert(response.ReasonPhrase, errorMessage, "OK");
						return errorMessage;
					}
					else
					{
						// Display an alert to the user
						await DisplayAlert("Network Error", "No internet access", "OK");
						return errorMessage;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error occurred while sending message to Gemini: {ex.Message}");
			}

			return null;
		}




	}
}


public class GeminiResponse
{
	public List<Candidate> Candidates { get; set; }
}

public class Candidate
{
	public Content Content { get; set; }
	public string FinishReason { get; set; }
	public int Index { get; set; }
	public List<SafetyRating> SafetyRatings { get; set; }
}

public class Content
{
	public List<Part> Parts { get; set; }
	public string Role { get; set; }
}

public class Part
{
	public string Text { get; set; }
}

public class SafetyRating
{
	public string Category { get; set; }
	public string Probability { get; set; }
}
