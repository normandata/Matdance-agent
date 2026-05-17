using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Matdance.Cli.Services;

public sealed class WebAuthService
{
    public const string TokenEnvironmentVariable = "MATDANCE_WEB_TOKEN";
    public const string CookieName = "matdance_auth";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string? _token;

    private WebAuthService(bool enabled, string? token, string source, bool remoteBinding, string? generatedToken)
    {
        Enabled = enabled;
        Source = source;
        RemoteBinding = remoteBinding;
        GeneratedToken = generatedToken;
        _token = token;
    }

    public bool Enabled { get; }
    public string Source { get; }
    public bool RemoteBinding { get; }
    public string? GeneratedToken { get; }

    public static WebAuthService LoadOrCreate(string host)
    {
        var remoteBinding = !IsLoopbackHost(host);
        var envToken = Environment.GetEnvironmentVariable(TokenEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(envToken))
            return new WebAuthService(true, envToken.Trim(), "environment", remoteBinding, null);

        var stored = ReadState();
        if (!string.IsNullOrWhiteSpace(stored?.Token))
            return new WebAuthService(true, stored.Token.Trim(), "state", remoteBinding, null);

        if (!remoteBinding)
            return new WebAuthService(false, null, "disabled", remoteBinding, null);

        var generated = GenerateToken();
        WriteState(new WebAuthState
        {
            Token = generated,
            CreatedAt = UserTimeZoneService.Now()
        });
        return new WebAuthService(true, generated, "generated", remoteBinding, generated);
    }

    public static string? TryReadConfiguredToken()
    {
        var envToken = Environment.GetEnvironmentVariable(TokenEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(envToken))
            return envToken.Trim();

        var stored = ReadState();
        return string.IsNullOrWhiteSpace(stored?.Token) ? null : stored.Token.Trim();
    }

    public bool IsAuthorized(HttpContext context)
    {
        if (!Enabled)
            return true;

        return Validate(ExtractPresentedToken(context));
    }

    public bool Validate(string? token)
    {
        if (string.IsNullOrWhiteSpace(_token) || string.IsNullOrWhiteSpace(token))
            return false;

        var expected = Encoding.UTF8.GetBytes(_token);
        var actual = Encoding.UTF8.GetBytes(token.Trim());
        return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public static void SetAuthCookie(HttpContext context, string token)
    {
        context.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }

    public static void ClearAuthCookie(HttpContext context)
    {
        context.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            Path = "/",
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps
        });
    }

    public static string StatePath => Path.Combine(MatdanceRuntime.StateRoot, "web-auth.json");

    private static string? ExtractPresentedToken(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        const string bearer = "Bearer ";
        if (authorization.StartsWith(bearer, StringComparison.OrdinalIgnoreCase))
            return authorization[bearer.Length..].Trim();

        var headerToken = context.Request.Headers["X-Matdance-Token"].ToString();
        if (!string.IsNullOrWhiteSpace(headerToken))
            return headerToken.Trim();

        return context.Request.Cookies.TryGetValue(CookieName, out var cookieToken)
            ? cookieToken
            : null;
    }

    private static WebAuthState? ReadState()
    {
        if (!File.Exists(StatePath))
            return null;

        try
        {
            return JsonSerializer.Deserialize<WebAuthState>(File.ReadAllText(StatePath), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static void WriteState(WebAuthState state)
    {
        Directory.CreateDirectory(MatdanceRuntime.StateRoot);
        if (state.CreatedAt != default && state.CreatedAt != DateTimeOffset.MinValue)
            state.CreatedAt = UserTimeZoneService.ToUserTime(state.CreatedAt);
        AtomicFile.WriteAllText(StatePath, JsonSerializer.Serialize(state, JsonOptions));
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal);
    }

    private static bool IsLoopbackHost(string host)
    {
        var value = (host ?? string.Empty).Trim().Trim('[', ']');
        if (value.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        return IPAddress.TryParse(value, out var address) && IPAddress.IsLoopback(address);
    }

    private sealed class WebAuthState
    {
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
