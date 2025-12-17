# Student Components Symbol Verification Report

## Summary
- **Total Symbols**: 340
- **✅ Found (exact camelCase properties/fields)**: 85
- **✅ Found (methods)**: 110
- **⚠️  Found (PascalCase - public properties)**: 0
- **🔧 Found (Func<> delegates)**: 0
- **❌ Missing**: 145

## Verified Symbols

### Exact Matches (camelCase Properties/Fields) - 85
These symbols exist in MainLayout.razor.cs with exact camelCase naming:
- `CompanySearchPerPageAsStudent`
- `ForeasType`
- `RegionToTownsMap`
- `Regions`
- `areasOfInterestSuggestionsAsStudent`
- `companyActivitySuggestionsAsStudent`
- `companyDesiredSkillsSuggestionsAsStudent`
- `companyNameSearchForThesesAsStudent`
- `companyNameSuggestionsAsStudent`
- `companyThesisApplications`
- `companyinternshipSearchByTown`
- `companyinternshipSearchByTransportOffer`
- `companyjobSearchByTransportOffer`
- `currentInternshipPage`
- `currentJobPage`
- `currentMonth`
- `currentPageForCompanyAnnouncements`
- `currentPageForInternshipsToSee`
- `currentPageForProfessorAnnouncements`
- `currentPageForResearchGroupPublicAnnouncements`
- `currentPageForThesisToSee`
- `currentPage_CompanySearchAsStudent`
- `currentThesisPage`
- `expandedAnnouncementId`
- `expandedProfessorAnnouncementId`
- `expandedResearchGroupPublicAnnouncementId`
- `globalCompanySearch`
- `globalInternshipSearch`
- `globalJobSearch`
- `globalThesisSearch`
- `internshipDataCache`
- `isAnnouncementsAsStudentVisible`
- `isCompanyAnnouncementsVisible`
- `isLoadingSearchJobApplicationsAsStudent`
- `isLoadingSearchThesisApplicationsAsStudent`
- `isLoadingStudentInternshipApplications`
- `isLoadingStudentJobApplications`
- `isLoadingStudentThesisApplications`
- `isModalVisibleToShowEventsOnCalendarForEachClickedDay`
- `isProfessorAnnouncementsVisible`
- `isResearchGroupPublicAnnouncementsVisible`
- `isStudentSearchCompanyFormVisible`
- `isSvseNewsVisible`
- `isUniversityNewsVisible`
- `jobDataCache`
- `loadingProgressWhenApplyForJobAsStudent`
- `loadingProgressWhenApplyForThesisAsStudent`
- `loadingProgressWhenWithdrawInternshipApplication`
- `loadingProgressWhenWithdrawJobApplication`
- `loadingProgressWhenWithdrawThesisApplication`
... and 35 more

### Methods - 110
These are methods (need to be wrapped as Func<> delegates or EventCallbacks):
- `ApplyForInternshipAsStudent`
- `ApplyForJobAsStudent`
- `ApplyForThesisAsStudent`
- `ClearSearchFieldsAsStudentToFindCompany`
- `CloseCompanyDetailsModalWhenSearchAsStudent`
- `CloseCompanyDetailsModal_StudentThesisApplications`
- `CloseCompanyThesisDetailsModal_StudentThesisApplications`
- `CloseEventDetails`
- `CloseModalForCompanyAndProfessorEventTitles`
- `CloseProfessorDetailsModal_StudentThesisApplications`
- `CloseProfessorThesisDetailsModal_StudentThesisApplications`
- `DownloadAnnouncementAttachmentFrontPage`
- `DownloadCompanyEventAttachmentFrontPage`
- `DownloadProfessorAnnouncementAttachmentFrontPage`
- `DownloadProfessorEventAttachmentFrontPage`
- `DownloadResearchGroupPublicAnnouncementAttachmentFrontPage`
- `FilterInternshipApplications`
- `FilterThesisApplications`
- `GetPaginatedCompanySearchResultsAsStudent`
- `GetVisibleInternshipPages`
- `GetVisiblePages`
- `GetVisiblePagesForCompanyAnnouncements`
- `GetVisiblePagesForInternships`
- `GetVisiblePagesForJobs`
- `GetVisiblePagesForProfessorAnnouncements`
- `GetVisiblePagesForResearchGroupPublicAnnouncements`
- `GetVisiblePages_CompanySearchAsStudent`
- `GoToFirstInternshipPage`
- `GoToFirstPage`
- `GoToFirstPageForCompanyAnnouncements`
... and 80 more

