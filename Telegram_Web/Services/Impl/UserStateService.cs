
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Data;
using System.Data.SqlClient;

namespace Telegram_Web.Services.Impl
{
    public class UserStateService
    {         
        private readonly ProtectedLocalStorage _localStorage;

        public UserStateService(ProtectedLocalStorage localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task SetUserAsync(string username, string userid)
        {
            await _localStorage.SetAsync("username", username);
            await _localStorage.SetAsync("userid", userid);
        }

        public async Task<(string username, string userid)> GetUserAsync()
        {
            try
            {
                var usernameResult = await _localStorage.GetAsync<string>("username");
                var useridResult = await _localStorage.GetAsync<string>("userid");

                string username = usernameResult.Success && !string.IsNullOrEmpty(usernameResult.Value)
                    ? usernameResult.Value
                    : "";

                string userid = useridResult.Success  && !string.IsNullOrEmpty(useridResult.Value)
                    ? useridResult.Value
                    : "";

                return (username, userid);
            }
            catch
            {
                // If something goes wrong, return defaults instead of throwing
                return ("", "");
            }
        }


        public async Task ClearUserAsync()
        {
            await _localStorage.DeleteAsync("username");
            await _localStorage.DeleteAsync("userid");
        }
    }

}
