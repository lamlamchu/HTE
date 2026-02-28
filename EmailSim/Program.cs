using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<EmailSimulator>();
builder.Services.AddSingleton<GameStateService>();
builder.Services.AddHostedService<EmailPickerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/stats", (GameStateService gameState) =>
{
    return Results.Ok(new
    {
        emailCount = gameState.EmailCount,
        agentCount = gameState.AgentCount,
        totalCount = gameState.TotalCount,
        maxLimit = gameState.MaxLimit
    });
});

app.MapGet("/api/emails/pending", (GameStateService gameState) =>
{
    var emails = gameState.DequeuePendingEmails();
    return Results.Ok(emails);
});

app.MapPost("/api/email/click", (EmailModel email) =>
{
    if (email.IsPhishing)
    {
        string warning = email.Type switch
        {
            PhishingType.FakeLink => "⚠️ 警告：這個連結指向一個虛假網站，可能會竊取你的密碼！",
            PhishingType.UrgentThreat => "⚠️ 警告：這封郵件使用威脅語氣強迫你操作，這是常見的詐騙手段。",
            PhishingType.GiftScam => "⚠️ 警告：天底下沒有免費的午餐！這是典型的不實中獎誘惑。",
            _ => email.WarningMessage
        };
        return Results.Ok(new { isPhishing = true, warningMessage = warning, phishingType = email.Type.ToString() });
    }
    return Results.Ok(new { isPhishing = false });
});

app.MapPost("/api/reset", (GameStateService gameState) =>
{
    gameState.ResetCounts();
    return Results.Ok(new { message = "已歸零，Game Loop 將恢復運作。" });
});

app.Run();

public class EmailPickerService : BackgroundService
{
    private readonly EmailSimulator _emailSimulator;
    private readonly GameStateService _gameState;
    private readonly IConfiguration _configuration;
    private readonly Random _random = new();

    public EmailPickerService(EmailSimulator emailSimulator, GameStateService gameState, IConfiguration configuration)
    {
        _emailSimulator = emailSimulator;
        _gameState = gameState;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int intervalSeconds = _configuration.GetValue("GameLoop:IntervalSeconds", 60);
        int maxLimit = _configuration.GetValue("GameLoop:MaxLimit", 10);
        int forceAgentThreshold = _configuration.GetValue("GameLoop:ForceAgentThreshold", 5);

        _gameState.MaxLimit = maxLimit;

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_gameState.TotalCount >= _gameState.MaxLimit)
            {
                Console.WriteLine($"\n⏸️ [暫停] 已達到 {_gameState.MaxLimit} 次操作。等待 TotalCount 歸零後才恢復... (可呼叫 POST /api/reset 歸零)");
                while (_gameState.TotalCount >= _gameState.MaxLimit && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                Console.WriteLine($"\n🚀 === 新一輪模擬開始 (目標: {_gameState.MaxLimit} 次) ===");
                continue;
            }

            int choice;
            if (_gameState.EmailCount >= forceAgentThreshold)
            {
                Console.WriteLine("⚠️ [安全提示] 郵件發送已達 5 次，強制啟動 AI Agent 檢查...");
                choice = 1;
            }
            else
            {
                choice = _random.Next(0, 2);
            }

            if (choice == 0)
            {
                var email = _emailSimulator.GetRandomEmail();
                _gameState.AddEmail(email);
                _gameState.IncrementEmailCount();
                Console.WriteLine($"📧 [郵件] {DateTime.Now:HH:mm:ss} - {email.Sender} | {email.Subject}{(email.IsPhishing ? " [PHISHING]" : "")}");
            }
            else
            {
                _gameState.IncrementAgentCount();
                Console.WriteLine($"🤖 [AI] AI Agent 正在執行掃描任務。");
            }

            Console.WriteLine($"📊 進度：Email({_gameState.EmailCount}) + Agent({_gameState.AgentCount}) = {_gameState.TotalCount}/{_gameState.MaxLimit}");

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }
}
