using BlazorBootstrap;
using Dapper;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Telegram_Web.Models;
using Telegram_Web.Models.Ai;
using Telegram_Web.Models.Telegram;
using Telegram_Web.Services.Impl;
using Telegram_Web.Shared.Components;
using static System.Net.Mime.MediaTypeNames;
using static Telegram_Web.Shared.Components.TeamManagement;

namespace Telegram_Web.Pages
{
    public partial class Inbox
    {
        private HubConnection? hubConnection;
        private DateOnly date1 = DateOnly.FromDateTime(DateTime.Now.AddDays(-4));
        private DateOnly date2 = DateOnly.FromDateTime(DateTime.Now);
        


        private DateOnly date1Summ = DateOnly.FromDateTime(DateTime.Now);
        private DateOnly date2Summ = DateOnly.FromDateTime(DateTime.Now);

       
        private TimeOnly time1 = TimeOnly.FromDateTime(DateTime.Now).AddMinutes(-30);
        private TimeOnly time2 = TimeOnly.FromDateTime(DateTime.Now);

        private List<TelegramChatStatus> chatList = new();
        private TelegramChatStatus GroupChatInfo = new();
        private List<TelegramMessage> messagesList = new();
        private List<TelegramChatCase> ChatCases { get; set; } = new();
        private TelegramChatCase TelegramChatCase { get; set; } = new();


        private Offcanvas offcanvas = default!;


        private ConfirmDialog confirmDialog;
        private List<ToastMessage> messages = new List<ToastMessage>();
        private Assign assignModal = default!;

        private string selectedShow = "All";
        private string selectedSort = "Newest";
        private string selectedAllMine = "All";
        private string? selectedFileName;

        private string selectedFilterCase = "Open";
        private string selectedFilterCaseSort = "Newest";

        private int selectedMessageID = 0;



        private string selectedSendAction = "";
        private bool IsBtnListCollapsed = false;
        private bool IsRightCollapsed = true;
        private string MiddleColumnClass => IsRightCollapsed ? "col-md-9" : "col-md-6";
        private List<string> StatusList = new() { "New", "Open", "Pending", "Close" };
        private List<string> CaseStatusList = new() { "Open", "Pending with Reply", "Pending", "Close", "Close with Reply" };

        private Dictionary<string, string> StatusColors = new Dictionary<string, string>
        {
            { "Open", "badge bg-success" },
            { "Pending", "badge bg-warning text-dark" },
            { "Pending with Reply", "badge bg-info text-dark" },
            { "Close", "badge bg-danger" },
            { "Close with Reply", "badge bg-danger" }
        };



        private bool showUnreply = false;
        private bool showSearch = false;
        private bool isSummaryLoading = false;
        private bool newMessageTextAdded = false;
        private bool isLoadingAI = false;
        public bool IsConnected => hubConnection?.State == HubConnectionState.Connected;




        private string searchText = "";
        private string newMessageText = "";
        private int? newMessageID = 0;

        private string? responseMessage;
        private string summaryData = "";
        private string username = "";
        private string userid = "";
        private string summaryResult = "";
        private int TotalGroupChats = 0;
        private int TotalMine = 0;
        private int TotalUnassign = 0;


        private ClaimsPrincipal? user;
        string _connectionString = string.Empty;




        private byte[]? photoBytes;
        private string? photoName;


        private bool isRecording = false;
        private bool isSending = false;
        private byte[]? recordedAudioBytes;
        private DotNetObjectReference<Inbox>? dotNetObjectRef;




        protected override async Task OnInitializedAsync()
        {
            var hubUrl = Configuration["SignalRHubUrl"];
            _connectionString = Configuration.GetConnectionString("AMIS_Data");


            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            user = authState.User;
            userid = user?.FindFirst("EmpID")?.Value ?? "0";
            username = user?.Identity?.Name ?? "";
            Console.WriteLine($"EmpID being passed: {userid}");


            await GetTelegramChatsAsync();



            //hubConnection = new HubConnectionBuilder()
            //.WithUrl(hubUrl)
            //.WithAutomaticReconnect()
            //.Build();

            hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = (handler) =>
                {
                    if (handler is HttpClientHandler clientHandler)
                    {
                        clientHandler.MaxRequestContentBufferSize = 1024 * 1024 * 50; // 50 MB
                    }
                    return handler;
                };
            })
            .WithAutomaticReconnect()
            .Build();



            /////////////////////////////// SignalR /////////////////////////////
            hubConnection.On<TelegramMessage>("ReceiveMessage", async (message) =>
            {
                if (message.Datetime.HasValue)
                    message.Datetime = message.Datetime.Value.ToUniversalTime().AddHours(7);

                var existingChat = chatList.FirstOrDefault(c => c.ChatID == message.ChatID);
                if (existingChat != null)
                {
                    await GetTelegramChatsAsync();
                    await LoadMessageList(GroupChatInfo.ChatID);
                }
                else
                {
                    await GetTelegramChatsAsync();

                }

                //this to show in Teams 
                await OnLoadCaseOpen();


                //this to show in More Info
                await LoadChatCases();
                await LoadInboxCount();


                messages.Add(new ToastMessage
                {
                    Type = ToastType.Primary,
                    Message = $"{message.Title}: {message.Text}"
                });


                await InvokeAsync(StateHasChanged);
            });


