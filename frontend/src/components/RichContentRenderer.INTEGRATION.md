# RichContentRenderer Integration Guide

## Overview

This guide shows how to integrate the RichContentRenderer component into existing survey components to display rich text and media content.

## Prerequisites

- ✅ RichContentRenderer component created
- ✅ DOMPurify installed
- ✅ Component exported in index.ts
- ✅ MediaContentDto types defined

## Integration Steps

### Step 1: Update Question Type Definition

First, ensure your Question type includes rich text fields:

```typescript
// src/types/index.ts or question types file
import type { MediaContentDto } from './media';

interface Question {
  id: number;
  surveyId: number;
  text: string;              // This is now HTML content
  type: QuestionType;
  order: number;
  isRequired: boolean;
  options?: string[];
  mediaContent?: MediaContentDto; // NEW: Optional media content
  createdAt: string;
  updatedAt: string;
}
```

### Step 2: Create QuestionDisplay Component

Create a new component that uses RichContentRenderer:

```typescript
// src/components/QuestionDisplay.tsx
import React from 'react';
import { Box, Typography, TextField, Radio, RadioGroup, FormControlLabel, Checkbox, Rating } from '@mui/material';
import { RichContentRenderer } from './RichContentRenderer';
import type { Question } from '../types';

interface QuestionDisplayProps {
  question: Question;
  value?: string | string[];
  onChange?: (value: string | string[]) => void;
  readOnly?: boolean;
}

export const QuestionDisplay: React.FC<QuestionDisplayProps> = ({
  question,
  value,
  onChange,
  readOnly = false,
}) => {
  return (
    <Box sx={{ mb: 4 }}>
      {/* Question Text with Rich Content and Media */}
      <RichContentRenderer
        htmlContent={question.text}
        mediaContent={question.mediaContent}
        readOnly={true}
      />

      {/* Question Required Indicator */}
      {question.isRequired && (
        <Typography variant="caption" color="error" sx={{ display: 'block', mt: 1 }}>
          * Required
        </Typography>
      )}

      {/* Answer Input Based on Question Type */}
      <Box sx={{ mt: 3 }}>
        {renderAnswerInput(question, value, onChange, readOnly)}
      </Box>
    </Box>
  );
};

function renderAnswerInput(
  question: Question,
  value?: string | string[],
  onChange?: (value: string | string[]) => void,
  readOnly?: boolean
) {
  if (readOnly) {
    return (
      <Box sx={{ p: 2, bgcolor: 'grey.100', borderRadius: 1 }}>
        <Typography variant="body2" color="text.secondary">
          Answer: {Array.isArray(value) ? value.join(', ') : value}
        </Typography>
      </Box>
    );
  }

  switch (question.type) {
    case 'Text':
      return (
        <TextField
          fullWidth
          multiline
          rows={4}
          placeholder="Enter your answer..."
          value={value || ''}
          onChange={(e) => onChange?.(e.target.value)}
          required={question.isRequired}
        />
      );

    case 'SingleChoice':
      return (
        <RadioGroup
          value={value || ''}
          onChange={(e) => onChange?.(e.target.value)}
        >
          {question.options?.map((option) => (
            <FormControlLabel
              key={option}
              value={option}
              control={<Radio />}
              label={option}
            />
          ))}
        </RadioGroup>
      );

    case 'MultipleChoice':
      return (
        <Box>
          {question.options?.map((option) => (
            <FormControlLabel
              key={option}
              control={
                <Checkbox
                  checked={Array.isArray(value) && value.includes(option)}
                  onChange={(e) => {
                    const currentValue = (value as string[]) || [];
                    const newValue = e.target.checked
                      ? [...currentValue, option]
                      : currentValue.filter((v) => v !== option);
                    onChange?.(newValue);
                  }}
                />
              }
              label={option}
            />
          ))}
        </Box>
      );

    case 'Rating':
      return (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Rating
            value={value ? parseInt(value as string) : 0}
            onChange={(_, newValue) => onChange?.(String(newValue || 0))}
            size="large"
          />
          <Typography variant="body2" color="text.secondary">
            {value || 0} / 5
          </Typography>
        </Box>
      );

    default:
      return null;
  }
}

export default QuestionDisplay;
```

### Step 3: Update Survey Preview Page

