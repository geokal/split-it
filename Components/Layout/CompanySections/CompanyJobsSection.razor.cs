using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using QuizManager.Data;
using QuizManager.Models;
using QuizManager.Services;
using QuizManager.Services.CompanyDashboard;
using QuizManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizManager.Components.Layout.CompanySections
{
    public partial class CompanyJobsSection : ComponentBase
    {
        [Inject] private ICompanyDashboardService CompanyDashboardService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private InternshipEmailService InternshipEmailService { get; set; } = default!;

        // User Info
        private string CurrentUserEmail = "";
        private QuizManager.Models.Company companyData;
        private string companyName = "";

        // Form Model and Validation
        private CompanyJob job = new CompanyJob();
        private bool showErrorMessageforUploadingjobsAsCompany = false;
        private bool showLoadingModalForJob = false;
        private int loadingProgress = 0;
        private bool showSuccessMessage = false;
        private bool showErrorMessagesForAreasWhenUploadJobAsCompany = false;

        // Character Limits
        private int remainingCharactersInJobFieldUploadAsCompany = 120;
        private int remainingCharactersInJobDescriptionUploadAsCompany = 1000;

        // Areas/Subfields
        private List<Area> Areas = new List<Area>();
        private List<Area> SelectedAreasWhenUploadJobAsCompany = new List<Area>();
        private Dictionary<int, HashSet<string>> SelectedSubFieldsForCompanyJob = new Dictionary<int, HashSet<string>>();
        private HashSet<int> ExpandedAreasForCompanyJob = new HashSet<int>();
        private bool areCheckboxesVisibleForCompanyJob = false;

        // Location Data
        private List<Region> Regions = new List<Region>();

        // View Uploaded Jobs
        private bool isForm2Visible = false;
        private bool isLoadingJobsHistory = false;
        private string selectedStatusFilterForJobs = "Όλα";
        private List<CompanyJob> UploadedJobs = new List<CompanyJob>();
        private List<CompanyJob> FilteredJobs = new List<CompanyJob>();
        private int currentPageForJobs = 1;
        private int JobsPerPage = 10;
        private int[] pageSizeOptions_SeeMyUploadedJobsAsCompany = new[] { 10, 50, 100 };
        private int totalCountJobs = 0;
        private int publishedCountJobs = 0;
        private int unpublishedCountJobs = 0;
        private int withdrawnCountJobs = 0;

        // Bulk Operations
        private bool isBulkEditMode = false;
        private HashSet<int> selectedJobIds = new HashSet<int>();
        private bool showBulkActionModal = false;
        private string bulkAction = "";
        private string newStatusForBulkAction = "Μη Δημοσιευμένη";

        // Job Menu
        private int activeJobMenuId = 0;
        private long? currentlyExpandedJobId = null;

        // Job Applicants
        private bool isLoadingJobApplicants = false;
        private long? loadingJobPositionId = null;
        private Dictionary<long, List<CompanyJobApplied>> jobApplicantsMap = new Dictionary<long, List<CompanyJobApplied>>();
        private Dictionary<long, int> acceptedApplicantsCountPerJob_ForCompanyJob = new Dictionary<long, int>();
        private Dictionary<long, int> availableSlotsPerJob_ForCompanyJob = new Dictionary<long, int>();

        // Bulk Operations for Applicants
        private bool isBulkEditModeForApplicants = false;
        private string pendingBulkActionForApplicants = "";
        private bool sendEmailsForBulkAction = false;
        private HashSet<(long, string)> selectedApplicantIds = new HashSet<(long, string)>();

        // Edit Job
        private CompanyJob currentJob;
        private bool isEditJobModalVisible = false;
        private HashSet<int> ExpandedAreasForEditCompanyJob = new HashSet<int>();
        private List<Area> SelectedAreasToEditForCompanyJob = new List<Area>();
        private Dictionary<string, List<string>> SelectedSubFieldsForEditCompanyJob = new Dictionary<string, List<string>>();
        private List<string> ForeasType = new List<string>
        {
            "Ιδιωτικός Φορέας",
            "Δημόσιος Φορέας",
            "Μ.Κ.Ο.",
            "Άλλο"
        };

        // Modals
        private bool showSlotWarningModal_ForCompanyJob = false;
        private string slotWarningMessage_ForCompanyJob = "";

        // Computed Properties
        private int totalPagesForJobs => (int)Math.Ceiling((double)(FilteredJobs?.Count ?? 0) / JobsPerPage);

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
                if (companyData != null) companyName = companyData.CompanyName;
            }

            var lookups = await CompanyDashboardService.GetLookupsAsync();
            Areas = lookups.Areas.ToList();
            Regions = lookups.RegionToTownsMap
                .Select(kvp => new Region { RegionName = kvp.Key, Towns = kvp.Value.Select(t => new Town { TownName = t }).ToList() })
                .ToList();
            RegionToTownsMap = lookups.RegionToTownsMap.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList());
        }

        // Character Limits
        private void CheckCharacterLimitInJobFieldUploadAsCompany(ChangeEventArgs e)
        {
            var text = e.Value?.ToString() ?? "";
            remainingCharactersInJobFieldUploadAsCompany = Math.Max(0, 120 - text.Length);
        }

        private void CheckCharacterLimitInJobDescriptionUploadAsCompany(ChangeEventArgs e)
        {
            var text = e.Value?.ToString() ?? "";
            remainingCharactersInJobDescriptionUploadAsCompany = Math.Max(0, 1000 - text.Length);
        }

        // Validation
        private bool IsValidEmailForCompanyJobs(string email)
        {
            return !string.IsNullOrEmpty(email) && email.Contains("@");
        }

        private bool IsValidPhoneNumber(string phone)
        {
            return !string.IsNullOrEmpty(phone) && phone.Length >= 10;
        }

        private void OnPhoneNumberInput(ChangeEventArgs e)
        {
            // Validation handled in markup
        }

        private void ValidatePostalCode(ChangeEventArgs e)
        {
            // Validation handled in markup
        }

        // Location
        private IEnumerable<Town> GetTownsForRegion(string regionName)
        {
            var region = Regions.FirstOrDefault(r => r.RegionName == regionName);
            return region?.Towns ?? Enumerable.Empty<Town>();
        }

        // Transport Offer
        private void UpdateTransportOffer(bool offer)
        {
            job.PositionTransportOffer = offer;
            StateHasChanged();
        }

        // Areas/Subfields
        private void ToggleCheckboxesForCompanyJob()
        {
            areCheckboxesVisibleForCompanyJob = !areCheckboxesVisibleForCompanyJob;
            StateHasChanged();
        }

        private bool HasAnySelectionForCompanyJob()
        {
            return SelectedAreasWhenUploadJobAsCompany.Any() ||
                   SelectedSubFieldsForCompanyJob.Any(kvp => kvp.Value.Any());
        }

        private void OnAreaCheckedChangedForCompanyJob(ChangeEventArgs e, Area area)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked)
            {
                if (!SelectedAreasWhenUploadJobAsCompany.Any(a => a.Id == area.Id))
                    SelectedAreasWhenUploadJobAsCompany.Add(area);
            }
            else
            {
                SelectedAreasWhenUploadJobAsCompany.RemoveAll(a => a.Id == area.Id);
                SelectedSubFieldsForCompanyJob.Remove(area.Id);
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForCompanyJob(Area area)
        {
            return SelectedAreasWhenUploadJobAsCompany.Any(a => a.Id == area.Id);
        }

        private void ToggleSubFieldsForCompanyJob(Area area)
        {
            if (ExpandedAreasForCompanyJob.Contains(area.Id))
                ExpandedAreasForCompanyJob.Remove(area.Id);
            else
                ExpandedAreasForCompanyJob.Add(area.Id);
            StateHasChanged();
        }

        private void OnSubFieldCheckedChangedForCompanyJob(ChangeEventArgs e, Area area, string subField)
        {
            bool isChecked = (bool)e.Value;
            if (!SelectedSubFieldsForCompanyJob.ContainsKey(area.Id))
                SelectedSubFieldsForCompanyJob[area.Id] = new HashSet<string>();

            if (isChecked)
                SelectedSubFieldsForCompanyJob[area.Id].Add(subField);
            else
                SelectedSubFieldsForCompanyJob[area.Id].Remove(subField);
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForCompanyJob(Area area, string subField)
        {
            return SelectedSubFieldsForCompanyJob.ContainsKey(area.Id) &&
                   SelectedSubFieldsForCompanyJob[area.Id].Contains(subField);
        }

        // File Handling
        private async Task HandleFileSelected(InputFileChangeEventArgs e)
        {
            try
            {
                if (e.File == null)
                {
                    job.PositionAttachment = null;
                    return;
                }

                const long maxFileSize = 10485760; // 10MB
                if (e.File.Size > maxFileSize) return;

                using var memoryStream = new System.IO.MemoryStream();
                await e.File.OpenReadStream(maxFileSize).CopyToAsync(memoryStream);
                job.PositionAttachment = memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
            }
        }

        // Save Operations
        private async Task HandleTemporarySaveJobAsCompany()
        {
            await SaveJobAsCompany(false);
        }

        private async Task HandlePublishSaveJobAsCompany()
        {
            await SaveJobAsCompany(true);
        }

        private async Task SaveJobAsCompany(bool publishJob)
        {
            showLoadingModalForJob = true;
            loadingProgress = 0;
            showErrorMessageforUploadingjobsAsCompany = false;
            showSuccessMessage = false;
            StateHasChanged();

            try
            {
                await UpdateProgressWhenSaveJobAsCompany(10);

                await UpdateProgressWhenSaveJobAsCompany(30);
                if (!await HandleJobValidationWhenSaveJobAsCompany())
                    return;

                await UpdateProgressWhenSaveJobAsCompany(50);

                // Build areas string
                var areasWithSubfields = new List<string>();
                foreach (var area in SelectedAreasWhenUploadJobAsCompany)
                    areasWithSubfields.Add(area.AreaName);
                foreach (var areaSubFields in SelectedSubFieldsForCompanyJob)
                    areasWithSubfields.AddRange(areaSubFields.Value);
                job.PositionAreas = string.Join(",", areasWithSubfields);

                // Set job properties
                job.RNGForPositionUploaded = new Random().NextInt64();
                job.RNGForPositionUploaded_HashedAsUniqueID = HashingHelper.HashLong(job.RNGForPositionUploaded);
                job.EmailUsedToUploadJobs = CurrentUserEmail;
                job.UploadDateTime = DateTime.Now;
                job.PositionForeas = companyData?.CompanyType ?? "";
                job.PositionStatus = publishJob ? "Δημοσιευμένη" : "Μη Δημοσιευμένη";

                await UpdateProgressWhenSaveJobAsCompany(70);
                var result = await CompanyDashboardService.CreateOrUpdateJobAsync(job);
                if (!result.Success)
                {
                    throw new InvalidOperationException(result.Error ?? "Failed to save job.");
                }

                await UpdateProgressWhenSaveJobAsCompany(90);
                showSuccessMessage = true;
                showErrorMessageforUploadingjobsAsCompany = false;

                job = new CompanyJob();
                SelectedAreasWhenUploadJobAsCompany.Clear();
                SelectedSubFieldsForCompanyJob.Clear();
                ExpandedAreasForCompanyJob.Clear();

                await UpdateProgressWhenSaveJobAsCompany(100);
                await Task.Delay(500);
                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            catch (Exception ex)
            {
                showLoadingModalForJob = false;
                showSuccessMessage = false;
                showErrorMessageforUploadingjobsAsCompany = true;
                Console.WriteLine($"Error uploading job: {ex.Message}");
                StateHasChanged();
            }
        }

        private async Task<bool> HandleJobValidationWhenSaveJobAsCompany()
        {
            if (string.IsNullOrWhiteSpace(job.PositionType))
            {
                await HandleJobValidationErrorWhenSaveJobAsCompany("positionType");
                return false;
            }
            if (job.PositionActivePeriod.Date <= DateTime.Today)
            {
                await HandleJobValidationErrorWhenSaveJobAsCompany("positionActivePeriod");
                return false;
            }
            if (string.IsNullOrWhiteSpace(job.PositionTitle))
            {
                await HandleJobValidationErrorWhenSaveJobAsCompany("positionTitle");
                return false;
            }
            if (string.IsNullOrWhiteSpace(job.PositionContactPerson))
            {
                await HandleJobValidationErrorWhenSaveJobAsCompany("positionContactPerson");
                return false;
            }
            if (string.IsNullOrWhiteSpace(job.PositionPerifereiaLocation))
            {
                await HandleJobValidationErrorWhenSaveJobAsCompany("positionPerifereia");
                return false;
            }
            if (string.IsNullOrWhiteSpace(job.PositionDimosLocation))
            {
                await HandleJobValidationErrorWhenSaveJobAsCompany("positionDimos");
                return false;
            }
            if (string.IsNullOrWhiteSpace(job.PositionDescription))
            {
                await HandleJobValidationErrorWhenSaveJobAsCompany("positionDescription");
                return false;
            }
            if (string.IsNullOrWhiteSpace(job.PositionAddressLocation))
            {
                await HandleJobValidationErrorWhenSaveJobAsCompany("positionAddress");
                return false;
            }
            if (!HasAnySelectionForCompanyJob())
            {
                await HandleJobValidationErrorWhenSaveJobAsCompany("positionAreas");
                return false;
            }
            if (job.OpenSlots < 3)
            {
                await HandleJobValidationErrorWhenSaveJobAsCompany("openSlots");
                return false;
            }
            return true;
        }

        private async Task HandleJobValidationErrorWhenSaveJobAsCompany(string elementId)
        {
            showLoadingModalForJob = false;
            showErrorMessageforUploadingjobsAsCompany = true;
            StateHasChanged();
            await JS.InvokeVoidAsync("scrollToElementById", elementId);
        }

        private async Task UpdateProgressWhenSaveJobAsCompany(int progress)
        {
            loadingProgress = progress;
            StateHasChanged();
            await Task.Delay(50);
        }

        // View Uploaded Jobs
        private async Task ToggleFormVisibilityToShowMyActiveJobsAsCompany()
        {
            isForm2Visible = !isForm2Visible;
            if (isForm2Visible)
            {
                isLoadingJobsHistory = true;
                StateHasChanged();
                try
                {
                    await LoadUploadedJobsAsync();
                    await ApplyFiltersAndUpdateCountsForJobs();
                }
                finally
                {
                    isLoadingJobsHistory = false;
                    StateHasChanged();
                }
            }
        }

        private async Task LoadUploadedJobsAsync()
        {
            var data = await CompanyDashboardService.LoadDashboardDataAsync();
            UploadedJobs = data.Jobs
                .Where(j => j.EmailUsedToUploadJobs == CurrentUserEmail)
                .OrderByDescending(j => j.UploadDateTime)
                .ToList();
        }

        private async Task ApplyFiltersAndUpdateCountsForJobs()
        {
            FilteredJobs = selectedStatusFilterForJobs == "Όλα"
                ? UploadedJobs.ToList()
                : UploadedJobs.Where(j => j.PositionStatus == selectedStatusFilterForJobs).ToList();

            totalCountJobs = UploadedJobs.Count;
            publishedCountJobs = UploadedJobs.Count(j => j.PositionStatus == "Δημοσιευμένη");
            unpublishedCountJobs = UploadedJobs.Count(j => j.PositionStatus == "Μη Δημοσιευμένη");
            withdrawnCountJobs = UploadedJobs.Count(j => j.PositionStatus == "Ανακληθείσα");
            currentPageForJobs = 1;
        }

        private void HandleStatusFilterChangeForJobs(ChangeEventArgs e)
        {
            selectedStatusFilterForJobs = e.Value?.ToString() ?? "Όλα";
            _ = ApplyFiltersAndUpdateCountsForJobs();
        }

        private void OnPageSizeChange_SeeMyUploadedJobsAsCompany(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var size) && size > 0)
            {
                JobsPerPage = size;
                currentPageForJobs = 1;
                StateHasChanged();
            }
        }

        private IEnumerable<CompanyJob> GetPaginatedJobs()
        {
            return FilteredJobs?
                .Skip((currentPageForJobs - 1) * JobsPerPage)
                .Take(JobsPerPage)
                ?? Enumerable.Empty<CompanyJob>();
        }

        // Pagination
        private void GoToFirstPageForJobs() => ChangePageForJobs(1);
        private void PreviousPageForJobs() => ChangePageForJobs(Math.Max(1, currentPageForJobs - 1));
        private void NextPageForJobs() => ChangePageForJobs(Math.Min(totalPagesForJobs, currentPageForJobs + 1));
        private void GoToLastPageForJobs() => ChangePageForJobs(totalPagesForJobs);
        private void GoToPageForJobs(int page) => ChangePageForJobs(page);

        private void ChangePageForJobs(int newPage)
        {
            if (newPage > 0 && newPage <= totalPagesForJobs)
            {
                currentPageForJobs = newPage;
                StateHasChanged();
            }
        }

        private List<int> GetVisiblePagesForJobs()
        {
            var pages = new List<int>();
            int current = currentPageForJobs;
            int total = totalPagesForJobs;
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
        private void EnableBulkEditMode()
        {
            isBulkEditMode = true;
            selectedJobIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEdit()
        {
            isBulkEditMode = false;
            selectedJobIds.Clear();
            StateHasChanged();
        }

        private void ToggleJobSelection(int jobId, ChangeEventArgs e)
        {
            bool isChecked = (bool)e.Value;
            if (isChecked) selectedJobIds.Add(jobId);
            else selectedJobIds.Remove(jobId);
            StateHasChanged();
        }

        private void ShowBulkActionOptions()
        {
            if (selectedJobIds.Any())
                showBulkActionModal = true;
        }

        private void CloseBulkActionModal()
        {
            showBulkActionModal = false;
            bulkAction = "";
        }

        private async Task ExecuteBulkStatusChangeForJobs(string newStatus)
        {
            foreach (var jobId in selectedJobIds)
            {
                await CompanyDashboardService.UpdateJobStatusAsync(jobId, newStatus);
            }
            CancelBulkEdit();
            await LoadUploadedJobsAsync();
            await ApplyFiltersAndUpdateCountsForJobs();
        }

        private async Task ExecuteBulkCopyForJobs()
        {
            var jobsToCopy = UploadedJobs
                .Where(j => selectedJobIds.Contains(j.Id))
                .ToList();

            foreach (var j in jobsToCopy)
            {
                var copy = new CompanyJob
                {
                    PositionTitle = j.PositionTitle + " (Αντίγραφο)",
                    PositionDescription = j.PositionDescription,
                    PositionType = j.PositionType,
                    PositionAreas = j.PositionAreas,
                    PositionStatus = "Μη Δημοσιευμένη",
                    EmailUsedToUploadJobs = CurrentUserEmail,
                    UploadDateTime = DateTime.Now,
                    RNGForPositionUploaded = new Random().NextInt64()
                };
                copy.RNGForPositionUploaded_HashedAsUniqueID = HashingHelper.HashLong(copy.RNGForPositionUploaded);
                await CompanyDashboardService.CreateOrUpdateJobAsync(copy);
            }

            CancelBulkEdit();
            await LoadUploadedJobsAsync();
            await ApplyFiltersAndUpdateCountsForJobs();
        }

        private async Task ExecuteBulkAction()
        {
            if (string.IsNullOrEmpty(bulkAction) || !selectedJobIds.Any()) return;

            if (bulkAction == "Αλλαγή Κατάστασης")
                await ExecuteBulkStatusChangeForJobs(newStatusForBulkAction);
            else if (bulkAction == "Αντιγραφή")
                await ExecuteBulkCopyForJobs();
            else if (bulkAction == "Διαγραφή")
            {
                foreach (var jobId in selectedJobIds)
                {
                    await CompanyDashboardService.DeleteJobAsync(jobId);
                }
                CancelBulkEdit();
                await LoadUploadedJobsAsync();
                await ApplyFiltersAndUpdateCountsForJobs();
            }

            CloseBulkActionModal();
        }

        // Job Menu & Operations
        private void ToggleJobMenu(int jobId)
        {
            activeJobMenuId = activeJobMenuId == jobId ? 0 : jobId;
            StateHasChanged();
        }

        private async Task DeleteJobPosition(int jobId)
        {
            await CompanyDashboardService.DeleteJobAsync(jobId);
            await LoadUploadedJobsAsync();
            await ApplyFiltersAndUpdateCountsForJobs();
        }

        private async Task DownloadAttachmentForCompanyJobs(int jobId)
        {
            var attachment = await CompanyDashboardService.GetJobAttachmentAsync(jobId);
            if (attachment?.Data != null && attachment.Data.Length > 0)
            {
                await JS.InvokeVoidAsync("BlazorDownloadAttachmentPositionFile",
                    attachment.FileName, attachment.ContentType, attachment.Data);
            }
        }

        private void ToggleJobsExpanded(long jobPositionId)
        {
            currentlyExpandedJobId = currentlyExpandedJobId == jobPositionId ? null : jobPositionId;
            StateHasChanged();
        }

        private async Task ShowJobDetails(CompanyJob job)
        {
            if (job.Company == null)
            {
                if (!string.IsNullOrWhiteSpace(job.EmailUsedToUploadJobs))
                {
                    job.Company = await CompanyDashboardService.GetCompanyByEmailAsync(job.EmailUsedToUploadJobs);
                }
            }

            currentJob = job;
            isModalVisibleForJobs = true;
            StateHasChanged();
        }

        private void EditJobDetails(CompanyJob job)
        {
            selectedJob = new CompanyJob
            {
                Id = job.Id,
                PositionTitle = job.PositionTitle,
                PositionDescription = job.PositionDescription,
                PositionStatus = job.PositionStatus,
                PositionType = job.PositionType,
                PositionForeas = job.PositionForeas,
                PositionContactPerson = job.PositionContactPerson,
                PositionPerifereiaLocation = job.PositionPerifereiaLocation,
                PositionDimosLocation = job.PositionDimosLocation,
                PositionPostalCodeLocation = job.PositionPostalCodeLocation,
                PositionTransportOffer = job.PositionTransportOffer,
                PositionAreas = job.PositionAreas,
                PositionActivePeriod = job.PositionActivePeriod,
                PositionAttachment = job.PositionAttachment,
                RNGForPositionUploaded_HashedAsUniqueID = job.RNGForPositionUploaded_HashedAsUniqueID,
                RNGForPositionUploaded = job.RNGForPositionUploaded,
                UploadDateTime = job.UploadDateTime,
                TimesUpdated = job.TimesUpdated,
                UpdateDateTime = DateTime.Today,
                OpenSlots = job.OpenSlots
            };

            SelectedAreasToEditForCompanyJob.Clear();
            SelectedSubFieldsForEditCompanyJob.Clear();
            ExpandedAreasForEditCompanyJob.Clear();

            InitializeAreaSelectionsForEdit(job);

            isEditPopupVisibleForJobs = true;
            StateHasChanged();
        }

        private void InitializeAreaSelectionsForEdit(CompanyJob job)
        {
            if (!string.IsNullOrEmpty(job.PositionAreas))
            {
                var selectedItems = job.PositionAreas.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .ToHashSet();

                foreach (var area in Areas)
                {
                    if (selectedItems.Contains(area.AreaName))
                    {
                        SelectedAreasToEditForCompanyJob.Add(area);
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
                                if (!SelectedSubFieldsForEditCompanyJob.ContainsKey(area.AreaName))
                                {
                                    SelectedSubFieldsForEditCompanyJob[area.AreaName] = new List<string>();
                                }
                                if (!SelectedSubFieldsForEditCompanyJob[area.AreaName].Contains(subField))
                                {
                                    SelectedSubFieldsForEditCompanyJob[area.AreaName].Add(subField);
                                    hasSelectedSubfields = true;
                                }
                            }
                        }

                        if (hasSelectedSubfields)
                        {
                            ExpandedAreasForEditCompanyJob.Add(area.Id);
                        }
                    }
                }
            }
        }

        private void OnAreaCheckedChangedForEditCompanyJob(ChangeEventArgs e, Area area)
        {
            var isChecked = (bool)e.Value!;

            if (isChecked)
            {
                if (!SelectedAreasToEditForCompanyJob.Contains(area))
                {
                    SelectedAreasToEditForCompanyJob.Add(area);
                }
            }
            else
            {
                SelectedAreasToEditForCompanyJob.Remove(area);

                if (SelectedSubFieldsForEditCompanyJob.ContainsKey(area.AreaName))
                {
                    SelectedSubFieldsForEditCompanyJob.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsAreaSelectedForEditCompanyJob(Area area)
        {
            return SelectedAreasToEditForCompanyJob.Contains(area);
        }

        private void OnSubFieldCheckedChangedForEditCompanyJob(ChangeEventArgs e, Area area, string subField)
        {
            var isChecked = (bool)e.Value!;

            if (!SelectedSubFieldsForEditCompanyJob.ContainsKey(area.AreaName))
            {
                SelectedSubFieldsForEditCompanyJob[area.AreaName] = new List<string>();
            }

            if (isChecked)
            {
                if (!SelectedSubFieldsForEditCompanyJob[area.AreaName].Contains(subField))
                {
                    SelectedSubFieldsForEditCompanyJob[area.AreaName].Add(subField);
                }
            }
            else
            {
                SelectedSubFieldsForEditCompanyJob[area.AreaName].Remove(subField);

                if (!SelectedSubFieldsForEditCompanyJob[area.AreaName].Any())
                {
                    SelectedSubFieldsForEditCompanyJob.Remove(area.AreaName);
                }
            }
            StateHasChanged();
        }

        private bool IsSubFieldSelectedForEditCompanyJob(Area area, string subField)
        {
            return SelectedSubFieldsForEditCompanyJob.ContainsKey(area.AreaName) &&
                SelectedSubFieldsForEditCompanyJob[area.AreaName].Contains(subField);
        }

        private async Task UpdateJobStatusAsCompany(int jobId, string newStatus)
        {
            await CompanyDashboardService.UpdateJobStatusAsync(jobId, newStatus);
            await LoadUploadedJobsAsync();
            await ApplyFiltersAndUpdateCountsForJobs();
        }

        private async Task ChangeJobStatusToUnpublished(int jobId)
        {
            await UpdateJobStatusAsCompany(jobId, "Μη Δημοσιευμένη");
        }

        private async Task ConfirmAndAcceptJob(long jobRNG, string studentUniqueID)
        {
            var job = UploadedJobs.FirstOrDefault(j => j.RNGForPositionUploaded == jobRNG);
            if (job == null) return;

            int acceptedCount = acceptedApplicantsCountPerJob_ForCompanyJob.GetValueOrDefault(jobRNG, 0);
            int availableSlots = availableSlotsPerJob_ForCompanyJob.GetValueOrDefault(jobRNG, job.OpenSlots);

            if (acceptedCount >= availableSlots)
            {
                slotWarningMessage_ForCompanyJob = $"Έχετε Αποδεχτεί {acceptedCount}/{availableSlots} Αιτούντες, πρέπει να αλλάξετε τον Αριθμό των διαθέσιμων Slots της Θέσης Εργασίας για να προχωρήσετε";
                showSlotWarningModal_ForCompanyJob = true;
                return;
            }

            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML",
                "- ΑΠΟΔΟΧΗ ΑΙΤΗΣΗΣ - \n Η ενέργεια αυτή δεν θα μπορεί να αναιρεθεί. Είστε σίγουρος/η;");

            if (!isConfirmed) return;

            var result = await CompanyDashboardService.DecideOnApplicationAsync(new ApplicationDecisionRequest
            {
                ApplicationType = ApplicationType.Job,
                ApplicationRng = jobRNG,
                StudentUniqueId = studentUniqueID,
                Decision = ApplicationDecision.Accept
            });

            if (!result.Success)
            {
                await JS.InvokeVoidAsync("confirmActionWithHTML2", result.Error ?? "Δεν βρέθηκε η Αίτηση ή ο Φοιτητής.");
                return;
            }

            acceptedApplicantsCountPerJob_ForCompanyJob[jobRNG] = acceptedCount + 1;
            await LoadUploadedJobsAsync();
            await ApplyFiltersAndUpdateCountsForJobs();
        }

        private async Task ConfirmAndRejectJob(long jobRNG, string studentUniqueID)
        {
            bool isConfirmed = await JS.InvokeAsync<bool>("confirmActionWithHTML",
                "- ΑΠΟΡΡΙΨΗ ΑΙΤΗΣΗΣ - \n Η ενέργεια αυτή δεν θα μπορεί να αναιρεθεί. Είστε σίγουρος/η;");

            if (!isConfirmed) return;

            var result = await CompanyDashboardService.DecideOnApplicationAsync(new ApplicationDecisionRequest
            {
                ApplicationType = ApplicationType.Job,
                ApplicationRng = jobRNG,
                StudentUniqueId = studentUniqueID,
                Decision = ApplicationDecision.Reject
            });

            if (!result.Success)
            {
                await JS.InvokeVoidAsync("confirmActionWithHTML2", result.Error ?? "Δεν βρέθηκε η Αίτηση ή ο Φοιτητής.");
                return;
            }

            await LoadUploadedJobsAsync();
            await ApplyFiltersAndUpdateCountsForJobs();
        }

        // Job Applicants
        private async Task LoadJobApplicants(long positionRng)
        {
            loadingJobPositionId = positionRng;
            isLoadingJobApplicants = true;
            StateHasChanged();

            try
            {
                var applicants = await CompanyDashboardService.GetJobApplicationsAsync();
                var filteredApplicants = applicants
                    .Where(a => a.RNGForCompanyJobApplied == positionRng &&
                               a.CompanysEmailWhereStudentAppliedForCompanyJob != null &&
                               a.CompanysEmailWhereStudentAppliedForCompanyJob.Equals(CurrentUserEmail, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(a => a.DateTimeStudentAppliedForCompanyJob)
                    .ToList();

                jobApplicantsMap[positionRng] = filteredApplicants;

                var acceptedCount = filteredApplicants.Count(a => a.CompanyPositionStatusAppliedAtTheCompanySide == "Επιτυχής");
                acceptedApplicantsCountPerJob_ForCompanyJob[positionRng] = acceptedCount;

                var job = UploadedJobs.FirstOrDefault(j => j.RNGForPositionUploaded == positionRng);
                if (job != null)
                    availableSlotsPerJob_ForCompanyJob[positionRng] = job.OpenSlots - acceptedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading applicants: {ex.Message}");
            }
            finally
            {
                isLoadingJobApplicants = false;
                loadingJobPositionId = null;
                StateHasChanged();
            }
        }

        private void EnableBulkEditModeForApplicants(long positionRng)
        {
            isBulkEditModeForApplicants = true;
            selectedApplicantIds.Clear();
            StateHasChanged();
        }

        private void CancelBulkEditForApplicants()
        {
            isBulkEditModeForApplicants = false;
            selectedApplicantIds.Clear();
            pendingBulkActionForApplicants = "";
            StateHasChanged();
        }

        private async Task ExecuteBulkActionForApplicants()
        {
            if (string.IsNullOrEmpty(pendingBulkActionForApplicants) || !selectedApplicantIds.Any())
                return;

            var decision = pendingBulkActionForApplicants == "accept"
                ? ApplicationDecision.Accept
                : ApplicationDecision.Reject;

            foreach (var (rng, studentId) in selectedApplicantIds)
            {
                await CompanyDashboardService.DecideOnApplicationAsync(new ApplicationDecisionRequest
                {
                    ApplicationType = ApplicationType.Job,
                    ApplicationRng = rng,
                    StudentUniqueId = studentId,
                    Decision = decision
                });
            }
            CancelBulkEditForApplicants();
        }

        // Edit Job
        private void OpenEditJobModal(CompanyJob jobToEdit)
        {
            currentJob = new CompanyJob
            {
                Id = jobToEdit.Id,
                RNGForPositionUploaded = jobToEdit.RNGForPositionUploaded,
                PositionTitle = jobToEdit.PositionTitle,
                PositionDescription = jobToEdit.PositionDescription,
                PositionStatus = jobToEdit.PositionStatus,
                PositionActivePeriod = jobToEdit.PositionActivePeriod
            };
            isEditJobModalVisible = true;
        }

        private void CloseEditPopupForJobs()
        {
            isEditJobModalVisible = false;
            currentJob = null;
        }

        private async Task HandleFileUploadToEditCompanyJobAttachment(InputFileChangeEventArgs e)
        {
            if (currentJob == null || e.File == null) return;

            try
            {
                const long maxFileSize = 10485760;
                using var memoryStream = new System.IO.MemoryStream();
                await e.File.OpenReadStream(maxFileSize).CopyToAsync(memoryStream);
                currentJob.PositionAttachment = memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading attachment: {ex.Message}");
            }
        }

        private async Task SaveEditedJob()
        {
            if (currentJob == null) return;

            var jobToUpdate = UploadedJobs.FirstOrDefault(j => j.Id == currentJob.Id);
            if (jobToUpdate == null)
            {
                var data = await CompanyDashboardService.LoadDashboardDataAsync();
                jobToUpdate = data.Jobs.FirstOrDefault(j => j.Id == currentJob.Id);
            }
            if (jobToUpdate != null)
            {
                jobToUpdate.PositionTitle = currentJob.PositionTitle;
                jobToUpdate.PositionDescription = currentJob.PositionDescription;
                jobToUpdate.PositionActivePeriod = currentJob.PositionActivePeriod;
                if (currentJob.PositionAttachment != null)
                    jobToUpdate.PositionAttachment = currentJob.PositionAttachment;

                await CompanyDashboardService.CreateOrUpdateJobAsync(jobToUpdate);
            }

            CloseEditPopupForJobs();
            await LoadUploadedJobsAsync();
            await ApplyFiltersAndUpdateCountsForJobs();
        }

        // Slot Warning Modal
        private void CloseSlotWarningModal_ForCompanyJob()
        {
            showSlotWarningModal_ForCompanyJob = false;
            slotWarningMessage_ForCompanyJob = "";
            StateHasChanged();
        }

        // Additional Missing Properties
        private CompanyJob selectedJob;
        private CompanyJobApplied selectedJobApplication;
        private List<CompanyJob> jobs = new List<CompanyJob>();
        private bool showErrorMessage = false;
        private bool showCheckboxesForEditCompanyJob = false;
        private Dictionary<string, Student> studentDataCache = new Dictionary<string, Student>();
        private bool showErrorMessageforPostalCode = false;
        private bool showEmailConfirmationModalForApplicants = false;
        private Dictionary<string, List<string>> RegionToTownsMap = new Dictionary<string, List<string>>();
        private string PositionAttachmentErrorMessage = "";
        private bool isModalVisibleForJobs = false;
        private bool isEditPopupVisibleForJobs = false;
        private bool showCheckboxesForCompanyJob = false;
        private bool showLoadingModalForDeleteJob = false;

        // Methods
        private void ShowEmailConfirmationModalForApplicants(string action)
        {
            pendingBulkActionForApplicants = action;
            showEmailConfirmationModalForApplicants = true;
            StateHasChanged();
        }

        private void CloseEmailConfirmationModalForApplicants()
        {
            showEmailConfirmationModalForApplicants = false;
            StateHasChanged();
        }

        private void CloseModalForJobs()
        {
            isModalVisibleForJobs = false;
            selectedJob = null;
            StateHasChanged();
        }

        private void ToggleCheckboxesForEditCompanyJob()
        {
            showCheckboxesForEditCompanyJob = !showCheckboxesForEditCompanyJob;
            StateHasChanged();
        }

        private void ToggleSubFieldsForEditCompanyJob(int areaId)
        {
            if (ExpandedAreasForEditCompanyJob.Contains(areaId))
                ExpandedAreasForEditCompanyJob.Remove(areaId);
            else
                ExpandedAreasForEditCompanyJob.Add(areaId);
            StateHasChanged();
        }

        private void ToggleApplicantSelection(long jobRng, string studentId, ChangeEventArgs e)
        {
            var key = (jobRng, studentId);
            if ((bool)e.Value!)
                selectedApplicantIds.Add(key);
            else
                selectedApplicantIds.Remove(key);
            StateHasChanged();
        }

        private async Task ShowStudentDetailsInNameAsHyperlink(string studentUniqueId, int applicationId, string source)
        {
            var student = await CompanyDashboardService.GetStudentByUniqueIdAsync(studentUniqueId);
            if (student != null)
            {
                studentDataCache[student.Email] = student;
            }
            StateHasChanged();
        }
    }
}
