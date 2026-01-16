import { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  FormControlLabel,
  Checkbox,
  RadioGroup,
  Radio,
  Box,
  Typography,
  Alert,
  CircularProgress,
  Tooltip,
} from '@mui/material';
import { Download as DownloadIcon, Close as CloseIcon } from '@mui/icons-material';

export interface ExportOptions {
  includeMetadata: boolean;
  includeTimestamps: boolean;
  exportFormat: 'all' | 'completed' | 'incomplete';
  questionFilter: 'all' | 'statistics_only';
  delimiter: ',' | ';';  // CSV delimiter - semicolon recommended for Russian/European Excel
  encoding: 'utf-8' | 'windows-1251';  // File encoding - Windows-1251 for Russian Excel
}

interface ExportDialogProps {
  open: boolean;
  onClose: () => void;
  onExport: (options: ExportOptions) => Promise<void>;
  responseCount: number;
  completedCount: number;
  incompleteCount: number;
  surveyTitle: string;
  totalQuestionCount: number;
  statisticsQuestionCount: number;
}

const ExportDialog = ({
  open,
  onClose,
  onExport,
  responseCount,
  completedCount,
  incompleteCount,
  surveyTitle,
  totalQuestionCount,
  statisticsQuestionCount,
}: ExportDialogProps) => {
  const [options, setOptions] = useState<ExportOptions>({
    includeMetadata: true,
    includeTimestamps: true,
    exportFormat: 'completed',
    questionFilter: 'all',
    delimiter: ';',  // Default semicolon for Russian/European Excel compatibility
    encoding: 'windows-1251',  // Default Windows-1251 for Russian Excel compatibility
  });
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleExport = async () => {
    try {
      setExporting(true);
      setError(null);
      await onExport(options);
      // Success - dialog will be closed by parent component
    } catch (err: any) {
      console.error('Export failed:', err);
      setError(err.message || 'Failed to export data. Please try again.');
      setExporting(false);
    }
  };

  const handleClose = () => {
    if (!exporting) {
      setError(null);
      onClose();
    }
  };

  const getExportCount = () => {
    switch (options.exportFormat) {
      case 'all':
        return responseCount;
      case 'completed':
        return completedCount;
      case 'incomplete':
        return incompleteCount;
      default:
        return 0;
    }
  };

  const exportCount = getExportCount();

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Typography variant="h6">Export Survey Data</Typography>
          {!exporting && (
            <Button
              onClick={handleClose}
              size="small"
              sx={{ minWidth: 'auto', p: 0.5 }}
            >
              <CloseIcon />
            </Button>
          )}
        </Box>
      </DialogTitle>

      <DialogContent dividers>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        <Box sx={{ mb: 3 }}>
          <Typography variant="subtitle2" gutterBottom>
            Survey: {surveyTitle}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Export responses to CSV format for analysis in Excel, Google Sheets, or other tools.
          </Typography>
        </Box>

        {/* Export Format Selection */}
        <Box sx={{ mb: 3 }}>
          <Typography variant="subtitle2" gutterBottom>
            Response Filter
          </Typography>
          <RadioGroup
            value={options.exportFormat}
            onChange={(e) =>
              setOptions({ ...options, exportFormat: e.target.value as ExportOptions['exportFormat'] })
            }
          >
            <FormControlLabel
              value="completed"
              control={<Radio />}
              label={
                <Box>
                  <Typography variant="body2">Completed Responses Only</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {completedCount} {completedCount === 1 ? 'response' : 'responses'}
                  </Typography>
                </Box>
              }
            />
            <FormControlLabel
              value="incomplete"
              control={<Radio />}
              label={
                <Box>
                  <Typography variant="body2">Incomplete Responses</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {incompleteCount} {incompleteCount === 1 ? 'response' : 'responses'}
                  </Typography>
                </Box>
              }
            />
            <FormControlLabel
              value="all"
              control={<Radio />}
              label={
                <Box>
                  <Typography variant="body2">All Responses</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {responseCount} total {responseCount === 1 ? 'response' : 'responses'}
                  </Typography>
                </Box>
              }
            />
          </RadioGroup>
        </Box>

        {/* Question Filter */}
        <Box sx={{ mb: 3 }}>
          <Typography variant="subtitle2" gutterBottom>
            Question Filter
          </Typography>
          <RadioGroup
            value={options.questionFilter}
            onChange={(e) =>
              setOptions({ ...options, questionFilter: e.target.value as 'all' | 'statistics_only' })
            }
          >
            <FormControlLabel
              value="all"
              control={<Radio />}
              label={
                <Box>
                  <Typography variant="body2">All Questions</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {totalQuestionCount} {totalQuestionCount === 1 ? 'question' : 'questions'}
                  </Typography>
                </Box>
              }
            />
            <FormControlLabel
              value="statistics_only"
              control={<Radio />}
              label={
                <Box>
                  <Typography variant="body2">Display in Statistics Only</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {statisticsQuestionCount} {statisticsQuestionCount === 1 ? 'question' : 'questions'}
                  </Typography>
                </Box>
              }
            />
          </RadioGroup>
        </Box>

        {/* Additional Options */}
        <Box sx={{ mb: 2 }}>
          <Typography variant="subtitle2" gutterBottom>
            Additional Options
          </Typography>
          <Tooltip title="Include Response ID, Respondent ID, and completion status">
            <FormControlLabel
              control={
                <Checkbox
                  checked={options.includeMetadata}
                  onChange={(e) =>
                    setOptions({ ...options, includeMetadata: e.target.checked })
                  }
                />
              }
              label="Include metadata columns"
            />
          </Tooltip>
          <Tooltip title="Include response start and submission timestamps">
            <FormControlLabel
              control={
                <Checkbox
                  checked={options.includeTimestamps}
                  onChange={(e) =>
                    setOptions({ ...options, includeTimestamps: e.target.checked })
                  }
                />
              }
              label="Include timestamps"
            />
          </Tooltip>
        </Box>

        {/* Delimiter Selection */}
        <Box sx={{ mb: 2 }}>
          <Typography variant="subtitle2" gutterBottom>
            CSV Delimiter
          </Typography>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
            Use semicolon for Russian/European Excel locales to display Cyrillic correctly
          </Typography>
          <RadioGroup
            row
            value={options.delimiter}
            onChange={(e) =>
              setOptions({ ...options, delimiter: e.target.value as ',' | ';' })
            }
          >
            <FormControlLabel
              value=";"
              control={<Radio />}
              label="Semicolon (;) - Recommended"
            />
            <FormControlLabel
              value=","
              control={<Radio />}
              label="Comma (,) - Standard"
            />
          </RadioGroup>
        </Box>

        {/* Encoding Selection */}
        <Box sx={{ mb: 2 }}>
          <Typography variant="subtitle2" gutterBottom>
            File Encoding
          </Typography>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
            Windows-1251 for Russian Excel, UTF-8 for Google Sheets/LibreOffice
          </Typography>
          <RadioGroup
            row
            value={options.encoding}
            onChange={(e) =>
              setOptions({ ...options, encoding: e.target.value as 'utf-8' | 'windows-1251' })
            }
          >
            <FormControlLabel
              value="windows-1251"
              control={<Radio />}
              label="Windows-1251 (Excel RU)"
            />
            <FormControlLabel
              value="utf-8"
              control={<Radio />}
              label="UTF-8 (Universal)"
            />
          </RadioGroup>
        </Box>

        {exportCount > 1000 && (
          <Alert severity="info" sx={{ mt: 2 }}>
            This survey has a large number of responses ({exportCount}). Export may take a moment.
          </Alert>
        )}

        {exportCount === 0 && (
          <Alert severity="warning" sx={{ mt: 2 }}>
            No responses match the selected criteria.
          </Alert>
        )}
      </DialogContent>

      <DialogActions>
        <Button onClick={handleClose} disabled={exporting}>
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={handleExport}
          disabled={exporting || exportCount === 0}
          startIcon={exporting ? <CircularProgress size={20} /> : <DownloadIcon />}
        >
          {exporting ? 'Exporting...' : `Export ${exportCount} ${exportCount === 1 ? 'Response' : 'Responses'}`}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ExportDialog;
