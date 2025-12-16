# Suggested Commands for Development

## File Operations
- `ls -la` - List files with details
- `cd <directory>` - Change directory
- `grep -n "pattern" file` - Search with line numbers
- `rg "pattern"` - Fast search (ripgrep)
- `sed -n 'start,end p' file` - Extract lines (preserves exact content)
- `wc -l file` - Count lines
- `md5 file` - Generate checksum (macOS)
- `diff file1 file2` - Compare files

## Git Operations
- `git status` - Check status
- `git branch` - List branches
- `git checkout -b <branch>` - Create and switch to branch
- `git add <file>` - Stage file
- `git commit -m "message"` - Commit changes
- `git diff` - View changes

## File Verification
- `grep -n "id=\".*-start\"\|id=\".*-end\"" MainLayout.razor` - Find anchor divs
- `grep -n "tab-pane" Company.razor` - Find tab sections
- `sed -n 'start,end p' source.razor > component.razor` - Extract component

## Component Extraction
- Use anchor divs (`<div id="...-start"></div>`) for boundaries
- Extract using `sed -n 'start,end p'` to preserve exact markup
- Verify with MD5 checksums after extraction

## Code Analysis
- `grep -n "@bind\|@onclick\|@onchange" component.razor` - Find bindings/events
- `grep -n "\[Parameter\]" component.razor` - Find parameters
- `grep -n "UserRole\|IsLoading\|CurrentPage" MainLayout.razor.cs` - Find properties

## Testing
- No automated tests currently
- Manual verification of component boundaries
- Manual verification of parameter wiring

## Build/Run
- This is a .NET 6 Blazor Server application
- Standard .NET commands apply (if project file exists):
  - `dotnet build` - Build project
  - `dotnet run` - Run application
  - `dotnet test` - Run tests (if any)
