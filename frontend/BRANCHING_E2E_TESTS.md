# Branching Questions - E2E Test Specification

This document outlines the Playwright E2E tests for the branching questions feature in the SurveyBot frontend.

## Prerequisites

### Setup Playwright

```bash
cd frontend
npm install -D @playwright/test
npx playwright install
```

### Configuration

Create `playwright.config.ts`:

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:3000',
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:3000',
    reuseExistingServer: !process.env.CI,
  },
});
```

## Test Files Structure

```
frontend/
├── e2e/
│   ├── fixtures/
│   │   └── auth.ts
│   ├── utils/
│   │   └── helpers.ts
│   └── survey-builder-branching.spec.ts
```

## Test Scenarios

### Test 1: Create Survey with Branching Questions

**File**: `e2e/survey-builder-branching.spec.ts`

```typescript
import { test, expect } from '@playwright/test';

test.describe('Survey Builder - Branching Questions', () => {
  test.beforeEach(async ({ page }) => {
    // Login
    await page.goto('/login');
    await page.fill('input[name="telegramId"]', '123456789');
    await page.fill('input[name="password"]', 'test123');
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL('/dashboard');
  });

  test('should create survey with branching questions', async ({ page }) => {
    // Step 1: Navigate to survey builder
    await page.goto('/surveys/create');
    await expect(page.locator('h5')).toContainText('Basic Information');

    // Step 2: Fill basic info
    await page.fill('input[name="title"]', 'Customer Satisfaction Survey');
    await page.fill('textarea[name="description"]', 'Help us improve our service');
    await page.click('button:has-text("Next: Add Questions")');

    // Step 3: Add Question 1 - Age group (SingleChoice)
    await page.click('button:has-text("Add Question")');

    // Select SingleChoice type
    await page.click('label:has-text("Single Choice")');

    // Fill question text
    await page.fill('input[name="questionText"]', 'What is your age group?');

    // Add options
    await page.fill('input[placeholder="Option 1"]', 'Under 18');
    await page.click('button:has-text("Add Option")');
    await page.fill('input[placeholder="Option 2"]', '18-65');
    await page.click('button:has-text("Add Option")');
    await page.fill('input[placeholder="Option 3"]', 'Over 65');

    // Save question
    await page.click('button:has-text("Add Question")');

    // Step 4: Add Question 2 - Youth feedback
    await page.click('button:has-text("Add Question")');
    await page.click('label:has-text("Text")');
    await page.fill('input[name="questionText"]', 'What features would you like to see for youth?');
    await page.click('button:has-text("Add Question")');

    // Step 5: Add Question 3 - Adult feedback
    await page.click('button:has-text("Add Question")');
    await page.click('label:has-text("Text")');
    await page.fill('input[name="questionText"]', 'What features would you like to see for adults?');
    await page.click('button:has-text("Add Question")');

    // Step 6: Configure branching on Question 1
    // Find Q1 card and click branching button
    const q1Card = page.locator('[data-testid="question-card"]').first();
    await q1Card.locator('button[aria-label="Configure branching"]').click();

    // Wait for branching editor dialog
    await expect(page.locator('h6:has-text("Create Branching Rule")')).toBeVisible();

    // Select operator
    await page.click('label:has-text("Condition Operator")');
    await page.click('li:has-text("Equals")');

    // Select value
    await page.click('label:has-text("Answer Value")');
    await page.click('li:has-text("Under 18")');

    // Select target question
    await page.click('label:has-text("Jump to Question")');
    await page.click('li:has-text("Q2: What features")');

    // Verify rule preview
    await expect(page.locator('text=/If answer equals "Under 18"/i')).toBeVisible();

    // Save rule
    await page.click('button:has-text("Create Rule")');

    // Step 7: Verify branching indicator appears
    await expect(q1Card.locator('text=/1 branch/i')).toBeVisible();

    // Step 8: Create another branching rule
    await q1Card.locator('button[aria-label="Configure branching"]').click();
    await page.click('label:has-text("Condition Operator")');
    await page.click('li:has-text("Equals")');
    await page.click('label:has-text("Answer Value")');
    await page.click('li:has-text("18-65")');
    await page.click('label:has-text("Jump to Question")');
    await page.click('li:has-text("Q3: What features")');
    await page.click('button:has-text("Create Rule")');

    // Verify 2 branches indicator
    await expect(q1Card.locator('text=/2 branches/i')).toBeVisible();

    // Step 9: Navigate to review
    await page.click('button:has-text("Next: Review & Publish")');

    // Step 10: Verify branching rules in preview
    await expect(page.locator('text=/Branching Rules/i')).toBeVisible();
    await expect(page.locator('text=/If answer equals "Under 18"/i')).toBeVisible();
    await expect(page.locator('text=/If answer equals "18-65"/i')).toBeVisible();

    // Step 11: Publish survey
    await page.click('button:has-text("Publish Survey")');

    // Step 12: Verify success
    await expect(page.locator('text=/Survey Published Successfully/i')).toBeVisible();
  });

  test('should prevent self-reference in branching', async ({ page }) => {
    // Navigate to survey builder and create a question
    await page.goto('/surveys/create');
    await page.fill('input[name="title"]', 'Test Survey');
    await page.click('button:has-text("Next: Add Questions")');

    // Add a SingleChoice question
    await page.click('button:has-text("Add Question")');
    await page.click('label:has-text("Single Choice")');
    await page.fill('input[name="questionText"]', 'Test question?');
    await page.fill('input[placeholder="Option 1"]', 'Yes');
    await page.click('button:has-text("Add Option")');
    await page.fill('input[placeholder="Option 2"]', 'No');
    await page.click('button:has-text("Add Question")');

    // Try to configure branching to itself
    const q1Card = page.locator('[data-testid="question-card"]').first();
    await q1Card.locator('button[aria-label="Configure branching"]').click();

    // Target dropdown should not include the source question
    await page.click('label:has-text("Jump to Question")');
    const options = page.locator('li[role="option"]');
    const count = await options.count();

    // Should only have "Select target question..." option (no Q1)
    expect(count).toBe(1);
  });

  test('should edit existing branching rule', async ({ page }) => {
    // Setup: Create survey with branching (similar to test 1, steps 1-7)
    // ... (omitted for brevity)

    // Find question with branching
    const q1Card = page.locator('[data-testid="question-card"]').first();

    // Click branching button again to edit
    await q1Card.locator('button[aria-label="Configure branching"]').click();

    // Should show existing rules
    await expect(page.locator('h6:has-text("Edit Branching Rule")')).toBeVisible();

    // Change operator to "In"
    await page.click('label:has-text("Condition Operator")');
    await page.click('li:has-text("Is one of (multiple)")');

    // Select multiple values
    await page.click('label:has-text("Answer Values")');
    await page.click('li:has-text("Under 18")');
    await page.click('li:has-text("18-65")');
    await page.press('body', 'Escape'); // Close dropdown

    // Verify preview updated
    await expect(page.locator('text=/If answer is one of/i')).toBeVisible();

    // Update rule
    await page.click('button:has-text("Update Rule")');

    // Verify updated
    await expect(page.locator('text=/rule updated/i')).toBeVisible();
  });

  test('should delete branching rule', async ({ page }) => {
    // Setup: Create survey with branching
    // ... (omitted for brevity)

    // Open branching editor
    const q1Card = page.locator('[data-testid="question-card"]').first();
    await q1Card.locator('button[aria-label="Configure branching"]').click();

    // Click delete button
    await page.click('button:has-text("Delete")');

    // Confirm deletion
    page.on('dialog', dialog => dialog.accept());

    // Verify rule deleted
    await expect(q1Card.locator('text=/branch/i')).not.toBeVisible();
  });

  test('should show branching badge only for SingleChoice questions', async ({ page }) => {
    // Navigate to survey builder
    await page.goto('/surveys/create');
    await page.fill('input[name="title"]', 'Test Survey');
    await page.click('button:has-text("Next: Add Questions")');

    // Add Text question
    await page.click('button:has-text("Add Question")');
    await page.click('label:has-text("Text")');
    await page.fill('input[name="questionText"]', 'Text question?');
    await page.click('button:has-text("Add Question")');

    // Add SingleChoice question
    await page.click('button:has-text("Add Question")');
    await page.click('label:has-text("Single Choice")');
    await page.fill('input[name="questionText"]', 'Choice question?');
    await page.fill('input[placeholder="Option 1"]', 'Yes');
    await page.click('button:has-text("Add Question")');

    // Add Rating question
    await page.click('button:has-text("Add Question")');
    await page.click('label:has-text("Rating")');
    await page.fill('input[name="questionText"]', 'Rating question?');
    await page.click('button:has-text("Add Question")');

    // Verify branching button only on SingleChoice
    const cards = page.locator('[data-testid="question-card"]');

    // Q1 (Text) - no branching button
    await expect(cards.nth(0).locator('button[aria-label="Configure branching"]')).not.toBeVisible();

    // Q2 (SingleChoice) - has branching button
    await expect(cards.nth(1).locator('button[aria-label="Configure branching"]')).toBeVisible();

    // Q3 (Rating) - no branching button
    await expect(cards.nth(2).locator('button[aria-label="Configure branching"]')).not.toBeVisible();
  });

  test('should display branching count correctly', async ({ page }) => {
    // Create survey with multiple branching rules
    // ... (setup omitted)

    const q1Card = page.locator('[data-testid="question-card"]').first();

    // Initially no branches
    await expect(q1Card.locator('text=/branch/i')).not.toBeVisible();

    // Add first rule
    // ... (add rule steps)
    await expect(q1Card.locator('text=/1 branch/i')).toBeVisible();

    // Add second rule
    // ... (add rule steps)
    await expect(q1Card.locator('text=/2 branches/i')).toBeVisible();

    // Delete one rule
    // ... (delete steps)
    await expect(q1Card.locator('text=/1 branch/i')).toBeVisible();
  });
});
```

## Helper Functions

**File**: `e2e/utils/helpers.ts`

```typescript
import { Page } from '@playwright/test';