            try
            {
                await hubConnection.StartAsync();

                if (hubConnection.State == HubConnectionState.Connected)
                {
                    Console.WriteLine("✅ Connected to SignalR hub");
                    await hubConnection.SendAsync("JoinChatSession", "-123");  // maybe use userid
                }
                else
                {
                    Console.WriteLine("❌ Failed to connect");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error connecting to hub: {ex.Message}");
            }
        }

        // 👇 Add this to clean up when leaving the page
        public async ValueTask DisposeAsync()
        {
            if (hubConnection != null)
            {
                await hubConnection.DisposeAsync();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {

            if (newMessageTextAdded)
            {
                newMessageTextAdded = false;
                await JSRuntime.InvokeVoidAsync("scrollToBottom", "messagesContainer");
            }


            if (firstRender)
            {
                await OnLoadCaseOpen();
                await LoadInboxCount();
                selectedSendAction = "chat";
                dotNetObjectRef = DotNetObjectReference.Create(this);
                // The JS call is correctly placed here.
                await JSRuntime.InvokeVoidAsync("initializeRecorder", dotNetObjectRef);

                 await JSRuntime.InvokeVoidAsync("eval", @"
                var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle=""tooltip""]'));
                tooltipTriggerList.map(function (tooltipTriggerEl) { return new bootstrap.Tooltip(tooltipTriggerEl); });
            ");


                StateHasChanged();
            }
        }

        private async Task GetTelegramChatsAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            var chats = await connection.QueryAsync<TelegramChatStatus>(
                "sp_TelegramWeb_Chatlist_Get",
                new
                {
                    EmpID = userid,
                    Filter = selectedAllMine,
                    Status = selectedShow,
                    SortOrder = selectedSort,
                    SearchTitle = searchText,
                },
                commandType: CommandType.StoredProcedure
            );

            chatList = chats.ToList();
        }


        private async Task GetTelegramChatsByTeamIDIDAsync(int TeamID)
        {
            using var connection = new SqlConnection(_connectionString);

            var chats = await connection.QueryAsync<TelegramChatStatus>(
                "sp_TelegramWeb_Chatlist_GetByTeam",
                new
                {
                    TeamID = TeamID,
                    Filter = selectedAllMine,
                    Status = selectedShow,
                    SortOrder = selectedSort,
                    SearchTitle = searchText,
                },
                commandType: CommandType.StoredProcedure
            );

            chatList = chats.ToList();
        }

        private async Task GetTelegramChatsByEmpIDAsync(string empid)
        {
            using var connection = new SqlConnection(_connectionString);

            var chats = await connection.QueryAsync<TelegramChatStatus>(
                "sp_TelegramWeb_Chatlist_Get",
                new
                {
                    EmpID = empid,
                    Filter = "Mine",
                    Status = selectedShow,
                    SortOrder = selectedSort,
                    SearchTitle = searchText,
                },
                commandType: CommandType.StoredProcedure
            );

            chatList = chats.ToList();
        }

        private async Task OnStatusChange(string status)
        {
            if (status != GroupChatInfo.StatusName)
            {
                await ShowToggleStatusDialog(GroupChatInfo.ChatID, status);
                GroupChatInfo.StatusName = status; // update local UI
            }
        }
        private string GetStatusBorderColor(string status) => status switch
        {
            "Open" => "#28a745",    // green
            "Close" => "#dc3545",   // red
            "Pending" => "#ffc107", // yellow
            "New" => "#007bff",     // blue
            _ => "#6c757d"          // gray default
        };
        private async Task ShowToggleStatusDialog(long chatId, string desiredStatus)
        {
            if (confirmDialog == null)
            {
                Console.WriteLine("confirmDialog is null!");
                return;
            }

            string actionVerb = desiredStatus switch
            {
                "Close" => "close",
                "Open" => "open",
                "Pending" => "mark as pending",
                "New" => "mark as new",
                _ => "change"
            };

            var options = new ConfirmDialogOptions
            {
                YesButtonText = desiredStatus,
                YesButtonColor = desiredStatus switch
                {
                    "Close" => ButtonColor.Danger,
                    "Open" => ButtonColor.Success,
                    "Pending" => ButtonColor.Warning,
                    "New" => ButtonColor.Primary,
                    _ => ButtonColor.Secondary
                },
                NoButtonText = "Cancel",
                NoButtonColor = ButtonColor.Secondary,
                Size = DialogSize.Small
            };

            bool confirmed = await confirmDialog.ShowAsync(
                title: $"Confirm {desiredStatus}",
                message1: $"Are you sure you want to {actionVerb} the chat?",
                confirmDialogOptions: options
            );

            if (confirmed)
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(
                    "sp_TelegramWeb_ChatStatus_Update",
                    new { ChatID = chatId, NewStatus = desiredStatus, UpdatedBy = userid },
                    commandType: CommandType.StoredProcedure
                );

                await GetTelegramChatsAsync();
                await LoadMessageList(chatId);
                await LoadChatStatus(chatId);

                StateHasChanged();

                messages.Add(new ToastMessage
                {
                    Type = desiredStatus switch
                    {
                        "Close" => ToastType.Danger,
                        "Open" => ToastType.Success,
                        "Pending" => ToastType.Warning,
                        "New" => ToastType.Info,
                        _ => ToastType.Info
                    },
                    Message = $"Chat {desiredStatus} successfully!"
                });
            }
        }
        private async Task LoadInboxCount()
        {

            using var connection = new SqlConnection(_connectionString);

            var result = await connection.QueryAsync(
                "sp_TelegramWeb_ChatInbox_Count",
                new { EmpID = userid },
                commandType: CommandType.StoredProcedure
            );

            foreach (var row in result)
            {
                string status = row.Status;
                int count = row.Count;

                switch (status)
                {
                    case "Total":
                        TotalGroupChats = count;
                        break;
                    case "Mine":
                        TotalMine = count;
                        break;
                    case "Unassign":
                        TotalUnassign = count;
                        break;
                }
            }
        }
        private async Task ToggleSearch()
        {
            showSearch = true;
            await Task.Delay(50); 
            await JSRuntime.InvokeVoidAsync("focusElement", "txtSearchInput");
        }

