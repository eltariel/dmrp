using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace DiscordMultiRP.Web.Util
{
    public static class DiscordOAuthExtensions
    {
        private static readonly string apiBase = "https://discordapp.com/api/";

        public static void RegisterDiscordOAuth(this IServiceCollection services, IConfiguration dfg)
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "Discord";
                })
                .AddCookie()
                .AddOAuth("Discord", options =>
                {
                    options.ClientId = dfg["Discord:ClientId"];
                    options.ClientSecret = dfg["Discord:ClientSecret"];
                    options.CallbackPath = "/signin-discord";

                    options.AuthorizationEndpoint = apiBase + "oauth2/authorize";
                    options.TokenEndpoint = apiBase + "oauth2/token";
                    options.UserInformationEndpoint = apiBase + "users/@me";

                    var scopes = "identify,email,guilds,connections,guilds.join,gdm.join";
                    foreach (var s in scopes.Split(','))
                    {
                        options.Scope.Add(s);
                    }

                    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "username");
                    options.ClaimActions.MapJsonKey("urn:discord:discriminator", "discriminator");

                    options.SaveTokens = true;

                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                            var response = await context.Backchannel.SendAsync(request,
                                HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                            response.EnsureSuccessStatusCode();

                            var user = JObject.Parse(await response.Content.ReadAsStringAsync());

                            context.RunClaimActions(user);
                        }
                    };
                });
        }
    }
}