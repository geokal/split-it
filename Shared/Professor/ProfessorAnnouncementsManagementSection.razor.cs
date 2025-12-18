using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace QuizManager.Shared.Professor
{
    public partial class ProfessorAnnouncementsManagementSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private HttpClient HttpClient { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        // User Information
        private string CurrentUserEmail = "";
        private string? professorName;
        private string? professorSurname;
        private string? professorDepartment;

        // Main Announcements Visibility
        private bool isAnnouncementsAsProfessorVisible = false;

        // University News Section
        private bool isUniversityNewsVisible = false;
        private List<NewsArticle> newsArticles = new List<NewsArticle>();

        // SVSE News Section
        private bool isSvseNewsVisible = false;
        private List<NewsArticle> svseNewsArticles = new List<NewsArticle>();

        // Company Announcements Section
        private bool isCompanyAnnouncementsVisible = false;
        private List<AnnouncementAsCompany> Announcements = new List<AnnouncementAsCompany>();
        private int expandedAnnouncementId = -1;
        private int currentPageForCompanyAnnouncements = 1;
        private int pageSize = 10;
        private int totalPagesForCompanyAnnouncements = 0;

        // Professor Announcements Section
        private bool isProfessorAnnouncementsVisible = false;
        private List<AnnouncementAsProfessor> ProfessorAnnouncements = new List<AnnouncementAsProfessor>();
        private int expandedProfessorAnnouncementId = -1;
        private int currentPageForProfessorAnnouncements = 1;
        private int totalPagesForProfessorAnnouncements = 0;

        // Research Group Announcements Section
        private bool isResearchGroupPublicAnnouncementsVisible = false;
        private List<AnnouncementAsResearchGroup> ResearchGroupAnnouncements = new List<AnnouncementAsResearchGroup>();
        private int expandedResearchGroupPublicAnnouncementId = 0;
        private int currentPageForResearchGroupPublicAnnouncements = 1;
        private int totalPagesForResearchGroupPublicAnnouncements = 0;

        // Company Events Section
        private bool isCompanyEventsVisible = false;
        private List<CompanyEvent> CompanyEventsToShowAtFrontPage = new List<CompanyEvent>();
        private int expandedCompanyEventId = -1;
        private int currentCompanyEventPage = 1;
        private int currentCompanyEventpageSize = 3;
        private int totalPagesForCompanyEvents = 0;

        // Professor Events Section
        private bool isProfessorEventsVisible = false;
        private List<ProfessorEvent> ProfessorEventsToShowAtFrontPage = new List<ProfessorEvent>();
        private int expandedProfessorEventId = -1;
        private int currentProfessorEventPage = 1;
        private int currentProfessorEventpageSize = 3;
        private int totalPagesForProfessorEvents = 0;

        // Create Announcement Form
        private bool isProfessorAnnouncementsFormVisible = false;
        private AnnouncementAsProfessor professorannouncement = new AnnouncementAsProfessor();
        private bool showErrorMessageforUploadingannouncementsAsProfessor = false;
        private int remainingCharactersInAnnouncementFieldUploadAsProfessor = 120;
        private int remainingCharactersInAnnouncementDescriptionUploadAsProfessor = 1000;
        private string ProfessorAnnouncementAttachmentErrorMessage = string.Empty;
        private bool showLoadingModalForProfessorAnnouncement = false;
        private int loadingProgress = 0;
        private bool isFormValidToSaveAnnouncementAsProfessor = true;
        private string saveAnnouncementAsProfessorMessage = string.Empty;
        private bool isSaveAnnouncementAsProfessorSuccessful = false;

        // Uploaded Announcements Management
        private bool isUploadedAnnouncementsVisibleAsProfessor = false;
        private bool isLoadingUploadedAnnouncementsAsProfessor = false;
        private string selectedStatusFilterForAnnouncementsAsProfessor = "Όλα";
        private int[] pageSizeOptions_SeeMyUploadedAnnouncementsAsProfessor = new[] { 10, 50, 100 };
        private int professorAnnouncementsPerPage_SeeMyUploadedAnnouncementsAsProfessor = 10;
        private int currentPageForProfessorAnnouncements_ProfessorAnnouncements = 1;
        private List<AnnouncementAsProfessor> UploadedAnnouncementsAsProfessor = new List<AnnouncementAsProfessor>();
        private List<AnnouncementAsProfessor> FilteredAnnouncementsAsProfessor = new List<AnnouncementAsProfessor>();
        private int totalCountAnnouncementsAsProfessor = 0;
        private int publishedCountAnnouncementsAsProfessor = 0;
        private int unpublishedCountAnnouncementsAsProfessor = 0;

        // Edit Modal
        private bool isEditModalVisibleForAnnouncementsAsProfessor = false;
        private AnnouncementAsProfessor currentAnnouncementAsProfessor = new AnnouncementAsProfessor();

        // Bulk Operations
        private bool isBulkEditModeForProfessorAnnouncements = false;
        private HashSet<int> selectedProfessorAnnouncementIds = new HashSet<int>();
        private string bulkActionForProfessorAnnouncements = "";
        private bool showBulkActionModalForProfessorAnnouncements = false;
        private List<AnnouncementAsProfessor> selectedProfessorAnnouncementsForAction = new List<AnnouncementAsProfessor>();
        private string newStatusForBulkActionForProfessorAnnouncements = "Μη Δημοσιευμένη";
        private bool showLoadingModalForDeleteProfessorAnnouncement = false;

        // Details Modal
        private bool showProfessorAnnouncementDetailsModal = false;
        private AnnouncementAsProfessor selectedProfessorAnnouncementToSeeDetailsAsProfessor = null;

        // Menu Toggle
        private int activeProfessorAnnouncementMenuId = 0;

        // Computed Properties
        private int totalPagesForProfessorAnnouncements_ProfessorAnnouncements =>
            (int)Math.Ceiling((double)(FilteredAnnouncementsAsProfessor?.Count ?? 0) / professorAnnouncementsPerPage_SeeMyUploadedAnnouncementsAsProfessor);

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            CurrentUserEmail = user.FindFirst("name")?.Value ?? "";

            // Load professor info
            if (!string.IsNullOrEmpty(CurrentUserEmail))
            {
                var professor = await dbContext.Professors
                    .FirstOrDefaultAsync(p => p.ProfEmail == CurrentUserEmail);
                if (professor != null)
                {
                    professorName = professor.ProfName;
                    professorSurname = professor.ProfSurname;
                    professorDepartment = professor.ProfDepartment;
                }
            }

            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            // Load news articles
            newsArticles = await FetchNewsArticlesAsync();
            svseNewsArticles = await FetchSVSENewsArticlesAsync();

            // Load announcements
            Announcements = await FetchAnnouncementsAsync();
            ProfessorAnnouncements = await FetchProfessorAnnouncementsAsync();
            ResearchGroupAnnouncements = await FetchResearchGroupAnnouncementsAsync();

            // Load events
            CompanyEventsToShowAtFrontPage = await FetchCompanyEventsAsync();
            ProfessorEventsToShowAtFrontPage = await FetchProfessorEventsAsync();

            // Calculate total pages
            UpdateTotalPages();
        }

        // Visibility Toggle Methods
        private void ToggleAnnouncementsAsProfessorVisibility()
        {
            isAnnouncementsAsProfessorVisible = !isAnnouncementsAsProfessorVisible;
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

        private void ToggleCompanyEventsVisibility()
        {
            isCompanyEventsVisible = !isCompanyEventsVisible;
            StateHasChanged();
        }

        private void ToggleProfessorEventsVisibility()
        {
            isProfessorEventsVisible = !isProfessorEventsVisible;
            StateHasChanged();
        }

        // Toggle Description Methods
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
            if (expandedResearchGroupPublicAnnouncementId == announcementId)
            {
                expandedResearchGroupPublicAnnouncementId = 0;
            }
            else
            {
                expandedResearchGroupPublicAnnouncementId = announcementId;
            }
            StateHasChanged();
        }

        private void ToggleDescriptionForCompanyEvent(int companyeventId)
        {
            if (expandedCompanyEventId == companyeventId)
            {
                expandedCompanyEventId = -1;
            }
            else
            {
                expandedCompanyEventId = companyeventId;
            }
            StateHasChanged();
        }

        private void ToggleDescriptionForProfessorEvent(int professoreventId)
        {
            if (expandedProfessorEventId == professoreventId)
            {
                expandedProfessorEventId = -1;
            }
            else
            {
                expandedProfessorEventId = professoreventId;
            }
            StateHasChanged();
        }

        // Download Attachment Methods
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
                if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".pdf";
                }
                await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile", fileName, mimeType, attachmentData);
            }
        }

        private async Task DownloadCompanyEventAttachmentFrontPage(byte[] attachmentData, string fileName)
        {
            if (attachmentData != null && attachmentData.Length > 0)
            {
                var base64 = Convert.ToBase64String(attachmentData);
                var fileUrl = $"data:application/pdf;base64,{base64}";
                await JS.InvokeVoidAsync("triggerDownload", fileUrl, fileName);
            }
        }

        private async Task DownloadProfessorEventAttachmentFrontPage(byte[] attachmentData, string fileName)
        {
            if (attachmentData != null && attachmentData.Length > 0)
            {
                var base64 = Convert.ToBase64String(attachmentData);
                var fileUrl = $"data:application/pdf;base64,{base64}";
                await JS.InvokeVoidAsync("triggerDownload", fileUrl, fileName);
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
            if (current > 3) pages.Add(-1);

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);
            for (int i = start; i <= end; i++) pages.Add(i);

            if (current < total - 2) pages.Add(-1);
            if (total > 1) pages.Add(total);

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
            if (current > 3) pages.Add(-1);

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);
            for (int i = start; i <= end; i++) pages.Add(i);

            if (current < total - 2) pages.Add(-1);
            if (total > 1) pages.Add(total);

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
            if (current > 3) pages.Add(-1);

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);
            for (int i = start; i <= end; i++) pages.Add(i);

            if (current < total - 2) pages.Add(-1);
            if (total > 1) pages.Add(total);

            return pages;
        }

        // Pagination Methods - Company Events
        private void ChangePageForCompanyEvents(int pageNumberForCompanyEvents)
        {
            if (pageNumberForCompanyEvents >= 1 && pageNumberForCompanyEvents <= totalPagesForCompanyEvents)
            {
                currentCompanyEventPage = pageNumberForCompanyEvents;
            }
            StateHasChanged();
        }

        // Pagination Methods - Professor Events
        private void ChangePageForProfessorEvents(int pageNumberForProfessorEvents)
        {
            if (pageNumberForProfessorEvents >= 1 && pageNumberForProfessorEvents <= totalPagesForProfessorEvents)
            {
                currentProfessorEventPage = pageNumberForProfessorEvents;
            }
            StateHasChanged();
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
                Console.WriteLine($"Error fetching news articles: {ex.Message}");
                return new List<NewsArticle>();
            }
        }

        private async Task<List<NewsArticle>> FetchSVSENewsArticlesAsync()
        {
            try
            {
                var response = await HttpClient.GetAsync("https://svse.gr/index.php/nea-anakoinoseis");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(content);

                var articles = new List<NewsArticle>();

                var articleNodes = htmlDocument.DocumentNode.SelectNodes("/html/body/div[1]/div/section[2]/div/div/div/main/div/div[3]/div[1]/div/div");

                if (articleNodes != null)
                {
                    foreach (var articleNode in articleNodes.Take(3))
                    {
                        var titleNode = articleNode.SelectSingleNode(".//h2/a");
                        var title = titleNode?.InnerText.Trim();
                        var relativeUrl = titleNode?.Attributes["href"]?.Value;
                        var url = new Uri(new Uri("https://svse.gr"), relativeUrl).ToString();

                        var dateNode = articleNode.SelectSingleNode(".//time");
                        var date = dateNode?.InnerText.Trim();

                        articles.Add(new NewsArticle
                        {
                            Title = title,
                            Url = url,
                            Date = date,
                            Category = "SVSE News"
                        });
                    }
                }

                return articles;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching SVSE news articles: {ex.Message}");
                return new List<NewsArticle>();
            }
        }

        private async Task<List<AnnouncementAsCompany>> FetchAnnouncementsAsync()
        {
            return await dbContext.AnnouncementsAsCompany
                .Include(a => a.Company)
                .AsNoTracking()
                .Where(a => a.CompanyAnnouncementStatus == "Δημοσιευμένη")
                .OrderByDescending(a => a.CompanyAnnouncementUploadDate)
                .ToListAsync();
        }

        private async Task<List<AnnouncementAsProfessor>> FetchProfessorAnnouncementsAsync()
        {
            return await dbContext.AnnouncementsAsProfessor
                .Include(a => a.Professor)
                .AsNoTracking()
                .Where(a => a.ProfessorAnnouncementStatus == "Δημοσιευμένη")
                .OrderByDescending(a => a.ProfessorAnnouncementUploadDate)
                .ToListAsync();
        }

        private async Task<List<AnnouncementAsResearchGroup>> FetchResearchGroupAnnouncementsAsync()
        {
            return await dbContext.AnnouncementAsResearchGroup
                .Include(a => a.ResearchGroup)
                .AsNoTracking()
                .Where(a => a.ResearchGroupAnnouncementStatus == "Δημοσιευμένη")
                .OrderByDescending(a => a.ResearchGroupAnnouncementUploadDate)
                .ToListAsync();
        }

        private async Task<List<CompanyEvent>> FetchCompanyEventsAsync()
        {
            return await dbContext.CompanyEvents
                .Include(e => e.Company)
                .AsNoTracking()
                .Where(e => e.CompanyEventStatus == "Δημοσιευμένη")
                .OrderByDescending(e => e.CompanyEventUploadedDate)
                .ToListAsync();
        }

        private async Task<List<ProfessorEvent>> FetchProfessorEventsAsync()
        {
            return await dbContext.ProfessorEvents
                .Include(e => e.Professor)
                .AsNoTracking()
                .Where(e => e.ProfessorEventStatus == "Δημοσιευμένη")
                .OrderByDescending(e => e.ProfessorEventUploadedDate)
                .ToListAsync();
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

            totalPagesForCompanyEvents = (int)Math.Ceiling((double)(CompanyEventsToShowAtFrontPage?
                .Where(e => e.CompanyEventStatus == "Δημοσιευμένη")
                .Count() ?? 0) / currentCompanyEventpageSize);

            totalPagesForProfessorEvents = (int)Math.Ceiling((double)(ProfessorEventsToShowAtFrontPage?
                .Where(e => e.ProfessorEventStatus == "Δημοσιευμένη")
                .Count() ?? 0) / currentProfessorEventpageSize);
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

        // Form Visibility Toggle
        private void ToggleFormVisibilityForUploadProfessorAnnouncements()
        {
            isProfessorAnnouncementsFormVisible = !isProfessorAnnouncementsFormVisible;
            StateHasChanged();
        }

        // Character Limit Methods
        private void CheckCharacterLimitInAnnouncementFieldUploadAsProfessor(ChangeEventArgs e)
        {
            var inputText = e.Value?.ToString() ?? string.Empty;
            remainingCharactersInAnnouncementFieldUploadAsProfessor = 120 - inputText.Length;
            StateHasChanged();
        }

        private void CheckCharacterLimitInAnnouncementDescriptionUploadAsProfessor(ChangeEventArgs e)
        {
            var inputText = e.Value?.ToString() ?? string.Empty;
            remainingCharactersInAnnouncementDescriptionUploadAsProfessor = 1000 - inputText.Length;
            StateHasChanged();
        }

        // File Upload Method
        private async Task HandleFileSelectedForAnnouncementAttachmentAsProfessor(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;

                if (file == null)
                {
                    professorannouncement.ProfessorAnnouncementAttachmentFile = null;
                    ProfessorAnnouncementAttachmentErrorMessage = null;
                    return;
                }

                if (file.ContentType != "application/pdf")
                {
                    ProfessorAnnouncementAttachmentErrorMessage = "Λάθος τύπος αρχείου. Επιλέξτε αρχείο PDF.";
                    professorannouncement.ProfessorAnnouncementAttachmentFile = null;
                    return;
                }

                const long maxFileSize = 10485760;
                if (file.Size > maxFileSize)
                {
                    ProfessorAnnouncementAttachmentErrorMessage = "Το αρχείο είναι πολύ μεγάλο. Μέγιστο μέγεθος: 10MB";
                    professorannouncement.ProfessorAnnouncementAttachmentFile = null;
                    return;
                }

                using var ms = new MemoryStream();
                await file.OpenReadStream(maxFileSize).CopyToAsync(ms);
                professorannouncement.ProfessorAnnouncementAttachmentFile = ms.ToArray();

                ProfessorAnnouncementAttachmentErrorMessage = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading professor announcement attachment: {ex.Message}");
                ProfessorAnnouncementAttachmentErrorMessage = "Προέκυψε ένα σφάλμα κατά την μεταφόρτωση του αρχείου.";
                professorannouncement.ProfessorAnnouncementAttachmentFile = null;
            }
        }

        // Save Announcement Methods
        private async Task SaveAnnouncementAsPublishedAsProfessor()
        {
            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] {
                $"Πρόκεται να Δημιουργήσετε μια Ανακοίνωση με Τίτλο: <strong>{professorannouncement.ProfessorAnnouncementTitle}</strong> ως '<strong>Δημοσιευμένη</strong>'.<br><br>" +
                "<strong style='color: red;'>Είστε σίγουρος/η;</strong>"
            });

            if (!isConfirmed)
                return;

            professorannouncement.ProfessorAnnouncementStatus = "Δημοσιευμένη";
            professorannouncement.ProfessorAnnouncementUploadDate = DateTime.Now;
            professorannouncement.ProfessorAnnouncementProfessorEmail = CurrentUserEmail;
            professorannouncement.ProfessorAnnouncementRNG = new Random().NextInt64();
            professorannouncement.ProfessorAnnouncementRNG_HashedAsUniqueID = HashingHelper.HashLong(professorannouncement.ProfessorAnnouncementRNG ?? 0);

            professorannouncement.Professor = new Professor
            {
                ProfEmail = CurrentUserEmail,
                ProfName = professorName,
                ProfSurname = professorSurname
            };

            await SaveAnnouncementToDatabaseAsProfessor();
        }

        private async Task SaveAnnouncementAsUnpublishedAsProfessor()
        {
            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] {
                $"Πρόκεται να Δημιουργήσετε μια Ανακοίνωση με Τίτλο: <strong>{professorannouncement.ProfessorAnnouncementTitle}</strong> ως '<strong>Μη Δημοσιευμένη (Προσωρινή Αποθήκευση)</strong>'.<br><br>" +
                "<strong style='color: red;'>Είστε σίγουρος/η;</strong>"
            });

            if (!isConfirmed)
                return;

            professorannouncement.ProfessorAnnouncementStatus = "Μη Δημοσιευμένη";
            professorannouncement.ProfessorAnnouncementUploadDate = DateTime.Now;
            professorannouncement.ProfessorAnnouncementProfessorEmail = CurrentUserEmail;
            professorannouncement.ProfessorAnnouncementRNG = new Random().NextInt64();
            professorannouncement.ProfessorAnnouncementRNG_HashedAsUniqueID = HashingHelper.HashLong(professorannouncement.ProfessorAnnouncementRNG ?? 0);

            professorannouncement.Professor = new Professor
            {
                ProfEmail = CurrentUserEmail,
                ProfName = professorName,
                ProfSurname = professorSurname
            };

            await SaveAnnouncementToDatabaseAsProfessor();
        }

        private async Task SaveAnnouncementToDatabaseAsProfessor()
        {
            showLoadingModalForProfessorAnnouncement = true;
            loadingProgress = 0;
            showErrorMessageforUploadingannouncementsAsProfessor = false;
            isSaveAnnouncementAsProfessorSuccessful = false;
            StateHasChanged();

            try
            {
                await UpdateProgressWhenSaveAnnouncementAsProfessor(20);
                if (string.IsNullOrWhiteSpace(professorannouncement.ProfessorAnnouncementTitle) ||
                    string.IsNullOrWhiteSpace(professorannouncement.ProfessorAnnouncementDescription) ||
                    professorannouncement.ProfessorAnnouncementTimeToBeActive.Date == DateTime.Today)
                {
                    await HandleProfessorAnnouncementValidationErrorWhenSaveAnnouncementAsProfessor();
                    return;
                }
                await UpdateProgressWhenSaveAnnouncementAsProfessor(40);

                await UpdateProgressWhenSaveAnnouncementAsProfessor(60);
                var existingProfessor = await dbContext.Professors
                    .FirstOrDefaultAsync(p => p.ProfEmail == CurrentUserEmail);

                if (existingProfessor != null)
                {
                    professorannouncement.Professor = existingProfessor;
                    professorannouncement.ProfessorAnnouncementProfessorEmail = existingProfessor.ProfEmail;
                }
                else
                {
                    await HandleProfessorNotFoundErrorWhenSaveAnnouncementAsProfessor();
                    return;
                }
                await UpdateProgressWhenSaveAnnouncementAsProfessor(80);

                await UpdateProgressWhenSaveAnnouncementAsProfessor(90);
                dbContext.AnnouncementsAsProfessor.Add(professorannouncement);
                await dbContext.SaveChangesAsync();
                await UpdateProgressWhenSaveAnnouncementAsProfessor(100);

                isSaveAnnouncementAsProfessorSuccessful = true;
                saveAnnouncementAsProfessorMessage = "Η Ανακοίνωση Δημιουργήθηκε Επιτυχώς";

                await Task.Delay(500);

                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            catch (Exception ex)
            {
                showLoadingModalForProfessorAnnouncement = false;
                isSaveAnnouncementAsProfessorSuccessful = false;
                saveAnnouncementAsProfessorMessage = "Κάποιο πρόβλημα παρουσιάστηκε με την Δημιουργία της Ανακοίνωσης! Ανανεώστε την σελίδα και προσπαθήστε ξανά";
                Console.WriteLine($"Error saving announcement: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                StateHasChanged();
            }
        }

        private async Task HandleProfessorAnnouncementValidationErrorWhenSaveAnnouncementAsProfessor()
        {
            showLoadingModalForProfessorAnnouncement = false;
            showErrorMessageforUploadingannouncementsAsProfessor = true;
            StateHasChanged();
        }

        private async Task HandleProfessorNotFoundErrorWhenSaveAnnouncementAsProfessor()
        {
            showLoadingModalForProfessorAnnouncement = false;
            isSaveAnnouncementAsProfessorSuccessful = false;
            saveAnnouncementAsProfessorMessage = "Ο Καθηγητής δεν βρέθηκε στο σύστημα";
            StateHasChanged();
        }

        private async Task UpdateProgressWhenSaveAnnouncementAsProfessor(int progress)
        {
            loadingProgress = progress;
            StateHasChanged();
            await Task.Delay(50);
        }

        // Uploaded Announcements Management
        private async Task ToggleUploadedAnnouncementsVisibilityAsProfessor()
        {
            isUploadedAnnouncementsVisibleAsProfessor = !isUploadedAnnouncementsVisibleAsProfessor;

            if (isUploadedAnnouncementsVisibleAsProfessor)
            {
                isLoadingUploadedAnnouncementsAsProfessor = true;
                StateHasChanged();

                try
                {
                    await LoadUploadedAnnouncementsAsProfessorAsync();
                    await ApplyFiltersAndUpdateCountsAsProfessor();
                }
                finally
                {
                    isLoadingUploadedAnnouncementsAsProfessor = false;
                    StateHasChanged();
                }
            }
            else
            {
                StateHasChanged();
            }
        }

        private async Task LoadUploadedAnnouncementsAsProfessorAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentUserEmail))
                {
                    UploadedAnnouncementsAsProfessor = await dbContext.AnnouncementsAsProfessor
                        .Where(i => i.ProfessorAnnouncementProfessorEmail == CurrentUserEmail)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Announcements: {ex.Message}");
            }
            StateHasChanged();
        }

        private async Task<List<AnnouncementAsProfessor>> GetUploadedAnnouncementsAsProfessor()
        {
            return await dbContext.AnnouncementsAsProfessor
                .Where(a => a.ProfessorAnnouncementProfessorEmail == CurrentUserEmail)
                .ToListAsync();
        }

        private async Task ApplyFiltersAndUpdateCountsAsProfessor()
        {
            if (UploadedAnnouncementsAsProfessor == null)
            {
                UploadedAnnouncementsAsProfessor = await GetUploadedAnnouncementsAsProfessor();
            }

            if (selectedStatusFilterForAnnouncementsAsProfessor == "Όλα")
            {
                FilteredAnnouncementsAsProfessor = UploadedAnnouncementsAsProfessor;
            }
            else
            {
                FilteredAnnouncementsAsProfessor = UploadedAnnouncementsAsProfessor
                    .Where(a => a.ProfessorAnnouncementStatus == selectedStatusFilterForAnnouncementsAsProfessor)
                    .ToList();
            }

            totalCountAnnouncementsAsProfessor = UploadedAnnouncementsAsProfessor.Count;
            publishedCountAnnouncementsAsProfessor = UploadedAnnouncementsAsProfessor.Count(a => a.ProfessorAnnouncementStatus == "Δημοσιευμένη");
            unpublishedCountAnnouncementsAsProfessor = UploadedAnnouncementsAsProfessor.Count(a => a.ProfessorAnnouncementStatus == "Μη Δημοσιευμένη");

            currentPageForProfessorAnnouncements_ProfessorAnnouncements = 1;
        }

        private void FilterAnnouncementsAsProfessor()
        {
            if (selectedStatusFilterForAnnouncementsAsProfessor == "Όλα")
            {
                FilteredAnnouncementsAsProfessor = UploadedAnnouncementsAsProfessor;
            }
            else if (selectedStatusFilterForAnnouncementsAsProfessor == "Δημοσιευμένη")
            {
                FilteredAnnouncementsAsProfessor = UploadedAnnouncementsAsProfessor
                    .Where(a => a.ProfessorAnnouncementStatus == "Δημοσιευμένη").ToList();
            }
            else if (selectedStatusFilterForAnnouncementsAsProfessor == "Μη Δημοσιευμένη")
            {
                FilteredAnnouncementsAsProfessor = UploadedAnnouncementsAsProfessor
                    .Where(a => a.ProfessorAnnouncementStatus == "Μη Δημοσιευμένη").ToList();
            }

            totalCountAnnouncementsAsProfessor = UploadedAnnouncementsAsProfessor.Count;
            publishedCountAnnouncementsAsProfessor = UploadedAnnouncementsAsProfessor
                .Count(a => a.ProfessorAnnouncementStatus == "Δημοσιευμένη");
            unpublishedCountAnnouncementsAsProfessor = UploadedAnnouncementsAsProfessor
                .Count(a => a.ProfessorAnnouncementStatus == "Μη Δημοσιευμένη");

            StateHasChanged();
        }

        private void HandleStatusFilterChangeForAnnouncementsAsProfessor(ChangeEventArgs e)
        {
            selectedStatusFilterForAnnouncementsAsProfessor = e.Value?.ToString() ?? "Όλα";
            FilterAnnouncementsAsProfessor();
        }

        private void OnPageSizeChange_SeeMyUploadedAnnouncementsAsProfessor(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                professorAnnouncementsPerPage_SeeMyUploadedAnnouncementsAsProfessor = newSize;
                currentPageForProfessorAnnouncements_ProfessorAnnouncements = 1;
                StateHasChanged();
            }
        }

        private IEnumerable<AnnouncementAsProfessor> GetPaginatedProfessorAnnouncements_ProfessorAnnouncements()
        {
            return FilteredAnnouncementsAsProfessor?
                .Skip((currentPageForProfessorAnnouncements_ProfessorAnnouncements - 1) * professorAnnouncementsPerPage_SeeMyUploadedAnnouncementsAsProfessor)
                .Take(professorAnnouncementsPerPage_SeeMyUploadedAnnouncementsAsProfessor) ?? Enumerable.Empty<AnnouncementAsProfessor>();
        }

        private List<int> GetVisiblePagesForProfessorAnnouncements_ProfessorAnnouncements()
        {
            var pages = new List<int>();
            int current = currentPageForProfessorAnnouncements_ProfessorAnnouncements;
            int total = totalPagesForProfessorAnnouncements_ProfessorAnnouncements;

            pages.Add(1);
            if (current > 3) pages.Add(-1);

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);
            for (int i = start; i <= end; i++) pages.Add(i);

            if (current < total - 2) pages.Add(-1);
            if (total > 1) pages.Add(total);

            return pages;
        }

        private void GoToFirstPageForProfessorAnnouncements_ProfessorAnnouncements()
        {
            currentPageForProfessorAnnouncements_ProfessorAnnouncements = 1;
            StateHasChanged();
        }

        private void GoToLastPageForProfessorAnnouncements_ProfessorAnnouncements()
        {
            currentPageForProfessorAnnouncements_ProfessorAnnouncements = totalPagesForProfessorAnnouncements_ProfessorAnnouncements;
            StateHasChanged();
        }

        private void PreviousPageForProfessorAnnouncements_ProfessorAnnouncements()
        {
            if (currentPageForProfessorAnnouncements_ProfessorAnnouncements > 1)
            {
                currentPageForProfessorAnnouncements_ProfessorAnnouncements--;
                StateHasChanged();
            }
        }

        private void NextPageForProfessorAnnouncements_ProfessorAnnouncements()
        {
            if (currentPageForProfessorAnnouncements_ProfessorAnnouncements < totalPagesForProfessorAnnouncements_ProfessorAnnouncements)
            {
                currentPageForProfessorAnnouncements_ProfessorAnnouncements++;
                StateHasChanged();
            }
        }

        private void GoToPageForProfessorAnnouncements_ProfessorAnnouncements(int page)
        {
            if (page > 0 && page <= totalPagesForProfessorAnnouncements_ProfessorAnnouncements)
            {
                currentPageForProfessorAnnouncements_ProfessorAnnouncements = page;
                StateHasChanged();
            }
        }

        // Menu Toggle Method
        private void ToggleProfessorAnnouncementMenu(int announcementId)
        {
            activeProfessorAnnouncementMenuId = activeProfessorAnnouncementMenuId == announcementId ? 0 : announcementId;
            StateHasChanged();
        }

        // Edit Modal Methods
        private void OpenEditModalAsProfessor(AnnouncementAsProfessor professorAnnouncement)
        {
            currentAnnouncementAsProfessor = new AnnouncementAsProfessor
            {
                Id = professorAnnouncement.Id,
                ProfessorAnnouncementRNG = professorAnnouncement.ProfessorAnnouncementRNG,
                ProfessorAnnouncementRNG_HashedAsUniqueID = professorAnnouncement.ProfessorAnnouncementRNG_HashedAsUniqueID,
                ProfessorAnnouncementTitle = professorAnnouncement.ProfessorAnnouncementTitle,
                ProfessorAnnouncementDescription = professorAnnouncement.ProfessorAnnouncementDescription,
                ProfessorAnnouncementStatus = professorAnnouncement.ProfessorAnnouncementStatus,
                ProfessorAnnouncementUploadDate = professorAnnouncement.ProfessorAnnouncementUploadDate,
                ProfessorAnnouncementProfessorEmail = professorAnnouncement.ProfessorAnnouncementProfessorEmail,
                ProfessorAnnouncementTimeToBeActive = professorAnnouncement.ProfessorAnnouncementTimeToBeActive,
                ProfessorAnnouncementAttachmentFile = professorAnnouncement.ProfessorAnnouncementAttachmentFile
            };

            if (professorAnnouncement.Professor != null)
            {
                currentAnnouncementAsProfessor.Professor = new Professor
                {
                    Id = professorAnnouncement.Professor.Id,
                    ProfEmail = professorAnnouncement.Professor.ProfEmail,
                    Professor_UniqueID = professorAnnouncement.Professor.Professor_UniqueID,
                    ProfImage = professorAnnouncement.Professor.ProfImage,
                    ProfName = professorAnnouncement.Professor.ProfName,
                    ProfSurname = professorAnnouncement.Professor.ProfSurname,
                    ProfUniversity = professorAnnouncement.Professor.ProfUniversity,
                    ProfDepartment = professorAnnouncement.Professor.ProfDepartment,
                    ProfVahmidaDEP = professorAnnouncement.Professor.ProfVahmidaDEP,
                    ProfWorkTelephone = professorAnnouncement.Professor.ProfWorkTelephone,
                    ProfPersonalTelephone = professorAnnouncement.Professor.ProfPersonalTelephone,
                    ProfPersonalTelephoneVisibility = professorAnnouncement.Professor.ProfPersonalTelephoneVisibility,
                    ProfPersonalWebsite = professorAnnouncement.Professor.ProfPersonalWebsite,
                    ProfLinkedInSite = professorAnnouncement.Professor.ProfLinkedInSite,
                    ProfScholarProfile = professorAnnouncement.Professor.ProfScholarProfile,
                    ProfOrchidProfile = professorAnnouncement.Professor.ProfOrchidProfile,
                    ProfGeneralFieldOfWork = professorAnnouncement.Professor.ProfGeneralFieldOfWork,
                    ProfGeneralSkills = professorAnnouncement.Professor.ProfGeneralSkills,
                    ProfPersonalDescription = professorAnnouncement.Professor.ProfPersonalDescription,
                    ProfCVAttachment = professorAnnouncement.Professor.ProfCVAttachment,
                    ProfRegistryNumber = professorAnnouncement.Professor.ProfRegistryNumber,
                    ProfCourses = professorAnnouncement.Professor.ProfCourses
                };
            }

            isEditModalVisibleForAnnouncementsAsProfessor = true;
            StateHasChanged();
        }

        private void CloseEditModalForAnnouncementsAsProfessor()
        {
            isEditModalVisibleForAnnouncementsAsProfessor = false;
            StateHasChanged();
        }

        private async Task UpdateAnnouncementAsProfessor(AnnouncementAsProfessor updatedAnnouncementasProfessor)
        {
            var existingAnnouncementasProfessor = await dbContext.AnnouncementsAsProfessor.FindAsync(updatedAnnouncementasProfessor.Id);

            if (existingAnnouncementasProfessor != null)
            {
                existingAnnouncementasProfessor.ProfessorAnnouncementTitle = updatedAnnouncementasProfessor.ProfessorAnnouncementTitle;
                existingAnnouncementasProfessor.ProfessorAnnouncementDescription = updatedAnnouncementasProfessor.ProfessorAnnouncementDescription;
                existingAnnouncementasProfessor.ProfessorAnnouncementAttachmentFile = updatedAnnouncementasProfessor.ProfessorAnnouncementAttachmentFile;
                await dbContext.SaveChangesAsync();
                CloseEditModalForAnnouncementsAsProfessor();
                await LoadUploadedAnnouncementsAsProfessorAsync();
                await ApplyFiltersAndUpdateCountsAsProfessor();
            }
        }

        private async Task HandleFileUploadToEditProfessorAnnouncementAttachment(InputFileChangeEventArgs e)
        {
            if (currentAnnouncementAsProfessor == null || e.File == null)
            {
                return;
            }

            try
            {
                if (e.File.ContentType != "application/pdf")
                {
                    await JS.InvokeVoidAsync("alert", "Λάθος τύπος αρχείου. Επιλέξτε αρχείο PDF.");
                    return;
                }

                const long maxFileSize = 10485760;
                if (e.File.Size > maxFileSize)
                {
                    await JS.InvokeVoidAsync("alert", "Το αρχείο είναι πολύ μεγάλο. Μέγιστο μέγεθος: 10MB");
                    return;
                }

                using var stream = e.File.OpenReadStream(maxFileSize);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                currentAnnouncementAsProfessor.ProfessorAnnouncementAttachmentFile = memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error uploading announcement attachment: {ex.Message}");
                await JS.InvokeVoidAsync("alert", "Προέκυψε ένα σφάλμα κατά την μεταφόρτωση του αρχείου.");
            }
        }

        // Status Change Methods
        private async Task ChangeAnnouncementStatusAsProfessor(int professorannouncementId, string professorannouncementnewStatus)
        {
            var isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[]
            {
                $"Πρόκειται να αλλάξετε την κατάσταση αυτής της Ανακοίνωσης σε '{professorannouncementnewStatus}'. Είστε σίγουρος/η;"
            });

            if (isConfirmed)
            {
                var professorannouncement = UploadedAnnouncementsAsProfessor.FirstOrDefault(a => a.Id == professorannouncementId);
                if (professorannouncement != null)
                {
                    professorannouncement.ProfessorAnnouncementStatus = professorannouncementnewStatus;
                    await dbContext.SaveChangesAsync();
                    await LoadUploadedAnnouncementsAsProfessorAsync();
                    await ApplyFiltersAndUpdateCountsAsProfessor();
                }
                StateHasChanged();
            }
        }

        // Delete Method
        private async Task DeleteAnnouncementAsProfessor(int professorannouncementId)
        {
            var isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML",
                "Πρόκειται να διαγράψετε οριστικά αυτή την Ανακοίνωση.<br><br>" +
                "<strong style='color: red;'>Είστε σίγουρος/η;</strong>");

            if (isConfirmed)
            {
                showLoadingModalForDeleteProfessorAnnouncement = true;
                loadingProgress = 0;
                StateHasChanged();

                try
                {
                    await UpdateProgressWhenDeleteAnnouncementAsProfessor(30);
                    var professorannouncement = await dbContext.AnnouncementsAsProfessor.FindAsync(professorannouncementId);

                    if (professorannouncement != null)
                    {
                        await UpdateProgressWhenDeleteAnnouncementAsProfessor(60);
                        dbContext.AnnouncementsAsProfessor.Remove(professorannouncement);
                        await dbContext.SaveChangesAsync();
                        await UpdateProgressWhenDeleteAnnouncementAsProfessor(80);

                        await UpdateProgressWhenDeleteAnnouncementAsProfessor(90);
                        UploadedAnnouncementsAsProfessor = await GetUploadedAnnouncementsAsProfessor();

                        await ApplyFiltersAndUpdateCountsAsProfessor();

                        await UpdateProgressWhenDeleteAnnouncementAsProfessor(100);

                        await Task.Delay(300);
                    }
                    else
                    {
                        showLoadingModalForDeleteProfessorAnnouncement = false;
                        await JS.InvokeVoidAsync("alert", "Η ανακοίνωση δεν βρέθηκε.");
                        StateHasChanged();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    showLoadingModalForDeleteProfessorAnnouncement = false;
                    await JS.InvokeVoidAsync("alert", $"Σφάλμα κατά τη διαγραφή: {ex.Message}");
                    StateHasChanged();
                    return;
                }
                finally
                {
                    showLoadingModalForDeleteProfessorAnnouncement = false;
                }

                StateHasChanged();
            }
        }

        private async Task UpdateProgressWhenDeleteAnnouncementAsProfessor(int progress)
        {
            loadingProgress = progress;
            StateHasChanged();
            await Task.Delay(50);
        }

        // Bulk Operations
        private void EnableBulkEditModeForProfessorAnnouncements()
        {
            isBulkEditModeForProfessorAnnouncements = true;
            selectedProfessorAnnouncementIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForProfessorAnnouncements()
        {
            isBulkEditModeForProfessorAnnouncements = false;
            selectedProfessorAnnouncementIds.Clear();
            StateHasChanged();
        }

        private void ToggleProfessorAnnouncementSelection(int announcementId, ChangeEventArgs e)
        {
            var isChecked = (bool)(e.Value ?? false);
            if (isChecked)
            {
                selectedProfessorAnnouncementIds.Add(announcementId);
            }
            else
            {
                selectedProfessorAnnouncementIds.Remove(announcementId);
            }
            StateHasChanged();
        }

        private void ToggleAllProfessorAnnouncementsSelection(ChangeEventArgs e)
        {
            var isChecked = (bool)(e.Value ?? false);
            var paginatedAnnouncements = GetPaginatedProfessorAnnouncements_ProfessorAnnouncements();

            if (isChecked)
            {
                selectedProfessorAnnouncementIds = new HashSet<int>(paginatedAnnouncements.Select(a => a.Id));
            }
            else
            {
                selectedProfessorAnnouncementIds.Clear();
            }
            StateHasChanged();
        }

        private void ShowBulkActionOptionsForProfessorAnnouncements()
        {
            if (selectedProfessorAnnouncementIds.Count == 0) return;

            selectedProfessorAnnouncementsForAction = FilteredAnnouncementsAsProfessor
                .Where(a => selectedProfessorAnnouncementIds.Contains(a.Id))
                .ToList();
            bulkActionForProfessorAnnouncements = "";
            newStatusForBulkActionForProfessorAnnouncements = "Μη Δημοσιευμένη";
            showBulkActionModalForProfessorAnnouncements = true;
            StateHasChanged();
        }

        private void CloseBulkActionModalForProfessorAnnouncements()
        {
            showBulkActionModalForProfessorAnnouncements = false;
            bulkActionForProfessorAnnouncements = "";
            StateHasChanged();
        }

        private async Task ExecuteBulkActionForProfessorAnnouncements()
        {
            if (string.IsNullOrEmpty(bulkActionForProfessorAnnouncements) || selectedProfessorAnnouncementIds.Count == 0) return;

            string confirmationMessage = "";
            string actionDescription = "";

            if (bulkActionForProfessorAnnouncements == "status")
            {
                var announcementsWithSameStatus = selectedProfessorAnnouncementsForAction
                    .Where(a => a.ProfessorAnnouncementStatus == newStatusForBulkActionForProfessorAnnouncements)
                    .ToList();

                if (announcementsWithSameStatus.Any())
                {
                    string alreadySameStatusMessage =
                        $"<strong style='color: orange;'>Προσοχή:</strong> {announcementsWithSameStatus.Count} από τις επιλεγμένες ανακοινώσεις είναι ήδη στην κατάσταση <strong>'{newStatusForBulkActionForProfessorAnnouncements}'</strong> και δεν θα επηρεαστούν.<br><br>" +
                        "<strong>Ανακοινώσεις που δεν θα αλλάξουν:</strong><br>";

                    foreach (var announcement in announcementsWithSameStatus.Take(5))
                    {
                        alreadySameStatusMessage += $"- {announcement.ProfessorAnnouncementTitle} ({announcement.ProfessorAnnouncementRNG_HashedAsUniqueID})<br>";
                    }

                    if (announcementsWithSameStatus.Count > 5)
                    {
                        alreadySameStatusMessage += $"- ... και άλλες {announcementsWithSameStatus.Count - 5} ανακοινώσεις<br>";
                    }

                    alreadySameStatusMessage += "<br>";

                    bool continueAfterWarning = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] {
                        alreadySameStatusMessage +
                        "Θέλετε να συνεχίσετε με τις υπόλοιπες ανακοινώσεις;"
                    });

                    if (!continueAfterWarning)
                    {
                        CloseBulkActionModalForProfessorAnnouncements();
                        return;
                    }

                    foreach (var announcement in announcementsWithSameStatus)
                    {
                        selectedProfessorAnnouncementIds.Remove(announcement.Id);
                    }

                    selectedProfessorAnnouncementsForAction = selectedProfessorAnnouncementsForAction
                        .Where(a => !announcementsWithSameStatus.Contains(a))
                        .ToList();

                    if (selectedProfessorAnnouncementIds.Count == 0)
                    {
                        await JS.InvokeVoidAsync("confirmActionWithHTML2", "Δεν υπάρχουν ανακοινώσεις για αλλαγή κατάστασης. Όλες οι επιλεγμένες ανακοινώσεις είναι ήδη στην επιθυμητή κατάσταση.");
                        CloseBulkActionModalForProfessorAnnouncements();
                        return;
                    }
                }

                actionDescription = $"αλλαγή κατάστασης σε '{newStatusForBulkActionForProfessorAnnouncements}'";
                confirmationMessage = $"Είστε σίγουροι πως θέλετε να αλλάξετε την κατάσταση των {selectedProfessorAnnouncementIds.Count} επιλεγμένων ανακοινώσεων σε <strong>'{newStatusForBulkActionForProfessorAnnouncements}'</strong>?<br><br>";
            }
            else if (bulkActionForProfessorAnnouncements == "copy")
            {
                actionDescription = "αντιγραφή";
                confirmationMessage = $"Είστε σίγουροι πως θέλετε να αντιγράψετε τις {selectedProfessorAnnouncementIds.Count} επιλεγμένες ανακοινώσεις?<br>Οι νέες ανακοινώσεις θα έχουν κατάσταση <strong>'Μη Δημοσιευμένη'</strong>.<br><br>";
            }

            confirmationMessage += "<strong>Επιλεγμένες Ανακοινώσεις:</strong><br>";
            foreach (var announcement in selectedProfessorAnnouncementsForAction.Take(10))
            {
                confirmationMessage += $"- {announcement.ProfessorAnnouncementTitle} ({announcement.ProfessorAnnouncementRNG_HashedAsUniqueID})<br>";
            }

            if (selectedProfessorAnnouncementsForAction.Count > 10)
            {
                confirmationMessage += $"- ... και άλλες {selectedProfessorAnnouncementsForAction.Count - 10} ανακοινώσεις<br>";
            }

            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] { confirmationMessage });

            if (!isConfirmed)
            {
                CloseBulkActionModalForProfessorAnnouncements();
                return;
            }

            try
            {
                showBulkActionModalForProfessorAnnouncements = false;

                if (bulkActionForProfessorAnnouncements == "status")
                {
                    await UpdateMultipleProfessorAnnouncementStatuses();
                }
                else if (bulkActionForProfessorAnnouncements == "copy")
                {
                    await CopyMultipleProfessorAnnouncements();
                }

                await LoadUploadedAnnouncementsAsProfessorAsync();
                await ApplyFiltersAndUpdateCountsAsProfessor();
                CancelBulkEditForProfessorAnnouncements();

                var tabUrl = $"{NavigationManager.Uri.Split('?')[0]}#professor-announcements";
                NavigationManager.NavigateTo(tabUrl, true);
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during bulk action for professor announcements: {ex.Message}");
            }
        }

        private async Task CopyMultipleProfessorAnnouncements()
        {
            var announcementsToCopy = FilteredAnnouncementsAsProfessor
                .Where(a => selectedProfessorAnnouncementIds.Contains(a.Id))
                .ToList();

            foreach (var originalAnnouncement in announcementsToCopy)
            {
                try
                {
                    var newAnnouncement = new AnnouncementAsProfessor
                    {
                        ProfessorAnnouncementTitle = originalAnnouncement.ProfessorAnnouncementTitle,
                        ProfessorAnnouncementDescription = originalAnnouncement.ProfessorAnnouncementDescription,
                        ProfessorAnnouncementTimeToBeActive = originalAnnouncement.ProfessorAnnouncementTimeToBeActive,
                        ProfessorAnnouncementAttachmentFile = originalAnnouncement.ProfessorAnnouncementAttachmentFile,
                        ProfessorAnnouncementProfessorEmail = originalAnnouncement.ProfessorAnnouncementProfessorEmail,

                        ProfessorAnnouncementRNG = new Random().NextInt64(),
                        ProfessorAnnouncementRNG_HashedAsUniqueID = HashingHelper.HashLong(new Random().NextInt64()),

                        ProfessorAnnouncementStatus = "Μη Δημοσιευμένη",
                        ProfessorAnnouncementUploadDate = DateTime.Now
                    };

                    dbContext.AnnouncementsAsProfessor.Add(newAnnouncement);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error copying professor announcement {originalAnnouncement.Id}: {ex.Message}");
                }
            }

            await dbContext.SaveChangesAsync();
        }

        private async Task UpdateMultipleProfessorAnnouncementStatuses()
        {
            foreach (var announcementId in selectedProfessorAnnouncementIds)
            {
                await UpdateProfessorAnnouncementStatusDirectly(announcementId, newStatusForBulkActionForProfessorAnnouncements);
            }
        }

        private async Task UpdateProfessorAnnouncementStatusDirectly(int announcementId, string newStatus)
        {
            try
            {
                var announcement = await dbContext.AnnouncementsAsProfessor
                    .FirstOrDefaultAsync(a => a.Id == announcementId);

                if (announcement != null)
                {
                    announcement.ProfessorAnnouncementStatus = newStatus;
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating professor announcement status for announcement {announcementId}: {ex.Message}");
            }
        }

        private async Task ExecuteBulkStatusChangeForProfessorAnnouncements(string newStatus)
        {
            if (selectedProfessorAnnouncementIds.Count == 0) return;

            bulkActionForProfessorAnnouncements = "status";
            newStatusForBulkActionForProfessorAnnouncements = newStatus;

            selectedProfessorAnnouncementsForAction = FilteredAnnouncementsAsProfessor
                .Where(a => selectedProfessorAnnouncementIds.Contains(a.Id))
                .ToList();

            await ExecuteBulkActionForProfessorAnnouncements();
        }

        private async Task ExecuteBulkCopyForProfessorAnnouncements()
        {
            if (selectedProfessorAnnouncementIds.Count == 0) return;

            bulkActionForProfessorAnnouncements = "copy";

            selectedProfessorAnnouncementsForAction = FilteredAnnouncementsAsProfessor
                .Where(a => selectedProfessorAnnouncementIds.Contains(a.Id))
                .ToList();

            await ExecuteBulkActionForProfessorAnnouncements();
        }

        // Details Modal Methods
        private void OpenProfessorAnnouncementDetailsModal(AnnouncementAsProfessor announcement)
        {
            selectedProfessorAnnouncementToSeeDetailsAsProfessor = announcement;
            showProfessorAnnouncementDetailsModal = true;
            activeProfessorAnnouncementMenuId = 0; // Close menu when opening details
            StateHasChanged();
        }

        private void CloseProfessorAnnouncementDetailsModal()
        {
            selectedProfessorAnnouncementToSeeDetailsAsProfessor = null;
            showProfessorAnnouncementDetailsModal = false;
            StateHasChanged();
        }

        // NewsArticle Model
        public class NewsArticle
        {
            public string Title { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public string Date { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
        }
    }
}


