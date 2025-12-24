using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizManager.Data;
using QuizManager.Models;

namespace QuizManager.Services.FrontPage
{
    public class FrontPageService : IFrontPageService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FrontPageService> _logger;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private FrontPageDataState _state = FrontPageDataState.Empty;

        public FrontPageService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IHttpClientFactory httpClientFactory,
            ILogger<FrontPageService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public FrontPageDataState CurrentState => _state;

        public event Action<FrontPageDataState>? StateChanged;

        public async Task EnsureDataLoadedAsync(CancellationToken cancellationToken = default)
        {
            if (_state.IsLoaded)
            {
                return;
            }

            await RefreshAsync(cancellationToken);
        }

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            await _refreshLock.WaitAsync(cancellationToken);
            try
            {
                // Start external HTTP calls concurrently (they don't use DbContext)
                var uoaNewsTask = FetchUoaNewsAsync(cancellationToken);
                var svseNewsTask = FetchSvseNewsAsync(cancellationToken);
                var weatherTask = Task.FromResult<WeatherSnapshot?>(null);

                // Execute database queries sequentially to avoid DbContext concurrency issues
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                var companyEvents = await context.CompanyEvents
                    .AsNoTracking()
                    .Where(e => e.CompanyEventStatus == "Δημοσιευμένη" && e.CompanyEventActiveDate <= DateTime.Now)
                    .OrderByDescending(e => e.CompanyEventUploadedDate)
                    .ToListAsync(cancellationToken);

                var professorEvents = await context.ProfessorEvents
                    .AsNoTracking()
                    .Where(e => e.ProfessorEventStatus == "Δημοσιευμένη" && e.ProfessorEventActiveDate <= DateTime.Now)
                    .OrderByDescending(e => e.ProfessorEventUploadedDate)
                    .ToListAsync(cancellationToken);

                var companyAnnouncements = await context.AnnouncementsAsCompany
                    .AsNoTracking()
                    .Where(a => a.CompanyAnnouncementStatus == "Δημοσιευμένη" && a.CompanyAnnouncementTimeToBeActive <= DateTime.Now)
                    .OrderByDescending(a => a.CompanyAnnouncementUploadDate)
                    .ToListAsync(cancellationToken);

                var professorAnnouncements = await context.AnnouncementsAsProfessor
                    .AsNoTracking()
                    .Where(a => a.ProfessorAnnouncementStatus == "Δημοσιευμένη" && a.ProfessorAnnouncementTimeToBeActive <= DateTime.Now)
                    .OrderByDescending(a => a.ProfessorAnnouncementUploadDate)
                    .ToListAsync(cancellationToken);

                var researchGroupAnnouncements = await context.AnnouncementAsResearchGroup
                    .AsNoTracking()
                    .Where(a => a.ResearchGroupAnnouncementStatus == "Δημοσιευμένη" && a.ResearchGroupAnnouncementTimeToBeActive <= DateTime.Now)
                    .OrderByDescending(a => a.ResearchGroupAnnouncementUploadDate)
                    .ToListAsync(cancellationToken);

                // Wait for external HTTP calls to complete
                await Task.WhenAll(uoaNewsTask, svseNewsTask, weatherTask);

                _state = new FrontPageDataState(
                    await uoaNewsTask,
                    await svseNewsTask,
                    companyAnnouncements.AsReadOnly(),
                    professorAnnouncements.AsReadOnly(),
                    researchGroupAnnouncements.AsReadOnly(),
                    companyEvents.AsReadOnly(),
                    professorEvents.AsReadOnly(),
                    await weatherTask,
                    DateTimeOffset.UtcNow);

                StateChanged?.Invoke(_state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh front page data");
                throw;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public async Task<IReadOnlyList<CompanyEvent>> GetPublishedCompanyEventsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                return await context.CompanyEvents
                    .Where(e => e.CompanyEventStatus == "Δημοσιευμένη")
                    .AsNoTracking()
                    .OrderByDescending(e => e.CompanyEventActiveDate)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading published company events");
                return Array.Empty<CompanyEvent>();
            }
        }

