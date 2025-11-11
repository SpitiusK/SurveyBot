import React from 'react';
import SurveyBuilder from './SurveyBuilder';

/**
 * SurveyEdit component
 *
 * This is just a wrapper that renders SurveyBuilder in edit mode.
 * The SurveyBuilder component detects edit mode based on the presence
 * of an 'id' parameter in the URL.
 */
const SurveyEdit: React.FC = () => {
  return <SurveyBuilder />;
};

export default SurveyEdit;
