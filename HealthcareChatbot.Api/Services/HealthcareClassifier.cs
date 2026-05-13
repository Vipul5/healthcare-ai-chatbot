using System.Linq;

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
        "blood pressure","diabetes","cancer","pain","nausea","infection"
    };

    public bool IsHealthcareRelated(string question)
    {
        if (string.IsNullOrWhiteSpace(question)) return false;
        var q = question.ToLowerInvariant();
        return Keywords.Any(k => q.Contains(k));
    }
}
