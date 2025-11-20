/**
 * Survey Builder Branching - E2E Tests
 *
 * This file contains Playwright E2E tests for the branching questions feature.
 *
 * Prerequisites:
 * 1. Install Playwright: npm install -D @playwright/test
 * 2. Install browsers: npx playwright install
 * 3. Configure playwright.config.ts (see BRANCHING_E2E_TESTS.md)
 * 4. Run tests: npx playwright test
 */

import { test, expect, Page } from '@playwright/test';

// Helper functions
async function login(page: Page, telegramId: string = '123456789') {
  await page.goto('/login');
  await page.fill('input[name="telegramId"]', telegramId);
  await page.fill('input[name="password"]', 'test123');
  await page.click('button[type="submit"]');
  await expect(page).toHaveURL(/dashboard/);
}

async function createBasicSurvey(page: Page, title: string) {
  await page.goto('/surveys/create');
  await page.fill('input[name="title"]', title);
  await page.click('button:has-text("Next: Add Questions")');
  await expect(page.locator('h5:has-text("Add Questions")')).toBeVisible();
}

async function addSingleChoiceQuestion(
  page: Page,
  text: string,
  options: string[]
) {
  await page.click('button:has-text("Add Question")');

  // Wait for dialog to open
  await expect(page.locator('h6:has-text("Add New Question")')).toBeVisible();

  // Select SingleChoice type
  await page.click('label:has-text("Single Choice")');

  // Fill question text
  await page.fill('input[name="questionText"]', text);

  // Add options
  for (let i = 0; i < options.length; i++) {
    const optionInput = page.locator(`input[placeholder*="Option ${i + 1}"]`);
    await optionInput.fill(options[i]);

    if (i < options.length - 1) {
      await page.click('button:has-text("Add Option")');
    }
  }

  // Save question
  await page.click('button[type="submit"]:has-text("Add Question")');

  // Wait for dialog to close
  await expect(page.locator('h6:has-text("Add New Question")')).not.toBeVisible();
}

async function addTextQuestion(page: Page, text: string) {
  await page.click('button:has-text("Add Question")');
  await expect(page.locator('h6:has-text("Add New Question")')).toBeVisible();

  // Text type should be selected by default
  await page.fill('input[name="questionText"]', text);
  await page.click('button[type="submit"]:has-text("Add Question")');

  await expect(page.locator('h6:has-text("Add New Question")')).not.toBeVisible();
}

