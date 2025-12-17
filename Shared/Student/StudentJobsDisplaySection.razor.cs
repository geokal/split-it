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

namespace SplitIt.Shared.Student
{
    public partial class StudentJobsDisplaySection : ComponentBase
    {
        [Inject] private AppDbContext dbContext { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private InternshipEmailService InternshipEmailService { get; set; } = default!;

        // Jobs Display Visibility
        private bool showStudentJobApplications = false;
        private bool isLoadingStudentJobApplications = false;

        // Job Applications Data
        private List<CompanyJobApplied> companyJobApplications = new List<CompanyJobApplied>();
        private Dictionary<long, CompanyJob> jobDataCache = new Dictionary<long, CompanyJob>();
        private int totalJobCountForJobsToSee = 0;

        // Pagination for Applications
        private int currentPageForJobsToSee = 1;
        private int pageSizeForJobsToSee = 10;
        private int totalPagesForJobsToSee = 1;
        private int[] pageSizeOptions_SeeMyJobApplicationsAsStudent = new[] { 10, 50, 100 };

        // Withdraw Application
        private bool showLoadingModalWhenWithdrawJobApplication = false;
        private int loadingProgressWhenWithdrawJobApplication = 0;

        // Company Details Modals
        private Company selectedCompanyDetails_StudentJobApplications;
        private bool showCompanyDetailsModal_StudentJobApplications = false;
        private Dictionary<string, Company> companyDataCache = new Dictionary<string, Company>();

        // Job Details Modals
        private CompanyJob selectedCompanyJobDetails_StudentJobApplications;
        private bool showCompanyJobDetailsModal_StudentJobApplications = false;

        // Computed Properties
        private List<CompanyJobApplied> jobApplications => companyJobApplications ?? new List<CompanyJobApplied>();
        private int jobPageSize => pageSizeForJobsToSee;
        private int currentJobPage => currentPageForJobsToSee;

        private List<CompanyJobApplied> paginatedJobs
        {
            get
            {
                return jobApplications
                    .Skip((currentJobPage - 1) * jobPageSize)
                    .Take(jobPageSize)
                    .ToList();
            }
        }

        // Main Methods
        private async Task ToggleAndLoadStudentJobApplications()
        {
            showStudentJobApplications = !showStudentJobApplications;

            if (showStudentJobApplications)
            {
                isLoadingStudentJobApplications = true;
                StateHasChanged();
            
                try
                {
                    await LoadUserJobApplications();
                }
                finally
                {
                    isLoadingStudentJobApplications = false;
                    StateHasChanged();
                }
            }
            else
            {
                StateHasChanged();
            }
        }

        private async Task LoadUserJobApplications()
        {
            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity.IsAuthenticated)
                {
                    var userEmail = user.FindFirst("name")?.Value;
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        showStudentJobApplications = true;

                        // Get student details
                        var student = await dbContext.Students
                            .FirstOrDefaultAsync(s => s.Email == userEmail);

                        if (student != null)
                        {
                            // Retrieve job applications using email and unique ID
                            companyJobApplications = await dbContext.CompanyJobsApplied
                                .Where(j => j.StudentEmailAppliedForCompanyJob == userEmail && 
                                        j.StudentUniqueIDAppliedForCompanyJob == student.Student_UniqueID)
                                .OrderByDescending(j => j.DateTimeStudentAppliedForCompanyJob)
                                .ToListAsync();

                            // Load all related jobs in one query
                            var jobRNGs = companyJobApplications
                                .Select(a => a.RNGForCompanyJobApplied)
                                .ToList();

                            var jobs = await dbContext.CompanyJobs
                                .Include(j => j.Company)
                                .Where(j => jobRNGs.Contains(j.RNGForPositionUploaded))
                                .ToListAsync();

                            // Populate cache
                            foreach (var job in jobs)
                            {
                                jobDataCache[job.RNGForPositionUploaded] = job;
                            }
                        }
                    }
                }

