# CSV Export Feature Documentation

## Overview

The CSV Export feature allows administrators to export survey responses to CSV (Comma-Separated Values) format for analysis in spreadsheet applications like Excel, Google Sheets, or statistical software.

## Location

**Page**: Survey Statistics (`/dashboard/surveys/:id/statistics`)
**Button**: "Export CSV" button in the page header (next to Refresh button)

## Features

### 1. Export Button
- Located in the statistics page header
- Icon: Download icon (GetApp)
- Disabled when survey has no responses
- Shows tooltip explaining the feature

### 2. Export Dialog

When clicked, a dialog appears with the following options:

#### Response Filter
- **Completed Responses Only** (default): Export only fully completed surveys
- **Incomplete Responses**: Export partially completed surveys
- **All Responses**: Export all responses regardless of completion status

Shows count for each filter type.

#### Additional Options
- **Include metadata columns**: Adds Response ID, Respondent ID, and Status columns
- **Include timestamps**: Adds Started At and Submitted At columns

Both options are enabled by default.

### 3. CSV Structure

#### Header Row
The CSV file contains the following columns (based on selected options):

**Metadata Columns** (if enabled):
- Response ID
- Respondent ID
- Status (Complete/Incomplete)

**Timestamp Columns** (if enabled):
- Started At (ISO 8601 format)
- Submitted At (ISO 8601 format)

**Question Columns**:
- One column per question
- Format: "Q{number}: {question text (truncated to 50 chars)}"
- Example: "Q1: How satisfied are you with our service?"

#### Data Rows
Each row represents one response with the following formats per question type:

**Text Questions**:
- Full text answer
- Empty if not answered

**Single Choice Questions**:
- Selected option text
- Empty if not answered

**Multiple Choice Questions**:
- Semicolon-separated list of selected options
- Example: "Option A; Option C"
- Empty if not answered

**Rating Questions**:
- Numeric value (1-5)
- Empty if not answered

### 4. CSV Escaping

The generator properly handles special characters:
- Commas: Cell wrapped in quotes
- Double quotes: Escaped by doubling (`"` becomes `""`)
- Line breaks: Cell wrapped in quotes
- Leading/trailing spaces: Cell wrapped in quotes

Example:
```csv
"This answer has, commas","This has ""quotes""","Normal text"
```

### 5. Large Dataset Handling

For surveys with more than 1000 responses:
- Automatic chunked processing (500 responses per chunk)
- Progress updates logged to console
- Prevents browser memory issues
- Shows info alert in dialog about large dataset

### 6. File Download

**Filename Format**:
```
survey_{surveyId}_{sanitizedTitle}_{date}_{timestamp}.csv
```

Example:
```
survey_1_customer_satisfaction_2025-01-15_1705320000000.csv
```

**Download Behavior**:
- Automatically triggers browser download
- UTF-8 encoding with BOM for proper Excel compatibility
- Cleanup of temporary download link after 100ms

## Usage Example

### Step-by-Step

1. Navigate to Survey Statistics page
2. Click "Export CSV" button in header
3. Select desired response filter:
   - Completed only (default)
   - Incomplete only
   - All responses
4. Configure additional options:
   - Toggle metadata columns
   - Toggle timestamp columns
5. Review response count
6. Click "Export X Responses" button
7. Wait for file download (automatic)
8. Open in spreadsheet application

### Sample CSV Output

With metadata and timestamps enabled:

```csv
Response ID,Respondent ID,Status,Started At,Submitted At,Q1: How satisfied are you?,Q2: Favorite feature?,Q3: Additional comments?
1,123456789,Complete,2025-01-01T10:00:00Z,2025-01-01T10:05:00Z,5,Design,Great service!
2,987654321,Complete,2025-01-01T11:00:00Z,2025-01-01T11:03:00Z,4,Speed,
3,555555555,Incomplete,2025-01-01T12:00:00Z,,3,Design,
```

Without metadata and timestamps:

```csv
Q1: How satisfied are you?,Q2: Favorite feature?,Q3: Additional comments?
5,Design,Great service!
4,Speed,
3,Design,
```

## Error Handling

### Validation Errors
- **No responses selected**: Export button disabled in dialog
- **No matching responses**: Warning alert shown in dialog
- **Empty survey**: Export button disabled on page

### Export Errors
- Caught and displayed in error snackbar
- User can retry export
- Dialog remains open to adjust options

### Success Notification
- Green success snackbar shown after successful export
- Auto-dismisses after 6 seconds
- Shows message: "Survey data exported successfully!"

## Technical Implementation

### Components

**ExportDialog.tsx**:
- Material-UI dialog component
- Form controls for options
- Response count display
- Loading state during export

**CSVGenerator.ts**:
- Core CSV generation logic
- Chunked processing for large datasets
- Proper CSV escaping
- Answer formatting by question type

### Key Methods

```typescript
// Generate CSV string
CSVGenerator.generateCSV(survey, responses, options): string

// Download CSV file
CSVGenerator.downloadCSV(survey, responses, options): Promise<void>

// Download large CSV with progress
CSVGenerator.downloadLargeCSV(
  survey,
  responses,
  options,
  onProgress
): Promise<void>
```

### Export Options Interface

```typescript
interface ExportOptions {
  includeMetadata: boolean;
  includeTimestamps: boolean;
  exportFormat: 'all' | 'completed' | 'incomplete';
}
```

## Browser Compatibility

- Modern browsers (Chrome, Firefox, Safari, Edge)
- Requires support for:
  - Blob API
  - URL.createObjectURL
  - File download via anchor tag
  - Async/await

## Performance

### Small Datasets (< 1000 responses)
- Synchronous processing
- Instant generation
- Minimal memory usage

### Large Datasets (1000+ responses)
- Chunked processing (500 per chunk)
- Progress updates
- UI remains responsive
- Memory-efficient

### Very Large Datasets (10,000+ responses)
- May take several seconds
- Browser may show "page unresponsive" warning (normal)
- File size can be 1-10 MB depending on question count
- No upper limit enforced

## Future Enhancements (TASK-059)

Backend CSV export API endpoint will provide:
- Server-side generation
- Direct download links
- Support for filters (date range, status)
- Paginated export for extremely large datasets
- Export history and scheduled exports

## Troubleshooting

### Export button disabled
- Check if survey has any responses
- Verify you're on the statistics page
- Refresh page to reload data

### File not downloading
- Check browser's download settings
- Disable popup blockers
- Try different browser
- Check browser console for errors

### Excel shows garbled characters
- File uses UTF-8 encoding
- Open using "Import Data" instead of double-clicking
- Select UTF-8 encoding during import

### Missing answers in CSV
- Verify response completion status
- Check if questions were actually answered
- Try exporting "All Responses" to see incomplete data

## Support

For issues or questions:
1. Check browser console for errors
2. Verify survey has responses
3. Try with smaller dataset first
4. Contact development team with:
   - Survey ID
   - Response count
   - Browser and version
   - Error messages
