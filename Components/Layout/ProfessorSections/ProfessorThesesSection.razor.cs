using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.Services.ProfessorDashboard;
using QuizManager.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.ProfessorSections
{
    public partial class ProfessorThesesSection : ComponentBase
    {
        [Inject] private IProfessorDashboardService ProfessorDashboardService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private ProfessorDashboardData dashboardData = ProfessorDashboardData.Empty;
        private IReadOnlyList<ProfessorThesisApplied> thesisApplications = Array.Empty<ProfessorThesisApplied>();
        private ProfessorDashboardLookups lookups = ProfessorDashboardLookups.Empty;

        // User Information
        private string CurrentUserEmail = "";
        private string? professorName;
        private string? professorSurname;
        private string? professorDepartment;

        // Form Visibility
        private bool isProfessorThesisFormVisible = false;

        // Thesis Model
        private ProfessorThesis professorthesis = new ProfessorThesis();
        private bool showErrorMessageforUploadingThesisAsProfessor = false;
        private int remainingCharactersInThesisFieldUploadAsProfessor = 120;
        private int remainingCharactersInThesisDescriptionUploadAsProfessor = 1000;
        private string ThesisAttachmentErrorMessage = string.Empty;
        private bool showLoadingModalForProfessorThesis = false;
        private int loadingProgress = 0;
        private bool isFormValidToSaveThesisAsProfessor = true;
        private string saveThesisAsProfessorMessage = string.Empty;
        private bool isSaveThesisAsProfessorSuccessful = false;

        // Areas and Skills Data
        private List<Area> Areas = new List<Area>();
        private List<Skill> Skills = new List<Skill>();

        // Areas Selection
        private bool showCheckboxesForProfessorThesis = false;
        private List<Area> selectedThesisAreasForProfessor = new List<Area>();
        private Dictionary<string, List<string>> SelectedSubFieldsForProfessorThesis = new Dictionary<string, List<string>>();
        private HashSet<int> ExpandedAreasForProfessorThesis = new HashSet<int>();

        // Skills Selection
        private bool showCheckboxesForThesisSkillsAsProfessor = false;
        private List<Skill> selectedThesisSkillsForProfessor = new List<Skill>();

        // Uploaded Theses Management
        private bool isUploadedThesesVisibleAsProfessor = false;
        private bool isLoadingUploadedThesesAsProfessor = false;
        private List<ProfessorThesis> UploadedThesesAsProfessor = new List<ProfessorThesis>();
        private List<ProfessorThesis> FilteredThesesAsProfessor = new List<ProfessorThesis>();
        private string selectedStatusFilterForThesesAsProfessor = "Όλα";
        private int[] pageSizeOptions_SeeMyUploadedThesesAsProfessor = new[] { 10, 50, 100 };
        private int ProfessorThesesPerPage = 10;
        private int currentPageForProfessorTheses = 1;
        private int totalCountThesesAsProfessor = 0;
        private int publishedCountThesesAsProfessor = 0;
        private int unpublishedCountThesesAsProfessor = 0;
        private int withdrawnCountThesesAsProfessor = 0;

        // Edit Modal
        private bool isEditModalVisibleForThesesAsProfessor = false;
        private ProfessorThesis currentThesisAsProfessor = new ProfessorThesis();
        private List<Area> SelectedAreasToEditForProfessorThesis = new List<Area>();
        private Dictionary<string, List<string>> SelectedSubFieldsForEditProfessorThesis = new Dictionary<string, List<string>>();
        private HashSet<int> ExpandedAreasForEditProfessorThesis = new HashSet<int>();
        private List<Skill> SelectedSkillsToEditForProfessorThesis = new List<Skill>();

        // Bulk Operations
        private bool isBulkEditModeForProfessorTheses = false;
        private HashSet<int> selectedProfessorThesisIds = new HashSet<int>();
        private string bulkActionForProfessorTheses = "";
        private bool showBulkActionModalForProfessorTheses = false;
        private List<ProfessorThesis> selectedProfessorThesesForAction = new List<ProfessorThesis>();
        private string newStatusForBulkActionForProfessorTheses = "Μη Δημοσιευμένη";
        private bool showLoadingModalForDeleteProfessorThesis = false;

        // Computed Properties
        private int totalPagesForProfessorTheses =>
            (int)Math.Ceiling((double)(GetFilteredProfessorTheses()?.Count() ?? 0) / ProfessorThesesPerPage);

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            CurrentUserEmail = user.FindFirst("name")?.Value ?? "";

            if (!string.IsNullOrEmpty(CurrentUserEmail))
            {
                var professor = await ProfessorDashboardService.GetProfessorProfileAsync();
                if (professor != null)
                {
                    professorName = professor.ProfName;
                    professorSurname = professor.ProfSurname;
                    professorDepartment = professor.ProfDepartment;
                }
            }

            dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
            thesisApplications = await ProfessorDashboardService.GetThesisApplicationsAsync();
            lookups = await ProfessorDashboardService.GetLookupsAsync();
            Areas = lookups.Areas.ToList();
            Skills = lookups.Skills.ToList();
        }

        // Form Visibility Toggle
        private void ToggleFormVisibilityForUploadProfessorThesis()
        {
            isProfessorThesisFormVisible = !isProfessorThesisFormVisible;
            StateHasChanged();
        }

        // Character Limit Methods
        private void CheckCharacterLimitInThesisFieldUploadAsProfessor(ChangeEventArgs e)
        {
            var inputText = e.Value?.ToString() ?? string.Empty;
            remainingCharactersInThesisFieldUploadAsProfessor = 120 - inputText.Length;
            StateHasChanged();
        }

        private void CheckCharacterLimitInThesisDescriptionUploadAsProfessor(ChangeEventArgs e)
        {
            var inputText = e.Value?.ToString() ?? string.Empty;
            remainingCharactersInThesisDescriptionUploadAsProfessor = 1000 - inputText.Length;
            StateHasChanged();
        }

        // File Upload Method
        private async Task HandleFileSelectedForThesisAttachmentAsProfessor(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;

                if (file == null)
                {
                    professorthesis.ThesisAttachment = null;
                    ThesisAttachmentErrorMessage = null;
                    return;
                }

                if (file.ContentType != "application/pdf")
                {
                    ThesisAttachmentErrorMessage = "Λάθος τύπος αρχείου. Επιλέξτε αρχείο PDF.";
                    professorthesis.ThesisAttachment = null;
                    return;
                }

                const long maxFileSize = 10485760;
                if (file.Size > maxFileSize)
                {
                    ThesisAttachmentErrorMessage = "Το αρχείο είναι πολύ μεγάλο. Μέγιστο μέγεθος: 10MB";
                    professorthesis.ThesisAttachment = null;
                    return;
                }

                using var ms = new MemoryStream();
                await file.OpenReadStream(maxFileSize).CopyToAsync(ms);
                professorthesis.ThesisAttachment = ms.ToArray();

                ThesisAttachmentErrorMessage = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading professor thesis attachment: {ex.Message}");
                ThesisAttachmentErrorMessage = "Προέκυψε ένα σφάλμα κατά την μεταφόρτωση του αρχείου.";
                professorthesis.ThesisAttachment = null;
            }
        }

        // Areas Selection Methods
        private void ToggleCheckboxesForProfessorThesis()
        {
            showCheckboxesForProfessorThesis = !showCheckboxesForProfessorThesis;
            StateHasChanged();
        }

        private void OnAreaCheckedChangedForProfessorThesis(ChangeEventArgs e, Area area)
        {
            var isChecked = (bool)(e.Value ?? false);

            if (isChecked)
            {
                if (!selectedThesisAreasForProfessor.Contains(area))
                {
                    selectedThesisAreasForProfessor.Add(area);
                }
            }
            else
            {
                selectedThesisAreasForProfessor.Remove(area);

                // Remove all subfields for this area when area is deselected
                if (SelectedSubFieldsForProfessorThesis.ContainsKey(area.AreaName))
                {
                    SelectedSubFieldsForProfessorThesis.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForProfessorThesis(Area area)
        {
            return selectedThesisAreasForProfessor.Contains(area);
        }

        private void ToggleSubFieldsForProfessorThesis(Area area)
        {
            if (ExpandedAreasForProfessorThesis.Contains(area.Id))
            {
                ExpandedAreasForProfessorThesis.Remove(area.Id);
            }
            else
            {
                ExpandedAreasForProfessorThesis.Add(area.Id);
            }
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForProfessorThesis(ChangeEventArgs e, Area area, string subField)
        {
            var isChecked = (bool)(e.Value ?? false);

            if (!SelectedSubFieldsForProfessorThesis.ContainsKey(area.AreaName))
            {
                SelectedSubFieldsForProfessorThesis[area.AreaName] = new List<string>();
            }

            if (isChecked)
            {
                if (!SelectedSubFieldsForProfessorThesis[area.AreaName].Contains(subField))
                {
                    SelectedSubFieldsForProfessorThesis[area.AreaName].Add(subField);
                }
            }
            else
            {
                SelectedSubFieldsForProfessorThesis[area.AreaName].Remove(subField);

                // Remove the area from subfields dictionary if no subfields are selected
                if (!SelectedSubFieldsForProfessorThesis[area.AreaName].Any())
                {
                    SelectedSubFieldsForProfessorThesis.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForProfessorThesis(Area area, string subField)
        {
            return SelectedSubFieldsForProfessorThesis.ContainsKey(area.AreaName) &&
                SelectedSubFieldsForProfessorThesis[area.AreaName].Contains(subField);
        }

        private bool HasAnySelectionForProfessorThesis()
        {
            return selectedThesisAreasForProfessor.Any() || SelectedSubFieldsForProfessorThesis.Any();
        }

        // Skills Selection Methods
        private void ToggleCheckboxesForThesisSkillsAsProfessor()
        {
            showCheckboxesForThesisSkillsAsProfessor = !showCheckboxesForThesisSkillsAsProfessor;
            StateHasChanged();
        }

        private void OnCheckedChangedForThesisSkillsAsProfessor(ChangeEventArgs e, Skill skill)
        {
            if ((bool)(e.Value ?? false)) // If checked
            {
                if (!selectedThesisSkillsForProfessor.Contains(skill))
                {
                    selectedThesisSkillsForProfessor.Add(skill);
                }
            }
            else // If unchecked
            {
                selectedThesisSkillsForProfessor.Remove(skill);
            }
            StateHasChanged();
        }

        private bool IsSelectedForThesisSkillsAsProfessor(Skill skill)
        {
            return selectedThesisSkillsForProfessor.Contains(skill);
        }

        // Save Thesis Methods
        private async Task SaveThesisAsPublishedAsProfessor()
        {
            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] {
                $"Πρόκεται να Δημιουργήσετε μια Διπλωματική Εργασία με Τίτλο: <strong>{professorthesis.ThesisTitle}</strong> ως '<strong>Δημοσιευμένη</strong>'.<br><br>" +
                "<strong style='color: red;'>Είστε σίγουρος/η;</strong>"
            });

            if (!isConfirmed) return;

            // Build areas and subfields string
            var areasWithSubfields = new List<string>();

            // Add selected areas
            foreach (var area in selectedThesisAreasForProfessor)
            {
                areasWithSubfields.Add(area.AreaName);
            }

            // Add subfields
            foreach (var areaSubFields in SelectedSubFieldsForProfessorThesis)
            {
                var subFields = areaSubFields.Value;
                foreach (var subField in subFields)
                {
                    areasWithSubfields.Add(subField);
                }
            }

            // Initialize new thesis
            var newThesis = new ProfessorThesis
            {
                ThesisTitle = professorthesis.ThesisTitle,
                ThesisDescription = professorthesis.ThesisDescription,
                ThesisAttachment = professorthesis.ThesisAttachment,
                ThesisStatus = "Δημοσιευμένη",
                ThesisUploadDateTime = DateTime.Now,
                ThesisActivePeriod = professorthesis.ThesisActivePeriod,
                ProfessorEmailUsedToUploadThesis = CurrentUserEmail,
                RNGForThesisUploaded = new Random().NextInt64(),
                RNGForThesisUploaded_HashedAsUniqueID = HashingHelper.HashLong(new Random().NextInt64()),
                ThesisAreas = string.Join(",", areasWithSubfields),
                ThesisSkills = string.Join(",", selectedThesisSkillsForProfessor.Select(s => s.SkillName)),
                ThesisType = ThesisType.Professor,
                IsCompanyInteresetedInProfessorThesis = false,
                IsCompanyInterestedInProfessorThesisStatus = "Δεν έχει γίνει Αποδοχή",
                ThesisUpdateDateTime = DateTime.Now,
                OpenSlots_ProfessorThesis = professorthesis.OpenSlots_ProfessorThesis
            };

            professorthesis = newThesis;
            await SaveThesisToDatabaseAsProfessor();
        }

        private async Task SaveThesisAsUnpublishedAsProfessor()
        {
            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] {
                $"Πρόκεται να Δημιουργήσετε μια Διπλωματική Εργασία με Τίτλο: <strong>{professorthesis.ThesisTitle}</strong> ως '<strong>Μη Δημοσιευμένη</strong>'.<br><br>" +
                "<strong style='color: red;'>Είστε σίγουρος/η;</strong>"
            });

            if (!isConfirmed) return;

            // Build areas and subfields string
            var areasWithSubfields = new List<string>();

            // Add selected areas
            foreach (var area in selectedThesisAreasForProfessor)
            {
                areasWithSubfields.Add(area.AreaName);
            }

            // Add subfields
            foreach (var areaSubFields in SelectedSubFieldsForProfessorThesis)
            {
                var subFields = areaSubFields.Value;
                foreach (var subField in subFields)
                {
                    areasWithSubfields.Add(subField);
                }
            }

            var newThesis = new ProfessorThesis
            {
                ThesisTitle = professorthesis.ThesisTitle,
                ThesisDescription = professorthesis.ThesisDescription,
                ThesisAttachment = professorthesis.ThesisAttachment,
                ThesisStatus = "Μη Δημοσιευμένη",
                ThesisUploadDateTime = DateTime.Now,
                ThesisActivePeriod = professorthesis.ThesisActivePeriod,
                ProfessorEmailUsedToUploadThesis = CurrentUserEmail,
                RNGForThesisUploaded = new Random().NextInt64(),
                RNGForThesisUploaded_HashedAsUniqueID = HashingHelper.HashLong(new Random().NextInt64()),
                ThesisAreas = string.Join(",", areasWithSubfields),
                ThesisSkills = string.Join(",", selectedThesisSkillsForProfessor.Select(s => s.SkillName)),
                ThesisType = ThesisType.Professor,
                IsCompanyInteresetedInProfessorThesis = false,
                IsCompanyInterestedInProfessorThesisStatus = "Δεν έχει γίνει Αποδοχή",
                ThesisUpdateDateTime = DateTime.Now,
                OpenSlots_ProfessorThesis = professorthesis.OpenSlots_ProfessorThesis
            };

            professorthesis = newThesis;
            await SaveThesisToDatabaseAsProfessor();
        }

        private async Task SaveThesisToDatabaseAsProfessor()
        {
            showLoadingModalForProfessorThesis = true;
            loadingProgress = 0;
            showErrorMessageforUploadingThesisAsProfessor = false;
            isSaveThesisAsProfessorSuccessful = false;
            StateHasChanged();

            try
            {
                await UpdateProgressWhenSaveThesisAsProfessor(20);
                if (string.IsNullOrWhiteSpace(professorthesis.ThesisTitle) ||
                    string.IsNullOrWhiteSpace(professorthesis.ThesisDescription) ||
                    !HasAnySelectionForProfessorThesis() ||
                    professorthesis.ThesisActivePeriod.Date <= DateTime.Today ||
                    professorthesis.OpenSlots_ProfessorThesis < 3)
                {
                    await HandleProfessorThesisValidationErrorWhenSaveThesisAsProfessor();
                    return;
                }
                await UpdateProgressWhenSaveThesisAsProfessor(40);

                await UpdateProgressWhenSaveThesisAsProfessor(60);
                professorthesis.ProfessorEmailUsedToUploadThesis = CurrentUserEmail;
                if (professorthesis.Professor == null)
                {
                    professorthesis.Professor = new Professor
                    {
                        ProfEmail = CurrentUserEmail,
                        ProfName = professorName,
                        ProfSurname = professorSurname,
                        ProfDepartment = professorDepartment
                    };
                }
                professorthesis.ThesisUpdateDateTime = DateTime.Now;

                await UpdateProgressWhenSaveThesisAsProfessor(80);
                var result = await ProfessorDashboardService.CreateOrUpdateThesisAsync(professorthesis);
                if (!result.Success)
                {
                    throw new InvalidOperationException(result.Error ?? "Failed to save thesis.");
                }
                await ProfessorDashboardService.RefreshDashboardCacheAsync();
                dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
                await UpdateProgressWhenSaveThesisAsProfessor(100);

                isSaveThesisAsProfessorSuccessful = true;
                saveThesisAsProfessorMessage = "Η Διπλωματική Εργασία Δημιουργήθηκε Επιτυχώς";

                await Task.Delay(500);

                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            catch (Exception ex)
            {
                showLoadingModalForProfessorThesis = false;
                isSaveThesisAsProfessorSuccessful = false;
                saveThesisAsProfessorMessage = "Κάποιο πρόβλημα παρουσιάστηκε με την Δημιουργία της Διπλωματικής Εργασίας! Ανανεώστε την σελίδα και προσπαθήστε ξανά";
                Console.WriteLine($"Πρόβλημα Δημιουργίας/Αποθήκευσης: {ex.Message}\n{ex.StackTrace}");
                StateHasChanged();
            }
        }

        private async Task HandleProfessorThesisValidationErrorWhenSaveThesisAsProfessor()
        {
            showLoadingModalForProfessorThesis = false;
            showErrorMessageforUploadingThesisAsProfessor = true;
            StateHasChanged();

            await JS.InvokeVoidAsync("scrollToTop");
        }

        private async Task UpdateProgressWhenSaveThesisAsProfessor(int progress)
        {
            loadingProgress = progress;
            StateHasChanged();
            await Task.Delay(50);
        }

        // Uploaded Theses Management
        private async Task ToggleUploadedThesesVisibilityAsProfessor()
        {
            isUploadedThesesVisibleAsProfessor = !isUploadedThesesVisibleAsProfessor;

            if (isUploadedThesesVisibleAsProfessor)
            {
                isLoadingUploadedThesesAsProfessor = true;
                StateHasChanged();

                try
                {
                    await LoadUploadedThesesAsProfessorAsync();
                    await CalculateStatusCountsForThesesAsProfessor();
                }
                finally
                {
                    isLoadingUploadedThesesAsProfessor = false;
                    StateHasChanged();
                }
            }
            else
            {
                StateHasChanged();
            }
        }

        private async Task LoadUploadedThesesAsProfessorAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentUserEmail))
                {
                    dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
                    UploadedThesesAsProfessor = dashboardData.Theses
                        .Where(t => t.ProfessorEmailUsedToUploadThesis == CurrentUserEmail)
                        .OrderByDescending(t => t.ThesisUploadDateTime)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading professor theses: {ex.Message}");
            }
            finally
            {
                StateHasChanged();
            }
        }

        private async Task CalculateStatusCountsForThesesAsProfessor()
        {
            await LoadUploadedThesesAsProfessorAsync();

            totalCountThesesAsProfessor = UploadedThesesAsProfessor.Count();
            publishedCountThesesAsProfessor = UploadedThesesAsProfessor.Count(i => i.ThesisStatus == "Δημοσιευμένη");
            unpublishedCountThesesAsProfessor = UploadedThesesAsProfessor.Count(i => i.ThesisStatus == "Μη Δημοσιευμένη");
            withdrawnCountThesesAsProfessor = UploadedThesesAsProfessor.Count(i => i.ThesisStatus == "Αποσυρμένη");

            StateHasChanged();
        }

        private void FilterThesesAsProfessor()
        {
            if (selectedStatusFilterForThesesAsProfessor == "Όλα")
            {
                FilteredThesesAsProfessor = UploadedThesesAsProfessor;
            }
            else
            {
                FilteredThesesAsProfessor = UploadedThesesAsProfessor
                    .Where(a => a.ThesisStatus == selectedStatusFilterForThesesAsProfessor)
                    .ToList();
            }

            totalCountThesesAsProfessor = UploadedThesesAsProfessor.Count;
            publishedCountThesesAsProfessor = UploadedThesesAsProfessor
                .Count(a => a.ThesisStatus == "Δημοσιευμένη");
            unpublishedCountThesesAsProfessor = UploadedThesesAsProfessor
                .Count(a => a.ThesisStatus == "Μη Δημοσιευμένη");
            withdrawnCountThesesAsProfessor = UploadedThesesAsProfessor
                .Count(a => a.ThesisStatus == "Αποσυρμένη");

            StateHasChanged();
        }

        private void HandleStatusFilterChangeForThesesAsProfessor(ChangeEventArgs e)
        {
            selectedStatusFilterForThesesAsProfessor = e.Value?.ToString() ?? "Όλα";
            FilterThesesAsProfessor();
        }

        private void OnPageSizeChange_SeeMyUploadedThesesAsProfessor(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                ProfessorThesesPerPage = newSize;
                currentPageForProfessorTheses = 1;
                StateHasChanged();
            }
        }

        // Pagination Methods
        private IEnumerable<ProfessorThesis> GetFilteredProfessorTheses()
        {
            return FilteredThesesAsProfessor?
                .Where(j => selectedStatusFilterForThesesAsProfessor == "Όλα" || j.ThesisStatus == selectedStatusFilterForThesesAsProfessor)
                ?? Enumerable.Empty<ProfessorThesis>();
        }

        private IEnumerable<ProfessorThesis> GetPaginatedProfessorTheses()
        {
            return GetFilteredProfessorTheses()
                .Skip((currentPageForProfessorTheses - 1) * ProfessorThesesPerPage)
                .Take(ProfessorThesesPerPage);
        }

        private List<int> GetVisiblePagesForProfessorTheses()
        {
            var pages = new List<int>();
            int current = currentPageForProfessorTheses;
            int total = totalPagesForProfessorTheses;

            pages.Add(1);
            if (current > 3) pages.Add(-1);

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);
            for (int i = start; i <= end; i++) pages.Add(i);

            if (current < total - 2) pages.Add(-1);
            if (total > 1) pages.Add(total);

            return pages;
        }

        private void GoToFirstPageForProfessorTheses()
        {
            currentPageForProfessorTheses = 1;
            StateHasChanged();
        }

        private void GoToLastPageForProfessorTheses()
        {
            currentPageForProfessorTheses = totalPagesForProfessorTheses;
            StateHasChanged();
        }

        private void PreviousPageForProfessorTheses()
        {
            if (currentPageForProfessorTheses > 1)
            {
                currentPageForProfessorTheses--;
                StateHasChanged();
            }
        }

        private void NextPageForProfessorTheses()
        {
            if (currentPageForProfessorTheses < totalPagesForProfessorTheses)
            {
                currentPageForProfessorTheses++;
                StateHasChanged();
            }
        }

        private void GoToPageForProfessorTheses(int page)
        {
            if (page > 0 && page <= totalPagesForProfessorTheses)
            {
                currentPageForProfessorTheses = page;
                StateHasChanged();
            }
        }

        // Edit Modal Methods
        private void OpenEditModalForThesisAsProfessor(ProfessorThesis professorthesis)
        {
            try
            {
                currentThesisAsProfessor = new ProfessorThesis
                {
                    Id = professorthesis.Id,
                    ThesisTitle = professorthesis.ThesisTitle,
                    ThesisDescription = professorthesis.ThesisDescription,
                    ThesisType = professorthesis.ThesisType,
                    ThesisStatus = professorthesis.ThesisStatus,
                    ThesisActivePeriod = professorthesis.ThesisActivePeriod,
                    ThesisAreas = professorthesis.ThesisAreas,
                    ThesisSkills = professorthesis.ThesisSkills,
                    ThesisAttachment = professorthesis.ThesisAttachment,
                    ProfessorEmailUsedToUploadThesis = professorthesis.ProfessorEmailUsedToUploadThesis,
                    ThesisUploadDateTime = professorthesis.ThesisUploadDateTime,
                    ThesisUpdateDateTime = professorthesis.ThesisUpdateDateTime,
                    ThesisTimesUpdated = professorthesis.ThesisTimesUpdated,
                    OpenSlots_ProfessorThesis = professorthesis.OpenSlots_ProfessorThesis,
                    Professor = professorthesis.Professor != null ? new Professor
                    {
                        ProfName = professorthesis.Professor.ProfName,
                        ProfSurname = professorthesis.Professor.ProfSurname,
                        ProfDepartment = professorthesis.Professor.ProfDepartment
                    } : null
                };

                SelectedAreasToEditForProfessorThesis.Clear();
                SelectedSubFieldsForEditProfessorThesis.Clear();
                ExpandedAreasForEditProfessorThesis.Clear();

                InitializeAreaSelectionsForEditProfessorThesis(professorthesis);

                SelectedSkillsToEditForProfessorThesis = new List<Skill>();
                if (!string.IsNullOrEmpty(professorthesis.ThesisSkills))
                {
                    var currentSkills = professorthesis.ThesisSkills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s));

                    SelectedSkillsToEditForProfessorThesis = Skills
                        .Where(skill => currentSkills.Contains(skill.SkillName))
                        .ToList();
                }

                isEditModalVisibleForThesesAsProfessor = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening edit modal: {ex.Message}");
            }
            finally
            {
                StateHasChanged();
            }
        }

        private void InitializeAreaSelectionsForEditProfessorThesis(ProfessorThesis thesis)
        {
            if (!string.IsNullOrEmpty(thesis.ThesisAreas))
            {
                var selectedItems = thesis.ThesisAreas.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .ToHashSet();

                foreach (var area in Areas)
                {
                    if (selectedItems.Contains(area.AreaName))
                    {
                        SelectedAreasToEditForProfessorThesis.Add(area);
                    }

                    if (!string.IsNullOrEmpty(area.AreaSubFields))
                    {
                        var subFields = area.AreaSubFields.Split(',')
                            .Select(s => s.Trim())
                            .ToList();

                        bool hasSelectedSubfields = false;

                        foreach (var subField in subFields)
                        {
                            if (selectedItems.Contains(subField))
                            {
                                if (!SelectedSubFieldsForEditProfessorThesis.ContainsKey(area.AreaName))
                                {
                                    SelectedSubFieldsForEditProfessorThesis[area.AreaName] = new List<string>();
                                }
                                if (!SelectedSubFieldsForEditProfessorThesis[area.AreaName].Contains(subField))
                                {
                                    SelectedSubFieldsForEditProfessorThesis[area.AreaName].Add(subField);
                                    hasSelectedSubfields = true;
                                }
                            }
                        }

                        if (hasSelectedSubfields)
                        {
                            ExpandedAreasForEditProfessorThesis.Add(area.Id);
                        }
                    }
                }
            }
        }

        private void CloseEditModalForThesesAsProfessor()
        {
            isEditModalVisibleForThesesAsProfessor = false;
            StateHasChanged();
        }

        private async Task HandleFileUploadToEditProfessorThesisAttachment(InputFileChangeEventArgs e)
        {
            if (currentThesisAsProfessor == null || e.File == null)
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
                currentThesisAsProfessor.ThesisAttachment = memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error uploading thesis attachment: {ex.Message}");
                await JS.InvokeVoidAsync("alert", "Προέκυψε ένα σφάλμα κατά την μεταφόρτωση του αρχείου.");
            }
        }

        private async Task UpdateThesisAsProfessor(ProfessorThesis updatedThesisProfessor)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(currentThesisAsProfessor.ThesisTitle) ||
                    string.IsNullOrWhiteSpace(currentThesisAsProfessor.ThesisDescription))
                {
                    await JS.InvokeVoidAsync("alert", "Παρακαλώ συμπληρώστε όλα τα απαραίτητα πεδία");
                    return;
                }

                var areasWithSubfields = new List<string>();

                foreach (var area in SelectedAreasToEditForProfessorThesis)
                {
                    areasWithSubfields.Add(area.AreaName);
                }

                foreach (var areaSubFields in SelectedSubFieldsForEditProfessorThesis)
                {
                    var subFields = areaSubFields.Value;
                    foreach (var subField in subFields)
                    {
                        areasWithSubfields.Add(subField);
                    }
                }

                currentThesisAsProfessor.ThesisAreas = string.Join(",", areasWithSubfields);

                SelectedSkillsToEditForProfessorThesis ??= new List<Skill>();
                if (!SelectedSkillsToEditForProfessorThesis.Any())
                {
                    var currentSkills = currentThesisAsProfessor.ThesisSkills?
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList() ?? new List<string>();

                    SelectedSkillsToEditForProfessorThesis = Skills
                        .Where(skill => currentSkills.Contains(skill.SkillName))
                        .ToList();
                }

                currentThesisAsProfessor.ThesisSkills = string.Join(",",
                    SelectedSkillsToEditForProfessorThesis.Select(s => s.SkillName));

                currentThesisAsProfessor.ThesisUpdateDateTime = DateTime.Now;
                currentThesisAsProfessor.ThesisTimesUpdated++;

                var result = await ProfessorDashboardService.CreateOrUpdateThesisAsync(currentThesisAsProfessor);
                if (!result.Success)
                {
                    throw new InvalidOperationException(result.Error ?? "Αποτυχία αποθήκευσης διπλωματικής.");
                }

                await ProfessorDashboardService.RefreshDashboardCacheAsync();
                dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
                UploadedThesesAsProfessor = dashboardData.Theses
                    .Where(t => t.ProfessorEmailUsedToUploadThesis == CurrentUserEmail)
                    .OrderByDescending(t => t.ThesisUploadDateTime)
                    .ToList();

                isEditModalVisibleForThesesAsProfessor = false;
                currentThesisAsProfessor = null;
                await LoadUploadedThesesAsProfessorAsync();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error saving professor thesis: {ex.Message}");
                await JS.InvokeVoidAsync("alert", "Σφάλμα κατά την αποθήκευση της Διπλωματικής");
            }
        }

        // Status Change Methods
        private async Task ChangeThesisStatusAsProfessor(int professorthesisId, string professorthesisnewStatus)
        {
            var isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[]
            {
                $"Πρόκειται να αλλάξετε την κατάσταση αυτής της Διπλωματικής σε '{professorthesisnewStatus}'. Είστε σίγουρος/η;"
            });

            if (isConfirmed)
            {
                var professorthesis = UploadedThesesAsProfessor.FirstOrDefault(a => a.Id == professorthesisId);
                if (professorthesis != null)
                {
                    var result = await ProfessorDashboardService.UpdateThesisStatusAsync(professorthesisId, professorthesisnewStatus);
                    if (!result.Success)
                    {
                        throw new InvalidOperationException(result.Error ?? "Αποτυχία αλλαγής κατάστασης.");
                    }
                    await ProfessorDashboardService.RefreshDashboardCacheAsync();
                    dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
                    UploadedThesesAsProfessor = dashboardData.Theses
                        .Where(t => t.ProfessorEmailUsedToUploadThesis == CurrentUserEmail)
                        .OrderByDescending(t => t.ThesisUploadDateTime)
                        .ToList();
                    await LoadUploadedThesesAsProfessorAsync();
                    await CalculateStatusCountsForThesesAsProfessor();
                }
                StateHasChanged();
            }
        }

        // Delete Method
        private async Task DeleteThesisAsProfessor(int professorThesisId)
        {
            var isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML",
                "Πρόκειται να διαγράψετε οριστικά αυτή την Διπλωματική Εργασία.<br><br>" +
                "<strong style='color: red;'>Είστε σίγουρος/η;</strong>");

            if (isConfirmed)
            {
                showLoadingModalForDeleteProfessorThesis = true;
                loadingProgress = 0;
                StateHasChanged();

                try
                {
                    await UpdateProgressWhenDeleteThesisAsProfessor(30);
                    var deleted = await ProfessorDashboardService.DeleteThesisAsync(professorThesisId);

                    if (deleted)
                    {
                        await UpdateProgressWhenDeleteThesisAsProfessor(60);
                        await ProfessorDashboardService.RefreshDashboardCacheAsync();
                        dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
                        UploadedThesesAsProfessor = dashboardData.Theses
                            .Where(t => t.ProfessorEmailUsedToUploadThesis == CurrentUserEmail)
                            .OrderByDescending(t => t.ThesisUploadDateTime)
                            .ToList();
                        await UpdateProgressWhenDeleteThesisAsProfessor(80);

                        await UpdateProgressWhenDeleteThesisAsProfessor(90);
                        await LoadUploadedThesesAsProfessorAsync();
                        await CalculateStatusCountsForThesesAsProfessor();

                        await UpdateProgressWhenDeleteThesisAsProfessor(100);

                        await Task.Delay(300);
                    }
                    else
                    {
                        showLoadingModalForDeleteProfessorThesis = false;
                        await JS.InvokeVoidAsync("alert", "Η διπλωματική εργασία δεν βρέθηκε.");
                        StateHasChanged();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    showLoadingModalForDeleteProfessorThesis = false;
                    await JS.InvokeVoidAsync("alert", $"Σφάλμα κατά τη διαγραφή: {ex.Message}");
                    StateHasChanged();
                    return;
                }
                finally
                {
                    showLoadingModalForDeleteProfessorThesis = false;
                }

                StateHasChanged();
            }
        }

        private async Task UpdateProgressWhenDeleteThesisAsProfessor(int progress)
        {
            loadingProgress = progress;
            StateHasChanged();
            await Task.Delay(50);
        }

        // Bulk Operations
        private void EnableBulkEditModeForProfessorTheses()
        {
            isBulkEditModeForProfessorTheses = true;
            selectedProfessorThesisIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForProfessorTheses()
        {
            isBulkEditModeForProfessorTheses = false;
            selectedProfessorThesisIds.Clear();
            StateHasChanged();
        }

        private void ToggleProfessorThesisSelection(int thesisId, ChangeEventArgs e)
        {
            var isChecked = (bool)(e.Value ?? false);
            if (isChecked)
            {
                selectedProfessorThesisIds.Add(thesisId);
            }
            else
            {
                selectedProfessorThesisIds.Remove(thesisId);
            }
            StateHasChanged();
        }

        private void ToggleAllProfessorThesesSelection(ChangeEventArgs e)
        {
            var isChecked = (bool)(e.Value ?? false);
            var filteredTheses = GetPaginatedProfessorTheses().Where(j => selectedStatusFilterForThesesAsProfessor == "Όλα" || j.ThesisStatus == selectedStatusFilterForThesesAsProfessor);

            if (isChecked)
            {
                selectedProfessorThesisIds = new HashSet<int>(filteredTheses.Select(t => t.Id));
            }
            else
            {
                selectedProfessorThesisIds.Clear();
            }
            StateHasChanged();
        }

        private void ShowBulkActionOptionsForProfessorTheses()
        {
            if (selectedProfessorThesisIds.Count == 0) return;

            selectedProfessorThesesForAction = FilteredThesesAsProfessor
                .Where(t => selectedProfessorThesisIds.Contains(t.Id))
                .ToList();
            bulkActionForProfessorTheses = "";
            newStatusForBulkActionForProfessorTheses = "Μη Δημοσιευμένη";
            showBulkActionModalForProfessorTheses = true;
            StateHasChanged();
        }

        private void CloseBulkActionModalForProfessorTheses()
        {
            showBulkActionModalForProfessorTheses = false;
            bulkActionForProfessorTheses = "";
            StateHasChanged();
        }

        private async Task ExecuteBulkActionForProfessorTheses()
        {
            if (string.IsNullOrEmpty(bulkActionForProfessorTheses) || selectedProfessorThesisIds.Count == 0) return;

            string confirmationMessage = "";
            string actionDescription = "";

            if (bulkActionForProfessorTheses == "status")
            {
                var thesesWithSameStatus = selectedProfessorThesesForAction
                    .Where(t => t.ThesisStatus == newStatusForBulkActionForProfessorTheses)
                    .ToList();

                if (thesesWithSameStatus.Any())
                {
                    string alreadySameStatusMessage =
                        $"<strong style='color: orange;'>Προσοχή:</strong> {thesesWithSameStatus.Count} από τις επιλεγμένες Διπλωματικές είναι ήδη στην κατάσταση <strong>'{newStatusForBulkActionForProfessorTheses}'</strong> και δεν θα επηρεαστούν.<br><br>" +
                        "<strong>Διπλωματικές που δεν θα αλλάξουν:</strong><br>";

                    foreach (var thesis in thesesWithSameStatus.Take(5))
                    {
                        alreadySameStatusMessage += $"- {thesis.ThesisTitle} ({thesis.RNGForThesisUploaded_HashedAsUniqueID})<br>";
                    }

                    if (thesesWithSameStatus.Count > 5)
                    {
                        alreadySameStatusMessage += $"- ... και άλλες {thesesWithSameStatus.Count - 5} Διπλωματικές<br>";
                    }

                    alreadySameStatusMessage += "<br>";

                    bool continueAfterWarning = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] {
                        alreadySameStatusMessage +
                        "Θέλετε να συνεχίσετε με τις υπόλοιπες Διπλωματικές;"
                    });

                    if (!continueAfterWarning)
                    {
                        CloseBulkActionModalForProfessorTheses();
                        return;
                    }

                    foreach (var thesis in thesesWithSameStatus)
                    {
                        selectedProfessorThesisIds.Remove(thesis.Id);
                    }

                    selectedProfessorThesesForAction = selectedProfessorThesesForAction
                        .Where(t => !thesesWithSameStatus.Contains(t))
                        .ToList();

                    if (selectedProfessorThesisIds.Count == 0)
                    {
                        await JS.InvokeVoidAsync("confirmActionWithHTML2", "Δεν υπάρχουν Διπλωματικές για αλλαγή κατάστασης. Όλες οι επιλεγμένες Διπλωματικές είναι ήδη στην επιθυμητή κατάσταση.");
                        CloseBulkActionModalForProfessorTheses();
                        return;
                    }
                }

                actionDescription = $"αλλαγή κατάστασης σε '{newStatusForBulkActionForProfessorTheses}'";
                confirmationMessage = $"Είστε σίγουροι πως θέλετε να αλλάξετε την κατάσταση των {selectedProfessorThesisIds.Count} επιλεγμένων Διπλωματικών σε <strong>'{newStatusForBulkActionForProfessorTheses}'</strong>?<br><br>";
            }
            else if (bulkActionForProfessorTheses == "copy")
            {
                actionDescription = "αντιγραφή";
                confirmationMessage = $"Είστε σίγουροι πως θέλετε να αντιγράψετε τις {selectedProfessorThesisIds.Count} επιλεγμένες Διπλωματικές?<br>Οι νέες Διπλωματικές θα έχουν κατάσταση <strong>'Μη Δημοσιευμένη'</strong>.<br><br>";
            }

            confirmationMessage += "<strong>Επιλεγμένες Διπλωματικές:</strong><br>";
            foreach (var thesis in selectedProfessorThesesForAction.Take(10))
            {
                confirmationMessage += $"- {thesis.ThesisTitle} ({thesis.RNGForThesisUploaded_HashedAsUniqueID})<br>";
            }

            if (selectedProfessorThesesForAction.Count > 10)
            {
                confirmationMessage += $"- ... και άλλες {selectedProfessorThesesForAction.Count - 10} Διπλωματικές<br>";
            }

            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] { confirmationMessage });

            if (!isConfirmed)
            {
                CloseBulkActionModalForProfessorTheses();
                return;
            }

            try
            {
                showBulkActionModalForProfessorTheses = false;

                if (bulkActionForProfessorTheses == "status")
                {
                    await UpdateMultipleProfessorThesisStatuses();
                }
                else if (bulkActionForProfessorTheses == "copy")
                {
                    await CopyMultipleProfessorTheses();
                }

                await LoadUploadedThesesAsProfessorAsync();
                await CalculateStatusCountsForThesesAsProfessor();
                CancelBulkEditForProfessorTheses();

                var tabUrl = $"{NavigationManager.Uri.Split('?')[0]}#professor-theses";
                NavigationManager.NavigateTo(tabUrl, true);
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during bulk action for professor theses: {ex.Message}");
            }
        }

        private async Task CopyMultipleProfessorTheses()
        {
            var thesesToCopy = FilteredThesesAsProfessor
                .Where(t => selectedProfessorThesisIds.Contains(t.Id))
                .ToList();

            foreach (var originalThesis in thesesToCopy)
            {
                try
                {
                    var newThesis = new ProfessorThesis
                    {
                        ThesisTitle = originalThesis.ThesisTitle,
                        ThesisDescription = originalThesis.ThesisDescription,
                        ThesisAreas = originalThesis.ThesisAreas,
                        ThesisSkills = originalThesis.ThesisSkills,
                        ThesisAttachment = originalThesis.ThesisAttachment,
                        ThesisActivePeriod = originalThesis.ThesisActivePeriod,
                        ThesisType = originalThesis.ThesisType,
                        ProfessorEmailUsedToUploadThesis = originalThesis.ProfessorEmailUsedToUploadThesis,

                        RNGForThesisUploaded = new Random().NextInt64(),
                        RNGForThesisUploaded_HashedAsUniqueID = HashingHelper.HashLong(new Random().NextInt64()),

                        ThesisStatus = "Μη Δημοσιευμένη",
                        IsCompanyInterestedInProfessorThesisStatus = "Δεν έχει γίνει Αποδοχή",
                        ThesisUploadDateTime = DateTime.Now,
                        ThesisUpdateDateTime = DateTime.Now,
                    ThesisTimesUpdated = 0,

                    IsCompanyInteresetedInProfessorThesis = false,
                    CompanyEmailInterestedInProfessorThesis = null
                };

                    await ProfessorDashboardService.CreateOrUpdateThesisAsync(newThesis);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error copying professor thesis {originalThesis.Id}: {ex.Message}");
                }
            }

            await ProfessorDashboardService.RefreshDashboardCacheAsync();
            dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
            UploadedThesesAsProfessor = dashboardData.Theses
                .Where(t => t.ProfessorEmailUsedToUploadThesis == CurrentUserEmail)
                .OrderByDescending(t => t.ThesisUploadDateTime)
                .ToList();
        }

        private async Task UpdateMultipleProfessorThesisStatuses()
        {
            foreach (var thesisId in selectedProfessorThesisIds)
            {
                await UpdateProfessorThesisStatusDirectly(thesisId, newStatusForBulkActionForProfessorTheses);
            }
        }

        private async Task UpdateProfessorThesisStatusDirectly(int thesisId, string newStatus)
        {
            try
            {
                await ProfessorDashboardService.UpdateThesisStatusAsync(thesisId, newStatus);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating professor thesis status for thesis {thesisId}: {ex.Message}");
            }
        }

        private async Task ExecuteBulkStatusChangeForProfessorTheses(string newStatus)
        {
            if (selectedProfessorThesisIds.Count == 0) return;

            bulkActionForProfessorTheses = "status";
            newStatusForBulkActionForProfessorTheses = newStatus;

            selectedProfessorThesesForAction = FilteredThesesAsProfessor
                .Where(t => selectedProfessorThesisIds.Contains(t.Id))
                .ToList();

            await ExecuteBulkActionForProfessorTheses();
        }

        private async Task ExecuteBulkCopyForProfessorTheses()
        {
            if (selectedProfessorThesisIds.Count == 0) return;

            bulkActionForProfessorTheses = "copy";

            selectedProfessorThesesForAction = FilteredThesesAsProfessor
                .Where(t => selectedProfessorThesisIds.Contains(t.Id))
                .ToList();

            await ExecuteBulkActionForProfessorTheses();
        }

        // Edit Modal Area/Skill Selection Methods
        private void ToggleCheckboxesForEditProfessorThesis()
        {
            // This method is used in the edit modal
            StateHasChanged();
        }

        private void OnAreaCheckedChangedForEditProfessorThesis(ChangeEventArgs e, Area area)
        {
            var isChecked = (bool)(e.Value ?? false);

            if (isChecked)
            {
                if (!SelectedAreasToEditForProfessorThesis.Contains(area))
                {
                    SelectedAreasToEditForProfessorThesis.Add(area);
                }
            }
            else
            {
                SelectedAreasToEditForProfessorThesis.Remove(area);

                if (SelectedSubFieldsForEditProfessorThesis.ContainsKey(area.AreaName))
                {
                    SelectedSubFieldsForEditProfessorThesis.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForEditProfessorThesis(Area area)
        {
            return SelectedAreasToEditForProfessorThesis.Contains(area);
        }

        private void ToggleSubFieldsForEditProfessorThesis(Area area)
        {
            if (ExpandedAreasForEditProfessorThesis.Contains(area.Id))
            {
                ExpandedAreasForEditProfessorThesis.Remove(area.Id);
            }
            else
            {
                ExpandedAreasForEditProfessorThesis.Add(area.Id);
            }
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForEditProfessorThesis(ChangeEventArgs e, Area area, string subField)
        {
            var isChecked = (bool)(e.Value ?? false);

            if (!SelectedSubFieldsForEditProfessorThesis.ContainsKey(area.AreaName))
            {
                SelectedSubFieldsForEditProfessorThesis[area.AreaName] = new List<string>();
            }

            if (isChecked)
            {
                if (!SelectedSubFieldsForEditProfessorThesis[area.AreaName].Contains(subField))
                {
                    SelectedSubFieldsForEditProfessorThesis[area.AreaName].Add(subField);
                }
            }
            else
            {
                SelectedSubFieldsForEditProfessorThesis[area.AreaName].Remove(subField);

                if (!SelectedSubFieldsForEditProfessorThesis[area.AreaName].Any())
                {
                    SelectedSubFieldsForEditProfessorThesis.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForEditProfessorThesis(Area area, string subField)
        {
            return SelectedSubFieldsForEditProfessorThesis.ContainsKey(area.AreaName) &&
                SelectedSubFieldsForEditProfessorThesis[area.AreaName].Contains(subField);
        }

        private void OnCheckedChangedForEditProfessorThesisSkills(ChangeEventArgs e, Skill skill)
        {
            if ((bool)(e.Value ?? false))
            {
                if (!SelectedSkillsToEditForProfessorThesis.Contains(skill))
                {
                    SelectedSkillsToEditForProfessorThesis.Add(skill);
                }
            }
            else
            {
                SelectedSkillsToEditForProfessorThesis.Remove(skill);
            }
            StateHasChanged();
        }

        private bool IsSkillSelectedForEditProfessorThesis(Skill skill)
        {
            return SelectedSkillsToEditForProfessorThesis.Contains(skill);
        }

        // Additional Missing Properties
        private bool showErrorMessage = false;
        private bool showSuccessMessage = false;
        private ProfessorInternship professorInternship = new ProfessorInternship();
        private bool showErrorMessageforUploadinginternshipsAsProfessor = false;
        private bool isProfessorInternshipFormVisible = false;
        private List<Area> SelectedAreasWhenUploadInternshipAsProfessor = new List<Area>();
        private Dictionary<string, List<string>> SelectedSubFieldsForProfessorInternship = new Dictionary<string, List<string>>();
        
        // Company Theses Search
        private List<CompanyThesis> companyThesesResultsToFindThesesAsProfessor = new List<CompanyThesis>();
        private bool isUploadedCompanyThesesVisibleAsProfessor = false;
        private int currentPage_CompanyTheses = 1;
        private int itemsPerPage_CompanyTheses = 10;
        private int totalPages_CompanyTheses =>
            (int)Math.Ceiling((double)(companyThesesResultsToFindThesesAsProfessor?.Count ?? 0) / itemsPerPage_CompanyTheses);
        private Dictionary<long, bool> expandedTheses = new Dictionary<long, bool>();
        private Dictionary<long, bool> expandedProfessorThesesForCompanyInterest = new Dictionary<long, bool>();
        private List<string> companyNameSuggestions = new List<string>();
        
        // Thesis Applicants Management
        private bool isBulkEditModeForProfessorThesisApplicants = false;
        private bool isLoadingProfessorThesisApplicants = false;
        private bool isLoadingProfessorThesisCompanies = false;
        private long? loadingProfessorThesisId = null;
        private long? loadingProfessorThesisCompanyId = null;
        private Dictionary<string, Student> studentDataCache = new Dictionary<string, Student>();
        private Dictionary<long, List<ProfessorThesisApplied>> professorThesisApplicantsMap = new Dictionary<long, List<ProfessorThesisApplied>>();
        private HashSet<(long RNG, string StudentId)> selectedProfessorThesisApplicantIds = new HashSet<(long, string)>();
        private string pendingBulkActionForProfessorThesisApplicants = "";
        private bool sendEmailsForBulkProfessorThesisAction = true;
        private bool showEmailConfirmationModalForProfessorThesisApplicants = false;
        
        // Skills Search
        private List<string> skillSuggestionsToFindThesesAsProfessor = new List<string>();
        private HashSet<string> selectedSkillsToFindThesesAsProfessor = new HashSet<string>();
        
        // Attachment Error
        private string ProfessorThesisAttachmentErrorMessage = string.Empty;

        // Menu Toggle
        private Dictionary<long, bool> activeProfessorThesisMenuId = new Dictionary<long, bool>();
        
        private void ToggleProfessorThesisMenu(long thesisId)
        {
            if (activeProfessorThesisMenuId.ContainsKey(thesisId))
                activeProfessorThesisMenuId[thesisId] = !activeProfessorThesisMenuId[thesisId];
            else
                activeProfessorThesisMenuId[thesisId] = true;
            StateHasChanged();
        }

        // Toggle Company Theses Search
        private void ToggleToSearchForUploadedCompanyThesesAsProfessor()
        {
            isUploadedCompanyThesesVisibleAsProfessor = !isUploadedCompanyThesesVisibleAsProfessor;
            if (!isUploadedCompanyThesesVisibleAsProfessor)
            {
                companyThesesResultsToFindThesesAsProfessor.Clear();
            }
            StateHasChanged();
        }

        // Toggle Professor Internship Form
        private void ToggleFormVisibilityForUploadProfessorInternship()
        {
            isProfessorInternshipFormVisible = !isProfessorInternshipFormVisible;
            StateHasChanged();
        }

        private void ToggleCheckboxesForProfessorInternship()
        {
            showCheckboxesForProfessorInternship = !showCheckboxesForProfessorInternship;
            StateHasChanged();
        }

        private void OnAreaCheckedChangedForProfessorInternship(ChangeEventArgs e, Area area)
        {
            var isChecked = (bool)e.Value!;

            if (isChecked)
            {
                if (!SelectedAreasWhenUploadInternshipAsProfessor.Contains(area))
                {
                    SelectedAreasWhenUploadInternshipAsProfessor.Add(area);
                }
            }
            else
            {
                SelectedAreasWhenUploadInternshipAsProfessor.Remove(area);

                if (SelectedSubFieldsForProfessorInternship.ContainsKey(area.AreaName))
                {
                    SelectedSubFieldsForProfessorInternship.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForProfessorInternship(Area area)
        {
            return SelectedAreasWhenUploadInternshipAsProfessor.Contains(area);
        }

        private void ToggleSubFieldsForProfessorInternship(Area area)
        {
            if (ExpandedAreasForProfessorInternship.Contains(area.Id))
            {
                ExpandedAreasForProfessorInternship.Remove(area.Id);
            }
            else
            {
                ExpandedAreasForProfessorInternship.Add(area.Id);
            }
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForProfessorInternship(ChangeEventArgs e, Area area, string subField)
        {
            var isChecked = (bool)e.Value!;

            if (!SelectedSubFieldsForProfessorInternship.ContainsKey(area.AreaName))
            {
                SelectedSubFieldsForProfessorInternship[area.AreaName] = new List<string>();
            }

            if (isChecked)
            {
                if (!SelectedSubFieldsForProfessorInternship[area.AreaName].Contains(subField))
                {
                    SelectedSubFieldsForProfessorInternship[area.AreaName].Add(subField);
                }
            }
            else
            {
                SelectedSubFieldsForProfessorInternship[area.AreaName].Remove(subField);

                if (!SelectedSubFieldsForProfessorInternship[area.AreaName].Any())
                {
                    SelectedSubFieldsForProfessorInternship.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForProfessorInternship(Area area, string subField)
        {
            return SelectedSubFieldsForProfessorInternship.ContainsKey(area.AreaName) &&
                SelectedSubFieldsForProfessorInternship[area.AreaName].Contains(subField);
        }

        // Update Transport Offer
        private void UpdateTransportOfferForProfessorInternship(bool offer)
        {
            if (professorInternship != null)
            {
                professorInternship.ProfessorInternshipTransportOffer = offer;
                StateHasChanged();
            }
        }

        // Has Any Selection For Professor Internship
        private bool HasAnySelectionForProfessorInternship()
        {
            return SelectedAreasWhenUploadInternshipAsProfessor.Any() || SelectedSubFieldsForProfessorInternship.Any();
        }

        private async Task HandleFileUploadForProfessorInternships(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;

                if (file == null)
                {
                    professorInternship.ProfessorInternshipAttachment = null;
                    ProfessorInternshipAttachmentErrorMessage = null;
                    return;
                }

                if (file.ContentType != "application/pdf")
                {
                    ProfessorInternshipAttachmentErrorMessage = "Λάθος τύπος αρχείου. Επιλέξτε αρχείο PDF.";
                    professorInternship.ProfessorInternshipAttachment = null;
                    return;
                }

                const long maxFileSize = 10485760;
                if (file.Size > maxFileSize)
                {
                    ProfessorInternshipAttachmentErrorMessage = "Το αρχείο είναι πολύ μεγάλο. Μέγιστο μέγεθος: 10MB";
                    professorInternship.ProfessorInternshipAttachment = null;
                    return;
                }

                using var ms = new MemoryStream();
                await file.OpenReadStream(maxFileSize).CopyToAsync(ms);
                professorInternship.ProfessorInternshipAttachment = ms.ToArray();

                ProfessorInternshipAttachmentErrorMessage = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading professor internship attachment: {ex.Message}");
                ProfessorInternshipAttachmentErrorMessage = "Προέκυψε ένα σφάλμα κατά την μεταφόρτωση του αρχείου.";
                professorInternship.ProfessorInternshipAttachment = null;
            }
        }

        private async Task HandleSaveClickToSaveProfessorInternship()
        {
            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] {
                "Είστε σίγουροι πως θέλετε να υποβάλλετε <strong>Νέα Θέση Πρακτικής Άσκησης</strong>;<br><br>" +
                "Η Θέση θα καταχωρηθεί ως '<strong>Μη Δημοσιευμένη</strong>' στο Ιστορικό Θέσεων Πρακτικής Άσκησης.<br><br>" +
                "<strong style='color: red;'>Αν επιθυμείτε να την Δημοσιεύσετε, απαιτούνται επιπλέον ενέργειες!</strong>"
            });

            if (!isConfirmed)
                return;

            professorInternship.ProfessorUploadedInternshipStatus = "Μη Δημοσιευμένη";
            await SaveProfessorInternship(false);
        }

        private async Task HandlePublishClickToSaveProfessorInternship()
        {
            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] {
                "Είστε σίγουροι πως θέλετε να υποβάλλετε <strong>Νέα Θέση Πρακτικής Άσκησης</strong>;<br><br>" +
                "Η Θέση θα καταχωρηθεί ως '<strong>Δημοσιευμένη</strong>' στο Ιστορικό Θέσεων Πρακτικής Άσκησης.<br><br>" +
                "<strong style='color: red;'>Αν επιθυμείτε να την Αποδημοσιεύσετε, απαιτούνται επιπλέον ενέργειες!</strong>"
            });

            if (!isConfirmed)
                return;

            professorInternship.ProfessorUploadedInternshipStatus = "Δημοσιευμένη";
            await SaveProfessorInternship(true);
        }

        private async Task SaveProfessorInternship(bool isPublished)
        {
            showLoadingModalForProfessorInternship = true;
            loadingProgress = 0;
            showErrorMessage = false;
            showSuccessMessage = false;
            StateHasChanged();

            try
            {
                await UpdateProgressWhenSaveInternshipAsProfessor(10);
                var areasWithSubfields = new List<string>();

                foreach (var area in SelectedAreasWhenUploadInternshipAsProfessor)
                {
                    areasWithSubfields.Add(area.AreaName);
                }

                foreach (var areaSubFields in SelectedSubFieldsForProfessorInternship)
                {
                    var subFields = areaSubFields.Value;
                    foreach (var subField in subFields)
                    {
                        areasWithSubfields.Add(subField);
                    }
                }
                await UpdateProgressWhenSaveInternshipAsProfessor(20);

                await UpdateProgressWhenSaveInternshipAsProfessor(30);
                if (await HandleProfessorInternshipValidationWhenSaveInternshipAsProfessor(areasWithSubfields) == false)
                {
                    return;
                }
                await UpdateProgressWhenSaveInternshipAsProfessor(40);

                await UpdateProgressWhenSaveInternshipAsProfessor(50);
                professorInternship.RNGForInternshipUploadedAsProfessor = new Random().NextInt64();
                professorInternship.RNGForInternshipUploadedAsProfessor_HashedAsUniqueID = HashingHelper.HashLong(professorInternship.RNGForInternshipUploadedAsProfessor);
                professorInternship.ProfessorUploadedInternshipStatus = isPublished ? "Δημοσιευμένη" : "Μη Δημοσιευμένη";
                professorInternship.ProfessorInternshipUploadDate = DateTime.Now;
                professorInternship.ProfessorInternshipAreas = string.Join(",", areasWithSubfields);
                professorInternship.OpenSlots_ProfessorInternship = professorInternship.OpenSlots_ProfessorInternship;
                await UpdateProgressWhenSaveInternshipAsProfessor(60);

                await UpdateProgressWhenSaveInternshipAsProfessor(70);
                if (!string.IsNullOrWhiteSpace(selectedCompanyId) && int.TryParse(selectedCompanyId, out var companyId))
                {
                    var company = lookups.Companies.FirstOrDefault(p => p.Id == companyId);
                    professorInternship.ProfessorInternshipEKPASupervisor = company?.CompanyName ?? "Unknown Company";
                }
                else
                {
                    professorInternship.ProfessorInternshipEKPASupervisor = "Χωρίς Προτίμηση";
                }
                await UpdateProgressWhenSaveInternshipAsProfessor(80);

                await UpdateProgressWhenSaveInternshipAsProfessor(90);

                var internshipResult = await ProfessorDashboardService.CreateOrUpdateInternshipAsync(professorInternship);
                if (!internshipResult.Success)
                {
                    showLoadingModalForProfessorInternship = false;
                    showSuccessMessage = false;
                    showErrorMessage = true;
                    await JS.InvokeVoidAsync("alert", $"Error saving internship: {internshipResult.Error}");
                    StateHasChanged();
                    return;
                }
                await UpdateProgressWhenSaveInternshipAsProfessor(100);

                showSuccessMessage = true;
                showErrorMessage = false;

                professorInternship = new ProfessorInternship();
                SelectedAreasWhenUploadInternshipAsProfessor.Clear();
                SelectedSubFieldsForProfessorInternship.Clear();
                ExpandedAreasForProfessorInternship.Clear();

                await Task.Delay(500);

                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            catch (Exception ex)
            {
                showLoadingModalForProfessorInternship = false;

                showSuccessMessage = false;
                showErrorMessage = true;
                Console.WriteLine($"Error uploading professor internship: {ex.Message}");
                await JS.InvokeVoidAsync("alert", $"Error saving internship: {ex.Message}");
                StateHasChanged();
            }
        }

        private async Task<bool> HandleProfessorInternshipValidationWhenSaveInternshipAsProfessor(List<string> areasWithSubfields)
        {
            if (string.IsNullOrWhiteSpace(professorInternship.ProfessorInternshipTitle) ||
                string.IsNullOrWhiteSpace(professorInternship.ProfessorInternshipType) ||
                (professorInternship.OpenSlots_ProfessorInternship < 3) ||
                string.IsNullOrWhiteSpace(professorInternship.ProfessorInternshipESPA) ||
                string.IsNullOrWhiteSpace(professorInternship.ProfessorInternshipContactPerson) ||
                string.IsNullOrWhiteSpace(professorInternship.ProfessorInternshipDescription) ||
                string.IsNullOrWhiteSpace(professorInternship.ProfessorInternshipPerifereiaLocation) ||
                string.IsNullOrWhiteSpace(professorInternship.ProfessorInternshipDimosLocation) ||
                !IsValidEmailForProfessorInternships(professorInternship.ProfessorEmailUsedToUploadInternship) ||
                professorInternship.ProfessorInternshipActivePeriod.Date <= DateTime.Today ||
                professorInternship.ProfessorInternshipFinishEstimation.Date <= professorInternship.ProfessorInternshipActivePeriod.Date ||
                !HasAnySelectionForProfessorInternship())
            {
                await HandleProfessorInternshipValidationErrorWhenSaveInternshipAsProfessor();
                return false;
            }

            return true;
        }

        private async Task HandleProfessorInternshipValidationErrorWhenSaveInternshipAsProfessor()
        {
            showLoadingModalForProfessorInternship = false;
            showErrorMessage = true;
            Console.WriteLine("Validation failed: Missing fields or invalid date.");
            StateHasChanged();

            await Task.CompletedTask;
        }

        private async Task UpdateProgressWhenSaveInternshipAsProfessor(int progress)
        {
            loadingProgress = progress;
            StateHasChanged();
            await Task.Delay(50);
        }

        // Show Email Confirmation Modal
        private void ShowEmailConfirmationModalForProfessorThesisApplicants(string action)
        {
            pendingBulkActionForProfessorThesisApplicants = action;
            showEmailConfirmationModalForProfessorThesisApplicants = true;
            StateHasChanged();
        }

        // Pagination for Company Theses
        private IEnumerable<CompanyThesis> GetPaginatedCompanyTheses_AsProfessor()
        {
            if (companyThesesResultsToFindThesesAsProfessor == null) return Enumerable.Empty<CompanyThesis>();
            var skip = (currentPage_CompanyTheses - 1) * itemsPerPage_CompanyTheses;
            return companyThesesResultsToFindThesesAsProfessor.Skip(skip).Take(itemsPerPage_CompanyTheses);
        }

        private void GoToFirstPage_CompanyTheses()
        {
            currentPage_CompanyTheses = 1;
            StateHasChanged();
        }

        private void GoToLastPage_CompanyTheses()
        {
            currentPage_CompanyTheses = totalPages_CompanyTheses;
            StateHasChanged();
        }

        private void PreviousPage_CompanyTheses()
        {
            if (currentPage_CompanyTheses > 1)
            {
                currentPage_CompanyTheses--;
                StateHasChanged();
            }
        }

        private void NextPage_CompanyTheses()
        {
            if (currentPage_CompanyTheses < totalPages_CompanyTheses)
            {
                currentPage_CompanyTheses++;
                StateHasChanged();
            }
        }

        private void GoToPage_CompanyTheses(int page)
        {
            if (page >= 1 && page <= totalPages_CompanyTheses)
            {
                currentPage_CompanyTheses = page;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePages_CompanyTheses()
        {
            var pages = new List<int>();
            int current = currentPage_CompanyTheses;
            int total = totalPages_CompanyTheses;

            if (total == 0) return pages;

            if (total <= 7)
            {
                for (int i = 1; i <= total; i++)
                    pages.Add(i);
            }
            else
            {
                pages.Add(1);
                if (current > 4) pages.Add(-1);

                int start = Math.Max(2, current - 1);
                int end = Math.Min(total - 1, current + 1);

                for (int i = start; i <= end; i++)
                    pages.Add(i);

                if (current < total - 2) pages.Add(-1);
                if (total > 1) pages.Add(total);
            }

            return pages;
        }

        // Additional Missing Methods
        private void EnableBulkEditModeForProfessorThesisApplicants(long thesisId)
        {
            isBulkEditModeForProfessorThesisApplicants = true;
            currentProfessorThesisIdForBulkApplicants = thesisId;
            selectedProfessorThesisApplicantIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForProfessorThesisApplicants()
        {
            isBulkEditModeForProfessorThesisApplicants = false;
            selectedProfessorThesisApplicantIds.Clear();
            currentProfessorThesisIdForBulkApplicants = null;
            StateHasChanged();
        }

        private async Task ExecuteBulkActionForProfessorThesisApplicants()
        {
            if (string.IsNullOrEmpty(pendingBulkActionForProfessorThesisApplicants) || selectedProfessorThesisApplicantIds.Count == 0)
                return;

            // TODO: Implement bulk action logic - should delegate to ProfessorDashboardService
            await Task.CompletedTask;
            StateHasChanged();
        }

        private void CloseEmailConfirmationModalForProfessorThesisApplicants()
        {
            showEmailConfirmationModalForProfessorThesisApplicants = false;
            StateHasChanged();
        }

        private void CloseSlotWarningModalForProfessorThesis()
        {
            showSlotWarningModalForProfessorThesis = false;
            StateHasChanged();
        }

        // Additional Properties
        private bool showSlotWarningModalForProfessorThesis = false;
        private string slotWarningMessageForProfessorThesis = "";
        private long? currentProfessorThesisIdForBulkApplicants = null;
        private bool showLoadingModalWhenMarkInterestInCompanyThesisAsProfessor = false;
        private bool showLoadingModalForProfessorInternship = false;
        private bool showCheckboxesForProfessorInternship = false;
        private HashSet<int> ExpandedAreasForProfessorInternship = new HashSet<int>();
        private string ProfessorInternshipAttachmentErrorMessage = "";
        private bool IsValidEmailForProfessorInternships(string email) => !string.IsNullOrEmpty(email) && email.Contains("@");
        
        // Search Properties for Theses
        private string searchThesisTitleToFindThesesAsProfessor = "";
        private string searchSupervisorToFindThesesAsProfessor = "";
        private DateTime? searchStartingDateToFindThesesAsProfessor = null;
        private string searchSkillsInputToFindThesesAsProfessor = "";
        private string searchDepartmentToFindThesesAsProfessor = "";
        private string searchCompanyNameToFindThesesAsProfessor = "";
        private bool searchPerformedToFindThesesAsProfessor = false;
        private string selectedCompanyId = "";
        private int remainingCharactersInInternshipFieldUploadAsProfessor = 120;
        private int remainingCharactersInInternshipDescriptionUploadAsProfessor = 1000;
        
        // Company Theses Search Properties
        private int[] companyThesesPageSize_SearchForCompanyThesesAsProfessor = new[] { 10, 50, 100 };
        
        // Company Interest Properties
        private Dictionary<long, int> acceptedApplicantsCountPerProfessorThesis = new Dictionary<long, int>();
        private Dictionary<long, int> availableSlotsPerProfessorThesis = new Dictionary<long, int>();
        private bool loadingProgressWhenMarkInterestInCompanyThesisAsProfessor = false;
        private Company currentCompanyDetails = new Company();
        private Student currentStudentDetails = new Student();
        private bool isModalVisibleToShowStudentDetailsInNameAsHyperlinkForProfessorThesis = false;
        private List<Company> companies = new List<Company>();
        
        // Regions and ForeasType
        private List<string> Regions = new List<string>
        {
            "Ανατολική Μακεδονία και Θράκη", "Κεντρική Μακεδονία", "Δυτική Μακεδονία",
            "Ήπειρος", "Θεσσαλία", "Ιόνια Νησιά", "Δυτική Ελλάδα", "Κεντρική Ελλάδα",
            "Αττική", "Πελοπόννησος", "Βόρειο Αιγαίο", "Νότιο Αιγαίο", "Κρήτη"
        };
        private List<string> ForeasType = new List<string>
        {
            "Ιδιωτικός Φορέας", "Δημόσιος Φορέας", "Μ.Κ.Ο.", "Άλλο"
        };
        
        private Dictionary<string, List<string>> RegionToTownsMap = new Dictionary<string, List<string>>
        {
            {"Ανατολική Μακεδονία και Θράκη", new List<string> {"Κομοτηνή", "Αλεξανδρούπολη", "Καβάλα", "Ξάνθη", "Δράμα", "Ορεστιάδα", "Διδυμότειχο", "Ίασμος", "Νέα Βύσσα", "Φέρες"}},
            {"Κεντρική Μακεδονία", new List<string> {"Θεσσαλονίκη", "Κατερίνη", "Σέρρες", "Κιλκίς", "Πολύγυρος", "Ναούσα", "Έδεσσα", "Γιαννιτσά", "Καβάλα", "Άμφισσα"}},
            {"Δυτική Μακεδονία", new List<string> {"Κοζάνη", "Φλώρινα", "Καστοριά", "Γρεβενά"}},
            {"Ήπειρος", new List<string> {"Ιωάννινα", "Άρτα", "Πρέβεζα", "Ηγουμενίτσα"}},
            {"Θεσσαλία", new List<string> {"Λάρισα", "Βόλος", "Τρίκαλα", "Καρδίτσα"}},
            {"Ιόνια Νησιά", new List<string> {"Κέρκυρα", "Λευκάδα", "Κεφαλονιά", "Ζάκυνθος", "Ιθάκη", "Παξοί", "Κυθήρα"}},
            {"Δυτική Ελλάδα", new List<string> {"Πάτρα", "Μεσολόγγι", "Αμφιλοχία", "Πύργος", "Αιγίο", "Ναύπακτος"}},
            {"Κεντρική Ελλάδα", new List<string> {"Λαμία", "Χαλκίδα", "Λιβαδειά", "Θήβα", "Αλιάρτος", "Αμφίκλεια"}},
            {"Αττική", new List<string> {"Αθήνα", "Πειραιάς", "Κηφισιά", "Παλλήνη", "Αγία Παρασκευή", "Χαλάνδρι", "Καλλιθέα", "Γλυφάδα", "Περιστέρι", "Αιγάλεω"}},
            {"Πελοπόννησος", new List<string> {"Πάτρα", "Τρίπολη", "Καλαμάτα", "Κορίνθος", "Άργος", "Ναύπλιο", "Σπάρτη", "Κυπαρισσία", "Πύργος", "Μεσσήνη"}},
            {"Βόρειο Αιγαίο", new List<string> {"Μυτιλήνη", "Χίος", "Λήμνος", "Σάμος", "Ίκαρος", "Λέσβος", "Θάσος", "Σκύρος", "Ψαρά"}},
            {"Νότιο Αιγαίο", new List<string> {"Ρόδος", "Κως", "Κρήτη", "Κάρπαθος", "Σαντορίνη", "Μύκονος", "Νάξος", "Πάρος", "Σύρος", "Άνδρος"}},
            {"Κρήτη", new List<string> {"Ηράκλειο", "Χανιά", "Ρέθυμνο", "Αγία Νικόλαος", "Ιεράπετρα", "Σητεία", "Κίσαμος", "Παλαιόχωρα", "Αρχάνες", "Ανώγεια"}},
        };
        
        private List<string> GetTownsForRegion(string region)
        {
            if (string.IsNullOrEmpty(region) || !RegionToTownsMap.ContainsKey(region))
            {
                return new List<string>();
            }
            return RegionToTownsMap[region];
        }
        
        // Search Company Theses Methods
        private async Task SearchCompanyThesesAsProfessor()
        {
            var filter = new CompanyThesisSearchFilter
            {
                CompanyName = searchCompanyNameToFindThesesAsProfessor,
                Title = searchThesisTitleToFindThesesAsProfessor,
                Supervisor = searchSupervisorToFindThesesAsProfessor,
                Department = searchDepartmentToFindThesesAsProfessor,
                EarliestStartDate = searchStartingDateToFindThesesAsProfessor,
                MaxResults = 500
            };

            var initialQuery = await ProfessorDashboardService.SearchCompanyThesesAsync(filter);

            if (selectedSkillsToFindThesesAsProfessor.Any())
            {
                initialQuery = initialQuery
                    .Where(t => selectedSkillsToFindThesesAsProfessor.All(skill => 
                        t.CompanyThesisSkillsNeeded != null && t.CompanyThesisSkillsNeeded.Contains(skill)))
                    .ToList();
            }

            companyThesesResultsToFindThesesAsProfessor = initialQuery.ToList();
            searchPerformedToFindThesesAsProfessor = true;
            currentPage_CompanyTheses = 1;
            StateHasChanged();
        }

        private void ClearSearchFieldsForSearchCompanyThesesAsProfessor()
        {
            searchCompanyNameToFindThesesAsProfessor = string.Empty;
            searchThesisTitleToFindThesesAsProfessor = string.Empty;
            searchSupervisorToFindThesesAsProfessor = string.Empty;
            searchDepartmentToFindThesesAsProfessor = string.Empty;
            searchSkillsInputToFindThesesAsProfessor = string.Empty;
            searchStartingDateToFindThesesAsProfessor = null;
            selectedSkillsToFindThesesAsProfessor.Clear();
            companyThesesResultsToFindThesesAsProfessor?.Clear();
            searchPerformedToFindThesesAsProfessor = false;
            companyNameSuggestions.Clear();
            skillSuggestionsToFindThesesAsProfessor.Clear();
            StateHasChanged();
        }

        private async Task HandleCompanyNameInput(ChangeEventArgs e)
        {
            searchCompanyNameToFindThesesAsProfessor = e.Value?.ToString().Trim() ?? string.Empty;
            companyNameSuggestions.Clear();

            if (searchCompanyNameToFindThesesAsProfessor.Length >= 2)
            {
                try
                {
                    companyNameSuggestions = lookups.Companies
                        .Where(c => c.CompanyName.Contains(searchCompanyNameToFindThesesAsProfessor, StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.CompanyName)
                        .Distinct()
                        .Take(10)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading company suggestions: {ex.Message}");
                }
            }
            await Task.Yield();
            StateHasChanged();
        }

        private void SelectCompanyNameSuggestion(string name)
        {
            searchCompanyNameToFindThesesAsProfessor = name;
            companyNameSuggestions.Clear();
            StateHasChanged();
        }

        private async Task HandleSkillsInputToFindThesesAsProfessor(ChangeEventArgs e)
        {
            searchSkillsInputToFindThesesAsProfessor = e.Value?.ToString().Trim() ?? string.Empty;
            skillSuggestionsToFindThesesAsProfessor.Clear();

            if (searchSkillsInputToFindThesesAsProfessor.Length >= 1)
            {
                try
                {
                    skillSuggestionsToFindThesesAsProfessor = lookups.Skills
                        .Where(s => s.SkillName.Contains(searchSkillsInputToFindThesesAsProfessor, StringComparison.OrdinalIgnoreCase))
                        .Select(s => s.SkillName)
                        .Distinct()
                        .Take(10)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading skill suggestions: {ex.Message}");
                }
            }
            await Task.Yield();
            StateHasChanged();
        }

        private void SelectSkillSuggestionToFindThesesAsProfessor(string suggestion)
        {
            if (!string.IsNullOrWhiteSpace(suggestion) && !selectedSkillsToFindThesesAsProfessor.Contains(suggestion))
            {
                selectedSkillsToFindThesesAsProfessor.Add(suggestion);
                skillSuggestionsToFindThesesAsProfessor.Clear();
                searchSkillsInputToFindThesesAsProfessor = string.Empty;
                StateHasChanged();
            }
        }

        private void RemoveSelectedSkillToFindThesesAsProfessor(string skill)
        {
            selectedSkillsToFindThesesAsProfessor.Remove(skill);
            StateHasChanged();
        }

        private void OnPageSizeChange_SearchForCompanyThesesAsProfessor(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                itemsPerPage_CompanyTheses = newSize;
                currentPage_CompanyTheses = 1;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePages_CompanyTheses_AsProfessor()
        {
            return GetVisiblePages_CompanyTheses();
        }

        // Thesis Details and Interest Methods
        private void ShowThesisDetails(CompanyThesis thesis)
        {
            selectedCompanyThesisToSeeDetailsOnEyeIconAsProfessor = thesis;
            isThesisDetailEyeIconModalVisibleToSeeAsProfessor = true;
            StateHasChanged();
        }

        private void ShowProfessorThesisDetailsAsProfessor(ProfessorThesis thesis)
        {
            currentThesisAsProfessor = thesis;
            isModalVisibleToShowProfessorThesisAsProfessor = true;
            StateHasChanged();
        }

        private async Task ShowStudentDetailsInNameAsHyperlinkForProfessorThesis(string studentUniqueID, int applicationId)
        {
            try
            {
                var record = thesisApplications.FirstOrDefault(x => x.Id == applicationId);
                if (record != null && !record.ProfessorThesisApplied_CandidateInfoSeenFromModal)
                {
                    record.ProfessorThesisApplied_CandidateInfoSeenFromModal = true;
                }

                var student = await ProfessorDashboardService.GetStudentByUniqueIdAsync(studentUniqueID);

                if (student != null)
                {
                    currentStudentDetails = student;
                    isModalVisibleToShowStudentDetailsInNameAsHyperlinkForProfessorThesis = true;
                }
                else
                {
                    await JS.InvokeVoidAsync("alert", "Δεν βρέθηκαν στοιχεία φοιτητή");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading student details: {ex.Message}");
                await JS.InvokeVoidAsync("alert", "Σφάλμα κατά την φόρτωση των στοιχείων του φοιτητή");
            }
            StateHasChanged();
        }

        private async Task ShowCompanyDetailsAtProfessorThesisInterestFromHyperlinkCompanyName(ProfessorThesis professorThesis)
        {
            if (professorThesis?.CompanyInterested != null)
            {
                var company = await ProfessorDashboardService.GetCompanyByEmailAsync(professorThesis.CompanyInterested.CompanyEmail);

                if (company != null)
                {
                    currentCompanyDetails = company;
                    isExpandedModalVisibleToSeeCompanyDetailsAsProfessor = true;
                    StateHasChanged();
                }
            }
        }

        private void ShowCompanyDetailsFromHyperlinkNameToTheProfessor(CompanyThesis thesis)
        {
            if (thesis?.Company != null)
            {
                currentCompanyDetails = thesis.Company;
                isExpandedModalVisibleToSeeCompanyDetailsAsProfessor = true;
                StateHasChanged();
            }
        }

        private async Task ToggleProfessorThesesExpandedForCompanyInterest(long professorthesisRNG)
        {
            if (expandedProfessorThesesForCompanyInterest.ContainsKey(professorthesisRNG))
                expandedProfessorThesesForCompanyInterest[professorthesisRNG] = !expandedProfessorThesesForCompanyInterest[professorthesisRNG];
            else
                expandedProfessorThesesForCompanyInterest[professorthesisRNG] = true;
            StateHasChanged();
        }

        private void ToggleProfessorThesisExpanded(long thesisId)
        {
            // Toggle expanded state for thesis applicants
            if (expandedTheses.ContainsKey(thesisId))
                expandedTheses[thesisId] = !expandedTheses[thesisId];
            else
                expandedTheses[thesisId] = true;
            StateHasChanged();
        }

        private void ToggleProfessorThesisApplicantSelection(long id, string studentId, ChangeEventArgs e)
        {
            var applicant = professorThesisApplicantsMap.Values
                .SelectMany(list => list)
                .FirstOrDefault(a => a.Id == id && a.StudentUniqueIDAppliedForProfessorThesis == studentId);

            if (applicant != null)
            {
                var applicantKey = (applicant.RNGForProfessorThesisApplied, studentId);

                if ((bool)e.Value!)
                {
                    selectedProfessorThesisApplicantIds.Add(applicantKey);
                }
                else
                {
                    selectedProfessorThesisApplicantIds.Remove(applicantKey);
                }
                StateHasChanged();
            }
        }

        private async Task ConfirmAndAcceptProfessorThesis(long thesisRNG, string studentUniqueID)
        {
            var thesisObj = dashboardData.Theses.FirstOrDefault(t => t.RNGForThesisUploaded == thesisRNG);
            if (thesisObj == null) return;

            int acceptedCount = acceptedApplicantsCountPerProfessorThesis.GetValueOrDefault(thesisRNG, 0);
            int availableSlots = availableSlotsPerProfessorThesis.GetValueOrDefault(thesisRNG, thesisObj.OpenSlots_ProfessorThesis);

            if (acceptedCount >= availableSlots)
            {
                await JS.InvokeVoidAsync("alert", $"Έχετε Αποδεχτεί {acceptedCount}/{availableSlots} Αιτούντες");
                return;
            }

            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", "- ΑΠΟΔΟΧΗ ΑΙΤΗΣΗΣ - \n Η ενέργεια αυτή δεν θα μπορεί να αναιρεθεί. Είστε σίγουρος/η;");
            if (!isConfirmed) return;

            var result = await ProfessorDashboardService.DecideOnThesisApplicationAsync(thesisRNG, studentUniqueID, ApplicationDecision.Accept);
            if (!result.Success)
            {
                await JS.InvokeVoidAsync("alert", result.Error ?? "Σφάλμα κατά την αποδοχή της αίτησης");
                return;
            }

            thesisApplications = await ProfessorDashboardService.GetThesisApplicationsAsync();
            dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
            acceptedApplicantsCountPerProfessorThesis[thesisRNG] = thesisApplications.Count(a =>
                a.RNGForProfessorThesisApplied == thesisRNG &&
                string.Equals(a.ProfessorThesisStatusAppliedAtProfessorSide, "Επιτυχής", StringComparison.OrdinalIgnoreCase));
            StateHasChanged();
        }

        private async Task ConfirmAndRejectProfessorThesis(long thesisRNG, string studentUniqueID)
        {
            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", "- ΑΠΟΡΡΙΨΗ ΑΙΤΗΣΗΣ - \n Η ενέργεια αυτή δεν θα μπορεί να αναιρεθεί. Είστε σίγουρος/η;");
            if (!isConfirmed) return;

            var result = await ProfessorDashboardService.DecideOnThesisApplicationAsync(thesisRNG, studentUniqueID, ApplicationDecision.Reject);
            if (!result.Success)
            {
                await JS.InvokeVoidAsync("alert", result.Error ?? "Σφάλμα κατά την απόρριψη της αίτησης");
                return;
            }

            thesisApplications = await ProfessorDashboardService.GetThesisApplicationsAsync();
            dashboardData = await ProfessorDashboardService.LoadDashboardDataAsync();
            acceptedApplicantsCountPerProfessorThesis[thesisRNG] = thesisApplications.Count(a =>
                a.RNGForProfessorThesisApplied == thesisRNG &&
                string.Equals(a.ProfessorThesisStatusAppliedAtProfessorSide, "Επιτυχής", StringComparison.OrdinalIgnoreCase));
            StateHasChanged();
        }

        private async Task DownloadAttachmentForProfessorTheses(int thesisId)
        {
            var download = await ProfessorDashboardService.GetThesisAttachmentAsync(thesisId);
            if (download?.Data == null || download.Data.Length == 0) return;

            string base64String = Convert.ToBase64String(download.Data);
            string fileName = string.IsNullOrWhiteSpace(download.FileName) ? $"Thesis_Attachment_{thesisId}.pdf" : download.FileName;
            await JS.InvokeVoidAsync("downloadInternshipAttachmentAsStudent", fileName, base64String);
        }

        private async Task MarkInterestInThesisCompanyThesisAsProfessor(CompanyThesis thesis)
        {
            showLoadingModalWhenMarkInterestInCompanyThesisAsProfessor = true;
            loadingProgressWhenMarkInterestInCompanyThesisAsProfessor = true;
            
            // TODO: Implement interest marking logic
            await Task.CompletedTask;
            
            showLoadingModalWhenMarkInterestInCompanyThesisAsProfessor = false;
            loadingProgressWhenMarkInterestInCompanyThesisAsProfessor = false;
            StateHasChanged();
        }

        private void CheckCharacterLimitInInternshipFieldUploadAsProfessor(ChangeEventArgs e)
        {
            var inputText = e.Value?.ToString() ?? string.Empty;
            remainingCharactersInInternshipFieldUploadAsProfessor = 120 - inputText.Length;
            StateHasChanged();
        }

        private void CheckCharacterLimitInInternshipDescriptionUploadAsProfessor(ChangeEventArgs e)
        {
            var inputText = e.Value?.ToString() ?? string.Empty;
            remainingCharactersInInternshipDescriptionUploadAsProfessor = 1000 - inputText.Length;
            StateHasChanged();
        }

        // Additional missing properties
        private CompanyThesis selectedCompanyThesisToSeeDetailsOnEyeIconAsProfessor;
        private bool isThesisDetailEyeIconModalVisibleToSeeAsProfessor = false;
        private bool isModalVisibleToShowProfessorThesisAsProfessor = false;
        private bool isExpandedModalVisibleToSeeCompanyDetailsAsProfessor = false;
    }
}
