namespace Hai;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
	}
	private async void OnExitMenuItemClicked(object sender, EventArgs e)
	{
		bool answer = await DisplayAlert("Exit", "Do you want to exit from the app or stay?", "Exit", "Cancel");
		if (answer)
		{
			//close the app
			System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
		}
	}
}
