using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using System.Text.RegularExpressions;
using Telegram_Web.Models.Ai;
using Telegram_Web.Models.Telegram;

namespace Telegram_Web.Pages
{
    public partial class Chat : ComponentBase
    {
        private HubConnection? hubConnection;
        private List<TelegramMessage> messages = new();
        private List<TelegramChatStatus> chatList = new();
        private List<TelegramEmp> TelegramEmpList = new();
        private List<TelegramEmp> AssignedEmpList = new();

        private string newMessage = "";
        private string responseMessage = "";
        public bool IsConnected => hubConnection?.State == HubConnectionState.Connected;

        private long selectedChatId;
        private string selectedEmpId;
        private bool isLoadingAI = false;
        private Offcanvas offcanvas = default!;
        private string summaryResult = string.Empty;


        private DateOnly date1 = DateOnly.FromDateTime(DateTime.Now.AddDays(-5));
        private DateOnly date2 = DateOnly.FromDateTime(DateTime.Now);
        bool newMessageAdded = false;

        private string username;
        private string userid;

        protected override async Task OnInitializedAsync()
        {
            var hubUrl = Configuration["SignalRHubUrl"];
         


            





            hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

            hubConnection.On<TelegramMessage>("ReceiveMessage", (message) =>
            {
                if (message.Datetime.HasValue)
                    message.Datetime = message.Datetime.Value.ToUniversalTime().AddHours(7);

                var existingChat = chatList.FirstOrDefault(c => c.ChatID == message.ChatID);
                if (existingChat != null)
                {
                    // Update timestamp and mark as unread
                    existingChat.ReceivedTime = message.Datetime;
                    existingChat.IsRead = false;
                    if (selectedChatId == message.ChatID)
                    {
                        messages.Add(message); // Add to bottom
                        newMessageAdded = true;                     
                    }
                }
                else
                {
                    // Add new chat to the list as unread
                    chatList.Insert(0, new TelegramChatStatus
                    {
                        ChatID = message.ChatID,
                        Title = message.Title,
                        ReceivedTime = message.Datetime,
                        IsRead = false // Make sure this field is available
                    });
                }


                InvokeAsync(StateHasChanged);
            });


            try
            {
                await hubConnection.StartAsync();

                if (hubConnection.State == HubConnectionState.Connected)
                {
                    Console.WriteLine("✅ Connected to SignalR hub");
                    await hubConnection.SendAsync("JoinChatSession", "-123");
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

  
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {   
            
            if (newMessageAdded)
            {
                newMessageAdded = false;
                await JSRuntime.InvokeVoidAsync("scrollToBottom", "messagesContainer", 115);
            }
          
            if (firstRender)
            {
                var result = await UserState.GetUserAsync();
                username = result.username;
                if (result.userid == null || result.userid == "")
                {
                    userid = "0";
                       
                }
                else
                {
                        userid = result.userid;
                }

                chatList = await TelegramService.GetChatStatusListAsync(userid);
                StateHasChanged(); // refresh the UI after data is loaded
            }        

        }



        private string TabName = "";

        private async void OnTabClick(TabEventArgs args, string TabName_)
        {
            TabName = TabName_;
            if (TabName == "Summary")
            {
                //await LoadSummaryAsync();
            }
            else if (TabName == "Assign")
            {
                await LoadTelegramEmpAsync();
                await OnLoadAssign();
                StateHasChanged();
            }
        }

        private async Task SendReply()
        {
            var TelegramMessage = new TelegramMessage
            {
                ID = 0,
                ChatID = selectedChatId,
                MessageId = 0,
                Text = newMessage,
                FirstName = username,
                LastName = "(bot)",
                EmpId = userid,
                Username = "",
                Datetime = DateTime.Now,
                Raw = "",
                FromUserID = 0,
                Title = "",
            };

            // var json = JsonSerializer.Serialize(TelegramMessage);
            // var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {


                var response = await TelegramService.PostSendMessageAsync(TelegramMessage);

                if (response.IsSuccessStatusCode)
                {
                    responseMessage = "Message sent successfully!";
                    newMessage = "";
                    messages = await TelegramService.GetTelegramMessages(date1.ToDateTime(TimeOnly.MinValue), date2.ToDateTime(TimeOnly.MinValue), selectedChatId);

                    newMessageAdded = true;

                   

                }
                else
                {
                    responseMessage = $"Failed: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                responseMessage = $"Error: {ex.Message}";
            }
        }


        public async Task MarkAsRead(long chatId)
        {
            try
            {
                await TelegramService.PostMarkAsReadAsync(chatId, userid);

                var chat = chatList.FirstOrDefault(c => c.ChatID == chatId);
                if (chat != null)
                {
                    chat.IsRead = true;
                    chat.LastReadTime = DateTime.UtcNow; // Optional: update timestamp
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error marking as read: {ex.Message}");
            }
        }

        Tabs tabs = default!;
        private async Task OnChatClick(long chatId)
        {
            selectedChatId = chatId;
            messages = await TelegramService.GetTelegramMessages(date1.ToDateTime(TimeOnly.MinValue), date2.ToDateTime(TimeOnly.MinValue), chatId);

            newMessage = "";
            summaryData = "";
            Console.WriteLine("Onchatclick");
            await MarkAsRead(chatId);
            await tabs.ShowTabByIndexAsync(0);
            await JSRuntime.InvokeVoidAsync("scrollToBottom", "messagesContainer", 115);
            StateHasChanged();
        }

        private string? summaryData;
        private bool isSummaryLoading = false;



        private async Task LoadSummaryAsync()
        {
            isSummaryLoading = true;
            string promptText = "";
            string combinedText = string.Join("\n", messages
                         .Where(m => !string.IsNullOrWhiteSpace(m.Text))
                         .Select(m => m.Text));

            promptText = "Please summary this conversation, write a short summary to me easy understand :\n\n" + combinedText;







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

                summaryData = await AiService.GenerateContentAsync(request);
            }
            isSummaryLoading = false;
            StateHasChanged();

        }
        private async Task LoadTelegramEmpAsync()
        {
            isSummaryLoading = true;
            TelegramEmpList = await TelegramService.GetTelegramEmp();
            isSummaryLoading = false;

        }
        private async Task OnAssign()
        {
            // Find the first matching employee who is not already assigned
            var emp = TelegramEmpList
                .FirstOrDefault(e => e.EmpId == selectedEmpId && !AssignedEmpList.Any(a => a.EmpId == e.EmpId));

            if (emp != null)
            {
                await TelegramService.Post_AssignTelegramEmpAsync(selectedChatId, emp.EmpId, true);
                AssignedEmpList.Add(emp);
            }
        }
        private async Task OnUnassign(TelegramEmp emp)
        {
            var assigned = AssignedEmpList.FirstOrDefault(e => e.EmpId == emp.EmpId);

            if (assigned != null)
            {
                await TelegramService.Post_AssignTelegramEmpAsync(selectedChatId, assigned.EmpId, false);
                AssignedEmpList.Remove(assigned);
            }
        }
        private async Task OnLoadAssign()
        {
            AssignedEmpList = await TelegramService.GetTelegramEmpAssign(selectedChatId);
        }



        private async Task ByAIAsync(string action)
        {
            string promptText = "";
            isLoadingAI = true;

            try
            {
                Console.WriteLine($"By AI: {action}");

                if (action == "Translate")
                {
                    promptText = $"Please translate the following message into Khmer:\n\n\"{newMessage}\"";
                }
                else if (action == "CheckSpelling")
                {
                    promptText = $"Please correct any spelling and grammar mistakes in this message:\n\n\"{newMessage}\"";
                }
                else if (action == "Answer")
                {
                    string combinedText = string.Join("\n", messages
                        .Where(m => !string.IsNullOrWhiteSpace(m.Text))
                        .Select(m => m.Text));

                    promptText = "Based on the conversation below, write a short, clear, and professional reply to the customer:\n\n" + combinedText;
                }
                else if (action == "Thanks")
                {
                    string combinedText = string.Join("\n", messages
                        .Where(m => !string.IsNullOrWhiteSpace(m.Text))
                        .Select(m => m.Text));

                    promptText = "Based on the conversation below, write a short thank-you message to the customer in a polite and friendly tone:\n\n" + combinedText;
                }
                else if (action == "Summary")
                {
                    string combinedText = string.Join("\n", messages
                      .Where(m => !string.IsNullOrWhiteSpace(m.Text))
                      .Select(m => m.Text));

                    promptText = "Please summary this conversation, write a short summary to me easy understand :\n\n" + combinedText;


                }







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
            }
            catch (Exception ex)
            {
                isLoadingAI = false;
                summaryResult = ex.Message;
            }
            finally
            {
                isLoadingAI = false;
                await offcanvas!.ShowAsync();
            }
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
            newMessage = summaryResult;
            await ResizeTextareaAsync();
            isLoadingAI = false;
        }
        
        
     

        private async Task OnInputResize(ChangeEventArgs e)
        {
            await ResizeTextareaAsync();
        }
        private async Task ResizeTextareaAsync()
        {
            await JSRuntime.InvokeVoidAsync("resizeTextarea", "myTextarea");
        }
        
        private string selectedShow = "All";
        private string selectedSort = "Newest";
            private bool showUnreply = false;
         private bool showSearch = false;
    private string searchText = "";

        void SetShowFilter(string value)
        {
            selectedShow = value;
            // Call filtering logic here
        }

        void SetSortOrder(string value)
        {
            selectedSort = value;
            // Call sorting logic here
        }
         private void ToggleSearch()
        {
            showSearch = true;
        }

        private void CloseSearch()
        {
            showSearch = false;
            searchText = "";
        }
    }
}
