import { useState, useEffect } from 'react';
import {
  Alert,
  AlertTitle,
  Box,
  Button,
  Collapse,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  IconButton,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  CheckCircle as CheckIcon,
} from '@mui/icons-material';
import questionFlowService from '@/services/questionFlowService';
import type { SurveyValidationResult } from '@/types';

interface FlowValidationWarningProps {
  surveyId: number;
  onFixClick?: () => void;
  autoValidate?: boolean; // Auto-validate on mount
}

/**
 * FlowValidationWarning Component
 *
 * Displays validation status for survey question flow.
 * Shows warnings/errors if there are cycle detections or other flow issues.
 *
 * Usage:
 * - In survey activation dialog
 * - In survey edit page
 * - Before publishing survey
 *
 * Features:
 * - Auto-validates on mount (optional)
 * - Collapsible error details
 * - "Fix" button callback
 * - Cycle path visualization
 *
 * @param surveyId - The survey ID to validate
 * @param onFixClick - Callback when "Fix" button clicked (navigate to flow config)
 * @param autoValidate - Whether to auto-validate on mount (default: true)
 */
export default function FlowValidationWarning({
  surveyId,
  onFixClick,
  autoValidate = true,
}: FlowValidationWarningProps) {
  const [validationResult, setValidationResult] = useState<SurveyValidationResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [expanded, setExpanded] = useState(false);

  useEffect(() => {
    if (autoValidate) {
      validateFlow();
    }
  }, [surveyId, autoValidate]);

  const validateFlow = async () => {
    try {
      setLoading(true);
      setError(null);

      const result = await questionFlowService.validateSurveyFlow(surveyId);
      setValidationResult(result);

      // Auto-expand if errors found
      if (!result.valid) {
        setExpanded(true);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Validation failed');
    } finally {
      setLoading(false);
    }
  };

  const handleToggle = () => {
    setExpanded((prev) => !prev);
  };

  // Don't show anything if still loading or no result yet
  if (loading || !validationResult) {
    return null;
  }

  // Show success message if validation passed
  if (validationResult.valid) {
    return (
      <Alert severity="success" icon={<CheckIcon />}>
        <AlertTitle>Survey flow is valid</AlertTitle>
        Your survey flow has been validated and is ready for activation.
      </Alert>
    );
  }

  // Show error/warning if validation failed
  return (
    <Alert
      severity="warning"
      icon={<WarningIcon />}
      action={
        onFixClick ? (
          <Button size="small" color="inherit" onClick={onFixClick}>
            Fix Issues
          </Button>
        ) : undefined
      }
    >
      <AlertTitle>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <span>Survey flow has validation issues</span>
          <IconButton
            size="small"
            onClick={handleToggle}
            sx={{ ml: 1 }}
          >
            {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
          </IconButton>
        </Box>
      </AlertTitle>

      {validationResult.errors && validationResult.errors.length > 0 && (
        <Box mb={1}>
          <strong>Issues detected:</strong>
        </Box>
      )}

      <Collapse in={expanded}>
        <List dense>
          {validationResult.errors?.map((errorMsg, index) => (
            <ListItem key={index}>
              <ListItemIcon sx={{ minWidth: 32 }}>
                <ErrorIcon fontSize="small" color="error" />
              </ListItemIcon>
              <ListItemText primary={errorMsg} />
            </ListItem>
          ))}

          {validationResult.cyclePath && validationResult.cyclePath.length > 0 && (
            <ListItem>
              <ListItemIcon sx={{ minWidth: 32 }}>
                <ErrorIcon fontSize="small" color="error" />
              </ListItemIcon>
              <ListItemText
                primary="Cycle detected in question flow"
                secondary={`Path: Question ${validationResult.cyclePath.join(' → Question ')}`}
              />
            </ListItem>
          )}
        </List>

        {!validationResult.errors || validationResult.errors.length === 0 ? (
          <Box mt={1}>
            <em>No specific error details available.</em>
          </Box>
        ) : null}

        <Box mt={2}>
          <strong>How to fix:</strong>
          <List dense>
            <ListItem>
              <ListItemText
                primary="1. Review your question flow configuration"
                secondary="Ensure no questions create circular references"
              />
            </ListItem>
            <ListItem>
              <ListItemText
                primary="2. Check all branching logic"
                secondary="Make sure all options have valid next questions or lead to survey end"
              />
            </ListItem>
            <ListItem>
              <ListItemText
                primary="3. Use the Flow Configuration page"
                secondary="Navigate to Flow Configuration to visualize and fix the issue"
              />
            </ListItem>
          </List>
        </Box>
      </Collapse>

      {error && (
        <Box mt={2}>
          <Alert severity="error">{error}</Alert>
        </Box>
      )}
    </Alert>
  );
}
