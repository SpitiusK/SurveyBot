import { Paper, Box, Typography, Chip, Divider } from '@mui/material';
import {
  TextFields as TextIcon,
  RadioButtonChecked as SingleChoiceIcon,
  CheckBox as MultipleChoiceIcon,
  Star as RatingIcon,
  LocationOn as LocationIcon,
} from '@mui/icons-material';
import type { Question, QuestionStatistics, Response } from '../../types';
import { QuestionType } from '../../types';
import ChoiceChart from './ChoiceChart';
import RatingChart from './RatingChart';
import TextResponseList from './TextResponseList';

interface QuestionCardProps {
  questionStat: QuestionStatistics;
  question: Question;
  responses: Response[];
}

const QuestionCard = ({ questionStat, question, responses }: QuestionCardProps) => {
  const getQuestionTypeIcon = (type: QuestionType) => {
    switch (type) {
      case QuestionType.Text:
        return <TextIcon />;
      case QuestionType.SingleChoice:
        return <SingleChoiceIcon />;
      case QuestionType.MultipleChoice:
        return <MultipleChoiceIcon />;
      case QuestionType.Rating:
        return <RatingIcon />;
      case QuestionType.Location:
        return <LocationIcon />;
      default:
        return null;
    }
  };

  const getQuestionTypeName = (type: QuestionType) => {
    switch (type) {
      case QuestionType.Text:
        return 'Text';
      case QuestionType.SingleChoice:
        return 'Single Choice';
      case QuestionType.MultipleChoice:
        return 'Multiple Choice';
      case QuestionType.Rating:
        return 'Rating';
      case QuestionType.Location:
        return 'Location';
      default:
        return 'Unknown';
    }
  };

  return (
    <Paper sx={{ p: 3 }}>
      {/* Question Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
        <Box sx={{ flex: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            {getQuestionTypeIcon(question.questionType)}
            <Chip
              label={getQuestionTypeName(question.questionType)}
              size="small"
              variant="outlined"
            />
            {question.isRequired && (
              <Chip label="Required" size="small" color="error" variant="outlined" />
            )}
          </Box>
          <Typography variant="h6" gutterBottom>
            {question.questionText}
          </Typography>
        </Box>
        <Box sx={{ textAlign: 'right' }}>
          <Typography variant="body2" color="text.secondary">
            Total Answers
          </Typography>
          <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
            {questionStat.totalAnswers}
          </Typography>
        </Box>
      </Box>

      <Divider sx={{ my: 2 }} />

      {/* Question Statistics Content */}
      <Box>
        {question.questionType === QuestionType.Text && (
          <TextResponseList
            questionStat={questionStat}
            question={question}
            responses={responses}
          />
        )}

        {(question.questionType === QuestionType.SingleChoice ||
          question.questionType === QuestionType.MultipleChoice) && (
          <ChoiceChart
            questionStat={questionStat}
            question={question}
            isSingleChoice={question.questionType === QuestionType.SingleChoice}
          />
        )}

        {question.questionType === QuestionType.Rating && (
          <RatingChart questionStat={questionStat} question={question} />
        )}

        {question.questionType === QuestionType.Location && (
          <TextResponseList
            questionStat={questionStat}
            question={question}
            responses={responses}
          />
        )}
      </Box>
    </Paper>
  );
};

export default QuestionCard;