```typescript
// src/pages/SurveyPreview.tsx
import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  Container,
  Paper,
  Typography,
  Box,
  Divider,
  CircularProgress,
} from '@mui/material';
import { QuestionDisplay } from '../components/QuestionDisplay';
import { surveyService } from '../services/surveyService';
import type { Survey } from '../types';

export const SurveyPreview: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [survey, setSurvey] = useState<Survey | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadSurvey();
  }, [id]);

  const loadSurvey = async () => {
    try {
      setLoading(true);
      const data = await surveyService.getSurvey(parseInt(id!));
      setSurvey(data);
    } catch (error) {
      console.error('Failed to load survey:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!survey) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Typography variant="h6" color="error">
          Survey not found
        </Typography>
      </Container>
    );
  }

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Paper elevation={2} sx={{ p: 4 }}>
        {/* Survey Header */}
        <Typography variant="h4" gutterBottom>
          {survey.title}
        </Typography>

        {survey.description && (
          <Typography variant="body1" color="text.secondary" paragraph>
            {survey.description}
          </Typography>
        )}

        <Divider sx={{ my: 3 }} />

        {/* Survey Questions */}
        {survey.questions.map((question, index) => (
          <Box key={question.id}>
            <Typography variant="overline" color="text.secondary">
              Question {index + 1} of {survey.questions.length}
            </Typography>

            <QuestionDisplay
              question={question}
              readOnly={true}
            />

            {index < survey.questions.length - 1 && (
              <Divider sx={{ my: 4 }} />
            )}
          </Box>
        ))}
      </Paper>
    </Container>
  );
};

export default SurveyPreview;
```

### Step 4: Update Survey Taking Page

```typescript
// src/pages/TakeSurvey.tsx
import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Container,
  Paper,
  Typography,
  Box,
  Button,
  Stepper,
  Step,
  StepLabel,
  CircularProgress,
} from '@mui/material';
import { QuestionDisplay } from '../components/QuestionDisplay';
import { surveyService } from '../services/surveyService';
import { responseService } from '../services/responseService';
import type { Survey } from '../types';

export const TakeSurvey: React.FC = () => {
  const { code } = useParams<{ code: string }>();
  const navigate = useNavigate();

  const [survey, setSurvey] = useState<Survey | null>(null);
  const [loading, setLoading] = useState(true);
  const [currentStep, setCurrentStep] = useState(0);
  const [answers, setAnswers] = useState<Record<number, string | string[]>>({});
  const [responseId, setResponseId] = useState<number | null>(null);

  useEffect(() => {
    loadSurvey();
  }, [code]);

  const loadSurvey = async () => {
    try {
      setLoading(true);
      const data = await surveyService.getSurveyByCode(code!);
      setSurvey(data);

      // Start response
      const response = await responseService.startResponse(data.id);
      setResponseId(response.id);
    } catch (error) {
      console.error('Failed to load survey:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleAnswerChange = (questionId: number, value: string | string[]) => {
    setAnswers((prev) => ({
      ...prev,
      [questionId]: value,
    }));
  };

  const handleNext = async () => {
    const currentQuestion = survey!.questions[currentStep];

    // Save current answer
    if (responseId && answers[currentQuestion.id]) {
      await responseService.saveAnswer(responseId, {
        questionId: currentQuestion.id,
        value: Array.isArray(answers[currentQuestion.id])
          ? (answers[currentQuestion.id] as string[]).join(',')
          : answers[currentQuestion.id] as string,
      });
    }

    if (currentStep < survey!.questions.length - 1) {
      setCurrentStep((prev) => prev + 1);
    } else {
      // Complete response
      if (responseId) {
        await responseService.completeResponse(responseId);
        navigate(`/survey/${code}/thank-you`);
      }
    }
  };

  const handleBack = () => {
    setCurrentStep((prev) => prev - 1);
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!survey) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Typography variant="h6" color="error">
          Survey not found or inactive
        </Typography>
      </Container>
    );
  }

  const currentQuestion = survey.questions[currentStep];

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      {/* Progress Stepper */}
      <Stepper activeStep={currentStep} sx={{ mb: 4 }}>
        {survey.questions.map((q, index) => (
          <Step key={q.id}>
            <StepLabel>Question {index + 1}</StepLabel>
          </Step>
        ))}
      </Stepper>

      {/* Question */}
      <Paper elevation={2} sx={{ p: 4 }}>
        <QuestionDisplay
          question={currentQuestion}
          value={answers[currentQuestion.id]}
          onChange={(value) => handleAnswerChange(currentQuestion.id, value)}
        />

        {/* Navigation Buttons */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 4 }}>
          <Button
            onClick={handleBack}
            disabled={currentStep === 0}
          >
            Back
          </Button>

          <Button
            variant="contained"
            onClick={handleNext}
            disabled={
              currentQuestion.isRequired &&
              !answers[currentQuestion.id]
            }
          >
            {currentStep === survey.questions.length - 1 ? 'Submit' : 'Next'}
          </Button>
        </Box>
      </Paper>
    </Container>
  );
};

export default TakeSurvey;
```

### Step 5: Update Response Viewing Page

