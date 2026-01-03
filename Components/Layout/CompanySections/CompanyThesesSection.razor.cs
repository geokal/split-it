using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.Services;
using QuizManager.Services.CompanyDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.CompanySections
{
    public partial class CompanyThesesSection : ComponentBase
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
        private bool isUploadCompanyThesisFormVisible = false;

        // Form Model and Validation
        private CompanyThesis thesis = new CompanyThesis();
        private bool showErrorMessageforUploadingthesisAsCompany = false;
        private bool showLoadingModalForThesis = false;
        private int loadingProgress = 0;
        private bool showSuccessMessage = false;

        // Character Limits
        private int remainingCharactersInThesisFieldUploadAsCompany = 120;
        private int remainingCharactersInThesisDescriptionUploadAsCompany = 1000;

        // Areas/Subfields
        private List<Area> Areas = new List<Area>();
        private List<Area> SelectedAreasWhenUploadThesisAsCompany = new List<Area>();
        private Dictionary<int, HashSet<string>> SelectedSubFieldsForCompanyThesis = new Dictionary<int, HashSet<string>>();
        private HashSet<int> ExpandedAreasForCompanyThesis = new HashSet<int>();
        private bool areCheckboxesVisibleForCompanyThesis = false;

        // Skills
        private List<Skill> Skills = new List<Skill>();
        private List<Skill> SelectedSkillsWhenUploadThesisAsCompany = new List<Skill>();
        private bool areSkillsCheckboxesVisibleForCompanyThesis = false;

        // View Uploaded Theses
        private bool isShowActiveThesesAsCompanyFormVisible = false;
        private bool isLoadingThesesHistory = false;
        private string selectedStatusFilterForCompanyTheses = "Όλα";
        private List<CompanyThesis> UploadedTheses = new List<CompanyThesis>();
        private List<CompanyThesis> FilteredTheses = new List<CompanyThesis>();
        private int currentPageForCompanyTheses = 1;
        private int CompanyThesesPerPage = 10;
        private int[] pageSizeOptions_SeeMyUploadedThesesAsCompany = new[] { 10, 50, 100 };
        private int totalCountForCompanyTheses = 0;
        private int publishedCountForCompanyTheses = 0;
        private int unpublishedCountForCompanyTheses = 0;
        private int withdrawnCountForCompanyTheses = 0;

        // Bulk Operations
        private bool isBulkEditModeForTheses = false;
        private HashSet<int> selectedThesisIds = new HashSet<int>();
        private bool showBulkActionModalForTheses = false;
        private string bulkActionForTheses = "";
        private string newStatusForBulkActionForTheses = "Μη Δημοσιευμένη";

        // Thesis Menu
        private int activeThesisMenuId = 0;
        private Dictionary<long, bool> expandedCompanyTheses = new Dictionary<long, bool>();
        private bool showLoadingModalForDeleteCompanyThesis = false;

        // Thesis Applicants
        private bool isLoadingCompanyThesisApplicants = false;
        private long? loadingCompanyThesisId = null;
        private Dictionary<long, List<CompanyThesisApplied>> companyThesisApplicantsMap = new Dictionary<long, List<CompanyThesisApplied>>();
        private Dictionary<long, int> acceptedApplicantsCountPerThesis_ForCompanyThesis = new Dictionary<long, int>();
        private Dictionary<long, int> availableSlotsPerThesis_ForCompanyThesis = new Dictionary<long, int>();

        // Bulk Operations for Applicants
        private bool isBulkEditModeForThesisApplicants = false;
        private HashSet<(long, string)> selectedThesisApplicantIds = new HashSet<(long, string)>();
        private string pendingBulkActionForThesisApplicants = "";
        private bool sendEmailsForBulkAction = true;

        // Professor Interest in Theses
        private Dictionary<long, bool> expandedCompanyThesesForProfessorInterest = new Dictionary<long, bool>();
        private bool isLoadingCompanyThesisProfessors = false;
        private long? loadingCompanyThesisProfessorId = null;
        private Dictionary<string, QuizManager.Models.Professor> professorDataCache = new Dictionary<string, QuizManager.Models.Professor>();

        // Search Professor Theses
        private List<ProfessorThesis> professorThesesSearchResults = new List<ProfessorThesis>();
        private List<ProfessorThesis> professorThesesResultsToFindThesesAsCompany = new List<ProfessorThesis>();
        private bool searchPerformedToFindThesesAsCompany = false;
        private int currentPageForProfessorTheses = 1;
        private int professorThesesPerPage = 10;
        private string searchProfessorNameToFindThesesAsCompany = "";
        private string searchProfessorSurnameToFindThesesAsCompany = "";
        private string searchProfessorThesisTitleToFindThesesAsCompany = "";
        private DateTime? searchStartingDateToFindThesesAsCompany = null;
        private string searchAreasInputToFindThesesAsCompany = "";
        private List<string> selectedAreasToFindThesesAsCompany = new List<string>();
        private List<string> areaSuggestionsToFindThesesAsCompany = new List<string>();
        private string searchSkillsInputToFindThesesAsCompany = "";
        private List<string> selectedSkillsToFindThesesAsCompany = new List<string>();
        private List<string> skillSuggestionsToFindThesesAsCompany = new List<string>();
        private List<string> professorNameSuggestions = new List<string>();
        private List<string> professorSurnameSuggestions = new List<string>();
        private bool showCheckboxesForCompanyThesis = false;

        // Professor Details Modal
        private QuizManager.Models.Professor selectedProfessorDetails;
        private QuizManager.Models.Professor currentProfessorDetails;
        private ProfessorThesis selectedProfessorThesisToSeeDetailsOnEyeIconAsCompany;

        // Edit Thesis
        private CompanyThesis selectedCompanyThesis;
        private CompanyThesis currentThesis;
        private bool isModalVisibleToEditCompanyThesisDetails = false;
        private HashSet<int> ExpandedAreasForEditCompanyThesis = new HashSet<int>();
        private bool showAreasForEditCompanyThesis = false;
        private bool showSkillsForEditCompanyThesis = false;
        private List<Skill> SelectedSkillsToEditForCompanyThesis = new List<Skill>();

        // Thesis Details Modal
        private bool isModalVisibleToShowCompanyThesisDetails = false;

        // Computed Properties
        private int totalPagesForCompanyTheses => (int)Math.Ceiling((double)(FilteredTheses?.Count ?? 0) / CompanyThesesPerPage);

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
            Skills = lookups.Skills.ToList();
        }

        // Form Visibility
        private void ToggleFormVisibilityForUploadCompanyThesis()
        {
            isUploadCompanyThesisFormVisible = !isUploadCompanyThesisFormVisible;
            StateHasChanged();
        }

        // Character Limits
        private void CheckCharacterLimitInThesisFieldUploadAsCompany(ChangeEventArgs e)
        {
            var text = e.Value?.ToString() ?? "";
            remainingCharactersInThesisFieldUploadAsCompany = Math.Max(0, 120 - text.Length);
        }

        private void CheckCharacterLimitInThesisDescriptionUploadAsCompany(ChangeEventArgs e)
        {
            var text = e.Value?.ToString() ?? "";
            remainingCharactersInThesisDescriptionUploadAsCompany = Math.Max(0, 1000 - text.Length);
        }

        // Validation
        private bool IsValidPhoneNumber(string phone)
        {
            return !string.IsNullOrEmpty(phone) && phone.Length >= 10;
        }

        private void OnCompanyCreateThesisPhoneNumberInput(ChangeEventArgs e)
        {
            // Validation handled in markup
        }

        // Areas/Subfields
        private void ToggleCheckboxesForCompanyThesis()
        {
            areCheckboxesVisibleForCompanyThesis = !areCheckboxesVisibleForCompanyThesis;
            StateHasChanged();
        }

        private bool HasAnySelectionForCompanyThesis()
        {
            return SelectedAreasWhenUploadThesisAsCompany.Any() ||
                   SelectedSubFieldsForCompanyThesis.Any(kvp => kvp.Value.Any());
        }

        private void OnAreaCheckedChangedForCompanyThesis(ChangeEventArgs e, Area area)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
            {
                if (!SelectedAreasWhenUploadThesisAsCompany.Any(a => a.Id == area.Id))
                    SelectedAreasWhenUploadThesisAsCompany.Add(area);
            }
            else
            {
                SelectedAreasWhenUploadThesisAsCompany.RemoveAll(a => a.Id == area.Id);
                SelectedSubFieldsForCompanyThesis.Remove(area.Id);
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForCompanyThesis(Area area)
        {
            return SelectedAreasWhenUploadThesisAsCompany.Any(a => a.Id == area.Id);
        }

        private void ToggleSubFieldsForCompanyThesis(Area area)
        {
            if (ExpandedAreasForCompanyThesis.Contains(area.Id))
                ExpandedAreasForCompanyThesis.Remove(area.Id);
            else
                ExpandedAreasForCompanyThesis.Add(area.Id);
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForCompanyThesis(ChangeEventArgs e, Area area, string subField)
        {
            bool isChecked = (bool)e.Value;
            if (!SelectedSubFieldsForCompanyThesis.ContainsKey(area.Id))
                SelectedSubFieldsForCompanyThesis[area.Id] = new HashSet<string>();

            if (isChecked)
                SelectedSubFieldsForCompanyThesis[area.Id].Add(subField);
            else
                SelectedSubFieldsForCompanyThesis[area.Id].Remove(subField);
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForCompanyThesis(Area area, string subField)
        {
            return SelectedSubFieldsForCompanyThesis.ContainsKey(area.Id) &&
                   SelectedSubFieldsForCompanyThesis[area.Id].Contains(subField);
        }

        // Skills
        private void ToggleCheckboxesForSkillsForCompanyThesis()
        {
            areSkillsCheckboxesVisibleForCompanyThesis = !areSkillsCheckboxesVisibleForCompanyThesis;
            StateHasChanged();
        }

        private void OnCheckedChangedForSkillsWhenUploadThesisAsCompany(ChangeEventArgs e, Skill skill)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
            {
                if (!SelectedSkillsWhenUploadThesisAsCompany.Any(s => s.Id == skill.Id))
                    SelectedSkillsWhenUploadThesisAsCompany.Add(skill);
            }
            else
            {
                SelectedSkillsWhenUploadThesisAsCompany.RemoveAll(s => s.Id == skill.Id);
            }
            StateHasChanged();
        }

        private bool IsSelectedForSkillsWhenUploadThesisAsCompany(Skill skill)
        {
            return SelectedSkillsWhenUploadThesisAsCompany.Any(s => s.Id == skill.Id);
        }

        // File Handling
        private async Task HandleFileSelectedForCompanyThesisAttachment(InputFileChangeEventArgs e)
        {
            try
            {
                if (e.File == null)
                {
                    thesis.CompanyThesisAttachmentUpload = null;
                    return;
                }

                const long maxFileSize = 10485760;
                if (e.File.Size > maxFileSize) return;

                using var memoryStream = new System.IO.MemoryStream();
                await e.File.OpenReadStream(maxFileSize).CopyToAsync(memoryStream);
                thesis.CompanyThesisAttachmentUpload = memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
            }
        }

        // Save Operations
        private async Task HandleTemporarySaveThesisAsCompany()
        {
            await SaveThesisAsCompany(false);
        }

        private async Task HandlePublishSaveThesisAsCompany()
        {
            await SaveThesisAsCompany(true);
        }

        private async Task SaveThesisAsCompany(bool publishThesis)
        {
            showLoadingModalForThesis = true;
            loadingProgress = 0;
            showErrorMessageforUploadingthesisAsCompany = false;
            showSuccessMessage = false;
            StateHasChanged();

            try
            {
                await UpdateProgress(30);

                // Build areas string
                var areasWithSubfields = new List<string>();
                foreach (var area in SelectedAreasWhenUploadThesisAsCompany)
                    areasWithSubfields.Add(area.AreaName);
                foreach (var areaSubFields in SelectedSubFieldsForCompanyThesis)
                    areasWithSubfields.AddRange(areaSubFields.Value);
                thesis.CompanyThesisAreasUpload = string.Join(",", areasWithSubfields);

                // Build skills string
                thesis.CompanyThesisSkillsNeeded = string.Join(",", SelectedSkillsWhenUploadThesisAsCompany.Select(s => s.SkillName));

                await UpdateProgress(50);

                // Set thesis properties
                thesis.RNGForThesisUploadedAsCompany = new Random().NextInt64();
                thesis.RNGForThesisUploadedAsCompany_HashedAsUniqueID = HashingHelper.HashLong(thesis.RNGForThesisUploadedAsCompany);
                thesis.CompanyEmailUsedToUploadThesis = CurrentUserEmail;
                thesis.CompanyThesisUploadDateTime = DateTime.Now;
                thesis.CompanyThesisStatus = publishThesis ? "Δημοσιευμένη" : "Μη Δημοσιευμένη";

                await UpdateProgress(70);
                var result = await CompanyDashboardService.CreateOrUpdateThesisAsync(thesis);
                if (!result.Success)
                {
                    throw new InvalidOperationException(result.Error ?? "Failed to save thesis.");
                }

                await UpdateProgress(90);
                showSuccessMessage = true;

                thesis = new CompanyThesis();
                SelectedAreasWhenUploadThesisAsCompany.Clear();
                SelectedSubFieldsForCompanyThesis.Clear();
                ExpandedAreasForCompanyThesis.Clear();
                SelectedSkillsWhenUploadThesisAsCompany.Clear();

                await UpdateProgress(100);
                await Task.Delay(500);
                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            catch (Exception ex)
            {
                showLoadingModalForThesis = false;
                showErrorMessageforUploadingthesisAsCompany = true;
                Console.WriteLine($"Error uploading thesis: {ex.Message}");
                StateHasChanged();
            }
        }

        private async Task UpdateProgress(int progress)
        {
            loadingProgress = progress;
            StateHasChanged();
            await Task.Delay(50);
        }

        // View Uploaded Theses
        private async Task ToggleFormVisibilityToShowMyActiveThesesAsCompany()
        {
            isShowActiveThesesAsCompanyFormVisible = !isShowActiveThesesAsCompanyFormVisible;
            if (isShowActiveThesesAsCompanyFormVisible)
            {
                isLoadingThesesHistory = true;
                StateHasChanged();
                try
                {
                    await LoadUploadedThesesAsync();
                    await ApplyFiltersAndUpdateCounts();
                }
                finally
                {
                    isLoadingThesesHistory = false;
                    StateHasChanged();
                }
            }
        }

        private async Task LoadUploadedThesesAsync()
        {
            var data = await CompanyDashboardService.LoadDashboardDataAsync();
            UploadedTheses = data.Theses
                .Where(t => t.CompanyEmailUsedToUploadThesis == CurrentUserEmail)
                .OrderByDescending(t => t.CompanyThesisUploadDateTime)
                .ToList();
        }

        private async Task ApplyFiltersAndUpdateCounts()
        {
            FilteredTheses = selectedStatusFilterForCompanyTheses == "Όλα"
                ? UploadedTheses.ToList()
                : UploadedTheses.Where(t => t.CompanyThesisStatus == selectedStatusFilterForCompanyTheses).ToList();

            totalCountForCompanyTheses = UploadedTheses.Count;
            publishedCountForCompanyTheses = UploadedTheses.Count(t => t.CompanyThesisStatus == "Δημοσιευμένη");
            unpublishedCountForCompanyTheses = UploadedTheses.Count(t => t.CompanyThesisStatus == "Μη Δημοσιευμένη");
            withdrawnCountForCompanyTheses = UploadedTheses.Count(t => t.CompanyThesisStatus == "Ανακληθείσα");
            currentPageForCompanyTheses = 1;
        }

        private void HandleStatusFilterChangeForThesesAsCompany(ChangeEventArgs e)
        {
            selectedStatusFilterForCompanyTheses = e.Value?.ToString() ?? "Όλα";
            _ = ApplyFiltersAndUpdateCounts();
        }

        private void OnPageSizeChange_SeeMyUploadedThesesAsCompany(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var size) && size > 0)
            {
                CompanyThesesPerPage = size;
                currentPageForCompanyTheses = 1;
                StateHasChanged();
            }
        }

        private IEnumerable<CompanyThesis> GetPaginatedCompanyTheses()
        {
            return FilteredTheses?
                .Skip((currentPageForCompanyTheses - 1) * CompanyThesesPerPage)
                .Take(CompanyThesesPerPage)
                ?? Enumerable.Empty<CompanyThesis>();
        }

        // Pagination
        private void GoToFirstPageForCompanyTheses() => ChangePage(1);
        private void PreviousPageForCompanyTheses() => ChangePage(Math.Max(1, currentPageForCompanyTheses - 1));
        private void NextPageForCompanyTheses() => ChangePage(Math.Min(totalPagesForCompanyTheses, currentPageForCompanyTheses + 1));
        private void GoToLastPageForCompanyTheses() => ChangePage(totalPagesForCompanyTheses);
        private void GoToPageForCompanyTheses(int page) => ChangePage(page);

        private void ChangePage(int newPage)
        {
            if (newPage > 0 && newPage <= totalPagesForCompanyTheses)
            {
                currentPageForCompanyTheses = newPage;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePagesForCompanyTheses()
        {
            var pages = new List<int>();
            int current = currentPageForCompanyTheses;
            int total = totalPagesForCompanyTheses;
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
        private void EnableBulkEditModeForTheses()
        {
            isBulkEditModeForTheses = true;
            selectedThesisIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForTheses()
        {
            isBulkEditModeForTheses = false;
            selectedThesisIds.Clear();
            StateHasChanged();
        }

        private void ToggleThesisSelection(int thesisId, ChangeEventArgs e)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked) selectedThesisIds.Add(thesisId);
            else selectedThesisIds.Remove(thesisId);
            StateHasChanged();
        }

        private void ShowBulkActionOptions()
        {
            if (selectedThesisIds.Any())
                showBulkActionModalForTheses = true;
        }

        private void CloseBulkActionModalForTheses()
        {
            showBulkActionModalForTheses = false;
            bulkActionForTheses = "";
        }

        private async Task ExecuteBulkStatusChangeForTheses(string newStatus)
        {
            foreach (var id in selectedThesisIds)
            {
                await CompanyDashboardService.UpdateThesisStatusAsync(id, newStatus);
            }
            CancelBulkEditForTheses();
            await LoadUploadedThesesAsync();
            await ApplyFiltersAndUpdateCounts();
        }

        private async Task ExecuteBulkCopyForTheses()
        {
            var thesesToCopy = UploadedTheses
                .Where(t => selectedThesisIds.Contains(t.Id))
                .ToList();

            foreach (var t in thesesToCopy)
            {
                var copy = new CompanyThesis
                {
                    CompanyThesisTitle = t.CompanyThesisTitle + " (Αντίγραφο)",
                    CompanyThesisDescriptionsUploaded = t.CompanyThesisDescriptionsUploaded,
                    CompanyThesisAreasUpload = t.CompanyThesisAreasUpload,
                    CompanyThesisSkillsNeeded = t.CompanyThesisSkillsNeeded,
                    CompanyThesisStatus = "Μη Δημοσιευμένη",
                    CompanyEmailUsedToUploadThesis = CurrentUserEmail,
                    CompanyThesisUploadDateTime = DateTime.Now,
                    RNGForThesisUploadedAsCompany = new Random().NextInt64()
                };
                copy.RNGForThesisUploadedAsCompany_HashedAsUniqueID = HashingHelper.HashLong(copy.RNGForThesisUploadedAsCompany);
                await CompanyDashboardService.CreateOrUpdateThesisAsync(copy);
            }

            CancelBulkEditForTheses();
            await LoadUploadedThesesAsync();
            await ApplyFiltersAndUpdateCounts();
        }

        private async Task ExecuteBulkActionForTheses()
        {
            if (string.IsNullOrEmpty(bulkActionForTheses) || !selectedThesisIds.Any()) return;

            if (bulkActionForTheses == "Αλλαγή Κατάστασης")
                await ExecuteBulkStatusChangeForTheses(newStatusForBulkActionForTheses);
            else if (bulkActionForTheses == "Αντιγραφή")
                await ExecuteBulkCopyForTheses();
            else if (bulkActionForTheses == "Διαγραφή")
            {
                foreach (var id in selectedThesisIds)
                {
                    await CompanyDashboardService.DeleteThesisAsync(id);
                }
                CancelBulkEditForTheses();
                await LoadUploadedThesesAsync();
                await ApplyFiltersAndUpdateCounts();
            }

            CloseBulkActionModalForTheses();
        }

        // Thesis Menu & Operations
        private void ToggleThesisMenu(int thesisId)
        {
            activeThesisMenuId = activeThesisMenuId == thesisId ? 0 : thesisId;
            StateHasChanged();
        }

        private async Task DeleteCompanyThesis(int thesisId)
        {
            showLoadingModalForDeleteCompanyThesis = true;
            StateHasChanged();

            try
            {
                await CompanyDashboardService.DeleteThesisAsync(thesisId);
                await LoadUploadedThesesAsync();
                await ApplyFiltersAndUpdateCounts();
            }
            finally
            {
                showLoadingModalForDeleteCompanyThesis = false;
                StateHasChanged();
            }
        }

        private void ShowCompanyThesisDetails(CompanyThesis thesisToShow)
        {
            selectedCompanyThesis = thesisToShow;
            isModalVisibleToShowCompanyThesisDetails = true;
            StateHasChanged();
        }

        private async Task DownloadAttachmentForCompanyTheses(int thesisId)
        {
            var attachment = await CompanyDashboardService.GetThesisAttachmentAsync(thesisId);
            if (attachment?.Data != null && attachment.Data.Length > 0)
            {
                await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile",
                    attachment.FileName, attachment.ContentType, attachment.Data);
            }
        }

        private void ToggleCompanyThesesExpanded(long thesisRng)
        {
            if (expandedCompanyTheses.ContainsKey(thesisRng))
                expandedCompanyTheses[thesisRng] = !expandedCompanyTheses[thesisRng];
            else
                expandedCompanyTheses[thesisRng] = true;
            StateHasChanged();
        }

        private void EditCompanyThesisDetails(CompanyThesis thesisToEdit)
        {
            currentThesis = new CompanyThesis
            {
                Id = thesisToEdit.Id,
                CompanyThesisTitle = thesisToEdit.CompanyThesisTitle,
                CompanyThesisDescriptionsUploaded = thesisToEdit.CompanyThesisDescriptionsUploaded,
                CompanyThesisStatus = thesisToEdit.CompanyThesisStatus
            };
            isModalVisibleToEditCompanyThesisDetails = true;
        }

        private async Task UpdateThesisStatusAsCompany(int thesisId, string newStatus)
        {
            await CompanyDashboardService.UpdateThesisStatusAsync(thesisId, newStatus);
            await LoadUploadedThesesAsync();
            await ApplyFiltersAndUpdateCounts();
        }

        private async Task ChangeCompanyThesisStatusToUnpublished(int thesisId)
        {
            await UpdateThesisStatusAsCompany(thesisId, "Μη Δημοσιευμένη");
        }

        // Thesis Applicants
        private async Task LoadThesisApplicants(long thesisRng)
        {
            loadingCompanyThesisId = thesisRng;
            isLoadingCompanyThesisApplicants = true;
            StateHasChanged();

            try
            {
                var applicants = await CompanyDashboardService.GetThesisApplicationsAsync();
                var filteredApplicants = applicants
                    .Where(a => a.RNGForCompanyThesisApplied == thesisRng &&
                               a.CompanyEmailWhereStudentAppliedForThesis != null &&
                               a.CompanyEmailWhereStudentAppliedForThesis.Equals(CurrentUserEmail, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(a => a.DateTimeStudentAppliedForThesis)
                    .ToList();

                companyThesisApplicantsMap[thesisRng] = filteredApplicants;

                var acceptedCount = filteredApplicants.Count(a => a.CompanyThesisStatusAppliedAtCompanySide == "Έχει γίνει Αποδοχή");
                acceptedApplicantsCountPerThesis_ForCompanyThesis[thesisRng] = acceptedCount;

                var thesis = UploadedTheses.FirstOrDefault(t => t.RNGForThesisUploadedAsCompany == thesisRng);
                if (thesis != null)
                    availableSlotsPerThesis_ForCompanyThesis[thesisRng] = thesis.OpenSlots_CompanyThesis - acceptedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading thesis applicants: {ex.Message}");
            }
            finally
            {
                isLoadingCompanyThesisApplicants = false;
                loadingCompanyThesisId = null;
                StateHasChanged();
            }
        }

        private void EnableBulkEditModeForThesisApplicants(long thesisRng)
        {
            isBulkEditModeForThesisApplicants = true;
            selectedThesisApplicantIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForThesisApplicants()
        {
            isBulkEditModeForThesisApplicants = false;
            selectedThesisApplicantIds.Clear();
            pendingBulkActionForThesisApplicants = "";
            StateHasChanged();
        }

        private async Task ExecuteBulkActionForThesisApplicants()
        {
            if (string.IsNullOrEmpty(pendingBulkActionForThesisApplicants) || !selectedThesisApplicantIds.Any())
                return;

            var decision = pendingBulkActionForThesisApplicants == "accept"
                ? ApplicationDecision.Accept
                : ApplicationDecision.Reject;

            foreach (var (rng, studentId) in selectedThesisApplicantIds)
            {
                await CompanyDashboardService.DecideOnApplicationAsync(new ApplicationDecisionRequest
                {
                    ApplicationType = ApplicationType.Thesis,
                    ApplicationRng = rng,
                    StudentUniqueId = studentId,
                    Decision = decision
                });
            }

            CancelBulkEditForThesisApplicants();
        }

        // Professor Interest in Theses
        private void ToggleCompanyThesesExpandedForProfessorInterest(long thesisRng)
        {
            if (expandedCompanyThesesForProfessorInterest.ContainsKey(thesisRng))
                expandedCompanyThesesForProfessorInterest[thesisRng] = !expandedCompanyThesesForProfessorInterest[thesisRng];
            else
                expandedCompanyThesesForProfessorInterest[thesisRng] = true;
            StateHasChanged();
        }

        private async Task ShowProfessorDetailsAtCompanyThesisInterest(CompanyThesis thesisForInterest)
        {
            // Implementation for showing professor interest
            StateHasChanged();
        }

        // Search Professor Theses
        private async Task SearchProfessorThesesAsCompany()
        {
            var filter = new ProfessorThesisSearchFilter
            {
                ProfessorName = searchProfessorNameToFindThesesAsCompany,
                ProfessorSurname = searchProfessorSurnameToFindThesesAsCompany,
                ThesisTitle = searchProfessorThesisTitleToFindThesesAsCompany,
                EarliestStartDate = searchStartingDateToFindThesesAsCompany,
                MaxResults = 500
            };

            var results = await CompanyDashboardService.SearchProfessorThesesAsync(filter);

            if (selectedAreasToFindThesesAsCompany.Any())
            {
                results = results.Where(t => t.ThesisAreas != null &&
                    selectedAreasToFindThesesAsCompany.Any(area => t.ThesisAreas.Contains(area, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (selectedSkillsToFindThesesAsCompany.Any())
            {
                results = results.Where(t => t.ThesisSkills != null &&
                    selectedSkillsToFindThesesAsCompany.Any(skill => t.ThesisSkills.Contains(skill, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            professorThesesResultsToFindThesesAsCompany = results.ToList();
            professorThesesSearchResults = results.ToList();
            searchPerformedToFindThesesAsCompany = true;
            currentPageForProfessorTheses = 1;
            StateHasChanged();
        }

        private void ClearSearchFieldsForSearchProfessorThesesAsCompany()
        {
            searchProfessorNameToFindThesesAsCompany = string.Empty;
            searchProfessorThesisTitleToFindThesesAsCompany = string.Empty;
            searchProfessorSurnameToFindThesesAsCompany = string.Empty;
            searchSkillsInputToFindThesesAsCompany = string.Empty;
            searchStartingDateToFindThesesAsCompany = null;
            searchAreasInputToFindThesesAsCompany = string.Empty;

            selectedAreasToFindThesesAsCompany.Clear(); 
            selectedSkillsToFindThesesAsCompany.Clear();

            professorThesesResultsToFindThesesAsCompany = new List<ProfessorThesis>();
            professorThesesSearchResults = new List<ProfessorThesis>();
            searchPerformedToFindThesesAsCompany = false;

            // Clear suggestions
            professorNameSuggestions.Clear();
            professorSurnameSuggestions.Clear();
            skillSuggestionsToFindThesesAsCompany.Clear();
            areaSuggestionsToFindThesesAsCompany.Clear();
            StateHasChanged();
        }

        private IEnumerable<ProfessorThesis> GetPaginatedProfessorTheses_AsCompany()
        {
            var results = professorThesesResultsToFindThesesAsCompany ?? professorThesesSearchResults;
            return results?
                .Skip((currentPageForProfessorTheses - 1) * professorThesesPerPage)
                .Take(professorThesesPerPage)
                ?? Enumerable.Empty<ProfessorThesis>();
        }

        private void OnPageSizeChange_SearchForProfessorThesesAsCompany(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                professorThesesPerPage = newSize;
                currentPageForProfessorTheses = 1;
                StateHasChanged();
            }
        }

        private void ShowThesisDetailsAsCompany(ProfessorThesis thesis)
        {
            selectedProfessorThesisToSeeDetailsOnEyeIconAsCompany = thesis;
            StateHasChanged();
        }

        private async Task MarkInterestInProfessorThesis(ProfessorThesis thesis)
        {
            // Implementation for marking interest
            StateHasChanged();
        }

        private async Task HandleProfessorNameInput(ChangeEventArgs e)
        {
            searchProfessorNameToFindThesesAsCompany = e.Value?.ToString() ?? string.Empty;
        
            if (string.IsNullOrWhiteSpace(searchProfessorNameToFindThesesAsCompany))
            {
                professorNameSuggestions.Clear();
                return;
            }

            var lookups = await CompanyDashboardService.GetLookupsAsync();
            professorNameSuggestions = lookups.Professors
                .Where(p => !string.IsNullOrEmpty(p.ProfName) &&
                            p.ProfName.Contains(searchProfessorNameToFindThesesAsCompany, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.ProfName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();
        }

        private async Task HandleProfessorSurnameInput(ChangeEventArgs e)
        {
            searchProfessorSurnameToFindThesesAsCompany = e.Value?.ToString() ?? string.Empty;
        
            if (string.IsNullOrWhiteSpace(searchProfessorSurnameToFindThesesAsCompany))
            {
                professorSurnameSuggestions.Clear();
                return;
            }

            var lookups = await CompanyDashboardService.GetLookupsAsync();
            professorSurnameSuggestions = lookups.Professors
                .Where(p => !string.IsNullOrEmpty(p.ProfSurname) &&
                            p.ProfSurname.Contains(searchProfessorSurnameToFindThesesAsCompany, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.ProfSurname)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();
        }

        private void SelectProfessorNameSuggestion(string name)
        {
            searchProfessorNameToFindThesesAsCompany = name;
            professorNameSuggestions.Clear();
            StateHasChanged();
        }

        private void SelectProfessorSurnameSuggestion(string surname)
        {
            searchProfessorSurnameToFindThesesAsCompany = surname;
            professorSurnameSuggestions.Clear();
            StateHasChanged();
        }

        private async Task HandleAreaInputWhenSearchForProfessorThesesAsCompany(ChangeEventArgs e)
        {
            searchAreasInputToFindThesesAsCompany = e.Value?.ToString().Trim() ?? string.Empty;
            areaSuggestionsToFindThesesAsCompany = new List<string>();

            if (searchAreasInputToFindThesesAsCompany.Length >= 1)
            {
                try
                {
                    var lookups = await CompanyDashboardService.GetLookupsAsync();
                    var suggestionsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var area in lookups.Areas)
                    {
                        if (area.AreaName != null &&
                            area.AreaName.Contains(searchAreasInputToFindThesesAsCompany, StringComparison.OrdinalIgnoreCase))
                        {
                            suggestionsSet.Add(area.AreaName);
                        }

                        if (!string.IsNullOrEmpty(area.AreaSubFields))
                        {
                            var subfields = area.AreaSubFields
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(sub => sub.Trim())
                                .Where(sub => !string.IsNullOrEmpty(sub) &&
                                            sub.Contains(searchAreasInputToFindThesesAsCompany, StringComparison.OrdinalIgnoreCase));

                            foreach (var subfield in subfields)
                            {
                                var combination = $"{area.AreaName} - {subfield}";
                                suggestionsSet.Add(combination);
                            }
                        }
                    }

                    areaSuggestionsToFindThesesAsCompany = suggestionsSet.Take(10).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading area suggestions: {ex.Message}");
                }
            }
            StateHasChanged();
        }

        private void SelectAreaSuggestionToFindThesesAsCompany(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedAreasToFindThesesAsCompany.Contains(suggestion))
            {
                selectedAreasToFindThesesAsCompany.Add(suggestion);
                areaSuggestionsToFindThesesAsCompany.Clear();
                searchAreasInputToFindThesesAsCompany = string.Empty;
                StateHasChanged();
            }
        }

        private void RemoveSelectedAreaToFindThesesAsCompany(string area)
        {
            selectedAreasToFindThesesAsCompany.Remove(area);
            StateHasChanged();
        }

        private async Task HandleSkillInputWhenSearchForProfessorThesesAsCompany(ChangeEventArgs e)
        {
            searchSkillsInputToFindThesesAsCompany = e.Value?.ToString().Trim() ?? string.Empty;
            skillSuggestionsToFindThesesAsCompany = new List<string>();

            if (searchSkillsInputToFindThesesAsCompany.Length >= 1)
            {
                try
                {
                    var lookups = await CompanyDashboardService.GetLookupsAsync();
                    skillSuggestionsToFindThesesAsCompany = lookups.Skills
                        .Where(s => s.SkillName != null &&
                                    s.SkillName.Contains(searchSkillsInputToFindThesesAsCompany, StringComparison.OrdinalIgnoreCase))
                        .Select(s => s.SkillName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(10)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading skill suggestions: {ex.Message}");
                }
            }
            StateHasChanged();
        }

        private void SelectSkillSuggestionToFindThesesAsCompany(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedSkillsToFindThesesAsCompany.Contains(suggestion))
            {
                selectedSkillsToFindThesesAsCompany.Add(suggestion);
                skillSuggestionsToFindThesesAsCompany.Clear();
                searchSkillsInputToFindThesesAsCompany = string.Empty;
                StateHasChanged();
            }
        }

        private void RemoveSelectedSkillToFindThesesAsCompany(string skill)
        {
            selectedSkillsToFindThesesAsCompany.Remove(skill);
            StateHasChanged();
        }

        private void HandleAreasInputToFindThesesAsCompany(ChangeEventArgs e)
        {
            searchAreasInputToFindThesesAsCompany = e.Value?.ToString() ?? "";
        }

        private void HandleSkillsInputToFindThesesAsCompany(ChangeEventArgs e)
        {
            searchSkillsInputToFindThesesAsCompany = e.Value?.ToString() ?? "";
        }

        // Professor Details Modal
        private void CloseProfessorModalFromHyperlinkName()
        {
            selectedProfessorDetails = null;
            StateHasChanged();
        }

        private async Task ShowProfessorDetailsFromHyperlinkName(ProfessorThesis thesis)
        {
            var lookups = await CompanyDashboardService.GetLookupsAsync();
            selectedProfessorDetails = lookups.Professors
                .FirstOrDefault(p => p.ProfEmail != null &&
                                     p.ProfEmail.Equals(thesis.ProfessorEmailUsedToUploadThesis, StringComparison.OrdinalIgnoreCase));
            StateHasChanged();
        }

        // Edit Thesis
        private void ToggleAreasForEditCompanyThesis()
        {
            showAreasForEditCompanyThesis = !showAreasForEditCompanyThesis;
            StateHasChanged();
        }

        private void ToggleSkillsForEditCompanyThesis()
        {
            showSkillsForEditCompanyThesis = !showSkillsForEditCompanyThesis;
            StateHasChanged();
        }

        private void OnCheckedChangedForEditCompanyThesisSkills(ChangeEventArgs e, Skill skill)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
            {
                if (!SelectedSkillsToEditForCompanyThesis.Any(s => s.Id == skill.Id))
                    SelectedSkillsToEditForCompanyThesis.Add(skill);
            }
            else
            {
                SelectedSkillsToEditForCompanyThesis.RemoveAll(s => s.Id == skill.Id);
            }
            StateHasChanged();
        }

        private async Task SaveEditedCompanyThesis()
        {
            if (currentThesis == null) return;

            var thesisToUpdate = UploadedTheses.FirstOrDefault(t => t.Id == currentThesis.Id);
            if (thesisToUpdate == null)
            {
                var data = await CompanyDashboardService.LoadDashboardDataAsync();
                thesisToUpdate = data.Theses.FirstOrDefault(t => t.Id == currentThesis.Id);
            }
            if (thesisToUpdate != null)
            {
                thesisToUpdate.CompanyThesisTitle = currentThesis.CompanyThesisTitle;
                thesisToUpdate.CompanyThesisDescriptionsUploaded = currentThesis.CompanyThesisDescriptionsUploaded;

                await CompanyDashboardService.CreateOrUpdateThesisAsync(thesisToUpdate);
            }

            isModalVisibleToEditCompanyThesisDetails = false;
            currentThesis = null;
            await LoadUploadedThesesAsync();
            await ApplyFiltersAndUpdateCounts();
        }

        // Thesis Details Modal
        private void CloseModalForCompanyThesis()
        {
            isModalVisibleToShowCompanyThesisDetails = false;
            selectedCompanyThesis = null;
        }

        // Professor Details Modal (from thesis interest)
        private void CloseModalForProfessorDetails()
        {
            currentProfessorDetails = null;
        }

        // Additional Missing Properties
        private QuizManager.Models.Professor selectedProfessorToShowDetailsForInterestinCompanyEvent;
        private List<CompanyThesis> companytheses = new List<CompanyThesis>();
        private bool sendEmailsForBulkThesisAction = true;
        private int currentPage_ProfessorTheses = 1;
        private int totalPages_ProfessorTheses = 1;
        private bool isThesisDetailEyeIconModalVisibleToSeeAsCompany = false;
        private Dictionary<string, Student> studentDataCache = new Dictionary<string, Student>();
        private bool showErrorMessage = false;
        private bool isUploadedProfessorThesesVisibleAsCompany = false;
        private string CompanyThesisAttachmentErrorMessage = "";
        private bool showSlotWarningModal_ForCompanyThesis = false;
        private string slotWarningMessage_ForCompanyThesis = "";
        private bool showEmailConfirmationModalForThesisApplicants = false;

        // Methods
        private void ToggleToSearchForUploadedProfessorThesesAsCompany()
        {
            isUploadedProfessorThesesVisibleAsCompany = !isUploadedProfessorThesesVisibleAsCompany;
            StateHasChanged();
        }

        private void ShowEmailConfirmationModalForThesisApplicants(string action)
        {
            pendingBulkActionForThesisApplicants = action;
            showEmailConfirmationModalForThesisApplicants = true;
            StateHasChanged();
        }

        private void CloseEmailConfirmationModalForThesisApplicants()
        {
            showEmailConfirmationModalForThesisApplicants = false;
            StateHasChanged();
        }

        private void CloseSlotWarningModal_ForCompanyThesis()
        {
            showSlotWarningModal_ForCompanyThesis = false;
            StateHasChanged();
        }

        private void CloseProfessorDetailsModal()
        {
            selectedProfessorToShowDetailsForInterestinCompanyEvent = null;
            StateHasChanged();
        }

        private void CloseCompanyThesisEditModal()
        {
            isModalVisibleToEditCompanyThesisDetails = false;
            currentThesis = null;
            StateHasChanged();
        }

        // Missing Methods - extracted from MainLayout.razor.cs.backup
        
        private async Task ConfirmAndAcceptStudentThesisApplicationAsCompany(long companythesisId, string studentUniqueID)
        {
            var thesisObj = UploadedTheses.FirstOrDefault(t => t.RNGForThesisUploadedAsCompany == companythesisId);
            if (thesisObj == null) return;

            int acceptedCount = acceptedApplicantsCountPerThesis_ForCompanyThesis.GetValueOrDefault(companythesisId, 0);
            int availableSlots = availableSlotsPerThesis_ForCompanyThesis.GetValueOrDefault(companythesisId, thesisObj.OpenSlots_CompanyThesis);

            if (acceptedCount >= availableSlots)
            {
                await JS.InvokeVoidAsync("alert", $"Έχετε Αποδεχτεί {acceptedCount}/{availableSlots} Αιτούντες");
                return;
            }

            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", "- ΑΠΟΔΟΧΗ ΑΙΤΗΣΗΣ - \n Η ενέργεια αυτή δεν θα μπορεί να αναιρεθεί. Είστε σίγουρος/η;");
            if (isConfirmed)
            {
                var result = await CompanyDashboardService.DecideOnApplicationAsync(new ApplicationDecisionRequest
                {
                    ApplicationType = ApplicationType.Thesis,
                    ApplicationRng = companythesisId,
                    StudentUniqueId = studentUniqueID,
                    Decision = ApplicationDecision.Accept
                });
                if (!result.Success)
                {
                    await JS.InvokeVoidAsync("alert", result.Error ?? "Σφάλμα κατά την αποδοχή της αίτησης.");
                    return;
                }

                acceptedApplicantsCountPerThesis_ForCompanyThesis[companythesisId] = acceptedCount + 1;
                StateHasChanged();
            }
        }

        private async Task ConfirmAndRejectStudentThesisApplicationAsCompany(long companythesisId, string studentUniqueID)
        {
            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", "- ΑΠΟΡΡΙΨΗ ΑΙΤΗΣΗΣ - \n Η ενέργεια αυτή δεν θα μπορεί να αναιρεθεί. Είστε σίγουρος/η;");
            if (isConfirmed)
            {
                var result = await CompanyDashboardService.DecideOnApplicationAsync(new ApplicationDecisionRequest
                {
                    ApplicationType = ApplicationType.Thesis,
                    ApplicationRng = companythesisId,
                    StudentUniqueId = studentUniqueID,
                    Decision = ApplicationDecision.Reject
                });
                if (!result.Success)
                {
                    await JS.InvokeVoidAsync("alert", result.Error ?? "Σφάλμα κατά την απόρριψη της αίτησης.");
                    return;
                }

                StateHasChanged();
            }
        }

        private async Task ShowStudentDetailsInNameAsHyperlink(string studentUniqueId, int applicationId, string source)
        {
            var student = await CompanyDashboardService.GetStudentByUniqueIdAsync(studentUniqueId);
            if (student != null)
            {
                studentDataCache[student.Email] = student;
                StateHasChanged();
            }
        }

        private void ToggleThesisApplicantSelection(long thesisRng, string studentId, ChangeEventArgs e)
        {
            var key = (thesisRng, studentId);
            if ((bool)e.Value!)
            {
                selectedThesisApplicantIds.Add(key);
            }
            else
            {
                selectedThesisApplicantIds.Remove(key);
            }
            StateHasChanged();
        }

        // Edit Modal Methods
        private void OnAreaCheckedChangedForEditCompanyThesis(ChangeEventArgs e, Area area)
        {
            var isChecked = (bool)e.Value!;
            if (isChecked)
            {
                if (!SelectedAreasToEditForCompanyThesis.Contains(area))
                {
                    SelectedAreasToEditForCompanyThesis.Add(area);
                }
            }
            else
            {
                SelectedAreasToEditForCompanyThesis.Remove(area);
                if (SelectedSubFieldsForEditCompanyThesis.ContainsKey(area.AreaName))
                {
                    SelectedSubFieldsForEditCompanyThesis.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForEditCompanyThesis(Area area)
        {
            return SelectedAreasToEditForCompanyThesis.Contains(area);
        }

        private void ToggleSubFieldsForEditCompanyThesis(Area area)
        {
            if (ExpandedAreasForEditCompanyThesis.Contains(area.Id))
                ExpandedAreasForEditCompanyThesis.Remove(area.Id);
            else
                ExpandedAreasForEditCompanyThesis.Add(area.Id);
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForEditCompanyThesis(ChangeEventArgs e, Area area, string subField)
        {
            var isChecked = (bool)e.Value!;
            if (!SelectedSubFieldsForEditCompanyThesis.ContainsKey(area.AreaName))
            {
                SelectedSubFieldsForEditCompanyThesis[area.AreaName] = new HashSet<string>();
            }

            if (isChecked)
            {
                SelectedSubFieldsForEditCompanyThesis[area.AreaName].Add(subField);
            }
            else
            {
                SelectedSubFieldsForEditCompanyThesis[area.AreaName].Remove(subField);
            }
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForEditCompanyThesis(Area area, string subField)
        {
            return SelectedSubFieldsForEditCompanyThesis.ContainsKey(area.AreaName) && 
                SelectedSubFieldsForEditCompanyThesis[area.AreaName].Contains(subField);
        }

        private async Task HandleFileUploadForEditCompanyThesisAttachment(InputFileChangeEventArgs e)
        {
            // TODO: Implement file upload
            await Task.CompletedTask;
        }

        // Pagination Methods for Professor Theses
        private List<int> GetVisiblePages_ProfessorTheses_AsCompany()
        {
            var pages = new List<int>();
            int current = currentPageForProfessorTheses;
            int total = (int)Math.Ceiling((double)(professorThesesResultsToFindThesesAsCompany?.Count ?? 0) / professorThesesPerPage);
        
            pages.Add(1);
            if (current > 3) pages.Add(-1);
        
            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);
        
            for (int i = start; i <= end; i++) pages.Add(i);
        
            if (current < total - 2) pages.Add(-1);
            if (total > 1) pages.Add(total);
        
            return pages;
        }

        private void GoToFirstPage_ProfessorTheses()
        {
            currentPageForProfessorTheses = 1;
            StateHasChanged();
        }

        private void GoToLastPage_ProfessorTheses()
        {
            currentPageForProfessorTheses = (int)Math.Ceiling((double)(professorThesesResultsToFindThesesAsCompany?.Count ?? 0) / professorThesesPerPage);
            StateHasChanged();
        }

        private void PreviousPage_ProfessorTheses()
        {
            if (currentPageForProfessorTheses > 1)
            {
                currentPageForProfessorTheses--;
                StateHasChanged();
            }
        }

        private void NextPage_ProfessorTheses()
        {
            int totalPages = (int)Math.Ceiling((double)(professorThesesResultsToFindThesesAsCompany?.Count ?? 0) / professorThesesPerPage);
            if (currentPageForProfessorTheses < totalPages)
            {
                currentPageForProfessorTheses++;
                StateHasChanged();
            }
        }

        private void GoToPage_ProfessorTheses(int page)
        {
            int totalPages = (int)Math.Ceiling((double)(professorThesesResultsToFindThesesAsCompany?.Count ?? 0) / professorThesesPerPage);
            if (page >= 1 && page <= totalPages)
            {
                currentPageForProfessorTheses = page;
                StateHasChanged();
            }
        }

        private async Task DownloadProfessorThesisAttachmentWhenSearchForProfessorThesisAsCompany(int thesisId)
        {
            var attachment = await CompanyDashboardService.GetProfessorThesisAttachmentAsync(thesisId);
            if (attachment?.Data != null && attachment.Data.Length > 0)
            {
                await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile",
                    attachment.FileName, attachment.ContentType, attachment.Data);
            }
        }

        // Additional missing properties
        private int itemsPerPage_ProfessorTheses = 10;
        private int[] pageSizeOptions_SearchForProfessorThesesAsCompany = new[] { 10, 50, 100 };
        private bool showLoadingModalWhenMarkInterestInProfessorThesis = false;
        private int loadingProgressWhenMarkInterestInProfessorThesis = 0;
        private bool isModalVisibleToShowprofessorDetailsAtCompanyThesisInterest = false;
        private bool isProfessorDetailModalVisible = false;
        private bool showProfessorModal = false;
        private List<Area> SelectedAreasToEditForCompanyThesis = new List<Area>();
        private Dictionary<string, HashSet<string>> SelectedSubFieldsForEditCompanyThesis = new Dictionary<string, HashSet<string>>();
    }
}
