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

namespace SplitIt.Shared.Company
{
    public partial class CompanyAnnouncementsSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        // User Information
        private string CurrentUserEmail = "";

        // Visibility & Loading
        private bool isLoadingUploadedAnnouncements = false;
        private bool isUploadedAnnouncementsVisible = false;

        // Filtering
        private string selectedStatusFilterForAnnouncements = "Όλα";

        // Data
        private List<AnnouncementAsCompany> UploadedAnnouncements = new List<AnnouncementAsCompany>();
        private List<AnnouncementAsCompany> FilteredAnnouncements = new List<AnnouncementAsCompany>();

        // Pagination
        private int currentPageForAnnouncements = 1;
        private int pageSizeForAnnouncements = 10;
        private int[] pageSizeOptions_SeeMyUploadedAnnouncementsAsCompany = new[] { 10, 50, 100 };

        // Counts
        private int totalCountAnnouncements = 0;
        private int publishedCountAnnouncements = 0;
        private int unpublishedCountAnnouncements = 0;

        // Computed Properties
        private int totalPagesForAnnouncements =>
            (int)Math.Ceiling((double)(FilteredAnnouncements?.Count() ?? 0) / pageSizeForAnnouncements);

        // Bulk Operations
        private bool isBulkEditModeForAnnouncements = false;
        private HashSet<int> selectedAnnouncementIds = new HashSet<int>();
        private string bulkActionForAnnouncements = "";
        private string newStatusForBulkActionForAnnouncements = "Μη Δημοσιευμένη";
        private bool showBulkActionModalForAnnouncements = false;