export async function login(page: Page, telegramId: string = '123456789') {
  await page.goto('/login');
  await page.fill('input[name="telegramId"]', telegramId);
  await page.fill('input[name="password"]', 'test123');
  await page.click('button[type="submit"]');
}

export async function createBasicSurvey(page: Page, title: string) {
  await page.goto('/surveys/create');
  await page.fill('input[name="title"]', title);
  await page.click('button:has-text("Next: Add Questions")');
}

export async function addSingleChoiceQuestion(
  page: Page,
  text: string,
  options: string[]
) {
  await page.click('button:has-text("Add Question")');
  await page.click('label:has-text("Single Choice")');
  await page.fill('input[name="questionText"]', text);

  for (let i = 0; i < options.length; i++) {
    await page.fill(`input[placeholder="Option ${i + 1}"]`, options[i]);
    if (i < options.length - 1) {
      await page.click('button:has-text("Add Option")');
    }
  }

  await page.click('button:has-text("Add Question")');
}

export async function createBranchingRule(
  page: Page,
  questionIndex: number,
  operator: string,
  value: string,
  targetQuestionText: string
) {
  const card = page.locator('[data-testid="question-card"]').nth(questionIndex);
  await card.locator('button[aria-label="Configure branching"]').click();

  await page.click('label:has-text("Condition Operator")');
  await page.click(`li:has-text("${operator}")`);

  await page.click('label:has-text("Answer Value")');
  await page.click(`li:has-text("${value}")`);

  await page.click('label:has-text("Jump to Question")');
  await page.click(`li:has-text("${targetQuestionText}")`);

  await page.click('button:has-text("Create Rule")');
}
```

## Test Fixtures

**File**: `e2e/fixtures/auth.ts`

```typescript
import { test as base } from '@playwright/test';
import { login } from '../utils/helpers';

