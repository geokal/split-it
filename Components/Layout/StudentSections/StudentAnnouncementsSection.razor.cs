using Microsoft.AspNetCore.Components;
using QuizManager.ViewModels;
using Microsoft.JSInterop;
using QuizManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using QuizManager.Services.FrontPage;

namespace QuizManager.Components.Layout.StudentSections
{
    public partial class StudentAnnouncementsSection : ComponentBase
    {
        [Inject] private IFrontPageService FrontPageService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private HttpClient HttpClient { get; set; } = default!;

        // Main Visibility
        private bool isAnnouncementsAsStudentVisible = false;

        // University News
        private bool isUniversityNewsVisible = false;
        private List<NewsArticle> newsArticles = new List<NewsArticle>();

        // SVSE News
        private bool isSvseNewsVisible = false;
        private List<NewsArticle> svseNewsArticles = new List<NewsArticle>();

        // Company Announcements
        private bool isCompanyAnnouncementsVisible = false;
        private List<AnnouncementAsCompany> Announcements = new List<AnnouncementAsCompany>();
        private int expandedAnnouncementId = -1;
        private int currentPageForCompanyAnnouncements = 1;
        private int pageSize = 10;
        private int totalPagesForCompanyAnnouncements = 0;

        // Professor Announcements
        private bool isProfessorAnnouncementsVisible = false;
        private List<AnnouncementAsProfessor> ProfessorAnnouncements = new List<AnnouncementAsProfessor>();
        private int expandedProfessorAnnouncementId = -1;
        private int currentPageForProfessorAnnouncements = 1;
        private int totalPagesForProfessorAnnouncements = 0;

        // Research Group Announcements
        private bool isResearchGroupPublicAnnouncementsVisible = false;
        private List<AnnouncementAsResearchGroup> ResearchGroupAnnouncements = new List<AnnouncementAsResearchGroup>();
        private int expandedResearchGroupPublicAnnouncementId = 0;
        private int currentPageForResearchGroupPublicAnnouncements = 1;
        private int totalPagesForResearchGroupPublicAnnouncements = 0;

        protected override async Task OnInitializedAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            await FrontPageService.EnsureDataLoadedAsync();
            var frontPageData = await FrontPageService.LoadFrontPageDataAsync();

            // Load news articles
            newsArticles = await FetchNewsArticlesAsync();
            svseNewsArticles = await FetchSVSENewsArticlesAsync();

            // Load announcements
            Announcements = frontPageData.CompanyAnnouncements.ToList();
            ProfessorAnnouncements = frontPageData.ProfessorAnnouncements.ToList();
            ResearchGroupAnnouncements = frontPageData.ResearchGroupAnnouncements.ToList();

            // Calculate total pages
            UpdateTotalPages();
        }

        // Visibility Toggle Methods
        private void ToggleAnnouncementsAsStudentVisibility()
        {
            isAnnouncementsAsStudentVisible = !isAnnouncementsAsStudentVisible;
            StateHasChanged();
        }

        private void ToggleUniversityNewsVisibility()
        {
            isUniversityNewsVisible = !isUniversityNewsVisible;
            StateHasChanged();
        }

        private void ToggleSvseNewsVisibility()
        {
            isSvseNewsVisible = !isSvseNewsVisible;
            StateHasChanged();
        }

        private void ToggleCompanyAnnouncementsVisibility()
        {
            isCompanyAnnouncementsVisible = !isCompanyAnnouncementsVisible;
            StateHasChanged();
        }

        private void ToggleProfessorAnnouncementsVisibility()
        {
            isProfessorAnnouncementsVisible = !isProfessorAnnouncementsVisible;
            StateHasChanged();
        }

        private void ToggleResearchGroupPublicAnnouncementsVisibility()
        {
            isResearchGroupPublicAnnouncementsVisible = !isResearchGroupPublicAnnouncementsVisible;
            StateHasChanged();
        }

