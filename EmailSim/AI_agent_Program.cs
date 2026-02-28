using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro; // 引入 UI 文字庫
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class MultiRoleChatManager : MonoBehaviour
{
    [Header("API 設定")]
    public string apiKey = "請填入你的_API_Key"; 
    private string apiUrl = "https://api.minimax.io/v1/chat/completions";

    [Header("UI 介面設定 - 對話區")]
    public Transform chatContent;            // 聊天紀錄的容器 (需掛載 Vertical Layout Group)
    public TMP_InputField messageInputField; // 玩家輸入框
    public Button sendButton;                // 發送按鈕
    public TextMeshProUGUI roomTitleText;    // 顯示目前正在哪個房間的標題
    public ScrollRect chatScrollRect;        // 聊天室的滾動條控制器

    [Header("UI 介面設定 - 未讀紅點 (選填)")]
    [Tooltip("請按 1~8 的順序放入按鈕上的未讀數字 TextMeshProUGUI，沒有可留空")]
    public List<TextMeshProUGUI> unreadBadgeTexts; 

    // --- 資料結構 ---
    [System.Serializable] public class ChatMessage { public string role; public string content; }
    [System.Serializable] private class ChatRequest { public string model; public List<ChatMessage> messages; public float temperature; public int max_tokens; }
    [System.Serializable] private class ChatResponse { public Choice[] choices; }
    [System.Serializable] private class Choice { public ChatMessage message; }
    
    [System.Serializable] 
    public class RoleSetup 
    { 
        public int roleId; 
        public string roleName; 
        [TextArea(2, 5)] public string systemPrompt; 
    }

    // 負責記憶每個房間狀態的類別
    public class ChatSession 
    { 
        public bool hasStarted = false; 
        public int unreadCount = 0; // 未讀訊息數量
        public List<ChatMessage> history = new List<ChatMessage>(); 
    }

    [Header("角色設定區 (請在這裡設定 8 個角色)")]
    public List<RoleSetup> roleConfigurations;

    private Dictionary<int, ChatSession> chatSessions = new Dictionary<int, ChatSession>();
    private int currentRoomId = -1;

    void Start()
    {
        // 1. 初始化房間記憶與 System Prompt
        foreach (var role in roleConfigurations)
        {
            chatSessions[role.roleId] = new ChatSession();
            chatSessions[role.roleId].history.Add(new ChatMessage { role = "system", content = role.systemPrompt });
        }

        // 2. 綁定玩家發送按鈕
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }

        // 3. 初始更新所有的未讀 UI 數字為 0 (或隱藏)
        UpdateAllUnreadUI();

        // 4. 啟動背景隨機收信系統
        StartCoroutine(RandomAutoMessageRoutine());
        
        Debug.Log("🎉 多頻道模擬聊天系統已完全啟動！");
    }

    // ==========================================
    // 頻道切換與背景自動發送邏輯
    // ==========================================

    /// <summary>
    /// 切換進入特定角色的房間
    /// </summary>
    public void SwitchToRoom(int roomId)
    {
        if (!chatSessions.ContainsKey(roomId)) return;

        currentRoomId = roomId;
        ChatSession currentSession = chatSessions[roomId];

        // 進入房間後，未讀數量歸零，並更新 UI
        currentSession.unreadCount = 0;
        UpdateAllUnreadUI();

        // 更新頂部標題
        string roomName = roleConfigurations.Find(r => r.roleId == roomId).roleName;
        if (roomTitleText != null) roomTitleText.text = $"目前對話：{roomName}";

        // 判斷是否為第一次進入
        if (!currentSession.hasStarted)
        {
            currentSession.hasStarted = true;
            ClearChatUI(); // 清空舊畫面
            // 傳送隱藏開場指令
            StartCoroutine(SendRequestToMiniMax(roomId, "請根據你的角色設定，自然地跟我打第一聲招呼。", true));
        }
        else
        {
            // 載入歷史對話並自動滾動到底部
            LoadRoomHistoryUI(currentSession);
            StartCoroutine(ScrollToBottom());
        }
    }

    /// <summary>
    /// 背景定時器：隨機讓角色主動傳訊息
    /// </summary>
    private IEnumerator RandomAutoMessageRoutine()
    {
        while (true)
        {
            // 隨機等待 20 到 45 秒 (可根據需求調整)
            float waitTime = Random.Range(20f, 45f);
            yield return new WaitForSeconds(waitTime);

            // 隨機挑選 1 到 8 號角色
            int randomRoleId = Random.Range(1, 9);
            
            // 如果該角色還沒被初始化開場，就先跳過，等玩家點進去再說
            if (!chatSessions[randomRoleId].hasStarted) continue;

            Debug.Log($"[系統觸發] 角色 {randomRoleId} 正在輸入訊息...");

            // 發送隱藏指令，要求 AI 主動找話題
            string bgPrompt = "【系統隱藏指令】請根據你的人設，主動發送一則簡短的訊息給玩家來開啟或延續話題。絕對不要提到這是一條指令。";
            StartCoroutine(SendRequestToMiniMax(randomRoleId, bgPrompt, true));
        }
    }

    // ==========================================
    // 玩家與 API 溝通邏輯
    // ==========================================

    public void OnSendButtonClicked()
    {
        string text = messageInputField.text;
        if (string.IsNullOrEmpty(text) || currentRoomId == -1) return;

        messageInputField.text = ""; // 清空輸入框
        
        // 在畫面上立即顯示玩家的話，並滾動到底部
        CreateChatBubble("user", text);
        StartCoroutine(ScrollToBottom());
        
        // 發送給 AI (這是一般對話，不是隱藏指令)
        StartCoroutine(SendRequestToMiniMax(currentRoomId, text, false));
    }

    /// <summary>
    /// 發送請求給 MiniMax
    /// </summary>
    private IEnumerator SendRequestToMiniMax(int roomId, string userInput, bool isHiddenCommand)
    {
        ChatSession session = chatSessions[roomId];

        // 準備要送給 API 的對話清單 (如果是隱藏指令，就不寫進永久的 session.history 裡)
        List<ChatMessage> messagesToSend = new List<ChatMessage>(session.history);
        messagesToSend.Add(new ChatMessage { role = "user", content = userInput });

        // 只有非隱藏指令，才真正存進玩家的對話記憶中
        if (!isHiddenCommand)
        {
            session.history.Add(new ChatMessage { role = "user", content = userInput });
        }

        ChatRequest requestData = new ChatRequest
        {
            model = "M2-her",
            messages = messagesToSend, // 使用暫時的清單發送
            temperature = 0.8f,
            max_tokens = 1024
        };

        string jsonData = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ChatResponse response = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
                string aiReply = response.choices[0].message.content;

                // AI 的回覆永遠都要存進歷史紀錄裡
                session.history.Add(new ChatMessage { role = "assistant", content = aiReply });

                // 判斷玩家現在是否正盯著這個房間看？
                if (currentRoomId == roomId)
                {
                    // 在房間內：直接印出氣泡並滾動
                    CreateChatBubble("assistant", aiReply);
                    StartCoroutine(ScrollToBottom());
                }
                else
                {
                    // 在房間外：增加未讀數量並更新紅點 UI
                    session.unreadCount++;
                    UpdateAllUnreadUI();
                    Debug.Log($"叮咚！收到來自房間 {roomId} 的新訊息。累積未讀：{session.unreadCount}");
                }
            }
            else
            {
                Debug.LogError("API 連線錯誤：" + request.error);
            }
        }
    }

    // ==========================================
    // UI 顯示與排版邏輯
    // ==========================================

    private void LoadRoomHistoryUI(ChatSession session)
    {
        ClearChatUI();

        foreach (var msg in session.history)
        {
            if (msg.role == "system") continue; // 隱藏系統設定
            CreateChatBubble(msg.role, msg.content);
        }
    }

    private void ClearChatUI()
    {
        foreach (Transform child in chatContent)
        {
            Destroy(child.gameObject);
        }
    }

    private void CreateChatBubble(string role, string content)
    {
        GameObject textObj = new GameObject(role + "_Bubble");
        textObj.transform.SetParent(chatContent, false); 

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        
        if (role == "user")
        {
            tmp.text = $"<color=#0055FF>你：</color>{content}";
            tmp.alignment = TextAlignmentOptions.Right; 
        }
        else
        {
            tmp.text = $"<color=#FF5500>AI：</color>{content}";
            tmp.alignment = TextAlignmentOptions.Left;  
        }

        tmp.fontSize = 24;
        tmp.enableWordWrapping = true;
    }

    /// <summary>
    /// 自動滑到對話最底部
    /// </summary>
    private IEnumerator ScrollToBottom()
    {
        // 必須等待一幀，讓 Unity 有時間把新的文字排版好
        yield return new WaitForEndOfFrame();
        
        if (chatScrollRect != null)
        {
            // 0 是最底，1 是最頂
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// 更新大廳 8 個按鈕上的小紅點數字
    /// </summary>
    private void UpdateAllUnreadUI()
    {
        // 如果沒有設定未讀文字，就直接跳過不處理
        if (unreadBadgeTexts == null || unreadBadgeTexts.Count == 0) return;

        for (int i = 0; i < roleConfigurations.Count; i++)
        {
            int rId = roleConfigurations[i].roleId;
            
            // 確保清單數量足夠才更新，以免報錯
            if (i < unreadBadgeTexts.Count && unreadBadgeTexts[i] != null)
            {
                int count = chatSessions.ContainsKey(rId) ? chatSessions[rId].unreadCount : 0;
                
                if (count > 0)
                {
                    unreadBadgeTexts[i].text = count.ToString();
                    unreadBadgeTexts[i].gameObject.SetActive(true); // 有訊息時顯示
                }
                else
                {
                    unreadBadgeTexts[i].gameObject.SetActive(false); // 沒訊息時隱藏紅點
                }
            }
        }
    }
}