        private async void CloseSearch()
        {
            showSearch = false;
            searchText = "";
            await GetTelegramChatsAsync();
            StateHasChanged();

        }

        private long loadingChatId = 0;
        private async Task OnChatClick(long chatId, string title)
        {

            loadingChatId = chatId; // set loading state
            try
            {
                await LoadMessageList(chatId);
                await LoadChatStatus(chatId);
                await LoadChatCases();


                newMessageText = "";
                responseMessage = "";
                summaryData = "";
                summaryResult = "";
                loadingChatId = 0;
                await InvokeAsync(StateHasChanged);
                Console.WriteLine("OnChatClick");


                await Task.Yield();
                await JSRuntime.InvokeVoidAsync("scrollToBottom", "messagesContainer");
                await JSRuntime.InvokeVoidAsync("focusElement", "txtMessageInput");
            }
            finally
            {
                loadingChatId = 0;   // reset loading state
            }
        }

        private Dictionary<int, string> messageTranslations = new();
        private Dictionary<int, bool> showTranslation = new();
        private async Task OnTranslate(int msgId, string text, string targetLang)
        {
            var translated = await TranslationService.TranslateAsync(text, targetLang);
            messageTranslations[msgId] = translated;
            showTranslation[msgId] = true; // show translation by default after translating
            StateHasChanged();
        }

        private void ToggleTranslation(int msgId)
        {
            if (!showTranslation.ContainsKey(msgId))
                showTranslation[msgId] = true;
            else
                showTranslation[msgId] = !showTranslation[msgId];

            StateHasChanged();
        }


        private Task LoadChatStatus(long chatId)
        {
            var selectedChat = chatList.FirstOrDefault(c => c.ChatID == chatId);
            if (selectedChat != null)
            {
                // Create a fresh copy for binding
                GroupChatInfo = new();
                GroupChatInfo = selectedChat;
            }

            return Task.CompletedTask;
        }

        private async Task LoadMessageList(long chatId)
        {
            using var connection = new SqlConnection(_connectionString);
            var messages = await connection.QueryAsync<TelegramMessage>(
                "sp_TelegramWeb_Messagelist_ByChatID", // your stored procedure name
                new
                {
                    FromDate = date1.ToDateTime(TimeOnly.MinValue),
                    ToDate = date2.ToDateTime(TimeOnly.MinValue),
                    ChatID = chatId
                },
                commandType: CommandType.StoredProcedure
            );
            messagesList = new();
            messagesList = messages.ToList();
        }
        private async Task ShowAssignModal()
        {
            if (assignModal != null)
            {
                await assignModal.ShowAsync();
            }
            else
            {
                Console.WriteLine("Modal reference is null! It might not have rendered yet.");
            }
        }



        //public async Task MarkAsRead(long chatId)
        //{
        //    try
        //    {
        //        await TelegramService.PostMarkAsReadAsync(chatId, userid);

        //        var chat = chatList.FirstOrDefault(c => c.ChatID == chatId);
        //        if (chat != null)
        //        {
        //            chat.IsRead = true;
        //            chat.LastMessageTime = DateTime.UtcNow; // Optional: update timestamp
        //        }

