/**
 * RichContentRenderer Usage Examples
 *
 * This file demonstrates how to use the RichContentRenderer component
 * in different scenarios within the SurveyBot application.
 */

import React from 'react';
import { Box, Card, CardContent, Typography, Paper } from '@mui/material';
import { RichContentRenderer } from './RichContentRenderer';
import type { MediaContentDto } from '../types/media';

/**
 * Example 1: Basic HTML Rendering
 * Simple rich text without any media
 */
export const BasicRichTextExample: React.FC = () => {
  const htmlContent = `
    <h2>Welcome to Our Survey</h2>
    <p>This is a <strong>simple survey</strong> about your preferences.</p>
    <p>Please take a moment to answer the following questions:</p>
    <ul>
      <li>What is your favorite color?</li>
      <li>How often do you exercise?</li>
      <li>What are your hobbies?</li>
    </ul>
  `;

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Basic Rich Text Example
        </Typography>
        <RichContentRenderer htmlContent={htmlContent} />
      </CardContent>
    </Card>
  );
};

/**
 * Example 2: Rich Text with Images
 * Question with embedded image media
 */
export const RichTextWithImagesExample: React.FC = () => {
  const htmlContent = `
    <h3>Product Feedback</h3>
    <p>Please review the product shown below and provide your feedback:</p>
  `;

  const mediaContent: MediaContentDto = {
    version: '1.0',
    items: [
      {
        id: 'img-1',
        type: 'image',
        filePath: '/uploads/products/product-123.jpg',
        thumbnailPath: '/uploads/products/product-123-thumb.jpg',
        displayName: 'Product XYZ',
        fileSize: 1024 * 500, // 500 KB
        mimeType: 'image/jpeg',
        uploadedAt: new Date().toISOString(),
        altText: 'Our new flagship product with improved features',
        order: 0,
      },
    ],
  };

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Rich Text with Images
        </Typography>
        <RichContentRenderer
          htmlContent={htmlContent}
          mediaContent={mediaContent}
        />
      </CardContent>
    </Card>
  );
};

/**
 * Example 3: Question Display in Survey
 * How to use in QuestionDisplay component
 */
export const QuestionDisplayExample: React.FC = () => {
  const questionText = `
    <h3>Video Tutorial Review</h3>
    <p>Watch the following tutorial video and rate your understanding:</p>
    <ul>
      <li>Was the explanation clear?</li>
      <li>Did you learn something new?</li>
      <li>Would you recommend this to others?</li>
    </ul>
  `;

  const mediaContent: MediaContentDto = {
    version: '1.0',
    items: [
      {
        id: 'vid-1',
        type: 'video',
        filePath: '/uploads/tutorials/intro-tutorial.mp4',
        displayName: 'Introduction Tutorial',
        fileSize: 1024 * 1024 * 15, // 15 MB
        mimeType: 'video/mp4',
        uploadedAt: new Date().toISOString(),
        altText: 'Tutorial covering basic features and navigation',
        order: 0,
      },
    ],
  };

  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h6" gutterBottom>
        Question with Video
      </Typography>
      <Box sx={{ mt: 2 }}>
        <RichContentRenderer
          htmlContent={questionText}
          mediaContent={mediaContent}
        />
      </Box>

      {/* Answer input would go here */}
      <Box sx={{ mt: 3, p: 2, bgcolor: 'grey.50', borderRadius: 1 }}>
        <Typography variant="body2" color="text.secondary">
          [Rating input would appear here]
        </Typography>
      </Box>
    </Paper>
  );
};

/**
 * Example 4: Multiple Media Types
 * Question with various media attachments
 */
export const MultipleMediaExample: React.FC = () => {
  const htmlContent = `
    <h3>Training Materials Review</h3>
    <p>Please review all the training materials provided below:</p>
  `;

  const mediaContent: MediaContentDto = {
    version: '1.0',
    items: [
      {
        id: 'img-1',
        type: 'image',
        filePath: '/uploads/diagram.png',
        thumbnailPath: '/uploads/diagram-thumb.png',
        displayName: 'Process Diagram',
        fileSize: 1024 * 300,
        mimeType: 'image/png',
        uploadedAt: new Date().toISOString(),
        altText: 'Workflow process diagram',
        order: 0,
      },
      {
        id: 'audio-1',
        type: 'audio',
        filePath: '/uploads/narration.mp3',
        displayName: 'Audio Narration',
        fileSize: 1024 * 1024 * 5,
        mimeType: 'audio/mpeg',
        uploadedAt: new Date().toISOString(),
        altText: 'Audio explanation of the process',
        order: 1,
      },
      {
        id: 'doc-1',
        type: 'document',
        filePath: '/uploads/guide.pdf',
        displayName: 'Complete Guide.pdf',
        fileSize: 1024 * 1024 * 2,
        mimeType: 'application/pdf',
        uploadedAt: new Date().toISOString(),
        altText: 'Detailed step-by-step guide',
        order: 2,
      },
    ],
  };

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Multiple Media Types Example
        </Typography>
        <RichContentRenderer
          htmlContent={htmlContent}
          mediaContent={mediaContent}
        />
      </CardContent>
    </Card>
  );
};

