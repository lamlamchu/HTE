public static class EmailData
{
    public static List<EmailModel> Templates =
    [
        // --- 類別 1: 校園與官方 (通常是安全的) ---
        new EmailModel
        {
            Sender = "library@cityu.edu.hk",
            Subject = "圖書逾期通知",
            Content = "您借閱的《Data Structures》已逾期，請盡快歸還以免產生罰款。",
            IsPhishing = false,
            Type = PhishingType.None
        },
        new EmailModel
        {
            Sender = "it-support@cityu.edu.hk",
            Subject = "校園密碼到期提醒",
            Content = "您的校園密碼將於 3 天後過期，請前往官方 portal 更改。",
            IsPhishing = false,
            Type = PhishingType.None
        },

        // --- 類別 2: 銀行與金融 (常見釣魚對象) ---
        new EmailModel
        {
            Sender = "no-reply@hsbcl-secure-mail.com",
            Subject = "您的帳戶已被暫時鎖定",
            Content = "偵測到異常活動。請點擊此處 [http://hsbcl-verify-login.net] 驗證身分，否則帳戶將在 24 小時內凍結。",
            IsPhishing = true,
            Type = PhishingType.UrgentThreat
        },
        new EmailModel
        {
            Sender = "info@paypall-support.org",
            Subject = "收到一筆新的付款",
            Content = "您收到一筆 $500 USD 的付款。請點擊連結確認：http://paypall-receive.biz",
            IsPhishing = true,
            Type = PhishingType.FakeLink
        },

        // --- 類別 3: 社交媒體與娛樂 ---
        new EmailModel
        {
            Sender = "security@faceboak-mail.com",
            Subject = "有人嘗試從莫斯科登入您的帳號",
            Content = "如果這不是您，請立即點擊此處修改密碼：http://secure-fb-accouut.com",
            IsPhishing = true,
            Type = PhishingType.UrgentThreat
        },
        new EmailModel
        {
            Sender = "no-reply@netflix.com",
            Subject = "付款方式更新通知",
            Content = "您的信用卡資訊已過期，請更新您的扣款資料以繼續觀看。",
            IsPhishing = false,
            Type = PhishingType.None
        },

        // --- 類別 4: 幸運中獎與虛假優惠 (典型詐騙) ---
        new EmailModel
        {
            Sender = "rewards@epple-store.com",
            Subject = "恭喜！您獲得了一台 iPhone 16 Pro",
            Content = "您被選為本月幸運用戶，只需支付 $10 運費即可領取：http://epple-free-gift.com",
            IsPhishing = true,
            Type = PhishingType.GiftScam
        },
        new EmailModel
        {
            Sender = "hr@amuzon-jobs.com",
            Subject = "高薪在家工作機會",
            Content = "每日只需 2 小時，時薪 $100。請加 LINE 聯繫領取職位：http://amuzon-job-scam.net",
            IsPhishing = true,
            Type = PhishingType.GiftScam
        }
    ];
}
