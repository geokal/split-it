using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.ViewModels;
using QuizManager.Services;
using QuizManager.Services.CompanyDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.CompanySections
{
    public partial class CompanyInternshipsSection : ComponentBase
    {
        [Inject] private ICompanyDashboardService CompanyDashboardService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private InternshipEmailService InternshipEmailService { get; set; } = default!;

        // User Info
        private string CurrentUserEmail = "";
        private QuizManager.Models.Company companyData;

        // Form Visibility
        private bool isUploadCompanyInternshipsFormVisible = false;

        // Form Model and Validation
        private CompanyInternship companyInternship = new CompanyInternship();
        private bool showErrorMessage = false;
        private bool showLoadingModalForInternship = false;
        private int loadingProgress = 0;
        private bool showSuccessMessage = false;

        // Character Limits
        private int remainingCharactersInInternshipFieldUploadAsCompany = 120;
        private int remainingCharactersInInternshipDescriptionUploadAsCompany = 1000;

        // Areas/Subfields
        private List<Area> Areas = new List<Area>();
        private List<Area> SelectedAreasWhenUploadInternshipAsCompany = new List<Area>();
        private Dictionary<int, HashSet<string>> SelectedSubFieldsForCompanyInternship = new Dictionary<int, HashSet<string>>();
        private HashSet<int> ExpandedAreasForCompanyInternship = new HashSet<int>();
        private bool areCheckboxesVisibleForCompanyInternship = false;

        // Location Data
        private List<Region> Regions = new List<Region>();

        // Professor Selection
        private int selectedProfessorId = 0;
        private List<QuizManager.Models.Professor> professors = new List<QuizManager.Models.Professor>();

        // View Uploaded Internships
        private bool isShowActiveInternshipsAsCompanyFormVisible = false;
        private bool isLoadingInternshipsHistory = false;
        private string selectedStatusFilterForInternships = "Όλα";
        private List<CompanyInternship> UploadedInternships = new List<CompanyInternship>();
        private List<CompanyInternship> FilteredInternships = new List<CompanyInternship>();
        private int currentPageForCompanyInternships = 1;
        private int companyInternshipsPerPage = 10;
        private int[] pageSizeOptions_SeeMyUploadedInternshipsAsCompany = new[] { 10, 50, 100 };
        private int totalCount = 0;
        private int publishedCount = 0;
        private int unpublishedCount = 0;
        private int withdrawnCount = 0;

        // Bulk Operations
        private bool isBulkEditModeForInternships = false;
        private HashSet<int> selectedInternshipIds = new HashSet<int>();
        private bool showBulkActionModalForInternships = false;
        private string bulkActionForInternships = "";
        private string newStatusForBulkActionForInternships = "Μη Δημοσιευμένη";

        // Internship Menu
        private int activeInternshipMenuId = 0;
        private long? currentlyExpandedInternshipId = null;
        private bool showLoadingModalForDeleteInternship = false;

        // Internship Applicants
        private bool isLoadingInternshipApplicants = false;
        private long? loadingInternshipId = null;
        private Dictionary<long, List<InternshipApplied>> internshipApplicantsMap = new Dictionary<long, List<InternshipApplied>>();
        private Dictionary<long, int> acceptedApplicantsCountPerInternship_ForCompanyInternship = new Dictionary<long, int>();
        private Dictionary<long, int> availableSlotsPerInternship_ForCompanyInternship = new Dictionary<long, int>();

        // Bulk Operations for Applicants
        private bool isBulkEditModeForInternshipApplicants = false;
        private HashSet<(long, string)> selectedInternshipApplicantIds = new HashSet<(long, string)>();
        private string pendingBulkActionForInternshipApplicants = "";
        private bool sendEmailsForBulkAction = false;

        // Edit Internship
        private CompanyInternship selectedInternship;
        private CompanyInternship currentInternship;
        private bool isEditInternshipModalVisible = false;
        private HashSet<int> ExpandedAreasForEditCompanyInternship = new HashSet<int>();

        // Student Details Modal
        private QuizManager.Models.Student selectedStudentFromCache;
        
        // Additional missing properties
        private bool showCheckboxesForCompanyInternship = false;
        private bool isEditPopupVisibleForInternships = false;
        private bool isModalVisibleForInternships = false;
        private bool isModalVisibleToShowStudentDetailsAsCompanyFromTheirHyperlinkNameInCompanyInternships = false;
        private List<Area> SelectedAreasToEditForCompanyInternship = new List<Area>();
        private Dictionary<string, HashSet<string>> SelectedSubFieldsForEditCompanyInternship = new Dictionary<string, HashSet<string>>();

        // Computed Properties
        private int totalPagesForCompanyInternships => (int)Math.Ceiling((double)(FilteredInternships?.Count ?? 0) / companyInternshipsPerPage);

        protected override async Task OnInitializedAsync()
        {
            await LoadInitialData();
        }

        private async Task LoadInitialData()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity.IsAuthenticated)
            {
                CurrentUserEmail = user.FindFirst("name")?.Value ?? "";
                var dashboard = await CompanyDashboardService.LoadDashboardDataAsync();
                companyData = dashboard.CompanyProfile;
            }

            var lookups = await CompanyDashboardService.GetLookupsAsync();
            Areas = lookups.Areas.ToList();
            professors = lookups.Professors.ToList();
            Regions = lookups.RegionToTownsMap
                .Select(kvp => new Region { RegionName = kvp.Key, Towns = kvp.Value.Select(t => new Town { TownName = t }).ToList() })
                .ToList();
        }

        // Form Visibility
        private void ToggleFormVisibilityForUploadCompanyInternships()
        {
            isUploadCompanyInternshipsFormVisible = !isUploadCompanyInternshipsFormVisible;
            StateHasChanged();
        }

        // Character Limits
        private void CheckCharacterLimitInInternshipFieldUploadAsCompany(ChangeEventArgs e)
        {
            var text = e.Value?.ToString() ?? "";
            remainingCharactersInInternshipFieldUploadAsCompany = Math.Max(0, 120 - text.Length);
        }

        private void CheckCharacterLimitInInternshipDescriptionUploadAsCompany(ChangeEventArgs e)
        {
            var text = e.Value?.ToString() ?? "";
            remainingCharactersInInternshipDescriptionUploadAsCompany = Math.Max(0, 1000 - text.Length);
        }

        // Validation
        private bool IsValidPhoneNumber(string phone)
        {
            return !string.IsNullOrEmpty(phone) && phone.Length >= 10;
        }

        private void OnCompanyCreateInternshipPhoneNumberInput(ChangeEventArgs e)
        {
            // Validation handled in markup
        }

        // Location
        private IEnumerable<Town> GetTownsForRegion(string regionName)
        {
            var region = Regions.FirstOrDefault(r => r.RegionName == regionName);
            return region?.Towns ?? Enumerable.Empty<Town>();
        }

        // Transport Offer
        private void UpdateTransportOffer(bool offer)
        {
            companyInternship.CompanyInternshipTransportOffer = offer;
            StateHasChanged();
        }

        // Areas/Subfields
        private void ToggleCheckboxesForCompanyInternship()
        {
            areCheckboxesVisibleForCompanyInternship = !areCheckboxesVisibleForCompanyInternship;
            StateHasChanged();
        }

        private bool HasAnySelectionForCompanyInternship()
        {
            return SelectedAreasWhenUploadInternshipAsCompany.Any() ||
                   SelectedSubFieldsForCompanyInternship.Any(kvp => kvp.Value.Any());
        }

        private void OnAreaCheckedChangedForCompanyInternship(ChangeEventArgs e, Area area)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
            {
                if (!SelectedAreasWhenUploadInternshipAsCompany.Any(a => a.Id == area.Id))
                    SelectedAreasWhenUploadInternshipAsCompany.Add(area);
            }
            else
            {
                SelectedAreasWhenUploadInternshipAsCompany.RemoveAll(a => a.Id == area.Id);
                SelectedSubFieldsForCompanyInternship.Remove(area.Id);
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForCompanyInternship(Area area)
        {
            return SelectedAreasWhenUploadInternshipAsCompany.Any(a => a.Id == area.Id);
        }

        private void ToggleSubFieldsForCompanyInternship(Area area)
        {
            if (ExpandedAreasForCompanyInternship.Contains(area.Id))
                ExpandedAreasForCompanyInternship.Remove(area.Id);
            else
                ExpandedAreasForCompanyInternship.Add(area.Id);
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForCompanyInternship(ChangeEventArgs e, Area area, string subField)
        {
            bool isChecked = (bool)e.Value;
            if (!SelectedSubFieldsForCompanyInternship.ContainsKey(area.Id))
                SelectedSubFieldsForCompanyInternship[area.Id] = new HashSet<string>();

            if (isChecked)
                SelectedSubFieldsForCompanyInternship[area.Id].Add(subField);
            else
                SelectedSubFieldsForCompanyInternship[area.Id].Remove(subField);
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForCompanyInternship(Area area, string subField)
        {
            return SelectedSubFieldsForCompanyInternship.ContainsKey(area.Id) &&
                   SelectedSubFieldsForCompanyInternship[area.Id].Contains(subField);
        }

        // File Handling
        private async Task HandleFileSelectedForUploadInternshipAsCompany(InputFileChangeEventArgs e)
        {
            try
            {
                if (e.File == null)
                {
                    companyInternship.CompanyInternshipAttachment = null;
                    return;
                }

                const long maxFileSize = 10485760; // 10MB
                if (e.File.Size > maxFileSize) return;

                using var memoryStream = new System.IO.MemoryStream();
                await e.File.OpenReadStream(maxFileSize).CopyToAsync(memoryStream);
                companyInternship.CompanyInternshipAttachment = memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
            }
        }

        // Save Operations
        private async Task HandleSaveClickToSaveInternshipsAsCompany()
        {
            await SaveInternshipAsCompany(false);
        }

        private async Task HandlePublishClickToSaveInternshipsAsCompany()
        {
            await SaveInternshipAsCompany(true);
        }

        private async Task SaveInternshipAsCompany(bool publishInternship)
        {
            showLoadingModalForInternship = true;
            loadingProgress = 0;
            showErrorMessage = false;
            showSuccessMessage = false;
            StateHasChanged();

            try
            {
                await UpdateProgress(30);

                // Build areas string
                var areasWithSubfields = new List<string>();
                foreach (var area in SelectedAreasWhenUploadInternshipAsCompany)
                    areasWithSubfields.Add(area.AreaName);
                foreach (var areaSubFields in SelectedSubFieldsForCompanyInternship)
                    areasWithSubfields.AddRange(areaSubFields.Value);
                companyInternship.CompanyInternshipAreas = string.Join(",", areasWithSubfields);

                await UpdateProgress(50);

                // Set internship properties
                companyInternship.RNGForInternshipUploadedAsCompany = new Random().NextInt64();
                companyInternship.RNGForInternshipUploadedAsCompany_HashedAsUniqueID = HashingHelper.HashLong(companyInternship.RNGForInternshipUploadedAsCompany);
                companyInternship.CompanyEmailUsedToUploadInternship = CurrentUserEmail;
                companyInternship.CompanyInternshipUploadDate = DateTime.Now;
                companyInternship.CompanyUploadedInternshipStatus = publishInternship ? "Δημοσιευμένη" : "Μη Δημοσιευμένη";

                await UpdateProgress(70);
                var result = await CompanyDashboardService.CreateOrUpdateInternshipAsync(companyInternship);
                if (!result.Success)
                {
                    throw new InvalidOperationException(result.Error ?? "Failed to save internship.");
                }

                await UpdateProgress(90);
                showSuccessMessage = true;

                companyInternship = new CompanyInternship();
                SelectedAreasWhenUploadInternshipAsCompany.Clear();
                SelectedSubFieldsForCompanyInternship.Clear();
                ExpandedAreasForCompanyInternship.Clear();

                await UpdateProgress(100);
                await Task.Delay(500);
                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            catch (Exception ex)
            {
                showLoadingModalForInternship = false;
                showErrorMessage = true;
                Console.WriteLine($"Error uploading internship: {ex.Message}");
                StateHasChanged();
            }
        }

        private async Task UpdateProgress(int progress)
        {
            loadingProgress = progress;
            StateHasChanged();
            await Task.Delay(50);
        }

        // View Uploaded Internships
        private async Task ToggleFormVisibilityToShowMyActiveInternshipsAsCompany()
        {
            isShowActiveInternshipsAsCompanyFormVisible = !isShowActiveInternshipsAsCompanyFormVisible;
            if (isShowActiveInternshipsAsCompanyFormVisible)
            {
                isLoadingInternshipsHistory = true;
                StateHasChanged();
                try
                {
                    await LoadUploadedInternshipsAsync();
                    await ApplyFiltersAndUpdateCounts();
                }
                finally
                {
                    isLoadingInternshipsHistory = false;
                    StateHasChanged();
                }
            }
        }

        private async Task LoadUploadedInternshipsAsync()
        {
            var data = await CompanyDashboardService.LoadDashboardDataAsync();
            UploadedInternships = data.Internships
                .Where(i => i.CompanyEmailUsedToUploadInternship == CurrentUserEmail)
                .OrderByDescending(i => i.CompanyInternshipUploadDate)
                .ToList();
        }

        private async Task ApplyFiltersAndUpdateCounts()
        {
            FilteredInternships = selectedStatusFilterForInternships == "Όλα"
                ? UploadedInternships.ToList()
                : UploadedInternships.Where(i => i.CompanyUploadedInternshipStatus == selectedStatusFilterForInternships).ToList();

            totalCount = UploadedInternships.Count;
            publishedCount = UploadedInternships.Count(i => i.CompanyUploadedInternshipStatus == "Δημοσιευμένη");
            unpublishedCount = UploadedInternships.Count(i => i.CompanyUploadedInternshipStatus == "Μη Δημοσιευμένη");
            withdrawnCount = UploadedInternships.Count(i => i.CompanyUploadedInternshipStatus == "Ανακληθείσα");
            currentPageForCompanyInternships = 1;
        }

        private void HandleStatusFilterChange(ChangeEventArgs e)
        {
            selectedStatusFilterForInternships = e.Value?.ToString() ?? "Όλα";
            _ = ApplyFiltersAndUpdateCounts();
        }

        private void OnPageSizeChange_SeeMyUploadedInternshipsAsCompany(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var size) && size > 0)
            {
                companyInternshipsPerPage = size;
                currentPageForCompanyInternships = 1;
                StateHasChanged();
            }
        }

        private IEnumerable<CompanyInternship> GetPaginatedCompanyInternships()
        {
            return FilteredInternships?
                .Skip((currentPageForCompanyInternships - 1) * companyInternshipsPerPage)
                .Take(companyInternshipsPerPage)
                ?? Enumerable.Empty<CompanyInternship>();
        }

        // Pagination
        private void GoToFirstPageForCompanyInternships() => ChangePage(1);
        private void PreviousPageForCompanyInternships() => ChangePage(Math.Max(1, currentPageForCompanyInternships - 1));
        private void NextPageForCompanyInternships() => ChangePage(Math.Min(totalPagesForCompanyInternships, currentPageForCompanyInternships + 1));
        private void GoToLastPageForCompanyInternships() => ChangePage(totalPagesForCompanyInternships);
        private void GoToPageForCompanyInternships(int page) => ChangePage(page);

        private void ChangePage(int newPage)
        {
            if (newPage > 0 && newPage <= totalPagesForCompanyInternships)
            {
                currentPageForCompanyInternships = newPage;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePagesForCompanyInternships()
        {
            var pages = new List<int>();
            int current = currentPageForCompanyInternships;
            int total = totalPagesForCompanyInternships;
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

        // Bulk Operations
        private void EnableBulkEditModeForInternships()
        {
            isBulkEditModeForInternships = true;
            selectedInternshipIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForInternships()
        {
            isBulkEditModeForInternships = false;
            selectedInternshipIds.Clear();
            StateHasChanged();
        }

        private void ToggleInternshipSelection(int internshipId, ChangeEventArgs e)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked) selectedInternshipIds.Add(internshipId);
            else selectedInternshipIds.Remove(internshipId);
            StateHasChanged();
        }

        private void ShowBulkActionOptions()
        {
            if (selectedInternshipIds.Any())
                showBulkActionModalForInternships = true;
        }

        private void CloseBulkActionModalForInternships()
        {
            showBulkActionModalForInternships = false;
            bulkActionForInternships = "";
        }

        private async Task ExecuteBulkStatusChangeForInternships(string newStatus)
        {
            foreach (var id in selectedInternshipIds)
            {
                await CompanyDashboardService.UpdateInternshipStatusAsync(id, newStatus);
            }
            CancelBulkEditForInternships();
            await LoadUploadedInternshipsAsync();
            await ApplyFiltersAndUpdateCounts();
        }

        private async Task ExecuteBulkCopyForInternships()
        {
            var internshipsToCopy = UploadedInternships
                .Where(i => selectedInternshipIds.Contains(i.Id))
                .ToList();

            foreach (var i in internshipsToCopy)
            {
                var copy = new CompanyInternship
                {
                    CompanyInternshipTitle = i.CompanyInternshipTitle + " (Αντίγραφο)",
                    CompanyInternshipDescription = i.CompanyInternshipDescription,
                    CompanyInternshipAreas = i.CompanyInternshipAreas,
                    CompanyUploadedInternshipStatus = "Μη Δημοσιευμένη",
                    CompanyEmailUsedToUploadInternship = CurrentUserEmail,
                    CompanyInternshipUploadDate = DateTime.Now,
                    RNGForInternshipUploadedAsCompany = new Random().NextInt64()
                };
                copy.RNGForInternshipUploadedAsCompany_HashedAsUniqueID = HashingHelper.HashLong(copy.RNGForInternshipUploadedAsCompany);
                await CompanyDashboardService.CreateOrUpdateInternshipAsync(copy);
            }

            CancelBulkEditForInternships();
            await LoadUploadedInternshipsAsync();
            await ApplyFiltersAndUpdateCounts();
        }

        private async Task ExecuteBulkActionForInternships()
        {
            if (string.IsNullOrEmpty(bulkActionForInternships) || !selectedInternshipIds.Any()) return;

            if (bulkActionForInternships == "Αλλαγή Κατάστασης")
                await ExecuteBulkStatusChangeForInternships(newStatusForBulkActionForInternships);
            else if (bulkActionForInternships == "Αντιγραφή")
                await ExecuteBulkCopyForInternships();
            else if (bulkActionForInternships == "Διαγραφή")
            {
                foreach (var id in selectedInternshipIds)
                {
                    await CompanyDashboardService.DeleteInternshipAsync(id);
                }
                CancelBulkEditForInternships();
                await LoadUploadedInternshipsAsync();
                await ApplyFiltersAndUpdateCounts();
            }

            CloseBulkActionModalForInternships();
        }

        // Internship Menu & Operations
        private void ToggleInternshipMenu(int internshipId)
        {
            activeInternshipMenuId = activeInternshipMenuId == internshipId ? 0 : internshipId;
            StateHasChanged();
        }

        private async Task DeleteInternship(int internshipId)
        {
            showLoadingModalForDeleteInternship = true;
            StateHasChanged();

            try
            {
                await CompanyDashboardService.DeleteInternshipAsync(internshipId);
                await LoadUploadedInternshipsAsync();
                await ApplyFiltersAndUpdateCounts();
            }
            finally
            {
                showLoadingModalForDeleteInternship = false;
                StateHasChanged();
            }
        }

        private void ShowInternshipDetails(CompanyInternship internship)
        {
            selectedInternship = internship;
            StateHasChanged();
        }

        private async Task DownloadAttachmentForCompanyInternships(int internshipId)
        {
            var attachment = await CompanyDashboardService.GetInternshipAttachmentAsync(internshipId);
            if (attachment != null && attachment.Data.Length > 0)
            {
                await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile",
                    attachment.FileName ?? $"internship-attachment-{internshipId}.pdf",
                    attachment.ContentType ?? "application/pdf",
                    attachment.Data);
            }
        }

        private void ToggleInternshipExpanded(long internshipRng)
        {
            currentlyExpandedInternshipId = currentlyExpandedInternshipId == internshipRng ? null : internshipRng;
            StateHasChanged();
        }

        private void EditInternshipDetails(CompanyInternship internship)
        {
            currentInternship = new CompanyInternship
            {
                Id = internship.Id,
                CompanyInternshipTitle = internship.CompanyInternshipTitle,
                CompanyInternshipDescription = internship.CompanyInternshipDescription,
                CompanyUploadedInternshipStatus = internship.CompanyUploadedInternshipStatus,
                CompanyInternshipActivePeriod = internship.CompanyInternshipActivePeriod
            };
            isEditInternshipModalVisible = true;
        }

        private async Task UpdateInternshipStatusAsCompany(int internshipId, string newStatus)
        {
            await CompanyDashboardService.UpdateInternshipStatusAsync(internshipId, newStatus);
            await LoadUploadedInternshipsAsync();
            await ApplyFiltersAndUpdateCounts();
        }

        private async Task ChangeInternshipStatusToUnpublished(int internshipId)
        {
            await UpdateInternshipStatusAsCompany(internshipId, "Μη Δημοσιευμένη");
        }

        // Internship Applicants
        private async Task LoadInternshipApplicants(long internshipRng)
        {
            loadingInternshipId = internshipRng;
            isLoadingInternshipApplicants = true;
            StateHasChanged();

            try
            {
                var applicants = await CompanyDashboardService.GetInternshipApplicationsAsync();
                var filteredApplicants = applicants
                    .Where(a => a.RNGForInternshipApplied == internshipRng &&
                               a.CompanyEmailWhereStudentAppliedForInternship != null &&
                               a.CompanyEmailWhereStudentAppliedForInternship.Equals(CurrentUserEmail, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(a => a.DateTimeStudentAppliedForInternship)
                    .ToList();

                internshipApplicantsMap[internshipRng] = filteredApplicants;

                var acceptedCount = filteredApplicants.Count(a => a.InternshipStatusAppliedAtTheCompanySide == "Επιτυχής");
                acceptedApplicantsCountPerInternship_ForCompanyInternship[internshipRng] = acceptedCount;

                var internship = UploadedInternships.FirstOrDefault(i => i.RNGForInternshipUploadedAsCompany == internshipRng);
                if (internship != null)
                    availableSlotsPerInternship_ForCompanyInternship[internshipRng] = internship.OpenSlots_CompanyInternships - acceptedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading applicants: {ex.Message}");
            }
            finally
            {
                isLoadingInternshipApplicants = false;
                loadingInternshipId = null;
                StateHasChanged();
            }
        }

        private void EnableBulkEditModeForInternshipApplicants(long internshipRng)
        {
            isBulkEditModeForInternshipApplicants = true;
            selectedInternshipApplicantIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForInternshipApplicants()
        {
            isBulkEditModeForInternshipApplicants = false;
            selectedInternshipApplicantIds.Clear();
            pendingBulkActionForInternshipApplicants = "";
            StateHasChanged();
        }

        private async Task ExecuteBulkActionForInternshipApplicants()
        {
            if (string.IsNullOrEmpty(pendingBulkActionForInternshipApplicants) || !selectedInternshipApplicantIds.Any())
                return;

            // Implementation for bulk action on applicants
            CancelBulkEditForInternshipApplicants();
        }

        // Edit Internship
        private void CloseEditPopupForInternships()
        {
            isEditInternshipModalVisible = false;
            currentInternship = null;
        }

        private async Task HandleFileUploadToEditCompanyInternshipAttachment(InputFileChangeEventArgs e)
        {
            if (currentInternship == null || e.File == null) return;

            try
            {
                const long maxFileSize = 10485760;
                using var memoryStream = new System.IO.MemoryStream();
                await e.File.OpenReadStream(maxFileSize).CopyToAsync(memoryStream);
                currentInternship.CompanyInternshipAttachment = memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading attachment: {ex.Message}");
            }
        }

        private async Task SaveEditedInternship()
        {
            if (currentInternship == null) return;

            await CompanyDashboardService.CreateOrUpdateInternshipAsync(currentInternship);

            CloseEditPopupForInternships();
            await LoadUploadedInternshipsAsync();
            await ApplyFiltersAndUpdateCounts();
        }

        // Student CV Download
        private async Task DownloadStudentCVFromCompanyInternships(string studentEmail)
        {
            // TODO: Student model doesn't have a CV property - check if CV is in StudentDetails or Attachment property
            var student = await CompanyDashboardService.GetStudentByEmailAsync(studentEmail);
            if (student?.Attachment != null)
            {
                await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile",
                    $"student-cv-{student.RegNumber}.pdf", "application/pdf", student.Attachment);
            }
        }

        // Additional Missing Properties
        private List<CompanyInternship> internships = new List<CompanyInternship>();
        private bool sendEmailsForBulkInternshipAction = true;
        private bool showCheckboxesForEditCompanyInternship = false;
        private Dictionary<string, Student> studentDataCache = new Dictionary<string, Student>();
        private Dictionary<string, List<string>> RegionToTownsMap = new Dictionary<string, List<string>>();
        private string CompanyInternshipAttachmentErrorMessage = "";
        private bool showSlotWarningModal_ForCompanyInternship = false;
        private string slotWarningMessage_ForCompanyInternship = "";
        private bool showEmailConfirmationModalForInternshipApplicants = false;
        private bool showSuccessMessageWhenSaveInternshipAsCompany = false;

        // Methods
        private void ShowEmailConfirmationModalForInternshipApplicants(string action)
        {
            pendingBulkActionForInternshipApplicants = action;
            showEmailConfirmationModalForInternshipApplicants = true;
            StateHasChanged();
        }

        private void CloseEmailConfirmationModalForInternshipApplicants()
        {
            showEmailConfirmationModalForInternshipApplicants = false;
            StateHasChanged();
        }

        private void CloseSlotWarningModal_ForCompanyInternship()
        {
            showSlotWarningModal_ForCompanyInternship = false;
            slotWarningMessage_ForCompanyInternship = "";
            StateHasChanged();
        }

        private void CloseModalforHyperLinkTitleStudentName()
        {
            StateHasChanged();
        }

        private void CloseModalForInternships()
        {
            isModalVisibleForInternships = false;
            StateHasChanged();
        }

        // Missing Methods - extracted from MainLayout.razor.cs.backup
        private async Task ConfirmAndAcceptInternship(long internshipRNG, string studentUniqueID)
        {
            var internshipObj = UploadedInternships.FirstOrDefault(i => i.RNGForInternshipUploadedAsCompany == internshipRNG);
            if (internshipObj == null) return;

            int acceptedCount = acceptedApplicantsCountPerInternship_ForCompanyInternship.GetValueOrDefault(internshipRNG, 0);
            int availableSlots = availableSlotsPerInternship_ForCompanyInternship.GetValueOrDefault(internshipRNG, internshipObj.OpenSlots_CompanyInternships);

            if (acceptedCount >= availableSlots)
            {
                await JS.InvokeVoidAsync("alert", $"Έχετε Αποδεχτεί {acceptedCount}/{availableSlots} Αιτούντες");
                return;
            }

            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", "- ΑΠΟΔΟΧΗ ΑΙΤΗΣΗΣ - \n Η ενέργεια αυτή δεν θα μπορεί να αναιρεθεί. Είστε σίγουρος/η;");
            if (isConfirmed)
            {
                acceptedApplicantsCountPerInternship_ForCompanyInternship[internshipRNG] = acceptedCount + 1;
                StateHasChanged();
            }
        }

        private async Task ConfirmAndRejectInternship(long internshipRNG, string studentUniqueID)
        {
            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", "- ΑΠΟΡΡΙΨΗ ΑΙΤΗΣΗΣ - \n Η ενέργεια αυτή δεν θα μπορεί να αναιρεθεί. Είστε σίγουρος/η;");
            if (isConfirmed)
            {
                StateHasChanged();
            }
        }

        private async Task ShowStudentDetailsInNameAsHyperlink(string studentUniqueId, int applicationId, string source)
        {
            var student = await CompanyDashboardService.GetStudentByUniqueIdAsync(studentUniqueId);
            if (student != null)
            {
                selectedStudentFromCache = student;
                isModalVisibleToShowStudentDetailsAsCompanyFromTheirHyperlinkNameInCompanyInternships = true;
                StateHasChanged();
            }
        }

        private void ToggleInternshipApplicantSelection(long internshipRng, string studentId, ChangeEventArgs e)
        {
            var key = (internshipRng, studentId);
            if ((bool)e.Value!)
            {
                selectedInternshipApplicantIds.Add(key);
            }
            else
            {
                selectedInternshipApplicantIds.Remove(key);
            }
            StateHasChanged();
        }

        // Edit Modal Methods
        private void ToggleCheckboxesForEditCompanyInternship()
        {
            showCheckboxesForCompanyInternship = !showCheckboxesForCompanyInternship;
            StateHasChanged();
        }

        private void OnAreaCheckedChangedForEditCompanyInternship(ChangeEventArgs e, Area area)
        {
            var isChecked = (bool)e.Value!;
            if (isChecked)
            {
                if (!SelectedAreasToEditForCompanyInternship.Contains(area))
                {
                    SelectedAreasToEditForCompanyInternship.Add(area);
                }
            }
            else
            {
                SelectedAreasToEditForCompanyInternship.Remove(area);
                if (SelectedSubFieldsForEditCompanyInternship.ContainsKey(area.AreaName))
                {
                    SelectedSubFieldsForEditCompanyInternship.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForEditCompanyInternship(Area area)
        {
            return SelectedAreasToEditForCompanyInternship.Contains(area);
        }

        private void ToggleSubFieldsForEditCompanyInternship(Area area)
        {
            if (ExpandedAreasForEditCompanyInternship.Contains(area.Id))
                ExpandedAreasForEditCompanyInternship.Remove(area.Id);
            else
                ExpandedAreasForEditCompanyInternship.Add(area.Id);
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForEditCompanyInternship(ChangeEventArgs e, Area area, string subField)
        {
            var isChecked = (bool)e.Value!;
            if (!SelectedSubFieldsForEditCompanyInternship.ContainsKey(area.AreaName))
            {
                SelectedSubFieldsForEditCompanyInternship[area.AreaName] = new HashSet<string>();
            }

            if (isChecked)
            {
                SelectedSubFieldsForEditCompanyInternship[area.AreaName].Add(subField);
            }
            else
            {
                SelectedSubFieldsForEditCompanyInternship[area.AreaName].Remove(subField);
            }
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForEditCompanyInternship(Area area, string subField)
        {
            return SelectedSubFieldsForEditCompanyInternship.ContainsKey(area.AreaName) && 
                SelectedSubFieldsForEditCompanyInternship[area.AreaName].Contains(subField);
        }
    }
}