```typescript
// src/pages/ResponseDetail.tsx
import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  Container,
  Paper,
  Typography,
  Box,
  Divider,
  Chip,
} from '@mui/material';
import { QuestionDisplay } from '../components/QuestionDisplay';
import { responseService } from '../services/responseService';
import type { Response } from '../types';

export const ResponseDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [response, setResponse] = useState<Response | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadResponse();
  }, [id]);

  const loadResponse = async () => {
    try {
      setLoading(true);
      const data = await responseService.getResponse(parseInt(id!));
      setResponse(data);
    } finally {
      setLoading(false);
    }
  };

  if (!response) return null;

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Paper elevation={2} sx={{ p: 4 }}>
        {/* Response Header */}
        <Box sx={{ mb: 3 }}>
          <Typography variant="h5" gutterBottom>
            Survey Response
          </Typography>
          <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
            <Chip
              label={response.isCompleted ? 'Completed' : 'In Progress'}
              color={response.isCompleted ? 'success' : 'warning'}
              size="small"
            />
            <Chip
              label={new Date(response.submittedAt || response.startedAt).toLocaleDateString()}
              variant="outlined"
              size="small"
            />
          </Box>
        </Box>

        <Divider sx={{ my: 3 }} />

        {/* Questions and Answers */}
        {response.answers.map((answer, index) => (
          <Box key={answer.id}>
            <Typography variant="overline" color="text.secondary">
              Question {index + 1}
            </Typography>

            <QuestionDisplay
              question={answer.question}
              value={answer.value}
              readOnly={true}
            />

            {index < response.answers.length - 1 && (
              <Divider sx={{ my: 4 }} />
            )}
          </Box>
        ))}
      </Paper>
    </Container>
  );
};

export default ResponseDetail;
```

## Testing Your Integration

### Test Checklist

1. **Basic Rendering**
   - [ ] Question text renders as HTML
   - [ ] Formatting (bold, italic, lists) displays correctly
   - [ ] Headings render with proper hierarchy

2. **Media Display**
   - [ ] Images display with alt text
   - [ ] Videos have playback controls
   - [ ] Audio players work correctly
   - [ ] Document download links function

3. **Security**
   - [ ] Script tags are removed
   - [ ] Event handlers are stripped
   - [ ] XSS attacks are prevented

4. **Responsiveness**
   - [ ] Mobile view works correctly
   - [ ] Media scales on different screens
   - [ ] Layout doesn't break on small screens

5. **Accessibility**
   - [ ] Screen reader announces content properly
   - [ ] Keyboard navigation works
   - [ ] Alt text is present on images
   - [ ] Media controls are accessible

### Manual Testing

```typescript
// Test data for manual testing
const testQuestion = {
  id: 1,
  surveyId: 1,
  text: `
    <h3>Product Feedback Survey</h3>
    <p>Please watch the video below and answer the following:</p>
    <ul>
      <li>Did the product meet your expectations?</li>
      <li>Would you recommend it to others?</li>
      <li>Any suggestions for improvement?</li>
    </ul>
  `,
  type: 'Text',
  order: 1,
  isRequired: true,
  mediaContent: {
    version: '1.0',
    items: [
      {
        id: 'vid-1',
        type: 'video',
        filePath: '/uploads/product-demo.mp4',
        displayName: 'Product Demo',
        fileSize: 10485760,
        mimeType: 'video/mp4',
        uploadedAt: new Date().toISOString(),
        altText: 'Product demonstration video',
        order: 0,
      },
    ],
  },
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

// Render in your test component
<QuestionDisplay question={testQuestion} />
```

## Troubleshooting

### Common Issues

1. **Media not displaying**
   - Check file paths are correct
   - Verify media files are accessible
   - Check browser console for errors

2. **Styles not applied**
   - Ensure RichContentRenderer.css is imported
   - Check for CSS conflicts
   - Verify MUI theme is configured

3. **XSS content showing**
   - This should never happen
   - If it does, report as a bug immediately
   - Check DOMPurify version

## Migration from Plain Text

If you're migrating from plain text questions:

```typescript
// Old way
<Typography variant="h6">{question.text}</Typography>

// New way
<RichContentRenderer htmlContent={question.text} />
```

For backward compatibility, plain text will render correctly as it will be wrapped in `<p>` tags by the browser.

## Performance Considerations

1. **Lazy Loading**: Images load only when visible
2. **Sanitization**: Happens once on mount and when content changes
3. **Re-rendering**: Component only updates when props change

## Next Steps

1. Create QuestionDisplay component
2. Update survey preview page
3. Update survey taking page
4. Update response viewing page
5. Add unit tests
6. Test with real data
7. Deploy to staging

## Related Documentation

- **Component README**: `RichContentRenderer.README.md`
- **Quick Start**: `RichContentRenderer.QUICKSTART.md`
- **Examples**: `RichContentRenderer.example.tsx`
- **Summary**: `RichContentRenderer.SUMMARY.md`

---

**Status**: Ready for Integration
**Last Updated**: 2025-11-19
