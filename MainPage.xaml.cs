using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Globalization;
using Hai.Platforms;
using System.Collections.Generic;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System.Text.Json;


namespace Hai;

public partial class MainPage : ContentPage
{
	private readonly HttpClient _httpClient;
	private readonly string _apiKey;
	private readonly string _apiEndpoint = "https://api.openai.com/v1/chat/completions";

	private readonly ISpeechToText speechToText;

	private readonly CancellationTokenSource tokenSource = new();


	public MainPage()
	{
		InitializeComponent();

		// Initialize the HttpClient and set the API key
		_httpClient = new HttpClient();
		//_apiKey = "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";

		// Initialize the speech-to-text implementation
		speechToText = new SpeechToTextImplementation();
	}

	private void OnTapOnDownArrow(object sender, EventArgs e)
	{
		chatScrollView.ScrollToAsync(chatLayout, ScrollToPosition.End, true);

	}

	private void OnTextInputChanged(object sender, TextChangedEventArgs e)
	{
		string text = e.NewTextValue;

		// Check if the text is not null or empty
		if (!string.IsNullOrWhiteSpace(text))
		{
			// Hide the mic button and show the send button
			micButton.IsVisible = false;
			sendButton.IsVisible = true;
		}
		else
		{
			// Show the mic button and hide the send button
			micButton.IsVisible = true;
			sendButton.IsVisible = false;
		}
	}


	/// <summary>
	/// belove code for the micro phone
	/// </summary>
	/// <param></param>

	public string RecognitionText { get; private set; }

	private async void OnMicButtonClicked(object sender, EventArgs e)
	{
		await StartListening();
	}

	private async void OnMicStopButtonClicked(object sender, EventArgs e)
	{
		await StopListening();
	}

	//private string recognitionText;

