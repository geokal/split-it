using HtmlAgilityPack;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Net;

namespace QuizManager.Data
{
    public class GoogleScholarService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleScholarService> _logger;

        public GoogleScholarService(HttpClient httpClient, ILogger<GoogleScholarService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // Use a modern User-Agent
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        }

        public async Task<List<ScholarPublication>> GetPublications(string profileUrl)
        {
            var publications = new List<ScholarPublication>();
            int start = 0;
            int pageSize = 100;
            bool hasMore = true;

            // Clean the URL
            var baseUrl = profileUrl;
            if (baseUrl.Contains("&cstart=")) baseUrl = baseUrl.Substring(0, baseUrl.IndexOf("&cstart="));
            if (baseUrl.Contains("?cstart=")) baseUrl = baseUrl.Substring(0, baseUrl.IndexOf("?cstart="));

            try
            {
                while (hasMore)
                {
                    var separator = baseUrl.Contains("?") ? "&" : "?";
                    var pagedUrl = $"{baseUrl}{separator}cstart={start}&pagesize={pageSize}";

                    string html;
                    try
                    {
                        html = await _httpClient.GetStringAsync(pagedUrl);
                    }
                    catch (HttpRequestException ex)
                    {
                        // 1. Handle Google Blocking Gracefully
                        _logger.LogWarning($"Google Scholar blocked request at page {start}. Returning {publications.Count} publications found so far. Error: {ex.Message}");
                        break; // Stop looping, but return what we have!
                    }

                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    var rows = doc.DocumentNode.SelectNodes("//tr[@class='gsc_a_tr']");

                    if (rows == null || !rows.Any())
                    {
                        break; // No more rows found
                    }

                    foreach (var row in rows)
                    {
                        var publication = new ScholarPublication();

                        // --- TITLE ---
                        var titleNode = row.SelectSingleNode(".//a[@class='gsc_a_at']");
                        if (titleNode != null)
                        {
                            // 2. SAFETY TRUNCATE: Ensure title fits in DB (assuming 500 chars limit)
                            publication.Title = Truncate(System.Net.WebUtility.HtmlDecode(titleNode.InnerText.Trim()), 450);

                            var href = titleNode.GetAttributeValue("href", "");
                            if (!string.IsNullOrEmpty(href))
                                publication.Url = $"https://scholar.google.com{href}";
                        }

                        // --- AUTHORS & JOURNAL ---
                        var authorNodes = row.SelectNodes(".//div[@class='gs_gray']");
                        if (authorNodes != null)
                        {
                            if (authorNodes.Count > 0)
                            {
                                // 2. SAFETY TRUNCATE: Authors list can be HUGE. Truncate to avoid DB crash.
                                // Adjust '1000' to match your database column size (e.g., if NVARCHAR(MAX), this isn't needed).
                                publication.Authors = Truncate(System.Net.WebUtility.HtmlDecode(authorNodes[0].InnerText.Trim()), 1000);
                            }
                            if (authorNodes.Count > 1)
                            {
                                publication.Journal = Truncate(System.Net.WebUtility.HtmlDecode(authorNodes[1].InnerText.Trim()), 450);
                            }
                        }

                        // --- CITATIONS ---
                        var citedByNode = row.SelectSingleNode(".//a[@class='gsc_a_ac gs_ibl']");
                        if (citedByNode != null) publication.CitedBy = citedByNode.InnerText.Trim();

                        // --- YEAR ---
                        var yearNode = row.SelectSingleNode(".//span[@class='gsc_a_h gsc_a_hc gs_ibl']");
                        if (yearNode != null) publication.Year = yearNode.InnerText.Trim();

                        // Add if unique
                        if (!publications.Any(p => p.Title == publication.Title && p.Year == publication.Year))
                        {
                            publications.Add(publication);
                        }
                    }

                    // Pagination Logic
                    if (rows.Count < pageSize)
                    {
                        hasMore = false;
                    }
                    else
                    {
                        start += pageSize;
                        // 3. RANDOM DELAY: Helps avoid bot detection slightly better than fixed delay
                        var randomDelay = new Random().Next(1500, 3000);
                        await Task.Delay(randomDelay);
                    }
                }
            }
            catch (Exception ex)
            {
                // If something else goes wrong, log it but return partial results to keep the app alive
                _logger.LogError(ex, "Error fetching publications.");
            }

            return publications;
        }

        // Helper to safely truncate strings
        private string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }

    public class ScholarPublication
    {
        public string Title { get; set; }
        public string Authors { get; set; }
        public string Journal { get; set; }
        public string CitedBy { get; set; }
        public string Year { get; set; }
        public string Url { get; set; }
    }
}