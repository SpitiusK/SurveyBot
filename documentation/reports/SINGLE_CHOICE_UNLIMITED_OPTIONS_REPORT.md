# Report: Single Choice Question - Unlimited Options Capability

**Date**: December 11, 2025
**System**: SurveyBot v1.6.2
**Subject**: Analysis of option creation limits for Single Choice questions

---

## Summary

This report demonstrates that **Single Choice questions support unlimited option creation** at the backend level (API, Database, Core), with **optional UI-level restrictions** available in the frontend through configurable props.

---

## Backend: No Hard Limits

### 1. Database Layer

The `question_options` table has **no constraint** on the number of options per question:

```sql
CREATE TABLE question_options (
    id SERIAL PRIMARY KEY,
    question_id INT NOT NULL,
    text TEXT NOT NULL,
    order_index INT NOT NULL,  -- Range: 0 to 2,147,483,647
    -- NO max_options constraint exists
);
```

**Source**: `src/SurveyBot.Infrastructure/Data/Configurations/QuestionOptionConfiguration.cs`

### 2. Core Domain Layer

The `Question` entity stores options in an unbounded collection:

```csharp
// No size limit on this collection
private readonly List<QuestionOption> _options = new();
public IReadOnlyCollection<QuestionOption> Options => _options.AsReadOnly();
```

**Source**: `src/SurveyBot.Core/Entities/Question.cs`

### 3. API Layer

The API accepts option arrays without enforcing a maximum count. Options can be added via:
- `POST /api/surveys` - Create survey with questions
- `PUT /api/surveys/{id}/complete` - Complete survey update
- `POST /api/questions` - Add individual questions

---

## Frontend: Configurable UI Limit

The frontend provides a **configurable limit** through the `OptionManager` component:

```typescript
// OptionManager.tsx - Props interface
interface OptionManagerProps {
  options: string[];
  onChange: (options: string[]) => void;
  minOptions?: number;  // Default: 2
  maxOptions?: number;  // Default: 10 (configurable)
}
```

**Source**: `frontend/src/components/SurveyBuilder/OptionManager.tsx`

### How the UI Limit Works

| Prop | Default | Purpose |
|------|---------|---------|
| `minOptions` | 2 | Minimum required options |
| `maxOptions` | 10 | Maximum allowed options (UI only) |

The "Add Option" button is disabled when `options.length >= maxOptions`:

```typescript
<Button
  onClick={handleAddOption}
  disabled={options.length >= maxOptions}  // UI enforcement only
>
  Add Option
</Button>
```

### Important Note

This is a **UI-only restriction**. The limit:
- Can be changed by passing different `maxOptions` prop value
- Does not affect direct API calls
- Does not enforce any backend validation

---

## Conclusion

| Layer | Option Limit | Type |
|-------|--------------|------|
| **Database** | Unlimited | No constraint |
| **Core (Entity)** | Unlimited | No collection limit |
| **API** | Unlimited | No validation |
| **Frontend UI** | 10 (default) | Configurable via `maxOptions` prop |

**Result**: Single Choice questions can have unlimited options at the system level. The only restriction is a configurable UI limit (`maxOptions` prop) that prevents users from adding more options through the interface, but this can be adjusted as needed.

---

## References

- Entity: `src/SurveyBot.Core/Entities/Question.cs` (lines 91-97)
- Entity: `src/SurveyBot.Core/Entities/QuestionOption.cs`
- Database: `src/SurveyBot.Infrastructure/Data/Configurations/QuestionOptionConfiguration.cs`
- Frontend: `frontend/src/components/SurveyBuilder/OptionManager.tsx` (lines 138-145)