                UpdateTotalJobCount();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading job applications: {ex.Message}");
                StateHasChanged();
            }
        }

        private void FilterJobApplications(ChangeEventArgs e)
        {
            // Job applications don't have a filter like thesis (company/professor)
            // This method is kept for compatibility but doesn't need to do anything
            StateHasChanged();
        }

        private void SetTotalJobCount(int count)
        {
            totalJobCountForJobsToSee = count;
            UpdatePagination();
        }

        private void UpdateTotalJobCount()
        {
            totalJobCountForJobsToSee = jobApplications.Count;
            UpdatePagination();
        }

        private void UpdatePagination()
        {
            totalPagesForJobsToSee = (int)Math.Ceiling((double)totalJobCountForJobsToSee / pageSizeForJobsToSee);
            if (totalPagesForJobsToSee < 1) totalPagesForJobsToSee = 1;
            StateHasChanged();
        }

        // Pagination Methods
        private void GoToFirstPageForJobs()
        {
            currentPageForJobsToSee = 1;
            StateHasChanged();
        }

        private void GoToLastPageForJobs()
        {
            currentPageForJobsToSee = totalPagesForJobsToSee;
            StateHasChanged();
        }

        private void GoToPageForJobs(int page)
        {
            if (page > 0 && page <= totalPagesForJobsToSee)
            {
                currentPageForJobsToSee = page;
                StateHasChanged();
            }
        }

        private void PreviousPageForJobsToSee()
        {
            if (currentPageForJobsToSee > 1)
            {
                currentPageForJobsToSee--;
                StateHasChanged();
            }
        }

        private void NextPageForJobsToSee()
        {
            if (currentPageForJobsToSee < totalPagesForJobsToSee)
            {
                currentPageForJobsToSee++;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePagesForJobs()
        {
            var pages = new List<int>();
            int current = currentPageForJobsToSee;
            int total = totalPagesForJobsToSee;
        
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

        private void OnJobPageSizeChange_SeeMyJobApplicationsAsStudent(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                pageSizeForJobsToSee = newSize;
                currentPageForJobsToSee = 1;
                UpdatePagination();
                StateHasChanged();
            }
        }

        // Withdraw Application
        private async Task WithdrawJobApplication(object applicationObj)
        {
            if (applicationObj is not CompanyJobApplied application)
                return;

            try
            {
                // First ask for confirmation
                var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", 
                    $"Πρόκεται να αποσύρετε την Αίτησή σας για την Θέση Εργασίας. Είστε σίγουρος/η;");
                if (!confirmed) return;

                // Show loading modal after user confirms
                showLoadingModalWhenWithdrawJobApplication = true;
                loadingProgressWhenWithdrawJobApplication = 0;
                StateHasChanged();

                // Step 1: Get the related job details
                await UpdateProgressWhenWithdrawJobApplication(10, 200);
            
                var job = await dbContext.CompanyJobs
                    .Include(j => j.Company)
                    .FirstOrDefaultAsync(j => j.RNGForPositionUploaded == application.RNGForCompanyJobApplied);

                if (job == null)
                {
                    showLoadingModalWhenWithdrawJobApplication = false;
                    StateHasChanged();
                    await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε η θέση εργασίας.");
                    return;
                }

                // Step 2: Update status
                await UpdateProgressWhenWithdrawJobApplication(30, 200);
            
                application.CompanyPositionStatusAppliedAtTheCompanySide = "Αποσύρθηκε από τον φοιτητή";
                application.CompanyPositionStatusAppliedAtTheStudentSide = "Αποσύρθηκε από τον φοιτητή";

                // Step 3: Add platform action
                await UpdateProgressWhenWithdrawJobApplication(50, 200);
            
                var platformAction = new PlatformActions
                {
                    UserRole_PerformedAction = "STUDENT",
                    ForWhat_PerformedAction = "COMPANY_JOB",
                    HashedPositionRNG_PerformedAction = HashLong(application.RNGForCompanyJobApplied),
                    TypeOfAction_PerformedAction = "SELFWITHDRAW",
                    DateTime_PerformedAction = DateTime.Now
                };

                dbContext.PlatformActions.Add(platformAction);
                await dbContext.SaveChangesAsync();
            
                await UpdateProgressWhenWithdrawJobApplication(70, 200);

                // Step 4: Get student details
                await UpdateProgressWhenWithdrawJobApplication(80, 200);
            
                var student = await GetStudentDetails(application.StudentEmailAppliedForCompanyJob);

                if (student == null)
                {
                    showLoadingModalWhenWithdrawJobApplication = false;
                    StateHasChanged();
                    await JS.InvokeVoidAsync("alert", "Δεν βρέθηκαν στοιχεία φοιτητή.");
                    return;
                }

                // Step 5: Send notifications
                await UpdateProgressWhenWithdrawJobApplication(90, 200);
            
                await InternshipEmailService.SendJobWithdrawalNotificationToCompany_AsStudent(
                    application.CompanysEmailWhereStudentAppliedForCompanyJob,
                    job.Company?.CompanyName,
                    student.Name,
                    student.Surname,
                    job.PositionTitle,
                    application.RNGForCompanyJobAppliedAsStudent_HashedAsUniqueID);

                await InternshipEmailService.SendJobWithdrawalConfirmationToStudent_AsCompany(
                    application.StudentEmailAppliedForCompanyJob,
                    student.Name,
                    student.Surname,
                    job.PositionTitle,
                    application.RNGForCompanyJobAppliedAsStudent_HashedAsUniqueID,
                    job.Company?.CompanyName);

                await UpdateProgressWhenWithdrawJobApplication(100, 200);
            
                // Small delay to show completion
                await Task.Delay(500);
            
                // Hide loading modal before navigation
                showLoadingModalWhenWithdrawJobApplication = false;
                StateHasChanged();

                // AFTER user clicks OK, show the navigation loader
                await Task.Delay(100);
            
                // Show the navigation loader
                await JS.InvokeVoidAsync("showBlazorNavigationLoader", "Παρακαλώ Περιμένετε...");
            
                // Give time for loader to render
                await Task.Delay(300);
            
                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            catch (Exception ex)
            {
                // Hide loading modal on error
                showLoadingModalWhenWithdrawJobApplication = false;
                StateHasChanged();
                Console.WriteLine($"Error withdrawing job application: {ex.Message}");
                await JS.InvokeVoidAsync("alert", "Σφάλμα κατά την απόσυρση της αίτησης.");
            }
        }

        private async Task UpdateProgressWhenWithdrawJobApplication(int progress, int delayMs = 0)
        {
            loadingProgressWhenWithdrawJobApplication = progress;
            StateHasChanged();

            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }
        }

        // Company Details Modals
        private async Task ShowCompanyDetailsInJobCompanyName_StudentJobApplications(string companyEmail)
        {
            try
            {
                // First check if we already have the company details in cache
                if (companyDataCache.TryGetValue(companyEmail, out var cachedCompany))
                {
                    selectedCompanyDetails_StudentJobApplications = cachedCompany;
                }
                else
                {
                    // Fetch the company details from the database using email
                    selectedCompanyDetails_StudentJobApplications = await dbContext.Companies
                        .FirstOrDefaultAsync(c => c.CompanyEmail == companyEmail);

                    // Add to cache if found
                    if (selectedCompanyDetails_StudentJobApplications != null)
                    { 
                        companyDataCache[companyEmail] = selectedCompanyDetails_StudentJobApplications;
                    }
                }

                if (selectedCompanyDetails_StudentJobApplications != null)
                {
                    showCompanyDetailsModal_StudentJobApplications = true;
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

        private void CloseCompanyDetailsModal_StudentJobApplications()
        {
            showCompanyDetailsModal_StudentJobApplications = false;
            StateHasChanged();
        }

        // Job Details Modals
        private async Task ShowJobDetailsInJobTitleAsHyperlink_StudentJobApplications(long jobRNG)
        {
            await ShowCompanyJobDetailsModal_StudentJobApplications(jobRNG);
        }

        private async Task ShowCompanyJobDetailsModal_StudentJobApplications(long jobRNG)
        {
            // Fetch the job details asynchronously
            selectedCompanyJobDetails_StudentJobApplications = await dbContext.CompanyJobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.RNGForPositionUploaded == jobRNG);

            if (selectedCompanyJobDetails_StudentJobApplications != null)
            {
                // Open the modal if the job details are found
                showCompanyJobDetailsModal_StudentJobApplications = true;
                StateHasChanged();
            }
            else
            {
                // Show an alert if no job details are found
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Δεν μπορούν να εμφανιστούν οι λεπτομέρειες της Θέσης. <span style='color:darkred;'>Η Θέση Δεν Είναι Πλέον Διαθέσιμη από τον Φορέα</span>");
            }
        }

        private void CloseCompanyJobDetailsModal_StudentJobApplications()
        {
            showCompanyJobDetailsModal_StudentJobApplications = false;
            StateHasChanged();
        }

        // Helper Methods
        private async Task<Student> GetStudentDetails(string email)
        {
            return await dbContext.Students
                .FirstOrDefaultAsync(s => s.Email == email);
        }

        // Helper method to get job details from cache
        private CompanyJob GetJobFromCache(long rng)
        {
            return jobDataCache.TryGetValue(rng, out var job) ? job : null;
        }

        // Helper method for status color
        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Επιτυχής" => "lightgreen",
                "Απορρίφθηκε" => "lightcoral",
                "Αποσύρθηκε από τον φοιτητή" => "lightyellow",
                "Απόσυρση Θέσεως" => "coral",
                _ => "transparent"
            };
        }

        // Helper method for hashing (placeholder - will need actual HashingHelper class)
        private string HashLong(long value)
        {
            // This is a placeholder - need to find the actual HashingHelper implementation
            return value.GetHashCode().ToString();
        }
    }
}