## Missing Symbols - 145

These symbols need to be created in MainLayout.razor.cs:

- `Announcements`
- `ClearInternshipSearchFiltersAsStudent`
- `ClearJobSearchFiltersAsStudent`
- `ClearThesisSearchFiltersAsStudent`
- `CloseCompanyDetailsModalFromEvents`
- `CloseCompanyDetailsModalFromInternship`
- `CloseCompanyDetailsModalFromJob`
- `CloseCompanyDetailsModalFromThesis`
- `CloseCompanyDetailsModal_StudentInternshipApplications`
- `CloseCompanyDetailsModal_StudentJobApplications`
- `CloseCompanyInternshipDetailsModal_StudentInternshipApplications`
- `CloseCompanyJobDetailsModal_StudentJobApplications`
- `CloseProfessorDetailsModalFromThesis`
- `CurrentUserEmail`
- `FilterJobApplications`
- `GetDayCellClass`
- `GetPaginatedInternshipResultsAsStudent`
- `GetPaginatedJobResultsAsStudent`
- `GetPaginatedThesisResultsAsStudent`
- `GetVisibleJobPages`
- `GetVisibleThesisPages`
- `GoToFirstJobPage`
- `GoToFirstThesisPage`
- `GoToJobPage`
- `GoToLastJobPage`
- `GoToLastThesisPage`
- `GoToThesisPage`
- `HandleCompanyNameInputForInternshipSearchAsStudent`
- `HandleCompanyNameInputForJobSearchAsStudent`
- `HandleCompanyNameInputForThesisSearchAsStudent`
- `IsThesisAreaSelected`
- `IsThesisSkillSelected`
- `IsThesisSubFieldSelected`
- `NextJobPage`
- `NextPageForJobsToSee`
- `NextThesisPage`
- `OnInternshipPageSizeChange`
- `OnJobPageSizeChange`
- `OnPageSizeChangeForApplications_SeeMyInternshipApplicationsAsStudent`
- `OnPageSizeChangeForApplications_SeeMyJobApplicationsAsStudent`
- `OnThesisPageSizeChange`
- `OnThesisSkillCheckboxChanged`
- `PreviousJobPage`
- `PreviousPageForJobsToSee`
- `PreviousThesisPage`
- `ProfessorAnnouncements`
- `ResearchGroupAnnouncements`
- `SearchJobsAsStudent`
- `SearchThesisAsStudent`
- `SelectCompanyDesiredSkillsSuggestionAsStudent`
- `SelectCompanyNameSuggestionWhenSearchForInternshipsAsStudent`
- `SelectCompanyNameSuggestionWhenSearchForJobsAsStudent`
- `SelectCompanyNameSuggestionWhenSearchForThesisAsStudent`
- `SetTotalJobCount`
- `ShowCompanyDetailsInInternshipCompanyName_StudentInternshipApplications`
- `ShowCompanyDetailsInJobCompanyName_StudentJobApplications`
- `ShowCompanyDetailsOnHyperlinkAsStudentForCompanyEvents`
- `ShowCompanyHyperlinkNameDetailsModalInStudentInternships`
- `ShowCompanyHyperlinkNameDetailsModalInStudentJobs`
- `ShowCompanyInternshipDetailsModal_StudentInternshipApplications`
- `ShowCompanyJobDetailsModal_StudentJobApplications`
- `ShowInterestInCompanyEventAsStudent`
- `ShowInterestInProfessorEventAsStudent`
- `ToggleAreasForThesisSearchAsStudent`
- `ToggleSearchJobsAsStudentFiltersVisibility`
- `ToggleSkillsForThesisSearchAsStudent`
- `ToggleSubFieldsForThesisSearchAsStudent`
- `WithdrawInternshipApplication`
- `WithdrawJobApplication`
- `availableAreasForThesisSearchAsStudent`
- `availableSkillsForThesisSearchAsStudent`
- `companyEventDetails`
- `companyEventsForSelectedDate`
- `companyInternshipApplications`
- `companyJobApplications`
- `companyNameAutocompleteSuggestionsWhenSearchForCompanyInternshipsAsStudent`
- `companyNameAutocompleteSuggestionsWhenSearchForCompanyJobsAsStudent`
- `companyNameSearch`
- `companyNameSuggestionsWhenSearchForProfessorThesisAutocompleteNameAsStudent`
- `companySearchPageSizeOptionsAsStudent`
- `companyinternshipSearch`
- `companyinternshipSearchByESPA`
- `companyinternshipSearchByRegion`
- `companyinternshipSearchByType`
- `companyjobSearchByRegion`
- `companyjobSearchByTown`
- `companyjobSearchByType`
- `currentCompanyDetailsToShowOnHyperlinkAsStudentForCompanyEvents`
- `currentPageForJobsToSee`
- `internshipPageSizeOptions`
- `internshipPerPage`
- `internshipSearchResultsAsStudent`
- `isExpandedModalVisibleToSeeCompanyDetailsAsStudent`
- `isLoadingSearchInternshipApplicationsAsStudent`
- `isSearchInternshipFiltersVisibleAsStudent`
- `isSearchJobFiltersVisibleAsStudent`
- `isSearchThesisFiltersVisibleAsStudent`
- `jobPageSizeOptions`
- `jobPerPage`
- `jobSearchResultsAsStudent`
- `loadingProgressWhenApplyForInternshipAsStudent`
- `pageSizeForJobsToSee`
- `pageSizeOptions_SeeMyInternshipApplicationsAsStudent`
- `pageSizeOptions_SeeMyJobApplicationsAsStudent`
- `pageSizeOptions_SeeMyThesisApplicationsAsStudent`
- `professorEventDetails`
- `professorEventsForSelectedDate`
- `selectedCompanyDetailsFromInternship`
- `selectedCompanyDetailsFromJob`
- `selectedCompanyDetailsFromThesis`
- `selectedCompanyDetails_StudentInternshipApplications`
- `selectedCompanyDetails_StudentJobApplications`
- `selectedCompanyDetails_StudentThesisApplications`
- `selectedCompanyInternshipDetails_StudentInternshipApplications`
- `selectedCompanyJobDetails_StudentJobApplications`
- `selectedCompanyThesisDetails_StudentThesisApplications`
- `selectedEventFilter`
- `selectedProfessorDetailsFromThesis`
- `selectedProfessorDetails_StudentThesisApplications`
- `selectedProfessorThesisDetails_StudentThesisApplications`
- `showAreasForThesisSearchAsStudent`
- `showCompanyDetailsModalFromEvents`
- `showCompanyDetailsModalFromInternship`
- `showCompanyDetailsModalFromJob`
- `showCompanyDetailsModalFromThesis`
- `showCompanyDetailsModal_StudentInternshipApplications`
- `showCompanyDetailsModal_StudentJobApplications`
- `showCompanyDetailsModal_StudentThesisApplications`
- `showCompanyInternshipDetailsModal_StudentInternshipApplications`
- `showCompanyJobDetailsModal_StudentJobApplications`
- `showCompanyThesisDetailsModal_StudentThesisApplications`
- `showLoadingModalWhenApplyForInternshipAsStudent`
- `showProfessorDetailsModalFromThesis`
- `showProfessorDetailsModal_StudentThesisApplications`
- `showProfessorThesisDetailsModal_StudentThesisApplications`
- `showSkillsForThesisSearchAsStudent`
- `showStudentJobApplications`
- `showStudentThesisApplications`
- `showSubFieldsForThesisSearchAsStudent`
- `thesisPageSizeOptions`
- `thesisPerPage`
- `thesisSearchResultsAsStudent`
- `totalJobPages`
- `totalPagesForJobsToSee`
- `totalThesisPages`

## Notes

1. **Methods**: Methods found need to be wrapped as Func<> delegates or EventCallbacks.
2. **PascalCase Properties**: Public properties in C# use PascalCase, which is correct for component binding.
3. **Missing Symbols**: These will need to be created in MainLayout.razor.cs.
4. **Casing**: All camelCase symbols in component markup should match camelCase in MainLayout.razor.cs.

## Recommendations

1. For methods that need to be Func<> delegates, create wrapper properties:
   ```csharp
   private Func<IEnumerable<Company>> GetPaginatedCompanySearchResultsAsStudent => () => {
       return GetPaginatedCompanySearchResultsAsStudent();
   };
   ```

2. Review missing symbols and determine if they need to be created or exist with different names.

3. Test compilation once project is available to identify any remaining issues.