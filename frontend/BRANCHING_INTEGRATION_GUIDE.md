# Branching Questions - Integration Guide

This guide explains how to integrate the branching questions feature into your survey builder workflow.

## Quick Start

The branching components are ready to use. Follow these steps to integrate:

### Step 1: Import Components

```typescript
import { BranchingRuleEditor } from '@/components/SurveyBuilder';
import branchingRuleService from '@/services/branchingRuleService';
```

### Step 2: Add State Management

In your survey builder or edit page:

```typescript
import { useState } from 'react';
import type { Question, BranchingRule } from '@/types';

function SurveyBuilderPage() {
  const [branchingEditorOpen, setBranchingEditorOpen] = useState(false);
  const [currentQuestion, setCurrentQuestion] = useState<Question | null>(null);
  const [branchingRules, setBranchingRules] = useState<Map<number, BranchingRule[]>>(new Map());

  // ... rest of your component
}
```

### Step 3: Implement Handlers

```typescript
// Open branching editor
const handleConfigureBranching = (question: Question) => {
  setCurrentQuestion(question);
  setBranchingEditorOpen(true);
};

// Save branching rule
const handleSaveBranchingRule = async (rule: Partial<BranchingRule>) => {
  try {
    if (!currentQuestion || !surveyId) return;

    const savedRule = await branchingRuleService.createBranchingRule(
      surveyId,
      currentQuestion.id,
      rule as CreateBranchingRuleDto
    );

    // Update local state
    const questionRules = branchingRules.get(currentQuestion.id) || [];
    setBranchingRules(prev => new Map(prev).set(
      currentQuestion.id,
      [...questionRules, savedRule]
    ));

    setBranchingEditorOpen(false);
    // Show success message
  } catch (error) {
    console.error('Failed to save branching rule:', error);
    // Show error message
  }
};

// Delete branching rule
const handleDeleteBranchingRule = async (ruleId: number) => {
  try {
    if (!currentQuestion || !surveyId) return;

    await branchingRuleService.deleteBranchingRule(
      surveyId,
      currentQuestion.id,
      ruleId
    );

    // Update local state
    const questionRules = branchingRules.get(currentQuestion.id) || [];
    setBranchingRules(prev => new Map(prev).set(
      currentQuestion.id,
      questionRules.filter(r => r.id !== ruleId)
    ));

    setBranchingEditorOpen(false);
    // Show success message
  } catch (error) {
    console.error('Failed to delete branching rule:', error);
    // Show error message
  }
};

// Cancel branching editor
const handleCancelBranching = () => {
  setBranchingEditorOpen(false);
  setCurrentQuestion(null);
};
```

### Step 4: Pass Props to QuestionList

```typescript
<QuestionList
  questions={questions}
  onReorder={handleReorderQuestions}
  onEdit={handleEditQuestion}
  onDelete={handleDeleteQuestion}
  onConfigureBranching={handleConfigureBranching}
/>
```

### Step 5: Render BranchingRuleEditor

```typescript
{currentQuestion && (
  <BranchingRuleEditor
    surveyId={surveyId}
    sourceQuestion={currentQuestion}
    targetQuestions={questions.filter(q => q.id !== currentQuestion.id)}
    onSave={handleSaveBranchingRule}
    onCancel={handleCancelBranching}
    onDelete={handleDeleteBranchingRule}
    open={branchingEditorOpen}
  />
)}
```

### Step 6: Calculate Branching Counts

Update QuestionCard to show accurate counts:

```typescript
// In QuestionList component
{questions.map((question, index) => {
  const rulesCount = branchingRules.get(question.id)?.length || 0;

  return (
    <QuestionCard
      key={question.id}
      question={question}
      index={index}
      onEdit={onEdit}
      onDelete={handleDeleteClick}
      onConfigureBranching={onConfigureBranching}
      branchingRulesCount={rulesCount}
      isDragging={question.id === activeId}
    />
  );
})}
```

## Complete Example

Here's a complete example of integrating branching into a survey edit page:

