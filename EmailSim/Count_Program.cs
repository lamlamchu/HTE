using System;
using System.Threading.Tasks;

class EmailSimulator
{
    // 公共變量：讓其他功能可以隨時讀取數據
    public static int EmailCount = 0; 
    public static int AgentCount = 0;
    
    // 計算屬性：自動加總
    public static int TotalCount => EmailCount + AgentCount; 
    public static int MaxLimit = 10;

    static Random random = new Random();

    static async Task Main(string[] args)
    {
        while (true) // 外部無限循環：實現自動重啟
        {
            // 重置計數器
            EmailCount = 0;
            AgentCount = 0;
            Console.WriteLine($"\n🚀 === 新一輪模擬開始 (目標: {MaxLimit} 次) ===");

            while (TotalCount < MaxLimit)
            {
                int choice;

                // --- 邏輯干預部分 ---
                if (EmailCount >= 5)
                {
                    Console.WriteLine("⚠️ [安全提示] 郵件發送已達 5 次，強制啟動 AI Agent 檢查...");
                    choice = 1; // 強制選擇 AI Agent
                }
                else
                {
                    // 正常情況：50% 隨機
                    choice = random.Next(0, 2);
                }
                // ------------------

                if (choice == 0)
                {
                    SendEmail();
                }
                else
                {
                    CallAIAgent();
                }

                Console.WriteLine($"📊 進度：Email({EmailCount}) + Agent({AgentCount}) = {TotalCount}/{MaxLimit}");

                // 測試時改為 1000 (1秒)，正式運行改回 60000 (1分)
                await Task.Delay(1000); 
            }

            Console.WriteLine("\n✅ [完成] 已達到 10 次操作。3 秒後自動歸零並重新啟動...");
            await Task.Delay(3000);
        }
    }

    public static void SendEmail()
    {
        EmailCount++;
        Console.WriteLine("📧 [郵件] 發送了一封新郵件。");
    }

    public static void CallAIAgent()
    {
        AgentCount++;
        Console.WriteLine("🤖 [AI] AI Agent 正在執行掃描任務。");
    }
}