import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Container,
  Grid,
  Typography,
  Alert,
  Button,
  Card,
  CardContent,
  Chip,
} from '@mui/material';
import {
  ArrowBack as BackIcon,
  CheckCircle as CheckIcon,
  Error as ErrorIcon,
} from '@mui/icons-material';
import { PageContainer } from '@/components/PageContainer';
import { LoadingSpinner } from '@/components/LoadingSpinner';
import FlowConfigurationPanel from '@/components/Surveys/FlowConfigurationPanel';
import FlowVisualization from '@/components/Surveys/FlowVisualization';
import surveyService from '@/services/surveyService';
import questionFlowService from '@/services/questionFlowService';
import type { Survey, Question, SurveyValidationResult } from '@/types';

/**
 * SurveyFlowConfiguration Page
 *
 * Dedicated page for configuring conditional question flow for a survey.
 *
 * Features:
 * - Left panel: Flow configuration for selected question
 * - Right panel: Visual flow diagram
 * - Validation status banner
 * - Question selector
 *
 * Usage:
 * - Navigate to /dashboard/surveys/:id/flow
 * - Select a question to configure its flow
 * - Save configuration and see real-time visualization
 */
export default function SurveyFlowConfiguration() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [survey, setSurvey] = useState<Survey | null>(null);
  const [selectedQuestion, setSelectedQuestion] = useState<Question | null>(null);
  const [validationResult, setValidationResult] = useState<SurveyValidationResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (id) {
      loadSurvey();
    }
  }, [id]);

  const loadSurvey = async () => {
    if (!id) return;

    try {
      setLoading(true);
      setError(null);

      const surveyData = await surveyService.getSurveyById(parseInt(id));
      setSurvey(surveyData);

      // Auto-select first question
      if (surveyData.questions && surveyData.questions.length > 0) {
        setSelectedQuestion(surveyData.questions[0]);
      }

      // Validate flow
      await validateFlow();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load survey');
    } finally {
      setLoading(false);
    }
  };

  const validateFlow = async () => {
    if (!id) return;

    try {
      const result = await questionFlowService.validateSurveyFlow(parseInt(id));
      setValidationResult(result);
    } catch (err) {
      console.error('Validation error:', err);
    }
  };

  const handleFlowUpdated = async () => {
    // Revalidate after flow update
    await validateFlow();
  };

  const handleQuestionSelect = (question: Question) => {
    setSelectedQuestion(question);
  };

  const handleBack = () => {
    navigate(`/dashboard/surveys/${id}`);
  };

  if (loading) {
    return <LoadingSpinner message="Loading survey..." />;
  }

  if (error || !survey) {
    return (
      <PageContainer title="Flow Configuration">
        <Container maxWidth="lg">
          <Alert severity="error">
            {error || 'Survey not found'}
            <Box mt={2}>
              <Button variant="contained" onClick={() => navigate('/dashboard/surveys')}>
                Back to Surveys
              </Button>
            </Box>
          </Alert>
        </Container>
      </PageContainer>
    );
  }

  return (
    <PageContainer
      title="Configure Question Flow"
      breadcrumbs={[
        { label: 'Dashboard', path: '/dashboard' },
        { label: 'Surveys', path: '/dashboard/surveys' },
        { label: survey.title, path: `/dashboard/surveys/${id}` },
        { label: 'Flow Configuration' },
      ]}
    >
      <Container maxWidth="xl">
        {/* Header */}
        <Box mb={3}>
          <Button
            startIcon={<BackIcon />}
            onClick={handleBack}
            sx={{ mb: 2 }}
          >
            Back to Survey
          </Button>

          <Typography variant="h4" fontWeight="bold" mb={1}>
            {survey.title}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Configure conditional question flow by selecting a question below and defining which
            question should appear next based on the respondent's answer.
          </Typography>
        </Box>

        {/* Validation Status Banner */}
        {validationResult && (
          <Alert
            severity={validationResult.valid ? 'success' : 'warning'}
            icon={validationResult.valid ? <CheckIcon /> : <ErrorIcon />}
            sx={{ mb: 3 }}
          >
            {validationResult.valid ? (
              <Typography variant="body2">
                Survey flow is valid and ready for use!
              </Typography>
            ) : (
              <>
                <Typography variant="body2" fontWeight="bold" mb={1}>
                  Flow validation issues detected:
                </Typography>
                <ul style={{ margin: 0, paddingLeft: '20px' }}>
                  {validationResult.errors?.map((err, idx) => (
                    <li key={idx}>
                      <Typography variant="body2">{err}</Typography>
                    </li>
                  ))}
                </ul>
                {validationResult.cyclePath && (
                  <Typography variant="body2" mt={1}>
                    <strong>Cycle detected in questions:</strong> Q
                    {validationResult.cyclePath.join(' → Q')}
                  </Typography>
                )}
              </>
            )}
          </Alert>
        )}

        {/* Question Selector */}
        {survey.questions && survey.questions.length > 0 ? (
          <>
            <Card sx={{ mb: 3 }}>
              <CardContent>
                <Typography variant="h6" mb={2}>
                  Select Question to Configure
                </Typography>
                <Box display="flex" flexWrap="wrap" gap={1}>
                  {survey.questions
                    .sort((a, b) => a.orderIndex - b.orderIndex)
                    .map((question) => (
                      <Chip
                        key={question.id}
                        label={`Q${question.orderIndex + 1}: ${question.questionText.substring(0, 50)}${question.questionText.length > 50 ? '...' : ''}`}
                        onClick={() => handleQuestionSelect(question)}
                        color={selectedQuestion?.id === question.id ? 'primary' : 'default'}
                        variant={selectedQuestion?.id === question.id ? 'filled' : 'outlined'}
                        sx={{
                          cursor: 'pointer',
                          '&:hover': {
                            backgroundColor: 'primary.light',
                          },
                        }}
                      />
                    ))}
                </Box>
              </CardContent>
            </Card>

            {/* Main Grid: Flow Config + Visualization */}
            <Grid container spacing={3}>
              {/* Left: Flow Configuration Panel */}
              <Grid item xs={12} lg={6}>
                {selectedQuestion ? (
                  <FlowConfigurationPanel
                    surveyId={parseInt(id!)}
                    question={selectedQuestion}
                    allQuestions={survey.questions}
                    onFlowUpdated={handleFlowUpdated}
                  />
                ) : (
                  <Card>
                    <CardContent>
                      <Alert severity="info">
                        Select a question above to configure its flow.
                      </Alert>
                    </CardContent>
                  </Card>
                )}
              </Grid>

              {/* Right: Flow Visualization */}
              <Grid item xs={12} lg={6}>
                <FlowVisualization
                  surveyId={parseInt(id!)}
                  questions={survey.questions}
                />
              </Grid>
            </Grid>
          </>
        ) : (
          <Alert severity="info">
            This survey has no questions yet. Add questions before configuring flow.
          </Alert>
        )}
      </Container>
    </PageContainer>
  );
}
