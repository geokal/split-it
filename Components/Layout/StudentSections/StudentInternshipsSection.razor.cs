using Microsoft.AspNetCore.Components;
using QuizManager.ViewModels;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.StudentSections
{
    public partial class StudentInternshipsSection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private InternshipEmailService InternshipEmailService { get; set; } = default!;

        // Internships Display Visibility
        private bool showStudentInternshipApplications = false;
        private bool isLoadingStudentInternshipApplications = false;

        // Internship Applications Data
        private List<InternshipApplied> internshipApplications = new List<InternshipApplied>();
        private List<ProfessorInternshipApplied> professorInternshipApplications = new List<ProfessorInternshipApplied>();
        private bool showCompanyInternshipApplications = true;
        private bool showProfessorInternshipApplications = true;
        private Dictionary<long, CompanyInternship> internshipDataCache = new Dictionary<long, CompanyInternship>();
        private Dictionary<long, ProfessorInternship> professorInternshipDataCache = new Dictionary<long, ProfessorInternship>();
        private int totalInternshipCountForInternshipsToSee = 0;

        // Pagination for Applications
        private int currentPageForInternshipsToSee = 1;
        private int pageSizeForInternshipsToSee = 10;
        private int totalPagesForInternshipsToSee = 1;
        private int[] pageSizeOptions_SeeMyInternshipApplicationsAsStudent = new[] { 10, 50, 100 };

        // Withdraw Application
        private bool showLoadingModalWhenWithdrawInternshipApplication = false;
        private int loadingProgressWhenWithdrawInternshipApplication = 0;

        // Company Details Modals
        private QuizManager.Models.Company selectedCompanyDetails_StudentInternshipApplications;
        private bool showCompanyDetailsModal_StudentInternshipApplications = false;
        private Dictionary<string, QuizManager.Models.Company> companyDataCache = new Dictionary<string, QuizManager.Models.Company>();

        // Internship Details Modals
        private CompanyInternship selectedCompanyInternshipDetails_StudentInternshipApplications;
        private bool showCompanyInternshipDetailsModal_StudentInternshipApplications = false;

        // Company Details from Internship Display (Search Results)
        private QuizManager.Models.Company selectedCompanyDetailsFromInternship;
        private bool showCompanyDetailsModalFromInternship = false;

        // Selected Entities for Details
        private QuizManager.Models.Company selectedCompany;
        private QuizManager.Models.Professor selectedProfessor;
        private CompanyInternship selectedCompanyInternshipDetails;
        private CompanyInternship currentInternship;
        private ProfessorInternship currentProfessorInternship;

        // Details for Hyperlink Names
        private QuizManager.Models.Company selectedCompanyDetailsForHyperlinkNameInInternshipAsStudent;
        private QuizManager.Models.Professor selectedProfessorDetailsForHyperlinkNameInInternshipAsStudent;

        // Search Filters
        private bool isInternshipSearchAsStudentFiltersVisible = false;
        private bool isInternshipAreasVisible = false;
        private HashSet<int> expandedInternshipAreas = new HashSet<int>();
        private string companyinternshipSearchByRegion = "";
        private List<string> internshipTitleAutocompleteSuggestionsWhenSearchInternshipAsStudent = new List<string>();
        private bool isLoadingSearchInternshipsAsStudent = false;

        // Pagination for Search
        private int currentInternshipPage = 1;
        private int totalInternshipPages = 1;

        // Region to Towns Map
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

        // Computed Properties
        private List<object> allInternshipApplications
        {
            get
            {
                var all = new List<object>();
                if (showCompanyInternshipApplications && internshipApplications != null)
                    all.AddRange(internshipApplications);
                if (showProfessorInternshipApplications && professorInternshipApplications != null)
                    all.AddRange(professorInternshipApplications);
                return all;
            }
        }

        private List<object> paginatedInternships
        {
            get
            {
                return allInternshipApplications
                    .Skip((currentPageForInternshipsToSee - 1) * pageSizeForInternshipsToSee)
                    .Take(pageSizeForInternshipsToSee)
                    .ToList();
            }
        }

        // Main Methods
        private async Task ToggleAndLoadStudentInternshipApplications()
        {
            showStudentInternshipApplications = !showStudentInternshipApplications;

            if (showStudentInternshipApplications)
            {
                isLoadingStudentInternshipApplications = true;
                StateHasChanged();
            
                try
                {
                    await LoadUserInternshipApplications();
                }
                finally
                {
                    isLoadingStudentInternshipApplications = false;
                    StateHasChanged();
                }
            }
            else
            {
                StateHasChanged();
            }
        }

        private async Task LoadUserInternshipApplications()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            // Initialize lists and caches
            internshipApplications = new List<InternshipApplied>();
            professorInternshipApplications = new List<ProfessorInternshipApplied>();
            internshipDataCache = new Dictionary<long, CompanyInternship>();
            professorInternshipDataCache = new Dictionary<long, ProfessorInternship>();

            if (user.Identity.IsAuthenticated)
            {
                var userEmail = user.FindFirst("name")?.Value;

                if (!string.IsNullOrEmpty(userEmail))
                {
                    // Get student details
                    var student = await dbContext.Students
                    .FirstOrDefaultAsync(s => s.Email == userEmail);

                    if (student != null)
                    {
                        // Fetch company internship applications with details
                        internshipApplications = await dbContext.InternshipsApplied
                            .Include(app => app.StudentDetails)
                            .Include(app => app.CompanyDetails)
                            .Where(app => app.StudentDetails.StudentUniqueIDAppliedForInternship == student.Student_UniqueID)
                            .ToListAsync();

                        // Load all related company internships in one query
                        var companyInternshipRNGs = internshipApplications.Select(a => a.RNGForInternshipApplied).ToList();
                        var companyInternships = await dbContext.CompanyInternships
                            .Include(i => i.Company)
                            .Where(i => companyInternshipRNGs.Contains(i.RNGForInternshipUploadedAsCompany))
                            .ToListAsync();

                        // Populate company internship cache
                        foreach (var internship in companyInternships)
                        {
                            internshipDataCache[internship.RNGForInternshipUploadedAsCompany] = internship;
                        }

                        // Fetch professor internship applications with details
                        professorInternshipApplications = await dbContext.ProfessorInternshipsApplied
                            .Include(app => app.StudentDetails)
                            .Include(app => app.ProfessorDetails)
                            .Where(app => app.StudentDetails.StudentUniqueIDAppliedForProfessorInternship == student.Student_UniqueID)
                            .ToListAsync();

                        // Load all related professor internships in one query
                        var professorInternshipRNGs = professorInternshipApplications.Select(a => a.RNGForProfessorInternshipApplied).ToList();
                        var professorInternships = await dbContext.ProfessorInternships
                            .Include(i => i.Professor)
                            .Where(i => professorInternshipRNGs.Contains(i.RNGForInternshipUploadedAsProfessor))
                            .ToListAsync();

                        // Populate professor internship cache
                        foreach (var internship in professorInternships)
                        {
                            professorInternshipDataCache[internship.RNGForInternshipUploadedAsProfessor] = internship;
                        }
                    }
                }
            }

            UpdateTotalInternshipCount();
            StateHasChanged();
        }

        private async Task FilterInternshipApplications(ChangeEventArgs e)
        {
            var filterValue = e.Value?.ToString() ?? "all";

            if (filterValue == "company")
            {
                showCompanyInternshipApplications = true;
                showProfessorInternshipApplications = false;
            }
            else if (filterValue == "professor")
            {
                showCompanyInternshipApplications = false;
                showProfessorInternshipApplications = true;
            }
            else
            {
                showCompanyInternshipApplications = true;
                showProfessorInternshipApplications = true;
            }

            await Task.Delay(100);
            UpdateTotalInternshipCount();
            currentPageForInternshipsToSee = 1;
            StateHasChanged();
        }

        private void SetTotalInternshipCount(int count)
        {
            totalInternshipCountForInternshipsToSee = count;
            UpdatePagination();
        }

        private void UpdateTotalInternshipCount()
        {
            totalInternshipCountForInternshipsToSee = allInternshipApplications.Count;
            UpdatePagination();
        }

        private void UpdatePagination()
        {
            totalPagesForInternshipsToSee = (int)Math.Ceiling((double)totalInternshipCountForInternshipsToSee / pageSizeForInternshipsToSee);
            if (totalPagesForInternshipsToSee < 1) totalPagesForInternshipsToSee = 1;
            StateHasChanged();
        }

        // Pagination Methods
        private void GoToFirstPageForInternships()
        {
            currentPageForInternshipsToSee = 1;
            StateHasChanged();
        }

        private void GoToLastPageForInternships()
        {
            currentPageForInternshipsToSee = totalPagesForInternshipsToSee;
            StateHasChanged();
        }

        private void GoToPageForInternships(int page)
        {
            if (page > 0 && page <= totalPagesForInternshipsToSee)
            {
                currentPageForInternshipsToSee = page;
                StateHasChanged();
            }
        }

        private void PreviousPageForInternshipsToSee()
        {
            if (currentPageForInternshipsToSee > 1)
            {
                currentPageForInternshipsToSee--;
                StateHasChanged();
            }
        }

        private void NextPageForInternshipsToSee()
        {
            if (currentPageForInternshipsToSee < totalPagesForInternshipsToSee)
            {
                currentPageForInternshipsToSee++;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePagesForInternships()
        {
            var pages = new List<int>();
            int current = currentPageForInternshipsToSee;
            int total = totalPagesForInternshipsToSee;
        
            pages.Add(1);
            if (current > 3) pages.Add(-1);
        
            int start = Math.Max(2, current - 1);
            int end = Math.Min(total - 1, current + 1);
        
            for (int i = start; i <= end; i++)
            {
                pages.Add(i);
            }
        
            if (current < total - 2) pages.Add(-1);
        
            if (total > 1) pages.Add(total);
        
            return pages;
        }

        private void OnPageSizeChangeForApplications_SeeMyInternshipApplicationsAsStudent(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                pageSizeForInternshipsToSee = newSize;
                currentPageForInternshipsToSee = 1;
                UpdatePagination();
                StateHasChanged();
            }
        }

        // Withdraw Application
        private async Task WithdrawInternshipApplicationMadeByStudent(object applicationObj)
        {
            await WithdrawInternshipApplication(applicationObj);
        }

        private async Task WithdrawInternshipApplication(object applicationObj)
        {
            try
            {
                if (applicationObj is InternshipApplied companyInternship)
                {
                    var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", 
                        $"Πρόκεται να αποσύρετε την Αίτησή σας για την Πρακτική Άσκηση. Είστε σίγουρος/η;");
                    if (!confirmed) return;

                    // Show loading modal after user confirms
                    showLoadingModalWhenWithdrawInternshipApplication = true;
                    loadingProgressWhenWithdrawInternshipApplication = 0;
                    StateHasChanged();

                    // Step 1: Get the related internship details
                    await UpdateProgressWhenWithdrawInternshipApplication(10, 200);
                
                    var internship = await dbContext.CompanyInternships
                        .Include(i => i.Company)
                        .FirstOrDefaultAsync(i => i.RNGForInternshipUploadedAsCompany == companyInternship.RNGForInternshipApplied);

                    if (internship == null)
                    {
                        showLoadingModalWhenWithdrawInternshipApplication = false;
                        StateHasChanged();
                        await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε η πρακτική άσκηση.");
                        return;
                    }

                    // Step 2: Update status
                    await UpdateProgressWhenWithdrawInternshipApplication(30, 200);
                
                    companyInternship.InternshipStatusAppliedAtTheCompanySide = "Αποσύρθηκε από τον φοιτητή";
                    companyInternship.InternshipStatusAppliedAtTheStudentSide = "Αποσύρθηκε από τον φοιτητή";

                    // Step 3: Add platform action
                    await UpdateProgressWhenWithdrawInternshipApplication(50, 200);
                
                    var platformAction = new PlatformActions
                    {
                        UserRole_PerformedAction = "STUDENT",
                        ForWhat_PerformedAction = "COMPANY_INTERNSHIP",
                        HashedPositionRNG_PerformedAction = HashLong(companyInternship.RNGForInternshipApplied),
                        TypeOfAction_PerformedAction = "SELFWITHDRAW",
                        DateTime_PerformedAction = DateTime.Now
                    };

                    dbContext.PlatformActions.Add(platformAction);
                    await dbContext.SaveChangesAsync();
                
                    await UpdateProgressWhenWithdrawInternshipApplication(70, 200);

                    // Step 4: Get student details
                    await UpdateProgressWhenWithdrawInternshipApplication(80, 200);
                
                    var student = await GetStudentDetails(companyInternship.StudentEmailAppliedForInternship);
                    if (student == null)
                    {
                        showLoadingModalWhenWithdrawInternshipApplication = false;
                        StateHasChanged();
                        await JS.InvokeVoidAsync("alert", "Δεν βρέθηκαν στοιχεία φοιτητή.");
                        return;
                    }

                    // Step 5: Send notifications
                    await UpdateProgressWhenWithdrawInternshipApplication(90, 200);
                
                    await InternshipEmailService.SendInternshipWithdrawalNotificationToCompany_AsStudent(
                        companyInternship.CompanyEmailWhereStudentAppliedForInternship,
                        internship.Company?.CompanyName,
                        student.Name,
                        student.Surname,
                        internship.CompanyInternshipTitle,
                        companyInternship.RNGForInternshipAppliedAsStudent_HashedAsUniqueID);

                    await InternshipEmailService.SendInternshipWithdrawalConfirmationToStudent_AsCompany(
                        companyInternship.StudentEmailAppliedForInternship,
                        student.Name,
                        student.Surname,
                        internship.CompanyInternshipTitle,
                        companyInternship.RNGForInternshipAppliedAsStudent_HashedAsUniqueID,
                        internship.Company?.CompanyName);

                    await UpdateProgressWhenWithdrawInternshipApplication(100, 200);
                
                    // Small delay to show completion
                    await Task.Delay(500);
                
                    // Hide loading modal before navigation
                    showLoadingModalWhenWithdrawInternshipApplication = false;
                    StateHasChanged();

                    // AFTER user clicks OK, show the navigation loader
                    await Task.Delay(100);
            
                    // Show the navigation loader
                    await JS.InvokeVoidAsync("showBlazorNavigationLoader", "Παρακαλώ Περιμένετε...");
            
                    // Give time for loader to render
                    await Task.Delay(300);
                
                    NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
                }
                else if (applicationObj is ProfessorInternshipApplied professorInternship)
                {
                    var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", 
                        $"Πρόκεται να αποσύρετε την Αίτησή σας για την Πρακτική Άσκηση. Είστε σίγουρος/η;");
                    if (!confirmed) return;

                    // Show loading modal after user confirms
                    showLoadingModalWhenWithdrawInternshipApplication = true;
                    loadingProgressWhenWithdrawInternshipApplication = 0;
                    StateHasChanged();

                    // Step 1: Get the related internship details
                    await UpdateProgressWhenWithdrawInternshipApplication(10, 200);
                
                    var internship = await dbContext.ProfessorInternships
                        .Include(i => i.Professor)
                        .FirstOrDefaultAsync(i => i.RNGForInternshipUploadedAsProfessor == professorInternship.RNGForProfessorInternshipApplied);

                    if (internship == null)
                    {
                        showLoadingModalWhenWithdrawInternshipApplication = false;
                        StateHasChanged();
                        await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε η πρακτική άσκηση.");
                        return;
                    }

                    // Step 2: Update status
                    await UpdateProgressWhenWithdrawInternshipApplication(30, 200);
                
                    professorInternship.InternshipStatusAppliedAtTheStudentSide = "Αποσύρθηκε από τον φοιτητή";
                    professorInternship.InternshipStatusAppliedAtTheProfessorSide = "Αποσύρθηκε από τον φοιτητή";

                    // Step 3: Add platform action
                    await UpdateProgressWhenWithdrawInternshipApplication(50, 200);
                
                    var platformAction = new PlatformActions
                    {
                        UserRole_PerformedAction = "STUDENT",
                        ForWhat_PerformedAction = "PROFESSOR_INTERNSHIP",
                        HashedPositionRNG_PerformedAction = HashLong(professorInternship.RNGForProfessorInternshipApplied),
                        TypeOfAction_PerformedAction = "SELFWITHDRAW",
                        DateTime_PerformedAction = DateTime.Now
                    };

                    dbContext.PlatformActions.Add(platformAction);
                    await dbContext.SaveChangesAsync();
                
                    await UpdateProgressWhenWithdrawInternshipApplication(70, 200);

                    // Step 4: Get student details
                    await UpdateProgressWhenWithdrawInternshipApplication(80, 200);
                
                    var student = await GetStudentDetails(professorInternship.StudentEmailAppliedForProfessorInternship);
                    if (student == null)
                    {
                        showLoadingModalWhenWithdrawInternshipApplication = false;
                        StateHasChanged();
                        await JS.InvokeVoidAsync("alert", "Δεν βρέθηκαν στοιχεία φοιτητή.");
                        return;
                    }

                    var professorName = internship.Professor != null 
                        ? $"{internship.Professor.ProfName} {internship.Professor.ProfSurname}" 
                        : "Άγνωστος Καθηγητής";

                    // Step 5: Send notifications
                    await UpdateProgressWhenWithdrawInternshipApplication(90, 200);
                
                    await InternshipEmailService.SendProfessorInternshipWithdrawalNotificationToProfessor(
                        professorInternship.ProfessorEmailWhereStudentAppliedForInternship,
                        professorName,
                        student.Name,
                        student.Surname,
                        internship.ProfessorInternshipTitle,
                        professorInternship.RNGForProfessorInternshipApplied_HashedAsUniqueID);

                    await InternshipEmailService.SendProfessorInternshipWithdrawalConfirmationToStudent(
                        professorInternship.StudentEmailAppliedForProfessorInternship,
                        student.Name,
                        student.Surname,
                        internship.ProfessorInternshipTitle,
                        professorInternship.RNGForProfessorInternshipApplied_HashedAsUniqueID,
                        professorName);

                    await UpdateProgressWhenWithdrawInternshipApplication(100, 200);
                
                    // Small delay to show completion
                    await Task.Delay(500);
                
                    // Hide loading modal before navigation
                    showLoadingModalWhenWithdrawInternshipApplication = false;
                    StateHasChanged();

                    // AFTER user clicks OK, show the navigation loader
                    await Task.Delay(100);
            
                    // Show the navigation loader
                    await JS.InvokeVoidAsync("showBlazorNavigationLoader", "Παρακαλώ Περιμένετε...");
            
                    // Give time for loader to render
                    await Task.Delay(300);
                
                    NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
                }
            }
            catch (Exception ex)
            {
                // Hide loading modal on error
                showLoadingModalWhenWithdrawInternshipApplication = false;
                StateHasChanged();
                Console.WriteLine($"Error withdrawing internship application: {ex.Message}");
                await JS.InvokeVoidAsync("alert", "Σφάλμα κατά την απόσυρση της αίτησης.");
            }
        }

        private async Task UpdateProgressWhenWithdrawInternshipApplication(int progress, int delayMs = 0)
        {
            loadingProgressWhenWithdrawInternshipApplication = progress;
            StateHasChanged();

            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }
        }

        // Company Details Modals
        private async Task ShowCompanyDetailsInInternshipCompanyName_StudentInternshipApplications(string companyEmail)
        {
            try
            {
                // First check if we already have the company details in cache
                if (companyDataCache.TryGetValue(companyEmail, out var cachedCompany))
                {
                    selectedCompanyDetails_StudentInternshipApplications = cachedCompany;
                }
                else
                {
                    // Fetch the company details from the database using email
                    selectedCompanyDetails_StudentInternshipApplications = await dbContext.Companies
                        .FirstOrDefaultAsync(c => c.CompanyEmail == companyEmail);

                    // Add to cache if found
                    if (selectedCompanyDetails_StudentInternshipApplications != null)
                    { 
                        companyDataCache[companyEmail] = selectedCompanyDetails_StudentInternshipApplications;
                    }
                }

                if (selectedCompanyDetails_StudentInternshipApplications != null)
                {
                    showCompanyDetailsModal_StudentInternshipApplications = true;
                    StateHasChanged();
                }
                else
                {
                    await JS.InvokeVoidAsync("alert", "Δεν βρέθηκαν στοιχεία εταιρείας");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading company details: {ex.Message}");
                await JS.InvokeVoidAsync("alert", "Σφάλμα φόρτωσης στοιχείων εταιρείας");
            }
        }

        private void CloseCompanyDetailsModal_StudentInternshipApplications()
        {
            showCompanyDetailsModal_StudentInternshipApplications = false;
            StateHasChanged();
        }

        // Internship Details Modals
        private async Task ShowInternshipDetailsInInternshipTitleAsHyperlink(long internshipRNG)
        {
            await ShowCompanyInternshipDetailsModal_StudentInternshipApplications(internshipRNG);
        }

        private async Task ShowCompanyInternshipDetailsModal_StudentInternshipApplications(long internshipRNG)
        {
            // Fetch the internship details asynchronously
            selectedCompanyInternshipDetails_StudentInternshipApplications = await dbContext.CompanyInternships
                .Include(i => i.Company)
                .FirstOrDefaultAsync(i => i.RNGForInternshipUploadedAsCompany == internshipRNG);

            if (selectedCompanyInternshipDetails_StudentInternshipApplications != null)
            {
                // Open the modal if the internship details are found
                showCompanyInternshipDetailsModal_StudentInternshipApplications = true;
                StateHasChanged();
            }
            else
            {
                // Show an alert if no internship details are found
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Δεν μπορούν να εμφανιστούν οι λεπτομέρειες της Πρακτικής. <span style='color:darkred;'>Η Πρακτική Δεν Είναι Πλέον Διαθέσιμη από τον Φορέα</span>");
            }
        }

        private void CloseCompanyInternshipDetailsModal_StudentInternshipApplications()
        {
            showCompanyInternshipDetailsModal_StudentInternshipApplications = false;
            StateHasChanged();
        }

        // Company Details from Internship Display (Search Results)
        private async Task ShowCompanyDetailsinTitleAsHyperlink(string companyEmail)
        {
            await ShowCompanyHyperlinkNameDetailsModalInStudentInternships(companyEmail);
        }

        private async Task ShowCompanyHyperlinkNameDetailsModalInStudentInternships(string companyEmail)
        {
            selectedCompanyDetailsFromInternship = await dbContext.Companies
                .FirstOrDefaultAsync(c => c.CompanyEmail == companyEmail);

            if (selectedCompanyDetailsFromInternship != null)
            {
                showCompanyDetailsModalFromInternship = true;
                StateHasChanged();
            }
        }

        private void CloseCompanyDetailsModalFromInternship()
        {
            showCompanyDetailsModalFromInternship = false;
            StateHasChanged();
        }

        // Modal Visibility States
        private bool isProfessorDetailsModalVisible_StudentInternshipApplicationsShow = false;
        private bool isInternshipDetailsModalVisible_StudentInternshipApplicationsShow = false;

        // Loading States for Withdrawal
        private bool showLoadingModalWhenWithdrawProfessorInternshipApplication = false;
        private int loadingProgressWhenWithdrawProfessorInternshipApplication = 0;

        // Helper Methods
        private async Task<QuizManager.Models.Student> GetStudentDetails(string email)
        {
            return await dbContext.Students
                .FirstOrDefaultAsync(s => s.Email == email);
        }

        // Helper method to get internship details from cache
        private CompanyInternship GetCompanyInternshipFromCache(long rng)
        {
            return internshipDataCache.TryGetValue(rng, out var internship) ? internship : null;
        }

        private ProfessorInternship GetProfessorInternshipFromCache(long rng)
        {
            return professorInternshipDataCache.TryGetValue(rng, out var internship) ? internship : null;
        }

        // Toggle Methods
        private void ToggleInternshipSearchAsStudentFiltersVisibility()
        {
            isInternshipSearchAsStudentFiltersVisible = !isInternshipSearchAsStudentFiltersVisible;
            StateHasChanged();
        }

        // Download Methods
        private async Task DownloadInternshipAttachmentAsStudent(long internshipId)
        {
            var internship = await dbContext.CompanyInternships.FirstOrDefaultAsync(i => i.Id == internshipId);
            if (internship == null || internship.CompanyInternshipAttachment == null)
            {
                await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε συνημμένο αρχείο.");
                return;
            }
            // File download logic here
        }

        private async Task DownloadStudentCVForProfessorInternships(string studentEmail)
        {
            try
            {
                var student = studentDataCache.Values.FirstOrDefault(s => s.Email == studentEmail);
                if (student == null)
                {
                    student = await dbContext.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
                }
                if (student?.Attachment == null)
                {
                    await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε βιογραφικό για αυτόν τον φοιτητή");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading CV: {ex.Message}");
            }
        }

        // Show Details Methods
        private async Task ShowStudentDetailsInNameAsHyperlink(string studentUniqueId, int applicationId, string applicationType)
        {
            try
            {
                var student = await dbContext.Students.FirstOrDefaultAsync(s => s.Student_UniqueID == studentUniqueId);
                if (student != null)
                {
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing student details: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        // Close Modal Methods
        private void CloseProfessorInternshipDetailsModal_StudentInternshipApplicationsShow()
        {
            isInternshipDetailsModalVisible_StudentInternshipApplicationsShow = false;
            StateHasChanged();
        }

        private void CloseProfessorDetailsModal_StudentInternshipApplications()
        {
            isProfessorDetailsModalVisible_StudentInternshipApplicationsShow = false;
            StateHasChanged();
        }

        private async Task UpdateProgressWhenWithdrawProfessorInternshipApplication(int current, int total)
        {
            loadingProgressWhenWithdrawProfessorInternshipApplication = (int)((double)current / total * 100);
            StateHasChanged();
            await Task.Delay(50);
        }

        private async Task WithdrawProfessorInternshipApplicationMadeByStudent(ProfessorInternshipApplied application)
        {
            try
            {
                var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", 
                    $"Πρόκεται να αποσύρετε την Αίτησή σας για την Πρακτική Άσκηση. Είστε σίγουρος/η;");
                if (!confirmed) return;

                showLoadingModalWhenWithdrawProfessorInternshipApplication = true;
                loadingProgressWhenWithdrawProfessorInternshipApplication = 0;
                StateHasChanged();

                await UpdateProgressWhenWithdrawProfessorInternshipApplication(10, 200);
                var internship = await dbContext.ProfessorInternships
                    .Include(i => i.Professor)
                    .FirstOrDefaultAsync(i => i.RNGForInternshipUploadedAsProfessor == application.RNGForProfessorInternshipApplied);

                if (internship == null)
                {
                    showLoadingModalWhenWithdrawProfessorInternshipApplication = false;
                    StateHasChanged();
                    await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε η πρακτική άσκηση.");
                    return;
                }

                await UpdateProgressWhenWithdrawProfessorInternshipApplication(30, 200);
                application.InternshipStatusAppliedAtTheProfessorSide = "Αποσύρθηκε από τον φοιτητή";
                application.InternshipStatusAppliedAtTheStudentSide = "Αποσύρθηκε από τον φοιτητή";

                await UpdateProgressWhenWithdrawProfessorInternshipApplication(50, 200);
                dbContext.PlatformActions.Add(new PlatformActions
                {
                    UserRole_PerformedAction = "STUDENT",
                    ForWhat_PerformedAction = "PROFESSOR_INTERNSHIP",
                    HashedPositionRNG_PerformedAction = HashingHelper.HashLong(application.RNGForProfessorInternshipApplied),
                    TypeOfAction_PerformedAction = "SELFWITHDRAW",
                    DateTime_PerformedAction = DateTime.Now
                });
                await dbContext.SaveChangesAsync();

                await UpdateProgressWhenWithdrawProfessorInternshipApplication(70, 200);
                var student = await GetStudentDetails(application.StudentDetails.StudentEmailAppliedForProfessorInternship);
                if (student == null)
                {
                    showLoadingModalWhenWithdrawProfessorInternshipApplication = false;
                    StateHasChanged();
                    await JS.InvokeVoidAsync("alert", "Δεν βρέθηκαν στοιχεία φοιτητή.");
                    return;
                }

                await UpdateProgressWhenWithdrawProfessorInternshipApplication(90, 200);
                await InternshipEmailService.SendProfessorInternshipWithdrawalNotificationToProfessor(
                    application.ProfessorDetails.ProfessorEmailWhereStudentAppliedForProfessorInternship,
                    $"{internship.Professor.ProfName} {internship.Professor.ProfSurname}",
                    student.Name,
                    student.Surname,
                    internship.ProfessorInternshipTitle,
                    application.RNGForProfessorInternshipApplied_HashedAsUniqueID);

                await InternshipEmailService.SendProfessorInternshipWithdrawalConfirmationToStudent(
                    application.StudentDetails.StudentEmailAppliedForProfessorInternship,
                    student.Name,
                    student.Surname,
                    internship.ProfessorInternshipTitle,
                    application.RNGForProfessorInternshipApplied_HashedAsUniqueID,
                    $"{internship.Professor.ProfName} {internship.Professor.ProfSurname}");

                await UpdateProgressWhenWithdrawProfessorInternshipApplication(100, 200);
                await Task.Delay(500);
                showLoadingModalWhenWithdrawProfessorInternshipApplication = false;
                StateHasChanged();
                await Task.Delay(100);
                await JS.InvokeVoidAsync("showBlazorNavigationLoader", "Παρακαλώ Περιμένετε...");
                await Task.Delay(300);
                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            catch (Exception ex)
            {
                showLoadingModalWhenWithdrawProfessorInternshipApplication = false;
                StateHasChanged();
                Console.WriteLine($"Error saving withdrawal: {ex.Message}");
                await JS.InvokeVoidAsync("alert", "Σφάλμα κατά την αποθήκευση της απόσυρσης.");
            }
        }

        // Additional Missing Properties
        private Dictionary<string, Student> studentDataCache = new Dictionary<string, Student>();
        private bool showInternships = false;
        private HashSet<string> selectedInternshipAreas = new HashSet<string>();
        private HashSet<string> selectedInternshipSubFields = new HashSet<string>();
        private DateTime? selectedDateToSearchInternship = null;
        private bool searchPerformedForInternships = false;
        private HashSet<long> professorInternshipIdsApplied = new HashSet<long>();
        private bool showLoadingModalWhenApplyForCompanyInternshipAsStudent = false;
        private bool showLoadingModalWhenApplyForProfessorInternshipAsStudent = false;

        // Helper Method
        private string HashLong(long value)
        {
            return HashingHelper.HashLong(value);
        }

        // Modal Close Methods
        private void CloseModalForInternships()
        {
            StateHasChanged();
        }

        private void CloseInternshipDetailsModal()
        {
            StateHasChanged();
        }

        private void CloseModalForCompanyNameHyperlinkDetailsInInternship()
        {
            selectedCompanyDetailsFromInternship = null;
            showCompanyDetailsModalFromInternship = false;
            StateHasChanged();
        }

        private void CloseModalForProfessorNameHyperlinkDetailsInInternship()
        {
            selectedProfessorDetailsFromInternship = null;
            showProfessorDetailsModalFromInternship = false;
            StateHasChanged();
        }

        private void CloseModalforHyperLinkTitle()
        {
            StateHasChanged();
        }

        // Additional Missing Properties
        private List<Area> Areas = new List<Area>();
        private int InternshipsPerPage = 10;

        // Search and Pagination Methods
        private void ClearSearchFieldsForInternshipsAsStudent()
        {
            selectedInternshipAreas.Clear();
            selectedInternshipSubFields.Clear();
            selectedDateToSearchInternship = null;
            StateHasChanged();
        }

        private void ConfirmApplyForInternship(object internship)
        {
            // TODO: Implement confirmation logic
            StateHasChanged();
        }

        private void ConfirmApplyForProfessorInternship(object internship)
        {
            // TODO: Implement confirmation logic
            StateHasChanged();
        }

        private object ConvertToCompanyInternship(object item)
        {
            return item;
        }

        private object ConvertToProfessorInternship(object item)
        {
            return item;
        }

        private IEnumerable<object> GetPaginatedInternships()
        {
            // TODO: Implement pagination logic
            return Enumerable.Empty<object>();
        }

        private List<int> GetVisibleInternshipPages()
        {
            // TODO: Implement pagination pages
            return new List<int>();
        }

        private void GoToFirstInternshipPage()
        {
            StateHasChanged();
        }

        private void GoToInternshipPage(int page)
        {
            StateHasChanged();
        }

        private void GoToLastInternshipPage()
        {
            StateHasChanged();
        }

        private void NextInternshipPage()
        {
            StateHasChanged();
        }

        private void PreviousInternshipPage()
        {
            StateHasChanged();
        }

        private async Task HandleInternshipTitleAutocompleteInputWhenSearchInternshipAsStudent(ChangeEventArgs e)
        {
            // TODO: Implement autocomplete
            await Task.CompletedTask;
            StateHasChanged();
        }

        private void OnInternshipAreaCheckboxChanged(Area area, object checkedValue)
        {
            bool isChecked = (bool)(checkedValue ?? false);
            if (isChecked)
                selectedInternshipAreas.Add(area.AreaName);
            else
                selectedInternshipAreas.Remove(area.AreaName);
            StateHasChanged();
        }

        private void OnInternshipSubFieldCheckboxChanged(string subField, object checkedValue)
        {
            bool isChecked = (bool)(checkedValue ?? false);
            if (isChecked)
                selectedInternshipSubFields.Add(subField);
            else
                selectedInternshipSubFields.Remove(subField);
            StateHasChanged();
        }

        private void OnInternshipFilterChange(ChangeEventArgs e)
        {
            StateHasChanged();
        }

        private void OnPageSizeChange_SearchForInternshipsAsStudent(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                InternshipsPerPage = newSize;
                StateHasChanged();
            }
        }

        private void OnPageSizeChange_SeeMyInternshipApplicationsAsStudent(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                pageSizeForInternshipsToSee = newSize;
                StateHasChanged();
            }
        }
    }
}

