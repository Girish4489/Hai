using System.Windows.Input;

namespace Hai.Views
{
	public partial class AboutPage : ContentPage
	{
		public ICommand TapCommand { get; }

		public AboutPage()
		{
			InitializeComponent();

			TapCommand = new Command<string>(ExecuteTapCommand);
			BindingContext = this;
		}

		private async void ExecuteTapCommand(string url)
		{
			if (!string.IsNullOrEmpty(url))
			{
				await Launcher.OpenAsync(new Uri(url));
			}
		}
	}
}