/**
 * Example 5: XSS Protection Demonstration
 * Shows that potentially dangerous HTML is sanitized
 */
export const XSSProtectionExample: React.FC = () => {
  // This HTML contains potentially dangerous content that will be sanitized
  const dangerousHtml = `
    <h3>Security Test</h3>
    <p>This is safe content.</p>
    <script>alert('This will be removed by DOMPurify!');</script>
    <p onclick="alert('Click handler removed')">This paragraph is safe.</p>
    <img src="x" onerror="alert('XSS attempt blocked')">
    <p>All dangerous scripts and event handlers are automatically removed.</p>
  `;

  return (
    <Card sx={{ border: '2px solid', borderColor: 'success.main' }}>
      <CardContent>
        <Typography variant="h6" gutterBottom color="success.main">
          XSS Protection Example
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          The content below contains script tags and event handlers,
          but they are safely removed by DOMPurify.
        </Typography>
        <RichContentRenderer htmlContent={dangerousHtml} />
        <Typography variant="caption" color="text.secondary" sx={{ mt: 2, display: 'block' }}>
          No alerts will appear - all dangerous code has been sanitized!
        </Typography>
      </CardContent>
    </Card>
  );
};

/**
 * Example 6: Survey Response View
 * Displaying a question as seen by survey respondents
 */
export const SurveyResponseViewExample: React.FC = () => {
  const questionHtml = `
    <h3>Customer Satisfaction Survey</h3>
    <p>How would you rate your experience with our service?</p>
    <blockquote>
      Please be honest - your feedback helps us improve!
    </blockquote>
  `;

  return (
    <Box sx={{ maxWidth: 800, mx: 'auto', p: 3 }}>
      <Paper elevation={2} sx={{ p: 4 }}>
        <RichContentRenderer
          htmlContent={questionHtml}
          className="survey-question"
        />

        {/* Rating buttons would go here */}
        <Box sx={{ mt: 4, display: 'flex', gap: 1, justifyContent: 'center' }}>
          {[1, 2, 3, 4, 5].map((rating) => (
            <Box
              key={rating}
              sx={{
                width: 50,
                height: 50,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                border: '2px solid',
                borderColor: 'primary.main',
                borderRadius: 1,
                cursor: 'pointer',
                '&:hover': {
                  bgcolor: 'primary.light',
                },
              }}
            >
              {rating}
            </Box>
          ))}
        </Box>
      </Paper>
    </Box>
  );
};

/**
 * Example 7: Custom Styled Renderer
 * Using className prop for custom styling
 */
export const CustomStyledExample: React.FC = () => {
  const htmlContent = `
    <h2>Special Announcement</h2>
    <p>This content uses custom styling through the className prop.</p>
    <ul>
      <li>Custom fonts</li>
      <li>Custom colors</li>
      <li>Custom spacing</li>
    </ul>
  `;

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Custom Styled Example
        </Typography>
        <Box
          sx={{
            '& .custom-announcement': {
              fontFamily: 'Georgia, serif',
              color: 'primary.main',
              '& h2': {
                borderBottom: '3px solid',
                borderColor: 'primary.main',
                paddingBottom: 1,
              },
              '& ul': {
                listStyleType: 'square',
              },
            },
          }}
        >
          <RichContentRenderer
            htmlContent={htmlContent}
            className="custom-announcement"
          />
        </Box>
      </CardContent>
    </Card>
  );
};

/**
 * Complete Demo Component
 * Shows all examples together
 */
export const RichContentRendererDemo: React.FC = () => {
  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" gutterBottom>
        RichContentRenderer Examples
      </Typography>

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3, mt: 3 }}>
        <BasicRichTextExample />
        <RichTextWithImagesExample />
        <QuestionDisplayExample />
        <MultipleMediaExample />
        <XSSProtectionExample />
        <SurveyResponseViewExample />
        <CustomStyledExample />
      </Box>
    </Box>
  );
};

export default RichContentRendererDemo;
