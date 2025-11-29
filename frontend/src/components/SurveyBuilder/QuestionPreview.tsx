import React from 'react';
import {
  Box,
  Typography,
  Stack,
  Chip,
  Paper,
  List,
  ListItem,
  ListItemText,
  Grid,
} from '@mui/material';
import {
  TextFields as TextIcon,
  RadioButtonChecked as SingleChoiceIcon,
  CheckBox as MultipleChoiceIcon,
  Star as RatingIcon,
  Image as ImageIcon,
  LocationOn as LocationIcon,
  Numbers as NumberIcon,
  CalendarToday as DateIcon,
} from '@mui/icons-material';
import { QuestionType } from '@/types';
import type { QuestionDraft } from '@/schemas/questionSchemas';
import type { MediaItemDto } from '@/types/media';
import { stripHtml } from '@/utils/stringUtils';

interface QuestionPreviewProps {
  question: QuestionDraft;
  index: number;
}

const QuestionPreview: React.FC<QuestionPreviewProps> = ({ question, index }) => {
  const getQuestionTypeIcon = () => {
    switch (question.questionType) {
      case QuestionType.Text:
        return <TextIcon fontSize="small" />;
      case QuestionType.SingleChoice:
        return <SingleChoiceIcon fontSize="small" />;
      case QuestionType.MultipleChoice:
        return <MultipleChoiceIcon fontSize="small" />;
      case QuestionType.Rating:
        return <RatingIcon fontSize="small" />;
      case QuestionType.Location:
        return <LocationIcon fontSize="small" />;
      case QuestionType.Number:
        return <NumberIcon fontSize="small" />;
      case QuestionType.Date:
        return <DateIcon fontSize="small" />;
      default:
        return null;
    }
  };

  const getQuestionTypeLabel = () => {
    switch (question.questionType) {
      case QuestionType.Text:
        return 'Text';
      case QuestionType.SingleChoice:
        return 'Single Choice';
      case QuestionType.MultipleChoice:
        return 'Multiple Choice';
      case QuestionType.Rating:
        return 'Rating (1-5)';
      case QuestionType.Location:
        return 'Location';
      case QuestionType.Number:
        return 'Number';
      case QuestionType.Date:
        return 'Date';
      default:
        return 'Unknown';
    }
  };

  const getQuestionTypeColor = () => {
    switch (question.questionType) {
      case QuestionType.Text:
        return 'info';
      case QuestionType.SingleChoice:
        return 'success';
      case QuestionType.MultipleChoice:
        return 'warning';
      case QuestionType.Rating:
        return 'secondary';
      case QuestionType.Location:
        return 'primary';
      case QuestionType.Number:
        return 'error';
      case QuestionType.Date:
        return 'default';
      default:
        return 'default';
    }
  };

  const hasOptions =
    question.questionType === QuestionType.SingleChoice ||
    question.questionType === QuestionType.MultipleChoice;

  return (
    <Paper
      elevation={0}
      sx={{
        p: 2.5,
        border: '1px solid',
        borderColor: 'divider',
        borderRadius: 2,
        backgroundColor: 'background.paper',
        transition: 'all 0.2s ease',
        '&:hover': {
          borderColor: 'primary.main',
          boxShadow: 1,
        },
      }}
    >
      <Stack spacing={2}>
        {/* Question Header */}
        <Stack direction="row" spacing={2} alignItems="flex-start">
          {/* Question Number */}
          <Box
            sx={{
              minWidth: 32,
              height: 32,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              borderRadius: '50%',
              backgroundColor: 'primary.main',
              color: 'white',
              fontWeight: 'bold',
              fontSize: '0.875rem',
            }}
          >
            {index + 1}
          </Box>

          {/* Question Content */}
          <Box sx={{ flex: 1 }}>
            <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
              <Chip
                icon={getQuestionTypeIcon() || undefined}
                label={getQuestionTypeLabel()}
                size="small"
                color={getQuestionTypeColor() as any}
                variant="outlined"
              />
              {question.isRequired && (
                <Chip label="Required" size="small" color="error" variant="outlined" />
              )}
            </Stack>

            <Typography variant="body1" fontWeight={500} sx={{ mb: 1.5 }}>
              {stripHtml(question.questionText)}
            </Typography>

            {/* Media Preview */}
            {question.mediaContent && question.mediaContent.items && question.mediaContent.items.length > 0 && (
              <Box sx={{ mb: 2 }}>
                <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
                  <ImageIcon fontSize="small" color="action" />
                  <Typography variant="caption" color="text.secondary" fontWeight={600}>
                    Attached Media ({question.mediaContent.items.length})
                  </Typography>
                </Stack>
                <Grid container spacing={1}>
                  {question.mediaContent.items.slice(0, 3).map((media: MediaItemDto) => (
                    <Grid item xs={4} key={media.id}>
                      <Box
                        sx={{
                          position: 'relative',
                          width: '100%',
                          paddingTop: '75%',
                          borderRadius: 1,
                          overflow: 'hidden',
                          border: '1px solid',
                          borderColor: 'divider',
                          backgroundColor: 'grey.100',
                        }}
                      >
                        {media.type === 'image' && media.thumbnailPath ? (
                          <Box
                            component="img"
                            src={media.thumbnailPath}
                            alt={media.altText || media.displayName}
                            sx={{
                              position: 'absolute',
                              top: 0,
                              left: 0,
                              width: '100%',
                              height: '100%',
                              objectFit: 'cover',
                            }}
                          />
                        ) : (
                          <Box
                            sx={{
                              position: 'absolute',
                              top: 0,
                              left: 0,
                              width: '100%',
                              height: '100%',
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'center',
                            }}
                          >
                            <ImageIcon sx={{ fontSize: 32, color: 'text.disabled' }} />
                          </Box>
                        )}
                      </Box>
                    </Grid>
                  ))}
                  {question.mediaContent.items.length > 3 && (
                    <Grid item xs={12}>
                      <Typography variant="caption" color="text.secondary">
                        +{question.mediaContent.items.length - 3} more
                      </Typography>
                    </Grid>
                  )}
                </Grid>
              </Box>
            )}

            {/* Options Preview */}
            {hasOptions && question.options && question.options.length > 0 && (
              <Box
                sx={{
                  pl: 2,
                  borderLeft: 2,
                  borderColor: 'divider',
                }}
              >
                <Typography
                  variant="caption"
                  color="text.secondary"
                  sx={{ display: 'block', mb: 1, fontWeight: 600 }}
                >
                  Options:
                </Typography>
                <List dense sx={{ py: 0 }}>
                  {question.options.map((option, optionIndex) => (
                    <ListItem key={optionIndex} sx={{ py: 0.5, px: 0 }}>
                      <Box
                        component="span"
                        sx={{
                          mr: 1,
                          width: 16,
                          height: 16,
                          display: 'inline-flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          borderRadius: question.questionType === QuestionType.SingleChoice ? '50%' : 0.5,
                          border: '2px solid',
                          borderColor: 'text.secondary',
                        }}
                      >
                        {question.questionType === QuestionType.MultipleChoice && (
                          <Box
                            sx={{
                              width: 8,
                              height: 8,
                              borderRadius: 0.5,
                              backgroundColor: 'transparent',
                            }}
                          />
                        )}
                      </Box>
                      <ListItemText
                        primary={option}
                        primaryTypographyProps={{
                          variant: 'body2',
                          color: 'text.primary',
                        }}
                      />
                    </ListItem>
                  ))}
                </List>
              </Box>
            )}

            {/* Rating Preview */}
            {question.questionType === QuestionType.Rating && (
              <Box
                sx={{
                  pl: 2,
                  borderLeft: 2,
                  borderColor: 'divider',
                }}
              >
                <Typography
                  variant="caption"
                  color="text.secondary"
                  sx={{ display: 'block', mb: 1, fontWeight: 600 }}
                >
                  Scale:
                </Typography>
                <Stack direction="row" spacing={0.5}>
                  {[1, 2, 3, 4, 5].map((rating) => (
                    <Box
                      key={rating}
                      sx={{
                        width: 36,
                        height: 36,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        border: '1px solid',
                        borderColor: 'divider',
                        borderRadius: 1,
                        fontSize: '0.875rem',
                        fontWeight: 500,
                        color: 'text.secondary',
                        backgroundColor: 'background.default',
                      }}
                    >
                      {rating}
                    </Box>
                  ))}
                </Stack>
              </Box>
            )}

            {/* Text Question Preview */}
            {question.questionType === QuestionType.Text && (
              <Box
                sx={{
                  pl: 2,
                  borderLeft: 2,
                  borderColor: 'divider',
                }}
              >
                <Typography variant="caption" color="text.secondary">
                  Respondents will provide free-form text answers
                </Typography>
              </Box>
            )}

            {/* Location Question Preview */}
            {question.questionType === QuestionType.Location && (
              <Box
                sx={{
                  pl: 2,
                  borderLeft: 2,
                  borderColor: 'divider',
                }}
              >
                <Typography variant="caption" color="text.secondary">
                  Respondents will share their GPS location
                </Typography>
              </Box>
            )}

            {/* Number Question Preview */}
            {question.questionType === QuestionType.Number && (
              <Box
                sx={{
                  pl: 2,
                  borderLeft: 2,
                  borderColor: 'divider',
                }}
              >
                <Typography variant="caption" color="text.secondary">
                  Respondents will provide numeric input (integers or decimals)
                </Typography>
              </Box>
            )}

            {/* Date Question Preview */}
            {question.questionType === QuestionType.Date && (
              <Box
                sx={{
                  pl: 2,
                  borderLeft: 2,
                  borderColor: 'divider',
                }}
              >
                <Typography variant="caption" color="text.secondary">
                  Respondents will enter a date in DD.MM.YYYY format
                </Typography>
              </Box>
            )}
          </Box>
        </Stack>
      </Stack>
    </Paper>
  );
};

export default QuestionPreview;
