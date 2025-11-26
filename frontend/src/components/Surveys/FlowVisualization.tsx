import { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  CircularProgress,
  Alert,
  Chip,
  Stack,
  IconButton,
  Collapse,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  CheckCircle as CheckIcon,
  Error as ErrorIcon,
  ArrowForward as ArrowIcon,
} from '@mui/icons-material';
import questionFlowService from '@/services/questionFlowService';
import type { Question, ConditionalFlowDto, SurveyValidationResult } from '@/types';

interface FlowVisualizationProps {
  surveyId: number;
  questions: Question[];
}

interface QuestionFlowNode {
  question: Question;
  flowConfig: ConditionalFlowDto | null;
  children: Map<string, number | null>; // label -> next question ID
  isEndpoint: boolean;
}

/**
 * FlowVisualization Component
 *
 * Displays a tree view of the survey's question flow structure.
 *
 * Features:
 * - Shows question hierarchy and branching paths
 * - Indicates endpoints (questions that lead to survey end)
 * - Displays which options branch to which questions
 * - Shows validation status
 * - Expandable/collapsible nodes
 *
 * Color coding:
 * - Green: Endpoints (lead to end of survey)
 * - Blue: Regular questions with flow configured
 * - Gray: Questions with no flow configured (default order)
 * - Red: Questions with validation errors
 *
 * @param surveyId - The survey ID
 * @param questions - All questions in the survey
 */
