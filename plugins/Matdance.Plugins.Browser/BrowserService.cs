using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Matdance.Plugins.Browser;

public sealed class BrowserService : IAsyncDisposable
{
    private static BrowserService? _instance;
    private static readonly object _instanceLock = new();
    private static readonly JsonSerializerOptions CookieJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly HashSet<string> MultiLabelPublicSuffixHints = new(StringComparer.OrdinalIgnoreCase)
    {
        "com.cn", "net.cn", "org.cn", "gov.cn", "edu.cn",
        "co.uk", "org.uk", "ac.uk", "gov.uk",
        "com.au", "net.au", "org.au", "edu.au",
        "co.jp", "ne.jp", "or.jp",
        "co.kr", "go.kr", "or.kr",
        "com.br", "com.mx", "com.tr",
        "co.nz", "co.in", "firm.in", "net.in", "org.in"
    };
    private const int OperationLockTimeoutMs = 45_000;
    private const int BrowserStartupTimeoutMs = 60_000;
    private const int BrowserContextTimeoutMs = 20_000;
    private const int DefaultEvaluateTimeoutMs = 8_000;
    private const int MaxEvaluateTimeoutMs = 30_000;
    private const int HungPageRecoveryTimeoutMs = 3_000;
    private const int NavigationTimeoutMs = 35_000;
    private const int MaxNetworkIdleWaitMs = 30_000;
    private const int PageReadTimeoutMs = 10_000;
    private const int ScreenshotTimeoutMs = 15_000;
    private const int TitleTimeoutMs = 5_000;
    private const int DefaultWaitTimeoutMs = 10_000;
    private const int MaxDynamicTimeoutMs = 30_000;
    private const int ToolTimeoutBufferMs = 2_000;
    private const int ScrollTotalTimeoutMs = 45_000;
    private const int CrawlTotalTimeoutMs = 90_000;
    private const int CookieOperationTimeoutMs = 20_000;
    private const int MaxInitScriptLength = 25_000;
    private const string EvaluateWrapper = """
        async ({ script, timeoutMs }) => {
          const execute = async () => {
            const indirectEval = (0, eval);
            try {
              const value = indirectEval(script);
              if (typeof value === 'function') {
                return await value();
              }
              return await value;
            } catch (firstError) {
              try {
                const AsyncFunction = Object.getPrototypeOf(async function(){}).constructor;
                return await new AsyncFunction(script)();
              } catch {
                throw firstError;
              }
            }
          };

          let timer = 0;
          const timeout = new Promise((_, reject) => {
            timer = setTimeout(() => reject(new Error(`Matdance browser_evaluate timed out after ${timeoutMs}ms.`)), timeoutMs);
          });
          try {
            return await Promise.race([execute(), timeout]);
          } finally {
            clearTimeout(timer);
          }
        }
        """;
    
    public static BrowserService Instance
    {
        get
        {
            lock (_instanceLock)
            {
                _instance ??= new BrowserService();
                return _instance;
            }
        }
    }
    
    private BrowserService()
    {
    }
    
    public static void ResetInstance()
    {
        lock (_instanceLock)
        {
            _instance = null;
        }
    }

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private bool _isHeadless = true;
    private bool _screencastRunning = false;
    private System.Threading.Timer? _keepAliveTimer;
    private readonly object _traceLock = new();
    private readonly List<BrowserTraceEvent> _traceEvents = new();
    private readonly HashSet<IPage> _traceAttachedPages = new();
    private bool _traceEnabled;
    private bool _traceNetwork = true;
    private bool _traceConsole = true;
    private int _traceMaxEvents = 200;

    public bool IsRunning => _browser != null && _browser.IsConnected;
    public bool IsBusy { get; private set; } = false;
    public bool IsScreencastRunning => _screencastRunning;
    public event Action<byte[]>? OnScreencastFrame;

