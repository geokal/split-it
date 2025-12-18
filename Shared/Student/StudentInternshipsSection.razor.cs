using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Shared.Student
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
        private Company selectedCompanyDetails_StudentInternshipApplications;
        private bool showCompanyDetailsModal_StudentInternshipApplications = false;
        private Dictionary<string, Company> companyDataCache = new Dictionary<string, Company>();

        // Internship Details Modals
        private CompanyInternship selectedCompanyInternshipDetails_StudentInternshipApplications;
        private bool showCompanyInternshipDetailsModal_StudentInternshipApplications = false;

        // Company Details from Internship Display (Search Results)
        private Company selectedCompanyDetailsFromInternship;
        private bool showCompanyDetailsModalFromInternship = false;

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
                
                    professorInternship.ProfessorInternshipStatusAppliedAtTheStudentSide = "Αποσύρθηκε από τον φοιτητή";
                    professorInternship.ProfessorInternshipStatusAppliedAtTheProfessorSide = "Αποσύρθηκε από τον φοιτητή";

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
                
                    await InternshipEmailService.SendInternshipWithdrawalNotificationToProfessor_AsStudent(
                        professorInternship.ProfessorEmailWhereStudentAppliedForProfessorInternship,
                        professorName,
                        student.Name,
                        student.Surname,
                        internship.ProfessorInternshipTitle,
                        professorInternship.RNGForProfessorInternshipApplied_HashedAsUniqueID);

                    await InternshipEmailService.SendInternshipWithdrawalConfirmationToStudent_AsProfessor(
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

        // Helper Methods
        private async Task<Student> GetStudentDetails(string email)
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

        // Helper method for hashing (placeholder - will need actual HashingHelper class)
        private string HashLong(long value)
        {
            return value.GetHashCode().ToString();
        }
    }
}