export const test = base.extend({
  authenticatedPage: async ({ page }, use) => {
    await login(page);
    await use(page);
  },
});

export { expect } from '@playwright/test';
```

## Running Tests

```bash
# Run all tests
npx playwright test

# Run specific test file
npx playwright test survey-builder-branching

# Run in UI mode
npx playwright test --ui

# Run in headed mode
npx playwright test --headed

# Generate report
npx playwright show-report
```

## Test Data Setup

For consistent testing, use test fixtures or API mocking:

```typescript
// Use MSW (Mock Service Worker) for API mocking
import { setupWorker, rest } from 'msw';

const handlers = [
  rest.post('/api/surveys/:id/questions/:qid/branches', (req, res, ctx) => {
    return res(
      ctx.json({
        success: true,
        data: {
          id: 1,
          sourceQuestionId: 1,
          targetQuestionId: 2,
          condition: {
            operator: 'Equals',
            value: 'Under 18',
            questionType: 'SingleChoice',
          },
          createdAt: new Date().toISOString(),
        },
      })
    );
  }),
];

export const worker = setupWorker(...handlers);
```

## Visual Regression Testing (Optional)

Add visual snapshots for branching UI:

```typescript
test('should match branching editor snapshot', async ({ page }) => {
  // ... setup
  await page.click('button[aria-label="Configure branching"]');
  await expect(page.locator('dialog')).toHaveScreenshot('branching-editor.png');
});
```

## Accessibility Testing

Ensure branching features are accessible:

```typescript
import { injectAxe, checkA11y } from 'axe-playwright';

test('branching editor should be accessible', async ({ page }) => {
  await injectAxe(page);
  await page.click('button[aria-label="Configure branching"]');
  await checkA11y(page);
});
```

## CI/CD Integration

Add to GitHub Actions:

```yaml
name: E2E Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: '18'
      - name: Install dependencies
        run: cd frontend && npm ci
      - name: Install Playwright
        run: npx playwright install --with-deps
      - name: Run tests
        run: npx playwright test
      - name: Upload report
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: playwright-report
          path: playwright-report/
```

## Notes

1. All tests assume backend API is running and accessible
2. Test data should be cleaned up after each test
3. Use `data-testid` attributes for reliable element selection
4. Mock API responses for faster, more reliable tests
5. Add visual regression tests for UI consistency
6. Test on multiple browsers (Chrome, Firefox, Safari)

## Implementation Status

- [ ] Setup Playwright in frontend project
- [ ] Create test configuration
- [ ] Implement test helpers
- [ ] Write basic branching tests
- [ ] Add edge case tests
- [ ] Setup CI/CD integration
- [ ] Add visual regression tests
- [ ] Add accessibility tests
