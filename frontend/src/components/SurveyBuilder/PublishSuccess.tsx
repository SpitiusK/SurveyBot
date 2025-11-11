import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Typography,
  Stack,
  Paper,
  Button,
  IconButton,
  Divider,
  Alert,
  Tooltip,
  Card,
  CardContent,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
import {
  CheckCircle as SuccessIcon,
  ContentCopy as CopyIcon,
  Visibility as ViewIcon,
  Assessment as StatsIcon,
  Share as ShareIcon,
  Edit as EditIcon,
  Check as CheckIcon,
} from '@mui/icons-material';
import type { Survey } from '@/types';

interface PublishSuccessProps {
  survey: Survey;
  onViewSurvey?: () => void;
  onViewStats?: () => void;
  onEditSurvey?: () => void;
}

const PublishSuccess: React.FC<PublishSuccessProps> = ({
  survey,
  onViewSurvey,
  onViewStats,
  onEditSurvey,
}) => {
  const navigate = useNavigate();
  const [copied, setCopied] = useState(false);

  const surveyCode = survey.code || '';
  const surveyUrl = surveyCode
    ? `${window.location.origin}/survey/${surveyCode}`
    : '';

  const handleCopyCode = async () => {
    try {
      await navigator.clipboard.writeText(surveyCode);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy code:', err);
    }
  };

  const handleCopyUrl = async () => {
    try {
      await navigator.clipboard.writeText(surveyUrl);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy URL:', err);
    }
  };

  const handleGoToDashboard = () => {
    navigate('/dashboard/surveys');
  };

  const handleViewSurvey = () => {
    if (onViewSurvey) {
      onViewSurvey();
    } else {
      navigate(`/dashboard/surveys/${survey.id}`);
    }
  };

  const handleViewStats = () => {
    if (onViewStats) {
      onViewStats();
    } else {
      navigate(`/dashboard/surveys/${survey.id}/stats`);
    }
  };

  const handleEditSurvey = () => {
    if (onEditSurvey) {
      onEditSurvey();
    } else {
      navigate(`/dashboard/surveys/${survey.id}/edit`);
    }
  };

  return (
    <Box>
      {/* Success Header */}
      <Paper
        elevation={0}
        sx={{
          p: 4,
          mb: 3,
          textAlign: 'center',
          backgroundColor: 'success.50',
          borderRadius: 2,
        }}
      >
        <SuccessIcon sx={{ fontSize: 64, color: 'success.main', mb: 2 }} />
        <Typography variant="h4" fontWeight="bold" color="success.main" gutterBottom>
          Survey Published Successfully!
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ maxWidth: 600, mx: 'auto' }}>
          Your survey is now live and ready to receive responses. Share the survey code or
          link with your respondents.
        </Typography>
      </Paper>

      {/* Survey Code Card */}
      <Card
        elevation={2}
        sx={{
          mb: 3,
          border: 2,
          borderColor: 'primary.main',
          backgroundColor: 'primary.50',
        }}
      >
        <CardContent sx={{ p: 3 }}>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            Survey Code
          </Typography>
          <Stack
            direction="row"
            spacing={2}
            alignItems="center"
            justifyContent="space-between"
            sx={{ mb: 2 }}
          >
            <Typography
              variant="h3"
              fontWeight="bold"
              color="primary.main"
              sx={{
                fontFamily: 'monospace',
                letterSpacing: 2,
              }}
            >
              {surveyCode}
            </Typography>
            <Tooltip title={copied ? 'Copied!' : 'Copy code'}>
              <IconButton
                onClick={handleCopyCode}
                color="primary"
                size="large"
                sx={{
                  backgroundColor: 'white',
                  '&:hover': {
                    backgroundColor: 'grey.100',
                  },
                }}
              >
                {copied ? <CheckIcon /> : <CopyIcon />}
              </IconButton>
            </Tooltip>
          </Stack>
          <Typography variant="caption" color="text.secondary">
            Share this code with respondents. They can use it in the Telegram bot or on the
            web.
          </Typography>
        </CardContent>
      </Card>

      {/* Survey URL Card */}
      {surveyUrl && (
        <Card elevation={1} sx={{ mb: 3 }}>
          <CardContent sx={{ p: 2.5 }}>
            <Typography variant="subtitle2" color="text.secondary" gutterBottom>
              Survey URL
            </Typography>
            <Stack
              direction="row"
              spacing={1}
              alignItems="center"
              sx={{
                p: 1.5,
                backgroundColor: 'grey.100',
                borderRadius: 1,
                mb: 1,
              }}
            >
              <Typography
                variant="body2"
                sx={{
                  flex: 1,
                  fontFamily: 'monospace',
                  wordBreak: 'break-all',
                }}
              >
                {surveyUrl}
              </Typography>
              <Tooltip title={copied ? 'Copied!' : 'Copy URL'}>
                <IconButton onClick={handleCopyUrl} size="small" color="primary">
                  {copied ? <CheckIcon fontSize="small" /> : <CopyIcon fontSize="small" />}
                </IconButton>
              </Tooltip>
            </Stack>
            <Typography variant="caption" color="text.secondary">
              Direct link to your survey
            </Typography>
          </CardContent>
        </Card>
      )}

      <Divider sx={{ my: 3 }} />

      {/* Next Steps */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="h6" fontWeight={600} gutterBottom>
          What's Next?
        </Typography>
        <List>
          <ListItem>
            <ListItemIcon>
              <ShareIcon color="primary" />
            </ListItemIcon>
            <ListItemText
              primary="Share Your Survey"
              secondary="Share the survey code or link with your target audience via Telegram, email, or social media."
            />
          </ListItem>
          <ListItem>
            <ListItemIcon>
              <StatsIcon color="primary" />
            </ListItemIcon>
            <ListItemText
              primary="Monitor Responses"
              secondary="Track responses in real-time and view detailed statistics and analytics."
            />
          </ListItem>
          <ListItem>
            <ListItemIcon>
              <EditIcon color="primary" />
            </ListItemIcon>
            <ListItemText
              primary="Edit Your Survey"
              secondary="You can still edit survey details and questions. Note that the survey must be deactivated first if it has responses."
            />
          </ListItem>
        </List>
      </Box>

      <Divider sx={{ my: 3 }} />

      {/* Action Buttons */}
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        justifyContent="center"
        sx={{ mb: 2 }}
      >
        <Button
          variant="contained"
          startIcon={<ViewIcon />}
          onClick={handleViewSurvey}
          size="large"
        >
          View Survey Details
        </Button>
        <Button
          variant="outlined"
          startIcon={<StatsIcon />}
          onClick={handleViewStats}
          size="large"
        >
          View Statistics
        </Button>
        <Button
          variant="outlined"
          startIcon={<EditIcon />}
          onClick={handleEditSurvey}
          size="large"
        >
          Edit Survey
        </Button>
      </Stack>

      <Box sx={{ textAlign: 'center', mt: 3 }}>
        <Button variant="text" onClick={handleGoToDashboard}>
          Back to Dashboard
        </Button>
      </Box>

      {/* Tips Alert */}
      <Alert severity="info" sx={{ mt: 3 }}>
        <Typography variant="body2" fontWeight={600} gutterBottom>
          Tips for Success:
        </Typography>
        <Typography variant="caption" component="div">
          • Keep your survey active to collect responses
          <br />
          • Check statistics regularly to monitor progress
          <br />
          • Remind respondents about the survey code
          <br />• Export data when you have enough responses
        </Typography>
      </Alert>
    </Box>
  );
};

export default PublishSuccess;
