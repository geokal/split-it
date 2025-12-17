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

namespace SplitIt.Shared.Professor
{
    public partial class ProfessorThesesSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

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

            // Load areas and skills
            await LoadAreasAsync();
            await LoadSkillsAsync();
        }

        // Data Loading Methods
        private async Task LoadAreasAsync()
        {
            Areas = await dbContext.Areas.ToListAsync();
        }

        private async Task LoadSkillsAsync()
        {
            Skills = await dbContext.Skills.ToListAsync();
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
                var professor = await dbContext.Professors
                    .FirstOrDefaultAsync(p => p.ProfEmail == CurrentUserEmail);

                if (professor == null)
                {
                    professor = new Professor
                    {
                        ProfEmail = CurrentUserEmail,
                        ProfName = professorName,
                        ProfSurname = professorSurname,
                        ProfDepartment = professorDepartment
                    };
                    dbContext.Professors.Add(professor);
                    await dbContext.SaveChangesAsync();
                }
                await UpdateProgressWhenSaveThesisAsProfessor(80);

                await UpdateProgressWhenSaveThesisAsProfessor(90);
                professorthesis.ThesisUpdateDateTime = DateTime.Now;
                professorthesis.Professor = professor;

                dbContext.ProfessorTheses.Add(professorthesis);
                await dbContext.SaveChangesAsync();
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
                    UploadedThesesAsProfessor = await dbContext.ProfessorTheses
                        .Include(t => t.Professor)
                        .Where(t => t.ProfessorEmailUsedToUploadThesis == CurrentUserEmail)
                        .OrderByDescending(t => t.ThesisUploadDateTime)
                        .ToListAsync();
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

            var filteredThesesAsProfessor = selectedStatusFilterForThesesAsProfessor == "Όλα"
                ? UploadedThesesAsProfessor
                : UploadedThesesAsProfessor.Where(i => i.ThesisStatus == selectedStatusFilterForThesesAsProfessor);

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

                var thesisToUpdate = await dbContext.ProfessorTheses
                    .Include(t => t.Professor)
                    .FirstOrDefaultAsync(t => t.Id == currentThesisAsProfessor.Id);

                if (thesisToUpdate == null)
                {
                    await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε η Διπλωματική εργασία");
                    return;
                }

                thesisToUpdate.ThesisTitle = currentThesisAsProfessor.ThesisTitle;
                thesisToUpdate.ThesisDescription = currentThesisAsProfessor.ThesisDescription;
                thesisToUpdate.ThesisType = currentThesisAsProfessor.ThesisType;
                thesisToUpdate.ThesisStatus = currentThesisAsProfessor.ThesisStatus;
                thesisToUpdate.ThesisActivePeriod = currentThesisAsProfessor.ThesisActivePeriod;
                thesisToUpdate.ThesisAreas = currentThesisAsProfessor.ThesisAreas;
                thesisToUpdate.ThesisSkills = currentThesisAsProfessor.ThesisSkills;
                thesisToUpdate.OpenSlots_ProfessorThesis = currentThesisAsProfessor.OpenSlots_ProfessorThesis;

                if (thesisToUpdate.Professor != null && !string.IsNullOrEmpty(currentThesisAsProfessor.Professor?.ProfDepartment))
                {
                    thesisToUpdate.Professor.ProfDepartment = currentThesisAsProfessor.Professor.ProfDepartment;
                }

                if (currentThesisAsProfessor.ThesisAttachment != null &&
                    currentThesisAsProfessor.ThesisAttachment.Length > 0)
                {
                    thesisToUpdate.ThesisAttachment = currentThesisAsProfessor.ThesisAttachment;
                }

                thesisToUpdate.ThesisUpdateDateTime = DateTime.Now;
                thesisToUpdate.ThesisTimesUpdated++;

                await dbContext.SaveChangesAsync();

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
                    professorthesis.ThesisStatus = professorthesisnewStatus;
                    await dbContext.SaveChangesAsync();
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
                    var professorthesis = await dbContext.ProfessorTheses.FindAsync(professorThesisId);

                    if (professorthesis != null)
                    {
                        await UpdateProgressWhenDeleteThesisAsProfessor(60);
                        dbContext.ProfessorTheses.Remove(professorthesis);
                        await dbContext.SaveChangesAsync();
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

                    dbContext.ProfessorTheses.Add(newThesis);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error copying professor thesis {originalThesis.Id}: {ex.Message}");
                }
            }

            await dbContext.SaveChangesAsync();
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
                var thesis = await dbContext.ProfessorTheses
                    .FirstOrDefaultAsync(t => t.Id == thesisId);

                if (thesis != null)
                {
                    thesis.ThesisStatus = newStatus;
                    await dbContext.SaveChangesAsync();
                }
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
    }
}

