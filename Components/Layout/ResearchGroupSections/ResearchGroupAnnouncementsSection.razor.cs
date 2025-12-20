using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.ViewModels;
using QuizManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace QuizManager.Components.Layout.ResearchGroupSections
{
    public partial class ResearchGroupAnnouncementsSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        // Current User
        private string CurrentUserEmail = "";

        // Visibility States
        private bool isAnnouncementsFormVisibleToShowGeneralAnnouncementsAndEventsAsRG = false;
        private bool isResearchGroupToUploadAnnouncementsFormVisible = false;
        private bool isUniversityNewsVisible = false;
        private bool isSvseNewsVisible = false;
        private bool isCompanyAnnouncementsVisible = false;
        private bool isProfessorAnnouncementsVisible = false;
        private bool isResearchGroupPublicAnnouncementsVisible = false;
        private bool isUploadedResearchGroupAnnouncementsVisible = false;

        // News Data
        private List<NewsArticle> newsArticles = new List<NewsArticle>();
        private List<NewsArticle> svseNewsArticles = new List<NewsArticle>();

        // Announcements Data
        private List<AnnouncementAsCompany> announcements = new List<AnnouncementAsCompany>();
        private List<AnnouncementAsProfessor> ProfessorAnnouncements = new List<AnnouncementAsProfessor>();
        private List<AnnouncementAsResearchGroup> ResearchGroupAnnouncements = new List<AnnouncementAsResearchGroup>();

        // Create Announcement Form
        private AnnouncementAsResearchGroup researchGroupAnnouncement = new();
        private bool showErrorMessageforUploadingResearchGroupAnnouncement = false;
        private bool isFormValidToSaveResearchGroupAnnouncement = true;
        private bool isSaveResearchGroupAnnouncementSuccessful = false;
        private string saveResearchGroupAnnouncementMessage = string.Empty;
        private int remainingCharactersInResearchGroupAnnouncementField = 120;
        private int remainingCharactersInResearchGroupAnnouncementDescription = 1000;
        private string ResearchGroupAnnouncementAttachmentErrorMessage = string.Empty;

        // Uploaded Announcements Management
        private List<AnnouncementAsResearchGroup> UploadedResearchGroupAnnouncements { get; set; } = new();
        private List<AnnouncementAsResearchGroup> FilteredResearchGroupAnnouncements { get; set; } = new();
        private string selectedStatusFilterForResearchGroupAnnouncements = "Όλα";
        private int[] pageSizeOptions_SeeMyUploadedAnnouncementsAsResearchGroup = new[] { 10, 50, 100 };
        private int pageSizeForResearchGroupAnnouncements = 10;
        private int currentPageForResearchGroupAnnouncements = 1;
        private int totalCountResearchGroupAnnouncements, publishedCountResearchGroupAnnouncements, unpublishedCountResearchGroupAnnouncements;

        // Edit Modal
        private AnnouncementAsResearchGroup currentResearchGroupAnnouncement = new();
        private bool isResearchGroupEditModalVisible = false;
        private byte[]? researchGroupAnnouncementAttachmentFile;

        // Details Modal
        private AnnouncementAsResearchGroup? selectedResearchGroupAnnouncementToSeeDetails;

        // Bulk Edit
        private bool isBulkEditModeForResearchGroupAnnouncements = false;
        private HashSet<int> selectedResearchGroupAnnouncementIds = new HashSet<int>();
        private string bulkActionForResearchGroupAnnouncements = "";
        private bool showBulkActionModalForResearchGroupAnnouncements = false;
        private List<AnnouncementAsResearchGroup> selectedResearchGroupAnnouncementsForAction = new List<AnnouncementAsResearchGroup>();
        private string newStatusForBulkActionForResearchGroupAnnouncements = "Μη Δημοσιευμένη";

        // Menu
        private int activeResearchGroupAnnouncementMenuId = 0;

        // Pagination for viewing announcements
        private int pageSize = 3;
        private int expandedAnnouncementId = -1;
        private int expandedProfessorAnnouncementId = -1;
        private int expandedResearchGroupPublicAnnouncementId = 0;
        private int currentPageForCompanyAnnouncements = 1;
        private int currentPageForProfessorAnnouncements = 1;
        private int currentPageForResearchGroupPublicAnnouncements = 1;

        // News Article Class
        public class NewsArticle
        {
            public string Title { get; set; }
            public string Link { get; set; }
            public string Date { get; set; }
        }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            CurrentUserEmail = user.FindFirst("name")?.Value ?? "";

            await LoadAnnouncementsData();
            await FetchNewsArticles();
        }

        private async Task LoadAnnouncementsData()
        {
                announcements = await dbContext.AnnouncementsAsCompany
                .Where(a => a.CompanyAnnouncementStatus == "Δημοσιευμένη")
                .OrderByDescending(a => a.CompanyAnnouncementUploadDate)
                .ToListAsync();

                ProfessorAnnouncements = await dbContext.AnnouncementsAsProfessor
                .Where(a => a.ProfessorAnnouncementStatus == "Δημοσιευμένη")
                .OrderByDescending(a => a.ProfessorAnnouncementUploadDate)
                .ToListAsync();

            ResearchGroupAnnouncements = await dbContext.AnnouncementAsResearchGroup
                .Where(a => a.ResearchGroupAnnouncementStatus == "Δημοσιευμένη")
                .OrderByDescending(a => a.ResearchGroupAnnouncementUploadDate)
                .ToListAsync();
        }

        private async Task FetchNewsArticles()
        {
            try
            {
                // University News
                var universityUrl = "https://www.auth.gr/news/";
                var html = await Http.GetStringAsync(universityUrl);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var articles = doc.DocumentNode.SelectNodes("//article");
                if (articles != null)
                {
                    newsArticles = articles.Take(10).Select(article => new NewsArticle
                    {
                        Title = article.SelectSingleNode(".//h2/a")?.InnerText.Trim() ?? "",
                        Link = article.SelectSingleNode(".//h2/a")?.GetAttributeValue("href", "") ?? "",
                        Date = article.SelectSingleNode(".//time")?.InnerText.Trim() ?? ""
                    }).ToList();
                }

                // SVSE News
                var svseUrl = "https://www.svse.auth.gr/category/news/";
                html = await Http.GetStringAsync(svseUrl);
                doc.LoadHtml(html);

                articles = doc.DocumentNode.SelectNodes("//article");
                if (articles != null)
                {
                    svseNewsArticles = articles.Take(10).Select(article => new NewsArticle
                    {
                        Title = article.SelectSingleNode(".//h2/a")?.InnerText.Trim() ?? "",
                        Link = article.SelectSingleNode(".//h2/a")?.GetAttributeValue("href", "") ?? "",
                        Date = article.SelectSingleNode(".//time")?.InnerText.Trim() ?? ""
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching news: {ex.Message}");
            }
        }

        // Visibility Toggles
        private void ToggleFormVisibilityToShowGeneralAnnouncementsAndEventsAsRG()
        {
            isAnnouncementsFormVisibleToShowGeneralAnnouncementsAndEventsAsRG = !isAnnouncementsFormVisibleToShowGeneralAnnouncementsAndEventsAsRG;
            StateHasChanged();
        }

        private void ToggleFormVisibilityForUploadResearchGroupAnnouncements()
        {
            isResearchGroupToUploadAnnouncementsFormVisible = !isResearchGroupToUploadAnnouncementsFormVisible;
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

        // Description Toggles
        private void ToggleDescription(int announcementId)
        {
            expandedAnnouncementId = expandedAnnouncementId == announcementId ? -1 : announcementId;
            StateHasChanged();
        }

        private void ToggleDescriptionForProfessorAnnouncements(int announcementId)
        {
            expandedProfessorAnnouncementId = expandedProfessorAnnouncementId == announcementId ? -1 : announcementId;
            StateHasChanged();
        }

        private void ToggleDescriptionForResearchGroupPublicAnnouncements(int announcementId)
        {
            expandedResearchGroupPublicAnnouncementId = expandedResearchGroupPublicAnnouncementId == announcementId ? 0 : announcementId;
            StateHasChanged();
        }

        // Create Announcement Methods
        private void CheckCharacterLimitInResearchGroupAnnouncementField(ChangeEventArgs e)
        {
            var inputText = e.Value?.ToString() ?? string.Empty;
            remainingCharactersInResearchGroupAnnouncementField = 120 - inputText.Length;
        }

        private void CheckCharacterLimitInResearchGroupAnnouncementDescription(ChangeEventArgs e)
        {
            var inputText = e.Value?.ToString() ?? string.Empty;
            remainingCharactersInResearchGroupAnnouncementDescription = 1000 - inputText.Length;
        }

        private async Task HandleFileSelectedForResearchGroupAnnouncementAttachment(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file == null)
                {
                    researchGroupAnnouncement.ResearchGroupAnnouncementAttachmentFile = null;
                    ResearchGroupAnnouncementAttachmentErrorMessage = null;
                    return;
                }

                if (file.ContentType != "application/pdf")
                {
                    ResearchGroupAnnouncementAttachmentErrorMessage = "Λάθος τύπος αρχείου. Επιλέξτε αρχείο PDF.";
                    researchGroupAnnouncement.ResearchGroupAnnouncementAttachmentFile = null;
                    return;
                }

                const long maxFileSize = 10485760;
                if (file.Size > maxFileSize)
                {
                    ResearchGroupAnnouncementAttachmentErrorMessage = "Το αρχείο είναι πολύ μεγάλο. Μέγιστο μέγεθος: 10MB";
                    researchGroupAnnouncement.ResearchGroupAnnouncementAttachmentFile = null;
                    return;
                }

                using var ms = new MemoryStream();
                await file.OpenReadStream(maxFileSize).CopyToAsync(ms);
                researchGroupAnnouncement.ResearchGroupAnnouncementAttachmentFile = ms.ToArray();
                ResearchGroupAnnouncementAttachmentErrorMessage = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading attachment: {ex.Message}");
                ResearchGroupAnnouncementAttachmentErrorMessage = "Προέκυψε ένα σφάλμα κατά την μεταφόρτωση του αρχείου.";
                researchGroupAnnouncement.ResearchGroupAnnouncementAttachmentFile = null;
            }
        }

        private async Task SaveResearchGroupAnnouncementAsPublished()
        {
            isFormValidToSaveResearchGroupAnnouncement = ValidateMandatoryFieldsForUploadResearchGroupAnnouncement();
            if (!isFormValidToSaveResearchGroupAnnouncement) return;

            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] {
                $"Πρόκεται να Δημιουργήσετε μια Ανακοίνωση με Τίτλο: <strong>{researchGroupAnnouncement.ResearchGroupAnnouncementTitle}</strong> ως '<strong>Δημοσιευμένη</strong>'.<br><br>" +
                "<strong style='color: red;'>Είστε σίγουρος/η;</strong>"
            });

            if (!isConfirmed) return;

            researchGroupAnnouncement.ResearchGroupAnnouncementStatus = "Δημοσιευμένη";
            researchGroupAnnouncement.ResearchGroupAnnouncementUploadDate = DateTime.Now;
            researchGroupAnnouncement.ResearchGroupAnnouncementResearchGroupEmail = CurrentUserEmail;
            researchGroupAnnouncement.ResearchGroupAnnouncementRNG = new Random().NextInt64();
            researchGroupAnnouncement.ResearchGroupAnnouncementRNG_HashedAsUniqueID = HashingHelper.HashLong(researchGroupAnnouncement.ResearchGroupAnnouncementRNG ?? 0);
            await SaveResearchGroupAnnouncementToDatabase();
        }

        private async Task SaveResearchGroupAnnouncementAsUnpublished()
        {
            isFormValidToSaveResearchGroupAnnouncement = ValidateMandatoryFieldsForUploadResearchGroupAnnouncement();
            if (!isFormValidToSaveResearchGroupAnnouncement) return;

            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] {
                $"Πρόκεται να Δημιουργήσετε μια Ανακοίνωση με Τίτλο: <strong>{researchGroupAnnouncement.ResearchGroupAnnouncementTitle}</strong> ως '<strong>Μη Δημοσιευμένη (Προσωρινή Αποθήκευση)</strong>'.<br><br>" +
                "<strong style='color: red;'>Είστε σίγουρος/η;</strong>"
            });

            if (!isConfirmed) return;

            researchGroupAnnouncement.ResearchGroupAnnouncementStatus = "Μη Δημοσιευμένη";
            researchGroupAnnouncement.ResearchGroupAnnouncementUploadDate = DateTime.Now;
            researchGroupAnnouncement.ResearchGroupAnnouncementResearchGroupEmail = CurrentUserEmail;
            researchGroupAnnouncement.ResearchGroupAnnouncementRNG = new Random().NextInt64();
            researchGroupAnnouncement.ResearchGroupAnnouncementRNG_HashedAsUniqueID = HashingHelper.HashLong(researchGroupAnnouncement.ResearchGroupAnnouncementRNG ?? 0);
            await SaveResearchGroupAnnouncementToDatabase();
        }

        private async Task SaveResearchGroupAnnouncementToDatabase()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(researchGroupAnnouncement.ResearchGroupAnnouncementTitle))
                {
                    showErrorMessageforUploadingResearchGroupAnnouncement = true;
                    await JS.InvokeVoidAsync("scrollToElementById", "researchGroupAnnouncementTitle");
                    return;
                }

                if (string.IsNullOrWhiteSpace(researchGroupAnnouncement.ResearchGroupAnnouncementDescription))
                {
                    showErrorMessageforUploadingResearchGroupAnnouncement = true;
                    await JS.InvokeVoidAsync("scrollToElementById", "ResearchGroupAnnouncementDescription");
                    return;
                }

                if (researchGroupAnnouncement.ResearchGroupAnnouncementTimeToBeActive.Date <= DateTime.Today)
                {
                    showErrorMessageforUploadingResearchGroupAnnouncement = true;
                    await JS.InvokeVoidAsync("scrollToElementById", "ResearchGroupAnnouncementTimeToBeActive");
                    return;
                }

                dbContext.AnnouncementAsResearchGroup.Add(researchGroupAnnouncement);
                await dbContext.SaveChangesAsync();

                isSaveResearchGroupAnnouncementSuccessful = true;
                saveResearchGroupAnnouncementMessage = "Η Ανακοίνωση Δημιουργήθηκε Επιτυχώς";

                researchGroupAnnouncement = new AnnouncementAsResearchGroup();
                remainingCharactersInResearchGroupAnnouncementField = 120;
                remainingCharactersInResearchGroupAnnouncementDescription = 1000;
                UploadedResearchGroupAnnouncements = await GetUploadedResearchGroupAnnouncements();
                FilterResearchGroupAnnouncements();
            }
            catch (Exception ex)
            {
                isSaveResearchGroupAnnouncementSuccessful = false;
                saveResearchGroupAnnouncementMessage = "Κάποιο πρόβλημα παρουσιάστηκε με την Δημιουργία της Ανακοίνωσης!";
                Console.WriteLine($"Error saving announcement: {ex.Message}");
            }
            StateHasChanged();
        }

        private bool ValidateMandatoryFieldsForUploadResearchGroupAnnouncement()
        {
            if (string.IsNullOrWhiteSpace(researchGroupAnnouncement.ResearchGroupAnnouncementTitle) ||
                string.IsNullOrWhiteSpace(researchGroupAnnouncement.ResearchGroupAnnouncementDescription) ||
                researchGroupAnnouncement.ResearchGroupAnnouncementTimeToBeActive.Date <= DateTime.Today)
            {
                showErrorMessageforUploadingResearchGroupAnnouncement = true;
                return false;
            }
            showErrorMessageforUploadingResearchGroupAnnouncement = false;
            return true;
        }

        // Uploaded Announcements Management
        private async Task ToggleUploadedResearchGroupAnnouncementsVisibility()
        {
            isUploadedResearchGroupAnnouncementsVisible = !isUploadedResearchGroupAnnouncementsVisible;
            if (isUploadedResearchGroupAnnouncementsVisible)
            {
                await LoadUploadedResearchGroupAnnouncementsAsync();
            }
            StateHasChanged();
        }

        private async Task LoadUploadedResearchGroupAnnouncementsAsync()
        {
            UploadedResearchGroupAnnouncements = await GetUploadedResearchGroupAnnouncements();
            FilterResearchGroupAnnouncements();
        }

        private async Task<List<AnnouncementAsResearchGroup>> GetUploadedResearchGroupAnnouncements()
        {
            return await dbContext.AnnouncementAsResearchGroup
                .Include(a => a.ResearchGroup)
                .Where(a => a.ResearchGroupAnnouncementResearchGroupEmail == CurrentUserEmail)
                .OrderByDescending(a => a.ResearchGroupAnnouncementUploadDate)
                .ToListAsync();
        }

        private void HandleResearchGroupStatusFilterChange(ChangeEventArgs e)
        {
            selectedStatusFilterForResearchGroupAnnouncements = e.Value.ToString();
            FilterResearchGroupAnnouncements();
        }

        private void FilterResearchGroupAnnouncements()
        {
            if (UploadedResearchGroupAnnouncements == null) return;

            FilteredResearchGroupAnnouncements = selectedStatusFilterForResearchGroupAnnouncements switch
            {
                "Δημοσιευμένη" => UploadedResearchGroupAnnouncements.Where(a => a.ResearchGroupAnnouncementStatus == "Δημοσιευμένη").ToList(),
                "Μη Δημοσιευμένη" => UploadedResearchGroupAnnouncements.Where(a => a.ResearchGroupAnnouncementStatus == "Μη Δημοσιευμένη").ToList(),
                _ => UploadedResearchGroupAnnouncements.ToList()
            };

            totalCountResearchGroupAnnouncements = UploadedResearchGroupAnnouncements.Count;
            publishedCountResearchGroupAnnouncements = UploadedResearchGroupAnnouncements.Count(a => a.ResearchGroupAnnouncementStatus == "Δημοσιευμένη");
            unpublishedCountResearchGroupAnnouncements = UploadedResearchGroupAnnouncements.Count(a => a.ResearchGroupAnnouncementStatus == "Μη Δημοσιευμένη");
            currentPageForResearchGroupAnnouncements = 1;
            StateHasChanged();
        }

        // Pagination for Uploaded Announcements
        private int totalPagesForResearchGroupAnnouncements => (int)Math.Ceiling((double)(FilteredResearchGroupAnnouncements?.Count ?? 0) / pageSizeForResearchGroupAnnouncements);

        private IEnumerable<AnnouncementAsResearchGroup> GetPaginatedResearchGroupAnnouncements()
        {
            return FilteredResearchGroupAnnouncements?
                .Skip((currentPageForResearchGroupAnnouncements - 1) * pageSizeForResearchGroupAnnouncements)
                .Take(pageSizeForResearchGroupAnnouncements) ?? Enumerable.Empty<AnnouncementAsResearchGroup>();
        }

        private void GoToFirstPageForResearchGroupAnnouncements() => ChangePageForResearchGroupAnnouncements(1);
        private void PreviousPageForResearchGroupAnnouncements() => ChangePageForResearchGroupAnnouncements(currentPageForResearchGroupAnnouncements - 1);
        private void NextPageForResearchGroupAnnouncements() => ChangePageForResearchGroupAnnouncements(currentPageForResearchGroupAnnouncements + 1);
        private void GoToLastPageForResearchGroupAnnouncements() => ChangePageForResearchGroupAnnouncements(totalPagesForResearchGroupAnnouncements);
        private void GoToPageForResearchGroupAnnouncements(int pageNum) => ChangePageForResearchGroupAnnouncements(pageNum);

        private void ChangePageForResearchGroupAnnouncements(int newPage)
        {
            if (newPage > 0 && newPage <= totalPagesForResearchGroupAnnouncements)
            {
                currentPageForResearchGroupAnnouncements = newPage;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePagesForResearchGroupAnnouncements()
        {
            var pages = new List<int>();
            int current = currentPageForResearchGroupAnnouncements;
            int total = totalPagesForResearchGroupAnnouncements;
            if (total == 0) return pages;
            pages.Add(1);
            if (current > 3) pages.Add(-1);
            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);
            for (int i = start; i <= end; i++) pages.Add(i);
            if (current < total - 2) pages.Add(-1);
            if (total > 1) pages.Add(total);
            return pages;
        }

        private void OnPageSizeChange_SeeMyUploadedAnnouncementsAsResearchGroup(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize))
            {
                pageSizeForResearchGroupAnnouncements = newSize;
                currentPageForResearchGroupAnnouncements = 1;
                StateHasChanged();
            }
        }

        // CRUD Operations
        private async Task DeleteResearchGroupAnnouncement(int announcementId)
        {
            var isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML",
                "Πρόκειται να διαγράψετε οριστικά αυτή την Ανακοίνωση.<br><br><strong style='color: red;'>Είστε σίγουρος/η;</strong>");

            if (isConfirmed)
            {
                var announcement = await dbContext.AnnouncementAsResearchGroup.FindAsync(announcementId);
                if (announcement != null)
                {
                    dbContext.AnnouncementAsResearchGroup.Remove(announcement);
                    await dbContext.SaveChangesAsync();
                    UploadedResearchGroupAnnouncements = await GetUploadedResearchGroupAnnouncements();
                    FilterResearchGroupAnnouncements();
                }
                StateHasChanged();
            }
        }

        private async Task ChangeResearchGroupAnnouncementStatus(int announcementId, string newStatus)
        {
            var isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML",
                $"Πρόκειται να αλλάξετε την κατάσταση αυτής της Ανακοίνωσης σε <strong>{newStatus}</strong>.<br><br><strong style='color: red;'>Είστε σίγουρος/η;</strong>");

            if (isConfirmed)
            {
                var announcement = UploadedResearchGroupAnnouncements.FirstOrDefault(a => a.Id == announcementId);
                if (announcement != null)
                {
                    announcement.ResearchGroupAnnouncementStatus = newStatus;
                    await dbContext.SaveChangesAsync();
                    UploadedResearchGroupAnnouncements = await GetUploadedResearchGroupAnnouncements();
                    FilterResearchGroupAnnouncements();
                }
                StateHasChanged();
            }
        }

        // Edit Modal
        private void OpenResearchGroupEditModal(AnnouncementAsResearchGroup announcement)
        {
            currentResearchGroupAnnouncement = new AnnouncementAsResearchGroup
            {
                Id = announcement.Id,
                ResearchGroupAnnouncementTitle = announcement.ResearchGroupAnnouncementTitle,
                ResearchGroupAnnouncementDescription = announcement.ResearchGroupAnnouncementDescription,
                ResearchGroupAnnouncementUploadDate = announcement.ResearchGroupAnnouncementUploadDate,
                ResearchGroupAnnouncementResearchGroupEmail = announcement.ResearchGroupAnnouncementResearchGroupEmail,
                ResearchGroupAnnouncementTimeToBeActive = announcement.ResearchGroupAnnouncementTimeToBeActive,
                ResearchGroupAnnouncementAttachmentFile = announcement.ResearchGroupAnnouncementAttachmentFile
            };
            isResearchGroupEditModalVisible = true;
        }

        private void CloseResearchGroupEditModal()
        {
            isResearchGroupEditModalVisible = false;
            currentResearchGroupAnnouncement = new AnnouncementAsResearchGroup();
        }

        private async Task UpdateResearchGroupAnnouncement(AnnouncementAsResearchGroup announcement)
        {
            var existingAnnouncement = await dbContext.AnnouncementAsResearchGroup.FindAsync(announcement.Id);
            if (existingAnnouncement != null)
            {
                existingAnnouncement.ResearchGroupAnnouncementTitle = announcement.ResearchGroupAnnouncementTitle;
                existingAnnouncement.ResearchGroupAnnouncementDescription = announcement.ResearchGroupAnnouncementDescription;
                existingAnnouncement.ResearchGroupAnnouncementTimeToBeActive = announcement.ResearchGroupAnnouncementTimeToBeActive;
                if (researchGroupAnnouncementAttachmentFile != null)
                {
                    existingAnnouncement.ResearchGroupAnnouncementAttachmentFile = researchGroupAnnouncementAttachmentFile;
                }
                await dbContext.SaveChangesAsync();
                UploadedResearchGroupAnnouncements = await GetUploadedResearchGroupAnnouncements();
                FilterResearchGroupAnnouncements();
                isResearchGroupEditModalVisible = false;
                StateHasChanged();
            }
        }

        private async Task HandleFileUploadToEditResearchGroupAnnouncementAttachment(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file != null)
            {
                using var memoryStream = new MemoryStream();
                await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(memoryStream);
                researchGroupAnnouncementAttachmentFile = memoryStream.ToArray();
            }
        }

        // Details Modal
        private void OpenResearchGroupAnnouncementDetailsModal(AnnouncementAsResearchGroup currentAnnouncement)
        {
            selectedResearchGroupAnnouncementToSeeDetails = currentAnnouncement;
        }

        private void CloseResearchGroupAnnouncementDetailsModal()
        {
            selectedResearchGroupAnnouncementToSeeDetails = null;
        }

        // Menu
        private void ToggleResearchGroupAnnouncementMenu(int announcementId)
        {
            activeResearchGroupAnnouncementMenuId = activeResearchGroupAnnouncementMenuId == announcementId ? 0 : announcementId;
            StateHasChanged();
        }

        // Bulk Edit
        private void EnableBulkEditModeForResearchGroupAnnouncements()
        {
            isBulkEditModeForResearchGroupAnnouncements = true;
            selectedResearchGroupAnnouncementIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForResearchGroupAnnouncements()
        {
            isBulkEditModeForResearchGroupAnnouncements = false;
            selectedResearchGroupAnnouncementIds.Clear();
            StateHasChanged();
        }

        private void ToggleResearchGroupAnnouncementSelection(int announcementId, ChangeEventArgs e)
        {
            var isChecked = (bool)(e.Value ?? false);
            if (isChecked) selectedResearchGroupAnnouncementIds.Add(announcementId);
            else selectedResearchGroupAnnouncementIds.Remove(announcementId);
            StateHasChanged();
        }

        private async Task ExecuteBulkCopyForResearchGroupAnnouncements()
        {
            if (selectedResearchGroupAnnouncementIds.Count == 0) return;

            var isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML",
                $"Πρόκειται να αντιγράψετε {selectedResearchGroupAnnouncementIds.Count} ανακοινώσεις.<br><br><strong style='color: red;'>Είστε σίγουρος/η;</strong>");

            if (!isConfirmed) return;

            foreach (var id in selectedResearchGroupAnnouncementIds)
            {
                var original = FilteredResearchGroupAnnouncements.FirstOrDefault(a => a.Id == id);
                if (original != null)
                {
                    var copy = new AnnouncementAsResearchGroup
                    {
                        ResearchGroupAnnouncementTitle = original.ResearchGroupAnnouncementTitle + " (Αντίγραφο)",
                        ResearchGroupAnnouncementDescription = original.ResearchGroupAnnouncementDescription,
                        ResearchGroupAnnouncementStatus = "Μη Δημοσιευμένη",
                        ResearchGroupAnnouncementUploadDate = DateTime.Now,
                        ResearchGroupAnnouncementResearchGroupEmail = CurrentUserEmail,
                        ResearchGroupAnnouncementTimeToBeActive = original.ResearchGroupAnnouncementTimeToBeActive,
                        ResearchGroupAnnouncementAttachmentFile = original.ResearchGroupAnnouncementAttachmentFile,
                        ResearchGroupAnnouncementRNG = new Random().NextInt64(),
                        ResearchGroupAnnouncementRNG_HashedAsUniqueID = HashingHelper.HashLong(new Random().NextInt64())
                    };
                    dbContext.AnnouncementAsResearchGroup.Add(copy);
                }
            }

            await dbContext.SaveChangesAsync();
            UploadedResearchGroupAnnouncements = await GetUploadedResearchGroupAnnouncements();
            FilterResearchGroupAnnouncements();
            CancelBulkEditForResearchGroupAnnouncements();
        }

        // Pagination for viewing Company Announcements
        private int totalPagesForCompanyAnnouncements => Math.Max(1, (int)Math.Ceiling((double)(announcements?.Where(a => a.CompanyAnnouncementStatus == "Δημοσιευμένη").Count() ?? 0) / pageSize));

        private void GoToFirstPageForCompanyAnnouncements() { currentPageForCompanyAnnouncements = 1; }
        private void PreviousPageForCompanyAnnouncements() { if (currentPageForCompanyAnnouncements > 1) currentPageForCompanyAnnouncements--; }
        private void NextPageForCompanyAnnouncements() { if (currentPageForCompanyAnnouncements < totalPagesForCompanyAnnouncements) currentPageForCompanyAnnouncements++; }
        private void GoToLastPageForCompanyAnnouncements() { currentPageForCompanyAnnouncements = totalPagesForCompanyAnnouncements; }
        private void GoToPageForCompanyAnnouncements(int pageNumber) { if (pageNumber >= 1 && pageNumber <= totalPagesForCompanyAnnouncements) currentPageForCompanyAnnouncements = pageNumber; }

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

        // Pagination for viewing Professor Announcements
        private int totalPagesForProfessorAnnouncements => Math.Max(1, (int)Math.Ceiling((double)(ProfessorAnnouncements?.Where(a => a.ProfessorAnnouncementStatus == "Δημοσιευμένη").Count() ?? 0) / pageSize));

        private void GoToFirstPageForProfessorAnnouncements() { currentPageForProfessorAnnouncements = 1; }
        private void PreviousPageForProfessorAnnouncements() { if (currentPageForProfessorAnnouncements > 1) currentPageForProfessorAnnouncements--; }
        private void NextPageForProfessorAnnouncements() { if (currentPageForProfessorAnnouncements < totalPagesForProfessorAnnouncements) currentPageForProfessorAnnouncements++; }
        private void GoToLastPageForProfessorAnnouncements() { currentPageForProfessorAnnouncements = totalPagesForProfessorAnnouncements; }
        private void GoToPageForProfessorAnnouncements(int pageNumber) { if (pageNumber >= 1 && pageNumber <= totalPagesForProfessorAnnouncements) currentPageForProfessorAnnouncements = pageNumber; }

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

        // Pagination for viewing Research Group Public Announcements
        private int totalPagesForResearchGroupPublicAnnouncements => Math.Max(1, (int)Math.Ceiling((ResearchGroupAnnouncements?.Where(a => a.ResearchGroupAnnouncementStatus == "Δημοσιευμένη").Count() ?? 0) / (double)pageSize));

        private void GoToFirstPageForResearchGroupPublicAnnouncements() { currentPageForResearchGroupPublicAnnouncements = 1; }
        private void PreviousPageForResearchGroupPublicAnnouncements() { if (currentPageForResearchGroupPublicAnnouncements > 1) currentPageForResearchGroupPublicAnnouncements--; }
        private void NextPageForResearchGroupPublicAnnouncements() { if (currentPageForResearchGroupPublicAnnouncements < totalPagesForResearchGroupPublicAnnouncements) currentPageForResearchGroupPublicAnnouncements++; }
        private void GoToLastPageForResearchGroupPublicAnnouncements() { currentPageForResearchGroupPublicAnnouncements = totalPagesForResearchGroupPublicAnnouncements; }
        private void GoToPageForResearchGroupPublicAnnouncements(int pageNumber) { currentPageForResearchGroupPublicAnnouncements = pageNumber; }

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

        // Download Methods
        private async Task DownloadAnnouncementAttachmentFrontPage(byte[] attachmentData, string fileName)
        {
            if (attachmentData != null && attachmentData.Length > 0)
            {
                await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile", fileName, "application/pdf", attachmentData);
            }
        }

        private async Task DownloadProfessorAnnouncementAttachmentFrontPage(byte[] attachmentData, string fileName)
        {
            await DownloadAnnouncementAttachmentFrontPage(attachmentData, fileName);
        }

        private async Task DownloadResearchGroupPublicAnnouncementAttachmentFrontPage(byte[] attachmentData, string fileName)
        {
            await DownloadAnnouncementAttachmentFrontPage(attachmentData, fileName);
        }
    }
}

