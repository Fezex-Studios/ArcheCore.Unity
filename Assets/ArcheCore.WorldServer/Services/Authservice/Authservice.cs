using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ArcheCore.WorldServer.ServerConfig;
using Newtonsoft.Json;
using UnityEngine;

namespace Shared.AuthService
{
    /// <summary>
    /// Calls the Auth Server's /validate-session endpoint.
    /// Returns the AccountId on success, or -1 if the token is invalid/expired.
    /// </summary>
    public static class AuthService
    {
        private static readonly HttpClient Http = new HttpClient();

        [Serializable]
        private class ValidateResponse
        {
            [JsonProperty("Valid")]
            public bool Valid;

            [JsonProperty("AccountId")]
            public int AccountId;
        }

        public static async Task<int> ValidateToken(string token)
        {
            try
            {
                string url  = $"{ConfigService.Config.AuthServerUrl}/validate-session";
                string body = JsonConvert.SerializeObject(new { Token = token });

                StringContent content = new StringContent(
                    body,
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = await Http.PostAsync(url, content);

                string json = await response.Content.ReadAsStringAsync();

                ValidateResponse result = JsonConvert.DeserializeObject<ValidateResponse>(json);

                if (result == null)
                {
                    Debug.LogError("[AuthService] Empty or malformed response from auth server");
                    return -1;
                }

                return result.Valid ? result.AccountId : -1;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthService] Validation request failed: {e.Message}");
                return -1;
            }
        }
    }
}