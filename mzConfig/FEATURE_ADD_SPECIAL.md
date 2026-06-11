# Enhanced Add Special Feature

## Overview
The "Add" button now provides a dialog-driven workflow that allows users to either pick from previously used specials (library) or create a new one with an integrated color picker.

## User Flow

### 1. **Initial Choice Dialog**
When the user taps the "Add" button, they see:
```
┌─────────────────────┐
│   Add Special       │
├─────────────────────┤
│ Pick from Library   │
│ Create New          │
│ Cancel              │
└─────────────────────┘
```

### 2a. **Pick from Library** Path
- Shows a list of all previously created specials
- Each entry displays the special's text
- Tapping an item adds it to the current list with its saved color
- Duplicates are prevented (case-insensitive comparison)
- If library is empty, shows helpful message

**Example:**
```
┌────────────────────────────┐
│ Select from Library        │
├────────────────────────────┤
│ Chocolate Chip Cookies     │
│ Apple Pie                  │
│ Fresh Bread                │
│ Cinnamon Rolls             │
│ Cancel                     │
└────────────────────────────┘
```

### 2b. **Create New** Path
**Step 1: Enter Text**
```
┌────────────────────────────┐
│        New Special         │
├────────────────────────────┤
│ Enter special text:        │
│ ┌────────────────────────┐ │
│ │ Chocolate Chip Cookies │ │
│ └────────────────────────┘ │
│                            │
│      [Next]  [Cancel]      │
└────────────────────────────┘
```

**Step 2: Select Color**
```
┌─────────────────────┐
│   Select Color      │
├─────────────────────┤
│ White               │
│ Pink                │
│ Red                 │
│ Orange              │
│ Yellow              │
│ Lime                │
│ Cyan                │
│ Blue                │
│ Purple              │
│ Magenta             │
│ Cancel              │
└─────────────────────┘
```

**Step 3: Result**
- Special is added to the current list
- Automatically saved to the library for future use
- Status message confirms the addition

## Technical Implementation

### Updated Files

**`mzConfig\ViewModels\MainViewModel.cs`**

Three new methods replace the simple `AddSpecial()`:

1. **`AddSpecial()`** - Entry point that shows the choice dialog
2. **`AddFromLibrary()`** - Loads and displays library specials
3. **`CreateNewSpecial()`** - Two-step wizard (text → color)

### Color Picker

Available colors (matching existing UI):
- White (#FFFFFF)
- Pink (#FF1595)
- Red (#FF0000)
- Orange (#FF8C00)
- Yellow (#FFFF00)
- Lime (#00FF00)
- Cyan (#00FFFF)
- Blue (#0000FF)
- Purple (#800080)
- Magenta (#FF00FF)

### Library Integration

- **Automatic Save**: New specials are automatically added to the library
- **Duplicate Prevention**: 
  - In the current list (case-insensitive text comparison)
  - In the library (handled by `SpecialsLibraryService`)
- **Persistent Storage**: Library is saved to `specials_library.json` in app data directory

## Benefits

1. **Quick Reuse**: Frequently used specials can be added in 2 taps
2. **Consistency**: Previously used colors/text are preserved
3. **Efficiency**: No need to re-type common items
4. **User-Friendly**: Clear step-by-step process for new items
5. **No Duplicates**: Automatic prevention of duplicate entries

## Edge Cases Handled

- ✅ Empty library (shows helpful message)
- ✅ Duplicate prevention (notifies user)
- ✅ Canceling at any step (safe exit)
- ✅ Invalid text input (empty/whitespace validation)
- ✅ Color selection cancellation (gracefully exits)
- ✅ Library load failures (shows error message)

## Example Usage Scenarios

### Scenario 1: First-Time User
1. Tap "Add" → "Create New"
2. Enter "Chocolate Chip Cookies"
3. Select "Pink"
4. Special added and saved to library

### Scenario 2: Regular Daily Update
1. Tap "Add" → "Pick from Library"
2. Tap "Chocolate Chip Cookies"
3. Special added instantly with saved Pink color

### Scenario 3: Seasonal Special
1. Tap "Add" → "Create New"
2. Enter "Pumpkin Spice Latte"
3. Select "Orange"
4. Special added and saved for future use

## Future Enhancements (Ideas)

- Edit library entries (text/color)
- Delete from library
- Search/filter in library
- Sort library (alphabetically, by frequency, by color)
- Import/export library
- Category/tags for specials
- Most frequently used at top
- Color preview dots next to library items
- Custom color hex input
- Favorite/pin specials in library

## Testing Checklist

- [ ] Tap Add → Cancel (no special added)
- [ ] Create New → Cancel at text prompt
- [ ] Create New → Cancel at color prompt
- [ ] Create New → Complete flow (special added)
- [ ] Pick from Library with empty library (shows message)
- [ ] Pick from Library with items (shows list)
- [ ] Pick from Library → Cancel
- [ ] Pick from Library → Select item (adds to list)
- [ ] Try to add duplicate from library (shows warning)
- [ ] Verify library persistence (restart app)
- [ ] Create multiple specials (library grows)
- [ ] Color picker displays all 10 colors
- [ ] Special displays with selected color in list