export default function FlowVisualization({ surveyId, questions }: FlowVisualizationProps) {
  const [flowNodes, setFlowNodes] = useState<Map<number, QuestionFlowNode>>(new Map());
  const [validationResult, setValidationResult] = useState<SurveyValidationResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [expandedNodes, setExpandedNodes] = useState<Set<number>>(new Set());

  useEffect(() => {
    loadFlowData();
  }, [surveyId, questions]);

  const loadFlowData = async () => {
    try {
      setLoading(true);
      setError(null);

      // Load flow configs for all questions
      const nodes = new Map<number, QuestionFlowNode>();

      for (const question of questions) {
        try {
          const flowConfig = await questionFlowService.getQuestionFlow(surveyId, question.id);

          const children = new Map<string, number | null>();

          if (flowConfig.supportsBranching && flowConfig.optionFlows.length > 0) {
            // Branching question: Add each option flow
            flowConfig.optionFlows.forEach((optionFlow) => {
              const label = `Option: "${optionFlow.optionText}"`;
              const next = optionFlow.next;
              children.set(label, !next ? null :
                next.type === 'EndSurvey' ? -1 :
                next.nextQuestionId ?? null);
            });
          } else if (flowConfig.defaultNext) {
            // Non-branching question with default next
            const next = flowConfig.defaultNext;
            children.set('Next', next.type === 'EndSurvey' ? -1 : next.nextQuestionId ?? null);
          }

          const isEndpoint =
            children.size === 0 ||
            Array.from(children.values()).every((nextId) => nextId === -1);

          nodes.set(question.id, {
            question,
            flowConfig,
            children,
            isEndpoint,
          });
        } catch (err) {
          // Question might not have flow config yet - that's okay
          nodes.set(question.id, {
            question,
            flowConfig: null,
            children: new Map(),
            isEndpoint: false,
          });
        }
      }

      setFlowNodes(nodes);

      // Validate flow
      const validation = await questionFlowService.validateSurveyFlow(surveyId);
      setValidationResult(validation);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load flow visualization');
    } finally {
      setLoading(false);
    }
  };

  const toggleNode = (questionId: number) => {
    setExpandedNodes((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(questionId)) {
        newSet.delete(questionId);
      } else {
        newSet.add(questionId);
      }
      return newSet;
    });
  };

  const getQuestionById = (id: number): Question | undefined => {
    return questions.find((q) => q.id === id);
  };

  const renderQuestionNode = (
    node: QuestionFlowNode,
    depth: number = 0,
    visitedIds: Set<number> = new Set()
  ): React.ReactNode => {
    const { question, children, isEndpoint } = node;

    // Detect cycles
    if (visitedIds.has(question.id)) {
      return (
        <Box key={`cycle-${question.id}`} ml={depth * 4} mb={1}>
          <Alert severity="error" sx={{ width: 'fit-content' }}>
            <Typography variant="body2">
              ⚠️ Cycle detected at Q{question.orderIndex + 1}
            </Typography>
          </Alert>
        </Box>
      );
    }

    const newVisited = new Set(visitedIds);
    newVisited.add(question.id);

    const hasChildren = children.size > 0;
    const isExpanded = expandedNodes.has(question.id);

    // Determine color based on state
    let chipColor: 'default' | 'success' | 'primary' | 'error' = 'default';
    if (validationResult?.cyclePath?.includes(question.id)) {
      chipColor = 'error';
    } else if (isEndpoint) {
      chipColor = 'success';
    } else if (hasChildren) {
      chipColor = 'primary';
    }

    return (
      <Box key={question.id} ml={depth * 4} mb={1}>
        <Stack direction="row" spacing={1} alignItems="center">
          {hasChildren && (
            <IconButton size="small" onClick={() => toggleNode(question.id)}>
              {isExpanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
            </IconButton>
          )}

          {!hasChildren && <Box sx={{ width: 32 }} />}

          <Chip
            label={`Q${question.orderIndex + 1}`}
            color={chipColor}
            size="small"
            sx={{ fontWeight: 'bold' }}
          />

          <Typography variant="body2">{question.questionText}</Typography>

          {isEndpoint && (
            <Chip
              icon={<CheckIcon />}
              label="Endpoint"
              color="success"
              size="small"
              variant="outlined"
            />
          )}
        </Stack>

        {hasChildren && isExpanded && (
          <Collapse in={isExpanded}>
            <Box ml={4} mt={1}>
              {Array.from(children.entries()).map(([label, nextId]) => (
                <Box key={label} mb={1}>
                  <Stack direction="row" spacing={1} alignItems="center" mb={0.5}>
                    <ArrowIcon fontSize="small" color="action" />
                    <Typography variant="body2" color="text.secondary">
                      {label}
                    </Typography>
                    <ArrowIcon fontSize="small" color="action" />
                    {nextId === -1 ? (
                      <Chip
                        label="END"
                        color="success"
                        size="small"
                        icon={<CheckIcon />}
                        sx={{ fontWeight: 'bold' }}
                      />
                    ) : nextId !== null ? (
                      <>
                        <Chip
                          label={`Q${getQuestionById(nextId)?.orderIndex ?? '?'}`}
                          size="small"
                        />
                        <Typography variant="body2" color="text.secondary">
                          {getQuestionById(nextId)?.questionText}
                        </Typography>
                      </>
                    ) : (
                      <Typography variant="body2" color="text.secondary" fontStyle="italic">
                        (Default order)
                      </Typography>
                    )}
                  </Stack>

                  {nextId !== null && nextId !== -1 && getQuestionById(nextId) && (
                    <Box ml={2}>
                      {renderQuestionNode(
                        flowNodes.get(nextId)!,
                        depth + 1,
                        newVisited
                      )}
                    </Box>
                  )}
                </Box>
              ))}
            </Box>
          </Collapse>
        )}
      </Box>
    );
  };

  if (loading) {
    return (
      <Card>
        <CardContent>
          <Box display="flex" justifyContent="center" alignItems="center" minHeight={200}>
            <CircularProgress />
          </Box>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" mb={2}>
          Question Flow Visualization
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {validationResult && (
          <Alert
            severity={validationResult.valid ? 'success' : 'error'}
            icon={validationResult.valid ? <CheckIcon /> : <ErrorIcon />}
            sx={{ mb: 2 }}
          >
            {validationResult.valid ? (
              <Typography variant="body2">Survey flow is valid!</Typography>
            ) : (
              <>
                <Typography variant="body2" fontWeight="bold">
                  Validation Errors:
                </Typography>
                <ul style={{ margin: '8px 0', paddingLeft: '20px' }}>
                  {validationResult.errors?.map((err, idx) => (
                    <li key={idx}>
                      <Typography variant="body2">{err}</Typography>
                    </li>
                  ))}
                </ul>
                {validationResult.cyclePath && (
                  <Typography variant="body2" fontWeight="bold" mt={1}>
                    Cycle path: Q{validationResult.cyclePath.map((id) => {
                      const q = getQuestionById(id);
                      return q ? q.orderIndex + 1 : id;
                    }).join(' → Q')}
                  </Typography>
                )}
              </>
            )}
          </Alert>
        )}

        {questions.length === 0 ? (
          <Alert severity="info">No questions in this survey yet.</Alert>
        ) : (
          <Box>
            <Typography variant="body2" color="text.secondary" mb={2}>
              Click on a question to expand/collapse its flow. Colors indicate:
              <Box component="span" ml={1}>
                <Chip label="Green" color="success" size="small" sx={{ mx: 0.5 }} /> = Endpoint
                <Chip label="Blue" color="primary" size="small" sx={{ mx: 0.5 }} /> = Has flow
                <Chip label="Gray" color="default" size="small" sx={{ mx: 0.5 }} /> = No flow
                <Chip label="Red" color="error" size="small" sx={{ mx: 0.5 }} /> = Error
              </Box>
            </Typography>

            <Box
              sx={{
                maxHeight: 600,
                overflowY: 'auto',
                border: '1px solid',
                borderColor: 'divider',
                borderRadius: 1,
                p: 2,
              }}
            >
              {Array.from(flowNodes.values())
                .filter((node) => node.question.orderIndex === 0) // Start with first question
                .map((node) => renderQuestionNode(node, 0, new Set()))}

              {/* Render remaining questions that aren't first in order */}
              {Array.from(flowNodes.values())
                .filter((node) => node.question.orderIndex !== 0)
                .sort((a, b) => a.question.orderIndex - b.question.orderIndex)
                .map((node) => renderQuestionNode(node, 0, new Set()))}
            </Box>
          </Box>
        )}
      </CardContent>
    </Card>
  );
}
