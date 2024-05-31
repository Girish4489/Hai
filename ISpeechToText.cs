using System.Globalization;

namespace Hai
{
	internal interface ISpeechToText
	{
		Task<bool> RequestPermissions();

		Task<string> Listen(CultureInfo culture,
				IProgress<string> recognitionResult,
				CancellationToken cancellation);
	}
}