test.describe('Survey Builder - Branching Questions', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('should create survey with branching questions', async ({ page }) => {
    // Step 1: Create survey with basic info
    await createBasicSurvey(page, 'Customer Satisfaction Survey');

    // Step 2: Add Question 1 - Age group (SingleChoice)
    await addSingleChoiceQuestion(
      page,
      'What is your age group?',
      ['Under 18', '18-65', 'Over 65']
    );

    // Step 3: Add Question 2 - Youth feedback
    await addTextQuestion(page, 'What features would you like to see for youth?');

    // Step 4: Add Question 3 - Adult feedback
    await addTextQuestion(page, 'What features would you like to see for adults?');

    // Step 5: Configure branching on Question 1
    const q1Card = page.locator('[data-testid="question-card"]').first();

    // Verify branching button exists for SingleChoice
    await expect(q1Card.locator('button[aria-label="Configure branching"]')).toBeVisible();

    // Click branching button
    await q1Card.locator('button[aria-label="Configure branching"]').click();

    // Step 6: Wait for branching editor dialog
    await expect(page.locator('h6:has-text("Create Branching Rule")')).toBeVisible();

    // Step 7: Configure first branching rule
    // Select operator
    await page.locator('label:has-text("Condition Operator")').click();
    await page.locator('li[role="option"]:has-text("Equals")').click();

    // Select value
    await page.locator('label:has-text("Answer Value")').click();
    await page.locator('li[role="option"]:has-text("Under 18")').click();

    // Select target question
    await page.locator('label:has-text("Jump to Question")').click();
    await page.locator('li[role="option"]:has-text("Q2:")').click();

    // Verify rule preview
    await expect(page.locator('text=/If answer.*equals.*"Under 18"/i')).toBeVisible();

    // Save rule
    await page.click('button:has-text("Create Rule")');

    // Step 8: Verify branching indicator appears
    await expect(q1Card.locator('text=/1 branch/i')).toBeVisible();

    // Step 9: Create another branching rule
    await q1Card.locator('button[aria-label="Configure branching"]').click();
    await expect(page.locator('h6:has-text("Create Branching Rule")')).toBeVisible();

    await page.locator('label:has-text("Condition Operator")').click();
    await page.locator('li[role="option"]:has-text("Equals")').click();

    await page.locator('label:has-text("Answer Value")').click();
    await page.locator('li[role="option"]:has-text("18-65")').click();

    await page.locator('label:has-text("Jump to Question")').click();
    await page.locator('li[role="option"]:has-text("Q3:")').click();

    await page.click('button:has-text("Create Rule")');

    // Step 10: Verify 2 branches indicator
    await expect(q1Card.locator('text=/2 branches/i')).toBeVisible();

    // Step 11: Navigate to review
    await page.click('button:has-text("Next: Review")');

    // Step 12: Verify survey in preview (branching rules shown in review)
    await expect(page.locator('h5:has-text("Review")')).toBeVisible();
    await expect(page.locator('text=/What is your age group/i')).toBeVisible();

    // Step 13: Publish survey
    await page.click('button:has-text("Publish Survey")');

    // Step 14: Verify success
    await expect(page.locator('text=/Survey.*Published.*Successfully/i')).toBeVisible();
  });

  test('should prevent self-reference in branching', async ({ page }) => {
    await createBasicSurvey(page, 'Test Survey');

    // Add a SingleChoice question
    await addSingleChoiceQuestion(
      page,
      'Test question?',
      ['Yes', 'No']
    );

    // Try to configure branching
    const q1Card = page.locator('[data-testid="question-card"]').first();
    await q1Card.locator('button[aria-label="Configure branching"]').click();

    // Open target dropdown
    await page.locator('label:has-text("Jump to Question")').click();

    // Count available options (should only be the placeholder)
    const options = page.locator('li[role="option"]');
    const count = await options.count();

    // Should only have "Select target question..." option (no Q1)
    expect(count).toBeLessThanOrEqual(1);
  });

  test('should show branching button only for SingleChoice questions', async ({ page }) => {
    await createBasicSurvey(page, 'Test Survey');

    // Add Text question
    await addTextQuestion(page, 'Text question?');

    // Add SingleChoice question
    await addSingleChoiceQuestion(
      page,
      'Choice question?',
      ['Yes', 'No']
    );

    // Add Rating question
    await page.click('button:has-text("Add Question")');
    await page.click('label:has-text("Rating")');
    await page.fill('input[name="questionText"]', 'Rating question?');
    await page.click('button[type="submit"]:has-text("Add Question")');

    // Verify branching button visibility
    const cards = page.locator('[data-testid="question-card"]');

    // Q1 (Text) - no branching button
    await expect(
      cards.nth(0).locator('button[aria-label="Configure branching"]')
    ).not.toBeVisible();

    // Q2 (SingleChoice) - has branching button
    await expect(
      cards.nth(1).locator('button[aria-label="Configure branching"]')
    ).toBeVisible();

    // Q3 (Rating) - no branching button
    await expect(
      cards.nth(2).locator('button[aria-label="Configure branching"]')
    ).not.toBeVisible();
  });

  test('should edit existing branching rule', async ({ page }) => {
    // Setup: Create survey with branching
    await createBasicSurvey(page, 'Edit Test Survey');

    await addSingleChoiceQuestion(
      page,
      'Pick one',
      ['Option A', 'Option B', 'Option C']
    );

    await addTextQuestion(page, 'Follow-up question');

    // Create initial branching rule
    const q1Card = page.locator('[data-testid="question-card"]').first();
    await q1Card.locator('button[aria-label="Configure branching"]').click();

    await page.locator('label:has-text("Condition Operator")').click();
    await page.locator('li[role="option"]:has-text("Equals")').click();

    await page.locator('label:has-text("Answer Value")').click();
    await page.locator('li[role="option"]:has-text("Option A")').click();

    await page.locator('label:has-text("Jump to Question")').click();
    await page.locator('li[role="option"]:has-text("Q2:")').click();

    await page.click('button:has-text("Create Rule")');

    // Verify rule created
    await expect(q1Card.locator('text=/1 branch/i')).toBeVisible();

    // Re-open to edit
    await q1Card.locator('button[aria-label="Configure branching"]').click();

    // Should show edit mode
    await expect(page.locator('h6:has-text("Edit Branching Rule")')).toBeVisible();

    // Change to "In" operator
    await page.locator('label:has-text("Condition Operator")').click();
    await page.locator('li[role="option"]:has-text("Is one of")').click();

    // Select multiple values
    await page.locator('label:has-text("Answer Values")').click();
    await page.locator('li[role="option"]:has-text("Option A")').click();
    await page.locator('li[role="option"]:has-text("Option B")').click();

    // Close dropdown
    await page.keyboard.press('Escape');

    // Verify preview updated
    await expect(page.locator('text=/If answer.*is one of/i')).toBeVisible();

    // Update rule
    await page.click('button:has-text("Update Rule")');

    // Verify still shows 1 branch
    await expect(q1Card.locator('text=/1 branch/i')).toBeVisible();
  });

  test('should delete branching rule', async ({ page }) => {
    // Setup: Create survey with branching
    await createBasicSurvey(page, 'Delete Test Survey');

    await addSingleChoiceQuestion(
      page,
      'Pick one',
      ['Yes', 'No']
    );

    await addTextQuestion(page, 'Follow-up');

    // Create branching rule
    const q1Card = page.locator('[data-testid="question-card"]').first();
    await q1Card.locator('button[aria-label="Configure branching"]').click();

    await page.locator('label:has-text("Condition Operator")').click();
    await page.locator('li[role="option"]:has-text("Equals")').click();

    await page.locator('label:has-text("Answer Value")').click();
    await page.locator('li[role="option"]:has-text("Yes")').click();

    await page.locator('label:has-text("Jump to Question")').click();
    await page.locator('li[role="option"]:has-text("Q2:")').click();

    await page.click('button:has-text("Create Rule")');

    // Verify rule created
    await expect(q1Card.locator('text=/1 branch/i')).toBeVisible();

    // Re-open and delete
    await q1Card.locator('button[aria-label="Configure branching"]').click();

    // Click delete button
    await page.click('button:has-text("Delete")');

    // Handle confirmation dialog
    page.on('dialog', dialog => dialog.accept());

    // Wait a moment for deletion
    await page.waitForTimeout(500);

    // Verify branch indicator removed
    await expect(q1Card.locator('text=/branch/i')).not.toBeVisible();
  });

  test('should display correct branching count', async ({ page }) => {
    await createBasicSurvey(page, 'Count Test Survey');

    await addSingleChoiceQuestion(
      page,
      'Pick',
      ['A', 'B', 'C']
    );

    await addTextQuestion(page, 'Q2');
    await addTextQuestion(page, 'Q3');

    const q1Card = page.locator('[data-testid="question-card"]').first();

    // Initially no branches
    await expect(q1Card.locator('text=/branch/i')).not.toBeVisible();

    // Add first rule
    await q1Card.locator('button[aria-label="Configure branching"]').click();
    await page.locator('label:has-text("Condition Operator")').click();
    await page.locator('li[role="option"]:has-text("Equals")').click();
    await page.locator('label:has-text("Answer Value")').click();
    await page.locator('li[role="option"]:has-text("A")').click();
    await page.locator('label:has-text("Jump to Question")').click();
    await page.locator('li[role="option"]:has-text("Q2:")').click();
    await page.click('button:has-text("Create Rule")');

    await expect(q1Card.locator('text=/1 branch/i')).toBeVisible();

    // Add second rule
    await q1Card.locator('button[aria-label="Configure branching"]').click();
    await page.locator('label:has-text("Condition Operator")').click();
    await page.locator('li[role="option"]:has-text("Equals")').click();
    await page.locator('label:has-text("Answer Value")').click();
    await page.locator('li[role="option"]:has-text("B")').click();
    await page.locator('label:has-text("Jump to Question")').click();
    await page.locator('li[role="option"]:has-text("Q3:")').click();
    await page.click('button:has-text("Create Rule")');

    await expect(q1Card.locator('text=/2 branches/i')).toBeVisible();
  });

  test('should validate required fields in branching editor', async ({ page }) => {
    await createBasicSurvey(page, 'Validation Test');

    await addSingleChoiceQuestion(
      page,
      'Test',
      ['Yes', 'No']
    );

    await addTextQuestion(page, 'Follow-up');

    const q1Card = page.locator('[data-testid="question-card"]').first();
    await q1Card.locator('button[aria-label="Configure branching"]').click();

    // Try to save without filling required fields
    await page.click('button:has-text("Create Rule")');

    // Should show validation errors
    await expect(page.locator('text=/required/i')).toBeVisible();
  });
});
