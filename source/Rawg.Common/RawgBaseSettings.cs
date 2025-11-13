using System.Collections.Generic;

namespace Rawg.Common;

public class RawgBaseSettings : ObservableObject
{
    public string ApiKey { get; set => SetValue(ref field, value); } = string.Empty;

    public string LanguageCode { get; set => SetValue(ref field, value); } = "eng";
}
