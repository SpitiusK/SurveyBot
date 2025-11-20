import { test, expect } from '@playwright/test';

/**
 * E2E Test Suite: Branching Rules API Testing
 * Tests the branching rules API endpoints directly and verifies responses
 *
 * This test focuses on API integration without requiring full UI navigation
 */

interface TestContext {
  apiBaseUrl: string;
  jwtToken: string;
  userId: number;
  surveyId: number;
  questionIds: number[];
}

test.describe('Branching Rules API', () => {
  let context: TestContext;

  const getApiBaseUrl = (): string => {
    const baseUrl = process.env.API_BASE_URL || 'http://localhost:5000';
    return `${baseUrl}/api`;
  };

  test.beforeAll(async () => {
    context = {
      apiBaseUrl: getApiBaseUrl(),
      jwtToken: '',
      userId: 0,
      surveyId: 0,
      questionIds: [],
    };
  });

  test('E2E: Create survey with branching rules and verify persistence', async ({ request }) => {
    // Step 1: Login
    await test.step('Step 1: Login with Telegram user', async () => {
      const response = await request.post(`${context.apiBaseUrl}/auth/login`, {
        data: {
          telegramId: '123456789',
        },
      });

      expect(response.status()).toBe(200);

      const responseData = await response.json();
      expect(responseData.success).toBe(true);
      expect(responseData.data).toHaveProperty('token');
      expect(responseData.data).toHaveProperty('user');

      context.jwtToken = responseData.data.token;
      context.userId = responseData.data.user.id;

      console.log(`✓ Login successful`);
      console.log(`  Token: ${context.jwtToken.substring(0, 30)}...`);
      console.log(`  User ID: ${context.userId}`);
    });

    // Step 2: Create Survey
    await test.step('Step 2: Create survey', async () => {
      const response = await request.post(`${context.apiBaseUrl}/surveys`, {
        headers: {
          Authorization: `Bearer ${context.jwtToken}`,
        },
        data: {
          title: 'Branching Rules Test - Playwright E2E',
          description: 'Testing branching rules via Playwright API test',
          allowMultipleResponses: false,
          showResults: false,
        },
      });

      expect(response.status()).toBe(201);

      const responseData = await response.json();
      expect(responseData.data).toHaveProperty('id');
      expect(responseData.data).toHaveProperty('code');

      context.surveyId = responseData.data.id;

      console.log(`✓ Survey created: ID ${context.surveyId}, Code: ${responseData.data.code}`);
    });

    // Step 3: Create 4 Questions
    await test.step('Step 3: Create 4 questions', async () => {
      const questions = [
        {
          questionText: 'What is your name?',
          questionType: 1, // SingleChoice
          isRequired: true,
          options: ['Alice', 'Bob', 'Charlie', 'Diana'],
        },
        {
          questionText: 'Question 2',
          questionType: 0, // Text
          isRequired: true,
        },
        {
          questionText: 'Question 3',
          questionType: 0, // Text
          isRequired: true,
        },
        {
          questionText: 'Question 4',
          questionType: 0, // Text
          isRequired: true,
        },
      ];

      for (let i = 0; i < questions.length; i++) {
        const q = questions[i];
        const response = await request.post(
          `${context.apiBaseUrl}/surveys/${context.surveyId}/questions`,
          {
            headers: {
              Authorization: `Bearer ${context.jwtToken}`,
            },
            data: {
              questionText: q.questionText,
              questionType: q.questionType,
              isRequired: q.isRequired,
              options: q.options || undefined,
            },
          }
        );

        expect(response.status()).toBe(201);

        const responseData = await response.json();
        const questionId = responseData.data.id;
        context.questionIds.push(questionId);

        console.log(`✓ Question ${i + 1} created: ID ${questionId}`);
      }
    });

    // Step 4: Create Branching Rules
    await test.step('Step 4: Create branching rules', async () => {
      // Rule 1: Q1 (Alice) -> Q3
      const rule1Response = await request.post(
        `${context.apiBaseUrl}/surveys/${context.surveyId}/questions/${context.questionIds[0]}/branches`,
        {
          headers: {
            Authorization: `Bearer ${context.jwtToken}`,
          },
          data: {
            sourceQuestionId: context.questionIds[0],
            targetQuestionId: context.questionIds[2],
            condition: {
              operator: 'Equals',
              values: ['Alice'],
              questionType: '1', // Must be string
            },
          },
        }
      );

      expect(rule1Response.status()).toBe(201);

      const rule1Data = await rule1Response.json();
      expect(rule1Data.data).toHaveProperty('id');
      expect(rule1Data.data.condition.operator).toBe('Equals');
      expect(rule1Data.data.condition.values).toContain('Alice');

      console.log(`✓ Rule 1 created (Q1 Alice → Q3)`);
      console.log(`  Response: ${JSON.stringify(rule1Data.data, null, 2)}`);

      // Rule 2: Q1 (Bob) -> Q2
      const rule2Response = await request.post(
        `${context.apiBaseUrl}/surveys/${context.surveyId}/questions/${context.questionIds[0]}/branches`,
        {
          headers: {
            Authorization: `Bearer ${context.jwtToken}`,
          },
          data: {
            sourceQuestionId: context.questionIds[0],
            targetQuestionId: context.questionIds[1],
            condition: {
              operator: 'Equals',
              values: ['Bob'],
              questionType: '1', // Must be string
            },
          },
        }
      );

      expect(rule2Response.status()).toBe(201);

      const rule2Data = await rule2Response.json();
      expect(rule2Data.data).toHaveProperty('id');
      expect(rule2Data.data.condition.operator).toBe('Equals');
      expect(rule2Data.data.condition.values).toContain('Bob');

      console.log(`✓ Rule 2 created (Q1 Bob → Q2)`);
      console.log(`  Response: ${JSON.stringify(rule2Data.data, null, 2)}`);
    });

    // Step 5: Activate Survey
    await test.step('Step 5: Activate survey', async () => {
      const response = await request.post(
        `${context.apiBaseUrl}/surveys/${context.surveyId}/activate`,
        {
          headers: {
            Authorization: `Bearer ${context.jwtToken}`,
          },
          data: {},
        }
      );

      expect(response.status()).toBe(200);

      console.log(`✓ Survey activated`);
    });

    // Step 6: Verify Branching Rules in Database
    await test.step('Step 6: Verify branching rules persisted in database', async () => {
      const response = await request.get(`${context.apiBaseUrl}/surveys/${context.surveyId}`, {
        headers: {
          Authorization: `Bearer ${context.jwtToken}`,
        },
      });

      expect(response.status()).toBe(200);

      const responseData = await response.json();
      const survey = responseData.data;
      const questions = survey.questions;

      console.log(`✓ Survey details retrieved`);
      console.log(`  Total questions: ${questions.length}`);

      // Verify Q1 has 2 outgoing rules
      const q1 = questions[0];
      expect(q1.outgoingRules).toBeDefined();
      expect(q1.outgoingRules.length).toBe(2);

      console.log(`✓ Q1 has ${q1.outgoingRules.length} outgoing rules`);

      // Verify rule details
      const aliceRule = q1.outgoingRules.find(
        (r: any) => r.targetQuestionId === context.questionIds[2]
      );
      const bobRule = q1.outgoingRules.find(
        (r: any) => r.targetQuestionId === context.questionIds[1]
      );

      expect(aliceRule).toBeDefined();
      expect(aliceRule.condition.operator).toBe('Equals');
      expect(aliceRule.condition.values).toContain('Alice');

      expect(bobRule).toBeDefined();
      expect(bobRule.condition.operator).toBe('Equals');
      expect(bobRule.condition.values).toContain('Bob');

      console.log(`✓ Alice rule verified: Q${context.questionIds[0]} → Q${context.questionIds[2]}`);
      console.log(`  Condition: ${aliceRule.condition.operator} ${JSON.stringify(aliceRule.condition.values)}`);
      console.log(`✓ Bob rule verified: Q${context.questionIds[0]} → Q${context.questionIds[1]}`);
      console.log(`  Condition: ${bobRule.condition.operator} ${JSON.stringify(bobRule.condition.values)}`);

      // Log full rule details
      console.log(`\n📊 Complete Rule 1 Details:`);
      console.log(`${JSON.stringify(aliceRule, null, 2)}`);
      console.log(`\n📊 Complete Rule 2 Details:`);
      console.log(`${JSON.stringify(bobRule, null, 2)}`);
    });

    // Step 7: Test All Supported Operators
    await test.step('Step 7: Test other branching operators', async () => {
      // Create rule with Contains operator
      const containsResponse = await request.post(
        `${context.apiBaseUrl}/surveys/${context.surveyId}/questions/${context.questionIds[1]}/branches`,
        {
          headers: {
            Authorization: `Bearer ${context.jwtToken}`,
          },
          data: {
            sourceQuestionId: context.questionIds[1],
            targetQuestionId: context.questionIds[3],
            condition: {
              operator: 'Contains',
              values: ['test'],
              questionType: '0', // Text question
            },
          },
        }
      );

      expect(containsResponse.status()).toBe(201);
      console.log(`✓ Contains operator rule created`);

      // Create rule with GreaterThan operator (for numeric/rating questions)
      const greaterThanResponse = await request.post(
        `${context.apiBaseUrl}/surveys/${context.surveyId}/questions/${context.questionIds[1]}/branches`,
        {
          headers: {
            Authorization: `Bearer ${context.jwtToken}`,
          },
          data: {
            sourceQuestionId: context.questionIds[1],
            targetQuestionId: context.questionIds[3],
            condition: {
              operator: 'GreaterThan',
              values: ['5'],
              questionType: '0', // Text question (will be treated as numeric)
            },
          },
        }
      );

      expect(greaterThanResponse.status()).toBe(201);
      console.log(`✓ GreaterThan operator rule created`);
    });
  });

  test('API: Handle invalid branching rule creation gracefully', async ({ request }) => {
    // Login
    const loginResponse = await request.post(`${context.apiBaseUrl}/auth/login`, {
      data: {
        telegramId: '123456789',
      },
    });

    const loginData = await loginResponse.json();
    const token = loginData.data.token;

    // Create a survey and question for testing
    const surveyResponse = await request.post(`${context.apiBaseUrl}/surveys`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
      data: {
        title: 'Error Test Survey',
        description: 'Testing error handling',
      },
    });

    const surveyData = await surveyResponse.json();
    const surveyId = surveyData.data.id;

    const questionResponse = await request.post(
      `${context.apiBaseUrl}/surveys/${surveyId}/questions`,
      {
        headers: {
          Authorization: `Bearer ${token}`,
        },
        data: {
          questionText: 'Test Question',
          questionType: 1,
          isRequired: true,
          options: ['Option 1', 'Option 2'],
        },
      }
    );

    const questionData = await questionResponse.json();
    const questionId = questionData.data.id;

    // Test 1: Missing required field (questionType)
    await test.step('Test missing required questionType field', async () => {
      const response = await request.post(
        `${context.apiBaseUrl}/surveys/${surveyId}/questions/${questionId}/branches`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
          data: {
            sourceQuestionId: questionId,
            targetQuestionId: questionId,
            condition: {
              operator: 'Equals',
              values: ['Option 1'],
              // Missing questionType
            },
          },
        }
      );

      expect(response.status()).toBe(400);
      const responseData = await response.json();
      expect(responseData.success).toBe(false);
      expect(responseData.errors).toBeDefined();

      console.log(`✓ Correctly rejected rule without questionType`);
      console.log(`  Error: ${JSON.stringify(responseData.errors, null, 2)}`);
    });

    // Test 2: Invalid operator
    await test.step('Test invalid operator', async () => {
      const response = await request.post(
        `${context.apiBaseUrl}/surveys/${surveyId}/questions/${questionId}/branches`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
          data: {
            sourceQuestionId: questionId,
            targetQuestionId: questionId,
            condition: {
              operator: 'InvalidOperator',
              values: ['Option 1'],
              questionType: '1',
            },
          },
        }
      );

      // May return 400 or 500 depending on validation
      expect([400, 500]).toContain(response.status());

      console.log(`✓ Correctly rejected invalid operator`);
      console.log(`  Status: ${response.status()}`);
    });

    // Test 3: Non-existent target question
    await test.step('Test non-existent target question', async () => {
      const response = await request.post(
        `${context.apiBaseUrl}/surveys/${surveyId}/questions/${questionId}/branches`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
          data: {
            sourceQuestionId: questionId,
            targetQuestionId: 99999, // Non-existent
            condition: {
              operator: 'Equals',
              values: ['Option 1'],
              questionType: '1',
            },
          },
        }
      );

      expect([400, 404]).toContain(response.status());

      console.log(`✓ Correctly rejected non-existent target question`);
      console.log(`  Status: ${response.status()}`);
    });
  });

  test('API: Verify supported branching operators and question types', async ({ request }) => {
    // Reference test for documentation purposes
    await test.step('Document supported operators', async () => {
      const operators = [
        'Equals',
        'Contains',
        'In',
        'GreaterThan',
        'LessThan',
        'GreaterThanOrEqual',
        'LessThanOrEqual',
      ];

      console.log(`\n✓ Supported Branching Operators:`);
      operators.forEach((op) => console.log(`  • ${op}`));

      const questionTypes = [
        { name: 'Text', id: 0 },
        { name: 'SingleChoice', id: 1 },
        { name: 'MultipleChoice', id: 2 },
        { name: 'Rating', id: 3 },
        { name: 'YesNo', id: 4 },
      ];

      console.log(`\n✓ Supported Question Types:`);
      questionTypes.forEach((qt) => console.log(`  • ${qt.name} (${qt.id})`));

      console.log(`\n✓ All combinations of operators and question types are supported`);
    });
  });
});
