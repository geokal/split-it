namespace split_it.Shared.Company
{
    public partial class CompanyUploadedJobsSection
    {
        [Parameter] public List<CompanyJobDto> Jobs { get; set; }
        [Parameter] public bool IsLoadingJobsHistory { get; set; }
        [Parameter] public bool IsForm2Visible { get; set; }
        [Parameter] public EventCallback ToggleFormVisibilityToShowMyActiveJobsAsCompany { get; set; }
        [Parameter] public string SelectedStatusFilterForJobs { get; set; }
        [Parameter] public EventCallback<ChangeEventArgs> OnStatusFilterChangeForJobs { get; set; }
        [Parameter] public int JobsPerPage { get; set; }
        [Parameter] public EventCallback<ChangeEventArgs> OnPageSizeChange_SeeMyUploadedJobsAsCompany { get; set; }
        [Parameter] public List<int> PageSizeOptions_SeeMyUploadedJobsAsCompany { get; set; }
        [Parameter] public int TotalCountJobs { get; set; }
        [Parameter] public int PublishedCountJobs { get; set; }
        [Parameter] public int UnpublishedCountJobs { get; set; }
        [Parameter] public int WithdrawnCountJobs { get; set; }
        [Parameter] public bool IsBulkEditMode { get; set; }
        [Parameter] public EventCallback EnableBulkEditMode { get; set; }
        [Parameter] public EventCallback<string> ExecuteBulkStatusChangeForJobs { get; set; }
        [Parameter] public EventCallback ExecuteBulkCopyForJobs { get; set; }
        [Parameter] public EventCallback CancelBulkEdit { get; set; }
        [Parameter] public HashSet<int> SelectedJobIds { get; set; }
        [Parameter] public EventCallback<int> ToggleJobSelection { get; set; }
        [Parameter] public Func<IEnumerable<CompanyJobDto>> GetPaginatedJobs { get; set; }
        [Parameter] public EventCallback<int> ToggleJobMenu { get; set; }
        [Parameter] public int ActiveJobMenuId { get; set; }
        [Parameter] public EventCallback<int> DeleteJobPosition { get; set; }
        [Parameter] public EventCallback<CompanyJobDto> ShowJobDetails { get; set; }
        [Parameter] public EventCallback<int> DownloadAttachmentForCompanyJobs { get; set; }
        [Parameter] public EventCallback<string> ToggleJobsExpanded { get; set; } // RNG for position uploaded
        [Parameter] public string CurrentlyExpandedJobId { get; set; }
        [Parameter] public Dictionary<string, List<JobApplicationDto>> JobApplicantsMap { get; set; }
        [Parameter] public bool IsLoadingJobApplicants { get; set; }
        [Parameter] public string LoadingJobPositionId { get; set; }
        [Parameter] public bool IsBulkEditModeForApplicants { get; set; }
        [Parameter] public EventCallback<string> EnableBulkEditModeForApplicants { get; set; }
        [Parameter] public EventCallback<string> ShowEmailConfirmationModalForApplicants { get; set; }
        [Parameter] public EventCallback CancelBulkEditForApplicants { get; set; }
        [Parameter] public HashSet<(string, string)> SelectedApplicantIds { get; set; }
        [Parameter] public Dictionary<string, int> AcceptedApplicantsCountPerJob_ForCompanyJob { get; set; }
        [Parameter] public Dictionary<string, int> AvailableSlotsPerJob_ForCompanyJob { get; set; }
        [Parameter] public EventCallback<(string, string, ChangeEventArgs)> ToggleApplicantSelection { get; set; }
        [Parameter] public Dictionary<string, StudentDetailsDto> StudentDataCache { get; set; }
        [Parameter] public EventCallback<(string, string, string)> ShowStudentDetailsInNameAsHyperlink { get; set; }
        [Parameter] public EventCallback<(string, string)> ConfirmAndAcceptJob { get; set; }
        [Parameter] public EventCallback<(string, string)> ConfirmAndRejectJob { get; set; }
        [Parameter] public bool ShowLoadingModalForDeleteJob { get; set; }
        [Parameter] public int LoadingProgress { get; set; }
        [Parameter] public EventCallback<CompanyJobDto> EditJobDetails { get; set; }
        [Parameter] public EventCallback<(int, string)> UpdateJobStatusAsCompany { get; set; }
        [Parameter] public EventCallback<int> ChangeJobStatusToUnpublished { get; set; }
        [Parameter] public int CurrentPageForJobs { get; set; }
        [Parameter] public int TotalPagesForJobs { get; set; }
        [Parameter] public Func<IEnumerable<int>> GetVisiblePagesForJobs { get; set; }
        [Parameter] public EventCallback GoToFirstPageForJobs { get; set; }
        [Parameter] public EventCallback PreviousPageForJobs { get; set; }
        [Parameter] public EventCallback NextPageForJobs { get; set; }
        [Parameter] public EventCallback GoToLastPageForJobs { get; set; }
        [Parameter] public EventCallback<int> GoToPageForJobs { get; set; }
        [Parameter] public bool IsModalVisibleForJobs { get; set; }
        [Parameter] public CompanyJobDto CurrentJob { get; set; }
        [Parameter] public EventCallback CloseModalForJobs { get; set; }
        [Parameter] public bool IsEditPopupVisibleForJobs { get; set; }
        [Parameter] public CompanyJobDto SelectedJob { get; set; }
        [Parameter] public EventCallback CloseEditPopupForJobs { get; set; }
        [Parameter] public List<string> ForeasType { get; set; }
        [Parameter] public bool ShowErrorMessage { get; set; }
        [Parameter] public List<string> Regions { get; set; }
        [Parameter] public Dictionary<string, List<string>> RegionToTownsMap { get; set; }
        [Parameter] public List<Area> Areas { get; set; } // Assuming Area is a defined model
        [Parameter] public bool ShowCheckboxesForEditCompanyJob { get; set; }
        [Parameter] public EventCallback ToggleCheckboxesForEditCompanyJob { get; set; }
        [Parameter] public EventCallback<ChangeEventArgs, Area> OnAreaCheckedChangedForEditCompanyJob { get; set; }
        [Parameter] public Func<Area, bool> IsAreaSelectedForEditCompanyJob { get; set; }
        [Parameter] public EventCallback<Area> ToggleSubFieldsForEditCompanyJob { get; set; }
        [Parameter] public HashSet<int> ExpandedAreasForEditCompanyJob { get; set; }
        [Parameter] public EventCallback<ChangeEventArgs, Area, string> OnSubFieldCheckedChangedForEditCompanyJob { get; set; }
        [Parameter] public Func<Area, string, bool> IsSubFieldSelectedForEditCompanyJob { get; set; }
        [Parameter] public EventCallback<InputFileChangeEventArgs> HandleFileUploadToEditCompanyJobAttachment { get; set; }
        [Parameter] public EventCallback SaveEditedJob { get; set; }
        [Parameter] public bool ShowSlotWarningModal_ForCompanyJob { get; set; }
        [Parameter] public EventCallback CloseSlotWarningModal_ForCompanyJob { get; set; }
        [Parameter] public string SlotWarningMessage_ForCompanyJob { get; set; }
        [Parameter] public bool ShowBulkActionModal { get; set; }
        [Parameter] public EventCallback CloseBulkActionModal { get; set; }
        [Parameter] public string BulkAction { get; set; }
        [Parameter] public string NewStatusForBulkAction { get; set; }
        [Parameter] public EventCallback ExecuteBulkAction { get; set; }
        [Parameter] public bool ShowEmailConfirmationModalForApplicants { get; set; }
        [Parameter] public EventCallback CloseEmailConfirmationModalForApplicants { get; set; }
        [Parameter] public string PendingBulkActionForApplicants { get; set; }
        [Parameter] public bool SendEmailsForBulkAction { get; set; }
        [Parameter] public EventCallback<bool> SetSendEmailsForBulkAction { get; set; }
        [Parameter] public EventCallback ExecuteBulkActionForApplicants { get; set; }

    }
}