using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

public static class Helpers
{
    public const string VERSION = "0.0.1";
    public const string DOMAIN = "";

    public static bool IsDev()
    {
        string funcEnv = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
        if (funcEnv == "Development")
            return true;
        else
            return false;
    }

    public static T Clone<T>(T original)
    {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(original));
    }

    public static decimal ToNumber(string text)
    {
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price) ? price : 0;
    }

    public static decimal SmartParse(string text)
    {
        if(text.Contains(",") && text.Contains(".")) 
        {
            if (text.LastIndexOf(",") > text.LastIndexOf(".")) text = text.Replace(".", "");
            if (text.LastIndexOf(",") < text.LastIndexOf(".")) text = text.Replace(",", "");
        }

        text = text.Replace(",", ".");

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d)) return d;
        else return 0;
    }
}
