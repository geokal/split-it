using Microsoft.AspNetCore.Components;
using QuizManager.ViewModels;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using QuizManager.Models;
using QuizManager.Services.StudentDashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.StudentSections
{
    public partial class StudentJobsDisplaySection : ComponentBase
    {
        [Inject] private IStudentDashboardService StudentDashboardService { get; set; } = default!;
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
        private QuizManager.Models.Company selectedCompanyDetails_StudentJobApplications;
        private bool showCompanyDetailsModal_StudentJobApplications = false;
        private Dictionary<string, QuizManager.Models.Company> companyDataCache = new Dictionary<string, QuizManager.Models.Company>();

        // Job Details Modals
        private CompanyJob selectedCompanyJobDetails_StudentJobApplications;
        private bool showCompanyJobDetailsModal_StudentJobApplications = false;
        private CompanyJob currentJob;
        private CompanyJob currentJobApplicationMadeAsStudent;
        private bool isModalVisibleForJobs = false;
        private bool isJobDetailsModalVisibleToSeeJobApplicationsAsStudent = false;
        private bool isJobDetailsModal2Visible = false;

        // Company Details for Job Show/Search
        private QuizManager.Models.Company selectedCompanyDetailsForJobShow;
        private QuizManager.Models.Company selectedCompanyDetailsForJobSearch;
        private bool isCompanyDetailsModalOpenForJobShow = false;
        private bool isCompanyDetailsModalOpenForJobSearch = false;

        // Job Search/Display Properties
        private List<CompanyJob> jobs = new List<CompanyJob>();
        private int currentJobPositionPage = 1;
        private int jobPositionPageSize = 10;
        private string jobSearchByRegion = "";
        private bool isJobPositionAsStudentFiltersVisible = false;
        private bool isPositionAreasVisible = false;
        private bool isLoadingSearchJobApplicationsAsStudent = false;
        private HashSet<int> expandedPositionAreas = new HashSet<int>();
        private List<string> jobTitleAutocompleteSuggestionsWhenSearchForCompanyJobsAsStudent = new List<string>();
        private List<string> companyNameAutocompleteSuggestionsWhenSearchForCompanyJobsAsStudent = new List<string>();

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

        // Application Loading Modal
        private bool showLoadingModalWhenApplyForJobAsStudent = false;
        private int loadingProgressWhenApplyForJobAsStudent = 0;

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

        private StudentDashboardData _dashboardData = StudentDashboardData.Empty;

        private async Task LoadUserJobApplications()
        {
            try
            {
                _dashboardData = await StudentDashboardService.LoadDashboardDataAsync();

                showStudentJobApplications = _dashboardData.IsAuthenticated && _dashboardData.IsRegisteredStudent;

                companyJobApplications = _dashboardData.JobApplications.ToList();
                jobDataCache = new Dictionary<long, CompanyJob>(_dashboardData.JobCache);

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

                await UpdateProgressWhenWithdrawJobApplication(10, 200);

                var jobDetails = jobDataCache.TryGetValue(application.RNGForCompanyJobApplied, out var cachedJob)
                    ? cachedJob
                    : null;
                var student = _dashboardData.Student;

                if (!await StudentDashboardService.WithdrawJobApplicationAsync(application.RNGForCompanyJobApplied))
                {
                    showLoadingModalWhenWithdrawJobApplication = false;
                    StateHasChanged();
                    await JS.InvokeVoidAsync("alert", "Δεν βρέθηκε η θέση εργασίας.");
                    return;
                }

                await UpdateProgressWhenWithdrawJobApplication(70, 200);

                // Refresh dashboard caches and reload UI data
                await StudentDashboardService.RefreshDashboardCacheAsync();
                await LoadUserJobApplications();

                // Optional notifications if data available
                if (jobDetails != null && student != null)
                {
                    var companyName = jobDetails.Company?.CompanyName ?? "Άγνωστη Εταιρεία";

                    await InternshipEmailService.SendJobWithdrawalNotificationToCompany_AsStudent(
                        application.CompanysEmailWhereStudentAppliedForCompanyJob,
                        companyName,
                        student.Name,
                        student.Surname,
                        jobDetails.PositionTitle,
                        application.RNGForCompanyJobAppliedAsStudent_HashedAsUniqueID);

                    await InternshipEmailService.SendJobWithdrawalConfirmationToStudent_AsCompany(
                        application.StudentEmailAppliedForCompanyJob,
                        student.Name,
                        student.Surname,
                        jobDetails.PositionTitle,
                        application.RNGForCompanyJobAppliedAsStudent_HashedAsUniqueID,
                        companyName);
                }

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
                if (companyDataCache.TryGetValue(companyEmail, out var cachedCompany))
                {
                    selectedCompanyDetails_StudentJobApplications = cachedCompany;
                }
                else
                {
                    selectedCompanyDetails_StudentJobApplications = await StudentDashboardService.GetCompanyByEmailAsync(companyEmail);

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
            selectedCompanyJobDetails_StudentJobApplications = jobDataCache.TryGetValue(jobRNG, out var cachedJob)
                ? cachedJob
                : null;

            if (selectedCompanyJobDetails_StudentJobApplications == null)
            {
                // attempt refresh once
                await StudentDashboardService.RefreshDashboardCacheAsync();
                _dashboardData = await StudentDashboardService.LoadDashboardDataAsync();
                jobDataCache = new Dictionary<long, CompanyJob>(_dashboardData.JobCache);
                companyJobApplications = _dashboardData.JobApplications.ToList();
                selectedCompanyJobDetails_StudentJobApplications = jobDataCache.TryGetValue(jobRNG, out var refreshedJob)
                    ? refreshedJob
                    : null;
            }

            if (selectedCompanyJobDetails_StudentJobApplications != null)
            {
                showCompanyJobDetailsModal_StudentJobApplications = true;
                StateHasChanged();
            }
            else
            {
                await JS.InvokeVoidAsync("confirmActionWithHTML2", "Δεν μπορούν να εμφανιστούν οι λεπτομέρειες της Θέσης. <span style='color:darkred;'>Η Θέση Δεν Είναι Πλέον Διαθέσιμη από τον Φορέα</span>");
            }
        }

        private void CloseCompanyJobDetailsModal_StudentJobApplications()
        {
            showCompanyJobDetailsModal_StudentJobApplications = false;
            StateHasChanged();
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

        // Additional Properties
        private bool showJobApplications = false;
        private HashSet<long> jobIdsApplied = new HashSet<long>();
        private string jobSearch = "";
        private string companyNameSearch = "";
        private string positionTypeSearch = "";
        private string jobSearchByTown = "";
        private string companyjobSearchByTransportOffer = "";
        private string globalJobSearch = "";
        private DateTime? selectedDateToSearchJob = null;
        private HashSet<string> selectedPositionAreas = new HashSet<string>();
        private HashSet<string> selectedPositionSubFields = new HashSet<string>();
        private int[] pageSizeOptions_SearchForJobsAsStudent = new[] { 10, 50, 100 };

        // Regions and Areas (placeholders - should be loaded from DB)
        private List<string> Regions = new List<string>
        {
            "Ανατολική Μακεδονία και Θράκη",
            "Κεντρική Μακεδονία",
            "Δυτική Μακεδονία",
            "Ήπειρος",
            "Θεσσαλία",
            "Ιόνια Νησιά",
            "Δυτική Ελλάδα",
            "Κεντρική Ελλάδα",
            "Αττική",
            "Πελοπόννησος",
            "Βόρειο Αιγαίο",
            "Νότιο Αιγαίο",
            "Κρήτη"
        };
        private List<Area> Areas = new List<Area>();

        // Job Details Methods (overloads)
        private async Task ShowJobDetails(CompanyJob job)
        {
            if (job.Company == null && !string.IsNullOrEmpty(job.EmailUsedToUploadJobs))
            {
                job.Company = await StudentDashboardService.GetCompanyByEmailAsync(job.EmailUsedToUploadJobs);
            }

            currentJob = job;
            isModalVisibleForJobs = true;
            StateHasChanged();
        }

        private async Task ShowJobDetails(CompanyJobApplied jobApplication)
        {
            currentJobApplicationMadeAsStudent = jobDataCache.TryGetValue(jobApplication.RNGForCompanyJobApplied, out var cachedJob)
                ? cachedJob
                : null;

            if (currentJobApplicationMadeAsStudent == null)
            {
                await StudentDashboardService.RefreshDashboardCacheAsync();
                _dashboardData = await StudentDashboardService.LoadDashboardDataAsync();
                jobDataCache = new Dictionary<long, CompanyJob>(_dashboardData.JobCache);
                companyJobApplications = _dashboardData.JobApplications.ToList();
                currentJobApplicationMadeAsStudent = jobDataCache.TryGetValue(jobApplication.RNGForCompanyJobApplied, out var refreshedJob)
                    ? refreshedJob
                    : null;
            }

            isJobDetailsModalVisibleToSeeJobApplicationsAsStudent = currentJobApplicationMadeAsStudent != null;
            StateHasChanged();
        }

        private async Task ShowJobDetailsModal()
        {
            await JS.InvokeVoidAsync("ShowBootstrapModal", "#jobDetailsModal");
        }

        // Modal Close Methods
        private void CloseModalForJobs()
        {
            isModalVisibleForJobs = false;
            currentJob = null;
            StateHasChanged();
        }

        private void CloseJobDetailsModalInJobTitleAsHyperlink_StudentJobApplications()
        {
            isJobDetailsModalVisibleToSeeJobApplicationsAsStudent = false;
            currentJobApplicationMadeAsStudent = null;
            StateHasChanged();
        }

        private void CloseModalForCompanyNameHyperlinkDetailsInJobShow()
        {
            isCompanyDetailsModalOpenForJobShow = false;
            selectedCompanyDetailsForJobShow = null;
            StateHasChanged();
        }

        private void CloseModalForCompanyNameHyperlinkDetailsInJobSearch()
        {
            isCompanyDetailsModalOpenForJobSearch = false;
            selectedCompanyDetailsForJobSearch = null;
            StateHasChanged();
        }

        // Company Details Show Methods
        private void ShowCompanyDetailsAsAHyperlinkInShowJobsAsStudent(QuizManager.Models.Company company)
        {
            selectedCompanyDetailsForJobShow = company;
            isCompanyDetailsModalOpenForJobShow = true;
            StateHasChanged();
        }

        private void ShowCompanyDetailsAsAHyperlinkInJobSearchAsStudent(QuizManager.Models.Company company)
        {
            selectedCompanyDetailsForJobSearch = company;
            isCompanyDetailsModalOpenForJobSearch = true;
            StateHasChanged();
        }

        // Profile Image
        private string ShowProfileImage(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return "/images/default-profile.png";
            return $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
        }

        // Pagination Methods
        private void ChangeJobPage(int newPage)
        {
            var totalPages = (int)Math.Ceiling((double)jobApplications.Count / jobPageSize);
            if (newPage > 0 && newPage <= totalPages)
            {
                currentPageForJobsToSee = newPage;
                StateHasChanged();
            }
        }

        private void ChangeJobPositionPage(int newPage)
        {
            var publishedJobs = jobs?.Where(i => i.PositionStatus == "Δημοσιευμένη").ToList();
            if (publishedJobs != null)
            {
                var totalPages = (int)Math.Ceiling((double)publishedJobs.Count / jobPositionPageSize);
                if (newPage > 0 && newPage <= totalPages)
                {
                    currentJobPositionPage = newPage;
                    StateHasChanged();
                }
            }
        }

        private List<int> GetVisiblePages(int currentPage, int totalPages)
        {
            var pages = new List<int>();
            
            if (totalPages <= 7)
            {
                for (int i = 1; i <= totalPages; i++)
                    pages.Add(i);
            }
            else
            {
                pages.Add(1);
                if (currentPage > 4) pages.Add(-1);

                int start = Math.Max(2, currentPage - 1);
                int end = Math.Min(totalPages - 1, currentPage + 1);

                for (int i = start; i <= end; i++)
                    pages.Add(i);

                if (currentPage < totalPages - 2) pages.Add(-1);
                if (totalPages > 1) pages.Add(totalPages);
            }

            return pages;
        }

        private List<int> GetVisiblePages()
        {
            return GetVisiblePages(currentJobPage, totalPagesForJobsToSee);
        }

        // Filter Toggle Methods
        private void ToggleJobPositionAsStudentFiltersVisibility()
        {
            isJobPositionAsStudentFiltersVisible = !isJobPositionAsStudentFiltersVisible;
            StateHasChanged();
        }

        private void TogglePositionAreasVisibility()
        {
            isPositionAreasVisible = !isPositionAreasVisible;
            StateHasChanged();
        }

        private void TogglePositionSubFields(int areaId)
        {
            if (expandedPositionAreas.Contains(areaId))
                expandedPositionAreas.Remove(areaId);
            else
                expandedPositionAreas.Add(areaId);
            StateHasChanged();
        }

        private void OnPositionAreaCheckboxChanged(ChangeEventArgs e, string areaName)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
                selectedPositionAreas.Add(areaName);
            else
                selectedPositionAreas.Remove(areaName);
            StateHasChanged();
        }

        private void OnPositionSubFieldCheckboxChanged(ChangeEventArgs e, string subField)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
                selectedPositionSubFields.Add(subField);
            else
                selectedPositionSubFields.Remove(subField);
            StateHasChanged();
        }

        // Autocomplete Handlers
        private async Task HandleJobTitleAutocompleteInputWhenSearchCompanyJobsAsStudent(ChangeEventArgs e)
        {
            jobSearch = e.Value?.ToString().Trim() ?? "";
            jobTitleAutocompleteSuggestionsWhenSearchForCompanyJobsAsStudent.Clear();

            if (jobSearch.Length >= 2)
            {
                try
                {
                    var suggestions = await StudentDashboardService.GetJobTitleSuggestionsAsync(jobSearch);
                    jobTitleAutocompleteSuggestionsWhenSearchForCompanyJobsAsStudent = suggestions.ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading job title suggestions: {ex.Message}");
                }
            }
            StateHasChanged();
        }

        private async Task HandleCompanyNameAutocompleteInputWhenSearchCompanyJobsAsStudent(ChangeEventArgs e)
        {
            companyNameSearch = e.Value?.ToString().Trim() ?? "";
            companyNameAutocompleteSuggestionsWhenSearchForCompanyJobsAsStudent.Clear();

            if (companyNameSearch.Length >= 2)
            {
                try
                {
                    var suggestions = await StudentDashboardService.GetCompanyNameSuggestionsAsync(companyNameSearch);
                    companyNameAutocompleteSuggestionsWhenSearchForCompanyJobsAsStudent = suggestions.ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading company name suggestions: {ex.Message}");
                }
            }
            StateHasChanged();
        }

        private void SelectJobTitleAutocompleteSuggestionWhenSearchCompanyJobsAsStudent(string suggestion)
        {
            jobSearch = suggestion;
            jobTitleAutocompleteSuggestionsWhenSearchForCompanyJobsAsStudent.Clear();
            StateHasChanged();
        }

        private void SelectCompanyNameAutocompleteSuggestionWhenSearchCompanyJobsAsStudent(string suggestion)
        {
            companyNameSearch = suggestion;
            companyNameAutocompleteSuggestionsWhenSearchForCompanyJobsAsStudent.Clear();
            StateHasChanged();
        }

        // Search and Clear Methods
        private void SearchJobApplicationsAsStudent()
        {
            // TODO: Implement search logic
            StateHasChanged();
        }

        private void ClearSearchFieldsForJobApplicationsAsStudent()
        {
            jobSearch = "";
            companyNameSearch = "";
            positionTypeSearch = "";
            jobSearchByTown = "";
            companyjobSearchByTransportOffer = "";
            globalJobSearch = "";
            selectedDateToSearchJob = null;
            selectedPositionAreas.Clear();
            selectedPositionSubFields.Clear();
            jobTitleAutocompleteSuggestionsWhenSearchForCompanyJobsAsStudent.Clear();
            companyNameAutocompleteSuggestionsWhenSearchForCompanyJobsAsStudent.Clear();
            StateHasChanged();
        }

        // Page Size Change
        private void OnJobPageSizeChange_SearchForJobsAsStudent(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newSize) && newSize > 0)
            {
                pageSizeForJobsToSee = newSize;
                currentPageForJobsToSee = 1;
                StateHasChanged();
            }
        }

        // Apply for Job (simplified - should delegate to StudentDashboardService)
        private async Task ApplyForJobAsStudent(CompanyJob job)
        {
            showLoadingModalWhenApplyForJobAsStudent = true;
            loadingProgressWhenApplyForJobAsStudent = 0;
            StateHasChanged();

            try
            {
                await UpdateProgressWhenApplyForJobAsStudent(50, 300);
                // TODO: Call StudentDashboardService.ApplyForJobAsync
                await UpdateProgressWhenApplyForJobAsStudent(100, 300);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying for job: {ex.Message}");
            }
            finally
            {
                showLoadingModalWhenApplyForJobAsStudent = false;
                StateHasChanged();
            }
        }

        private async Task UpdateProgressWhenApplyForJobAsStudent(int current, int total)
        {
            loadingProgressWhenApplyForJobAsStudent = (int)((double)current / total * 100);
            StateHasChanged();
            await Task.Delay(50);
        }

        // Apply for Job Confirmation
        private async Task ConfirmApplyForJob(CompanyJob job)
        {
            var message = $"Πρόκεται να κάνετε αίτηση για την Θέση \"{job.PositionTitle}\". Είστε σίγουρος/η;";
            var confirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML", message);
            
            if (confirmed)
            {
                await ApplyForJobAsStudent(job);
            }
        }

        // Withdraw Job Application
        private async Task WithdrawJobApplicationMadeByStudent(CompanyJobApplied jobApplication)
        {
            // TODO: Implement withdrawal logic - should delegate to StudentDashboardService
            await Task.CompletedTask;
        }
    }
}
