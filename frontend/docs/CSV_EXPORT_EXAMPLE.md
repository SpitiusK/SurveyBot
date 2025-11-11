# CSV Export - Usage Examples

## Basic Usage

### Example 1: Export All Completed Responses with Metadata

This is the default and most common use case.

**Steps**:
1. Navigate to survey statistics
2. Click "Export CSV"
3. Keep default settings:
   - Response Filter: "Completed Responses Only"
   - Include metadata columns: ✓
   - Include timestamps: ✓
4. Click "Export 150 Responses"

**Result**:
```csv
Response ID,Respondent ID,Status,Started At,Submitted At,Q1: How satisfied are you with our service?,Q2: What features do you like most?,Q3: Any suggestions?
1,123456789,Complete,2025-01-01T10:00:00.000Z,2025-01-01T10:05:00.000Z,5,Speed; Design,Great work!
2,987654321,Complete,2025-01-01T11:00:00.000Z,2025-01-01T11:03:00.000Z,4,Design,
3,555555555,Complete,2025-01-01T12:00:00.000Z,2025-01-01T12:02:00.000Z,5,Speed; Functionality; Design,Love it!
```

---

## Example 2: Clean Export for Analysis (No Metadata)

When you only need the answers for statistical analysis.

**Settings**:
- Response Filter: "Completed Responses Only"
- Include metadata columns: ✗
- Include timestamps: ✗

**Result**:
```csv
Q1: How satisfied are you with our service?,Q2: What features do you like most?,Q3: Any suggestions?
5,Speed; Design,Great work!
4,Design,
5,Speed; Functionality; Design,Love it!
```

**Use Case**: Import into R, Python, or SPSS for statistical analysis.

---

## Example 3: Export All Responses (Including Incomplete)

Monitor partially completed surveys.

**Settings**:
- Response Filter: "All Responses"
- Include metadata columns: ✓
- Include timestamps: ✓

**Result**:
```csv
Response ID,Respondent ID,Status,Started At,Submitted At,Q1: Rate our service?,Q2: Favorite feature?,Q3: Comments?
1,123456789,Complete,2025-01-01T10:00:00.000Z,2025-01-01T10:05:00.000Z,5,Design,Great!
2,987654321,Incomplete,2025-01-01T11:00:00.000Z,,4,Speed,
3,555555555,Incomplete,2025-01-01T12:00:00.000Z,,,Design,
4,111111111,Complete,2025-01-01T13:00:00.000Z,2025-01-01T13:10:00.000Z,3,Functionality,Needs improvement
```

**Analysis**:
- 50% completion rate (2/4)
- Can identify where users drop off
- Empty cells show unanswered questions

---

## Example 4: Large Dataset Export (5000+ Responses)

**Settings**:
- Response Filter: "Completed Responses Only"
- Include metadata columns: ✓
- Include timestamps: ✓

**Behavior**:
1. Dialog shows: "This survey has a large number of responses (5247). Export may take a moment."
2. Export button clicked
3. Console shows progress:
   ```
   Export progress: 10%
   Export progress: 20%
   ...
   Export progress: 100%
   ```
4. File downloads automatically
5. Success notification appears

**File Size**: Approximately 2-5 MB depending on question types and answer length.

---

## Question Type Examples

### Text Question

**Question**: "What improvements would you suggest?"

**CSV Column**:
```csv
Q5: What improvements would you suggest?
```

**Example Answers**:
```csv
Better performance
More features and better UI
"This needs work, especially the ""settings"" page"
```

Note: Answers with commas or quotes are automatically escaped.

---

### Single Choice Question

**Question**: "What is your preferred contact method?"

**Options**: Email, Phone, SMS

**CSV Column**:
```csv
Q2: What is your preferred contact method?
```

**Example Answers**:
```csv
Email
Phone
SMS
```

---

### Multiple Choice Question

**Question**: "Which features do you use? (Select all)"

**Options**: Dashboard, Reports, Export, API

**CSV Column**:
```csv
Q3: Which features do you use? (Select all)
```

**Example Answers**:
```csv
Dashboard; Reports
Export; API
Dashboard; Reports; Export; API

```

Note: Multiple selections separated by semicolons.

---

### Rating Question

**Question**: "How likely are you to recommend us?"

**Scale**: 1-5

**CSV Column**:
```csv
Q1: How likely are you to recommend us?
```

**Example Answers**:
```csv
5
4
3
5
```

---

## Advanced Scenarios

### Scenario 1: Survey with Long Question Text

**Question**: "On a scale of 1-5, where 1 is very dissatisfied and 5 is very satisfied, how would you rate your overall experience with our customer support team over the past month?"