        //        StateHasChanged();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error marking as read: {ex.Message}");
        //    }
        //}\

        private async Task OnShowSummary()
        {
            await offcanvas!.ShowAsync();
        }

        private async Task OnSummary()
        {
            isLoadingAI = true;
            Console.WriteLine("start summary");
            if (messagesList == null || !messagesList.Any())
            {
                summaryResult = "<span style='color:red;'>Please select chat.</span>";
                isLoadingAI = false;
                return;
            }

            // Combine Date + Time into DateTime
            DateTime fromDateTime = date1Summ.ToDateTime(time1);
            DateTime toDateTime = date2Summ.ToDateTime(time2);
 
            
            // Check if range exceeds 4 hours
            if ((toDateTime - fromDateTime).TotalHours > 4)
            {
                summaryResult = "<span style='color:red;'>Selected range cannot exceed 4 hours.</span>";
                isLoadingAI = false;
                return;
            }

            // Filter messages within the selected range
            var promptMessages = messagesList
                .Where(m => m.Datetime >= fromDateTime && m.Datetime <= toDateTime)
                .Select(m => $"{m.FirstName} {m.LastName}: {m.Text}");

            string promptText = string.Join(Environment.NewLine, promptMessages);
            Console.WriteLine(messagesList.Count());
            if (!string.IsNullOrWhiteSpace(promptText))
            {
                var request = new GeminiContentRequest
                {
                    contents = new List<GeminiContent>
                    {
                        new GeminiContent
                        {
                            parts = new List<GeminiPart>
                            {
                                new GeminiPart { text = "Please help summarize customer chat and return me as a veryshort point to easy understand:\n" + promptText }
                            }
                        }
                    }
                };

                Console.WriteLine("summary");
                summaryResult = "";
                summaryResult = await AiService.GenerateContentAsync(request);
            }
            else
            {
                summaryResult = "<span style='color:red;'>No message summarize for selected date range.</span>";
            }
              Console.WriteLine("summary end");  
            isLoadingAI = false;
        }



        private string OriginalText = string.Empty;
        private string? selectedtranslatelanguage = "English";
        private string? selectedLanguageAuto = "None";
        private string? targetLanguage = "en";
  
       
        private async Task OnselectedTranslateAuto(string action, string? targetLang = null, string? languageName = null)
        {
            selectedLanguageAuto = languageName;
            targetLanguage = targetLang;          
        }

        private async Task OnSelectedComment(string action)
        {
            selectedSendAction = action;
        }
        
        private async Task SelectAction(string action)
        {
            

            if (string.IsNullOrWhiteSpace(newMessageText))
                return;
            isLoadingAI = true;
            OriginalText = action;
            try
            {
                var targetLang = action;

                switch (action.ToLower())
                {
                    //case "changetone":
                    //    processedText = await ChangeToneAsync(newMessageText);
                    //    break;

                    //case "checkspelling":
                    //    processedText = await CheckSpellingAsync(newMessageText);
                    //    break;

                    case "en":
                        // Example: translate to Khmer                        
                        newMessageText = await TranslationService.TranslateAsync(newMessageText, targetLang);
                        break;

                    case "km":
                        // Example: translate to Khmer
                        newMessageText = await TranslationService.TranslateAsync(newMessageText, targetLang);
                        break;
                    case "zh":
                        // Example: translate to Khmer
                        newMessageText = await TranslationService.TranslateAsync(newMessageText, targetLang);
                        break;
                }
            }
            catch (Exception ex)
            {
                responseMessage = $"Error: {ex.Message}";
            }
            finally
            {
                isLoadingAI = false;
            }
        }

        private async Task OnAiHelpAsync(string originalMessage)
        {
            string promptText = "";
            isLoadingAI = true;

            promptText = "Please answer this question :\n\n" + originalMessage;

            if (!string.IsNullOrWhiteSpace(promptText))
            {
                var request = new GeminiContentRequest
                {
                    contents = new List<GeminiContent>
                    {
                        new GeminiContent
                        {
                            parts = new List<GeminiPart>
                            {
                                new GeminiPart { text = promptText }
                            }
                        }
                    }
                };

                summaryResult = await AiService.GenerateContentAsync(request);
            }
            newMessageText = summaryResult;
            //await ResizeTextareaAsync();
            isLoadingAI = false;
        }


        private string ConvertToHtml(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
                return string.Empty;

            string html = rawText;

            // Convert bold (**text**) to <b>
            html = Regex.Replace(html, @"\*\*(.*?)\*\*", "<b>$1</b>", RegexOptions.Singleline);

            // Convert bullet points (* text) to <li>
            html = Regex.Replace(html, @"\* (.*?)($|\r|\n)", "<li>$1</li>", RegexOptions.Multiline);

            // Wrap lists in <ul> if <li> exists
            if (html.Contains("<li>"))
            {
                html = "" + html + "";
            }

            // Replace new lines with <br/>
            html = html.Replace("\r\n", "<br/>").Replace("\n", "<br/>");

            return html;
        }

