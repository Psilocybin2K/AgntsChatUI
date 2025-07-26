namespace AgntsChatUI.Services
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Playwright;

    /// <summary>
    /// Service for managing Playwright browser lifecycle and interactions.
    /// </summary>
    public class PlaywrightManagementService : IAsyncDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IPage? _page;
        private bool _isInitialized = false;
        private readonly object _lock = new();

        private void SubscribeToBrowserEvents()
        {
            if (this._browser is not null)
            {
                this._browser.Disconnected += (_, _) => this.OnBrowserClosedOrCrashed();
            }
        }

        private void SubscribeToPageEvents(IPage page)
        {
            page.Close += (_, _) => this.OnPageClosedOrCrashed();
            page.Crash += (_, _) => this.OnPageClosedOrCrashed();
            PageClosedOrCrashed += (sender, args) =>
            {

                this._page = null;
            };
        }

        private void OnBrowserClosedOrCrashed()
        {
            lock (this._lock)
            {
                this._isInitialized = false;
                this._browser = null;
                this._playwright = null;
            }

            BrowserClosedOrCrashed?.Invoke(this, EventArgs.Empty);
        }

        private void OnPageClosedOrCrashed()
        {
            // Optionally handle per-page state here
            PageClosedOrCrashed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? BrowserClosedOrCrashed;
        public event EventHandler? PageClosedOrCrashed;

        /// <summary>
        /// Initializes Playwright and launches a browser if not already started.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (this._isInitialized && this._browser is not null && this._playwright is not null && this._page is not null)
            {
                return;
            }

            lock (this._lock)
            {
                if (this._isInitialized && this._browser is not null && this._playwright is not null && this._page is not null)
                {
                    return;
                }

                this._isInitialized = true;
            }

            this._playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            this._browser = await this._playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
            this.SubscribeToBrowserEvents();
            IPage page = await this._browser.NewPageAsync();
            this.SubscribeToPageEvents(page);
            this._page = page;
        }

        /// <summary>
        /// Gets an open browser instance, initializing if necessary.
        /// </summary>
        public async Task<IBrowser> GetBrowserAsync()
        {
            if (!this._isInitialized || this._browser is null)
            {
                await this.InitializeAsync();
            }

            return this._browser!;
        }

        /// <summary>
        /// Gets a new page from the browser.
        /// </summary>
        public async Task<IPage> GetNewPageAsync()
        {
            IBrowser browser = await this.GetBrowserAsync();
            IPage page = await browser.NewPageAsync();
            this.SubscribeToPageEvents(page);
            return page;
        }

        /// <summary>
        /// Closes the browser and disposes Playwright.
        /// </summary>
        public async Task CloseAsync()
        {
            if (this._browser != null)
            {
                await this._browser.CloseAsync();
                this._browser = null;
            }

            if (this._playwright != null)
            {
                this._playwright.Dispose();
                this._playwright = null;
            }

            this._isInitialized = false;
        }

        /// <summary>
        /// Returns true if the browser is open.
        /// </summary>
        public bool IsBrowserOpen => this._browser is not null;

        public async ValueTask DisposeAsync()
        {
            await this.CloseAsync();
        }
    }
}