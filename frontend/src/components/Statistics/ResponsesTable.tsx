import { useState, Fragment } from 'react';
import {
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  TableSortLabel,
  Chip,
  IconButton,
  Collapse,
  Box,
  Typography,
} from '@mui/material';
import {
  CheckCircle as CheckCircleIcon,
  RadioButtonUnchecked as IncompleteIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
} from '@mui/icons-material';
import type { Response, Survey } from '../../types';
import { QuestionType } from '../../types';
import { format } from 'date-fns';

interface ResponsesTableProps {
  responses: Response[];
  survey: Survey;
}

type Order = 'asc' | 'desc';
type OrderBy = 'respondentTelegramId' | 'startedAt' | 'submittedAt' | 'isComplete';

const ResponsesTable = ({ responses, survey }: ResponsesTableProps) => {
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [order, setOrder] = useState<Order>('desc');
  const [orderBy, setOrderBy] = useState<OrderBy>('submittedAt');
  const [expandedRows, setExpandedRows] = useState<Set<number>>(new Set());

  const handleRequestSort = (property: OrderBy) => {
    const isAsc = orderBy === property && order === 'asc';
    setOrder(isAsc ? 'desc' : 'asc');
    setOrderBy(property);
  };

  const handleChangePage = (_event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const toggleRow = (responseId: number) => {
    const newExpanded = new Set(expandedRows);
    if (newExpanded.has(responseId)) {
      newExpanded.delete(responseId);
    } else {
      newExpanded.add(responseId);
    }
    setExpandedRows(newExpanded);
  };

  const formatDate = (dateString: string | null) => {
    if (!dateString) return 'N/A';
    try {
      return format(new Date(dateString), 'MMM d, yyyy HH:mm');
    } catch {
      return 'N/A';
    }
  };

  const sortedResponses = [...responses].sort((a, b) => {
    let aValue: any = a[orderBy];
    let bValue: any = b[orderBy];

    if (orderBy === 'startedAt' || orderBy === 'submittedAt') {
      aValue = aValue ? new Date(aValue).getTime() : 0;
      bValue = bValue ? new Date(bValue).getTime() : 0;
    }

    if (order === 'asc') {
      return aValue > bValue ? 1 : -1;
    } else {
      return aValue < bValue ? 1 : -1;
    }
  });

  const paginatedResponses = sortedResponses.slice(
    page * rowsPerPage,
    page * rowsPerPage + rowsPerPage
  );

  /**
   * Returns the display value for an answer.
   * Uses pre-computed DisplayValue from backend.
   */
  const getAnswerDisplay = (answer: any, _questionType?: QuestionType) => {
    return answer?.displayValue ?? 'No answer';
  };

  if (responses.length === 0) {
    return (
      <Paper sx={{ p: 3, textAlign: 'center' }}>
        <Typography color="text.secondary">No responses yet</Typography>
      </Paper>
    );
  }

  return (
    <Paper>
      <TableContainer>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell padding="checkbox" />
              <TableCell>
                <TableSortLabel
                  active={orderBy === 'respondentTelegramId'}
                  direction={orderBy === 'respondentTelegramId' ? order : 'asc'}
                  onClick={() => handleRequestSort('respondentTelegramId')}
                >
                  Respondent ID
                </TableSortLabel>
              </TableCell>
              <TableCell>
                <TableSortLabel
                  active={orderBy === 'startedAt'}
                  direction={orderBy === 'startedAt' ? order : 'asc'}
                  onClick={() => handleRequestSort('startedAt')}
                >
                  Started
                </TableSortLabel>
              </TableCell>
              <TableCell>
                <TableSortLabel
                  active={orderBy === 'submittedAt'}
                  direction={orderBy === 'submittedAt' ? order : 'asc'}
                  onClick={() => handleRequestSort('submittedAt')}
                >
                  Completed
                </TableSortLabel>
              </TableCell>
              <TableCell>
                <TableSortLabel
                  active={orderBy === 'isComplete'}
                  direction={orderBy === 'isComplete' ? order : 'asc'}
                  onClick={() => handleRequestSort('isComplete')}
                >
                  Status
                </TableSortLabel>
              </TableCell>
              <TableCell align="center">Answers</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {paginatedResponses.map((response) => (
              <Fragment key={response.id}>
                <TableRow hover>
                  <TableCell padding="checkbox">
                    <IconButton
                      size="small"
                      onClick={() => toggleRow(response.id)}
                    >
                      {expandedRows.has(response.id) ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                    </IconButton>
                  </TableCell>
                  <TableCell>{response.respondentTelegramId}</TableCell>
                  <TableCell>{formatDate(response.startedAt)}</TableCell>
                  <TableCell>{formatDate(response.submittedAt)}</TableCell>
                  <TableCell>
                    <Chip
                      icon={response.isComplete ? <CheckCircleIcon /> : <IncompleteIcon />}
                      label={response.isComplete ? 'Complete' : 'Incomplete'}
                      color={response.isComplete ? 'success' : 'default'}
                      size="small"
                    />
                  </TableCell>
                  <TableCell align="center">
                    {response.answers?.length || 0} / {survey.questions.length}
                  </TableCell>
                </TableRow>
                <TableRow>
                  <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={6}>
                    <Collapse in={expandedRows.has(response.id)} timeout="auto" unmountOnExit>
                      <Box sx={{ margin: 2 }}>
                        <Typography variant="h6" gutterBottom component="div">
                          Response Details
                        </Typography>
                        {response.answers && response.answers.length > 0 ? (
                          <Table size="small">
                            <TableHead>
                              <TableRow>
                                <TableCell>Question</TableCell>
                                <TableCell>Answer</TableCell>
                              </TableRow>
                            </TableHead>
                            <TableBody>
                              {response.answers.map((answer) => {
                                const question = survey.questions.find(
                                  (q) => q.id === answer.questionId
                                );
                                return (
                                  <TableRow key={answer.id}>
                                    <TableCell component="th" scope="row">
                                      {question?.questionText || 'Unknown Question'}
                                    </TableCell>
                                    <TableCell>
                                      {getAnswerDisplay(answer, question?.questionType || 0)}
                                    </TableCell>
                                  </TableRow>
                                );
                              })}
                            </TableBody>
                          </Table>
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            No answers submitted yet
                          </Typography>
                        )}
                      </Box>
                    </Collapse>
                  </TableCell>
                </TableRow>
              </Fragment>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      <TablePagination
        rowsPerPageOptions={[10, 25, 50]}
        component="div"
        count={responses.length}
        rowsPerPage={rowsPerPage}
        page={page}
        onPageChange={handleChangePage}
        onRowsPerPageChange={handleChangeRowsPerPage}
      />
    </Paper>
  );
};

export default ResponsesTable;
