using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class EmailApp
{
    // 儲存所有收到的郵件列表
    public static List<string> EmailList = new List<string>();
    
    // 引用之前的計數器
    public static int EmailCount = 0;
    public static int MaxLimit = 10;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Email 模擬器：新郵件置頂模式 ===");

        while (EmailCount < MaxLimit)
        {
            // 1. 生成新郵件
            string newEmail = GenerateNewEmail();
            
            // 2. 關鍵功能：將新郵件插在列表的最前面 (Index 0)
            EmailList.Insert(0, newEmail);
            EmailCount++;

            // 3. 顯示目前的列表（永遠從最新的開始看）
            ShowPopUpList();

            // 4. 模擬等待（例如每 3 秒收到一封）
            await Task.Delay(3000);
        }

        Console.WriteLine("\n[系統] 列表已滿，準備重置...");
    }

    public static string GenerateNewEmail()
    {
        string[] subjects = { "銀行帳單", "學校通知", "中獎感言", "密碼修改", "好友申請" };
        string time = DateTime.Now.ToString("HH:mm:ss");
        return $"[{time}] 主旨: {subjects[new Random().Next(subjects.Length)]}";
    }

    public static void ShowPopUpList()
    {
        Console.Clear();
        Console.WriteLine($"--- 收件箱 (總計: {EmailCount}/{MaxLimit}) ---");
        
        // 遍歷列表，最上面的就是最新的
        for (int i = 0; i < EmailList.Count; i++)
        {
            // 第一封信加個 [NEW] 標籤
            string prefix = (i == 0) ? "🔥 [最新] " : "   [舊件] ";
            Console.WriteLine(prefix + EmailList[i]);
        }
        Console.WriteLine("------------------------------------------");
    }
}