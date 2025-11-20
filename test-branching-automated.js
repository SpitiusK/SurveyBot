#!/usr/bin/env node

/**
 * Automated Branching Rules Test
 * Tests survey creation with branching rules and captures all debug logs
 *
 * Usage: node test-branching-automated.js
 */

const http = require('http');
const https = require('https');

const API_BASE = 'http://localhost:5000/api';
const TELEGRAM_USER_ID = '123456789'; // Test user ID

// Colors for console output
const colors = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  red: '\x1b[31m',
  cyan: '\x1b[36m',
  gray: '\x1b[90m',
};

const log = {
  section: (msg) => console.log(`\n${colors.bright}${colors.cyan}=== ${msg} ===${colors.reset}`),
  success: (msg) => console.log(`${colors.green}✓ ${msg}${colors.reset}`),
  error: (msg) => console.log(`${colors.red}✗ ${msg}${colors.reset}`),
  info: (msg) => console.log(`${colors.cyan}ℹ ${msg}${colors.reset}`),
  warn: (msg) => console.log(`${colors.yellow}⚠ ${msg}${colors.reset}`),
  debug: (msg) => console.log(`${colors.gray}  ${msg}${colors.reset}`),
};

// HTTP request helper
function makeRequest(method, path, body = null, token = null) {
  return new Promise((resolve, reject) => {
    const url = new URL(API_BASE + path);
    const isHttps = url.protocol === 'https:';
    const client = isHttps ? https : http;

    const options = {
      hostname: url.hostname,
      port: url.port || (isHttps ? 443 : 80),
      path: url.pathname + url.search,
      method: method,
      headers: {
        'Content-Type': 'application/json',
      },
    };

    if (token) {
      options.headers['Authorization'] = `Bearer ${token}`;
    }

    const req = client.request(options, (res) => {
      let data = '';
      res.on('data', (chunk) => (data += chunk));
      res.on('end', () => {
        try {
          const parsed = data ? JSON.parse(data) : {};
          resolve({ status: res.statusCode, data: parsed });
        } catch (e) {
          resolve({ status: res.statusCode, data: data });
        }
      });
    });

    req.on('error', reject);
    if (body) {
      req.write(JSON.stringify(body));
    }
    req.end();
  });
}

