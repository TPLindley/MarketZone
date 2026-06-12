# Feature: Unified Add Special Dialog

## Overview
The Add Special dialog combines library selection, text entry, and visual color picking in a single streamlined interface, making it faster and easier to add new specials to the display.

## User Experience

### 1. Dialog Layout
When the user taps the **Add** button, a modal dialog appears with three main sections:

1. **Library Picker** (Dropdown)
   - Shows all previously saved specials from the library
   - Selecting an item from the dropdown automatically fills the text entry field
   - Also selects the matching color circle if available

2. **Text Entry Field**
   - Allows manual text input
   - Pre-filled when a library item is selected
   - Can be edited even after selecting from library

3. **Visual Color Picker**
   - Grid of colored circles (White, Pink, Red, Orange, Yellow, Lime, Cyan, Blue, Purple, Magenta)
   - Currently selected color has a pink border
   - Tap any circle to select that color
   - White is selected by default

### 2. Workflow

**Option A: Create New Special**
1. Type text directly into the text entry field
2. Choose a color by tapping a circle
3. Tap **Add**

**Option B: Use Library Item**
1. Pick an item from the dropdown
2. Text and color are auto-filled
3. Optionally modify the text or color
4. Tap **Add**

**Cancel**
- Tap **Cancel** to dismiss without adding

### 3. Implementation Details

**Files Created:**
- `mzConfig/Views/AddSpecialDialog.xaml` - Combined dialog UI
- `mzConfig/Views/AddSpecialDialog.xaml.cs` - Dialog code-behind with library loading and color selection

**ViewModel Changes:**
- `MainViewModel.AddSpecial()` - Updated to show the new unified dialog
- Removed obsolete `AddFromLibrary()` and `CreateNewSpecial()` methods
- `PickColor()` retained for editing existing specials

**Features:**
- Dropdown populated from `SpecialsLibraryService`
- Selecting library item fills text entry and selects matching color
- Color circles dynamically created from a color dictionary
- Pink border highlights the selected color
- Duplicate detection prevents adding items already in the list
- Successful additions are automatically saved to the library

**Color Selection:**
- Frame-based circles with tap gesture recognizers
- Selected color stored as hex value and friendly name
- Previous selection border reset when new color tapped

## Benefits
- **Faster workflow**: Single dialog instead of multiple prompts
- **Library reuse**: Easy to pick existing specials and customize
- **Visual feedback**: See colors while choosing
- **Flexibility**: Can type new or pick from library in the same flow
- **Consistent UX**: Matches the visual color picker design pattern

## Future Enhancements
- Search/filter for large libraries
- Recently used specials at the top
- Custom color picker for beyond the preset palette
- Edit mode for existing specials using the same dialog
