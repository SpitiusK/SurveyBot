# TASK-053 Visual Diagrams

## Component Hierarchy

```
SurveyBuilder (Page)
├── PageContainer (Layout)
│   ├── Breadcrumbs
│   └── Title
├── Container (MUI)
│   ├── Paper (Card)
│   │   ├── Stepper (MUI)
│   │   │   ├── Step 1: Basic Info ✓
│   │   │   ├── Step 2: Questions (placeholder)
│   │   │   └── Step 3: Review & Publish (placeholder)
│   │   ├── Alert (Error/Success)
│   │   ├── Step Content Area
│   │   │   ├── [Step 0] BasicInfoStep
│   │   │   │   ├── Info Banner (Paper)
│   │   │   │   ├── Title TextField (Controller)
│   │   │   │   ├── Description TextField (Controller)
│   │   │   │   └── Settings Section (Paper)
│   │   │   │       ├── Show Results Checkbox
│   │   │   │       └── Allow Multiple Responses Checkbox
│   │   │   ├── [Step 1] QuestionsStep (placeholder)
│   │   │   └── [Step 2] ReviewStep (placeholder)
│   │   └── Navigation Buttons
│   │       ├── Cancel Button (left)
│   │       └── Action Buttons (right)
│   │           ├── Save Draft Button
│   │           ├── Back Button (conditional)
│   │           ├── Next Button (conditional)
│   │           └── Publish Button (conditional)
│   └── Auto-save Info Alert
```

## Data Flow

```
User Loads Page
      │
      ├──[Has ID?]──YES──► Load from API
      │                         │
      │                         ▼
      │                    Populate Form
      │
      └──[Has Draft?]──YES──► Load from localStorage
                                 │
                                 ▼
                            Populate Form
                                 │
                                 ▼
                         User Edits Form
                                 │
                    ┌────────────┴────────────┐
                    │                         │
                    ▼                         ▼
            Auto-save (1s)              Manual Save
            to localStorage              to API
                    │                         │
                    │                         ▼
                    │                   Success/Error
                    │                         │
                    └─────────────┬───────────┘
                                  │
                                  ▼
                            Continue Editing
```