    public async Task<string> EnsureBrowserAsync(bool headless = true, CancellationToken ct = default)
    {
        if (!await _gate.WaitAsync(TimeSpan.FromMilliseconds(BrowserStartupTimeoutMs), ct))
            throw new TimeoutException($"Timed out after {BrowserStartupTimeoutMs}ms waiting for browser startup/initialization.");

        try
        {
            ct.ThrowIfCancellationRequested();
            if (_browser != null && _browser.IsConnected)
            {
                if (_isHeadless != headless)
                {
                    return $"[browser] Browser already running (headless={_isHeadless}); requested headless={headless} ignored to preserve the current page, login state, and browser session.";
                }

                return "[browser] Browser already running.";
            }

            if (_browser != null)
            {
                try { await _browser.CloseAsync().WaitAsync(TimeSpan.FromMilliseconds(BrowserContextTimeoutMs), ct); } catch { }
                _browser = null;
                _context = null;
                _page = null;
            }

            _playwright ??= await Playwright.CreateAsync().WaitAsync(TimeSpan.FromMilliseconds(BrowserStartupTimeoutMs), ct);
            var requestedHeadless = headless;
            headless = true;
            _isHeadless = headless;

            var launchArgs = new List<string>
            {
                "--disable-blink-features=AutomationControlled",
                "--no-sandbox",
                "--disable-dev-shm-usage",
                "--disable-gpu",
                "--disable-background-timer-throttling",
                "--disable-renderer-backgrounding",
                "--disable-backgrounding-occluded-windows"
            };

            if (OperatingSystem.IsMacOS())
            {
                launchArgs.Add("--disable-features=TranslateUI");
                launchArgs.Add("--disable-ipc-flooding-protection");
            }

            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = headless,
                Args = launchArgs,
                Timeout = BrowserStartupTimeoutMs
            }).WaitAsync(TimeSpan.FromMilliseconds(BrowserStartupTimeoutMs), ct);
            _context = await _browser.NewContextAsync(CreateContextOptions()).WaitAsync(TimeSpan.FromMilliseconds(BrowserContextTimeoutMs), ct);
            _page = await _context.NewPageAsync().WaitAsync(TimeSpan.FromMilliseconds(BrowserContextTimeoutMs), ct);
            AttachTraceHandlers(_page);
            StartKeepAlive();
            var visibleNote = requestedHeadless
                ? string.Empty
                : " Requested visible/headless=false launch was ignored; Matdance keeps browser automation in background mode and streams it through the Web UI overlay.";
            return $"[browser] Chromium launched (headless={headless}).{visibleNote}";
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> NavigateAsync(string url, int waitUntilNetworkIdle = 0, CancellationToken ct = default)
    {
        var networkIdleTimeoutMs = ClampSecondsToMilliseconds(waitUntilNetworkIdle, MaxNetworkIdleWaitMs);
        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            var options = new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = NavigationTimeoutMs };
            var response = await _page!.GotoAsync(url, options).WaitAsync(TimeSpan.FromMilliseconds(NavigationTimeoutMs), ct);
            if (networkIdleTimeoutMs > 0)
            {
                try { await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = networkIdleTimeoutMs }).WaitAsync(TimeSpan.FromMilliseconds(networkIdleTimeoutMs + ToolTimeoutBufferMs), ct); }
                catch (TimeoutException) { /* ignore */ }
            }
            var status = response?.Status ?? 0;
            var title = await _page.TitleAsync().WaitAsync(TimeSpan.FromMilliseconds(TitleTimeoutMs), ct);
            return $"[browser_navigate] URL: {url}\nStatus: {status}\nTitle: {title}";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> ClickAsync(string selector, int timeout = 5000, CancellationToken ct = default)
    {
        timeout = ClampMilliseconds(timeout, 5_000, 500, MaxDynamicTimeoutMs);
        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            await _page!.ClickAsync(selector, new PageClickOptions { Timeout = timeout }).WaitAsync(TimeSpan.FromMilliseconds(timeout + ToolTimeoutBufferMs), ct);
            return $"[browser_click] Clicked '{selector}'.";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> TypeAsync(string selector, string text, bool submit = false, int timeout = 5000, CancellationToken ct = default)
    {
        timeout = ClampMilliseconds(timeout, 5_000, 500, MaxDynamicTimeoutMs);
        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            await _page!.FillAsync(selector, text, new PageFillOptions { Timeout = timeout }).WaitAsync(TimeSpan.FromMilliseconds(timeout + ToolTimeoutBufferMs), ct);
            if (submit)
            {
                await _page.PressAsync(selector, "Enter", new PagePressOptions { Timeout = timeout }).WaitAsync(TimeSpan.FromMilliseconds(timeout + ToolTimeoutBufferMs), ct);
            }
            return $"[browser_type] Typed into '{selector}'.";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> ScreenshotAsync(string? outputPath = null, bool fullPage = false, CancellationToken ct = default)
    {
        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            var path = outputPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                var fileName = $"screenshot_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png";
                var browserTempDir = Path.Combine(Directory.GetCurrentDirectory(), "browser_temp");
                path = Path.Combine(browserTempDir, fileName);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await _page!.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = path,
                FullPage = fullPage,
                Type = ScreenshotType.Png
            }).WaitAsync(TimeSpan.FromMilliseconds(ScreenshotTimeoutMs), ct);
            return $"[browser_screenshot] Saved to: {path}";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> GetContentAsync(bool html = false, int maxLength = 12000, CancellationToken ct = default)
    {
        maxLength = Math.Clamp(maxLength <= 0 ? 12000 : maxLength, 500, 50_000);
        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            string content;
            if (html)
            {
                content = await _page!.ContentAsync().WaitAsync(TimeSpan.FromMilliseconds(PageReadTimeoutMs), ct);
            }
            else
            {
                content = await _page!.InnerTextAsync("body", new PageInnerTextOptions { Timeout = PageReadTimeoutMs })
                    .WaitAsync(TimeSpan.FromMilliseconds(PageReadTimeoutMs + 1_000), ct);
            }
            if (content.Length > maxLength)
            {
                content = content.Substring(0, maxLength) + "\n...[truncated]";
            }
            return $"[browser_get_content] {(html ? "HTML" : "Text")} ({content.Length} chars):\n{content}";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> EvaluateAsync(string script, int timeoutMs = DefaultEvaluateTimeoutMs, CancellationToken ct = default)
    {
        timeoutMs = Math.Clamp(timeoutMs <= 0 ? DefaultEvaluateTimeoutMs : timeoutMs, 1_000, MaxEvaluateTimeoutMs);
        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            var evaluateTask = _page!.EvaluateAsync<object?>(EvaluateWrapper, new { script, timeoutMs });
            var completed = await Task.WhenAny(evaluateTask, Task.Delay(timeoutMs + 2_000, ct));
            ct.ThrowIfCancellationRequested();
            if (completed != evaluateTask)
            {
                _ = evaluateTask.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
                var recovery = await RecoverPageAfterHungOperationAsync("browser_evaluate host timeout", ct);
                return $"[browser_evaluate] Timed out after {timeoutMs}ms while waiting for the page evaluation to return. {recovery}";
            }

            var result = await evaluateTask;
            var json = FormatEvaluateResult(result);
            if (json.Length > 8000)
            {
                json = json.Substring(0, 8000) + "\n...[truncated]";
            }
            return $"[browser_evaluate] Result:\n{json}";
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Matdance browser_evaluate timed out", StringComparison.OrdinalIgnoreCase))
        {
            return $"[browser_evaluate] Timed out after {timeoutMs}ms. The script did not finish. Keep future evaluate scripts short, synchronous when possible, and avoid waiting for UI/network conditions inside browser_evaluate.";
        }
        catch (TimeoutException ex)
        {
            var recovery = await RecoverPageAfterHungOperationAsync("browser_evaluate timeout", ct);
            return $"[browser_evaluate] Timed out: {ex.Message}. {recovery}";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> WaitForAsync(string kind, string? selector, string? text, string? state, bool regex, int timeoutMs, CancellationToken ct = default)
    {
        timeoutMs = Math.Clamp(timeoutMs <= 0 ? DefaultWaitTimeoutMs : timeoutMs, 500, MaxDynamicTimeoutMs);
        var waitKind = (kind ?? string.Empty).Trim().ToLowerInvariant();
        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            switch (waitKind)
            {
                case "selector":
                {
                    if (string.IsNullOrWhiteSpace(selector))
                        return "[browser_wait_for] selector is required for kind=selector.";

                    var selectorState = ParseSelectorState(state);
                    await _page!.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                    {
                        State = selectorState,
                        Timeout = timeoutMs
                    }).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + ToolTimeoutBufferMs), ct);
                    return $"[browser_wait_for] Selector matched: {selector} state={selectorState} timeout={timeoutMs}ms.";
                }
                case "text":
                {
                    if (string.IsNullOrWhiteSpace(text))
                        return "[browser_wait_for] text is required for kind=text.";
                    if (regex)
                        _ = new Regex(text, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                    await _page!.WaitForFunctionAsync(
                        """
                        ({ value, regex }) => {
                          const body = document.body ? (document.body.innerText || document.body.textContent || '') : '';
                          if (regex) return new RegExp(value, 'i').test(body);
                          return body.toLowerCase().includes(String(value).toLowerCase());
                        }
                        """,
                        new { value = text, regex },
                        new PageWaitForFunctionOptions { Timeout = timeoutMs }).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + ToolTimeoutBufferMs), ct);
                    return $"[browser_wait_for] Text matched: {TrimForMessage(text, 160)} timeout={timeoutMs}ms.";
                }
                case "url":
                {
                    if (string.IsNullOrWhiteSpace(text))
                        return "[browser_wait_for] text is required for kind=url.";
                    if (regex)
                        _ = new Regex(text, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                    await _page!.WaitForFunctionAsync(
                        """
                        ({ value, regex }) => {
                          const href = location.href;
                          if (regex) return new RegExp(value, 'i').test(href);
                          return href.toLowerCase().includes(String(value).toLowerCase());
                        }
                        """,
                        new { value = text, regex },
                        new PageWaitForFunctionOptions { Timeout = timeoutMs }).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + ToolTimeoutBufferMs), ct);
                    return $"[browser_wait_for] URL matched: {_page.Url}";
                }
                case "load_state":
                {
                    var loadState = ParseLoadState(state);
                    await _page!.WaitForLoadStateAsync(loadState, new PageWaitForLoadStateOptions { Timeout = timeoutMs }).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + ToolTimeoutBufferMs), ct);
                    return $"[browser_wait_for] Load state reached: {loadState} timeout={timeoutMs}ms.";
                }
                case "function":
                {
                    if (string.IsNullOrWhiteSpace(text))
                        return "[browser_wait_for] text must contain a short JavaScript predicate for kind=function.";
                    var script = text.Trim();
                    var block = GetUnsafeDynamicScriptReason(script);
                    if (block != null)
                        return "[blocked] " + block;

                    await _page!.WaitForFunctionAsync(script, null, new PageWaitForFunctionOptions { Timeout = timeoutMs }).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + ToolTimeoutBufferMs), ct);
                    return $"[browser_wait_for] JavaScript predicate became truthy timeout={timeoutMs}ms.";
                }
                default:
                    return "[browser_wait_for] kind must be selector, text, url, load_state, or function.";
            }
        }
        catch (TimeoutException)
        {
            return $"[browser_wait_for] Timed out after {timeoutMs}ms waiting for {waitKind}.";
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
        {
            return $"[browser_wait_for] Timed out after {timeoutMs}ms waiting for {waitKind}: {ex.Message}";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> QueryAsync(string? selector, string? text, int limit, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit <= 0 ? 30 : limit, 1, 100);
        var targetSelector = string.IsNullOrWhiteSpace(selector)
            ? "a,button,input,textarea,select,[role],[aria-label],[data-testid],main,article,section,h1,h2,h3,li"
            : selector.Trim();

        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            var json = await _page!.EvaluateAsync<string>(
                """
                ({ selector, filter, limit }) => {
                  const clean = value => String(value || '').replace(/\s+/g, ' ').trim();
                  const cssEscape = value => {
                    if (window.CSS && CSS.escape) return CSS.escape(value);
                    return String(value).replace(/["\\]/g, '\\$&');
                  };
                  const selectorFor = el => {
                    if (el.id) return '#' + cssEscape(el.id);
                    const testid = el.getAttribute('data-testid');
                    if (testid) return `${el.tagName.toLowerCase()}[data-testid="${cssEscape(testid)}"]`;
                    const aria = el.getAttribute('aria-label');
                    if (aria) return `${el.tagName.toLowerCase()}[aria-label="${cssEscape(aria)}"]`;
                    const name = el.getAttribute('name');
                    if (name) return `${el.tagName.toLowerCase()}[name="${cssEscape(name)}"]`;
                    const parts = [];
                    let node = el;
                    while (node && node.nodeType === 1 && parts.length < 4) {
                      let part = node.tagName.toLowerCase();
                      if (node.classList && node.classList.length) {
                        part += '.' + Array.from(node.classList).slice(0, 2).map(cssEscape).join('.');
                      }
                      const parent = node.parentElement;
                      if (parent) {
                        const siblings = Array.from(parent.children).filter(item => item.tagName === node.tagName);
                        if (siblings.length > 1) part += `:nth-of-type(${siblings.indexOf(node) + 1})`;
                      }
                      parts.unshift(part);
                      node = parent;
                    }
                    return parts.join(' > ');
                  };
                  const filterText = clean(filter).toLowerCase();
                  const items = [];
                  for (const el of Array.from(document.querySelectorAll(selector))) {
                    if (items.length >= limit) break;
                    const rect = el.getBoundingClientRect();
                    const visible = !!(rect.width || rect.height || el.getClientRects().length);
                    const ownText = clean(el.innerText || el.textContent).slice(0, 500);
                    const label = clean(el.getAttribute('aria-label') || el.getAttribute('title') || el.getAttribute('alt'));
                    const haystack = `${ownText} ${label} ${el.getAttribute('href') || ''}`.toLowerCase();
                    if (filterText && !haystack.includes(filterText)) continue;
                    items.push({
                      index: items.length + 1,
                      tag: el.tagName.toLowerCase(),
                      selector: selectorFor(el),
                      text: ownText.slice(0, 240),
                      label,
                      role: el.getAttribute('role') || '',
                      name: el.getAttribute('name') || '',
                      type: el.getAttribute('type') || '',
                      href: el.href || el.getAttribute('href') || '',
                      visible,
                      disabled: !!el.disabled || el.getAttribute('aria-disabled') === 'true',
                      rect: { x: Math.round(rect.x), y: Math.round(rect.y), width: Math.round(rect.width), height: Math.round(rect.height) }
                    });
                  }
                  return JSON.stringify({ url: location.href, title: document.title, selector, filter: filter || '', count: items.length, items }, null, 2);
                }
                """,
                new { selector = targetSelector, filter = text ?? "", limit }).WaitAsync(TimeSpan.FromMilliseconds(PageReadTimeoutMs), ct);

            return $"[browser_query] DOM candidates:\n{json}";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> ScrollAsync(string? selector, string? direction, int pixels, int steps, string? untilSelector, string? untilText, int delayMs, CancellationToken ct = default)
    {
        pixels = Math.Clamp(pixels <= 0 ? 900 : pixels, 100, 3000);
        steps = Math.Clamp(steps <= 0 ? 1 : steps, 1, 30);
        delayMs = Math.Clamp(delayMs < 0 ? 300 : delayMs, 0, 3000);
        var dir = (direction ?? "down").Trim().ToLowerInvariant();
        if (dir is not ("down" or "up" or "left" or "right"))
            return "[browser_scroll] direction must be down, up, left, or right.";

        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            var stopwatch = Stopwatch.StartNew();
            var stoppedBy = "";
            var finalState = "{}";
            var completedSteps = 0;
            for (var i = 0; i < steps; i++)
            {
                var remainingMs = RemainingMilliseconds(stopwatch, ScrollTotalTimeoutMs);
                if (remainingMs <= 0)
                {
                    stoppedBy = "tool_timeout";
                    break;
                }

                finalState = await _page!.EvaluateAsync<string>(
                    """
                    ({ selector, direction, pixels, untilSelector, untilText }) => {
                      const root = selector ? document.querySelector(selector) : (document.scrollingElement || document.documentElement);
                      if (!root) return JSON.stringify({ ok: false, reason: 'scroll container not found', selector });
                      const dx = direction === 'left' ? -pixels : direction === 'right' ? pixels : 0;
                      const dy = direction === 'up' ? -pixels : direction === 'down' ? pixels : 0;
                      root.scrollBy(dx, dy);
                      const visible = el => {
                        if (!el) return false;
                        const rect = el.getBoundingClientRect();
                        return !!(rect.width || rect.height || el.getClientRects().length);
                      };
                      let matchedSelector = false;
                      if (untilSelector) matchedSelector = visible(document.querySelector(untilSelector));
                      let matchedText = false;
                      if (untilText) {
                        const body = document.body ? (document.body.innerText || document.body.textContent || '') : '';
                        matchedText = body.toLowerCase().includes(String(untilText).toLowerCase());
                      }
                      return JSON.stringify({
                        ok: true,
                        url: location.href,
                        title: document.title,
                        stepScrollX: dx,
                        stepScrollY: dy,
                        scrollX: Math.round(window.scrollX),
                        scrollY: Math.round(window.scrollY),
                        containerScrollLeft: Math.round(root.scrollLeft || 0),
                        containerScrollTop: Math.round(root.scrollTop || 0),
                        matchedSelector,
                        matchedText
                      }, null, 2);
                    }
                    """,
                    new { selector, direction = dir, pixels, untilSelector, untilText }).WaitAsync(TimeSpan.FromMilliseconds(Math.Min(PageReadTimeoutMs, remainingMs)), ct);
                completedSteps++;

                if (finalState.Contains("\"matchedSelector\": true", StringComparison.OrdinalIgnoreCase))
                {
                    stoppedBy = "until_selector";
                    break;
                }
                if (finalState.Contains("\"matchedText\": true", StringComparison.OrdinalIgnoreCase))
                {
                    stoppedBy = "until_text";
                    break;
                }
                if (i + 1 < steps && delayMs > 0)
                    await Task.Delay(Math.Min(delayMs, Math.Max(0, RemainingMilliseconds(stopwatch, ScrollTotalTimeoutMs))), ct);
            }

            var suffix = string.IsNullOrWhiteSpace(stoppedBy) ? "" : $"\nStopped by: {stoppedBy}";
            return $"[browser_scroll] Completed {completedSteps}/{steps} bounded step(s).{suffix}\n{finalState}";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> InjectInitScriptAsync(string script, string purpose, CancellationToken ct = default)
    {
        script = script?.Trim() ?? string.Empty;
        purpose = purpose?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(script))
            return "[browser_inject_init_script] script is required.";
        if (string.IsNullOrWhiteSpace(purpose))
            return "[browser_inject_init_script] purpose is required.";
        if (script.Length > MaxInitScriptLength)
            return $"[browser_inject_init_script] script is too long; keep init scripts under {MaxInitScriptLength} characters.";

        var block = GetUnsafeDynamicScriptReason(script + "\n" + purpose);
        if (block != null)
            return "[blocked] " + block;

        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            await _context!.AddInitScriptAsync(script).WaitAsync(TimeSpan.FromMilliseconds(BrowserContextTimeoutMs), ct);
            return $"[browser_inject_init_script] Init script installed for future navigations. Purpose: {TrimForMessage(purpose, 240)}";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> GetUrlAsync(CancellationToken ct = default)
    {
        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            var url = _page!.Url;
            var title = await _page.TitleAsync().WaitAsync(TimeSpan.FromMilliseconds(TitleTimeoutMs), ct);
            return $"[browser] Current URL: {url}\nTitle: {title}";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> SourceAnalyzeAsync(bool includeInline = false, int limit = 80, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit <= 0 ? 80 : limit, 1, 200);
        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            var json = await _page!.EvaluateAsync<string>(
                """
                ({ includeInline, limit }) => {
                  const clean = value => String(value || '').replace(/\s+/g, ' ').trim();
                  const clip = (value, max) => {
                    value = clean(value);
                    return value.length > max ? value.slice(0, max) + '...' : value;
                  };
                  const attr = (el, name) => el.getAttribute(name) || '';
                  const scriptInfo = Array.from(document.scripts).slice(0, limit).map((el, index) => {
                    const inline = !el.src;
                    return {
                      index,
                      src: el.src || '',
                      type: attr(el, 'type') || 'classic',
                      async: !!el.async,
                      defer: !!el.defer,
                      module: (attr(el, 'type') || '').toLowerCase() === 'module',
                      inline,
                      length: inline ? (el.textContent || '').length : 0,
                      preview: includeInline && inline ? clip(el.textContent || '', 500) : ''
                    };
                  });
                  const styles = Array.from(document.querySelectorAll('link[rel~="stylesheet"], style')).slice(0, limit).map((el, index) => ({
                    index,
                    tag: el.tagName.toLowerCase(),
                    href: el.href || '',
                    inline: el.tagName.toLowerCase() === 'style',
                    length: el.tagName.toLowerCase() === 'style' ? (el.textContent || '').length : 0,
                    preview: includeInline && el.tagName.toLowerCase() === 'style' ? clip(el.textContent || '', 500) : ''
                  }));
                  const forms = Array.from(document.forms).slice(0, limit).map((form, index) => ({
                    index,
                    id: form.id || '',
                    name: form.getAttribute('name') || '',
                    method: form.method || '',
                    action: form.action || '',
                    inputCount: form.elements ? form.elements.length : 0
                  }));
                  const eventAttrs = Array.from(document.querySelectorAll('*'))
                    .flatMap((el, index) => Array.from(el.attributes || [])
                      .filter(a => a.name.toLowerCase().startsWith('on'))
                      .map(a => ({ index, tag: el.tagName.toLowerCase(), id: el.id || '', event: a.name.toLowerCase(), handlerPreview: includeInline ? clip(a.value, 220) : '' })))
                    .slice(0, limit);
                  const meta = Array.from(document.querySelectorAll('meta')).slice(0, limit).map(el => ({
                    name: attr(el, 'name') || attr(el, 'property') || attr(el, 'http-equiv'),
                    content: clip(attr(el, 'content'), 240)
                  }));
                  const links = Array.from(document.querySelectorAll('a[href]')).slice(0, limit).map(el => ({
                    text: clip(el.innerText || el.textContent || '', 120),
                    href: el.href
                  }));
                  return JSON.stringify({
                    url: location.href,
                    title: document.title,
                    doctype: document.doctype ? document.doctype.name : '',
                    lang: document.documentElement ? document.documentElement.lang : '',
                    counts: {
                      scripts: document.scripts.length,
                      stylesheets: document.querySelectorAll('link[rel~="stylesheet"], style').length,
                      forms: document.forms.length,
                      links: document.querySelectorAll('a[href]').length,
                      eventHandlers: eventAttrs.length
                    },
                    scripts: scriptInfo,
                    styles,
                    forms,
                    eventHandlers: eventAttrs,
                    meta,
                    links
                  }, null, 2);
                }
                """,
                new { includeInline, limit }).WaitAsync(TimeSpan.FromMilliseconds(PageReadTimeoutMs), ct);
            if (json.Length > 20000)
                json = json[..20000] + "\n...[truncated]";
            return "[browser_source_analyze] Page source structure:\n" + json;
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> VerifyAsync(string kind, string? selector, string? text, string? state, bool regex, bool negate, int timeoutMs, CancellationToken ct = default)
    {
        timeoutMs = Math.Clamp(timeoutMs <= 0 ? DefaultWaitTimeoutMs : timeoutMs, 500, MaxDynamicTimeoutMs);
        var verifyKind = (kind ?? string.Empty).Trim().ToLowerInvariant();
        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            switch (verifyKind)
            {
                case "selector":
                {
                    if (string.IsNullOrWhiteSpace(selector))
                        return "[browser_verify] selector is required for kind=selector.";

                    var selectorState = (state ?? "visible").Trim().ToLowerInvariant();
                    if (negate)
                    {
                        await _page!.WaitForFunctionAsync(
                            """
                            ({ selector, state }) => {
                              const visible = el => {
                                if (!el) return false;
                                const rect = el.getBoundingClientRect();
                                return !!(rect.width || rect.height || el.getClientRects().length);
                              };
                              const items = Array.from(document.querySelectorAll(selector));
                              if (state === 'attached') return items.length === 0;
                              if (state === 'detached') return items.length > 0;
                              if (state === 'hidden') return items.some(visible);
                              return !items.some(visible);
                            }
                            """,
                            new { selector, state = selectorState },
                            new PageWaitForFunctionOptions { Timeout = timeoutMs }).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + ToolTimeoutBufferMs), ct);
                    }
                    else
                    {
                        await _page!.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                        {
                            State = ParseSelectorState(selectorState),
                            Timeout = timeoutMs
                        }).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + ToolTimeoutBufferMs), ct);
                    }

                    return "[browser_verify] ok\n" + JsonSerializer.Serialize(new
                    {
                        ok = true,
                        kind = verifyKind,
                        selector,
                        state = selectorState,
                        negate,
                        url = _page!.Url,
                        title = await _page.TitleAsync().WaitAsync(TimeSpan.FromMilliseconds(TitleTimeoutMs), ct)
                    }, CookieJsonOptions);
                }
                case "text":
                {
                    if (string.IsNullOrWhiteSpace(text))
                        return "[browser_verify] text is required for kind=text.";
                    if (regex)
                        _ = new Regex(text, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                    await _page!.WaitForFunctionAsync(
                        """
                        ({ value, regex, negate }) => {
                          const body = document.body ? (document.body.innerText || document.body.textContent || '') : '';
                          const matched = regex
                            ? new RegExp(value, 'i').test(body)
                            : body.toLowerCase().includes(String(value).toLowerCase());
                          return negate ? !matched : matched;
                        }
                        """,
                        new { value = text, regex, negate },
                        new PageWaitForFunctionOptions { Timeout = timeoutMs }).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + ToolTimeoutBufferMs), ct);
                    return "[browser_verify] ok\n" + JsonSerializer.Serialize(new
                    {
                        ok = true,
                        kind = verifyKind,
                        text = TrimForMessage(text, 240),
                        regex,
                        negate,
                        url = _page!.Url
                    }, CookieJsonOptions);
                }
                case "url":
                {
                    if (string.IsNullOrWhiteSpace(text))
                        return "[browser_verify] text is required for kind=url.";
                    if (regex)
                        _ = new Regex(text, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

                    await _page!.WaitForFunctionAsync(
                        """
                        ({ value, regex, negate }) => {
                          const href = location.href;
                          const matched = regex
                            ? new RegExp(value, 'i').test(href)
                            : href.toLowerCase().includes(String(value).toLowerCase());
                          return negate ? !matched : matched;
                        }
                        """,
                        new { value = text, regex, negate },
                        new PageWaitForFunctionOptions { Timeout = timeoutMs }).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + ToolTimeoutBufferMs), ct);
                    return "[browser_verify] ok\n" + JsonSerializer.Serialize(new
                    {
                        ok = true,
                        kind = verifyKind,
                        expected = text,
                        regex,
                        negate,
                        url = _page!.Url
                    }, CookieJsonOptions);
                }
                case "load_state":
                {
                    if (negate)
                        return "[browser_verify] negate is not supported for kind=load_state.";

                    var loadState = ParseLoadState(state);
                    await _page!.WaitForLoadStateAsync(loadState, new PageWaitForLoadStateOptions { Timeout = timeoutMs }).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + ToolTimeoutBufferMs), ct);
                    return "[browser_verify] ok\n" + JsonSerializer.Serialize(new
                    {
                        ok = true,
                        kind = verifyKind,
                        state = loadState.ToString(),
                        url = _page!.Url
                    }, CookieJsonOptions);
                }
                case "function":
                {
                    if (string.IsNullOrWhiteSpace(text))
                        return "[browser_verify] text must contain a short JavaScript predicate for kind=function.";
                    var script = text.Trim();
                    var block = GetUnsafeDynamicScriptReason(script);
                    if (block != null)
                        return "[blocked] " + block;

                    await _page!.WaitForFunctionAsync(
                        """
                        ({ script, negate }) => {
                          const indirectEval = (0, eval);
                          const value = indirectEval(script);
                          const matched = typeof value === 'function' ? value() : value;
                          return negate ? !matched : !!matched;
                        }
                        """,
                        new { script, negate },
                        new PageWaitForFunctionOptions { Timeout = timeoutMs }).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + ToolTimeoutBufferMs), ct);
                    return "[browser_verify] ok\n" + JsonSerializer.Serialize(new
                    {
                        ok = true,
                        kind = verifyKind,
                        negate,
                        url = _page!.Url
                    }, CookieJsonOptions);
                }
                default:
                    return "[browser_verify] kind must be selector, text, url, load_state, or function.";
            }
        }
        catch (TimeoutException)
        {
            return $"[browser_verify] failed: timed out after {timeoutMs}ms waiting for {verifyKind}.";
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
        {
            return $"[browser_verify] failed: timed out after {timeoutMs}ms waiting for {verifyKind}: {ex.Message}";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> CrawlAsync(string? startUrl, int maxPages, int maxDepth, bool sameOrigin, int maxChars, bool restore, CancellationToken ct = default)
    {
        maxPages = Math.Clamp(maxPages <= 0 ? 5 : maxPages, 1, 20);
        maxDepth = Math.Clamp(maxDepth < 0 ? 1 : maxDepth, 0, 3);
        maxChars = Math.Clamp(maxChars <= 0 ? 2000 : maxChars, 200, 8000);

        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            var stopwatch = Stopwatch.StartNew();
            var originalUrl = _page!.Url;
            var seed = string.IsNullOrWhiteSpace(startUrl) ? originalUrl : startUrl.Trim();
            if (!IsHttpUrl(seed))
                return "[browser_crawl] start_url is required when the current page is not an http(s) page.";

            var seedUri = new Uri(seed, UriKind.Absolute);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<(string Url, int Depth)>();
            var pages = new List<CrawlPageResult>();
            var origin = seedUri.GetLeftPart(UriPartial.Authority);
            var timedOut = false;

            var seedCanonical = CanonicalizeCrawlUrl(seed);
            queue.Enqueue((seedCanonical, 0));
            queued.Add(seedCanonical);

            while (queue.Count > 0 && pages.Count < maxPages)
            {
                ct.ThrowIfCancellationRequested();
                var remainingMs = RemainingMilliseconds(stopwatch, CrawlTotalTimeoutMs);
                if (remainingMs <= 0)
                {
                    timedOut = true;
                    break;
                }

                var (url, depth) = queue.Dequeue();
                if (!visited.Add(url))
                    continue;

                try
                {
                    var pageTimeoutMs = Math.Max(1_000, Math.Min(NavigationTimeoutMs, remainingMs));
                    var response = await _page!.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = pageTimeoutMs
                    }).WaitAsync(TimeSpan.FromMilliseconds(pageTimeoutMs + ToolTimeoutBufferMs), ct);
                    var title = await _page.TitleAsync().WaitAsync(TimeSpan.FromMilliseconds(TitleTimeoutMs), ct);
                    var body = await ReadBodyTextPreviewAsync(maxChars, ct);
                    var links = await ReadPageLinksAsync(ct);
                    var filteredLinks = links
                        .Select(link => new CrawlLink { Text = TrimForMessage(link.Text, 120), Href = CanonicalizeCrawlUrl(link.Href) })
                        .Where(link => IsHttpUrl(link.Href))
                        .Where(link => !sameOrigin || IsSameOrigin(link.Href, origin))
                        .GroupBy(link => link.Href, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.First())
                        .Take(80)
                        .ToList();

                    pages.Add(new CrawlPageResult
                    {
                        Url = SanitizeTraceUrl(_page.Url),
                        Status = response?.Status ?? 0,
                        Depth = depth,
                        Title = title,
                        TextPreview = body,
                        LinkCount = links.Count,
                        QueuedLinkCount = filteredLinks.Count,
                        Links = filteredLinks.Take(20).Select(link => new CrawlLink
                        {
                            Text = link.Text,
                            Href = SanitizeTraceUrl(link.Href)
                        }).ToList()
                    });

                    if (depth >= maxDepth)
                        continue;

                    foreach (var link in filteredLinks)
                    {
                        if (pages.Count + queue.Count >= maxPages)
                            break;
                        if (visited.Contains(link.Href) || !queued.Add(link.Href))
                            continue;
                        queue.Enqueue((link.Href, depth + 1));
                    }
                }
                catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
                {
                    pages.Add(new CrawlPageResult
                    {
                        Url = SanitizeTraceUrl(url),
                        Depth = depth,
                        Error = ex.Message
                    });
                }
            }

            if (restore && IsHttpUrl(originalUrl) && !string.Equals(originalUrl, _page!.Url, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var restoreTimeoutMs = Math.Min(NavigationTimeoutMs, Math.Max(1_000, RemainingMilliseconds(stopwatch, CrawlTotalTimeoutMs)));
                    await _page.GotoAsync(originalUrl, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = restoreTimeoutMs
                    }).WaitAsync(TimeSpan.FromMilliseconds(restoreTimeoutMs + ToolTimeoutBufferMs), ct);
                }
                catch
                {
                }
            }

            var payload = new
            {
                startUrl = SanitizeTraceUrl(seedCanonical),
                sameOrigin,
                maxPages,
                maxDepth,
                timedOut,
                timeoutMs = CrawlTotalTimeoutMs,
                restored = restore && IsHttpUrl(originalUrl),
                pages
            };
            return "[browser_crawl] Completed bounded crawl. It navigates only by discovered links; it does not click forms, bypass login, or read browser storage.\n"
                + JsonSerializer.Serialize(payload, CookieJsonOptions);
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> TraceAsync(string action, bool network, bool console, int maxEvents, int take, CancellationToken ct = default)
    {
        var normalized = (action ?? "read").Trim().ToLowerInvariant();
        maxEvents = Math.Clamp(maxEvents <= 0 ? 200 : maxEvents, 20, 1000);
        take = Math.Clamp(take <= 0 ? 80 : take, 1, 300);

        switch (normalized)
        {
            case "start":
                if (!network && !console)
                    return "[browser_trace] Enable at least one trace stream: network or console.";
                await AcquireOperationLock(ct);
                try
                {
                    await EnsurePageAsync(ct);
                    lock (_traceLock)
                    {
                        _traceEvents.Clear();
                        _traceNetwork = network;
                        _traceConsole = console;
                        _traceMaxEvents = maxEvents;
                        _traceEnabled = true;
                    }
                    AttachTraceHandlers(_page);
                    return "[browser_trace] started\n" + JsonSerializer.Serialize(new
                    {
                        active = true,
                        network,
                        console,
                        maxEvents,
                        url = _page!.Url
                    }, CookieJsonOptions);
                }
                finally
                {
                    ReleaseOperationLock();
                }
            case "read":
                return "[browser_trace] events\n" + JsonSerializer.Serialize(new
                {
                    active = _traceEnabled,
                    events = SnapshotTraceEvents(take)
                }, CookieJsonOptions);
            case "stop":
                lock (_traceLock)
                    _traceEnabled = false;
                return "[browser_trace] stopped\n" + JsonSerializer.Serialize(new
                {
                    active = false,
                    events = SnapshotTraceEvents(take)
                }, CookieJsonOptions);
            default:
                return "[browser_trace] action must be start, read, or stop.";
        }
    }

    public async Task<string> SaveCookiesAsync(string storePath, string? site = null, CancellationToken ct = default)
    {
        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            var targetSite = NormalizeSiteFilter(site);
            var captured = (await _context!.CookiesAsync().WaitAsync(TimeSpan.FromMilliseconds(CookieOperationTimeoutMs), ct))
                .Select(ToSavedCookie)
                .Where(cookie => targetSite == null || cookie.Site.Equals(targetSite, StringComparison.OrdinalIgnoreCase))
                .ToList();

            CookieStore store;
            if (targetSite != null && File.Exists(storePath))
            {
                store = ReadCookieStore(storePath);
                store.Cookies = store.Cookies
                    .Where(cookie => !cookie.Site.Equals(targetSite, StringComparison.OrdinalIgnoreCase))
                    .Concat(captured)
                    .ToList();
            }
            else
            {
                store = new CookieStore { Cookies = captured };
            }

            store.Version = 1;
            store.SavedAt = UserTimeNow();
            store.Sites = BuildCookieSiteSummaries(store.Cookies);
            WriteAllTextAtomic(storePath, JsonSerializer.Serialize(store, CookieJsonOptions));

            var scope = targetSite ?? "all sites";
            return $"[save_cookie] Saved {captured.Count} cookie(s) for {scope}. Store now has {store.Cookies.Count} cookie(s) across {store.Sites.Count} site(s):\n{JsonSerializer.Serialize(store.Sites, CookieJsonOptions)}";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> ListCookiesBySiteAsync(string storePath, string? site = null, CancellationToken ct = default)
    {
        await AcquireOperationLock(ct);
        try
        {
            var source = "saved cookie store";
            CookieStore store;
            if (File.Exists(storePath))
            {
                store = ReadCookieStore(storePath);
            }
            else
            {
                await EnsurePageAsync(ct);
                source = "current browser context (not saved yet)";
                store = new CookieStore
                {
                    SavedAt = UserTimeNow(),
                    Cookies = (await _context!.CookiesAsync().WaitAsync(TimeSpan.FromMilliseconds(CookieOperationTimeoutMs), ct)).Select(ToSavedCookie).ToList()
                };
                store.Sites = BuildCookieSiteSummaries(store.Cookies);
            }

            var targetSite = NormalizeSiteFilter(site);
            var sites = store.Sites;
            if (targetSite != null)
            {
                sites = sites
                    .Where(item => item.Site.Equals(targetSite, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return $"[list_cookie_by_site] {source}: {store.Cookies.Count} cookie(s), {sites.Count} matching site(s). Cookie values are intentionally omitted.\n{JsonSerializer.Serialize(sites, CookieJsonOptions)}";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task<string> ApplyCookiesAsync(string storePath, string? site = null, CancellationToken ct = default)
    {
        await AcquireOperationLock(ct);
        try
        {
            await EnsurePageAsync(ct);
            if (!File.Exists(storePath))
                return $"[apply_cookie] No saved cookie store found at: {storePath}";

            var store = ReadCookieStore(storePath);
            var targetSite = NormalizeSiteFilter(site);
            var savedCookies = store.Cookies
                .Where(cookie => targetSite == null || cookie.Site.Equals(targetSite, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var candidates = new List<CookieApplyCandidate>();
            var skipped = new List<CookieApplyIssue>();
            foreach (var savedCookie in savedCookies)
            {
                if (!TryCreateApplyCandidate(savedCookie, out var candidate, out var reason))
                {
                    skipped.Add(CookieApplyIssue.From(savedCookie, reason));
                    continue;
                }

                candidates.Add(candidate);
            }

            if (candidates.Count == 0)
                return "[apply_cookie] No applicable saved cookies matched the request.";

            var applied = 0;
            var failed = new List<CookieApplyIssue>();
            var batchRejected = false;
            try
            {
                await _context!.AddCookiesAsync(candidates.Select(candidate => candidate.Cookie)).WaitAsync(TimeSpan.FromMilliseconds(CookieOperationTimeoutMs), ct);
                applied = candidates.Count;
            }
            catch
            {
                batchRejected = true;
                foreach (var candidate in candidates)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        await _context!.AddCookiesAsync(new[] { candidate.Cookie }).WaitAsync(TimeSpan.FromMilliseconds(Math.Min(5_000, CookieOperationTimeoutMs)), ct);
                        applied++;
                    }
                    catch
                    {
                        failed.Add(CookieApplyIssue.From(candidate, "browser rejected this cookie"));
                    }
                }
            }

            var scope = targetSite ?? "all sites";
            var sites = BuildCookieSiteSummaries(savedCookies);
            var contextCookies = (await _context!.CookiesAsync().WaitAsync(TimeSpan.FromMilliseconds(CookieOperationTimeoutMs), ct))
                .Select(ToSavedCookie)
                .Where(cookie => targetSite == null || cookie.Site.Equals(targetSite, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var currentUrl = _page?.Url ?? string.Empty;
            var currentSite = SiteFromUrl(currentUrl);
            var currentPageMatchesScope = targetSite == null
                ? sites.Any(site => site.Site.Equals(currentSite, StringComparison.OrdinalIgnoreCase))
                : string.Equals(currentSite, targetSite, StringComparison.OrdinalIgnoreCase);
            var diagnostics = new
            {
                batchFallbackUsed = batchRejected,
                applied,
                attempted = candidates.Count,
                skipped = skipped.Count,
                failed = failed.Count,
                contextMatchingCookies = contextCookies.Count,
                contextMatchingSites = BuildCookieSiteSummaries(contextCookies),
                currentUrl,
                currentSite,
                currentPageMatchesScope,
                note = currentPageMatchesScope
                    ? "Cookies are now in the browser context. The already-loaded page may still need a normal navigation/reload by the site to observe the new login state."
                    : "Cookies are now in the browser context, but the current page is outside the applied site scope. Navigate to the target site to use them.",
                skippedExamples = skipped.Take(10),
                failedExamples = failed.Take(10)
            };
            return $"[apply_cookie] Applied {applied}/{candidates.Count} cookie(s) for {scope} across {sites.Count} site(s). Cookie values are intentionally omitted.\n{JsonSerializer.Serialize(diagnostics, CookieJsonOptions)}";
        }
        finally
        {
            ReleaseOperationLock();
        }
    }

    public async Task StartScreencastAsync(int quality = 80, int maxWidth = 1280, int maxHeight = 720, CancellationToken ct = default)
    {
        await EnsurePageAsync(ct).WaitAsync(TimeSpan.FromMilliseconds(BrowserStartupTimeoutMs), ct);
        if (_screencastRunning) return;
        
        _screencastRunning = true;
        
        // Start a background loop to capture frames for real-time streaming
        _ = Task.Run(async () =>
        {
            while (_screencastRunning)
            {
                try
                {
                    if (IsBusy || _page == null)
                    {
                    await Task.Delay(250);
                        continue;
                    }

                    var bytes = await _page!.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Type = ScreenshotType.Jpeg,
                        Quality = quality
                    }).WaitAsync(TimeSpan.FromSeconds(2));
                    OnScreencastFrame?.Invoke(bytes);
                    await Task.Delay(100); // ~10 fps
                }
                catch
                {
                    await Task.Delay(200);
                }
            }
        });
    }

    public Task StopScreencastAsync()
    {
        _screencastRunning = false;
        return Task.CompletedTask;
    }

    public async Task<string> CloseAsync(bool force = false, CancellationToken ct = default)
    {
        if (!force)
        {
            return "[browser] Close request ignored. Matdance keeps the shared browser/context warm to preserve page state and cookies; it will be released automatically when the Web UI shuts down.";
        }

        if (!await _gate.WaitAsync(TimeSpan.FromMilliseconds(BrowserContextTimeoutMs), ct))
            return $"[browser] Close timed out after {BrowserContextTimeoutMs}ms waiting for browser state lock.";

        try
        {
            StopKeepAlive();
            await StopScreencastAsync();
            if (_browser != null)
            {
                try { await _browser.CloseAsync().WaitAsync(TimeSpan.FromMilliseconds(BrowserContextTimeoutMs), ct); } catch { }
                _browser = null;
                _context = null;
                _page = null;
            }
            if (_playwright != null)
            {
                _playwright.Dispose();
                _playwright = null;
            }
            IsBusy = false;
            return "[browser] Browser closed.";
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task AcquireOperationLock(CancellationToken ct = default)
    {
        if (!await _operationLock.WaitAsync(TimeSpan.FromMilliseconds(OperationLockTimeoutMs), ct))
        {
            throw new TimeoutException($"Another browser operation is still running after {OperationLockTimeoutMs}ms. The current call was not executed to avoid stacking browser operations.");
        }

        IsBusy = true;
    }

    private void ReleaseOperationLock()
    {
        IsBusy = false;
        _operationLock.Release();
    }

    private async Task EnsurePageAsync(CancellationToken ct = default)
    {
        if (_page == null || _browser == null || !_browser.IsConnected)
        {
            await EnsureBrowserAsync(_isHeadless, ct);
            if (_page != null && !_page.IsClosed)
                return;
        }

        if (_browser == null || !_browser.IsConnected)
            return;

        if (_context == null)
        {
            _context = await _browser.NewContextAsync(CreateContextOptions()).WaitAsync(TimeSpan.FromMilliseconds(BrowserContextTimeoutMs), ct);
            _page = await _context.NewPageAsync().WaitAsync(TimeSpan.FromMilliseconds(BrowserContextTimeoutMs), ct);
            AttachTraceHandlers(_page);
            return;
        }

        if (_page == null || _page.IsClosed)
        {
            try
            {
                _page = await _context.NewPageAsync().WaitAsync(TimeSpan.FromMilliseconds(BrowserContextTimeoutMs), ct);
                AttachTraceHandlers(_page);
            }
            catch
            {
                _context = await _browser.NewContextAsync(CreateContextOptions()).WaitAsync(TimeSpan.FromMilliseconds(BrowserContextTimeoutMs), ct);
                _page = await _context.NewPageAsync().WaitAsync(TimeSpan.FromMilliseconds(BrowserContextTimeoutMs), ct);
                AttachTraceHandlers(_page);
            }
        }
    }

    private static int ClampMilliseconds(int requested, int defaultValue, int minValue, int maxValue)
    {
        return Math.Clamp(requested <= 0 ? defaultValue : requested, minValue, maxValue);
    }

    private static int ClampSecondsToMilliseconds(int requestedSeconds, int maxMilliseconds)
    {
        if (requestedSeconds <= 0)
            return 0;

        var maxSeconds = Math.Max(1, maxMilliseconds / 1000);
        var seconds = Math.Min(requestedSeconds, maxSeconds);
        return seconds * 1000;
    }

    private static int RemainingMilliseconds(Stopwatch stopwatch, int totalTimeoutMs)
    {
        return Math.Max(0, totalTimeoutMs - (int)Math.Min(int.MaxValue, stopwatch.ElapsedMilliseconds));
    }

    private static BrowserNewContextOptions CreateContextOptions()
    {
        return new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        };
    }

    private async Task<string> RecoverPageAfterHungOperationAsync(string reason, CancellationToken ct = default)
    {
        try
        {
            var oldPage = _page;
            _page = null;
            if (oldPage != null)
            {
                try
                {
                    await oldPage.CloseAsync().WaitAsync(TimeSpan.FromMilliseconds(HungPageRecoveryTimeoutMs), ct);
                }
                catch
                {
                    // A hung page is allowed to die in the background; keep the context.
                }
            }

            if (_context != null)
            {
                _page = await _context.NewPageAsync().WaitAsync(TimeSpan.FromMilliseconds(HungPageRecoveryTimeoutMs), ct);
                AttachTraceHandlers(_page);
                return $"Recovered by replacing only the active page after {reason}; browser context and cookies were preserved.";
            }
        }
        catch (Exception ex)
        {
            return $"Page recovery after {reason} failed: {ex.Message}";
        }

        return $"No browser context was available for page recovery after {reason}.";
    }

    private static WaitForSelectorState ParseSelectorState(string? state)
    {
        return (state ?? "visible").Trim().ToLowerInvariant() switch
        {
            "attached" => WaitForSelectorState.Attached,
            "detached" => WaitForSelectorState.Detached,
            "hidden" => WaitForSelectorState.Hidden,
            _ => WaitForSelectorState.Visible
        };
    }

    private static LoadState ParseLoadState(string? state)
    {
        return (state ?? "networkidle").Trim().ToLowerInvariant() switch
        {
            "domcontentloaded" => LoadState.DOMContentLoaded,
            "load" => LoadState.Load,
            _ => LoadState.NetworkIdle
        };
    }

    private static string? GetUnsafeDynamicScriptReason(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
            return "Empty browser script.";

        var normalized = Regex.Replace(script.ToLowerInvariant(), @"\s+", " ");
        var blocked = new[]
        {
            "document.cookie", "cookie", "localstorage", "sessionstorage", "indexeddb",
            "password", "passwd", "credential", "credentials", "token", "authorization", "bearer",
            "captcha", "recaptcha", "hcaptcha", "turnstile", "paywall",
            "webdriver", "automationcontrolled", "navigator.webdriver", "useragentdata",
            "setrequestheader",
            "serviceworker", "permissions.query", "chrome.runtime"
        };

        if (blocked.Any(marker => normalized.Contains(marker, StringComparison.Ordinal)))
        {
            return "Browser dynamic scripts may not touch cookies/storage/credentials/tokens, CAPTCHA/paywall markers, anti-bot fingerprints, privileged request headers, service workers, or access-control bypass patterns.";
        }

        if (Regex.IsMatch(normalized, @"object\.defineproperty\s*\(\s*navigator", RegexOptions.IgnoreCase))
        {
            return "Browser dynamic scripts may not modify navigator fingerprint properties.";
        }

        return null;
    }

    private static string FormatEvaluateResult(object? result)
    {
        if (result == null)
            return "null";
        if (result is string text)
            return text;

        try
        {
            return JsonSerializer.Serialize(result, CookieJsonOptions);
        }
        catch
        {
            return result.ToString() ?? "null";
        }
    }

    private static string TrimForMessage(string value, int max)
    {
        value = value ?? string.Empty;
        return value.Length <= max ? value : value[..max] + "...";
    }

    private void StartKeepAlive()
    {
        if (_keepAliveTimer != null) return;
        _keepAliveTimer = new System.Threading.Timer(async _ =>
        {
            try
            {
                if (_browser == null || !_browser.IsConnected)
                {
                    Console.WriteLine("[browser] Keepalive detected browser closed; attempting restart...");
                    await EnsureBrowserAsync(_isHeadless);
                }
                else if (!IsBusy && (_page == null || _page.IsClosed))
                {
                    Console.WriteLine("[browser] Keepalive detected closed page; preparing a replacement page...");
                    await EnsurePageAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[browser] Keepalive restart failed: {ex.Message}");
            }
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private void StopKeepAlive()
    {
        _keepAliveTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _keepAliveTimer?.Dispose();
        _keepAliveTimer = null;
    }

    private async Task<string> ReadBodyTextPreviewAsync(int maxChars, CancellationToken ct = default)
    {
        try
        {
            var text = await _page!.InnerTextAsync("body", new PageInnerTextOptions { Timeout = PageReadTimeoutMs })
                .WaitAsync(TimeSpan.FromMilliseconds(PageReadTimeoutMs + 1_000), ct);
            text = Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();
            return TrimForMessage(text, maxChars);
        }
        catch
        {
            return string.Empty;
        }
    }

    private async Task<List<CrawlLink>> ReadPageLinksAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await _page!.EvaluateAsync<string>(
                """
                () => {
                  const clean = value => String(value || '').replace(/\s+/g, ' ').trim();
                  const links = Array.from(document.querySelectorAll('a[href]')).map(el => ({
                    text: clean(el.innerText || el.textContent || el.getAttribute('aria-label') || el.getAttribute('title') || ''),
                    href: el.href || ''
                  })).filter(item => item.href);
                  return JSON.stringify(links.slice(0, 300));
                }
                """).WaitAsync(TimeSpan.FromMilliseconds(PageReadTimeoutMs), ct);
            return JsonSerializer.Deserialize<List<CrawlLink>>(json, CookieJsonOptions) ?? new List<CrawlLink>();
        }
        catch
        {
            return new List<CrawlLink>();
        }
    }

    private static bool IsHttpUrl(string? url)
    {
        return !string.IsNullOrWhiteSpace(url)
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
    }

    private static string CanonicalizeCrawlUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        var builder = new UriBuilder(uri)
        {
            Fragment = string.Empty
        };
        return builder.Uri.AbsoluteUri;
    }

    private static bool IsSameOrigin(string url, string origin)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
            return false;

        return uri.Scheme.Equals(originUri.Scheme, StringComparison.OrdinalIgnoreCase)
            && uri.Host.Equals(originUri.Host, StringComparison.OrdinalIgnoreCase)
            && uri.Port == originUri.Port;
    }

    private void AttachTraceHandlers(IPage? page)
    {
        if (page == null)
            return;

        lock (_traceLock)
        {
            if (!_traceAttachedPages.Add(page))
                return;
        }

        page.Request += (_, request) =>
        {
            if (!ShouldRecordTrace(network: true))
                return;

            AddTraceEvent(new BrowserTraceEvent
            {
                At = UserTimeNow(),
                Kind = "request",
                Method = request.Method,
                Url = SanitizeTraceUrl(request.Url),
                ResourceType = request.ResourceType
            });
        };
        page.Response += (_, response) =>
        {
            if (!ShouldRecordTrace(network: true))
                return;

            AddTraceEvent(new BrowserTraceEvent
            {
                At = UserTimeNow(),
                Kind = "response",
                Method = response.Request.Method,
                Url = SanitizeTraceUrl(response.Url),
                ResourceType = response.Request.ResourceType,
                Status = response.Status
            });
        };
        page.RequestFailed += (_, request) =>
        {
            if (!ShouldRecordTrace(network: true))
                return;

            AddTraceEvent(new BrowserTraceEvent
            {
                At = UserTimeNow(),
                Kind = "request_failed",
                Method = request.Method,
                Url = SanitizeTraceUrl(request.Url),
                ResourceType = request.ResourceType,
                Error = request.Failure ?? string.Empty
            });
        };
        page.Console += (_, message) =>
        {
            if (!ShouldRecordTrace(console: true))
                return;

            AddTraceEvent(new BrowserTraceEvent
            {
                At = UserTimeNow(),
                Kind = "console",
                Level = message.Type,
                Text = TrimForMessage(message.Text, 500),
                Url = _page == null ? string.Empty : SanitizeTraceUrl(_page.Url)
            });
        };
    }

    private bool ShouldRecordTrace(bool network = false, bool console = false)
    {
        lock (_traceLock)
        {
            return _traceEnabled
                && (!network || _traceNetwork)
                && (!console || _traceConsole);
        }
    }

    private void AddTraceEvent(BrowserTraceEvent item)
    {
        lock (_traceLock)
        {
            if (!_traceEnabled)
                return;

            _traceEvents.Add(item);
            if (_traceEvents.Count > _traceMaxEvents)
                _traceEvents.RemoveRange(0, _traceEvents.Count - _traceMaxEvents);
        }
    }

    private List<BrowserTraceEvent> SnapshotTraceEvents(int take)
    {
        lock (_traceLock)
        {
            var skip = Math.Max(0, _traceEvents.Count - take);
            return _traceEvents.Skip(skip).ToList();
        }
    }

    private static string SanitizeTraceUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return TrimForMessage(url ?? string.Empty, 500);

        var builder = new UriBuilder(uri)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Fragment = string.Empty
        };

        var query = uri.Query.TrimStart('?');
        if (!string.IsNullOrWhiteSpace(query))
        {
            var parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(part =>
                {
                    var index = part.IndexOf('=', StringComparison.Ordinal);
                    var key = index >= 0 ? part[..index] : part;
                    var value = index >= 0 ? part[(index + 1)..] : string.Empty;
                    return IsSensitiveQueryKey(key)
                        ? key + "=[redacted]"
                        : string.IsNullOrEmpty(value)
                            ? key
                            : key + "=" + TrimForMessage(value, 120);
                });
            builder.Query = string.Join("&", parts);
        }
        else
        {
            builder.Query = string.Empty;
        }

        return TrimForMessage(builder.Uri.AbsoluteUri, 500);
    }

    private static bool IsSensitiveQueryKey(string key)
    {
        var normalized = (key ?? string.Empty).ToLowerInvariant();
        return normalized.Contains("token", StringComparison.Ordinal)
            || normalized.Contains("auth", StringComparison.Ordinal)
            || normalized.Contains("secret", StringComparison.Ordinal)
            || normalized.Contains("password", StringComparison.Ordinal)
            || normalized.Equals("pass", StringComparison.Ordinal)
            || normalized.Contains("session", StringComparison.Ordinal)
            || normalized.Contains("credential", StringComparison.Ordinal)
            || normalized.Equals("code", StringComparison.Ordinal)
            || normalized.Equals("sig", StringComparison.Ordinal)
            || normalized.Contains("signature", StringComparison.Ordinal)
            || normalized.Contains("jwt", StringComparison.Ordinal);
    }

    private static SavedCookie ToSavedCookie(BrowserContextCookiesResult cookie)
    {
        return new SavedCookie
        {
            Name = cookie.Name,
            Value = cookie.Value,
            Domain = cookie.Domain,
            Path = cookie.Path,
            Expires = cookie.Expires,
            HttpOnly = cookie.HttpOnly,
            Secure = cookie.Secure,
            SameSite = cookie.SameSite.ToString(),
            PartitionKey = cookie.PartitionKey,
            Site = SiteFromHost(cookie.Domain)
        };
    }

    private static bool TryCreateApplyCandidate(SavedCookie cookie, out CookieApplyCandidate candidate, out string reason)
    {
        candidate = new CookieApplyCandidate();
        reason = string.Empty;

        if (string.IsNullOrWhiteSpace(cookie.Name))
        {
            reason = "missing cookie name";
            return false;
        }

        if (cookie.Expires > 0 && cookie.Expires <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            reason = "expired";
            return false;
        }

        var playwrightCookie = ToPlaywrightCookie(cookie);
        if (string.IsNullOrWhiteSpace(playwrightCookie.Name)
            || (string.IsNullOrWhiteSpace(playwrightCookie.Url)
                && (string.IsNullOrWhiteSpace(playwrightCookie.Domain) || string.IsNullOrWhiteSpace(playwrightCookie.Path))))
        {
            reason = "missing browser cookie scope";
            return false;
        }

        candidate = new CookieApplyCandidate
        {
            Cookie = playwrightCookie,
            Site = cookie.Site,
            Domain = cookie.Domain,
            Name = cookie.Name
        };
        return true;
    }

    private static Microsoft.Playwright.Cookie ToPlaywrightCookie(SavedCookie cookie)
    {
        var result = new Microsoft.Playwright.Cookie
        {
            Name = cookie.Name,
            Value = cookie.Value,
            Domain = cookie.Domain,
            Path = string.IsNullOrWhiteSpace(cookie.Path) ? "/" : cookie.Path,
            HttpOnly = cookie.HttpOnly,
            Secure = cookie.Secure
        };

        if (cookie.Expires > 0)
            result.Expires = cookie.Expires;
        if (Enum.TryParse<SameSiteAttribute>(cookie.SameSite, ignoreCase: true, out var sameSite))
            result.SameSite = sameSite;
        if (!string.IsNullOrWhiteSpace(cookie.PartitionKey))
            result.PartitionKey = cookie.PartitionKey;

        return result;
    }

    private sealed class CookieApplyCandidate
    {
        public Microsoft.Playwright.Cookie Cookie { get; set; } = new();
        public string Site { get; set; } = "(hostless)";
        public string Domain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private sealed class CookieApplyIssue
    {
        public string Site { get; set; } = "(hostless)";
        public string Domain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;

        public static CookieApplyIssue From(SavedCookie cookie, string reason) => new()
        {
            Site = cookie.Site,
            Domain = cookie.Domain,
            Name = cookie.Name,
            Reason = reason
        };

        public static CookieApplyIssue From(CookieApplyCandidate candidate, string reason) => new()
        {
            Site = candidate.Site,
            Domain = candidate.Domain,
            Name = candidate.Name,
            Reason = reason
        };
    }

    private static string? NormalizeSiteFilter(string? site)
    {
        if (string.IsNullOrWhiteSpace(site))
            return null;

        var value = site.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
            value = uri.Host;

        return SiteFromHost(value);
    }

    private static string SiteFromUrl(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url)
            && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && !string.IsNullOrWhiteSpace(uri.Host))
        {
            return SiteFromHost(uri.Host);
        }

        return "(unknown)";
    }

    private static string SiteFromHost(string? host)
    {
        var value = (host ?? string.Empty).Trim().TrimStart('.').TrimEnd('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(value))
            return "(hostless)";

        if (System.Net.IPAddress.TryParse(value, out _))
            return value;

        var labels = value.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (labels.Length <= 2)
            return value;

        var lastTwo = string.Join(".", labels[^2], labels[^1]);
        if (MultiLabelPublicSuffixHints.Contains(lastTwo) && labels.Length >= 3)
            return string.Join(".", labels[^3], labels[^2], labels[^1]);

        return lastTwo;
    }

    private static List<CookieSiteSummary> BuildCookieSiteSummaries(IEnumerable<SavedCookie> cookies)
    {
        return cookies
            .GroupBy(cookie => cookie.Site, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new CookieSiteSummary
            {
                Site = group.Key,
                Count = group.Count(),
                Domains = group.Select(cookie => cookie.Domain).Where(domain => !string.IsNullOrWhiteSpace(domain)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(domain => domain).ToList(),
                Names = group.Select(cookie => cookie.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name).ToList()
            })
            .ToList();
    }

    private static CookieStore ReadCookieStore(string storePath)
    {
        try
        {
            var store = JsonSerializer.Deserialize<CookieStore>(File.ReadAllText(storePath), CookieJsonOptions) ?? new CookieStore();
            if (store.SavedAt != default && store.SavedAt != DateTimeOffset.MinValue)
                store.SavedAt = ToUserTime(store.SavedAt);
            return store;
        }
        catch
        {
            return new CookieStore();
        }
    }

    private static void WriteAllTextAtomic(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory ?? string.Empty, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(tempPath, content, Encoding.UTF8);
            if (File.Exists(path))
            {
                try
                {
                    File.Replace(tempPath, path, null);
                    return;
                }
                catch
                {
                    File.Delete(path);
                }
            }

            File.Move(tempPath, path);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
            }
        }
    }

    private static DateTimeOffset UserTimeNow()
    {
        return ToUserTime(DateTimeOffset.UtcNow);
    }

    private static DateTimeOffset ToUserTime(DateTimeOffset instant)
    {
        return TimeZoneInfo.ConvertTime(instant, FindUserTimeZone());
    }

    private static TimeZoneInfo FindUserTimeZone()
    {
        var env = Environment.GetEnvironmentVariable("MATDANCE_TIME_ZONE");
        if (TryFindZone(env, out var zone))
            return zone;

        var runtimeRoot = Environment.GetEnvironmentVariable("MATDANCE_RUNTIME_DIR");
        if (!string.IsNullOrWhiteSpace(runtimeRoot))
        {
            var statePath = Path.Combine(runtimeRoot, "state", "user-time-zone.json");
            try
            {
                if (File.Exists(statePath))
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(statePath));
                    if (document.RootElement.TryGetProperty("timeZone", out var timeZone)
                        && TryFindZone(timeZone.GetString(), out zone))
                        return zone;
                }
            }
            catch
            {
            }
        }

        return TimeZoneInfo.Local;
    }

    private static bool TryFindZone(string? timeZone, out TimeZoneInfo zone)
    {
        zone = TimeZoneInfo.Local;
        if (string.IsNullOrWhiteSpace(timeZone))
            return false;

        var id = timeZone.Trim();
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch
        {
        }

        if (string.Equals(id, "Asia/Shanghai", StringComparison.OrdinalIgnoreCase))
            return TryFindSystemZone("China Standard Time", out zone);
        if (string.Equals(id, "China Standard Time", StringComparison.OrdinalIgnoreCase))
            return TryFindSystemZone("Asia/Shanghai", out zone);
        if (string.Equals(id, "Etc/UTC", StringComparison.OrdinalIgnoreCase))
            return TryFindSystemZone("UTC", out zone);

        try
        {
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId)
                && TryFindSystemZone(windowsId, out zone))
                return true;
        }
        catch
        {
        }

        try
        {
            if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var ianaId)
                && TryFindSystemZone(ianaId, out zone))
                return true;
        }
        catch
        {
        }

        return false;
    }

    private static bool TryFindSystemZone(string id, out TimeZoneInfo zone)
    {
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch
        {
            zone = TimeZoneInfo.Local;
            return false;
        }
    }

    private sealed class CrawlLink
    {
        public string Text { get; set; } = string.Empty;
        public string Href { get; set; } = string.Empty;
    }

    private sealed class CrawlPageResult
    {
        public string Url { get; set; } = string.Empty;
        public int Status { get; set; }
        public int Depth { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TextPreview { get; set; } = string.Empty;
        public int LinkCount { get; set; }
        public int QueuedLinkCount { get; set; }
        public string Error { get; set; } = string.Empty;
        public List<CrawlLink> Links { get; set; } = new();
    }

    private sealed class BrowserTraceEvent
    {
        public DateTimeOffset At { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    private sealed class CookieStore
    {
        public int Version { get; set; } = 1;
        public DateTimeOffset SavedAt { get; set; } = UserTimeNow();
        public List<SavedCookie> Cookies { get; set; } = new();
        public List<CookieSiteSummary> Sites { get; set; } = new();
    }

    private sealed class SavedCookie
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Path { get; set; } = "/";
        public float Expires { get; set; }
        public bool HttpOnly { get; set; }
        public bool Secure { get; set; }
        public string SameSite { get; set; } = string.Empty;
        public string? PartitionKey { get; set; }
        public string Site { get; set; } = "(hostless)";
    }

    private sealed class CookieSiteSummary
    {
        public string Site { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<string> Domains { get; set; } = new();
        public List<string> Names { get; set; } = new();
    }

    public async ValueTask DisposeAsync()
    {
        StopKeepAlive();
        await CloseAsync(force: true);
        _gate.Dispose();
        _operationLock.Dispose();
    }
}