**CSV Column** (truncated):
```csv
Q4: On a scale of 1-5, where 1 is very dissatis...
```

Note: Question text truncated to 50 characters in header.

---

### Scenario 2: Answers with Special Characters

**Question**: "What did you think?"

**Answer**: `I love the "new feature", it's great!`

**CSV Cell**:
```csv
"I love the ""new feature"", it's great!"
```

**In Excel**: Displays as: `I love the "new feature", it's great!`

---

### Scenario 3: Empty/Skipped Questions

**Question 1**: "Required question"
**Question 2**: "Optional question" (not answered)
**Question 3**: "Another question"

**CSV Row**:
```csv
Answer 1,,Answer 3
```

Note: Empty cell for skipped optional question.

---

## Excel/Google Sheets Tips

### Opening in Excel

**Method 1** (Recommended):
1. Open Excel
2. Go to Data → Get Data → From File → From Text/CSV
3. Select the CSV file
4. Choose UTF-8 encoding
5. Click Load

**Method 2**:
Double-click the CSV file (may have encoding issues on some systems)

### Opening in Google Sheets

1. Go to Google Sheets
2. File → Import
3. Upload the CSV file
4. Import location: New spreadsheet
5. Separator type: Auto-detect
6. Click Import

### Creating Pivot Tables

After importing:
1. Select all data (Ctrl+A)
2. Insert → Pivot Table
3. Add fields:
   - Rows: Question columns
   - Values: Count of responses
4. Analyze distribution

---

## Data Analysis Examples

### Example: Calculate Average Rating

**Python**:
```python
import pandas as pd

df = pd.read_csv('survey_1_customer_satisfaction_2025-01-15.csv')
avg_rating = df['Q1: How satisfied are you?'].mean()
print(f"Average rating: {avg_rating:.2f}")
```

**Excel Formula**:
```excel
=AVERAGE(D2:D1000)
```

Where D is the rating question column.

---

### Example: Count Response by Choice

**Excel PivotTable**:
1. Rows: Q2 (choice question)
2. Values: Count of responses

**Result**:
```
Option A: 45
Option B: 23
Option C: 67
```

---

### Example: Analyze Text Responses

**Export text question answers**:
1. Copy text column
2. Paste into word cloud generator
3. Identify common themes

**Tools**:
- WordClouds.com
- Python's wordcloud library
- R's wordcloud package

---

## Troubleshooting

### Problem: Empty cells in required questions

**Cause**: Response marked incomplete or answer data missing

**Solution**:
1. Filter by "Status" column
2. Check "Incomplete" responses
3. Or export "Completed Only"

---

### Problem: Wrong encoding in Excel

**Symptom**: Special characters display as �

**Solution**:
1. Don't double-click CSV
2. Use Data → Get Data → From Text/CSV
3. Select UTF-8 encoding

---

### Problem: Multiple choice shows only one option

**Cause**: Excel treating semicolon as separator

**Solution**:
1. Format column as Text before import
2. Or use "Text to Columns" with semicolon delimiter

---

### Problem: Dates not recognized

**Cause**: ISO 8601 format not auto-detected

**Solution**:
1. Select timestamp columns
2. Format → Date
3. Choose custom format: yyyy-mm-dd hh:mm:ss

---

## Automation Examples

### PowerShell: Download Multiple Survey Exports

```powershell
# Assuming API endpoint exists (TASK-059)
$surveys = @(1, 2, 3, 4, 5)

foreach ($surveyId in $surveys) {
    $url = "http://localhost:5000/api/surveys/$surveyId/export?format=csv"
    $output = "survey_$surveyId.csv"

    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "Downloaded $output"
}
```

### Python: Combine Multiple Exports

```python
import pandas as pd
import glob

# Read all CSV files
csv_files = glob.glob('survey_*.csv')
dfs = [pd.read_csv(f) for f in csv_files]

# Combine
combined = pd.concat(dfs, ignore_index=True)

# Save
combined.to_csv('all_surveys_combined.csv', index=False)
```

---

## Best Practices

1. **Export Regularly**: Don't wait until you have thousands of responses
2. **Keep Metadata**: Always include metadata for traceability
3. **Backup Exports**: Save CSV files for historical analysis
4. **Test Small First**: Try export on small dataset before large one
5. **Use Filters**: Export only what you need to reduce file size
6. **Document Analysis**: Keep notes on what each export was used for
7. **Validate Data**: Spot-check a few rows after export
8. **Clean Before Analysis**: Remove test responses if needed

---

## Next Steps

Once backend export is implemented (TASK-059), you'll be able to:
- Export via API endpoint
- Schedule automated exports
- Apply server-side filters
- Download historical exports
- Export to other formats (JSON, Excel)

Stay tuned for updates!
