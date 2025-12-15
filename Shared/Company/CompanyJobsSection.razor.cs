namespace split_it.Shared.Company
{
    public partial class CompanyJobsSection
    {
        private bool isForm1Visible = false;

        private void ToggleFormVisibilityForUploadCompanyJobs()
        {
            isForm1Visible = !isForm1Visible;
            StateHasChanged();
        }
    }
}