```typescript
import React, { useState, useEffect } from 'react';
import {
  QuestionList,
  BranchingRuleEditor,
} from '@/components/SurveyBuilder';
import branchingRuleService from '@/services/branchingRuleService';
import surveyService from '@/services/surveyService';
import { useParams } from 'react-router-dom';
import type { Question, BranchingRule, CreateBranchingRuleDto } from '@/types';

function SurveyEditPage() {
  const { surveyId } = useParams<{ surveyId: string }>();
  const [questions, setQuestions] = useState<Question[]>([]);
  const [branchingRules, setBranchingRules] = useState<Map<number, BranchingRule[]>>(new Map());
  const [branchingEditorOpen, setBranchingEditorOpen] = useState(false);
  const [currentQuestion, setCurrentQuestion] = useState<Question | null>(null);
  const [loading, setLoading] = useState(true);

  // Load survey and branching rules
  useEffect(() => {
    loadSurveyData();
  }, [surveyId]);

  const loadSurveyData = async () => {
    try {
      setLoading(true);

      // Load survey with questions
      const survey = await surveyService.getSurvey(Number(surveyId));
      setQuestions(survey.questions);

      // Load branching rules for each question
      const rulesMap = new Map<number, BranchingRule[]>();

      for (const question of survey.questions) {
        if (question.questionType === QuestionType.SingleChoice) {
          try {
            const rules = await branchingRuleService.getBranchingRules(
              survey.id,
              question.id
            );
            if (rules.length > 0) {
              rulesMap.set(question.id, rules);
            }
          } catch (error) {
            console.error(`Failed to load rules for question ${question.id}:`, error);
          }
        }
      }

      setBranchingRules(rulesMap);
    } catch (error) {
      console.error('Failed to load survey:', error);
      // Show error message
    } finally {
      setLoading(false);
    }
  };

  const handleConfigureBranching = (question: Question) => {
    setCurrentQuestion(question);
    setBranchingEditorOpen(true);
  };

  const handleSaveBranchingRule = async (rule: Partial<BranchingRule>) => {
    try {
      if (!currentQuestion || !surveyId) return;

      const savedRule = await branchingRuleService.createBranchingRule(
        Number(surveyId),
        currentQuestion.id,
        rule as CreateBranchingRuleDto
      );

      // Update local state
      const questionRules = branchingRules.get(currentQuestion.id) || [];
      setBranchingRules(prev => {
        const newMap = new Map(prev);
        newMap.set(currentQuestion.id, [...questionRules, savedRule]);
        return newMap;
      });

      setBranchingEditorOpen(false);
      // Show success toast/notification
    } catch (error) {
      console.error('Failed to save branching rule:', error);
      throw error; // Let BranchingRuleEditor handle error display
    }
  };

  const handleDeleteBranchingRule = async (ruleId: number) => {
    try {
      if (!currentQuestion || !surveyId) return;

      await branchingRuleService.deleteBranchingRule(
        Number(surveyId),
        currentQuestion.id,
        ruleId
      );

      // Update local state
      const questionRules = branchingRules.get(currentQuestion.id) || [];
      setBranchingRules(prev => {
        const newMap = new Map(prev);
        newMap.set(
          currentQuestion.id,
          questionRules.filter(r => r.id !== ruleId)
        );
        return newMap;
      });

      setBranchingEditorOpen(false);
      // Show success toast/notification
    } catch (error) {
      console.error('Failed to delete branching rule:', error);
      throw error; // Let BranchingRuleEditor handle error display
    }
  };

  const handleCancelBranching = () => {
    setBranchingEditorOpen(false);
    setCurrentQuestion(null);
  };

  if (loading) {
    return <LoadingSpinner />;
  }

  return (
    <Box>
      <QuestionList
        questions={questions}
        onReorder={handleReorderQuestions}
        onEdit={handleEditQuestion}
        onDelete={handleDeleteQuestion}
        onConfigureBranching={handleConfigureBranching}
      />

      {currentQuestion && (
        <BranchingRuleEditor
          surveyId={Number(surveyId)}
          sourceQuestion={currentQuestion}
          targetQuestions={questions.filter(q => q.id !== currentQuestion.id)}
          onSave={handleSaveBranchingRule}
          onCancel={handleCancelBranching}
          onDelete={handleDeleteBranchingRule}
          open={branchingEditorOpen}
        />
      )}
    </Box>
  );
}

export default SurveyEditPage;
```

## Working with Draft Questions

For the survey builder (creating new surveys), you'll work with draft questions that don't have IDs yet. Here's how to handle that:

```typescript
interface QuestionDraft {
  id: string; // Temporary UUID
  // ... other fields
}

// When publishing survey:
const publishSurvey = async () => {
  // 1. Create survey
  const survey = await surveyService.createSurvey(basicInfo);

  // 2. Create questions
  const createdQuestions = [];
  for (const draftQuestion of questions) {
    const question = await questionService.createQuestion(
      survey.id,
      draftQuestion
    );
    createdQuestions.push(question);
  }

  // 3. Create branching rules using actual question IDs
  for (const rule of draftBranchingRules) {
    // Map draft IDs to real IDs
    const sourceQuestion = createdQuestions.find(
      q => q.orderIndex === rule.sourceDraftIndex
    );
    const targetQuestion = createdQuestions.find(
      q => q.orderIndex === rule.targetDraftIndex
    );

    if (sourceQuestion && targetQuestion) {
      await branchingRuleService.createBranchingRule(
        survey.id,
        sourceQuestion.id,
        {
          sourceQuestionId: sourceQuestion.id,
          targetQuestionId: targetQuestion.id,
          condition: rule.condition,
        }
      );
    }
  }
};
```

