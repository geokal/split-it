using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.ViewModels;
using QuizManager.Models;
using QuizManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.CompanySections
{
    public partial class CompanyInternshipsSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
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
        private string currentlyExpandedInternshipId = "";
        private bool showLoadingModalForDeleteInternship = false;

        // Internship Applicants
        private bool isLoadingInternshipApplicants = false;
        private string loadingInternshipId = "";
        private Dictionary<string, List<InternshipApplicant>> internshipApplicantsMap = new Dictionary<string, List<InternshipApplicant>>();
        private Dictionary<string, int> acceptedApplicantsCountPerInternship_ForCompanyInternship = new Dictionary<string, int>();
        private Dictionary<string, int> availableSlotsPerInternship_ForCompanyInternship = new Dictionary<string, int>();

        // Bulk Operations for Applicants
        private bool isBulkEditModeForInternshipApplicants = false;
        private HashSet<(string, string)> selectedInternshipApplicantIds = new HashSet<(string, string)>();
        private string pendingBulkActionForInternshipApplicants = "";
        private bool sendEmailsForBulkAction = false;

        // Edit Internship
        private CompanyInternship selectedInternship;
        private CompanyInternship currentInternship;
        private bool isEditInternshipModalVisible = false;
        private HashSet<int> ExpandedAreasForEditCompanyInternship = new HashSet<int>();

        // Student Details Modal
        private QuizManager.Models.Student selectedStudentFromCache;

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
                companyData = await dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyEmail == CurrentUserEmail);
            }

            Areas = await dbContext.Areas.ToListAsync();
            // Regions loading - check if Regions DbSet exists in your DbContext
            // Regions = await dbContext.Regions.Include(r => r.Towns).ToListAsync();
            professors = await dbContext.Professors.ToListAsync();
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

        private void OnSubFieldCheckedChangedForCompanyInternship(ChangeEventArgs e, Area area, SubField subField)
        {
            bool isChecked = (bool)e.Value;
            if (!SelectedSubFieldsForCompanyInternship.ContainsKey(area.Id))
                SelectedSubFieldsForCompanyInternship[area.Id] = new HashSet<string>();

            if (isChecked)
                SelectedSubFieldsForCompanyInternship[area.Id].Add(subField.SubFieldName);
            else
                SelectedSubFieldsForCompanyInternship[area.Id].Remove(subField.SubFieldName);
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForCompanyInternship(Area area, SubField subField)
        {
            return SelectedSubFieldsForCompanyInternship.ContainsKey(area.Id) &&
                   SelectedSubFieldsForCompanyInternship[area.Id].Contains(subField.SubFieldName);
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
                dbContext.CompanyInternships.Add(companyInternship);
                await dbContext.SaveChangesAsync();

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
            UploadedInternships = await dbContext.CompanyInternships
                .Where(i => i.CompanyEmailUsedToUploadInternship == CurrentUserEmail)
                .OrderByDescending(i => i.CompanyInternshipUploadDate)
                .ToListAsync();
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
            var internshipsToUpdate = await dbContext.CompanyInternships
                .Where(i => selectedInternshipIds.Contains(i.Id))
                .ToListAsync();

            foreach (var i in internshipsToUpdate)
                i.CompanyUploadedInternshipStatus = newStatus;

            await dbContext.SaveChangesAsync();
            CancelBulkEditForInternships();
            await LoadUploadedInternshipsAsync();
            await ApplyFiltersAndUpdateCounts();
        }

        private async Task ExecuteBulkCopyForInternships()
        {
            var internshipsToCopy = await dbContext.CompanyInternships
                .Where(i => selectedInternshipIds.Contains(i.Id))
                .ToListAsync();

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
                dbContext.CompanyInternships.Add(copy);
            }

            await dbContext.SaveChangesAsync();
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
                var internshipsToDelete = await dbContext.CompanyInternships
                    .Where(i => selectedInternshipIds.Contains(i.Id))
                    .ToListAsync();
                dbContext.CompanyInternships.RemoveRange(internshipsToDelete);
                await dbContext.SaveChangesAsync();
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
                var internshipToDelete = await dbContext.CompanyInternships.FindAsync(internshipId);
                if (internshipToDelete != null)
                {
                    dbContext.CompanyInternships.Remove(internshipToDelete);
                    await dbContext.SaveChangesAsync();
                    await LoadUploadedInternshipsAsync();
                    await ApplyFiltersAndUpdateCounts();
                }
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
            var internshipWithAttachment = await dbContext.CompanyInternships.FindAsync(internshipId);
            if (internshipWithAttachment?.CompanyInternshipAttachment != null)
            {
                await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile",
                    $"internship-attachment-{internshipId}.pdf", "application/pdf", internshipWithAttachment.CompanyInternshipAttachment);
            }
        }

        private void ToggleInternshipExpanded(string internshipHashedId)
        {
            currentlyExpandedInternshipId = currentlyExpandedInternshipId == internshipHashedId ? "" : internshipHashedId;
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
            var internshipToUpdate = await dbContext.CompanyInternships.FindAsync(internshipId);
            if (internshipToUpdate != null)
            {
                internshipToUpdate.CompanyUploadedInternshipStatus = newStatus;
                await dbContext.SaveChangesAsync();
                await LoadUploadedInternshipsAsync();
                await ApplyFiltersAndUpdateCounts();
            }
        }

        private async Task ChangeInternshipStatusToUnpublished(int internshipId)
        {
            await UpdateInternshipStatusAsCompany(internshipId, "Μη Δημοσιευμένη");
        }

        // Internship Applicants
        private async Task LoadInternshipApplicants(string internshipHashedId)
        {
            loadingInternshipId = internshipHashedId;
            isLoadingInternshipApplicants = true;
            StateHasChanged();

            try
            {
                // Note: InternshipsApplied doesn't have a direct link to internship RNG
                // This needs to be refactored to use CompanyDashboardService or query differently
                var applicants = await dbContext.InternshipsApplied
                    .Where(a => a.CompanyEmailWhereStudentAppliedForInternship != null &&
                               a.CompanyEmailWhereStudentAppliedForInternship.ToLower() == CurrentUserEmail.ToLower())
                    .OrderByDescending(a => a.DateTimeStudentAppliedForInternship)
                    .ToListAsync();

                // Convert InternshipsApplied to InternshipApplicant ViewModel
                // TODO: This logic needs to be properly implemented based on the data model
                var internshipApplicants = applicants.Select(a => new InternshipApplicant
                {
                    Id = a.Id,
                    StudentEmail = a.StudentEmailAppliedForInternship ?? "",
                    StudentName = "", // TODO: Load from StudentDetails if available
                    Status = a.InternshipStatusAppliedAtTheCompanySide ?? "",
                    StudentCv = "", // TODO: Load CV if available
                    StudentMotivationLetter = "",
                    CompanyInternshipId = internshipHashedId,
                    ApplicationDate = a.DateTimeStudentAppliedForInternship
                }).ToList();

                internshipApplicantsMap[internshipHashedId] = internshipApplicants;

                var acceptedCount = applicants.Count(a => a.InternshipStatusAppliedAtTheCompanySide == "Αποδεκτή");
                acceptedApplicantsCountPerInternship_ForCompanyInternship[internshipHashedId] = acceptedCount;

                var internship = UploadedInternships.FirstOrDefault(i => i.RNGForInternshipUploadedAsCompany_HashedAsUniqueID == internshipHashedId);
                if (internship != null)
                    availableSlotsPerInternship_ForCompanyInternship[internshipHashedId] = internship.OpenSlots_CompanyInternships - acceptedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading applicants: {ex.Message}");
            }
            finally
            {
                isLoadingInternshipApplicants = false;
                loadingInternshipId = "";
                StateHasChanged();
            }
        }

        private void EnableBulkEditModeForInternshipApplicants(string internshipHashedId)
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

            var internshipToUpdate = await dbContext.CompanyInternships.FindAsync(currentInternship.Id);
            if (internshipToUpdate != null)
            {
                internshipToUpdate.CompanyInternshipTitle = currentInternship.CompanyInternshipTitle;
                internshipToUpdate.CompanyInternshipDescription = currentInternship.CompanyInternshipDescription;
                internshipToUpdate.CompanyInternshipActivePeriod = currentInternship.CompanyInternshipActivePeriod;
                if (currentInternship.CompanyInternshipAttachment != null)
                    internshipToUpdate.CompanyInternshipAttachment = currentInternship.CompanyInternshipAttachment;

                await dbContext.SaveChangesAsync();
            }

            CloseEditPopupForInternships();
            await LoadUploadedInternshipsAsync();
            await ApplyFiltersAndUpdateCounts();
        }

        // Student CV Download
        private async Task DownloadStudentCVFromCompanyInternships(string studentEmail)
        {
            // TODO: Student model doesn't have a CV property - check if CV is in StudentDetails or Attachment property
            var student = await dbContext.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
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
        private void ShowEmailConfirmationModalForInternshipApplicants()
        {
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
            StateHasChanged();
        }
    }
}

