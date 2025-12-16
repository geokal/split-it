# Wiring Plan: CompanyAnnouncementsManagementSection.razor

**Component**: `Shared/Company/CompanyAnnouncementsManagementSection.razor`  
**Lines**: 629  
**Status**: Pending Wiring

---

## Component Overview

This component handles:
1. **Viewing Recent Announcements** (University, SVSE, Company, Professor, Research Group)
2. **Creating New Announcements** (Form with validation, file upload, save options)

---

## Dependencies Analysis

### 1. Main Form Visibility Toggle
- `isAnnouncementsFormVisibleToShowGeneralAnnouncementsAndEventsAsCompany` (bool)
- `ToggleFormVisibilityToShowGeneralAnnouncementsAndEventsAsCompany` (EventCallback)

### 2. University News Section
- `isUniversityNewsVisible` (bool)
- `ToggleUniversityNewsVisibility` (EventCallback)
- `newsArticles` (IEnumerable<NewsArticle>)

### 3. SVSE News Section
- `isSvseNewsVisible` (bool)
- `ToggleSvseNewsVisibility` (EventCallback)
- `svseNewsArticles` (IEnumerable<NewsArticle>)

### 4. Company Announcements Section
- `isCompanyAnnouncementsVisible` (bool)
- `ToggleCompanyAnnouncementsVisibility` (EventCallback)
- `Announcements` (IEnumerable<CompanyAnnouncement>)
- `expandedAnnouncementId` (int?)
- `ToggleDescription(int id)` (EventCallback<int>)
- `DownloadAnnouncementAttachmentFrontPage(byte[] file, string title)` (EventCallback<byte[], string>)
- `currentPageForCompanyAnnouncements` (int)
- `pageSize` (int)
- `totalPagesForCompanyAnnouncements` (int)
- `GoToFirstPageForCompanyAnnouncements` (EventCallback)
- `PreviousPageForCompanyAnnouncements` (EventCallback)
- `GoToPageForCompanyAnnouncements(int page)` (EventCallback<int>)
- `NextPageForCompanyAnnouncements` (EventCallback)
- `GoToLastPageForCompanyAnnouncements` (EventCallback)
- `GetVisiblePagesForCompanyAnnouncements()` (Func<List<int>>)

### 5. Professor Announcements Section
- `isProfessorAnnouncementsVisible` (bool)
- `ToggleProfessorAnnouncementsVisibility` (EventCallback)
- `ProfessorAnnouncements` (IEnumerable<ProfessorAnnouncement>)
- `expandedProfessorAnnouncementId` (int?)
- `ToggleDescriptionForProfessorAnnouncements(int id)` (EventCallback<int>)
- `DownloadProfessorAnnouncementAttachmentFrontPage(byte[] file, string title)` (EventCallback<byte[], string>)
- `currentPageForProfessorAnnouncements` (int)
- `totalPagesForProfessorAnnouncements` (int)
- `GoToFirstPageForProfessorAnnouncements` (EventCallback)
- `PreviousPageForProfessorAnnouncements` (EventCallback)
- `GoToPageForProfessorAnnouncements(int page)` (EventCallback<int>)
- `NextPageForProfessorAnnouncements` (EventCallback)
- `GoToLastPageForProfessorAnnouncements` (EventCallback)
- `GetVisiblePagesForProfessorAnnouncements()` (Func<List<int>>)

### 6. Research Group Announcements Section
- `isResearchGroupPublicAnnouncementsVisible` (bool)
- `ToggleResearchGroupPublicAnnouncementsVisibility` (EventCallback)
- `ResearchGroupAnnouncements` (IEnumerable<ResearchGroupAnnouncement>)
- `expandedResearchGroupPublicAnnouncementId` (int?)
- `ToggleDescriptionForResearchGroupPublicAnnouncements(int id)` (EventCallback<int>)
- `DownloadResearchGroupPublicAnnouncementAttachmentFrontPage(byte[] file, string title)` (EventCallback<byte[], string>)
- `currentPageForResearchGroupPublicAnnouncements` (int)
- `totalPagesForResearchGroupPublicAnnouncements` (int)
- `GoToFirstPageForResearchGroupPublicAnnouncements` (EventCallback)
- `PreviousPageForResearchGroupPublicAnnouncements` (EventCallback)
- `GoToPageForResearchGroupPublicAnnouncements(int page)` (EventCallback<int>)
- `NextPageForResearchGroupPublicAnnouncements` (EventCallback)
- `GoToLastPageForResearchGroupPublicAnnouncements` (EventCallback)
- `GetVisiblePagesForResearchGroupPublicAnnouncements()` (Func<List<int>>)

### 7. Create Announcement Form
- `isAnnouncementsFormVisible` (bool)
- `ToggleFormVisibilityForUploadCompanyAnnouncements` (EventCallback)
- `announcement` (CompanyAnnouncement object)
- `showErrorMessageforUploadingAnnouncementAsCompany` (bool)
- `remainingCharactersInAnnouncementFieldUploadAsCompany` (int)
- `remainingCharactersInAnnouncementDescriptionUploadAsCompany` (int)
- `CheckCharacterLimitInAnnouncementFieldUploadAsCompany` (EventCallback)
- `CheckCharacterLimitInAnnouncementDescriptionUploadAsCompany` (EventCallback)
- `AnnouncementAttachmentErrorMessage` (string)
- `HandleFileSelectedForAnnouncementAttachment` (EventCallback<InputFileChangeEventArgs>)
- `showLoadingModal` (bool)
- `loadingProgress` (int)
- `isFormValidToSaveAnnouncementAsCompany` (bool)
- `saveAnnouncementAsCompanyMessage` (string)
- `isSaveAnnouncementAsCompanySuccessful` (bool)
- `SaveAnnouncementAsUnpublished` (EventCallback)
- `SaveAnnouncementAsPublished` (EventCallback)

---

## Wiring Steps

1. **Verify all symbols exist in MainLayout.razor.cs** using Serena's `find_symbol`
2. **Create @code section** at end of component (before any leftover markup)
3. **Add [Parameter] declarations** for all properties/fields
4. **Add EventCallback parameters** for all methods
5. **Add Func<T> parameters** for computed properties
6. **Update markup** to use parameters (ensure camelCase)
7. **Remove leftover markup** if any
8. **Verify structure** - component should end with `</div>` then `@code { ... }`
9. **Run verification** using Serena tools per AGENTS.md guidelines

---

## Estimated Parameters: ~60-70 parameters

This is a large component with many dependencies. We'll wire it systematically following the established pattern from CompanyAnnouncementsSection.razor.

