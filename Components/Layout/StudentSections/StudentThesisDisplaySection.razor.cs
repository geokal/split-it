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
    public partial class StudentThesisDisplaySection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private InternshipEmailService InternshipEmailService { get; set; } = default!;

        // Thesis Applications Visibility
        private bool showStudentThesisApplications = false;
        private bool isLoadingStudentThesisApplications = false;

        // Thesis Applications Data
        private List<CompanyThesisApplied> companyThesisApplications = new List<CompanyThesisApplied>();
        private List<ProfessorThesisApplied> professorThesisApplications = new List<ProfessorThesisApplied>();
        private bool showCompanyThesisApplications = true;
        private bool showProfessorThesisApplications = true;
        private Dictionary<long, CompanyThesis> thesisDataCache = new Dictionary<long, CompanyThesis>();
        private Dictionary<long, ProfessorThesis> professorThesisDataCache = new Dictionary<long, ProfessorThesis>();

        // Pagination for Applications
        private int currentPageForThesisToSee = 1;
        private int pageSizeForThesisToSee = 10;
        private int totalPagesForThesisToSee = 1;
        private int totalThesisCountForThesisToSee = 0;
        private int[] pageSizeOptions_SeeMyThesisApplicationsAsStudent = new[] { 10, 50, 100 };

        // Withdraw Application
        private bool showLoadingModalWhenWithdrawThesisApplication = false;
        private int loadingProgressWhenWithdrawThesisApplication = 0;

        // Company Details Modals (from Applications)
        private QuizManager.Models.Company selectedCompanyDetails_ThesisStudentApplicationsToShow;
        private bool isModalOpenToSeeCompanyDetails_ThesisStudentApplicationsToShow = false;
        private Dictionary<string, QuizManager.Models.Company> companyDataCache = new Dictionary<string, QuizManager.Models.Company>();

        // Professor Details Modals (from Applications)
        private QuizManager.Models.Professor selectedProfessorDetails_ThesisStudentApplicationsToShow;
        private bool isModalOpenToSeeProfessorDetails_ThesisStudentApplicationsToShow = false;
        private Dictionary<string, QuizManager.Models.Professor> professorDataCache = new Dictionary<string, QuizManager.Models.Professor>();

        // Thesis Details Modals
        private CompanyThesis selectedCompanyThesisDetails_ThesisStudentApplicationsToShow;
        private bool isModalOpenToSeeCompanyThesisDetails_ThesisStudentApplicationsToShow = false;
        private ProfessorThesis selectedProfessorThesisDetails_ThesisStudentApplicationsToShow;
        private bool isModalOpenToSeeProfessorThesisDetails_ThesisStudentApplicationsToShow = false;

        // Professor/Company Details from Thesis Display (Search Results)
        private QuizManager.Models.Professor selectedProfessorDetailsFromThesis;
        private QuizManager.Models.Company selectedCompanyDetailsFromThesis;
        private bool showProfessorDetailsModalFromThesis = false;
        private bool showCompanyDetailsModalFromThesis = false;

        // Computed Properties
        private List<object> allThesisApplications
        {
            get
            {
                var all = new List<object>();
                if (showCompanyThesisApplications && companyThesisApplications != null)
                    all.AddRange(companyThesisApplications);
                if (showProfessorThesisApplications && professorThesisApplications != null)
                    all.AddRange(professorThesisApplications);
                return all;
            }
        }

        private List<object> paginatedThesis
        {
            get
            {
                return allThesisApplications
                    .Skip((currentPageForThesisToSee - 1) * pageSizeForThesisToSee)
                    .Take(pageSizeForThesisToSee)
                    .ToList();
            }
        }

        // Main Methods
        private async Task ToggleAndLoadStudentThesisApplications()
        {
            showStudentThesisApplications = !showStudentThesisApplications;

            if (showStudentThesisApplications)
            {
                isLoadingStudentThesisApplications = true;
                StateHasChanged();
            
                try
                {
                    await LoadUserThesisApplications();
                }
                finally
                {
                    isLoadingStudentThesisApplications = false;
                    StateHasChanged();
                }
            }
            else
            {
                StateHasChanged();
            }
        }

        private async Task LoadUserThesisApplications()
        {
            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                // Initialize lists and cache
                companyThesisApplications = new List<CompanyThesisApplied>();
                professorThesisApplications = new List<ProfessorThesisApplied>();
                thesisDataCache = new Dictionary<long, CompanyThesis>();
                professorThesisDataCache = new Dictionary<long, ProfessorThesis>(); 

                if (user.Identity.IsAuthenticated)
                {
                    var userEmail = user.FindFirst("name")?.Value;

                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        showStudentThesisApplications = true;

                        // Get student details
                        var student = await dbContext.Students
                            .FirstOrDefaultAsync(s => s.Email == userEmail);

                        if (student != null)
                        {
                            // Fetch company thesis applications
                            companyThesisApplications = await dbContext.CompanyThesesApplied
                                .Include(a => a.StudentDetails)
                                .Include(a => a.CompanyDetails)
                                .Where(app => app.StudentEmailAppliedForThesis == userEmail && 
                                            app.StudentUniqueIDAppliedForThesis == student.Student_UniqueID)
                                .OrderByDescending(app => app.DateTimeStudentAppliedForThesis)
                                .ToListAsync();

                            // Load all related theses in one query
                            var thesisRNGs = companyThesisApplications
                                .Select(a => a.RNGForCompanyThesisApplied)
                                .ToList();

                            var theses = await dbContext.CompanyTheses
                                .Include(t => t.Company) 
                                .Where(t => thesisRNGs.Contains(t.RNGForThesisUploadedAsCompany))
                                .ToListAsync();

                            // Populate cache
                            foreach (var thesis in theses)
                            {
                                thesisDataCache[thesis.RNGForThesisUploadedAsCompany] = thesis;
                            }

                            // Fetch professor thesis applications
                            professorThesisApplications = await dbContext.ProfessorThesesApplied
                                .Include(a => a.StudentDetails)
                                .Include(a => a.ProfessorDetails)
                                .Where(app => app.StudentEmailAppliedForProfessorThesis == userEmail && 
                                            app.StudentUniqueIDAppliedForProfessorThesis == student.Student_UniqueID)
                                .OrderByDescending(app => app.DateTimeStudentAppliedForProfessorThesis)
                                .ToListAsync();

                            // Load all related professor theses in one query
                            var professorThesisRNGs = professorThesisApplications
                                .Select(a => a.RNGForProfessorThesisApplied)
                                .ToList();

                            var professorTheses = await dbContext.ProfessorTheses
                                .Where(t => professorThesisRNGs.Contains(t.RNGForThesisUploaded))
                                .ToListAsync();

                            // Populate professor thesis cache
                            foreach (var thesis in professorTheses)
                            {
                                professorThesisDataCache[thesis.RNGForThesisUploaded] = thesis;
                            }
                        }
                    }
                }

                UpdateTotalThesisCount();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading thesis applications: {ex.Message}");
                StateHasChanged();
            }
        }

        private async Task FilterThesisApplications(ChangeEventArgs e)
        {
            var filterValue = e.Value?.ToString() ?? "all";

            if (filterValue == "company")
            {
                showCompanyThesisApplications = true;
                showProfessorThesisApplications = false;
            }
            else if (filterValue == "professor")
            {
                showCompanyThesisApplications = false;
                showProfessorThesisApplications = true;
            }
            else
            {
                showCompanyThesisApplications = true;
                showProfessorThesisApplications = true;
            }

            await Task.Delay(100); // Small delay for UI update
            UpdateTotalThesisCount();
            currentPageForThesisToSee = 1; // Reset to first page
            StateHasChanged();
        }

        private void SetTotalThesisCount(int count)
        {
            totalThesisCountForThesisToSee = count;
            UpdatePagination();
        }

        private void UpdateTotalThesisCount()
        {
            totalThesisCountForThesisToSee = allThesisApplications.Count;
            UpdatePagination();
        }

        private void UpdatePagination()
        {
            totalPagesForThesisToSee = (int)Math.Ceiling((double)totalThesisCountForThesisToSee / pageSizeForThesisToSee);
            if (totalPagesForThesisToSee < 1) totalPagesForThesisToSee = 1;
            StateHasChanged();
        }

        // Pagination Methods
        private void GoToFirstPage()
        {
            currentPageForThesisToSee = 1;
            StateHasChanged();
        }

        private void GoToLastPage()
        {
            currentPageForThesisToSee = totalPagesForThesisToSee;
            StateHasChanged();
        }

        private void GoToPage(int pageNumber)
        {
            if (pageNumber >= 1 && pageNumber <= totalPagesForThesisToSee)
            {
                currentPageForThesisToSee = pageNumber;
                StateHasChanged();
            }
        }

        private void PreviousPageForThesisToSee()
        {
            if (currentPageForThesisToSee > 1)
            {
                currentPageForThesisToSee--;
                StateHasChanged();
            }
        }

        private void NextPageForThesisToSee()
        {
            if (currentPageForThesisToSee < totalPagesForThesisToSee)
            {
                currentPageForThesisToSee++;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePages()
        {
            return GetVisiblePages(currentPageForThesisToSee, totalPagesForThesisToSee);
        }

        private List<int> GetVisiblePages(int currentPage, int totalPages)
        {
            var pages = new List<int>();
            if (totalPages <= 1)
                return pages;

            pages.Add(1);

            if (currentPage > 3)
            {
                pages.Add(-1);
            }

            int start = Math.Max(2, currentPage - 1);
            int end = Math.Min(totalPages - 1, currentPage + 1);

            for (int i = start; i <= end; i++)
            {
                pages.Add(i);
            }

            if (currentPage < totalPages - 2)
            {
                pages.Add(-1);
            }

            if (totalPages > 1)
            {
                pages.Add(totalPages);
            }

            return pages;
        }

        private void OnPageSizeChangeForApplications_SeeMyThesisApplicationsAsStudent(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                pageSizeForThesisToSee = newSize;
                currentPageForThesisToSee = 1;
                UpdatePagination();
                StateHasChanged();
            }
        }

        // Withdraw Application
        private async Task WithdrawThesisApplication(object thesis)
        {
            try
            {
                PlatformActions platformAction = null;

                if (thesis is CompanyThesisApplied companyThesis)
                {
                    var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", 
                        $"Πρόκεται να αποσύρετε την Αίτησή σας για την Διπλωματική Εργασία. Είστε σίγουρος/η;");
                    if (!confirmed) return;

                    // Show loading modal after user confirms
                    showLoadingModalWhenWithdrawThesisApplication = true;
                    loadingProgressWhenWithdrawThesisApplication = 0;
                    StateHasChanged();

                    // Step 1: Get thesis details
                    await UpdateProgressWhenWithdrawThesisApplication(10, 200);
                
                    var thesisDetails = await dbContext.CompanyTheses
                        .Include(t => t.Company)
                        .FirstOrDefaultAsync(t => t.RNGForThesisUploadedAsCompany == companyThesis.RNGForCompanyThesisApplied);

                    if (thesisDetails == null)
                    {
                        showLoadingModalWhenWithdrawThesisApplication = false;
                        StateHasChanged();
                        await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε η Διπλωματικής Εργασία.");
                        return;
                    }

                    // Step 2: Update thesis status
                    await UpdateProgressWhenWithdrawThesisApplication(30, 200);
                
                    companyThesis.CompanyThesisStatusAppliedAtStudentSide = "Αποσύρθηκε από τον φοιτητή";
                    companyThesis.CompanyThesisStatusAppliedAtCompanySide = "Αποσύρθηκε από τον φοιτητή";

                    // Step 3: Add platform action
                    await UpdateProgressWhenWithdrawThesisApplication(50, 200);
                
                    platformAction = new PlatformActions
                    {
                        UserRole_PerformedAction = "STUDENT",
                        ForWhat_PerformedAction = "COMPANY_THESIS",
                        HashedPositionRNG_PerformedAction = HashingHelper.HashLong(companyThesis.RNGForCompanyThesisApplied),
                        TypeOfAction_PerformedAction = "SELFWITHDRAW",
                        DateTime_PerformedAction = DateTime.Now
                    };

                    dbContext.PlatformActions.Add(platformAction);
                    await dbContext.SaveChangesAsync();
                
                    await UpdateProgressWhenWithdrawThesisApplication(70, 200);

                    // Step 4: Get student details
                    await UpdateProgressWhenWithdrawThesisApplication(80, 200);
                
                    var student = await GetStudentDetails(companyThesis.StudentEmailAppliedForThesis);
                    if (student == null)
                    {
                        showLoadingModalWhenWithdrawThesisApplication = false;
                        StateHasChanged();
                        await JS.InvokeVoidAsync("alert", "Δεν βρέθηκαν στοιχεία φοιτητή.");
                        return;
                    }

                    var companyName = thesisDetails.Company?.CompanyName ?? "Άγνωστη Εταιρεία";

                    // Step 5: Send emails
                    await UpdateProgressWhenWithdrawThesisApplication(90, 200);
                
                    await InternshipEmailService.SendStudentThesisWithdrawalNotificationToCompanyOrProfessor(
                        companyThesis.CompanyEmailWhereStudentAppliedForThesis,
                        companyName,
                        student.Name,
                        student.Surname,
                        thesisDetails.CompanyThesisTitle,
                        companyThesis.RNGForCompanyThesisAppliedAsStudent_HashedAsUniqueID);

                    await InternshipEmailService.SendStudentThesisWithdrawalConfirmationToStudent(
                        companyThesis.StudentEmailAppliedForThesis,
                        student.Name,
                        student.Surname,
                        thesisDetails.CompanyThesisTitle,
                        companyThesis.RNGForCompanyThesisAppliedAsStudent_HashedAsUniqueID,
                        companyName);

                    await UpdateProgressWhenWithdrawThesisApplication(100, 200);
                
                    // Small delay to show completion
                    await Task.Delay(500);
                
                    // Hide loading modal before navigation
                    showLoadingModalWhenWithdrawThesisApplication = false;
                    StateHasChanged();

                    // AFTER user clicks OK, show the navigation loader
                    await Task.Delay(100);
            
                    // Show the navigation loader
                    await JS.InvokeVoidAsync("showBlazorNavigationLoader", "Παρακαλώ Περιμένετε...");
            
                    // Give time for loader to render
                    await Task.Delay(300);
                
                    NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
                }
                else if (thesis is ProfessorThesisApplied professorThesis)
                {
                    var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", 
                        $"Πρόκεται να αποσύρετε την Αίτησή σας για την Διπλωματική Εργασία. Είστε σίγουρος/η;");
                    if (!confirmed) return;

                    // Show loading modal after user confirms
                    showLoadingModalWhenWithdrawThesisApplication = true;
                    loadingProgressWhenWithdrawThesisApplication = 0;
                    StateHasChanged();

                    // Step 1: Get thesis details
                    await UpdateProgressWhenWithdrawThesisApplication(10, 200);
                
                    var thesisDetails = await dbContext.ProfessorTheses
                        .Include(t => t.Professor)
                        .FirstOrDefaultAsync(t => t.RNGForThesisUploaded == professorThesis.RNGForProfessorThesisApplied);

                    if (thesisDetails == null)
                    {
                        showLoadingModalWhenWithdrawThesisApplication = false;
                        StateHasChanged();
                        await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε η Διπλωματικής Εργασία.");
                        return;
                    }

                    // Step 2: Update thesis status
                    await UpdateProgressWhenWithdrawThesisApplication(30, 200);
                
                    professorThesis.ProfessorThesisStatusAppliedAtStudentSide = "Αποσύρθηκε από τον φοιτητή";
                    professorThesis.ProfessorThesisStatusAppliedAtProfessorSide = "Αποσύρθηκε από τον φοιτητή";

                    // Step 3: Add platform action
                    await UpdateProgressWhenWithdrawThesisApplication(50, 200);
                
                    platformAction = new PlatformActions
                    {
                        UserRole_PerformedAction = "STUDENT",
                        ForWhat_PerformedAction = "PROFESSOR_THESIS",
                        HashedPositionRNG_PerformedAction = HashingHelper.HashLong(professorThesis.RNGForProfessorThesisApplied),
                        TypeOfAction_PerformedAction = "SELFWITHDRAW",
                        DateTime_PerformedAction = DateTime.Now
                    };

                    dbContext.PlatformActions.Add(platformAction);
                    await dbContext.SaveChangesAsync();
                
                    await UpdateProgressWhenWithdrawThesisApplication(70, 200);

                    // Step 4: Get student details
                    await UpdateProgressWhenWithdrawThesisApplication(80, 200);
                
                    var student = await GetStudentDetails(professorThesis.StudentEmailAppliedForProfessorThesis);
                    if (student == null)
                    {
                        showLoadingModalWhenWithdrawThesisApplication = false;
                        StateHasChanged();
                        await JS.InvokeVoidAsync("alert", "Δεν βρέθηκαν στοιχεία φοιτητή.");
                        return;
                    }

                    var professorName = thesisDetails.Professor != null 
                        ? $"{thesisDetails.Professor.ProfName} {thesisDetails.Professor.ProfSurname}" 
                        : "Άγνωστος Καθηγητής";

                    // Step 5: Send emails
                    await UpdateProgressWhenWithdrawThesisApplication(90, 200);
                
                    await InternshipEmailService.SendStudentThesisWithdrawalNotificationToCompanyOrProfessor(
                        professorThesis.ProfessorEmailWhereStudentAppliedForProfessorThesis,
                        professorName,
                        student.Name,
                        student.Surname,
                        thesisDetails.ThesisTitle,
                        professorThesis.RNGForProfessorThesisApplied_HashedAsUniqueID);

                    await InternshipEmailService.SendStudentThesisWithdrawalConfirmationToStudent(
                        professorThesis.StudentEmailAppliedForProfessorThesis,
                        student.Name,
                        student.Surname,
                        thesisDetails.ThesisTitle,
                        professorThesis.RNGForProfessorThesisApplied_HashedAsUniqueID,
                        professorName);

                    await UpdateProgressWhenWithdrawThesisApplication(100, 200);
                
                    // Small delay to show completion
                    await Task.Delay(500);
                
                    // Hide loading modal before navigation
                    showLoadingModalWhenWithdrawThesisApplication = false;
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
                showLoadingModalWhenWithdrawThesisApplication = false;
                StateHasChanged();
                Console.WriteLine($"Error withdrawing thesis application: {ex.Message}");
                await JS.InvokeVoidAsync("alert", "Σφάλμα κατά την απόσυρση της αίτησης.");
            }
        }

        private async Task UpdateProgressWhenWithdrawThesisApplication(int progress, int delayMs = 0)
        {
            loadingProgressWhenWithdrawThesisApplication = progress;
            StateHasChanged();

            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }
        }

        // Company Details Modals (from Applications)
        private async Task ShowCompanyDetailsInThesisCompanyName_StudentThesisApplications(string companyEmail)
        {
            try
            {
                // First check if we already have the company details in cache
                if (companyDataCache.TryGetValue(companyEmail, out var cachedCompany))
                {
                    selectedCompanyDetails_ThesisStudentApplicationsToShow = cachedCompany;
                }
                else
                {
                    // Fetch the company details from the database using email
                    selectedCompanyDetails_ThesisStudentApplicationsToShow = await dbContext.Companies
                        .FirstOrDefaultAsync(c => c.CompanyEmail == companyEmail);

                    // Add to cache if found
                    if (selectedCompanyDetails_ThesisStudentApplicationsToShow != null)
                    { 
                        companyDataCache[companyEmail] = selectedCompanyDetails_ThesisStudentApplicationsToShow;
                    }
                }

                if (selectedCompanyDetails_ThesisStudentApplicationsToShow != null)
                {
                    isModalOpenToSeeCompanyDetails_ThesisStudentApplicationsToShow = true;
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

        private void CloseCompanyDetailsModal_StudentThesisApplications()
        {
            isModalOpenToSeeCompanyDetails_ThesisStudentApplicationsToShow = false;
            StateHasChanged();
        }

        // Professor Details Modals (from Applications)
        private async Task ShowProfessorDetailsInThesisProfessorName_StudentThesisApplications(string professorEmail)
        {
            try
            {
                // First check if we already have the professor details in cache
                if (professorDataCache.TryGetValue(professorEmail, out var cachedProfessor))
                {
                    selectedProfessorDetails_ThesisStudentApplicationsToShow = cachedProfessor;
                }
                else
                {
                    // Fetch the professor details from the database using email
                    selectedProfessorDetails_ThesisStudentApplicationsToShow = await dbContext.Professors
                        .FirstOrDefaultAsync(p => p.ProfEmail == professorEmail);

                    // Add to cache if found
                    if (selectedProfessorDetails_ThesisStudentApplicationsToShow != null)
                    { 
                        professorDataCache[professorEmail] = selectedProfessorDetails_ThesisStudentApplicationsToShow;
                    }
                }

                if (selectedProfessorDetails_ThesisStudentApplicationsToShow != null)
                {
                    isModalOpenToSeeProfessorDetails_ThesisStudentApplicationsToShow = true;
                    StateHasChanged();
                }
                else
                {
                    await JS.InvokeVoidAsync("alert", "Δεν βρέθηκαν στοιχεία καθηγητή");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading professor details: {ex.Message}");
                await JS.InvokeVoidAsync("alert", "Σφάλμα φόρτωσης στοιχείων καθηγητή");
            }
        }

        private void CloseProfessorDetailsModal_StudentThesisApplications()
        {
            isModalOpenToSeeProfessorDetails_ThesisStudentApplicationsToShow = false;
            StateHasChanged();
        }

        // Thesis Details Modals
        private async Task ShowCompanyThesisDetailsModal_StudentThesisApplications(long thesisRNG)
        {
            // Fetch the company thesis details asynchronously
            selectedCompanyThesisDetails_ThesisStudentApplicationsToShow = await dbContext.CompanyTheses
                .FirstOrDefaultAsync(t => t.RNGForThesisUploadedAsCompany == thesisRNG);

            if (selectedCompanyThesisDetails_ThesisStudentApplicationsToShow != null)
            {
                // Open the modal if the thesis details are found
                isModalOpenToSeeCompanyThesisDetails_ThesisStudentApplicationsToShow = true;
                StateHasChanged();
            }
            else
            {
                // Show an alert if no thesis details are found
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Δεν μπορούν να εμφανιστούν οι λεπτομέρειες της Διπλωματικής. <span style='color:darkred;'>Η Διπλωματική Δεν Είναι Πλέον Διαθέσιμη από τον Φορέα</span>");
            }
        }

        private void CloseCompanyThesisDetailsModal_StudentThesisApplications()
        {
            isModalOpenToSeeCompanyThesisDetails_ThesisStudentApplicationsToShow = false;
            StateHasChanged();
        }

        private async Task ShowProfessorThesisDetailsModal_StudentThesisApplications(long thesisRNG)
        {
            // Fetch the professor thesis details asynchronously
            selectedProfessorThesisDetails_ThesisStudentApplicationsToShow = await dbContext.ProfessorTheses
                .FirstOrDefaultAsync(t => t.RNGForThesisUploaded == thesisRNG);

            if (selectedProfessorThesisDetails_ThesisStudentApplicationsToShow != null)
            {
                // Open the modal if the thesis details are found
                isModalOpenToSeeProfessorThesisDetails_ThesisStudentApplicationsToShow = true;
                StateHasChanged();
            }
            else
            {
                // Show an alert if no thesis details are found
                await JS.InvokeVoidAsync("alert", "Professor thesis details not found.");
            }
        }

        private void CloseProfessorThesisDetailsModal_StudentThesisApplications()
        {
            isModalOpenToSeeProfessorThesisDetails_ThesisStudentApplicationsToShow = false;
            StateHasChanged(); 
        }

        // Professor/Company Details from Thesis Display (Search Results)
        private async Task ShowProfessorHyperlinkNameDetailsModalInStudentThesis(string professorEmail)
        {
            // Fetch professor details based on the professorId
            selectedProfessorDetailsFromThesis = await dbContext.Professors
                .FirstOrDefaultAsync(p => p.ProfEmail == professorEmail);

            // Show the modal after fetching the details
            if (selectedProfessorDetailsFromThesis != null)
            {
                showProfessorDetailsModalFromThesis = true;
                StateHasChanged();
            }
        }

        private async Task ShowCompanyHyperlinkNameDetailsModalInStudentThesis(string companyEmail)
        {
            selectedCompanyDetailsFromThesis = await dbContext.Companies
                .FirstOrDefaultAsync(c => c.CompanyEmail == companyEmail);

            if (selectedCompanyDetailsFromThesis != null)
            {
                showCompanyDetailsModalFromThesis = true;
                StateHasChanged();
            }
        }

        // Helper Methods
        private async Task<QuizManager.Models.Student> GetStudentDetails(string email)
        {
            return await dbContext.Students
                .FirstOrDefaultAsync(s => s.Email == email);
        }

        // Helper method to get thesis details from cache
        private CompanyThesis GetCompanyThesisFromCache(long rng)
        {
            return thesisDataCache.TryGetValue(rng, out var thesis) ? thesis : null;
        }

        private ProfessorThesis GetProfessorThesisFromCache(long rng)
        {
            return professorThesisDataCache.TryGetValue(rng, out var thesis) ? thesis : null;
        }

        // Additional Missing Properties
        private QuizManager.Models.Company selectedCompanyDetailsForHyperlinkNameInThesisAsStudent;
        private QuizManager.Models.Professor selectedProfessorDetailsForHyperlinkNameInThesisAsStudent;
        private CompanyThesis selectedCompanyThesisDetails;
        private ProfessorThesis selectedProfessorThesisDetails;
        private bool showThesisApplications = false;
        private bool showLoadingModalWhenApplyForThesisAsStudent = false;
        private HashSet<long> professorThesisIdsApplied = new HashSet<long>();
        private HashSet<long> companyThesisIdsApplied = new HashSet<long>();
        
        // Thesis Search Properties
        private string thesisSearchForThesesAsStudent = "";
        private DateTime? thesisStartDateForThesesAsStudent = null;
        private HashSet<string> selectedThesisAreas = new HashSet<string>();
        private HashSet<string> selectedThesisSubFields = new HashSet<string>();
        private List<string> thesisTitleSuggestions = new List<string>();
        private List<string> professorNameSurnameSuggestions = new List<string>();
        private List<string> companyNameSuggestionsWhenSearchForProfessorThesisAutocompleteNameAsStudent = new List<string>();
        private string searchNameSurnameAsStudentToFindProfessor = "";
        private bool isThesisAreasVisible = false;
        private bool isSearchInternshipsAsStudentFiltersVisible = false;
        private bool isLoadingSearchThesisApplicationsAsStudent = false;
        private HashSet<int> expandedThesisAreas = new HashSet<int>();
        private List<AllTheses> publishedTheses = new List<AllTheses>();
        
        // Pagination for Thesis Search
        private int currentThesisPage = 1;
        private int thesisPageSize = 10;
        private int totalThesisPages_SearchThesisAsStudent = 1;

        // Profile Image
        private string ShowProfileImage(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return "/images/default-profile.png";
            return $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
        }

        // Modal Close Methods
        private void CloseModalForCompanyThesisDetails()
        {
            selectedCompanyThesisDetails = null;
            isModalOpenToSeeCompanyThesisDetails_ThesisStudentApplicationsToShow = false;
            StateHasChanged();
        }

        private void CloseModalForProfessorThesisDetails()
        {
            selectedProfessorThesisDetails = null;
            isModalOpenToSeeProfessorThesisDetails_ThesisStudentApplicationsToShow = false;
            StateHasChanged();
        }

        private void CloseModalForCompanyNameHyperlinkDetails()
        {
            selectedCompanyDetailsForHyperlinkNameInThesisAsStudent = null;
            showCompanyDetailsModalFromThesis = false;
            StateHasChanged();
        }

        private void CloseModalForProfessorNameHyperlinkDetails()
        {
            selectedProfessorDetailsForHyperlinkNameInThesisAsStudent = null;
            showProfessorDetailsModalFromThesis = false;
            StateHasChanged();
        }

        // Pagination Methods
        private void ChangeThesisPage(int newPage)
        {
            if (newPage > 0 && newPage <= totalThesisPages_SearchThesisAsStudent)
            {
                currentThesisPage = newPage;
                StateHasChanged();
            }
        }

        // Filter Toggle
        private void ToggleSearchInternshipsAsStudentFiltersVisibility()
        {
            isSearchInternshipsAsStudentFiltersVisible = !isSearchInternshipsAsStudentFiltersVisible;
            StateHasChanged();
        }

        // Additional Missing Properties
        private string globalThesisSearch = "";
        private bool isCompanyDetailsModalOpenForHyperlinkNameAsStudent = false;
        private int loadingProgressWhenApplyForThesisAsStudent = 0;
        private List<Area> Areas = new List<Area>();
        private int[] pageSizeOptions_SearchForThesisAsStudent = new[] { 10, 50, 100 };

        // Search Methods
        private void SearchThesisApplicationsAsStudent()
        {
            // TODO: Implement search logic
            StateHasChanged();
        }

        private void ClearSearchFieldsForThesisAsStudent()
        {
            thesisSearchForThesesAsStudent = "";
            searchNameSurnameAsStudentToFindProfessor = "";
            selectedThesisAreas.Clear();
            selectedThesisSubFields.Clear();
            thesisStartDateForThesesAsStudent = null;
            thesisTitleSuggestions.Clear();
            professorNameSurnameSuggestions.Clear();
            companyNameSuggestionsWhenSearchForProfessorThesisAutocompleteNameAsStudent.Clear();
            StateHasChanged();
        }

        // Apply for Thesis
        private async Task ApplyForThesisAsStudent(object thesis)
        {
            showLoadingModalWhenApplyForThesisAsStudent = true;
            loadingProgressWhenApplyForThesisAsStudent = 0;
            StateHasChanged();

            try
            {
                // TODO: Call StudentDashboardService.ApplyForThesisAsync
                await Task.Delay(100);
                loadingProgressWhenApplyForThesisAsStudent = 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying for thesis: {ex.Message}");
            }
            finally
            {
                showLoadingModalWhenApplyForThesisAsStudent = false;
                StateHasChanged();
            }
        }

        // Show Thesis Details
        private async Task ShowCompanyThesisDetailsAsStudent(long thesisRng)
        {
            selectedCompanyThesisDetails = await dbContext.CompanyTheses
                .FirstOrDefaultAsync(t => t.RNGForThesisUploadedAsCompany == thesisRng);
            isModalOpenToSeeCompanyThesisDetails_ThesisStudentApplicationsToShow = selectedCompanyThesisDetails != null;
            StateHasChanged();
        }

        private async Task ShowProfessorThesisDetailsAsStudent(long thesisRng)
        {
            selectedProfessorThesisDetails = await dbContext.ProfessorTheses
                .FirstOrDefaultAsync(t => t.RNGForThesisUploaded == thesisRng);
            isModalOpenToSeeProfessorThesisDetails_ThesisStudentApplicationsToShow = selectedProfessorThesisDetails != null;
            StateHasChanged();
        }

        // Page Size Change
        private void OnPageSizeChange_SearchForThesisAsStudent(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                thesisPageSize = newSize;
                currentThesisPage = 1;
                StateHasChanged();
            }
        }

        // Filter Change
        private void OnFilterChange(ChangeEventArgs e)
        {
            // Handle filter change
            StateHasChanged();
        }

        // Thesis Area/SubField Methods
        private void ToggleThesisSubFields(int areaId)
        {
            if (expandedThesisAreas.Contains(areaId))
                expandedThesisAreas.Remove(areaId);
            else
                expandedThesisAreas.Add(areaId);
            StateHasChanged();
        }

        private void OnThesisAreaCheckboxChanged(ChangeEventArgs e, string areaName)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
                selectedThesisAreas.Add(areaName);
            else
                selectedThesisAreas.Remove(areaName);
            StateHasChanged();
        }

        private void OnThesisSubFieldCheckboxChanged(ChangeEventArgs e, string subField)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
                selectedThesisSubFields.Add(subField);
            else
                selectedThesisSubFields.Remove(subField);
            StateHasChanged();
        }

        // Additional Missing Properties
        private string companyNameSearchForThesesAsStudent = "";

        // Additional Methods
        private void ToggleThesisAreasVisibility()
        {
            isThesisAreasVisible = !isThesisAreasVisible;
            StateHasChanged();
        }

        private async Task HandleThesisTitleInputForBothCompaniesAndProfessorsWhenSearchForThesisAsStudent(ChangeEventArgs e)
        {
            thesisSearchForThesesAsStudent = e.Value?.ToString().Trim() ?? "";
            thesisTitleSuggestions.Clear();

            if (thesisSearchForThesesAsStudent.Length >= 2)
            {
                try
                {
                    // TODO: Load suggestions from database
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading suggestions: {ex.Message}");
                }
            }
            StateHasChanged();
        }

        private async Task HandleProfessorInputWhenSearchForProfessorThesisAutocompleteNameAsStudent(ChangeEventArgs e)
        {
            searchNameSurnameAsStudentToFindProfessor = e.Value?.ToString().Trim() ?? "";
            professorNameSurnameSuggestions.Clear();

            if (searchNameSurnameAsStudentToFindProfessor.Length >= 2)
            {
                try
                {
                    // TODO: Load professor name suggestions
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading suggestions: {ex.Message}");
                }
            }
            StateHasChanged();
        }

        private async Task HandleCompanyNameInputWhenSearchForProfessorThesisAutocompleteNameAsStudent(ChangeEventArgs e)
        {
            companyNameSearchForThesesAsStudent = e.Value?.ToString().Trim() ?? "";
            companyNameSuggestionsWhenSearchForProfessorThesisAutocompleteNameAsStudent.Clear();

            if (companyNameSearchForThesesAsStudent.Length >= 2)
            {
                try
                {
                    // TODO: Load company name suggestions
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading suggestions: {ex.Message}");
                }
            }
            StateHasChanged();
        }

        private void SelectThesisTitleSuggestionForBothCompaniesAndProfessorsWhenSearchForThesisAsStudent(string suggestion)
        {
            thesisSearchForThesesAsStudent = suggestion;
            thesisTitleSuggestions.Clear();
            StateHasChanged();
        }

        private void SelectProfessorNameSurnameSuggestionWhenSearchForProfessorThesisAutocompleteNameAsStudent(string suggestion)
        {
            searchNameSurnameAsStudentToFindProfessor = suggestion;
            professorNameSurnameSuggestions.Clear();
            StateHasChanged();
        }

        private void SelectCompanyNameSuggestionWhenSearchForProfessorThesisAutocompleteNameAsStudent(string suggestion)
        {
            companyNameSearchForThesesAsStudent = suggestion;
            companyNameSuggestionsWhenSearchForProfessorThesisAutocompleteNameAsStudent.Clear();
            StateHasChanged();
        }
    }
}
