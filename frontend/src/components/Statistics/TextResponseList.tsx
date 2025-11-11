import { useState } from 'react';
import {
  Box,
  Typography,
  Paper,
  IconButton,
  Chip,
  Button,
  Tooltip,
} from '@mui/material';
import {
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  ContentCopy as CopyIcon,
  CheckCircle as CheckIcon,
} from '@mui/icons-material';
import type { Question, QuestionStatistics, Response } from '../../types';

interface TextResponseListProps {
  questionStat: QuestionStatistics;
  question: Question;
  responses: Response[];
}

const TextResponseList = ({ questionStat: _questionStat, question, responses }: TextResponseListProps) => {
  const [showAll, setShowAll] = useState(false);
  const [copiedIndex, setCopiedIndex] = useState<number | null>(null);

  // Extract text answers from responses
  const textAnswers = responses
    .flatMap((response) => response.answers || [])
    .filter((answer) => answer.questionId === question.id)
    .map((answer) => {
      if (answer.answerText) return answer.answerText;
      if (answer.answerData && typeof answer.answerData === 'object' && 'text' in answer.answerData) {
        return (answer.answerData as any).text;
      }
      return null;
    })
    .filter((text) => text !== null && text !== '');

  if (textAnswers.length === 0) {
    return (
      <Box sx={{ textAlign: 'center', py: 4 }}>
        <Typography color="text.secondary">No text responses yet</Typography>
      </Box>
    );
  }

  // Calculate statistics
  const totalWords = textAnswers.reduce((sum, text) => {
    return sum + (text?.split(/\s+/).filter((word: string) => word.length > 0).length || 0);
  }, 0);

  const averageWords = totalWords / textAnswers.length;

  const averageChars = textAnswers.reduce((sum, text) => sum + (text?.length || 0), 0) / textAnswers.length;

  const handleCopy = async (text: string, index: number) => {
    try {
      await navigator.clipboard.writeText(text || '');
      setCopiedIndex(index);
      setTimeout(() => setCopiedIndex(null), 2000);
    } catch (err) {
      console.error('Failed to copy text:', err);
    }
  };

  const displayedAnswers = showAll ? textAnswers : textAnswers.slice(0, 5);

  return (
    <Box>
      {/* Statistics */}
      <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
        <Paper sx={{ p: 2, flex: '1 1 200px' }}>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Total Responses
          </Typography>
          <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
            {textAnswers.length}
          </Typography>
        </Paper>
        <Paper sx={{ p: 2, flex: '1 1 200px' }}>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Avg. Words per Response
          </Typography>
          <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
            {averageWords.toFixed(1)}
          </Typography>
        </Paper>
        <Paper sx={{ p: 2, flex: '1 1 200px' }}>
          <Typography variant="body2" color="text.secondary" gutterBottom>
            Avg. Characters
          </Typography>
          <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
            {averageChars.toFixed(0)}
          </Typography>
        </Paper>
      </Box>

      {/* Response List */}
      <Box>
        <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 'bold', mb: 2 }}>
          All Text Responses
        </Typography>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {displayedAnswers.map((text, index) => (
            <TextResponseCard
              key={index}
              text={text || ''}
              index={index}
              onCopy={handleCopy}
              isCopied={copiedIndex === index}
            />
          ))}
        </Box>

        {textAnswers.length > 5 && (
          <Box sx={{ textAlign: 'center', mt: 2 }}>
            <Button
              variant="outlined"
              endIcon={showAll ? <ExpandLessIcon /> : <ExpandMoreIcon />}
              onClick={() => setShowAll(!showAll)}
            >
              {showAll ? 'Show Less' : `Show All (${textAnswers.length} responses)`}
            </Button>
          </Box>
        )}
      </Box>
    </Box>
  );
};

interface TextResponseCardProps {
  text: string;
  index: number;
  onCopy: (text: string, index: number) => void;
  isCopied: boolean;
}

const TextResponseCard = ({ text, index, onCopy, isCopied }: TextResponseCardProps) => {
  const [expanded, setExpanded] = useState(false);
  const isLongText = text.length > 200;
  const truncatedText = text.substring(0, 200);
  const wordCount = text.split(/\s+/).filter((word) => word.length > 0).length;

  return (
    <Paper
      sx={{
        p: 2,
        bgcolor: 'background.default',
        border: 1,
        borderColor: 'divider',
      }}
    >
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
        <Chip label={`Response #${index + 1}`} size="small" variant="outlined" />
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Chip label={`${wordCount} words`} size="small" />
          <Tooltip title={isCopied ? 'Copied!' : 'Copy text'}>
            <IconButton
              size="small"
              onClick={() => onCopy(text, index)}
              color={isCopied ? 'success' : 'default'}
            >
              {isCopied ? <CheckIcon /> : <CopyIcon />}
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
        {isLongText && !expanded ? (
          <>
            {truncatedText}...{' '}
            <Button
              size="small"
              onClick={() => setExpanded(true)}
              sx={{ textTransform: 'none', minWidth: 'auto', p: 0 }}
            >
              Show more
            </Button>
          </>
        ) : (
          <>
            {text}
            {isLongText && (
              <>
                {' '}
                <Button
                  size="small"
                  onClick={() => setExpanded(false)}
                  sx={{ textTransform: 'none', minWidth: 'auto', p: 0 }}
                >
                  Show less
                </Button>
              </>
            )}
          </>
        )}
      </Typography>
    </Paper>
  );
};

export default TextResponseList;
