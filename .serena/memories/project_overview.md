# Split-It Project - .NET 8 Components Structure

## Status: Restructured ✅

## Structure
```
Components/
├── Layout/
│   ├── MainLayout.razor + .cs + .css
│   ├── NavMenu.razor + .css
│   ├── AccessControl.razor
│   ├── Student/StudentSection.razor + subcomponents (6)
│   ├── Company/CompanySection.razor + subcomponents (9)
│   ├── Professor/ProfessorSection.razor + subcomponents (7)
│   ├── ResearchGroup/ResearchGroupSection.razor + subcomponents (5)
│   └── Admin/AdminSection.razor
├── Helpers/
└── App.razor
```

## Namespaces
- `QuizManager.Components.Layout`
- `QuizManager.Components.Layout.[Role]`
- `QuizManager.Components.Helpers`

## Key Files
- `_Imports.razor` - global namespace imports
- `Components/Layout/MainLayout.razor` - main layout using `<StudentSection />` etc.
