using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.ViewModels;
using QuizManager.Services.CompanyDashboard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace QuizManager.Components.Layout.CompanySections
{
    public partial class CompanyAnnouncementsManagementSection : ComponentBase
    {
        [Inject] private ICompanyDashboardService CompanyDashboardService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private HttpClient HttpClient { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        // User Information
        private string CurrentUserEmail = "";

        // Main Form Visibility
        private bool isAnnouncementsFormVisibleToShowGeneralAnnouncementsAndEventsAsCompany = false;

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
        // Professor Announcements Section
        private bool isProfessorAnnouncementsVisible = false;
        private List<AnnouncementAsProfessor> ProfessorAnnouncements = new List<AnnouncementAsProfessor>();
        private int expandedProfessorAnnouncementId = -1;
        private int currentPageForProfessorAnnouncements = 1;

        // Research Group Announcements Section
        private bool isResearchGroupPublicAnnouncementsVisible = false;
        private List<AnnouncementAsResearchGroup> ResearchGroupAnnouncements = new List<AnnouncementAsResearchGroup>();
        private int expandedResearchGroupPublicAnnouncementId = 0;
        private int currentPageForResearchGroupPublicAnnouncements = 1;

        // Create Announcement Form
        private bool isAnnouncementsFormVisible = false;
        private AnnouncementAsCompany announcement = new AnnouncementAsCompany();
        private bool showErrorMessageforUploadingannouncementsAsCompany = false;
        private int remainingCharactersInAnnouncementFieldUploadAsCompany = 120;
        private int remainingCharactersInAnnouncementDescriptionUploadAsCompany = 1000;
        private string AnnouncementAttachmentErrorMessage = string.Empty;
        private bool showLoadingModal = false;
        private int loadingProgress = 0;
        private bool isFormValidToSaveAnnouncementAsCompany = true;
        private string saveAnnouncementAsCompanyMessage = string.Empty;
        private bool isSaveAnnouncementAsCompanySuccessful = false;

        // Computed Properties
        private int totalPagesForCompanyAnnouncements =>
            (int)Math.Ceiling((double)Announcements
                .Where(a => a.CompanyAnnouncementStatus == "Δημοσιευμένη")
                .Count() / pageSize);

        private int totalPagesForProfessorAnnouncements =>
            (int)Math.Ceiling((double)ProfessorAnnouncements
                .Where(a => a.ProfessorAnnouncementStatus == "Δημοσιευμένη")
                .Count() / pageSize);

        private int totalPagesForResearchGroupPublicAnnouncements =>
            (int)Math.Ceiling((double)(ResearchGroupAnnouncements?
                .Where(a => a.ResearchGroupAnnouncementStatus == "Δημοσιευμένη")
                .Count() ?? 0) / pageSize);

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            CurrentUserEmail = user.FindFirst("name")?.Value ?? "";

            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            // Load news articles
            newsArticles = await FetchNewsArticlesAsync();
            svseNewsArticles = await FetchSVSENewsArticlesAsync();

            // Load announcements
            var companyData = await CompanyDashboardService.LoadDashboardDataAsync();
            Announcements = companyData.Announcements.ToList();
            ProfessorAnnouncements = companyData.ProfessorEvents.SelectMany(_ => new List<AnnouncementAsProfessor>()).ToList(); // placeholder, data not in service
            ResearchGroupAnnouncements = companyData.ResearchGroupAnnouncements.ToList();

            // Calculate total pages
            UpdateTotalPages();
        }

        // Visibility Toggle Methods
        private void ToggleFormVisibilityToShowGeneralAnnouncementsAndEventsAsCompany()
        {
            isAnnouncementsFormVisibleToShowGeneralAnnouncementsAndEventsAsCompany = !isAnnouncementsFormVisibleToShowGeneralAnnouncementsAndEventsAsCompany;
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
            var data = await CompanyDashboardService.LoadDashboardDataAsync();
            return data.Announcements
                .Where(a => a.CompanyAnnouncementStatus == "Δημοσιευμένη")
                .OrderByDescending(a => a.CompanyAnnouncementUploadDate)
                .ToList();
        }

        private async Task<List<AnnouncementAsProfessor>> FetchProfessorAnnouncementsAsync()
        {
            // Professor announcements are not provided by CompanyDashboardService; return empty for now.
            return new List<AnnouncementAsProfessor>();
        }

        private async Task<List<AnnouncementAsResearchGroup>> FetchResearchGroupAnnouncementsAsync()
        {
            var data = await CompanyDashboardService.LoadDashboardDataAsync();
            return data.ResearchGroupAnnouncements
                .Where(a => a.ResearchGroupAnnouncementStatus == "Δημοσιευμένη")
                .OrderByDescending(a => a.ResearchGroupAnnouncementUploadDate)
                .ToList();
        }

        // Helper Methods
        private void UpdateTotalPages()
        {
            // Pages are computed properties, no update needed
            StateHasChanged();
        }

        // Form Visibility Toggle
        private void ToggleFormVisibilityForUploadCompanyAnnouncements()
        {
            isAnnouncementsFormVisible = !isAnnouncementsFormVisible;
            StateHasChanged();
        }

        // Character Limit Methods
        private void CheckCharacterLimitInAnnouncementFieldUploadAsCompany(ChangeEventArgs e)
        {
            var inputText = e.Value?.ToString() ?? string.Empty;
            remainingCharactersInAnnouncementFieldUploadAsCompany = 120 - inputText.Length;
            StateHasChanged();
        }

        private void CheckCharacterLimitInAnnouncementDescriptionUploadAsCompany(ChangeEventArgs e)
        {
            var inputText = e.Value?.ToString() ?? string.Empty;
            remainingCharactersInAnnouncementDescriptionUploadAsCompany = 1000 - inputText.Length;
            StateHasChanged();
        }

        // File Upload Method
        private async Task HandleFileSelectedForAnnouncementAttachment(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;

                if (file == null)
                {
                    announcement.CompanyAnnouncementAttachmentFile = null;
                    AnnouncementAttachmentErrorMessage = null;
                    return;
                }

                if (file.ContentType != "application/pdf")
                {
                    AnnouncementAttachmentErrorMessage = "Λάθος τύπος αρχείου. Επιλέξτε αρχείο PDF.";
                    announcement.CompanyAnnouncementAttachmentFile = null;
                    return;
                }

                const long maxFileSize = 10485760;
                if (file.Size > maxFileSize)
                {
                    AnnouncementAttachmentErrorMessage = "Το αρχείο είναι πολύ μεγάλο. Μέγιστο μέγεθος: 10MB";
                    announcement.CompanyAnnouncementAttachmentFile = null;
                    return;
                }

                using var ms = new MemoryStream();
                await file.OpenReadStream(maxFileSize).CopyToAsync(ms);
                announcement.CompanyAnnouncementAttachmentFile = ms.ToArray();

                AnnouncementAttachmentErrorMessage = null;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading announcement attachment: {ex.Message}");
                AnnouncementAttachmentErrorMessage = "Προέκυψε ένα σφάλμα κατά την μεταφόρτωση του αρχείου.";
                announcement.CompanyAnnouncementAttachmentFile = null;
                StateHasChanged();
            }
        }

        // Save Announcement Methods
        private async Task SaveAnnouncementAsPublished()
        {
            isFormValidToSaveAnnouncementAsCompany = ValidateMandatoryFieldsForUploadAnnouncementAsCompany();

            if (!isFormValidToSaveAnnouncementAsCompany)
                return;

            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] {
                $"Πρόκεται να Δημιουργήσετε μια Ανακοίνωση με Τίτλο: <strong>{announcement.CompanyAnnouncementTitle}</strong> ως '<strong>Δημοσιευμένη</strong>'.<br><br>" +
                "<strong style='color: red;'>Είστε σίγουρος/η;</strong>"
            });

            if (!isConfirmed)
                return;

            announcement.CompanyAnnouncementStatus = "Δημοσιευμένη";
            announcement.CompanyAnnouncementUploadDate = DateTime.Now;
            announcement.CompanyAnnouncementCompanyEmail = CurrentUserEmail;
            announcement.CompanyAnnouncementRNG = new Random().NextInt64();
            announcement.CompanyAnnouncementRNG_HashedAsUniqueID = HashingHelper.HashLong(announcement.CompanyAnnouncementRNG ?? 0);
            await SaveAnnouncementToDatabase();
        }

        private async Task SaveAnnouncementAsUnpublished()
        {
            isFormValidToSaveAnnouncementAsCompany = ValidateMandatoryFieldsForUploadAnnouncementAsCompany();

            if (!isFormValidToSaveAnnouncementAsCompany)
                return;

            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] {
                $"Πρόκεται να Δημιουργήσετε μια Ανακοίνωση με Τίτλο: <strong>{announcement.CompanyAnnouncementTitle}</strong> ως '<strong>Μη Δημοσιευμένη (Προσωρινή Αποθήκευση)</strong>'.<br><br>" +
                "<strong style='color: red;'>Είστε σίγουρος/η;</strong>"
            });

            if (!isConfirmed)
                return;

            announcement.CompanyAnnouncementStatus = "Μη Δημοσιευμένη";
            announcement.CompanyAnnouncementUploadDate = DateTime.Now;
            announcement.CompanyAnnouncementCompanyEmail = CurrentUserEmail;
            announcement.CompanyAnnouncementRNG = new Random().NextInt64();
            announcement.CompanyAnnouncementRNG_HashedAsUniqueID = HashingHelper.HashLong(announcement.CompanyAnnouncementRNG ?? 0);
            await SaveAnnouncementToDatabase();
        }

        private bool ValidateMandatoryFieldsForUploadAnnouncementAsCompany()
        {
            if (string.IsNullOrWhiteSpace(announcement.CompanyAnnouncementTitle) ||
                string.IsNullOrWhiteSpace(announcement.CompanyAnnouncementDescription) ||
                announcement.CompanyAnnouncementTimeToBeActive.Date == DateTime.Today)
            {
                showErrorMessageforUploadingannouncementsAsCompany = true;
                return false;
            }

            showErrorMessageforUploadingannouncementsAsCompany = false;
            return true;
        }

        private async Task SaveAnnouncementToDatabase()
        {
            showLoadingModal = true;
            loadingProgress = 0;
            StateHasChanged();

            try
            {
                await UpdateProgressWhenSaveAnnouncementAsCompany(25, 100);

                if (string.IsNullOrWhiteSpace(announcement.CompanyAnnouncementTitle))
                {
                    await HandleValidationErrorWhenSaveAnnouncementAsCompany("announcementTitle");
                    return;
                }

                if (string.IsNullOrWhiteSpace(announcement.CompanyAnnouncementDescription))
                {
                    await HandleValidationErrorWhenSaveAnnouncementAsCompany("announcementDescription");
                    return;
                }

                if (announcement.CompanyAnnouncementTimeToBeActive.Date <= DateTime.Today)
                {
                    await HandleValidationErrorWhenSaveAnnouncementAsCompany("announcementActiveDate");
                    return;
                }

                var result = await CompanyDashboardService.CreateOrUpdateAnnouncementAsync(announcement);
                if (!result.Success)
                {
                    throw new InvalidOperationException(result.Error ?? "Failed to save announcement.");
                }
                await UpdateProgressWhenSaveAnnouncementAsCompany(50, 200);

                isSaveAnnouncementAsCompanySuccessful = true;
                saveAnnouncementAsCompanyMessage = "Η Ανακοίνωση Δημιουργήθηκε Επιτυχώς";
                announcement = new AnnouncementAsCompany();
                await UpdateProgressWhenSaveAnnouncementAsCompany(75, 100);

                await UpdateProgressWhenSaveAnnouncementAsCompany(100, 200);
                await Task.Delay(500);

                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            catch (Exception ex)
            {
                showLoadingModal = false;
                isSaveAnnouncementAsCompanySuccessful = false;
                saveAnnouncementAsCompanyMessage = "Κάποιο πρόβλημα παρουσιάστηκε με την Δημιουργία της Ανακοίνωσης! Ανανεώστε την σελίδα και προσπαθήστε ξανά";
                Console.WriteLine($"Error saving announcement: {ex.Message}");
                StateHasChanged();
            }
        }

        private async Task HandleValidationErrorWhenSaveAnnouncementAsCompany(string elementId)
        {
            showErrorMessageforUploadingannouncementsAsCompany = true;
            showLoadingModal = false;
            StateHasChanged();
            await JS.InvokeVoidAsync("scrollToElementById", elementId);
        }

        private async Task UpdateProgressWhenSaveAnnouncementAsCompany(int increment, int delayMs = 0)
        {
            loadingProgress = increment;
            StateHasChanged();

            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }
        }

        // NewsArticle class definition
        public class NewsArticle
        {
            public string Title { get; set; }
            public string Url { get; set; }
            public string Date { get; set; }
            public string Category { get; set; }
        }
    }
}