        public async Task<IReadOnlyList<ProfessorEvent>> GetPublishedProfessorEventsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                return await context.ProfessorEvents
                    .Where(e => e.ProfessorEventStatus == "Δημοσιευμένη")
                    .AsNoTracking()
                    .OrderByDescending(e => e.ProfessorEventActiveDate)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading published professor events");
                return Array.Empty<ProfessorEvent>();
            }
        }

        // Backward compatibility method
        public async Task<FrontPageData> LoadFrontPageDataAsync(CancellationToken cancellationToken = default)
        {
            await EnsureDataLoadedAsync(cancellationToken);
            var state = CurrentState;
            
            return new FrontPageData
            {
                CompanyEvents = state.CompanyEvents.ToList(),
                ProfessorEvents = state.ProfessorEvents.ToList(),
                CompanyAnnouncements = state.CompanyAnnouncements.ToList(),
                ProfessorAnnouncements = state.ProfessorAnnouncements.ToList(),
                ResearchGroupAnnouncements = state.ResearchGroupAnnouncements.ToList()
            };
        }

        private async Task<IReadOnlyList<FrontPageNewsArticle>> FetchUoaNewsAsync(CancellationToken cancellationToken)
        {
            const string uoaUrl = "https://www.uoa.gr/anakoinoseis_kai_ekdiloseis";
            _logger.LogDebug("Starting fetch of UoA news from {Url}", uoaUrl);
            
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, uoaUrl);
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0 Safari/537.36");
                
                _logger.LogDebug("Sending HTTP request to UoA...");
                using var response = await client.SendAsync(request, cancellationToken);
                _logger.LogDebug("UoA response received with status code {StatusCode}", response.StatusCode);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("UoA news endpoint returned {StatusCode}", response.StatusCode);
                    return Array.Empty<FrontPageNewsArticle>();
                }
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(content);
                var articleNodes = htmlDocument.DocumentNode.SelectNodes("//div[contains(@class, 'topnews')]");

                var results = new List<FrontPageNewsArticle>();
                if (articleNodes != null)
                {
                    foreach (var node in articleNodes.Take(3))
                    {
                        var titleNode = node.SelectSingleNode(".//h3[@class='article__title']/a");
                        var rawTitle = titleNode?.InnerText.Trim();
                        // Decode HTML entities like &quot; to "
                        var title = string.IsNullOrWhiteSpace(rawTitle) ? null : WebUtility.HtmlDecode(rawTitle);
                        var relativeUrl = titleNode?.Attributes["href"]?.Value;
                        var url = relativeUrl == null ? null : new Uri(new Uri("https://www.uoa.gr"), relativeUrl).ToString();
                        var dateNode = node.SelectSingleNode(".//span[@class='article__date']/time");
                        var date = dateNode?.Attributes["datetime"]?.Value;
                        var categoryNode = node.SelectSingleNode(".//span[@class='article__category']/a");
                        var rawCategory = categoryNode?.InnerText.Trim();
                        // Decode HTML entities in category as well
                        var category = string.IsNullOrWhiteSpace(rawCategory) ? null : WebUtility.HtmlDecode(rawCategory);

                        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(url))
                        {
                            results.Add(new FrontPageNewsArticle(title, url!, date, category));
                        }
                    }
                }

                _logger.LogDebug("Successfully fetched {Count} UoA news articles", results.Count);
                return results;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Network error fetching UoA news articles. This may be due to connectivity issues, firewall, or the UoA website being temporarily unavailable.");
                return Array.Empty<FrontPageNewsArticle>();
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogDebug(ex, "UoA news fetch was cancelled (timeout or cancellation token)");
                return Array.Empty<FrontPageNewsArticle>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error fetching UoA news articles");
                return Array.Empty<FrontPageNewsArticle>();
            }
        }

        private async Task<IReadOnlyList<FrontPageNewsArticle>> FetchSvseNewsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://svse.gr/index.php/nea-anakoinoseis");
                request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0 Safari/537.36");
                using var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("SVSE news endpoint returned {StatusCode}", response.StatusCode);
                    return Array.Empty<FrontPageNewsArticle>();
                }
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(content);

                var articleNodes = htmlDocument.DocumentNode.SelectNodes("/html/body/div[1]/div/section[2]/div/div/div/main/div/div[3]/div[1]/div/div");
                var results = new List<FrontPageNewsArticle>();
                if (articleNodes != null)
                {
                    foreach (var node in articleNodes.Take(3))
                    {
                        var titleNode = node.SelectSingleNode(".//h2/a");
                        var rawTitle = titleNode?.InnerText.Trim();
                        // Decode HTML entities like &quot; to "
                        var title = string.IsNullOrWhiteSpace(rawTitle) ? null : WebUtility.HtmlDecode(rawTitle);
                        var relativeUrl = titleNode?.Attributes["href"]?.Value;
                        var url = relativeUrl == null ? null : new Uri(new Uri("https://svse.gr"), relativeUrl).ToString();
                        var dateNode = node.SelectSingleNode(".//time");
                        var rawDate = dateNode?.InnerText.Trim();
                        var date = string.IsNullOrWhiteSpace(rawDate) ? null : WebUtility.HtmlDecode(rawDate);

                        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(url))
                        {
                            results.Add(new FrontPageNewsArticle(title, url!, date, "SVSE"));
                        }
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch SVSE news articles");
                return Array.Empty<FrontPageNewsArticle>();
            }
        }
    }
}

