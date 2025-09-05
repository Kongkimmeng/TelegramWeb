using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Telegram_Web.Services.Impl;


namespace Telegram_Web.Pages.Account
{


    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserStateService _userState;
        public LoginModel(IHttpClientFactory httpClientFactory, UserStateService userState)
        {
            _httpClientFactory = httpClientFactory;
            _userState = userState;
        }
        

        [BindProperty]
        public LoginDto Input { get; set; }

        string ToMd5(string plainText)
        {
            using var md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert hash bytes to hex string
            var sb = new StringBuilder();
            foreach (var b in hashBytes)
                sb.Append(b.ToString("x2")); // lowercase hex

            return sb.ToString();
        }
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return Page();

            var client = _httpClientFactory.CreateClient("AuthApi");

             
            // Prepare form data
            var formData = new Dictionary<string, string>
            {
                ["username"] = Input.Username,
                ["md5password"] = ToMd5( Input.Password),
                ["apikey"] = "5e52aa7bc17ae1f2376f7a0a40a3c5d3",
                ["apisecret"] = "AwUAMDAvBANAcEATBAQAkDAQBAOAAFAEBwRAADAXBgY",
                ["clientip"] = "203.176.130.117",
                ["rememberMe"] = Input.RememberMe ? "true" : "false"
            };

            var content = new FormUrlEncodedContent(formData);

            // POST as form
            var response = await client.PostAsync("http://amis.angkornet.com.kh/api/AuthAPI.aspx", content);

            if (response.IsSuccessStatusCode)
            {
                var jsontext = await response.Content.ReadAsStringAsync();
                
               

                try
                {
                    using var doc = JsonDocument.Parse(jsontext);
                    var root = doc.RootElement;

                    //// Example: get a specific field
                    //bool success = root.GetProperty("success").GetBoolean();
                    //string token = root.GetProperty("token").GetString() ?? "";
                    string amisstatus = root[0].GetProperty("AMISUserStatus").GetString() ?? "";
                    string fullname = root[0].GetProperty("FullName").GetString() ?? ""; ;
                    string empid = root[0].GetProperty("EmployeeID").GetString() ?? ""; ;
                    string Team = root[0].GetProperty("TeamName").GetString() ?? ""; ;
                    if (amisstatus == "Active")
                    {

                        // Example: create claims and sign in
                        var claims = new[] 
                            { 
                                new Claim(ClaimTypes.Name, fullname) ,
                                new(ClaimTypes.Role, Team == "Developer" || Team == "CEO-MKN" ? "Admin" : "User"),
                                new Claim(ClaimTypes.UserData, jsontext),
                                new Claim("EmpID", empid),   // Employee ID
                                new Claim("Team", Team),      // Team Name
                                new Claim(ClaimTypes.UserData, jsontext) 
                            };
                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                        return Redirect("/SetStorage?returnURL=" + returnUrl ?? "/SetStorage?returnURL=");
                    }
                }
                catch
                {

                }
                

            }

            ModelState.AddModelError(string.Empty, "Invalid username or password");
            return Page();
        }
    }

    public record LoginDto(string Username, string Password, bool RememberMe = false);


}



