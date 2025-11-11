import { Box, Grid } from '@mui/material';
import type { Survey, SurveyStatistics, Response } from '../../types';
import QuestionCard from './QuestionCard';

interface QuestionStatisticsProps {
  statistics: SurveyStatistics;
  survey: Survey;
  responses: Response[];
}

const QuestionStatistics = ({ statistics, survey, responses }: QuestionStatisticsProps) => {
  return (
    <Box>
      <Grid container spacing={3}>
        {statistics.questionStatistics.map((questionStat) => {
          const question = survey.questions.find((q) => q.id === questionStat.questionId);
          if (!question) return null;

          return (
            <Grid item xs={12} key={questionStat.questionId}>
              <QuestionCard
                questionStat={questionStat}
                question={question}
                responses={responses}
              />
            </Grid>
          );
        })}
      </Grid>
    </Box>
  );
};

export default QuestionStatistics;
