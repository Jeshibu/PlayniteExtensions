using System.Collections.Generic;

namespace Rawg.Common;

public class RawgBaseSettings : ObservableObject
{
    private string apiKey = string.Empty;
    private string languageCode = "eng";

    public string ApiKey { get => apiKey; set => SetValue(ref apiKey, value); }
    public string LanguageCode { get => languageCode; set => SetValue(ref languageCode, value); }
}