        // Individual Operations
        private int activeAnnouncementMenuId = 0;
        private AnnouncementAsCompany? selectedCompanyAnnouncementToSeeDetailsAsCompany = null;
        private bool isEditModalVisible = false;
        private AnnouncementAsCompany currentAnnouncement = new AnnouncementAsCompany();
        private bool showLoadingModalForDeleteAnnouncement = false;
        private int loadingProgress = 0;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            CurrentUserEmail = user.FindFirst("name")?.Value ?? "";
        }

        // Visibility Toggle
        private async Task ToggleUploadedAnnouncementsVisibility()
        {
            isUploadedAnnouncementsVisible = !isUploadedAnnouncementsVisible;

            if (isUploadedAnnouncementsVisible)
            {
                isLoadingUploadedAnnouncements = true;
                StateHasChanged();

                try
                {
                    await LoadUploadedAnnouncementsAsync();
                    await ApplyFiltersAndUpdateCounts();
                }
                finally
                {
                    isLoadingUploadedAnnouncements = false;
                    StateHasChanged();
                }
            }
            else
            {
                StateHasChanged();
            }
        }

        // Data Loading
        private async Task LoadUploadedAnnouncementsAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentUserEmail))
                {
                    UploadedAnnouncements = await dbContext.AnnouncementsAsCompany
                        .Where(a => a.CompanyAnnouncementCompanyEmail == CurrentUserEmail)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Announcements: {ex.Message}");
            }
            finally
            {
                StateHasChanged();
            }
        }

        private async Task ApplyFiltersAndUpdateCounts()
        {
            if (UploadedAnnouncements == null)
            {
                UploadedAnnouncements = await GetUploadedAnnouncements();
            }

            // Apply status filter
            if (selectedStatusFilterForAnnouncements == "Όλα")
            {
                FilteredAnnouncements = UploadedAnnouncements.ToList();
            }
            else
            {
                FilteredAnnouncements = UploadedAnnouncements
                    .Where(a => a.CompanyAnnouncementStatus == selectedStatusFilterForAnnouncements)
                    .ToList();
            }

            // Update counts
            totalCountAnnouncements = UploadedAnnouncements.Count;
            publishedCountAnnouncements = UploadedAnnouncements.Count(a => a.CompanyAnnouncementStatus == "Δημοσιευμένη");
            unpublishedCountAnnouncements = UploadedAnnouncements.Count(a => a.CompanyAnnouncementStatus == "Μη Δημοσιευμένη");

            // Reset to first page after filtering
            currentPageForAnnouncements = 1;
            StateHasChanged();
        }

        private async Task<List<AnnouncementAsCompany>> GetUploadedAnnouncements()
        {
            return await dbContext.AnnouncementsAsCompany
                .Where(a => a.CompanyAnnouncementCompanyEmail == CurrentUserEmail)
                .ToListAsync();
        }

        // Filtering
        private async Task HandleStatusFilterChange(ChangeEventArgs e)
        {
            selectedStatusFilterForAnnouncements = e.Value?.ToString() ?? "Όλα";
            await ApplyFiltersAndUpdateCounts();
        }

        // Pagination
        private IEnumerable<AnnouncementAsCompany> GetPaginatedAnnouncements()
        {
            return FilteredAnnouncements?
                .Skip((currentPageForAnnouncements - 1) * pageSizeForAnnouncements)
                .Take(pageSizeForAnnouncements) ?? Enumerable.Empty<AnnouncementAsCompany>();
        }

        private void OnPageSizeChange_SeeMyUploadedAnnouncementsAsCompany(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var size) && size > 0)
            {
                pageSizeForAnnouncements = size;
                currentPageForAnnouncements = 1;
                StateHasChanged();
            }
        }

        private void GoToFirstPageForAnnouncements()
        {
            currentPageForAnnouncements = 1;
            StateHasChanged();
        }

        private void PreviousPageForAnnouncements()
        {
            if (currentPageForAnnouncements > 1)
            {
                currentPageForAnnouncements--;
                StateHasChanged();
            }
        }

        private void GoToPageForAnnouncements(int page)
        {
            if (page >= 1 && page <= totalPagesForAnnouncements)
            {
                currentPageForAnnouncements = page;
                StateHasChanged();
            }
        }

        private void NextPageForAnnouncements()
        {
            if (currentPageForAnnouncements < totalPagesForAnnouncements)
            {
                currentPageForAnnouncements++;
                StateHasChanged();
            }
        }

        private void GoToLastPageForAnnouncements()
        {
            currentPageForAnnouncements = totalPagesForAnnouncements;
            StateHasChanged();
        }

        private List<int> GetVisiblePagesForAnnouncements()
        {
            var pages = new List<int>();
            int current = currentPageForAnnouncements;
            int total = totalPagesForAnnouncements;

            pages.Add(1);
            if (current > 3)
                pages.Add(-1);

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);
            for (int i = start; i <= end; i++)
                pages.Add(i);

            if (current < total - 2)
                pages.Add(-1);

            if (total > 1)
                pages.Add(total);

            return pages;
        }

        // Bulk Operations
        private void EnableBulkEditModeForAnnouncements()
        {
            isBulkEditModeForAnnouncements = true;
            selectedAnnouncementIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForAnnouncements()
        {
            isBulkEditModeForAnnouncements = false;
            selectedAnnouncementIds.Clear();
            StateHasChanged();
        }

        private void ToggleAnnouncementSelection(int announcementId, ChangeEventArgs e)
        {
            var isChecked = (bool)(e.Value ?? false);
            if (isChecked)
            {
                selectedAnnouncementIds.Add(announcementId);
            }
            else
            {
                selectedAnnouncementIds.Remove(announcementId);
            }
            StateHasChanged();
        }

        private void ShowBulkActionOptionsForAnnouncements()
        {
            if (selectedAnnouncementIds.Count > 0)
            {
                showBulkActionModalForAnnouncements = true;
                StateHasChanged();
            }
        }

        private void CloseBulkActionModalForAnnouncements()
        {
            showBulkActionModalForAnnouncements = false;
            StateHasChanged();
        }

        private async Task ExecuteBulkStatusChange(string newStatus)
        {
            if (selectedAnnouncementIds.Count == 0) return;

            var confirmMessage = $"Θα αλλάξετε τη κατάσταση {selectedAnnouncementIds.Count} ανακοινώσεων σε '{newStatus}'. Είστε σίγουρος/η;";
            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", confirmMessage);

            if (isConfirmed)
            {
                try
                {
                    foreach (var id in selectedAnnouncementIds)
                    {
                        var announcement = await dbContext.AnnouncementsAsCompany.FindAsync(id);
                        if (announcement != null)
                        {
                            announcement.CompanyAnnouncementStatus = newStatus;
                        }
                    }
                    await dbContext.SaveChangesAsync();
                    await LoadUploadedAnnouncementsAsync();
                    await ApplyFiltersAndUpdateCounts();
                    CancelBulkEditForAnnouncements();
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating announcement statuses: {ex.Message}");
                    await JS.InvokeVoidAsync("alert", $"Σφάλμα κατά την ενημέρωση: {ex.Message}");
                }
            }
        }

        private async Task ExecuteBulkCopy()
        {
            if (selectedAnnouncementIds.Count == 0) return;

            var confirmMessage = $"Θα αντιγράψετε {selectedAnnouncementIds.Count} ανακοινώσεις. Είστε σίγουρος/η;";
            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", confirmMessage);

            if (isConfirmed)
            {
                try
                {
                    foreach (var id in selectedAnnouncementIds)
                    {
                        var original = await dbContext.AnnouncementsAsCompany.FindAsync(id);
                        if (original != null)
                        {
                            var copy = new AnnouncementAsCompany
                            {
                                CompanyAnnouncementRNG = new Random().NextInt64(),
                                CompanyAnnouncementRNG_HashedAsUniqueID = HashingHelper.HashLong(new Random().NextInt64()),
                                CompanyAnnouncementTitle = original.CompanyAnnouncementTitle + " (Αντίγραφο)",
                                CompanyAnnouncementDescription = original.CompanyAnnouncementDescription,
                                CompanyAnnouncementCompanyEmail = CurrentUserEmail,
                                CompanyAnnouncementStatus = "Μη Δημοσιευμένη",
                                CompanyAnnouncementUploadDate = DateTime.UtcNow,
                                CompanyAnnouncementTimeToBeActive = original.CompanyAnnouncementTimeToBeActive,
                                CompanyAnnouncementAttachmentFile = original.CompanyAnnouncementAttachmentFile
                            };
                            dbContext.AnnouncementsAsCompany.Add(copy);
                        }
                    }
                    await dbContext.SaveChangesAsync();
                    await LoadUploadedAnnouncementsAsync();
                    await ApplyFiltersAndUpdateCounts();
                    CancelBulkEditForAnnouncements();
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error copying announcements: {ex.Message}");
                    await JS.InvokeVoidAsync("alert", $"Σφάλμα κατά την αντιγραφή: {ex.Message}");
                }
            }
        }

        // Individual Operations
        private void ToggleAnnouncementMenu(int announcementId)
        {
            activeAnnouncementMenuId = activeAnnouncementMenuId == announcementId ? 0 : announcementId;
            StateHasChanged();
        }

        private void OpenCompanyAnnouncementDetailsModal(AnnouncementAsCompany announcement)
        {
            selectedCompanyAnnouncementToSeeDetailsAsCompany = announcement;
            StateHasChanged();
        }

        private void CloseCompanyAnnouncementDetailsModal()
        {
            selectedCompanyAnnouncementToSeeDetailsAsCompany = null;
            StateHasChanged();
        }

        private async Task DeleteAnnouncement(int announcementId)
        {
            var isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML",
                "Πρόκειται να διαγράψετε οριστικά αυτή την Ανακοίνωση.<br><br>" +
                "<strong style='color: red;'>Είστε σίγουρος/η;</strong>");

            if (isConfirmed)
            {
                showLoadingModalForDeleteAnnouncement = true;
                loadingProgress = 0;
                StateHasChanged();

                try
                {
                    await UpdateProgressWhenDeleteAnnouncementAsCompany(30);
                    var announcement = await dbContext.AnnouncementsAsCompany.FindAsync(announcementId);

                    if (announcement != null)
                    {
                        await UpdateProgressWhenDeleteAnnouncementAsCompany(60);
                        dbContext.AnnouncementsAsCompany.Remove(announcement);
                        await dbContext.SaveChangesAsync();
                        await UpdateProgressWhenDeleteAnnouncementAsCompany(80);

                        await UpdateProgressWhenDeleteAnnouncementAsCompany(90);
                        await LoadUploadedAnnouncementsAsync();
                        await ApplyFiltersAndUpdateCounts();

                        await UpdateProgressWhenDeleteAnnouncementAsCompany(100);
                        await Task.Delay(300);
                    }
                    else
                    {
                        showLoadingModalForDeleteAnnouncement = false;
                        await JS.InvokeVoidAsync("alert", "Η ανακοίνωση δεν βρέθηκε.");
                        StateHasChanged();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    showLoadingModalForDeleteAnnouncement = false;
                    await JS.InvokeVoidAsync("alert", $"Σφάλμα κατά τη διαγραφή: {ex.Message}");
                    StateHasChanged();
                    return;
                }
                finally
                {
                    showLoadingModalForDeleteAnnouncement = false;
                }

                StateHasChanged();
            }
        }

        private async Task UpdateProgressWhenDeleteAnnouncementAsCompany(int progress)
        {
            loadingProgress = progress;
            StateHasChanged();
            await Task.Delay(50);
        }

        private async Task ChangeAnnouncementStatus(int announcementId, string newStatus)
        {
            try
            {
                var announcement = await dbContext.AnnouncementsAsCompany.FindAsync(announcementId);
                if (announcement != null)
                {
                    announcement.CompanyAnnouncementStatus = newStatus;
                    await dbContext.SaveChangesAsync();
                    await LoadUploadedAnnouncementsAsync();
                    await ApplyFiltersAndUpdateCounts();
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing announcement status: {ex.Message}");
                await JS.InvokeVoidAsync("alert", $"Σφάλμα κατά την αλλαγή κατάστασης: {ex.Message}");
            }
        }

        private void OpenEditModal(AnnouncementAsCompany announcement)
        {
            currentAnnouncement = new AnnouncementAsCompany
            {
                Id = announcement.Id,
                CompanyAnnouncementTitle = announcement.CompanyAnnouncementTitle,
                CompanyAnnouncementDescription = announcement.CompanyAnnouncementDescription,
                CompanyAnnouncementUploadDate = announcement.CompanyAnnouncementUploadDate,
                CompanyAnnouncementCompanyEmail = announcement.CompanyAnnouncementCompanyEmail,
                CompanyAnnouncementTimeToBeActive = announcement.CompanyAnnouncementTimeToBeActive,
                CompanyAnnouncementAttachmentFile = announcement.CompanyAnnouncementAttachmentFile,
                CompanyAnnouncementStatus = announcement.CompanyAnnouncementStatus
            };
            isEditModalVisible = true;
            StateHasChanged();
        }

        private void CloseEditModal()
        {
            isEditModalVisible = false;
            currentAnnouncement = new AnnouncementAsCompany();
            StateHasChanged();
        }

        private async Task UpdateAnnouncement(AnnouncementAsCompany updatedAnnouncement)
        {
            try
            {
                var announcement = await dbContext.AnnouncementsAsCompany.FindAsync(updatedAnnouncement.Id);
                if (announcement != null)
                {
                    announcement.CompanyAnnouncementTitle = updatedAnnouncement.CompanyAnnouncementTitle;
                    announcement.CompanyAnnouncementDescription = updatedAnnouncement.CompanyAnnouncementDescription;
                    announcement.CompanyAnnouncementTimeToBeActive = updatedAnnouncement.CompanyAnnouncementTimeToBeActive;
                    if (updatedAnnouncement.CompanyAnnouncementAttachmentFile != null && updatedAnnouncement.CompanyAnnouncementAttachmentFile.Length > 0)
                    {
                        announcement.CompanyAnnouncementAttachmentFile = updatedAnnouncement.CompanyAnnouncementAttachmentFile;
                    }
                    await dbContext.SaveChangesAsync();
                    CloseEditModal();
                    await LoadUploadedAnnouncementsAsync();
                    await ApplyFiltersAndUpdateCounts();
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating announcement: {ex.Message}");
                await JS.InvokeVoidAsync("alert", $"Σφάλμα κατά την ενημέρωση: {ex.Message}");
            }
        }

        private async Task HandleFileUploadToEditCompanyAnnouncementAttachment(InputFileChangeEventArgs e)
        {
            if (currentAnnouncement == null || e.File == null)
            {
                return;
            }

            try
            {
                using var stream = e.File.OpenReadStream(5 * 1024 * 1024);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                currentAnnouncement.CompanyAnnouncementAttachmentFile = memoryStream.ToArray();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading announcement attachment: {ex.Message}");
            }
        }
    }
}