async function testBranchingRuleCreation() {
  log.section('BRANCHING RULES CREATION TEST');

  let jwtToken = null;
  let userId = null;

  try {
    // Step 1: Login
    log.section('Step 1: Login with Telegram User');
    const loginResponse = await makeRequest('POST', '/auth/login', {
      telegramId: TELEGRAM_USER_ID,
    });

    if (loginResponse.status === 200 && loginResponse.data.data?.token) {
      jwtToken = loginResponse.data.data.token;
      userId = loginResponse.data.data.user?.id;
      log.success(`Login successful. Token: ${jwtToken.substring(0, 20)}...`);
      log.info(`User ID: ${userId}`);
    } else {
      log.error(`Login failed: ${JSON.stringify(loginResponse.data)}`);
      return;
    }

    // Step 2: Create Survey
    log.section('Step 2: Create Survey');
    const surveyResponse = await makeRequest('POST', '/surveys', {
      title: 'Branching Test Survey - Automated',
      description: 'Testing branching rules persistence',
      allowMultipleResponses: false,
      showResults: false,
    }, jwtToken);

    let surveyId = null;
    if (surveyResponse.status === 201 && surveyResponse.data.data?.id) {
      surveyId = surveyResponse.data.data.id;
      log.success(`Survey created: ID ${surveyId}`);
      log.debug(`Title: ${surveyResponse.data.data.title}`);
    } else {
      log.error(`Survey creation failed: ${JSON.stringify(surveyResponse.data)}`);
      return;
    }

    // Step 3: Create 4 Questions
    log.section('Step 3: Create 4 Questions');
    const questions = [];

    const questionConfigs = [
      {
        text: 'What is your name?',
        type: 1, // SingleChoice
        options: ['Alice', 'Bob', 'Charlie', 'Diana'],
        required: true,
      },
      {
        text: 'Question 2',
        type: 0, // Text
        required: true,
      },
      {
        text: 'Question 3',
        type: 0, // Text
        required: true,
      },
      {
        text: 'Question 4',
        type: 0, // Text
        required: true,
      },
    ];

    for (let i = 0; i < questionConfigs.length; i++) {
      const config = questionConfigs[i];
      const qResponse = await makeRequest(
        'POST',
        `/surveys/${surveyId}/questions`,
        {
          questionText: config.text,
          questionType: config.type,
          isRequired: config.required,
          options: config.options || undefined,
        },
        jwtToken
      );

      if (qResponse.status === 201 && qResponse.data.data?.id) {
        questions.push(qResponse.data.data);
        log.success(`Q${i + 1} created: ID ${qResponse.data.data.id}`);
      } else {
        log.error(`Q${i + 1} creation failed: ${JSON.stringify(qResponse.data)}`);
      }
    }

    log.info(`Total questions created: ${questions.length}`);

    // Step 4: Create Branching Rules
    log.section('Step 4: Create Branching Rules');

    // Rule 1: Q1 (Alice) -> Q3
    if (questions.length >= 3) {
      const rule1 = {
        sourceQuestionId: questions[0].id,
        targetQuestionId: questions[2].id,
        condition: {
          operator: 'Equals',
          values: ['Alice'],
          questionType: questions[0].questionType.toString(),
        },
      };

      const rule1Response = await makeRequest(
        'POST',
        `/surveys/${surveyId}/questions/${questions[0].id}/branches`,
        rule1,
        jwtToken
      );

      if (rule1Response.status === 201) {
        log.success(`Rule 1 created: Q1 (Alice) → Q3`);
        log.debug(`Response: ${JSON.stringify(rule1Response.data)}`);
      } else {
        log.error(`Rule 1 failed: ${JSON.stringify(rule1Response.data)}`);
        log.warn(`Status: ${rule1Response.status}`);
      }
    }

    // Rule 2: Q1 (Bob) -> Q2
    if (questions.length >= 2) {
      const rule2 = {
        sourceQuestionId: questions[0].id,
        targetQuestionId: questions[1].id,
        condition: {
          operator: 'Equals',
          values: ['Bob'],
          questionType: questions[0].questionType.toString(),
        },
      };

      const rule2Response = await makeRequest(
        'POST',
        `/surveys/${surveyId}/questions/${questions[0].id}/branches`,
        rule2,
        jwtToken
      );

      if (rule2Response.status === 201) {
        log.success(`Rule 2 created: Q1 (Bob) → Q2`);
        log.debug(`Response: ${JSON.stringify(rule2Response.data)}`);
      } else {
        log.error(`Rule 2 failed: ${JSON.stringify(rule2Response.data)}`);
      }
    }

    // Step 5: Activate Survey
    log.section('Step 5: Activate Survey');
    const activateResponse = await makeRequest(
      'POST',
      `/surveys/${surveyId}/activate`,
      {},
      jwtToken
    );

    if (activateResponse.status === 200) {
      log.success(`Survey activated`);
    } else {
      log.error(`Survey activation failed: ${JSON.stringify(activateResponse.data)}`);
    }

    // Step 6: Verify Branching Rules in Database
    log.section('Step 6: Verify Branching Rules');
    const surveyDetailsResponse = await makeRequest(
      'GET',
      `/surveys/${surveyId}`,
      null,
      jwtToken
    );

    if (surveyDetailsResponse.status === 200) {
      const survey = surveyDetailsResponse.data.data;
      const branchingRules = survey.questions?.flatMap(q => q.outgoingRules || []) || [];

      log.info(`Survey Details Retrieved`);
      log.debug(`Total Questions: ${survey.questions?.length || 0}`);
      log.debug(`Branching Rules Found: ${branchingRules.length}`);
      log.debug(`Full Survey Response: ${JSON.stringify(survey, null, 2)}`);
      log.debug(`First Question Details: ${JSON.stringify(survey.questions?.[0], null, 2)}`);

      if (branchingRules.length > 0) {
        log.success(`✓ Branching rules are persisted in database!`);
        branchingRules.forEach((rule, idx) => {
          log.debug(`  Rule ${idx + 1}: Q${rule.sourceQuestionId} → Q${rule.targetQuestionId}`);
          log.debug(`    Condition: ${rule.condition?.operator} ${JSON.stringify(rule.condition?.values)}`);
        });
      } else {
        log.warn(`✗ No branching rules found in database!`);
        log.error('This indicates rules are not being persisted properly');
      }
    }

    log.section('TEST COMPLETE');
    log.success('Test execution finished');
    console.log(`\n${colors.bright}Summary:${colors.reset}`);
    console.log(`  Survey ID: ${surveyId}`);
    console.log(`  Questions: ${questions.length}`);
    console.log(`  Status: ${jwtToken ? 'Logged In' : 'Not Logged In'}`);

  } catch (error) {
    log.error(`Test failed with error: ${error.message}`);
    console.error(error);
  }
}

// Run the test
testBranchingRuleCreation().catch(console.error);
