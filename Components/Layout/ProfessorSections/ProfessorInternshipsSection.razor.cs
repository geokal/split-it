using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.ProfessorSections
{
    public partial class ProfessorInternshipsSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private QuizManager.Services.InternshipEmailService InternshipEmailService { get; set; } = default!;

        // User Information
        private string CurrentUserEmail = "";

        // Form Visibility
        private bool isShowActiveInternshipsAsProfessorFormVisible = false;
        private bool isLoadingInternshipsHistoryAsProfessor = false;

        // Status Filter
        private string selectedStatusFilterForProfessorInternships = "Όλα";

        // Uploaded Internships Data
        private List<ProfessorInternship> professorInternships = new List<ProfessorInternship>();
        private List<ProfessorInternship> FilteredInternshipsAsProfessor = new List<ProfessorInternship>();

        // Pagination
        private int currentPage_ProfessorInternships = 1;
        private int itemsPerPage_ProfessorInternships = 10;
        private int[] pageSizeOptions_SeeMyUploadedInternshipsAsProfessor = new[] { 10, 50, 100 };

        // Counts
        private int totalCountInternshipsAsProfessor = 0;
        private int publishedCountInternshipsAsProfessor = 0;
        private int unpublishedCountInternshipsAsProfessor = 0;
        private int withdrawnCountInternshipsAsProfessor = 0;

        // Bulk Operations
        private bool isBulkEditModeForProfessorInternships = false;
        private HashSet<int> selectedProfessorInternshipIds = new HashSet<int>();
        private string bulkActionForProfessorInternships = "";
        private bool showBulkActionModalForProfessorInternships = false;
        private List<ProfessorInternship> selectedProfessorInternshipsForAction = new List<ProfessorInternship>();
        private string newStatusForBulkActionForProfessorInternships = "Μη Δημοσιευμένη";
        private bool showLoadingModalForDeleteProfessorInternship = false;
        private int loadingProgress = 0;

        // Individual Internship Operations
        private int activeProfessorInternshipMenuId = 0;
        private Dictionary<long, bool> expandedProfessorInternships = new Dictionary<long, bool>();
        private bool isLoadingProfessorInternshipApplicants = false;
        private long? loadingProfessorInternshipId = null;
        private Dictionary<long, IEnumerable<ProfessorInternshipApplied>> professorInternshipApplicantsMap = new Dictionary<long, IEnumerable<ProfessorInternshipApplied>>();

        // Computed Properties
        private int totalPages_ProfessorInternships =>
            (int)Math.Ceiling((double)(professorInternships?.Count ?? 0) / itemsPerPage_ProfessorInternships);

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            CurrentUserEmail = user.FindFirst("name")?.Value ?? "";
        }

        // Form Visibility Toggle
        private async Task ToggleFormVisibilityToShowMyActiveInternshipsAsProfessor()
        {
            isShowActiveInternshipsAsProfessorFormVisible = !isShowActiveInternshipsAsProfessorFormVisible;

            if (isShowActiveInternshipsAsProfessorFormVisible)
            {
                isLoadingInternshipsHistoryAsProfessor = true;
                StateHasChanged();

                try
                {
                    await LoadProfessorInternships();
                    InitializeSlotDataForProfessorInternships();
                }
                finally
                {
                    isLoadingInternshipsHistoryAsProfessor = false;
                    StateHasChanged();
                }
            }
            else
            {
                StateHasChanged();
            }
        }

        // Data Loading Methods
        private async Task LoadProfessorInternships()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentUserEmail))
                {
                    professorInternships = await dbContext.ProfessorInternships
                        .Include(i => i.Professor)
                        .Where(i => i.ProfessorEmailUsedToUploadInternship == CurrentUserEmail)
                        .OrderByDescending(i => i.ProfessorInternshipUploadDate)
                        .ToListAsync();

                    FilterProfessorInternships();
                    CalculateStatusCountsForInternshipsAsProfessor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading professor internships: {ex.Message}");
            }
            finally
            {
                StateHasChanged();
            }
        }

        // Applicant Management
        private bool isBulkEditModeForProfessorInternshipApplicants = false;
        private HashSet<(long, string)> selectedProfessorInternshipApplicantIds = new HashSet<(long, string)>();
        private QuizManager.Models.Student selectedStudent = null;
        private bool isModalVisibleToShowStudentDetailsAsProfessorFromTheirHyperlinkNameInProfessorInternships = false;
        private Dictionary<long, int> acceptedApplicantsCountPerProfessorInternship = new Dictionary<long, int>();
        private Dictionary<long, int> availableSlotsPerProfessorInternship = new Dictionary<long, int>();
        private string slotWarningMessageForProfessorInternship = "";
        private bool showSlotWarningModalForProfessorInternship = false;
        
        // Bulk Applicant Management
        private bool showBulkActionModalForProfessorInternshipApplicants = false;
        private string bulkActionForProfessorInternshipApplicants = "";
        private long? currentProfessorInternshipIdForBulkApplicants = null;
        private bool showEmailConfirmationModalForProfessorInternshipApplicants = false;
        private string pendingBulkActionForProfessorInternshipApplicants = "";
        private bool sendEmailsForBulkProfessorInternshipAction = true;
        private bool actionsPerformedToAcceptorRejectInternshipsAsProfessor = false;
        private Dictionary<string, QuizManager.Models.Student> studentDataCache = new Dictionary<string, QuizManager.Models.Student>();

        private void InitializeSlotDataForProfessorInternships()
        {
            foreach (var internship in professorInternships)
            {
                if (!availableSlotsPerProfessorInternship.ContainsKey(internship.RNGForInternshipUploadedAsProfessor))
                {
                    availableSlotsPerProfessorInternship[internship.RNGForInternshipUploadedAsProfessor] = internship.OpenSlots_ProfessorInternship;
                }

                if (professorInternshipApplicantsMap.ContainsKey(internship.RNGForInternshipUploadedAsProfessor))
                {
                    var acceptedCount = professorInternshipApplicantsMap[internship.RNGForInternshipUploadedAsProfessor]
                        .Count(a => a.InternshipStatusAppliedAtTheProfessorSide == "Επιτυχής");
                    acceptedApplicantsCountPerProfessorInternship[internship.RNGForInternshipUploadedAsProfessor] = acceptedCount;
                }
            }
        }

        // Filtering Methods
        private void FilterProfessorInternships()
        {
            if (selectedStatusFilterForProfessorInternships == "Όλα")
            {
                FilteredInternshipsAsProfessor = professorInternships;
            }
            else
            {
                FilteredInternshipsAsProfessor = professorInternships
                    .Where(i => i.ProfessorUploadedInternshipStatus == selectedStatusFilterForProfessorInternships)
                    .ToList();
            }
            StateHasChanged();
        }

        private void HandleStatusFilterChangeForProfessorInternships(ChangeEventArgs e)
        {
            selectedStatusFilterForProfessorInternships = e.Value?.ToString() ?? "Όλα";
            FilterProfessorInternships();
        }

        private void CalculateStatusCountsForInternshipsAsProfessor()
        {
            totalCountInternshipsAsProfessor = professorInternships.Count();
            publishedCountInternshipsAsProfessor = professorInternships.Count(i => i.ProfessorUploadedInternshipStatus == "Δημοσιευμένη");
            unpublishedCountInternshipsAsProfessor = professorInternships.Count(i => i.ProfessorUploadedInternshipStatus == "Μη Δημοσιευμένη");
            withdrawnCountInternshipsAsProfessor = professorInternships.Count(i => i.ProfessorUploadedInternshipStatus == "Αποσυρμένη");
            StateHasChanged();
        }

        // Pagination Methods
        private IEnumerable<ProfessorInternship> GetPaginatedProfessorInternships()
        {
            return FilteredInternshipsAsProfessor?
                .Skip((currentPage_ProfessorInternships - 1) * itemsPerPage_ProfessorInternships)
                .Take(itemsPerPage_ProfessorInternships) ?? Enumerable.Empty<ProfessorInternship>();
        }

        private List<int> GetVisiblePages_ProfessorInternships()
        {
            var pages = new List<int>();
            int current = currentPage_ProfessorInternships;
            int total = totalPages_ProfessorInternships;

            pages.Add(1);
            if (current > 3) pages.Add(-1);

            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);

            for (int i = start; i <= end; i++) pages.Add(i);
            if (current < total - 2) pages.Add(-1);

            if (total > 1) pages.Add(total);
            return pages;
        }

        private void GoToFirstPage_ProfessorInternships() => currentPage_ProfessorInternships = 1;
        private void GoToLastPage_ProfessorInternships() => currentPage_ProfessorInternships = totalPages_ProfessorInternships;
        private void PreviousPage_ProfessorInternships()
        {
            if (currentPage_ProfessorInternships > 1) currentPage_ProfessorInternships--;
            StateHasChanged();
        }
        private void NextPage_ProfessorInternships()
        {
            if (currentPage_ProfessorInternships < totalPages_ProfessorInternships) currentPage_ProfessorInternships++;
            StateHasChanged();
        }
        private void GoToPage_ProfessorInternships(int page)
        {
            if (page >= 1 && page <= totalPages_ProfessorInternships)
                currentPage_ProfessorInternships = page;
            StateHasChanged();
        }

        private void OnPageSizeChange_SeeMyUploadedInternshipsAsProfessor(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                itemsPerPage_ProfessorInternships = newSize;
                currentPage_ProfessorInternships = 1;
                StateHasChanged();
            }
        }

        // Individual Internship Operations
        private void ToggleProfessorInternshipMenu(int internshipId)
        {
            if (activeProfessorInternshipMenuId == internshipId)
            {
                activeProfessorInternshipMenuId = 0;
            }
            else
            {
                activeProfessorInternshipMenuId = internshipId;
            }
            StateHasChanged();
        }

        private void ShowProfessorInternshipDetails(ProfessorInternship professorinternship)
        {
            // This can be expanded to show details in a modal
            StateHasChanged();
        }

        private async Task DownloadAttachmentForProfessorInternships(int internshipId)
        {
            var internship = await dbContext.ProfessorInternships.FindAsync(internshipId);
            if (internship != null && internship.ProfessorInternshipAttachment != null)
            {
                var fileName = $"{internship.ProfessorInternshipTitle}_Attachment.pdf";
                var mimeType = "application/pdf";
                await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile", fileName, mimeType, internship.ProfessorInternshipAttachment);
            }
        }

        private async Task ToggleProfessorInternshipExpanded(long internshipId)
        {
            // Close all other expanded internships
            foreach (var key in expandedProfessorInternships.Keys.ToList())
            {
                if (key != internshipId)
                {
                    expandedProfessorInternships[key] = false;
                    professorInternshipApplicantsMap.Remove(key);
                }
            }

            // Toggle expansion state
            if (expandedProfessorInternships.ContainsKey(internshipId))
            {
                expandedProfessorInternships[internshipId] = !expandedProfessorInternships[internshipId];
            }
            else
            {
                expandedProfessorInternships[internshipId] = true;
            }

            // Load data if expanding
            if (expandedProfessorInternships[internshipId])
            {
                isLoadingProfessorInternshipApplicants = true;
                loadingProfessorInternshipId = internshipId;
                StateHasChanged();

                try
                {
                    if (!professorInternshipApplicantsMap.ContainsKey(internshipId))
                    {
                        professorInternshipApplicantsMap[internshipId] = await GetApplicantsForProfessorInternship(internshipId);
                        await LoadProfessorInternshipApplicantData();
                        InitializeSlotDataForProfessorInternships();
                    }
                }
                finally
                {
                    isLoadingProfessorInternshipApplicants = false;
                    loadingProfessorInternshipId = null;
                    StateHasChanged();
                }
            }
            else
            {
                professorInternshipApplicantsMap.Remove(internshipId);
                StateHasChanged();
            }
        }

        private bool IsProfessorInternshipExpanded(long internshipId)
        {
            return expandedProfessorInternships.ContainsKey(internshipId) && expandedProfessorInternships[internshipId];
        }

        private async Task<IEnumerable<ProfessorInternshipApplied>> GetApplicantsForProfessorInternship(long internshipId)
        {
            try
            {
                var internship = await dbContext.ProfessorInternships
                    .Where(i => i.RNGForInternshipUploadedAsProfessor == internshipId)
                    .FirstOrDefaultAsync();

                if (internship == null)
                {
                    return Enumerable.Empty<ProfessorInternshipApplied>();
                }

                return await dbContext.ProfessorInternshipsApplied
                    .Where(a => a.RNGForProfessorInternshipApplied == internshipId)
                    .Include(a => a.StudentDetails)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting applicants for professor internship: {ex.Message}");
                return Enumerable.Empty<ProfessorInternshipApplied>();
            }
        }


        private async Task LoadProfessorInternshipApplicantData()
        {
            try
            {
                var studentEmails = professorInternshipApplicantsMap.Values
                    .SelectMany(x => x)
                    .Select(a => a.StudentDetails.StudentEmailAppliedForProfessorInternship)
                    .Distinct()
                    .ToList();

                var students = await dbContext.Students
                    .Where(s => studentEmails.Contains(s.Email.ToLower()))
                    .Select(s => new Student
                    {
                        Id = s.Id,
                        Student_UniqueID = s.Student_UniqueID,
                        Email = s.Email,
                        Image = s.Image,
                        Name = s.Name,
                        Surname = s.Surname,
                        Telephone = s.Telephone,
                        PermanentAddress = s.PermanentAddress,
                        PermanentRegion = s.PermanentRegion,
                        PermanentTown = s.PermanentTown,
                        Attachment = s.Attachment,
                        LinkedInProfile = s.LinkedInProfile,
                        PersonalWebsite = s.PersonalWebsite,
                        Transport = s.Transport,
                        RegNumber = s.RegNumber,
                        University = s.University,
                        Department = s.Department,
                        EnrollmentDate = s.EnrollmentDate,
                        StudyYear = s.StudyYear,
                        LevelOfDegree = s.LevelOfDegree,
                        AreasOfExpertise = s.AreasOfExpertise,
                        Keywords = s.Keywords,
                        ExpectedGraduationDate = s.ExpectedGraduationDate,
                        CompletedECTS = s.CompletedECTS
                    })
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var student in students)
                {
                    studentDataCache[student.Email.ToLower()] = student;
                }

                Console.WriteLine($"Loaded {students.Count} professor internship student records");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading professor internship student data: {ex.Message}");
            }
            StateHasChanged();
        }

        private async Task UpdateInternshipStatusAsProfessor(int internshipId, string newStatus)
        {
            var isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[]
            {
                $"Πρόκειται να αλλάξετε την κατάσταση αυτής της Πρακτικής Άσκησης σε '{newStatus}'. Είστε σίγουρος/η;"
            });

            if (isConfirmed)
            {
                var internship = await dbContext.ProfessorInternships
                    .FirstOrDefaultAsync(i => i.Id == internshipId);

                if (internship != null)
                {
                    internship.ProfessorUploadedInternshipStatus = newStatus;

                    if (newStatus == "Αποσυρμένη")
                    {
                        var rngForInternship = internship.RNGForInternshipUploadedAsProfessor;

                        var studentApplications = await dbContext.ProfessorInternshipsApplied
                            .Where(a => a.RNGForProfessorInternshipApplied == rngForInternship)
                            .ToListAsync();

                        foreach (var application in studentApplications)
                        {
                            application.InternshipStatusAppliedAtTheProfessorSide = "Απορρίφθηκε (Απόσυρση Θέσεως Από Καθηγητή)";
                            application.InternshipStatusAppliedAtTheStudentSide = "Απορρίφθηκε (Απόσυρση Θέσεως Από Καθηγητή)";
                        }
                    }

                    await dbContext.SaveChangesAsync();

                    await LoadProfessorInternships();
                    var tabUrl = $"{NavigationManager.Uri.Split('?')[0]}#professor-internships";
                    NavigationManager.NavigateTo(tabUrl, true);
                    await Task.Delay(500);
                    await JS.InvokeVoidAsync("activateTab", "professor-internships");
                }
            }
        }

        private async Task ChangeProfessorInternshipStatusToUnpublished(int internshipId)
        {
            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML",
                "Προχωράτε στην Αποδημοσίευση της Θέσης. Η Θέση μετά από αυτήν την ενέργεια ΔΕΝ θα είναι ορατή σε νέους υποψηφίους. Η κατάσταση των αιτήσεων παλαιότερων υποψηφίων θα παραμείνει ως έχει. Θέλετε σίγουρα να συνεχίσετε ;");

            if (isConfirmed)
            {
                await UpdateInternshipStatusAsProfessor(internshipId, "Μη Δημοσιευμένη");
            }
        }

        private void EditProfessorInternshipDetails(ProfessorInternship internship)
        {
            // This method can be expanded to show an edit modal
            // For now, it's a placeholder that can be implemented based on requirements
            StateHasChanged();
        }

        // Applicant Management Methods
        private void EnableBulkEditModeForProfessorInternshipApplicants(string internshipId)
        {
            isBulkEditModeForProfessorInternshipApplicants = true;
            selectedProfessorInternshipApplicantIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForProfessorInternshipApplicants()
        {
            isBulkEditModeForProfessorInternshipApplicants = false;
            selectedProfessorInternshipApplicantIds.Clear();
            StateHasChanged();
        }

        private void ToggleProfessorInternshipApplicantSelection(long rng, string studentId, ChangeEventArgs e)
        {
            var isChecked = (bool)(e.Value ?? false);
            var key = (rng, studentId);

            if (isChecked)
            {
                selectedProfessorInternshipApplicantIds.Add(key);
            }
            else
            {
                selectedProfessorInternshipApplicantIds.Remove(key);
            }
            StateHasChanged();
        }

        private async Task ConfirmAndAcceptProfessorInternship(long professorInternshipId, string studentUniqueID)
        {
            // First check slot availability
            var internshipObj = professorInternships.FirstOrDefault(t => t.RNGForInternshipUploadedAsProfessor == professorInternshipId);
            if (internshipObj == null) return;

            // Check available slots
            int acceptedCountForProfessorInternship = acceptedApplicantsCountPerProfessorInternship.GetValueOrDefault(professorInternshipId, 0);
            int availableSlotsForProfessorInternship = availableSlotsPerProfessorInternship.GetValueOrDefault(professorInternshipId, internshipObj.OpenSlots_ProfessorInternship);

            if (acceptedCountForProfessorInternship >= availableSlotsForProfessorInternship)
            {
                slotWarningMessageForProfessorInternship = $"Έχετε Αποδεχτεί {acceptedCountForProfessorInternship}/{availableSlotsForProfessorInternship} Αιτούντες, πρέπει να αλλάξετε τον Αριθμό των διαθέσιμων Slots της Πρακτικής Άσκησης για να προχωρήσετε";
                showSlotWarningModalForProfessorInternship = true;
                StateHasChanged();
                return;
            }

            // If slots are available, show confirmation
            bool isConfirmedForProfessorInternships = await JS.InvokeAsync<bool>(
                "confirmActionWithHTML", 
                "- ΑΠΟΔΟΧΗ ΑΙΤΗΣΗΣ - \n Η ενέργεια αυτή δεν θα μπορεί να αναιρεθεί. Είστε σίγουρος/η;"
            );

            if (isConfirmedForProfessorInternships)
            {
                await AcceptInternshipApplicationAsProfessor_MadeByStudent(professorInternshipId, studentUniqueID);
                actionsPerformedToAcceptorRejectInternshipsAsProfessor = true;
        
                // Update accepted count on successful acceptance
                acceptedApplicantsCountPerProfessorInternship[professorInternshipId] = acceptedCountForProfessorInternship + 1;
                StateHasChanged();
            }
        }

        private async Task ConfirmAndRejectProfessorInternship(long professorInternshipId, string studentUniqueID)
        {
            bool isConfirmedForProfessorInternships = await JS.InvokeAsync<bool>(
                "confirmActionWithHTML", 
                "- ΑΠΟΡΡΙΨΗ ΑΙΤΗΣΗΣ - \n Η ενέργεια αυτή δεν θα μπορεί να αναιρεθεί. Είστε σίγουρος/η;"
            );

            if (isConfirmedForProfessorInternships)
            {     
                await RejectInternshipApplicationAsProfessor_MadeByStudent(professorInternshipId, studentUniqueID);
                actionsPerformedToAcceptorRejectInternshipsAsProfessor = true;
            }
        }

        private async Task ShowStudentDetailsInNameAsHyperlink_StudentAppliedinternshipsAtProfessor(string studentUniqueID, int applicationId)
        {
            try
            {
                var application = await dbContext.ProfessorInternshipsApplied
                    .Include(a => a.StudentDetails)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application?.StudentDetails != null)
                {
                    var studentEmail = application.StudentDetails.StudentEmailAppliedForProfessorInternship;
                    if (studentDataCache.TryGetValue(studentEmail.ToLower(), out var cachedStudent))
                    {
                        selectedStudent = cachedStudent;
                    }
                    else
                    {
                        var student = await dbContext.Students
                            .FirstOrDefaultAsync(s => s.Student_UniqueID == studentUniqueID);
                        if (student != null)
                        {
                            selectedStudent = student;
                            studentDataCache[student.Email.ToLower()] = student;
                        }
                    }
                }

                isModalVisibleToShowStudentDetailsAsProfessorFromTheirHyperlinkNameInProfessorInternships = true;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing student details: {ex.Message}");
            }
        }

        private void CloseStudentDetailsModal()
        {
            isModalVisibleToShowStudentDetailsAsProfessorFromTheirHyperlinkNameInProfessorInternships = false;
            selectedStudent = null;
            StateHasChanged();
        }

        private void ShowEmailConfirmationModalForProfessorInternshipApplicants(string action)
        {
            pendingBulkActionForProfessorInternshipApplicants = action;
            showEmailConfirmationModalForProfessorInternshipApplicants = true;
            StateHasChanged();
        }

        private void CloseEmailConfirmationModalForProfessorInternshipApplicants()
        {
            showEmailConfirmationModalForProfessorInternshipApplicants = false;
            pendingBulkActionForProfessorInternshipApplicants = "";
            sendEmailsForBulkProfessorInternshipAction = true; // Reset to default
            StateHasChanged();
        }

        private void CloseSlotWarningModalForProfessorInternship()
        {
            showSlotWarningModalForProfessorInternship = false;
            slotWarningMessageForProfessorInternship = "";
            StateHasChanged();
        }

        // Delete Method
        private async Task DeleteProfessorInternship(int internshipId)
        {
            var isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML",
                "Πρόκειται να διαγράψετε οριστικά αυτή την Πρακτική Άσκηση.<br><br>" +
                "<strong style='color: red;'>Είστε σίγουρος/η;</strong>");

            if (isConfirmed)
            {
                showLoadingModalForDeleteProfessorInternship = true;
                loadingProgress = 0;
                StateHasChanged();

                try
                {
                    await UpdateProgressWhenDeleteProfessorInternship(30);
                    var internship = await dbContext.ProfessorInternships.FindAsync(internshipId);

                    if (internship != null)
                    {
                        await UpdateProgressWhenDeleteProfessorInternship(60);
                        dbContext.ProfessorInternships.Remove(internship);
                        await dbContext.SaveChangesAsync();
                        await UpdateProgressWhenDeleteProfessorInternship(80);

                        await UpdateProgressWhenDeleteProfessorInternship(90);
                        await LoadProfessorInternships();

                        await UpdateProgressWhenDeleteProfessorInternship(100);

                        await Task.Delay(300);
                    }
                    else
                    {
                        showLoadingModalForDeleteProfessorInternship = false;
                        await JS.InvokeVoidAsync("alert", "Η πρακτική άσκηση δεν βρέθηκε.");
                        StateHasChanged();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    showLoadingModalForDeleteProfessorInternship = false;
                    await JS.InvokeVoidAsync("alert", $"Σφάλμα κατά τη διαγραφή: {ex.Message}");
                    StateHasChanged();
                    return;
                }
                finally
                {
                    showLoadingModalForDeleteProfessorInternship = false;
                }

                StateHasChanged();
            }
        }

        private async Task UpdateProgressWhenDeleteProfessorInternship(int progress)
        {
            loadingProgress = progress;
            StateHasChanged();
            await Task.Delay(50);
        }

        // Bulk Operations
        private void EnableBulkEditModeForProfessorInternships()
        {
            isBulkEditModeForProfessorInternships = true;
            selectedProfessorInternshipIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForProfessorInternships()
        {
            isBulkEditModeForProfessorInternships = false;
            selectedProfessorInternshipIds.Clear();
            StateHasChanged();
        }

        private void ToggleProfessorInternshipSelection(int internshipId, ChangeEventArgs e)
        {
            var isChecked = (bool)(e.Value ?? false);
            if (isChecked)
            {
                selectedProfessorInternshipIds.Add(internshipId);
            }
            else
            {
                selectedProfessorInternshipIds.Remove(internshipId);
            }
            StateHasChanged();
        }

        private void ToggleAllProfessorInternshipsSelection(ChangeEventArgs e)
        {
            var isChecked = (bool)(e.Value ?? false);
            var filteredInternships = GetPaginatedProfessorInternships().Where(i => selectedStatusFilterForProfessorInternships == "Όλα" || i.ProfessorUploadedInternshipStatus == selectedStatusFilterForProfessorInternships);

            if (isChecked)
            {
                selectedProfessorInternshipIds = new HashSet<int>(filteredInternships.Select(i => i.Id));
            }
            else
            {
                selectedProfessorInternshipIds.Clear();
            }
            StateHasChanged();
        }

        private void ShowBulkActionOptionsForProfessorInternships()
        {
            if (selectedProfessorInternshipIds.Count == 0) return;

            selectedProfessorInternshipsForAction = FilteredInternshipsAsProfessor
                .Where(i => selectedProfessorInternshipIds.Contains(i.Id))
                .ToList();
            bulkActionForProfessorInternships = "";
            newStatusForBulkActionForProfessorInternships = "Μη Δημοσιευμένη";
            showBulkActionModalForProfessorInternships = true;
            StateHasChanged();
        }

        private void CloseBulkActionModalForProfessorInternships()
        {
            showBulkActionModalForProfessorInternships = false;
            bulkActionForProfessorInternships = "";
            StateHasChanged();
        }

        private async Task ExecuteBulkActionForProfessorInternships()
        {
            if (string.IsNullOrEmpty(bulkActionForProfessorInternships) || selectedProfessorInternshipIds.Count == 0) return;

            string confirmationMessage = "";
            string actionDescription = "";

            if (bulkActionForProfessorInternships == "status")
            {
                var internshipsWithSameStatus = selectedProfessorInternshipsForAction
                    .Where(i => i.ProfessorUploadedInternshipStatus == newStatusForBulkActionForProfessorInternships)
                    .ToList();

                if (internshipsWithSameStatus.Any())
                {
                    string alreadySameStatusMessage =
                        $"<strong style='color: orange;'>Προσοχή:</strong> {internshipsWithSameStatus.Count} από τις επιλεγμένες πρακτικές είναι ήδη στην κατάσταση <strong>'{newStatusForBulkActionForProfessorInternships}'</strong> και δεν θα επηρεαστούν.<br><br>" +
                        "<strong>Πρακτικές που δεν θα αλλάξουν:</strong><br>";

                    foreach (var internship in internshipsWithSameStatus.Take(5))
                    {
                        alreadySameStatusMessage += $"- {internship.ProfessorInternshipTitle} ({internship.RNGForInternshipUploadedAsProfessor_HashedAsUniqueID})<br>";
                    }

                    if (internshipsWithSameStatus.Count > 5)
                    {
                        alreadySameStatusMessage += $"- ... και άλλες {internshipsWithSameStatus.Count - 5} πρακτικές<br>";
                    }

                    alreadySameStatusMessage += "<br>";

                    bool continueAfterWarning = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] {
                        alreadySameStatusMessage +
                        "Θέλετε να συνεχίσετε με τις υπόλοιπες πρακτικές;"
                    });

                    if (!continueAfterWarning)
                    {
                        CloseBulkActionModalForProfessorInternships();
                        return;
                    }

                    foreach (var internship in internshipsWithSameStatus)
                    {
                        selectedProfessorInternshipIds.Remove(internship.Id);
                    }

                    selectedProfessorInternshipsForAction = selectedProfessorInternshipsForAction
                        .Where(i => !internshipsWithSameStatus.Contains(i))
                        .ToList();

                    if (selectedProfessorInternshipIds.Count == 0)
                    {
                        await JS.InvokeVoidAsync("confirmActionWithHTML2", "Δεν υπάρχουν πρακτικές για αλλαγή κατάστασης. Όλες οι επιλεγμένες πρακτικές είναι ήδη στην επιθυμητή κατάσταση.");
                        CloseBulkActionModalForProfessorInternships();
                        return;
                    }
                }

                actionDescription = $"αλλαγή κατάστασης σε '{newStatusForBulkActionForProfessorInternships}'";
                confirmationMessage = $"Είστε σίγουροι πως θέλετε να αλλάξετε την κατάσταση των {selectedProfessorInternshipIds.Count} επιλεγμένων πρακτικών σε <strong>'{newStatusForBulkActionForProfessorInternships}'</strong>?<br><br>";
            }
            else if (bulkActionForProfessorInternships == "copy")
            {
                actionDescription = "αντιγραφή";
                confirmationMessage = $"Είστε σίγουροι πως θέλετε να αντιγράψετε τις {selectedProfessorInternshipIds.Count} επιλεγμένες πρακτικές?<br>Οι νέες πρακτικές θα έχουν κατάσταση <strong>'Μη Δημοσιευμένη'</strong>.<br><br>";
            }

            confirmationMessage += "<strong>Επιλεγμένες Πρακτικές:</strong><br>";
            foreach (var internship in selectedProfessorInternshipsForAction.Take(10))
            {
                confirmationMessage += $"- {internship.ProfessorInternshipTitle} ({internship.RNGForInternshipUploadedAsProfessor_HashedAsUniqueID})<br>";
            }

            if (selectedProfessorInternshipsForAction.Count > 10)
            {
                confirmationMessage += $"- ... και άλλες {selectedProfessorInternshipsForAction.Count - 10} πρακτικές<br>";
            }

            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", new object[] { confirmationMessage });

            if (!isConfirmed)
            {
                CloseBulkActionModalForProfessorInternships();
                return;
            }

            try
            {
                showBulkActionModalForProfessorInternships = false;

                if (bulkActionForProfessorInternships == "status")
                {
                    await UpdateMultipleProfessorInternshipStatuses();
                }
                else if (bulkActionForProfessorInternships == "copy")
                {
                    await CopyMultipleProfessorInternships();
                }

                await LoadProfessorInternships();
                CancelBulkEditForProfessorInternships();

                var tabUrl = $"{NavigationManager.Uri.Split('?')[0]}#professor-internships";
                NavigationManager.NavigateTo(tabUrl, true);
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during bulk action for professor internships: {ex.Message}");
            }
        }

        private async Task CopyMultipleProfessorInternships()
        {
            var internshipsToCopy = FilteredInternshipsAsProfessor
                .Where(i => selectedProfessorInternshipIds.Contains(i.Id))
                .ToList();

            foreach (var originalInternship in internshipsToCopy)
            {
                try
                {
                    var newInternship = new ProfessorInternship
                    {
                        ProfessorInternshipESPA = originalInternship.ProfessorInternshipESPA,
                        ProfessorInternshipType = originalInternship.ProfessorInternshipType,
                        ProfessorInternshipTitle = originalInternship.ProfessorInternshipTitle,
                        ProfessorInternshipForeas = originalInternship.ProfessorInternshipForeas,
                        ProfessorInternshipContactPerson = originalInternship.ProfessorInternshipContactPerson,
                        ProfessorInternshipContactTelephonePerson = originalInternship.ProfessorInternshipContactTelephonePerson,
                        ProfessorInternshipAddress = originalInternship.ProfessorInternshipAddress,
                        ProfessorInternshipPerifereiaLocation = originalInternship.ProfessorInternshipPerifereiaLocation,
                        ProfessorInternshipDimosLocation = originalInternship.ProfessorInternshipDimosLocation,
                        ProfessorInternshipPostalCodeLocation = originalInternship.ProfessorInternshipPostalCodeLocation,
                        ProfessorInternshipTransportOffer = originalInternship.ProfessorInternshipTransportOffer,
                        ProfessorInternshipAreas = originalInternship.ProfessorInternshipAreas,
                        ProfessorInternshipActivePeriod = originalInternship.ProfessorInternshipActivePeriod,
                        ProfessorInternshipFinishEstimation = originalInternship.ProfessorInternshipFinishEstimation,
                        ProfessorInternshipLastUpdate = DateTime.Now,
                        ProfessorInternshipDescription = originalInternship.ProfessorInternshipDescription,
                        ProfessorInternshipAttachment = originalInternship.ProfessorInternshipAttachment,
                        ProfessorInternshipEKPASupervisor = originalInternship.ProfessorInternshipEKPASupervisor,
                        ProfessorEmailUsedToUploadInternship = originalInternship.ProfessorEmailUsedToUploadInternship,

                        RNGForInternshipUploadedAsProfessor = new Random().NextInt64(),
                        RNGForInternshipUploadedAsProfessor_HashedAsUniqueID = HashingHelper.HashLong(new Random().NextInt64()),

                        ProfessorUploadedInternshipStatus = "Μη Δημοσιευμένη",
                        ProfessorInternshipUploadDate = DateTime.Now
                    };

                    dbContext.ProfessorInternships.Add(newInternship);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error copying professor internship {originalInternship.Id}: {ex.Message}");
                }
            }

            await dbContext.SaveChangesAsync();
        }

        private async Task UpdateMultipleProfessorInternshipStatuses()
        {
            foreach (var internshipId in selectedProfessorInternshipIds)
            {
                await UpdateProfessorInternshipStatusDirectly(internshipId, newStatusForBulkActionForProfessorInternships);
            }
        }

        private async Task UpdateProfessorInternshipStatusDirectly(int internshipId, string newStatus)
        {
            try
            {
                var internship = await dbContext.ProfessorInternships
                    .FirstOrDefaultAsync(i => i.Id == internshipId);

                if (internship != null)
                {
                    internship.ProfessorUploadedInternshipStatus = newStatus;

                    // If status is changed to "Αποσυρμένη", handle student applications
                    if (newStatus == "Αποσυρμένη")
                    {
                        var rngForInternship = internship.RNGForInternshipUploadedAsProfessor;

                        var applications = await dbContext.ProfessorInternshipsApplied
                            .Where(a => a.RNGForProfessorInternshipApplied == rngForInternship)
                            .ToListAsync();

                        foreach (var application in applications)
                        {
                            application.InternshipStatusAppliedAtTheProfessorSide = "Απορρίφθηκε (Απόσυρση Θέσεως Από Καθηγητή)";
                            application.InternshipStatusAppliedAtTheStudentSide = "Απορρίφθηκε (Απόσυρση Θέσεως Από Καθηγητή)";
                        }
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating professor internship status for internship {internshipId}: {ex.Message}");
            }
        }

        private async Task ExecuteBulkStatusChangeForProfessorInternships(string newStatus)
        {
            if (selectedProfessorInternshipIds.Count == 0) return;

            bulkActionForProfessorInternships = "status";
            newStatusForBulkActionForProfessorInternships = newStatus;

            selectedProfessorInternshipsForAction = FilteredInternshipsAsProfessor
                .Where(i => selectedProfessorInternshipIds.Contains(i.Id))
                .ToList();

            await ExecuteBulkActionForProfessorInternships();
        }

        private async Task ExecuteBulkCopyForProfessorInternships()
        {
            if (selectedProfessorInternshipIds.Count == 0) return;

            bulkActionForProfessorInternships = "copy";

            selectedProfessorInternshipsForAction = FilteredInternshipsAsProfessor
                .Where(i => selectedProfessorInternshipIds.Contains(i.Id))
                .ToList();

            await ExecuteBulkActionForProfessorInternships();
        }

        // Computed properties for pagination (matching parameter names)
        private int currentPageForProfessorInternships => currentPage_ProfessorInternships;
        private int totalPagesForProfessorInternships => totalPages_ProfessorInternships;
        private int professorInternshipsPerPage => itemsPerPage_ProfessorInternships;

        // Wrapper methods to match parameter names
        private List<int> GetVisiblePagesForProfessorInternships() => GetVisiblePages_ProfessorInternships();
        private void GoToFirstPageForProfessorInternships() { GoToFirstPage_ProfessorInternships(); StateHasChanged(); }
        private void PreviousPageForProfessorInternships() { PreviousPage_ProfessorInternships(); }
        private void GoToPageForProfessorInternships(int page) { GoToPage_ProfessorInternships(page); }
        private void NextPageForProfessorInternships() { NextPage_ProfessorInternships(); }
        private void GoToLastPageForProfessorInternships() { GoToLastPage_ProfessorInternships(); StateHasChanged(); }

        // Core Applicant Action Methods
        private async Task AcceptInternshipApplicationAsProfessor_MadeByStudent(long internshipRNG, string studentUniqueID)
        {
            try
            {
                // Fetch application with related internship
                var application = await dbContext.ProfessorInternshipsApplied
                    .Join(dbContext.ProfessorInternships.Include(i => i.Professor),
                        applied => applied.RNGForProfessorInternshipApplied,
                        internship => internship.RNGForInternshipUploadedAsProfessor,
                        (applied, internship) => new { Application = applied, Internship = internship })
                    .FirstOrDefaultAsync(x => x.Application.RNGForProfessorInternshipApplied == internshipRNG &&
                                            x.Application.StudentDetails.StudentUniqueIDAppliedForProfessorInternship == studentUniqueID);

                if (application == null)
                {
                    await SafeInvokeJsAsync(() => JS.InvokeVoidAsync("confirmActionWithHTML2", 
                        "Δεν βρέθηκε η Αίτηση ή ο Φοιτητής").AsTask());
                    return;
                }

                // Update status directly on the main application entity
                application.Application.InternshipStatusAppliedAtTheProfessorSide = "Επιτυχής";
                application.Application.InternshipStatusAppliedAtTheStudentSide = "Επιτυχής";

                await dbContext.SaveChangesAsync();

                try
                {
                    // Get student details
                    var student = await GetStudentDetails(application.Application.StudentDetails.StudentEmailAppliedForProfessorInternship);

                    // Send acceptance email to student
                    await InternshipEmailService.SendAcceptanceEmailAsProfessorToStudentAfterHeAppliedForInternshipPosition(
                        application.Application.StudentDetails.StudentEmailAppliedForProfessorInternship.Trim(),
                        student?.Name,
                        student?.Surname,
                        application.Internship.ProfessorInternshipTitle,
                        application.Application.RNGForProfessorInternshipApplied_HashedAsUniqueID,
                        $"{application.Internship.Professor.ProfName} {application.Internship.Professor.ProfSurname}"
                    );

                    // Send notification email to professor
                    await InternshipEmailService.SendAcceptanceConfirmationEmailToProfessorAfterStudentAppliedForInternshipPosition(
                        application.Application.ProfessorDetails.ProfessorEmailWhereStudentAppliedForProfessorInternship.Trim(),
                        $"{application.Internship.Professor.ProfName} {application.Internship.Professor.ProfSurname}",
                        student?.Name,
                        student?.Surname,
                        application.Application.RNGForProfessorInternshipApplied_HashedAsUniqueID,
                        application.Internship.ProfessorInternshipTitle
                    );

                    await SafeInvokeJsAsync(() => JS.InvokeVoidAsync("confirmActionWithHTML2", 
                        $"Ενημερώσεις Αποδοχής στάλθηκαν τόσο στον Φοιτητή " +
                        $"({student?.Name} {student?.Surname}) " +
                        $"όσο και στον Καθηγητή ({application.Internship.Professor.ProfName} {application.Internship.Professor.ProfSurname})").AsTask());

                    StateHasChanged();
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid email address format.");
                    await SafeInvokeJsAsync(() => JS.InvokeVoidAsync("confirmActionWithHTML2", 
                        "Μη έγκυρη διεύθυνση email.").AsTask());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting internship: {ex.Message} \n StackTrace: {ex.StackTrace}");
                await SafeInvokeJsAsync(() => JS.InvokeVoidAsync("confirmActionWithHTML2", 
                    $"Παρουσιάστηκε σφάλμα: {ex.Message}").AsTask());
            }
        }

        private async Task RejectInternshipApplicationAsProfessor_MadeByStudent(long internshipRNG, string studentUniqueID)
        {
            try
            {
                // Fetch application with related internship
                var application = await dbContext.ProfessorInternshipsApplied
                    .Join(dbContext.ProfessorInternships.Include(i => i.Professor),
                        applied => applied.RNGForProfessorInternshipApplied,
                        internship => internship.RNGForInternshipUploadedAsProfessor,
                        (applied, internship) => new { Application = applied, Internship = internship })
                    .FirstOrDefaultAsync(x => x.Application.RNGForProfessorInternshipApplied == internshipRNG &&
                                        x.Application.StudentDetails.StudentUniqueIDAppliedForProfessorInternship == studentUniqueID);

                if (application == null)
                {
                    await SafeInvokeJsAsync(() => JS.InvokeVoidAsync("confirmActionWithHTML2", 
                        "Δεν βρέθηκε η Αίτηση ή ο Φοιτητής").AsTask());
                    return;
                }

                // Update status directly on the main application entity
                application.Application.InternshipStatusAppliedAtTheProfessorSide = "Απορρίφθηκε";
                application.Application.InternshipStatusAppliedAtTheStudentSide = "Απορρίφθηκε";

                await dbContext.SaveChangesAsync();

                try
                {
                    // Get student details
                    var student = await GetStudentDetails(application.Application.StudentDetails.StudentEmailAppliedForProfessorInternship);

                    // Send rejection email to student
                    await InternshipEmailService.SendRejectionEmailAsProfessorToStudentAfterHeAppliedForInternshipPosition(
                        application.Application.StudentDetails.StudentEmailAppliedForProfessorInternship.Trim(),
                        student?.Name,
                        student?.Surname,
                        application.Internship.ProfessorInternshipTitle,
                        application.Application.RNGForProfessorInternshipApplied_HashedAsUniqueID,
                        $"{application.Internship.Professor.ProfName} {application.Internship.Professor.ProfSurname}"
                    );

                    // Send notification email to professor
                    await InternshipEmailService.SendRejectionConfirmationEmailToProfessorAfterStudentAppliedForInternshipPosition(
                        application.Application.ProfessorDetails.ProfessorEmailWhereStudentAppliedForProfessorInternship.Trim(),
                        $"{application.Internship.Professor.ProfName} {application.Internship.Professor.ProfSurname}",
                        student?.Name,
                        student?.Surname,
                        application.Application.RNGForProfessorInternshipApplied_HashedAsUniqueID,
                        application.Internship.ProfessorInternshipTitle
                    );

                    await SafeInvokeJsAsync(() => JS.InvokeVoidAsync("confirmActionWithHTML2",
                        $"Η Απόρριψη της Αίτησης κοινοποιήθηκε μέσω Email τόσο στον Φοιτητή " +
                        $"({student?.Name} {student?.Surname}) " +
                        $"όσο και στον Καθηγητή ({application.Internship.Professor.ProfName} {application.Internship.Professor.ProfSurname})").AsTask());

                    StateHasChanged();
                }
                catch (FormatException)
                {
                    Console.WriteLine("Invalid email address format.");
                    await SafeInvokeJsAsync(() => JS.InvokeVoidAsync("confirmActionWithHTML2", 
                        "Μη έγκυρη διεύθυνση email.").AsTask());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rejecting internship: {ex.Message} \n StackTrace: {ex.StackTrace}");
                await SafeInvokeJsAsync(() => JS.InvokeVoidAsync("confirmActionWithHTML2", 
                    $"Παρουσιάστηκε σφάλμα: {ex.Message}").AsTask());
            }
        }

        // Bulk Applicant Action Methods
        private async Task ExecuteBulkActionForProfessorInternshipApplicants()
        {
            if (string.IsNullOrEmpty(pendingBulkActionForProfessorInternshipApplicants) || selectedProfessorInternshipApplicantIds.Count == 0)
                return;

            // Check slot availability for bulk acceptance
            if (pendingBulkActionForProfessorInternshipApplicants == "accept")
            {
                // Group applicants by internship (tuple is (long RNG, string studentId))
                var applicantsByInternshipForProfessorInternship = selectedProfessorInternshipApplicantIds
                    .GroupBy(x => x.Item1)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var kvp in applicantsByInternshipForProfessorInternship)
                {
                    var internshipRNG = kvp.Key;
                    var applicants = kvp.Value;

                    var internship = professorInternships.FirstOrDefault(t => t.RNGForInternshipUploadedAsProfessor == internshipRNG);
                    if (internship == null) continue;

                    int acceptedCountForProfessorInternship = acceptedApplicantsCountPerProfessorInternship.GetValueOrDefault(internshipRNG, 0);
                    int availableSlotsForProfessorInternship = availableSlotsPerProfessorInternship.GetValueOrDefault(internshipRNG, internship.OpenSlots_ProfessorInternship);
                    int applicantsToAcceptForProfessorInternship = applicants.Count;

                    if (acceptedCountForProfessorInternship + applicantsToAcceptForProfessorInternship > availableSlotsForProfessorInternship)
                    {
                        // Close email modal first before showing slot warning
                        CloseEmailConfirmationModalForProfessorInternshipApplicants();
            
                        slotWarningMessageForProfessorInternship = $"Για την πρακτική άσκηση '{internship.ProfessorInternshipTitle}' έχετε Αποδεχτεί {acceptedCountForProfessorInternship}/{availableSlotsForProfessorInternship} Αιτούντες, επιλέξατε να αποδεχτείτε {applicantsToAcceptForProfessorInternship} ακόμη. Πρέπει να αλλάξετε τον Αριθμό των διαθέσιμων Slots της Πρακτικής Άσκησης για να προχωρήσετε";
                        showSlotWarningModalForProfessorInternship = true;
                        StateHasChanged();
                        return; // Stop execution if any internship exceeds slots
                    }
                }
            }

            // Show confirmation dialog
            var actionText = pendingBulkActionForProfessorInternshipApplicants == "accept" ? "ΑΠΟΔΟΧΗ" : "ΑΠΟΡΡΙΨΗ";
            var emailText = sendEmailsForBulkProfessorInternshipAction ? "και αποστολή email" : "χωρίς αποστολή email";
            var confirmMessage = $"- ΜΑΖΙΚΗ {actionText} ΑΙΤΗΣΕΩΝ ΠΡΑΚΤΙΚΗΣ - \n" +
                                $"Θα εφαρμοστεί σε {selectedProfessorInternshipApplicantIds.Count} αιτήσεις {emailText}.\n\n" +
                                $"Η ενέργεια αυτή δεν θα μπορεί να αναιρεθεί. Είστε σίγουρος/η;";

            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", confirmMessage);

            if (isConfirmed)
            {
                try
                {
                    var successCount = 0;
                    var failCount = 0;

                    // Store the email setting for this bulk operation
                    var shouldSendEmails = sendEmailsForBulkProfessorInternshipAction;

                    foreach (var (rng, studentId) in selectedProfessorInternshipApplicantIds)
                    {
                        try
                        {
                            if (pendingBulkActionForProfessorInternshipApplicants == "accept")
                            {
                                await AcceptInternshipApplicationAsProfessor_MadeByStudent_Bulk(rng, studentId, shouldSendEmails);
                                // Update slot count for accepted applications
                                acceptedApplicantsCountPerProfessorInternship[rng] = 
                                    acceptedApplicantsCountPerProfessorInternship.GetValueOrDefault(rng, 0) + 1;
                            }
                            else
                            {
                                await RejectInternshipApplicationAsProfessor_MadeByStudent_Bulk(rng, studentId, shouldSendEmails);
                            }
                            successCount++;

                            // Small delay to avoid overwhelming the server
                            await Task.Delay(100);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing professor internship application {rng}-{studentId}: {ex.Message}");
                            failCount++;
                        }
                    }

                    // Show results summary
                    var resultMessage = $"Ολοκληρώθηκε η μαζική ενέργεια!\n\n" +
                                    $"Επιτυχείς Ενημερώσεις: {successCount}\n" +
                                    $"Αποτυχίες Ενημερώσεων: {failCount}";

                    if (shouldSendEmails)
                    {
                        resultMessage += $"\n\nΑποστολή Email: Ολοκληρώθηκε";
                    }
                    else
                    {
                        resultMessage += $"\n\nΔεν στάλθηκαν email ειδοποιήσεων.\n" +
                                    $"Οι φοιτητές θα δουν την αλλαγή κατάστασης όταν συνδεθούν στην εφαρμογή.";
                    }

                    await SafeInvokeJsAsync(() => JS.InvokeVoidAsync("confirmActionWithHTML2", resultMessage).AsTask());

                    // Set the flag to trigger refresh
                    actionsPerformedToAcceptorRejectInternshipsAsProfessor = true;
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    await SafeInvokeJsAsync(() => JS.InvokeVoidAsync("confirmActionWithHTML2", 
                        $"Σφάλμα κατά την μαζική επεξεργασία: {ex.Message}").AsTask());
                }
                finally
                {
                    CloseEmailConfirmationModalForProfessorInternshipApplicants();
                    CancelBulkEditForProfessorInternshipApplicants();
                    // Reset email option to default for next time
                    sendEmailsForBulkProfessorInternshipAction = true;
                }
            }
            else
            {
                CloseEmailConfirmationModalForProfessorInternshipApplicants();
            }
        }

        private async Task AcceptInternshipApplicationAsProfessor_MadeByStudent_Bulk(long internshipRNG, string studentUniqueID, bool sendEmails)
        {
            try
            {
                // Fetch application with related internship
                var application = await dbContext.ProfessorInternshipsApplied
                    .Join(dbContext.ProfessorInternships.Include(i => i.Professor),
                        applied => applied.RNGForProfessorInternshipApplied,
                        internship => internship.RNGForInternshipUploadedAsProfessor,
                        (applied, internship) => new { Application = applied, Internship = internship })
                    .FirstOrDefaultAsync(x => x.Application.RNGForProfessorInternshipApplied == internshipRNG &&
                                            x.Application.StudentDetails.StudentUniqueIDAppliedForProfessorInternship == studentUniqueID);

                if (application == null)
                {
                    Console.WriteLine($"Professor internship application not found: {internshipRNG}-{studentUniqueID}");
                    return;
                }

                // Update status directly on the main application entity
                application.Application.InternshipStatusAppliedAtTheProfessorSide = "Επιτυχής";
                application.Application.InternshipStatusAppliedAtTheStudentSide = "Επιτυχής";

                await dbContext.SaveChangesAsync();

                if (sendEmails)
                {
                    try
                    {
                        // Get student details
                        var student = await GetStudentDetails(application.Application.StudentDetails.StudentEmailAppliedForProfessorInternship);

                        // Send acceptance email to student
                        await InternshipEmailService.SendAcceptanceEmailAsProfessorToStudentAfterHeAppliedForInternshipPosition(
                            application.Application.StudentDetails.StudentEmailAppliedForProfessorInternship.Trim(),
                            student?.Name,
                            student?.Surname,
                            application.Internship.ProfessorInternshipTitle,
                            application.Application.RNGForProfessorInternshipApplied_HashedAsUniqueID,
                            $"{application.Internship.Professor.ProfName} {application.Internship.Professor.ProfSurname}"
                        );

                        // Send notification email to professor
                        await InternshipEmailService.SendAcceptanceConfirmationEmailToProfessorAfterStudentAppliedForInternshipPosition(
                            application.Application.ProfessorDetails.ProfessorEmailWhereStudentAppliedForProfessorInternship.Trim(),
                            $"{application.Internship.Professor.ProfName} {application.Internship.Professor.ProfSurname}",
                            student?.Name,
                            student?.Surname,
                            application.Application.RNGForProfessorInternshipApplied_HashedAsUniqueID,
                            application.Internship.ProfessorInternshipTitle
                        );

                        Console.WriteLine($"Professor internship acceptance emails sent for: {internshipRNG}-{studentUniqueID}");
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine($"Invalid email address format for professor internship: {internshipRNG}-{studentUniqueID}");
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"Error sending professor internship emails for {internshipRNG}-{studentUniqueID}: {emailEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting professor internship {internshipRNG}-{studentUniqueID}: {ex.Message}");
                throw;
            }
        }

        private async Task RejectInternshipApplicationAsProfessor_MadeByStudent_Bulk(long internshipRNG, string studentUniqueID, bool sendEmails)
        {
            try
            {
                // Fetch application with related internship
                var application = await dbContext.ProfessorInternshipsApplied
                    .Join(dbContext.ProfessorInternships.Include(i => i.Professor),
                        applied => applied.RNGForProfessorInternshipApplied,
                        internship => internship.RNGForInternshipUploadedAsProfessor,
                        (applied, internship) => new { Application = applied, Internship = internship })
                    .FirstOrDefaultAsync(x => x.Application.RNGForProfessorInternshipApplied == internshipRNG &&
                                        x.Application.StudentDetails.StudentUniqueIDAppliedForProfessorInternship == studentUniqueID);

                if (application == null)
                {
                    Console.WriteLine($"Professor internship application not found: {internshipRNG}-{studentUniqueID}");
                    return;
                }

                // Update status directly on the main application entity
                application.Application.InternshipStatusAppliedAtTheProfessorSide = "Απορρίφθηκε";
                application.Application.InternshipStatusAppliedAtTheStudentSide = "Απορρίφθηκε";

                await dbContext.SaveChangesAsync();

                if (sendEmails)
                {
                    try
                    {
                        // Get student details
                        var student = await GetStudentDetails(application.Application.StudentDetails.StudentEmailAppliedForProfessorInternship);

                        // Send rejection email to student
                        await InternshipEmailService.SendRejectionEmailAsProfessorToStudentAfterHeAppliedForInternshipPosition(
                            application.Application.StudentDetails.StudentEmailAppliedForProfessorInternship.Trim(),
                            student?.Name,
                            student?.Surname,
                            application.Internship.ProfessorInternshipTitle,
                            application.Application.RNGForProfessorInternshipApplied_HashedAsUniqueID,
                            $"{application.Internship.Professor.ProfName} {application.Internship.Professor.ProfSurname}"
                        );

                        // Send notification email to professor
                        await InternshipEmailService.SendRejectionConfirmationEmailToProfessorAfterStudentAppliedForInternshipPosition(
                            application.Application.ProfessorDetails.ProfessorEmailWhereStudentAppliedForProfessorInternship.Trim(),
                            $"{application.Internship.Professor.ProfName} {application.Internship.Professor.ProfSurname}",
                            student?.Name,
                            student?.Surname,
                            application.Application.RNGForProfessorInternshipApplied_HashedAsUniqueID,
                            application.Internship.ProfessorInternshipTitle
                        );

                        Console.WriteLine($"Professor internship rejection emails sent for: {internshipRNG}-{studentUniqueID}");
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine($"Invalid email address format for professor internship: {internshipRNG}-{studentUniqueID}");
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"Error sending professor internship emails for {internshipRNG}-{studentUniqueID}: {emailEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rejecting professor internship {internshipRNG}-{studentUniqueID}: {ex.Message}");
                throw;
            }
        }

        // Helper Methods
        private async Task<QuizManager.Models.Student?> GetStudentDetails(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            try
            {
                return await dbContext.Students
                    .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting student details: {ex.Message}");
                return null;
            }
        }

        private async Task SafeInvokeJsAsync(Func<Task> jsAction)
        {
            try
            {
                if (JS != null)
                {
                    await jsAction();
                }
            }
            catch (JSDisconnectedException)
            {
                Console.WriteLine("JS interop call failed because the circuit is disconnected.");
            }
        }
    }
}

