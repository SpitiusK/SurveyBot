import { test, expect } from '@playwright/test';

/**
 * E2E Test Suite: Branching Rule Creation
 * Tests the complete flow of creating a survey with branching rules through the admin panel
 * and verifies API responses are correct
 */

test.describe('Branching Rules Creation', () => {
  let surveyId: number;
  let questionIds: number[] = [];
  const baseUrl = process.env.BASE_URL || 'http://localhost:3002';
  const apiBaseUrl = process.env.API_BASE_URL || 'http://localhost:5000/api';

  test.beforeAll(async () => {
    // Set up any global state if needed
  });

  test('should create a survey with branching rules and verify persistence', async ({ page }) => {
    // Step 1: Navigate to admin panel
    await test.step('Navigate to admin panel', async () => {
      await page.goto(`${baseUrl}`);
      await expect(page).toHaveTitle(/Admin/i);
    });

    // Step 2: Create a new survey
    await test.step('Create survey', async () => {
      // Click "Create Survey" button
      await page.click('button:has-text("Create Survey")');

      // Wait for modal/form to appear
      await page.waitForSelector('input[placeholder*="Survey Title"]');

      // Fill in survey details
      await page.fill('input[placeholder*="Survey Title"]', 'Branching Rules Test - Playwright');
      await page.fill('textarea[placeholder*="Description"]', 'Testing branching rules via Playwright E2E test');

      // Click create button
      await page.click('button:has-text("Create")');

      // Wait for survey to be created and navigate to builder
      await page.waitForURL(/surveys\/\d+\/builder/);

      // Extract survey ID from URL
      const urlMatch = page.url().match(/surveys\/(\d+)/);
      if (urlMatch) {
        surveyId = parseInt(urlMatch[1]);
        console.log(`✓ Survey created with ID: ${surveyId}`);
      }
    });

    // Step 3: Add 4 questions to survey
    await test.step('Add 4 questions to survey', async () => {
      const questions = [
        {
          text: 'What is your name?',
          type: 'SingleChoice',
          options: ['Alice', 'Bob', 'Charlie', 'Diana'],
        },
        {
          text: 'Question 2',
          type: 'Text',
        },
        {
          text: 'Question 3',
          type: 'Text',
        },
        {
          text: 'Question 4',
          type: 'Text',
        },
      ];

      for (let i = 0; i < questions.length; i++) {
        const q = questions[i];

        // Click "Add Question" button
        await page.click('button:has-text("Add Question")');

        // Wait for question form
        await page.waitForSelector('textarea[placeholder*="Question"]');

        // Fill question text
        await page.fill('textarea[placeholder*="Question"]', q.text);

        // Select question type if not first (SingleChoice already selected by default in some cases)
        if (q.type !== 'SingleChoice') {
          await page.click('select[aria-label*="Type"]');
          await page.click(`option:has-text("${q.type}")`);
        }

        // Add options if needed
        if (q.options) {
          for (const option of q.options) {
            await page.click('button:has-text("Add Option")');
            await page.fill('input[placeholder*="Option"]', option);
          }
        }

        // Save question
        await page.click('button:has-text("Save Question")');

        // Wait for question to appear in list and capture ID from API response
        await page.waitForResponse(
          response => response.url().includes(`/questions`) && response.status() === 201
        );

        console.log(`✓ Question ${i + 1} added: ${q.text}`);
      }
    });

    // Step 4: Create branching rules
    await test.step('Create branching rules', async () => {
      // Get all questions from the API to get their IDs
      const surveyResponse = await page.request.get(
        `${apiBaseUrl}/surveys/${surveyId}`,
        {
          headers: {
            'Authorization': `Bearer ${await getTokenFromStorage(page)}`,
          },
        }
      );

      const surveyData = await surveyResponse.json();
      const questions = surveyData.data.questions;

      console.log(`✓ Retrieved ${questions.length} questions from API`);

      // Navigate to branching rules section
      await page.click('button:has-text("Branching")');

      // Rule 1: Q1 (Alice) -> Q3
      await test.step('Create Rule 1: Q1 (Alice) → Q3', async () => {
        await page.click('button:has-text("Add Rule")');

        // Select source question (Q1)
        await page.selectOption('select[aria-label*="Source"]', questions[0].id.toString());

        // Select condition operator
        await page.selectOption('select[aria-label*="Operator"]', 'Equals');

        // Set condition value
        await page.fill('input[aria-label*="Value"]', 'Alice');

        // Select target question (Q3)
        await page.selectOption('select[aria-label*="Target"]', questions[2].id.toString());

        // Create rule and capture API response
        const ruleResponse = page.waitForResponse(
          response => response.url().includes(`/branches`) && response.status() === 201
        );

        await page.click('button:has-text("Create Rule")');

        const response = await ruleResponse;
        const ruleData = await response.json();

        expect(response.status()).toBe(201);
        expect(ruleData.data.condition.operator).toBe('Equals');
        expect(ruleData.data.condition.values).toContain('Alice');

        console.log(`✓ Rule 1 created: ${JSON.stringify(ruleData.data)}`);
      });

      // Rule 2: Q1 (Bob) -> Q2
      await test.step('Create Rule 2: Q1 (Bob) → Q2', async () => {
        await page.click('button:has-text("Add Rule")');

        // Select source question (Q1)
        await page.selectOption('select[aria-label*="Source"]', questions[0].id.toString());

        // Select condition operator
        await page.selectOption('select[aria-label*="Operator"]', 'Equals');

        // Set condition value
        await page.fill('input[aria-label*="Value"]', 'Bob');

        // Select target question (Q2)
        await page.selectOption('select[aria-label*="Target"]', questions[1].id.toString());

        // Create rule and capture API response
        const ruleResponse = page.waitForResponse(
          response => response.url().includes(`/branches`) && response.status() === 201
        );

        await page.click('button:has-text("Create Rule")');

        const response = await ruleResponse;
        const ruleData = await response.json();

        expect(response.status()).toBe(201);
        expect(ruleData.data.condition.operator).toBe('Equals');
        expect(ruleData.data.condition.values).toContain('Bob');

        console.log(`✓ Rule 2 created: ${JSON.stringify(ruleData.data)}`);
      });
    });

    // Step 5: Publish survey
    await test.step('Publish survey', async () => {
      await page.click('button:has-text("Publish")');

      // Wait for publish confirmation
      await page.waitForResponse(
        response => response.url().includes(`/activate`) && response.status() === 200
      );

      console.log(`✓ Survey published`);
    });

    // Step 6: Verify branching rules in database
    await test.step('Verify branching rules in database', async () => {
      const surveyResponse = await page.request.get(
        `${apiBaseUrl}/surveys/${surveyId}`,
        {
          headers: {
            'Authorization': `Bearer ${await getTokenFromStorage(page)}`,
          },
        }
      );

      expect(surveyResponse.status()).toBe(200);

      const surveyData = await surveyResponse.json();
      const questions = surveyData.data.questions;

      // Check Q1 has 2 outgoing rules
      const q1OutgoingRules = questions[0].outgoingRules || [];
      expect(q1OutgoingRules.length).toBe(2);

      console.log(`✓ Q1 has ${q1OutgoingRules.length} outgoing rules`);

      // Verify rule details
      const rule1 = q1OutgoingRules.find((r: any) => r.targetQuestionId === questions[2].id);
      const rule2 = q1OutgoingRules.find((r: any) => r.targetQuestionId === questions[1].id);

      expect(rule1).toBeDefined();
      expect(rule1.condition.operator).toBe('Equals');
      expect(rule1.condition.values).toContain('Alice');

      expect(rule2).toBeDefined();
      expect(rule2.condition.operator).toBe('Equals');
      expect(rule2.condition.values).toContain('Bob');

      console.log(`✓ All branching rules verified in database`);
      console.log(`✓ Rule 1: Q1 (Alice) → Q${questions.indexOf(questions[2]) + 1}`);
      console.log(`✓ Rule 2: Q1 (Bob) → Q${questions.indexOf(questions[1]) + 1}`);
    });
  });

  test('should handle invalid branching rule creation', async ({ page }) => {
    // Create survey first
    await page.goto(`${baseUrl}`);

    // Test attempting to create rule without selecting all required fields
    await test.step('Validate required fields in branching rule form', async () => {
      // Click to start creating rule
      await page.click('button:has-text("Add Rule")');

      // Try to submit without filling in required fields
      const createButton = page.locator('button:has-text("Create Rule")');

      // Button should be disabled or form should show validation error
      const isDisabled = await createButton.isDisabled();
      expect(isDisabled).toBe(true);

      console.log(`✓ Form validation prevents creation without required fields`);
    });
  });

  test('should display API errors when branching rule creation fails', async ({ page }) => {
    await page.goto(`${baseUrl}`);

    // Intercept API call to simulate failure
    await page.route(`${apiBaseUrl}/surveys/*/questions/*/branches`, route => {
      route.abort('failed');
    });

    await test.step('Handle API failure gracefully', async () => {
      // Attempt to create rule
      await page.click('button:has-text("Add Rule")');
      await page.click('button:has-text("Create Rule")');

      // Should show error message
      await page.waitForSelector('text=/Error creating rule/i');

      console.log(`✓ Error handling working for API failures`);
    });
  });
});

/**
 * Helper function to get JWT token from localStorage
 */
async function getTokenFromStorage(page: any): Promise<string> {
  const token = await page.evaluate(() => {
    const data = localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
    return data;
  });

  return token || '';
}