## API Error Handling

Handle common API errors:

```typescript
const handleSaveBranchingRule = async (rule: Partial<BranchingRule>) => {
  try {
    // ... save logic
  } catch (error) {
    if (axios.isAxiosError(error)) {
      if (error.response?.status === 400) {
        // Validation error
        const message = error.response.data.message || 'Invalid rule configuration';
        // Show validation error to user
      } else if (error.response?.status === 404) {
        // Survey or question not found
        // Show error and possibly redirect
      } else {
        // Generic error
        // Show generic error message
      }
    }
    throw error; // Re-throw for BranchingRuleEditor to handle
  }
};
```

## State Persistence

For draft surveys, persist branching rules to localStorage:

```typescript
// Save to localStorage
const saveDraftBranchingRules = (rules: Map<string, any[]>) => {
  const rulesArray = Array.from(rules.entries());
  localStorage.setItem('draft_branching_rules', JSON.stringify(rulesArray));
};

// Load from localStorage
const loadDraftBranchingRules = (): Map<string, any[]> => {
  const stored = localStorage.getItem('draft_branching_rules');
  if (stored) {
    const rulesArray = JSON.parse(stored);
    return new Map(rulesArray);
  }
  return new Map();
};

// Clear on publish
const clearDraftBranchingRules = () => {
  localStorage.removeItem('draft_branching_rules');
};
```

## Validation

Client-side validation is already handled by BranchingRuleEditor. Add additional business logic validation:

```typescript
const validateBranchingRule = (
  rule: Partial<BranchingRule>,
  questions: Question[]
): string | null => {
  // Check if target question exists
  const targetExists = questions.some(q => q.id === rule.targetQuestionId);
  if (!targetExists) {
    return 'Target question not found';
  }

  // Check for circular references
  if (hasCircularReference(rule, questions)) {
    return 'Circular reference detected';
  }

  // Check if source and target are the same
  if (rule.sourceQuestionId === rule.targetQuestionId) {
    return 'Cannot branch to the same question';
  }

  return null; // Valid
};

const hasCircularReference = (
  rule: Partial<BranchingRule>,
  questions: Question[]
): boolean => {
  // Implement circular reference detection
  // This is a simplified version
  const visited = new Set<number>();
  let current = rule.targetQuestionId;

  while (current) {
    if (visited.has(current)) {
      return true; // Circular reference found
    }

    visited.add(current);

    // Find next question in the chain
    const question = questions.find(q => q.id === current);
    const nextRule = question?.branchingRules?.[0];

    if (!nextRule) break;
    current = nextRule.targetQuestionId;
  }

  return false;
};
```

## Testing

Test the integration:

```typescript
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import SurveyEditPage from './SurveyEditPage';

test('should open branching editor when clicking branch button', async () => {
  render(<SurveyEditPage />);

  // Wait for questions to load
  await waitFor(() => {
    expect(screen.getByText(/Question 1/)).toBeInTheDocument();
  });

  // Click branching button
  const branchButton = screen.getByLabelText('Configure branching');
  fireEvent.click(branchButton);

  // Verify editor opened
  expect(screen.getByText('Create Branching Rule')).toBeInTheDocument();
});

test('should create branching rule', async () => {
  render(<SurveyEditPage />);

  // ... open editor ...

  // Fill form
  // ... select operator, value, target ...

  // Submit
  fireEvent.click(screen.getByText('Create Rule'));

  // Verify rule created
  await waitFor(() => {
    expect(screen.getByText(/1 branch/)).toBeInTheDocument();
  });
});
```

## Troubleshooting

### Issue: Branching button not visible
**Solution**: Ensure question type is SingleChoice and `onConfigureBranching` prop is passed

### Issue: Target question list is empty
**Solution**: Verify there are other questions in the survey and they're being filtered correctly

### Issue: Rules not persisting
**Solution**: Check API responses and ensure state updates are working correctly

### Issue: Count not updating
**Solution**: Verify `branchingRulesCount` prop is calculated and passed correctly

## Best Practices

1. **Always load branching rules when editing a survey**
2. **Use Map for storing rules by question ID** for O(1) lookup
3. **Validate rules before saving** to prevent API errors
4. **Show loading states** during async operations
5. **Handle errors gracefully** with user-friendly messages
6. **Clear draft data** after successful publish
7. **Use optimistic updates** for better UX
8. **Test with various question types** and edge cases

## Next Steps

After integration:
1. Test with real backend API
2. Add unit tests for handlers
3. Add E2E tests for full flow
4. Implement undo/redo for rules
5. Add rule preview in review step
6. Implement rule analytics

---

For questions or issues, refer to:
- [Main Documentation](CLAUDE.md)
- [E2E Tests Guide](BRANCHING_E2E_TESTS.md)
- [Implementation Summary](PHASE-5-IMPLEMENTATION-SUMMARY.md)
