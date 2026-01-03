# Git Workflow Guide - Maintaining Linear History

## Problem

When working from multiple machines, merge commits can create a messy, non-linear git history that looks like this:

```
*   c5f231a Merge branch 'feature/...' (BAD)
|\  
| * 0142183 commit from machine A
* | a2c4d7e commit from machine B
|\| 
| * df9b375 another commit
* | 7cb53e7 yet another commit
```

## Solution: Always Rebase, Never Merge

### 1. Configure Git to Always Rebase

Run this **on every machine** you work from:

```bash
# For this repository only
git config pull.rebase true

# Or globally for all repositories
git config --global pull.rebase true
```

This ensures `git pull` automatically rebases instead of merging.

### 2. Daily Workflow

#### Before Starting Work

```bash
# Fetch latest changes
git fetch origin

# Rebase your local branch on top of remote
git pull --rebase origin feature/migration/routes-host
```

#### After Making Commits

```bash
# Fetch latest changes (in case someone else pushed)
git fetch origin

# Rebase your commits on top of remote
git pull --rebase origin feature/migration/routes-host

# Push your changes
git push origin feature/migration/routes-host
```

### 3. If You Accidentally Created Merge Commits

If you already have merge commits, you need to clean up the history:

```bash
# Fetch latest
git fetch origin

# Create a clean linear history (interactive rebase)
git rebase -i <base-commit>

# In the editor that opens:
# - Keep all "pick" lines for real commits
# - DELETE all lines that say "Merge branch..."
# - Save and close

# Force push the cleaned history
git push --force-with-lease origin feature/migration/routes-host
```

### 4. Working from Multiple Machines

**Machine A:**
```bash
# Make changes
git add .
git commit -m "feat: add feature X"

# Before pushing, always rebase
git pull --rebase origin feature/migration/routes-host
git push origin feature/migration/routes-host
```

**Machine B (later):**
```bash
# ALWAYS pull with rebase before starting work
git pull --rebase origin feature/migration/routes-host

# Make changes
git add .
git commit -m "feat: add feature Y"

# Before pushing, rebase again
git pull --rebase origin feature/migration/routes-host
git push origin feature/migration/routes-host
```

## Why This Matters

### Bad History (with merges):
```
*   Merge branch
|\  
| * commit A
* | commit B
|\| 
| * commit C
* | commit D
```
- Hard to read
- Difficult to bisect
- Confusing for code review
- Makes cherry-picking harder

### Good History (linear):
```
* commit D
* commit C
* commit B
* commit A
```
- Easy to read
- Simple to bisect
- Clear for code review
- Easy to cherry-pick

## Common Scenarios

### Scenario 1: You Forgot to Pull Before Committing

```bash
# You made commits locally
git log --oneline -3
# abc123 my new commit
# def456 previous commit

# Try to push
git push origin feature/migration/routes-host
# ERROR: rejected (non-fast-forward)

# CORRECT way to fix:
git pull --rebase origin feature/migration/routes-host
git push origin feature/migration/routes-host
```

### Scenario 2: Rebase Conflicts

```bash
git pull --rebase origin feature/migration/routes-host
# CONFLICT (content): Merge conflict in file.txt

# Fix the conflict in your editor
# Then:
git add file.txt
git rebase --continue

# If you want to abort:
git rebase --abort
```

### Scenario 3: You Already Pushed Merge Commits

```bash
# History is messy with merges
# Clean it up:

# Find the base commit (before the mess started)
git log --oneline --graph -20
# Identify the last good commit, e.g., 91cb3cf

# Interactive rebase
git rebase -i 91cb3cf

# In editor: remove merge commit lines, keep real commits
# Save and close

# Force push (ONLY on feature branches, NEVER on main!)
git push --force-with-lease origin feature/migration/routes-host
```

## Automated Script

Save this as `git-clean-history.sh`:

```bash
#!/bin/bash
set -e

BRANCH=$(git branch --show-current)
BASE_COMMIT="$1"

if [ -z "$BASE_COMMIT" ]; then
    echo "Usage: ./git-clean-history.sh <base-commit>"
    echo "Example: ./git-clean-history.sh 91cb3cf"
    exit 1
fi

echo "Creating clean linear history from $BASE_COMMIT..."

# Get all non-merge commits
COMMITS=$(git log --oneline --no-merges $BASE_COMMIT..HEAD | awk '{print $1}' | tac)

# Checkout base
git checkout $BASE_COMMIT

# Create temp branch
git checkout -b temp-clean-history

# Cherry-pick all commits
for commit in $COMMITS; do
    echo "Cherry-picking $commit..."
    git cherry-pick $commit || {
        echo "Conflict! Fix it and run: git cherry-pick --continue"
        exit 1
    }
done

# Replace original branch
git branch -D $BRANCH
git checkout -b $BRANCH

# Clean up
git branch -D temp-clean-history

echo "Done! Review with: git log --oneline --graph -20"
echo "Push with: git push --force-with-lease origin $BRANCH"
```

## Best Practices

1. ✅ **Always** `git pull --rebase` before starting work
2. ✅ **Always** `git pull --rebase` before pushing
3. ✅ Configure `git config pull.rebase true` on all machines
4. ✅ Use `--force-with-lease` instead of `--force` (safer)
5. ❌ **Never** use `git pull` without `--rebase` on feature branches
6. ❌ **Never** use `--force` on shared branches like `main` or `develop`

## Quick Reference

```bash
# Setup (once per machine)
git config pull.rebase true

# Daily workflow
git pull --rebase origin <branch>  # Before work
git add .
git commit -m "message"
git pull --rebase origin <branch>  # Before push
git push origin <branch>

# Fix messy history
git rebase -i <base-commit>        # Remove merge commits
git push --force-with-lease origin <branch>

# Check history
git log --oneline --graph -20      # Visualize
```

## When to Use Force Push

✅ **Safe to force push:**
- Feature branches (like `feature/migration/routes-host`)
- Your personal branches
- After cleaning up history with rebase

❌ **NEVER force push:**
- `main` or `master` branch
- `develop` branch
- Any branch others are actively working on
- Public/shared branches

## Summary

**Golden Rule:** Always rebase, never merge on feature branches.

This keeps your git history clean, linear, and easy to understand.