        // Description Toggle Methods
        private void ToggleDescription(int announcementId)
        {
            if (expandedAnnouncementId == announcementId)
            {
                expandedAnnouncementId = -1;
            }
            else
            {
                expandedAnnouncementId = announcementId;
            }
            StateHasChanged();
        }

        private void ToggleDescriptionForProfessorAnnouncements(int announcementId)
        {
            if (expandedProfessorAnnouncementId == announcementId)
            {
                expandedProfessorAnnouncementId = -1;
            }
            else
            {
                expandedProfessorAnnouncementId = announcementId;
            }
            StateHasChanged();
        }

        private void ToggleDescriptionForResearchGroupPublicAnnouncements(int announcementId)
        {
            expandedResearchGroupPublicAnnouncementId = expandedResearchGroupPublicAnnouncementId == announcementId ? 0 : announcementId;
            StateHasChanged();
        }

        // Download Methods
        private async Task DownloadAnnouncementAttachmentFrontPage(byte[] attachmentData, string fileName)
        {
            if (attachmentData != null && attachmentData.Length > 0)
            {
                var mimeType = "application/pdf";
                await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile", fileName, mimeType, attachmentData);
            }
        }

        private async Task DownloadProfessorAnnouncementAttachmentFrontPage(byte[] attachmentData, string fileName)
        {
            if (attachmentData != null && attachmentData.Length > 0)
            {
                var mimeType = "application/pdf";
                if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".pdf";
                }
                await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile", fileName, mimeType, attachmentData);
            }
        }

        private async Task DownloadResearchGroupPublicAnnouncementAttachmentFrontPage(byte[] attachmentData, string fileName)
        {
            if (attachmentData != null && attachmentData.Length > 0)
            {
                var mimeType = "application/pdf";
                await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile", fileName, mimeType, attachmentData);
            }
        }

        // Pagination Methods - Company Announcements
        private void GoToFirstPageForCompanyAnnouncements()
        {
            currentPageForCompanyAnnouncements = 1;
            StateHasChanged();
        }

        private void PreviousPageForCompanyAnnouncements()
        {
            if (currentPageForCompanyAnnouncements > 1)
            {
                currentPageForCompanyAnnouncements--;
                StateHasChanged();
            }
        }

        private void NextPageForCompanyAnnouncements()
        {
            if (currentPageForCompanyAnnouncements < totalPagesForCompanyAnnouncements)
            {
                currentPageForCompanyAnnouncements++;
                StateHasChanged();
            }
        }

        private void GoToLastPageForCompanyAnnouncements()
        {
            currentPageForCompanyAnnouncements = totalPagesForCompanyAnnouncements;
            StateHasChanged();
        }

        private void GoToPageForCompanyAnnouncements(int pageNumber)
        {
            if (pageNumber >= 1 && pageNumber <= totalPagesForCompanyAnnouncements)
            {
                currentPageForCompanyAnnouncements = pageNumber;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePagesForCompanyAnnouncements()
        {
            var pages = new List<int>();
            int current = currentPageForCompanyAnnouncements;
            int total = totalPagesForCompanyAnnouncements;

            pages.Add(1);

            if (current > 3)
            {
                pages.Add(-1);
            }

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);

            for (int i = start; i <= end; i++)
            {
                pages.Add(i);
            }

            if (current < total - 2)
            {
                pages.Add(-1);
            }

            if (total > 1)
            {
                pages.Add(total);
            }

            return pages;
        }

        // Pagination Methods - Professor Announcements
        private void GoToFirstPageForProfessorAnnouncements()
        {
            currentPageForProfessorAnnouncements = 1;
            StateHasChanged();
        }

        private void PreviousPageForProfessorAnnouncements()
        {
            if (currentPageForProfessorAnnouncements > 1)
            {
                currentPageForProfessorAnnouncements--;
                StateHasChanged();
            }
        }

        private void NextPageForProfessorAnnouncements()
        {
            if (currentPageForProfessorAnnouncements < totalPagesForProfessorAnnouncements)
            {
                currentPageForProfessorAnnouncements++;
                StateHasChanged();
            }
        }

        private void GoToLastPageForProfessorAnnouncements()
        {
            currentPageForProfessorAnnouncements = totalPagesForProfessorAnnouncements;
            StateHasChanged();
        }

        private void GoToPageForProfessorAnnouncements(int pageNumber)
        {
            if (pageNumber >= 1 && pageNumber <= totalPagesForProfessorAnnouncements)
            {
                currentPageForProfessorAnnouncements = pageNumber;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePagesForProfessorAnnouncements()
        {
            var pages = new List<int>();
            int current = currentPageForProfessorAnnouncements;
            int total = totalPagesForProfessorAnnouncements;

            pages.Add(1);

            if (current > 3)
            {
                pages.Add(-1);
            }

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);

            for (int i = start; i <= end; i++)
            {
                pages.Add(i);
            }

            if (current < total - 2)
            {
                pages.Add(-1);
            }

            if (total > 1)
            {
                pages.Add(total);
            }

            return pages;
        }

        // Pagination Methods - Research Group Announcements
        private void GoToFirstPageForResearchGroupPublicAnnouncements()
        {
            currentPageForResearchGroupPublicAnnouncements = 1;
            StateHasChanged();
        }

        private void PreviousPageForResearchGroupPublicAnnouncements()
        {
            if (currentPageForResearchGroupPublicAnnouncements > 1)
            {
                currentPageForResearchGroupPublicAnnouncements--;
                StateHasChanged();
            }
        }

        private void NextPageForResearchGroupPublicAnnouncements()
        {
            if (currentPageForResearchGroupPublicAnnouncements < totalPagesForResearchGroupPublicAnnouncements)
            {
                currentPageForResearchGroupPublicAnnouncements++;
                StateHasChanged();
            }
        }

        private void GoToLastPageForResearchGroupPublicAnnouncements()
        {
            currentPageForResearchGroupPublicAnnouncements = totalPagesForResearchGroupPublicAnnouncements;
            StateHasChanged();
        }

        private void GoToPageForResearchGroupPublicAnnouncements(int pageNumber)
        {
            if (pageNumber >= 1 && pageNumber <= totalPagesForResearchGroupPublicAnnouncements)
            {
                currentPageForResearchGroupPublicAnnouncements = pageNumber;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePagesForResearchGroupPublicAnnouncements()
        {
            var pages = new List<int>();
            int current = currentPageForResearchGroupPublicAnnouncements;
            int total = totalPagesForResearchGroupPublicAnnouncements;

            pages.Add(1);

            if (current > 3)
            {
                pages.Add(-1);
            }

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);

            for (int i = start; i <= end; i++)
            {
                pages.Add(i);
            }

            if (current < total - 2)
            {
                pages.Add(-1);
            }

            if (total > 1)
            {
                pages.Add(total);
            }

            return pages;
        }

        // Data Loading Methods
        private async Task<List<NewsArticle>> FetchNewsArticlesAsync()
        {
            try
            {
                var response = await HttpClient.GetAsync("https://www.uoa.gr/anakoinoseis_kai_ekdiloseis");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(content);

                var articles = new List<NewsArticle>();

                var articleNodes = htmlDocument.DocumentNode.SelectNodes("//div[contains(@class, 'topnews')]");
                if (articleNodes != null)
                {
                    // Limit the number of articles to 3
                    for (int i = 0; i < Math.Min(articleNodes.Count, 3); i++)
                    {
                        var articleNode = articleNodes[i];

                        var titleNode = articleNode.SelectSingleNode(".//h3[@class='article__title']/a");
                        var title = titleNode?.InnerText.Trim();
                        var relativeUrl = titleNode?.Attributes["href"]?.Value;
                        var url = new Uri(new Uri("https://www.uoa.gr"), relativeUrl).ToString(); 

                        var dateNode = articleNode.SelectSingleNode(".//span[@class='article__date']/time");
                        var date = dateNode?.Attributes["datetime"]?.Value;

                        var categoryNode = articleNode.SelectSingleNode(".//span[@class='article__category']/a");
                        var category = categoryNode?.InnerText.Trim();

                        articles.Add(new NewsArticle
                            {
                                Title = title,
                                Url = url,
                                Date = date,
                                Category = category
                            });
                    }
                }

                return articles;
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine($"Error fetching news articles: {ex.Message}");
                return new List<NewsArticle>();
            }
        }

        private async Task<List<NewsArticle>> FetchSVSENewsArticlesAsync()
        {
            try
            {
                var response = await HttpClient.GetAsync("https://www.svse.uoa.gr/anakoinoseis/");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(content);

                var articles = new List<NewsArticle>();

                var articleNodes = htmlDocument.DocumentNode.SelectNodes("//div[contains(@class, 'topnews')]");
                if (articleNodes != null)
                {
                    // Limit the number of articles to 3
                    for (int i = 0; i < Math.Min(articleNodes.Count, 3); i++)
                    {
                        var articleNode = articleNodes[i];

                        var titleNode = articleNode.SelectSingleNode(".//h3[@class='article__title']/a");
                        var title = titleNode?.InnerText.Trim();
                        var relativeUrl = titleNode?.Attributes["href"]?.Value;
                        var url = new Uri(new Uri("https://www.svse.uoa.gr"), relativeUrl).ToString(); 

                        var dateNode = articleNode.SelectSingleNode(".//span[@class='article__date']/time");
                        var date = dateNode?.Attributes["datetime"]?.Value;

                        var categoryNode = articleNode.SelectSingleNode(".//span[@class='article__category']/a");
                        var category = categoryNode?.InnerText.Trim();

                        articles.Add(new NewsArticle
                            {
                                Title = title,
                                Url = url,
                                Date = date,
                                Category = category
                            });
                    }
                }

                return articles;
            }
            catch (Exception ex)
            {
                // Log error if needed
                Console.WriteLine($"Error fetching SVSE news articles: {ex.Message}");
                return new List<NewsArticle>();
            }
        }

        // Helper Methods
        private void UpdateTotalPages()
        {
            totalPagesForCompanyAnnouncements = (int)Math.Ceiling((double)Announcements
                .Where(a => a.CompanyAnnouncementStatus == "Δημοσιευμένη")
                .Count() / pageSize);

            totalPagesForProfessorAnnouncements = (int)Math.Ceiling((double)ProfessorAnnouncements
                .Where(a => a.ProfessorAnnouncementStatus == "Δημοσιευμένη")
                .Count() / pageSize);

            totalPagesForResearchGroupPublicAnnouncements = (int)Math.Ceiling((double)(ResearchGroupAnnouncements?
                .Where(a => a.ResearchGroupAnnouncementStatus == "Δημοσιευμένη")
                .Count() ?? 0) / pageSize);
        }

        // Computed Properties for Pagination
        private IEnumerable<AnnouncementAsCompany> GetPaginatedCompanyAnnouncements()
        {
            return Announcements
                .Where(a => a.CompanyAnnouncementStatus == "Δημοσιευμένη")
                .Skip((currentPageForCompanyAnnouncements - 1) * pageSize)
                .Take(pageSize);
        }

        private IEnumerable<AnnouncementAsProfessor> GetPaginatedProfessorAnnouncements()
        {
            return ProfessorAnnouncements
                .Where(a => a.ProfessorAnnouncementStatus == "Δημοσιευμένη")
                .Skip((currentPageForProfessorAnnouncements - 1) * pageSize)
                .Take(pageSize);
        }

        private IEnumerable<AnnouncementAsResearchGroup> GetPaginatedResearchGroupAnnouncements()
        {
            return ResearchGroupAnnouncements?
                .Where(a => a.ResearchGroupAnnouncementStatus == "Δημοσιευμένη")
                .Skip((currentPageForResearchGroupPublicAnnouncements - 1) * pageSize)
                .Take(pageSize) ?? Enumerable.Empty<AnnouncementAsResearchGroup>();
        }
    }
}