	private async Task StartListening()
	{
		// Update button visibility
		micButton.IsVisible = false;
		sendButton.IsVisible = false;
		micStopButton.IsVisible = true;

		// Request microphone permissions
		var isAuthorized = await speechToText.RequestPermissions();

		if (isAuthorized)
		{
			try
			{
				Console.WriteLine("before the startlistening");

				RecognitionText = await speechToText.Listen(CultureInfo.GetCultureInfo("en-us"),
				new Progress<string>(partialText =>
				{
					if (DeviceInfo.Platform == DevicePlatform.Android ||
											DeviceInfo.Platform == DevicePlatform.iOS ||
											DeviceInfo.Platform == DevicePlatform.WinUI)
					{
						Device.BeginInvokeOnMainThread(() =>
										{
											textInput.Text = partialText;
										});
					}

					Console.WriteLine("RecognitionText is this: " + RecognitionText);
					OnPropertyChanged(nameof(RecognitionText));
				}), tokenSource.Token);

				Console.WriteLine("okkk " + " " + RecognitionText);
				// Process the final recognized text as needed
				textInput.Text = string.Empty;
				await DisplayMessage(RecognitionText, isUser: true, isAi: false);

				string responseText = await GenerateResponse(RecognitionText);

				await DisplayMessage(responseText, isUser: false, isAi: true);
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

		micButton.IsVisible = true;
		micStopButton.IsVisible = false;
	}



	private Task StopListening()
	{

		tokenSource?.Cancel();
		// Update button visibility

		micButton.IsVisible = true;
		textInput.Text = string.Empty;
		micStopButton.IsVisible = false;
		return Task.CompletedTask;
	}

	// rest of the code

	/// <summary>
	/// bellowe codw is for key board sending
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private async void OnSendButtonClicked(object sender, EventArgs e)
	{
		await ProcessUserInput();
	}

	private async Task ProcessUserInput()
	{
		string inputText = textInput.Text;
		textInput.Text = string.Empty;
		if (!string.IsNullOrWhiteSpace(inputText))
		{
			await DisplayMessage(inputText, isUser: true, isAi: false);

			string responseText = await GenerateResponse(inputText);

			await DisplayMessage(responseText, isUser: false, isAi: true);
		}
	}

	public class ResponseData
	{
		public List<Choice> Choices { get; set; }
	}

	public class Choice
	{
		public string Text { get; set; }
	}

	private async Task<string> GenerateResponse(string inputText)
	{
		try
		{
			var requestData = new
			{
				messages = new[]
					{
										new
										{
												role = "system",
												content = "You are a helpful assistant."
										},
										new
										{
												role = "user",
												content = inputText
										}
								},
				model = "gpt-3.5-turbo"
			};

			var requestDataJson = JsonConvert.SerializeObject(requestData);

			var request = new HttpRequestMessage
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri(_apiEndpoint),
				Content = new StringContent(requestDataJson, Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

			var response = await _httpClient.SendAsync(request);

			if (response.IsSuccessStatusCode)
			{
				var responseContent = await response.Content.ReadAsStringAsync();

				dynamic responseData = JsonConvert.DeserializeObject(responseContent);

				if (responseData != null && responseData.choices != null && responseData.choices.Count > 0)
				{
					string generatedResponse = responseData.choices[0].message.content.ToString();
					return generatedResponse;
				}
				else
				{
					Debug.WriteLine("Response does not contain valid data.");
				}
			}
			else
			{

				string responseContent = await response.Content.ReadAsStringAsync();

				Debug.WriteLine($"Response Error: {response.StatusCode}");
				Debug.WriteLine($"Response Content: {responseContent}");
				// Parse the response content to extract the error message
				var jsonResponse = JsonDocument.Parse(responseContent);
				string errorMessage = jsonResponse.RootElement.GetProperty("error").GetProperty("message").GetString();

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
			Debug.WriteLine($"An error occurred: {ex.Message}");

			// Display an alert to the user
			await DisplayAlert("Network Error", "No internet access", "OK");
		}

		return string.Empty;
	}


	private async Task DisplayMessage(string message, bool isUser, bool isAi)
	{
		Debug.WriteLine("DisplayMessage method started");

		var userLogoImage = new Image
		{
			Source = "user.png",
			HeightRequest = 30,
			WidthRequest = 30,
			Margin = new Thickness(5, 0),
			
			BackgroundColor = Color.FromRgb(20, 189, 173),
			Aspect = Aspect.AspectFill
		};

		var chatbotLogoImage = new Image
		{
			Source = "chatbot.png",
			HeightRequest = 30,
			WidthRequest = 30,
			Margin = new Thickness(5, 0),
			BackgroundColor = Color.FromRgb(20, 189, 173),
			Aspect = Aspect.AspectFill
		};

		var messageEditor = new Editor
		{
			Text = message,
			HorizontalOptions = LayoutOptions.FillAndExpand,
			VerticalOptions = LayoutOptions.StartAndExpand,
			IsReadOnly = true,
			BackgroundColor = new Color(0, 0, 0, 0) // Transparent color
		};

		var copyButtonImage = new Image
		{
			Source = "copy_96.png",
			HeightRequest = 25,
			WidthRequest = 25,
			IsAnimationPlaying = true,
			VerticalOptions = LayoutOptions.End,
			Margin = new Thickness(5, 0),
			BackgroundColor = Color.FromRgb(20, 189, 173),
			Aspect = Aspect.AspectFill
		};

		var gestureRecognizer = new TapGestureRecognizer();
		gestureRecognizer.Tapped += (s, e) => { Clipboard.SetTextAsync(messageEditor.Text); };
		copyButtonImage.GestureRecognizers.Add(gestureRecognizer);

		var topWrapStack = new StackLayout
		{
			Orientation = StackOrientation.Horizontal,
			HorizontalOptions = isAi ? LayoutOptions.Start : LayoutOptions.End,
			Margin = new Thickness(1),
			Padding = new Thickness(2)
		};

		if (isUser)
		{
			topWrapStack.Children.Add(userLogoImage);
			topWrapStack.Children.Add(copyButtonImage);
		}
		else
		{
			topWrapStack.Children.Add(chatbotLogoImage);
			topWrapStack.Children.Add(copyButtonImage);
		}

		var stackLayout = new StackLayout();
		stackLayout.Children.Add(topWrapStack);
		stackLayout.Children.Add(messageEditor);

		var scrollView = new ScrollView
		{
			Content = stackLayout
		};

		var frame = new Frame
		{
			Content = scrollView,
			BorderColor = Microsoft.Maui.Graphics.Color.FromRgb(68, 70, 84),
			BackgroundColor = new Color(0, 0, 0, 0), // Transparent color
			CornerRadius = 10,
			Padding = new Thickness(10),
			Margin = new Thickness(10, 5)
		};

		chatLayout.Children.Add(frame);

		await Task.Delay(100); // Add a delay of 100 milliseconds

		if (chatScrollView != null && frame != null)
		{
			_ = chatScrollView.ScrollToAsync(frame, ScrollToPosition.End, animated: true).ConfigureAwait(true);
		}

		Debug.WriteLine("DisplayMessage method finished");
	}

}
