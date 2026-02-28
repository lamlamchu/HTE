public enum PhishingType { None, FakeLink, UrgentThreat, GiftScam }

public class EmailModel
{
    public string Sender { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPhishing { get; set; } // 是否為釣魚郵件
    public PhishingType Type { get; set; } // 詐騙類型，用於觸發不同警告視圖
    public string WarningMessage { get; set; } = "警告！這是一個釣魚連結，請勿輸入任何資料。";
}
