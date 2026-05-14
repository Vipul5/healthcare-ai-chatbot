using System.Linq;
using System.Text.RegularExpressions;

namespace HealthcareChatbot.Api.Services;

public interface IHealthcareClassifier
{
    bool IsHealthcareRelated(string question);
}

public class HealthcareClassifier : IHealthcareClassifier
{
    private static readonly string[] Keywords = new[]
    {
        "doctor","hospital","symptom","fever","cough","medicine","prescription",
        "diagnosis","surgery","therapy","health","clinic","covid","vaccine",
        "blood pressure","diabetes","cancer","pain","nausea","infection",
        "temperature","degree","degrees","°f","f","°c","c"
    };

    public bool IsHealthcareRelated(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return false;
        var q = question.ToLowerInvariant();
        // Basic keyword match
        if (Keywords.Any(k => q.Contains(k))) return true;

        // Detect numeric temperature mentions like "104 F", "104 degrees", "temperature 104"
        var tempPattern = new Regex(@"\b\d{2,3}\s*(°?f|f|°?c|c|degrees|degree)\b", RegexOptions.IgnoreCase);
        if (tempPattern.IsMatch(question)) return true;

        // Detect phrases like "high temperature" or "body temperature"
        if (q.Contains("high temperature") || q.Contains("body temperature")) return true;

        return false;
    }
}
