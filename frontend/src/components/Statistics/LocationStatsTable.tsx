import {
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  Link,
  IconButton,
  Box,
} from '@mui/material';
import { OpenInNew as OpenInNewIcon } from '@mui/icons-material';
import { format } from 'date-fns';

interface LocationAnswer {
  respondentId?: number;
  latitude: number;
  longitude: number;
  answeredAt?: string;
}

interface LocationStatsTableProps {
  answers: LocationAnswer[];
}

const LocationStatsTable = ({ answers }: LocationStatsTableProps) => {
  const formatDate = (dateString: string | undefined) => {
    if (!dateString) return 'N/A';
    try {
      return format(new Date(dateString), 'dd.MM.yyyy HH:mm');
    } catch {
      return 'N/A';
    }
  };

  const formatCoordinate = (coordinate: number): string => {
    return coordinate.toFixed(6);
  };

  const getGoogleMapsUrl = (latitude: number, longitude: number): string => {
    return `https://www.google.com/maps?q=${latitude},${longitude}`;
  };

  if (answers.length === 0) {
    return (
      <Paper sx={{ p: 3, textAlign: 'center' }}>
        <Typography color="text.secondary">No location data available</Typography>
      </Paper>
    );
  }

  return (
    <TableContainer
      component={Paper}
      sx={{
        overflowX: 'auto',
        '& .MuiTable-root': {
          minWidth: 650,
        },
      }}
    >
      <Table>
        <TableHead>
          <TableRow>
            <TableCell sx={{ fontWeight: 600 }}>#</TableCell>
            <TableCell sx={{ fontWeight: 600 }}>Latitude</TableCell>
            <TableCell sx={{ fontWeight: 600 }}>Longitude</TableCell>
            <TableCell sx={{ fontWeight: 600 }}>Map Link</TableCell>
            {answers.some((a) => a.answeredAt) && (
              <TableCell sx={{ fontWeight: 600 }}>Answered At</TableCell>
            )}
          </TableRow>
        </TableHead>
        <TableBody>
          {answers.map((answer, index) => (
            <TableRow
              key={answer.respondentId || index}
              hover
              sx={{
                '&:hover': {
                  backgroundColor: 'action.hover',
                },
              }}
            >
              <TableCell>{index + 1}</TableCell>
              <TableCell>{formatCoordinate(answer.latitude)}</TableCell>
              <TableCell>{formatCoordinate(answer.longitude)}</TableCell>
              <TableCell>
                <Link
                  href={getGoogleMapsUrl(answer.latitude, answer.longitude)}
                  target="_blank"
                  rel="noopener noreferrer"
                  sx={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    textDecoration: 'none',
                    color: 'primary.main',
                    '&:hover': {
                      textDecoration: 'underline',
                    },
                  }}
                >
                  <Box component="span" sx={{ mr: 0.5 }}>
                    View on Map
                  </Box>
                  <OpenInNewIcon fontSize="small" />
                </Link>
              </TableCell>
              {answers.some((a) => a.answeredAt) && (
                <TableCell>{formatDate(answer.answeredAt)}</TableCell>
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
};

export default LocationStatsTable;
