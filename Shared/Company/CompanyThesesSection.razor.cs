using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Helpers;
using QuizManager.Models;
using QuizManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Shared.Company
{
    public partial class CompanyThesesSection : ComponentBase
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
        private Dictionary<string, bool> expandedCompanyTheses = new Dictionary<string, bool>();
        private bool showLoadingModalForDeleteCompanyThesis = false;

        // Thesis Applicants
        private bool isLoadingCompanyThesisApplicants = false;
        private string loadingCompanyThesisId = "";
        private Dictionary<string, List<ThesisApplicant>> companyThesisApplicantsMap = new Dictionary<string, List<ThesisApplicant>>();
        private Dictionary<string, int> acceptedApplicantsCountPerThesis_ForCompanyThesis = new Dictionary<string, int>();
        private Dictionary<string, int> availableSlotsPerThesis_ForCompanyThesis = new Dictionary<string, int>();

        // Bulk Operations for Applicants
        private bool isBulkEditModeForThesisApplicants = false;
        private HashSet<(string, string)> selectedThesisApplicantIds = new HashSet<(string, string)>();
        private string pendingBulkActionForThesisApplicants = "";
        private bool sendEmailsForBulkAction = true;

        // Professor Interest in Theses
        private Dictionary<string, bool> expandedCompanyThesesForProfessorInterest = new Dictionary<string, bool>();
        private bool isLoadingCompanyThesisProfessors = false;
        private string loadingCompanyThesisProfessorId = "";
        private Dictionary<string, Professor> professorDataCache = new Dictionary<string, Professor>();

        // Search Professor Theses
        private List<ProfessorThesis> professorThesesSearchResults = new List<ProfessorThesis>();
        private int currentPageForProfessorTheses = 1;
        private int professorThesesPerPage = 10;
        private string searchAreasInputToFindThesesAsCompany = "";
        private List<Area> selectedAreasToFindThesesAsCompany = new List<Area>();
        private string searchSkillsInputToFindThesesAsCompany = "";
        private List<Skill> selectedSkillsToFindThesesAsCompany = new List<Skill>();

        // Professor Details Modal
        private Professor selectedProfessorDetails;
        private Professor currentProfessorDetails;
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
                companyData = await dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyEmail == CurrentUserEmail);
            }

            Areas = await dbContext.Areas.Include(a => a.SubFields).ToListAsync();
            Skills = await dbContext.Skills.ToListAsync();
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

        private void OnSubFieldCheckedChangedForCompanyThesis(ChangeEventArgs e, Area area, SubField subField)
        {
            bool isChecked = (bool)e.Value;
            if (!SelectedSubFieldsForCompanyThesis.ContainsKey(area.Id))
                SelectedSubFieldsForCompanyThesis[area.Id] = new HashSet<string>();

            if (isChecked)
                SelectedSubFieldsForCompanyThesis[area.Id].Add(subField.SubFieldName);
            else
                SelectedSubFieldsForCompanyThesis[area.Id].Remove(subField.SubFieldName);
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForCompanyThesis(Area area, SubField subField)
        {
            return SelectedSubFieldsForCompanyThesis.ContainsKey(area.Id) &&
                   SelectedSubFieldsForCompanyThesis[area.Id].Contains(subField.SubFieldName);
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
                    thesis.CompanyThesisAttachmentFile = null;
                    return;
                }

                const long maxFileSize = 10485760;
                if (e.File.Size > maxFileSize) return;

                using var memoryStream = new System.IO.MemoryStream();
                await e.File.OpenReadStream(maxFileSize).CopyToAsync(memoryStream);
                thesis.CompanyThesisAttachmentFile = memoryStream.ToArray();
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
                thesis.CompanyThesisAreas = string.Join(",", areasWithSubfields);

                // Build skills string
                thesis.CompanyThesisSkills = string.Join(",", SelectedSkillsWhenUploadThesisAsCompany.Select(s => s.SkillName));

                await UpdateProgress(50);

                // Set thesis properties
                thesis.RNGForThesisUploadedAsCompany = new Random().NextInt64();
                thesis.RNGForThesisUploadedAsCompany_HashedAsUniqueID = HashingHelper.HashLong(thesis.RNGForThesisUploadedAsCompany);
                thesis.CompanyEmailUsedToUploadThesis = CurrentUserEmail;
                thesis.CompanyThesisUploadedDate = DateTime.Now;
                thesis.CompanyThesisStatus = publishThesis ? "Δημοσιευμένη" : "Μη Δημοσιευμένη";

                await UpdateProgress(70);
                dbContext.CompanyTheses.Add(thesis);
                await dbContext.SaveChangesAsync();

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
            UploadedTheses = await dbContext.CompanyTheses
                .Where(t => t.CompanyEmailUsedToUploadThesis == CurrentUserEmail)
                .OrderByDescending(t => t.CompanyThesisUploadedDate)
                .ToListAsync();
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
            var thesesToUpdate = await dbContext.CompanyTheses
                .Where(t => selectedThesisIds.Contains(t.Id))
                .ToListAsync();

            foreach (var t in thesesToUpdate)
                t.CompanyThesisStatus = newStatus;

            await dbContext.SaveChangesAsync();
            CancelBulkEditForTheses();
            await LoadUploadedThesesAsync();
            await ApplyFiltersAndUpdateCounts();
        }

        private async Task ExecuteBulkCopyForTheses()
        {
            var thesesToCopy = await dbContext.CompanyTheses
                .Where(t => selectedThesisIds.Contains(t.Id))
                .ToListAsync();

            foreach (var t in thesesToCopy)
            {
                var copy = new CompanyThesis
                {
                    CompanyThesisTitle = t.CompanyThesisTitle + " (Αντίγραφο)",
                    CompanyThesisDescription = t.CompanyThesisDescription,
                    CompanyThesisAreas = t.CompanyThesisAreas,
                    CompanyThesisSkills = t.CompanyThesisSkills,
                    CompanyThesisStatus = "Μη Δημοσιευμένη",
                    CompanyEmailUsedToUploadThesis = CurrentUserEmail,
                    CompanyThesisUploadedDate = DateTime.Now,
                    RNGForThesisUploadedAsCompany = new Random().NextInt64()
                };
                copy.RNGForThesisUploadedAsCompany_HashedAsUniqueID = HashingHelper.HashLong(copy.RNGForThesisUploadedAsCompany);
                dbContext.CompanyTheses.Add(copy);
            }

            await dbContext.SaveChangesAsync();
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
                var thesesToDelete = await dbContext.CompanyTheses
                    .Where(t => selectedThesisIds.Contains(t.Id))
                    .ToListAsync();
                dbContext.CompanyTheses.RemoveRange(thesesToDelete);
                await dbContext.SaveChangesAsync();
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
                var thesisToDelete = await dbContext.CompanyTheses.FindAsync(thesisId);
                if (thesisToDelete != null)
                {
                    dbContext.CompanyTheses.Remove(thesisToDelete);
                    await dbContext.SaveChangesAsync();
                    await LoadUploadedThesesAsync();
                    await ApplyFiltersAndUpdateCounts();
                }
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
            var thesisWithAttachment = await dbContext.CompanyTheses.FindAsync(thesisId);
            if (thesisWithAttachment?.CompanyThesisAttachmentFile != null)
            {
                await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile",
                    $"thesis-attachment-{thesisId}.pdf", "application/pdf", thesisWithAttachment.CompanyThesisAttachmentFile);
            }
        }

        private void ToggleCompanyThesesExpanded(string thesisHashedId)
        {
            if (expandedCompanyTheses.ContainsKey(thesisHashedId))
                expandedCompanyTheses[thesisHashedId] = !expandedCompanyTheses[thesisHashedId];
            else
                expandedCompanyTheses[thesisHashedId] = true;
            StateHasChanged();
        }

        private void EditCompanyThesisDetails(CompanyThesis thesisToEdit)
        {
            currentThesis = new CompanyThesis
            {
                Id = thesisToEdit.Id,
                CompanyThesisTitle = thesisToEdit.CompanyThesisTitle,
                CompanyThesisDescription = thesisToEdit.CompanyThesisDescription,
                CompanyThesisStatus = thesisToEdit.CompanyThesisStatus,
                CompanyThesisActivePeriod = thesisToEdit.CompanyThesisActivePeriod
            };
            isModalVisibleToEditCompanyThesisDetails = true;
        }

        private async Task UpdateThesisStatusAsCompany(int thesisId, string newStatus)
        {
            var thesisToUpdate = await dbContext.CompanyTheses.FindAsync(thesisId);
            if (thesisToUpdate != null)
            {
                thesisToUpdate.CompanyThesisStatus = newStatus;
                await dbContext.SaveChangesAsync();
                await LoadUploadedThesesAsync();
                await ApplyFiltersAndUpdateCounts();
            }
        }

        private async Task ChangeCompanyThesisStatusToUnpublished(int thesisId)
        {
            await UpdateThesisStatusAsCompany(thesisId, "Μη Δημοσιευμένη");
        }

        // Thesis Applicants
        private async Task LoadThesisApplicants(string thesisHashedId)
        {
            loadingCompanyThesisId = thesisHashedId;
            isLoadingCompanyThesisApplicants = true;
            StateHasChanged();

            try
            {
                var applicants = await dbContext.ThesisApplicants
                    .Where(a => a.RNGForThesisPosition_HashedAsUniqueID == thesisHashedId)
                    .OrderByDescending(a => a.ApplicationDate)
                    .ToListAsync();

                companyThesisApplicantsMap[thesisHashedId] = applicants;

                var acceptedCount = applicants.Count(a => a.ThesisApplicantStatus == "Αποδεκτή");
                acceptedApplicantsCountPerThesis_ForCompanyThesis[thesisHashedId] = acceptedCount;

                var thesis = UploadedTheses.FirstOrDefault(t => t.RNGForThesisUploadedAsCompany_HashedAsUniqueID == thesisHashedId);
                if (thesis != null)
                    availableSlotsPerThesis_ForCompanyThesis[thesisHashedId] = thesis.CompanyThesisOpenSlots - acceptedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading thesis applicants: {ex.Message}");
            }
            finally
            {
                isLoadingCompanyThesisApplicants = false;
                loadingCompanyThesisId = "";
                StateHasChanged();
            }
        }

        private void EnableBulkEditModeForThesisApplicants(string thesisHashedId)
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

            CancelBulkEditForThesisApplicants();
        }

        // Professor Interest in Theses
        private void ToggleCompanyThesesExpandedForProfessorInterest(string thesisHashedId)
        {
            if (expandedCompanyThesesForProfessorInterest.ContainsKey(thesisHashedId))
                expandedCompanyThesesForProfessorInterest[thesisHashedId] = !expandedCompanyThesesForProfessorInterest[thesisHashedId];
            else
                expandedCompanyThesesForProfessorInterest[thesisHashedId] = true;
            StateHasChanged();
        }

        private async Task ShowProfessorDetailsAtCompanyThesisInterest(CompanyThesis thesisForInterest)
        {
            // Implementation for showing professor interest
            StateHasChanged();
        }

        // Search Professor Theses
        private IEnumerable<ProfessorThesis> GetPaginatedProfessorTheses_AsCompany()
        {
            return professorThesesSearchResults?
                .Skip((currentPageForProfessorTheses - 1) * professorThesesPerPage)
                .Take(professorThesesPerPage)
                ?? Enumerable.Empty<ProfessorThesis>();
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
            selectedProfessorDetails = await dbContext.Professors
                .FirstOrDefaultAsync(p => p.ProfEmail == thesis.ProfessorEmailUsedToUploadThesis);
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

            var thesisToUpdate = await dbContext.CompanyTheses.FindAsync(currentThesis.Id);
            if (thesisToUpdate != null)
            {
                thesisToUpdate.CompanyThesisTitle = currentThesis.CompanyThesisTitle;
                thesisToUpdate.CompanyThesisDescription = currentThesis.CompanyThesisDescription;
                thesisToUpdate.CompanyThesisActivePeriod = currentThesis.CompanyThesisActivePeriod;

                await dbContext.SaveChangesAsync();
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
    }
}