        public async Task Enter(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !e.ShiftKey)
            {
                if (isSending) return; // ignore if already sending
                isSending = true;
                try
                {
                    switch (selectedSendAction)
                    {
                        case "photo":
                            await SendPhoto();
                            break;

                        case "voice":
                            await SendVoice();
                            break;

                        case "comment":
                            await OnAddComment();
                            break;

                        case "chat":
                            await OnSend();
                            break;
                    }


                    // 3️ Notify other users via SignalR
                    var messageForOthers = new TelegramMessage
                    {
                        ChatID = GroupChatInfo.ChatID                    
                    };

                    bool result = await hubConnection.InvokeAsync<bool>("BroadcastMessage", messageForOthers);
                    Console.WriteLine($"✅ Broadcast result: {result}");
                }
                finally
                {
                    isSending = false; // unlock after sending
                }
            }
        }

        private async Task OnCreateCase(TelegramMessage telegramMessage)
        {
            // 1. Confirm with user
            bool confirmed = await confirmDialog.ShowAsync(
                title: "Confirm Create Case",
                message1: "Do you want to create this case?"
            );

            if (!confirmed)
                return;

            using var connection = new SqlConnection(_connectionString);

            // 2. Execute the stored procedure with CaseID = 0
            var parameters = new
            {
                CaseID = 0,  // 0 = new case
                ChatID = telegramMessage.ChatID,
                MessageID = telegramMessage.MessageId,
                MessageText = telegramMessage.Text,
                CaseBy = userid,  // current user
                Action = "Open",
                Note = $"Created by {userid}"
            };

            // 3. Execute SP and get the new CaseID
            var newCaseId = await connection.QuerySingleAsync<int>(
                "sp_TelegramWeb_ChatCase_Set",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            // 4. Optional: display confirmation in console or UI
            Console.WriteLine($"Case created successfully. CaseID: {newCaseId}");

            // 5. Refresh the UI
            await LoadChatCases();
        }








        TelegramMessage TelegramMessageReply = new();
        private async Task OnReplyPreview(TelegramMessage TelegramMessage)
        {
            selectedChatCaseStatus = "";
            TelegramMessageReply = new();
            TelegramMessageReply = TelegramMessage;
        }
        private void OnReplyCancel()
        {
            selectedChatCaseStatus = "";
            TelegramMessageReply = new();
        }


        int replyToMessageID = 0;
        private async Task OnSend()
        {
            try
            {
                if(selectedLanguageAuto != "None")
                {
                    newMessageText = await TranslationService.TranslateAsync(newMessageText, targetLanguage);
                }




                replyToMessageID = (TelegramMessageReply?.MessageId > 0) ? TelegramMessageReply.MessageId : 0;
                var response = await TelegramService.SendTextMessageAsync(
                    GroupChatInfo.ChatID.ToString(),
                    newMessageText,
                    replyToMessageID
                );

                if (response != null)
                {
                    newMessageID = response;
                    await InsertTelegramLogAsync("101", "");



                    if(selectedChatCaseStatus != "")
                    {
                        await OnSaveChatCase();
                    }

                    //await LoadMessageList(GroupChatInfo.ChatID);

                    newMessageText = "";
                    responseMessage = "Message sent successfully!";
                    TelegramMessageReply = new();
                }
                else
                {
                    responseMessage = "Failed to send message. Possibly invalid reply ID.";
                }
            }
            catch (Exception ex)
            {
                responseMessage = $"Error: {ex.Message}";
            }
        }



        private async Task InsertTelegramLogAsync(string type, string typeCustom = "")
        {
            using var connection = new SqlConnection(_connectionString);

            await connection.ExecuteAsync(
                "sp_InsertTelegramMessage_Test",
                new
                {
                    ChatID = GroupChatInfo.ChatID,
                    MessageId = newMessageID,
                    Title = GroupChatInfo.Title,
                    FirstName = username,
                    LastName = "(bot)",
                    Username = "",
                    Datetime = DateTime.Now.AddHours(-7),
                    Text = newMessageText,
                    Raw = replyToMessageID == 0 ? "" : TelegramMessageReply.FirstName + TelegramMessageReply.LastName + ":" + TelegramMessageReply.Text,
                    EmpID = userid,
                    Type = type,
                    TypeCustom = typeCustom,
                    ReplyMessageID = TelegramMessageReply.MessageId,
                },
                commandType: CommandType.StoredProcedure
            );
            newMessageTextAdded = true;
        }

        private async void SelectButton(string name)
        {
            selectedAllMine = name;
            selectedShow = "All";
            await GetTelegramChatsAsync();
            StateHasChanged();
        }
        private async Task OnShowChange(string value)
        {
            selectedShow = value;
            await GetTelegramChatsAsync();
        }
        private async Task OnSortChange(string value)
        {
            selectedSort = value;
            await GetTelegramChatsAsync();
        }



        private System.Timers.Timer? debounceTimer;
        private void OnSearchInput(ChangeEventArgs e)
        {
            searchText = e.Value?.ToString() ?? "";

            // Reset debounce timer
            debounceTimer?.Stop();
            debounceTimer?.Dispose();

            debounceTimer = new System.Timers.Timer(1000); // 2 seconds delay
            debounceTimer.AutoReset = false;
            debounceTimer.Elapsed += async (s, ev) =>
            {
                debounceTimer?.Stop();

                await InvokeAsync(async () =>
                {
                    Console.WriteLine("Performing search now..."); // <-- fires after 2s
                    await PerformSearch();
                    StateHasChanged(); // refresh UI after search
                });
            };
            debounceTimer.Start();
        }


     

       

        private async Task PerformSearch()
        {
            await GetTelegramChatsAsync();
        }

        private void ToggleButtonList()
        {
            IsBtnListCollapsed = !IsBtnListCollapsed;
        }
        private void ToggleRightSidebar()
        {
            IsRightCollapsed = !IsRightCollapsed;


        }






        private async Task OnAddComment()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "sp_TelegramWeb_Chat_AddComment",
                new
                {
                    ChatID = GroupChatInfo.ChatID,
                    EmpID = userid,
                    Text = newMessageText,
                },
                commandType: CommandType.StoredProcedure
            );

            newMessageText = "";
            //await LoadMessageList(GroupChatInfo.ChatID);

        }
        private async Task OnModalClosed()
        {
            long chatid = GroupChatInfo.ChatID;
            await GetTelegramChatsAsync();
            await LoadMessageList(chatid);
            await LoadChatStatus(chatid);
        }

        private async Task LoadPhoto(InputFileChangeEventArgs e)
        {
            photoBytes = null;
            photoName = null;
            responseMessage = null;
            var file = e.File;
            if (file != null)
            {
                selectedFileName = file.Name;
                using var memoryStream = new MemoryStream();
                await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(memoryStream);
                photoBytes = memoryStream.ToArray();
                photoName = file.Name;

                selectedSendAction = "photo";
            }
        }
        private void CancelPhoto()
        {
            selectedSendAction = "chat";
            photoBytes = null;
            photoName = null;
        }
        private void CancelVoice()
        {
            selectedSendAction = "chat";
        }
        private void CancelComment()
        {
            selectedSendAction = "chat";
        }

        ////////// <  imageaction  >
        private bool showImageViewer = false;
        private string currentImage;
        private double zoomLevel = 1.0;
        private DotNetObjectReference<Inbox> objRef;

        private async void OpenImage(string imageUrl)
        {
            currentImage = imageUrl;
            zoomLevel = 1.0;
            showImageViewer = true;

            await Task.Delay(50); // wait until render
            objRef = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("imageZoomHelper.enableZoom", "imageViewer", objRef);
        }

        private void CloseImage()
        {
            showImageViewer = false;
            objRef?.Dispose();
        }

        [JSInvokable]
        public void OnImageZoom(double deltaY)
        {
            AdjustZoom(deltaY < 0 ? 0.1 : -0.1);
        }

        private void AdjustZoom(double step)
        {
            zoomLevel += step;
            if (zoomLevel < 0.2) zoomLevel = 0.2;
            if (zoomLevel > 5) zoomLevel = 5;
            StateHasChanged();
        }
        ////////// </imageaction>




        private async Task SendPhoto()
        {
            if (photoBytes == null || string.IsNullOrEmpty(photoName))
            {
                responseMessage = "❌ No photo selected.";
                return;
            }

            responseMessage = "Sending photo...";

            string fileName = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            string relativePath = $"downloads/photo/{fileName}";
            string wwwrootPath = Path.Combine(Environment.CurrentDirectory, "wwwroot");
            string fullPath = Path.Combine(wwwrootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            // Ensure folder exists
            string folderPath = Path.GetDirectoryName(fullPath)!;
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);





            var botToken = Configuration["TelegramSettings:BotToken"];

            var success = await TelegramService.SendPhotoAsync(botToken, GroupChatInfo.ChatID.ToString(), photoBytes, photoName, newMessageText);

            if (success)
            {

                // Save the file
                await File.WriteAllBytesAsync(fullPath, photoBytes);
                photoBytes = null;
                photoName = null;

                // Log to SQL
                await InsertTelegramLogAsync("102", relativePath);
                //await LoadMessageList(GroupChatInfo.ChatID);

                responseMessage = "🎉 Photo sent successfully!";
                newMessageText = "";

            }
            else
            {
                responseMessage = "🔥 Failed to send photo.";
            }
            selectedSendAction = "";
        }

        private async Task StartRecording()
        {
            var started = await JSRuntime.InvokeAsync<bool>("startRecording");
            if (started)
            {
                isRecording = true;
                responseMessage = "🔴 Recording in progress...";
            }
            else
            {
                responseMessage = "❌ Could not start recording. Please check microphone permissions.";
            }
        }

        private async Task StopRecording()
        {
            await JSRuntime.InvokeVoidAsync("stopRecording");
            isRecording = false;
            responseMessage = "Processing audio...";

            Console.WriteLine("stop");
        }

        [JSInvokable]
        public void OnRecordingComplete(string base64Audio)
        {
            recordedAudioBytes = Convert.FromBase64String(base64Audio);
            selectedSendAction = "voice";
            responseMessage = "";
            StateHasChanged();
        }

        private async Task SendVoice()
        {
            if (recordedAudioBytes == null) return;

            isSending = true;
            responseMessage = "Sending voice message...";

            var botToken = Configuration["TelegramSettings:BotToken"];

            // Optional: generate unique filename
            string fileName = $"voice_{DateTime.Now:yyyyMMdd_HHmmss}.ogg";
            string relativePath = $"downloads/voice/{fileName}";
            string wwwrootPath = Path.Combine(Environment.CurrentDirectory, "wwwroot");
            string fullPath = Path.Combine(wwwrootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            // Ensure folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));


            var success = await TelegramService.SendVoiceMessageAsync(botToken, GroupChatInfo.ChatID.ToString(), recordedAudioBytes);





            if (success)
            {
                await File.WriteAllBytesAsync(fullPath, recordedAudioBytes);
                // Log to SQL
                await InsertTelegramLogAsync("105", relativePath);
                //await LoadMessageList(GroupChatInfo.ChatID);

                responseMessage = "🎉 Voice message sent successfully!";

                recordedAudioBytes = null;
            }
            else
            {
                responseMessage = "🔥 Failed to send voice message.";
            }
            isSending = false;
            selectedSendAction = "";
        }

        //private async Task LoadChatCases()
        //{
        //    using var connection = new SqlConnection(_connectionString);

        //    ChatCases = (await connection.QueryAsync<TelegramChatCase>(
        //        "sp_TelegramWeb_ChatCase_GetByChat",  // SP we discussed earlier
        //        new { GroupChatInfo.ChatID },
        //        commandType: CommandType.StoredProcedure
        //    )).ToList();
        //}
        private async Task LoadChatCases()
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new
            {
                GroupChatInfo.ChatID,
                CaseStatus = selectedFilterCase,
                CaseSort = selectedFilterCaseSort
            };

            ChatCases = (await connection.QueryAsync<TelegramChatCase>(
                "sp_TelegramWeb_ChatCase_GetByChat",
                parameters,
                commandType: CommandType.StoredProcedure
            )).ToList();
        }



        private async Task CloseCase(int caseId)
        {
            // 1. Ask user for confirmation
            bool confirmed = await confirmDialog.ShowAsync(
                title: "Confirm Close Case",
                message1: $"Are you sure you want to close case #{caseId}?"
            );

            if (!confirmed)
                return;

            // 2. Call SP to close the case
            using var connection = new SqlConnection(_connectionString);

            var parameters = new
            {
                CaseID = caseId,         // Existing CaseID to close
                ChatID = 0,              // Not needed for close, can be 0
                MessageID = 0,           // Not needed for close, can be 0
                MessageText = (string?)null,
                CaseBy = userid,
                Action = "Close",
                Note = $"Closed by {username}"
            };

            await connection.ExecuteAsync(
                "sp_TelegramWeb_ChatCase_Set",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            // 3. Refresh the UI
            await LoadChatCases();
        }



        private string? selectedChatCaseStatus { get; set; }
        
        private async Task OnCaseStatusChange(TelegramChatCase c, string newStatus)
        {
            try
            {
                string note = "Noted.....";

                TelegramChatCase = c;

                selectedChatCaseStatus = newStatus;

                if (newStatus.Contains("Reply"))
                {

                    TelegramMessageReply = new();
                    TelegramMessageReply.MessageId = c.MessageID;
                    TelegramMessageReply.Text = c.MessageText;
                    
                    var message = messagesList.FirstOrDefault(m => m.MessageId == c.MessageID);
                    if (message != null)
                    {
                        TelegramMessageReply = message;
                    }


                    await JSRuntime.InvokeVoidAsync("scrollToMessage", c.MessageID);




                    await JSRuntime.InvokeVoidAsync("focusElement", "txtMessageInput");
                }
                else
                {
                     // Configure confirm dialog
                    var options = new ConfirmDialogOptions
                    {
                        YesButtonText = newStatus,
                        YesButtonColor = newStatus switch
                        {
                            "Close" => ButtonColor.Danger,
                            "Open" => ButtonColor.Success,
                            "Pending" => ButtonColor.Warning,
                            "New" => ButtonColor.Primary,
                            "Pending with Reply" => ButtonColor.Warning,
                            "Close with Reply" => ButtonColor.Danger,
                            _ => ButtonColor.Secondary
                        },
                        NoButtonText = "Cancel",
                        NoButtonColor = ButtonColor.Secondary,
                        Size = DialogSize.Small
                    };

                    // Show confirmation dialog
                    bool confirmed = await confirmDialog.ShowAsync(
                        title: $"Confirm {newStatus}",
                        message1: $"Are you sure you want to set this case to '{newStatus}'?",
                        confirmDialogOptions: options
                    );

                    if (confirmed)
                    {
                        await OnSaveChatCase();
                         // trigger signalR
                        var messageForOthers = new TelegramMessage
                        {
                            ChatID = GroupChatInfo.ChatID
                        };
                        bool result = await hubConnection.InvokeAsync<bool>("BroadcastMessage", messageForOthers);
                    }
                }

                   
                   

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating case status: {ex.Message}");
            }
        }


        private async Task OnSaveChatCase()
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new
            {
                CaseID = TelegramChatCase.CaseID,
                ChatID = TelegramChatCase.ChatID,
                MessageID = TelegramChatCase.MessageID,
                MessageText = TelegramChatCase.MessageText,
                CaseBy = userid,
                Action = selectedChatCaseStatus,
                Note =  "Noted..."
            };

            await connection.ExecuteAsync(
                "sp_TelegramWeb_ChatCase_Set",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }



        private async Task GoToChatMessage(long messageId_)
        {
           
            await JSRuntime.InvokeVoidAsync("scrollToMessage", messageId_);
                   
        }
 
        private async Task OnCaseFilterChange(string selected)
        {
            if(selected == "Newest" || selected == "Oldest")
            {
                selectedFilterCaseSort = selected;
            }
            else
            {
                 selectedFilterCase = selected;
            }
            await LoadChatCases();
        }
        public class OpenCaseResult
        {
            public string EmpID { get; set; }           
            public string Name { get; set; }
            public string TeamName { get; set; }
            public int OpenCaseCount { get; set; }
            public string OpenCaseIDs { get; set; }
            public string ChatIDs { get; set; }
            public string ChatTitles { get; set; } // semicolon-separated
        }


        List<OpenCaseResult> openCaseslist = new List<OpenCaseResult>();
        List<EmployeeResult> allEmployees = new List<EmployeeResult>();
        List<TeamModel> Teams = new List<TeamModel>();
        public class EmployeeResult
        {
            public int EmpID { get; set; }
            public string Name { get; set; }
            public string TeamName { get; set; }
        }
        private async Task OnLoadCaseOpen()
        {
            using var connection = new SqlConnection(_connectionString);

            // 1. Get all employees
            var employees = await connection.QueryAsync<EmployeeResult>(
                "sp_TelegramWeb_AllEmployees",
                commandType: CommandType.StoredProcedure
            );
            allEmployees = employees.ToList();
            
            
            var result = await connection.QueryAsync<OpenCaseResult>(
                "sp_TelegramWeb_OpenCasesWithAllEmployees",
                commandType: CommandType.StoredProcedure
            );

            openCaseslist = result.ToList();


            Teams = (await connection.QueryAsync<TeamModel>(
                        "sp_TelegramWeb_Team_GetAll", commandType: System.Data.CommandType.StoredProcedure)).ToList();

        }



       private TeamManagement teamModal = default!;
       private EmployeeManagement EmpModal = default!;
       private async Task ShowTeamModal()
       {
            await teamModal.ShowModal();  // we'll add AddTeamAsync in child
       }
       private async Task ShowEmpModal(TeamModel Team)
       {
            await EmpModal.ShowModal(Team);  // we'll add AddTeamAsync in child
       }

        private async Task OnMembnerClick(string empid)
        {
            await GetTelegramChatsByEmpIDAsync(empid);
            StateHasChanged(); // refresh UI after search
        }
        private async Task OnTeamCaseClick(int team)
        {
            await GetTelegramChatsByTeamIDIDAsync(team);
            StateHasChanged(); // refresh UI after search
        }
        

        private string GetInitials(string firstName, string lastName)
        {
            string initials = "";
            if (!string.IsNullOrWhiteSpace(firstName))
                initials += firstName[0];
            if (!string.IsNullOrWhiteSpace(lastName))
                initials += lastName[0];
            return initials.ToUpper();
        }


        private string GetColorFromUserId(long? userId)
        {
            // Use 0 if userId is null
            int seed = (int)(userId ?? 0 % int.MaxValue);
            var random = new Random(seed);

            int r = random.Next(50, 200); // avoid too dark or too bright
            int g = random.Next(50, 200);
            int b = random.Next(50, 200);
            return $"rgb({r},{g},{b})";
        }



    }
}
