# Location Statistics Implementation Plan

**Feature**: Location Question Statistics Visualization with Leaflet
**Version**: 1.6.3 (planned)
**Created**: 2025-12-04
**Status**: AWAITING APPROVAL
**Estimated Effort**: 8-12 hours

---

## Executive Summary

This document outlines the implementation plan for adding location question statistics visualization to the SurveyBot frontend admin panel using Leaflet maps. The feature will display survey response locations as interactive markers on an OpenStreetMap-based map.

### Current State
- Backend has `LocationAnswerValue` value object fully implemented
- Backend stores location data in JSONB format (latitude, longitude, accuracy, timestamp)
- Frontend statistics architecture uses consistent patterns for other question types
- **Missing**: Location statistics calculation and visualization

### Proposed Solution
- Add `LocationStatisticsDto` to backend for location data aggregation
- Implement `CalculateLocationStatistics()` method following existing patterns
- Create `LeafletMapStats` React component for map visualization
- Integrate with existing `QuestionCard` statistics dispatcher

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Phase 1: Backend Implementation](#2-phase-1-backend-implementation)
3. [Phase 2: Frontend Type Definitions](#3-phase-2-frontend-type-definitions)
4. [Phase 3: Leaflet Component Implementation](#4-phase-3-leaflet-component-implementation)
5. [Phase 4: Integration with QuestionCard](#5-phase-4-integration-with-questioncard)
6. [Phase 5: Testing & Verification](#6-phase-5-testing--verification)
7. [Phase 6: Documentation](#7-phase-6-documentation)
8. [Dependencies & Prerequisites](#8-dependencies--prerequisites)
9. [Risk Assessment](#9-risk-assessment)
10. [Acceptance Criteria](#10-acceptance-criteria)
11. [Appendix: Code Specifications](#11-appendix-code-specifications)

---

## 1. Architecture Overview

### Data Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│ PostgreSQL Database                                                  │
│ answers.answer_value_json (JSONB)                                   │
│ { "latitude": 40.71, "longitude": -74.00, "accuracy": 10.5, ... }   │
└───────────────────┬─────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│ Infrastructure Layer                                                 │
│ SurveyService.CalculateLocationStatistics()                         │
│ → Extracts LocationAnswerValue from answers                         │
│ → Calculates bounds (min/max lat/lng)                               │
│ → Returns LocationStatisticsDto                                     │
└───────────────────┬─────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│ API Layer                                                           │
│ GET /api/surveys/{id}/statistics                                    │
│ → Returns SurveyStatisticsDto with locationStatistics               │
└───────────────────┬─────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│ Frontend Service Layer                                              │
│ surveyService.getSurveyStatistics(id)                               │
└───────────────────┬─────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│ Frontend Components                                                  │
│ SurveyStatistics → QuestionStatistics → QuestionCard                │
│                                              ↓                       │
│                                       LeafletMapStats (NEW)         │
│                                       - OpenStreetMap tiles          │
│                                       - Markers for each location    │
│                                       - Popups with details          │
└─────────────────────────────────────────────────────────────────────┘
```

### Files to Create/Modify

| Layer | File | Action | Description |
|-------|------|--------|-------------|
| Core | `DTOs/Statistics/LocationStatisticsDto.cs` | CREATE | Location statistics DTO |
| Core | `DTOs/Statistics/QuestionStatisticsDto.cs` | MODIFY | Add LocationStatistics property |
| Infrastructure | `Services/SurveyService.cs` | MODIFY | Add CalculateLocationStatistics method |
| Frontend | `src/types/index.ts` | MODIFY | Add TypeScript interfaces |
| Frontend | `src/components/Statistics/LeafletMapStats.tsx` | CREATE | Map visualization component |
| Frontend | `src/components/Statistics/QuestionCard.tsx` | MODIFY | Add Location case |
| Frontend | `src/main.tsx` | MODIFY | Import Leaflet CSS |
| Frontend | `package.json` | MODIFY | Add Leaflet dependencies |

---

## 2. Phase 1: Backend Implementation

**Estimated Time**: 2-3 hours
**Complexity**: Low-Medium
**Agent**: @aspnet-api-agent

### Task 1.1: Create LocationStatisticsDto

**File**: `src/SurveyBot.Core/DTOs/Statistics/LocationStatisticsDto.cs` (NEW)

**Purpose**: Define DTO structure for location statistics data

**Properties**:
| Property | Type | Description |
|----------|------|-------------|
| TotalLocations | int | Count of location responses |
| MinLatitude | double? | South bound |
| MaxLatitude | double? | North bound |
| MinLongitude | double? | West bound |
| MaxLongitude | double? | East bound |
| CenterLatitude | double? | Average latitude |
| CenterLongitude | double? | Average longitude |
| Locations | List&lt;LocationDataPointDto&gt; | Individual data points |

**LocationDataPointDto Properties**:
| Property | Type | Description |
|----------|------|-------------|
| Latitude | double | Latitude (-90 to 90) |
| Longitude | double | Longitude (-180 to 180) |
| Accuracy | double? | Accuracy in meters |
| Timestamp | DateTime? | Capture time |
| ResponseId | int | Link to response |

### Task 1.2: Implement CalculateLocationStatistics Method

**File**: `src/SurveyBot.Infrastructure/Services/SurveyService.cs`

**Location**: After `CalculateDateStatistics()` method (around line 1250)

**Implementation Pattern**:
1. Pattern match on `LocationAnswerValue` from polymorphic hierarchy
2. Fallback to legacy `AnswerJson` parsing for backward compatibility
3. Calculate geographic bounds (min/max lat/lng)
4. Calculate center point (average lat/lng)
5. Return `LocationStatisticsDto` with all data points

### Task 1.3: Add Location Case to Switch Statement

**File**: `src/SurveyBot.Infrastructure/Services/SurveyService.cs`

**Location**: Lines 881-903 in `CalculateQuestionStatisticsAsync()` method

**Change**: Add case for `QuestionType.Location` calling `CalculateLocationStatistics()`

### Task 1.4: Update QuestionStatisticsDto

**File**: `src/SurveyBot.Core/DTOs/Statistics/QuestionStatisticsDto.cs`

**Change**: Add `LocationStatistics` nullable property

### Task 1.5: Test Backend Implementation

**Verification Steps**:
1. Create survey with location question
2. Submit responses with different locations
3. Call `GET /api/surveys/{id}/statistics`
4. Verify `locationStatistics` object in response
5. Verify bounds calculation accuracy
6. Verify all data points returned

---

## 3. Phase 2: Frontend Type Definitions

**Estimated Time**: 30 minutes
**Complexity**: Low
**Agent**: @frontend-admin-agent

### Task 2.1: Add TypeScript Interfaces

**File**: `frontend/src/types/index.ts`

**New Interfaces**:
```typescript
interface LocationDataPoint {
  latitude: number;
  longitude: number;
  accuracy?: number | null;
  timestamp?: string | null;
  responseId: number;
}

interface LocationStatistics {
  totalLocations: number;
  minLatitude?: number | null;
  maxLatitude?: number | null;
  minLongitude?: number | null;
  maxLongitude?: number | null;
  centerLatitude?: number | null;
  centerLongitude?: number | null;
  locations: LocationDataPoint[];
}
```

### Task 2.2: Update QuestionStatistics Interface

**File**: `frontend/src/types/index.ts`

**Change**: Add `locationStatistics?: LocationStatistics` property

---

## 4. Phase 3: Leaflet Component Implementation

**Estimated Time**: 3-4 hours
**Complexity**: Medium
**Agent**: @frontend-admin-agent

### Task 3.1: Install Leaflet Dependencies

**Command**:
```bash
cd frontend
npm install leaflet react-leaflet
npm install --save-dev @types/leaflet
```

**Expected Versions**:
- leaflet: ^1.9.4
- react-leaflet: ^4.2.1
- @types/leaflet: ^1.9.12

### Task 3.2: Create LeafletMapStats Component

**File**: `frontend/src/components/Statistics/LeafletMapStats.tsx` (NEW)

**Component Features**:
| Feature | Description |
|---------|-------------|
| Map Container | OpenStreetMap tiles via react-leaflet |
| Markers | One marker per location response |
| Popups | Details (coordinates, accuracy, timestamp, response ID) |
| Auto-fit Bounds | Map viewport fits all markers |
| Empty State | "No locations recorded" message |
| Responsive | Resizes on mobile breakpoints |

**Props Interface**:
```typescript
interface LeafletMapStatsProps {
  data: LocationStatistics;
  questionText: string;
}
```

### Task 3.3: Fix Leaflet Default Marker Icon Issue

**Problem**: Leaflet markers don't show in React due to asset path issues

**Solution**: Configure custom icon paths at component initialization

### Task 3.4: Import Leaflet CSS

**File**: `frontend/src/main.tsx`

**Change**: Add `import 'leaflet/dist/leaflet.css';`

---

## 5. Phase 4: Integration with QuestionCard

**Estimated Time**: 15 minutes
**Complexity**: Low
**Agent**: @frontend-admin-agent

### Task 4.1: Update QuestionCard Component

**File**: `frontend/src/components/Statistics/QuestionCard.tsx`

**Changes**:
1. Import `LeafletMapStats` component
2. Add `case QuestionType.Location` to switch statement
3. Render `LeafletMapStats` when `locationStatistics` exists

---

## 6. Phase 5: Testing & Verification

**Estimated Time**: 2-3 hours
**Complexity**: Medium

### Test Scenarios

| # | Scenario | Expected Behavior |
|---|----------|-------------------|
| 5.1 | Zero location responses | Shows "No locations recorded" message |
| 5.2 | Single location response | Map centers on marker, popup works |
| 5.3 | Multiple clustered locations | All markers visible, appropriate zoom |
| 5.4 | Widely distributed locations | Bounds fit all markers automatically |
| 5.5 | Mobile viewport (<768px) | Map resizes, markers clickable |
| 5.6 | 100+ location responses | Performance acceptable (<2s render) |

### Frontend Verification (via @frontend-story-verifier)

**User Story**: As an admin, I want to view location responses on a map so that I can see geographic distribution of survey responses.

**Test Steps**:
1. Navigate to survey statistics page
2. Locate location question section
3. Verify map displays with all response markers
4. Click marker to view popup details
5. Verify popup shows coordinates, accuracy, timestamp, response ID
6. Verify map bounds fit all markers
7. Test on mobile viewport

---

## 7. Phase 6: Documentation

**Estimated Time**: 30 minutes
**Complexity**: Low
**Agent**: @claude-md-documentation-agent

### Documentation Updates

| File | Section to Update |
|------|-------------------|
| `src/SurveyBot.Core/CLAUDE.md` | DTOs section - add LocationStatisticsDto |
| `src/SurveyBot.Infrastructure/CLAUDE.md` | Services section - add CalculateLocationStatistics |
| `frontend/CLAUDE.md` | Components section - add LeafletMapStats |
| `CLAUDE.md` (root) | Recent Changes - add v1.6.3 location statistics |

---

## 8. Dependencies & Prerequisites

### Backend Dependencies
- No new NuGet packages required
- Uses existing `System.Text.Json` for JSON parsing

### Frontend Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| leaflet | ^1.9.4 | Map rendering library |
| react-leaflet | ^4.2.1 | React wrapper for Leaflet |
| @types/leaflet | ^1.9.12 | TypeScript definitions |

### Prerequisites
- Docker containers running (PostgreSQL)
- Survey with location question exists for testing
- Location responses submitted for testing

---

## 9. Risk Assessment

### Low Risk
| Risk | Mitigation |
|------|------------|
| TypeScript compilation errors | Install @types/leaflet |
| Bundle size increase | Leaflet is ~39KB gzipped (acceptable) |

### Medium Risk
| Risk | Mitigation |
|------|------------|
| Leaflet marker icons not showing | Configure custom icon paths (documented solution) |
| Map container height zero | Explicit height in CSS (400px) |

### Considerations
| Item | Decision |
|------|----------|
| Map tile provider | OpenStreetMap (free, no API key required) |
| Marker clustering | Not included in initial implementation (can add later) |
| Heatmap visualization | Not included in initial implementation (can add later) |

---

## 10. Acceptance Criteria

### Backend
- [ ] `LocationStatisticsDto` class created with all properties
- [ ] `LocationDataPointDto` class created with all properties
- [ ] `CalculateLocationStatistics()` method implemented
- [ ] `QuestionType.Location` case added to statistics switch
- [ ] `QuestionStatisticsDto.LocationStatistics` property added
- [ ] API endpoint returns location statistics correctly
- [ ] No compilation errors

### Frontend
- [ ] TypeScript interfaces defined (`LocationStatistics`, `LocationDataPoint`)
- [ ] `QuestionStatistics` interface updated
- [ ] Leaflet packages installed
- [ ] `LeafletMapStats` component created
- [ ] Leaflet CSS imported
- [ ] `QuestionCard` updated with Location case
- [ ] Map renders correctly with markers
- [ ] Marker popups display correct information
- [ ] Empty state handled gracefully
- [ ] Mobile responsive
- [ ] No console errors

### Integration
- [ ] Full flow works end-to-end
- [ ] Statistics page displays location map for location questions
- [ ] Multiple locations display correctly
- [ ] Performance acceptable with many locations

### Documentation
- [ ] Core CLAUDE.md updated
- [ ] Infrastructure CLAUDE.md updated
- [ ] Frontend CLAUDE.md updated
- [ ] Root CLAUDE.md updated with version notes

---

## 11. Appendix: Code Specifications

### A. LocationStatisticsDto.cs (Full Code)

```csharp
using System;
using System.Collections.Generic;

namespace SurveyBot.Core.DTOs.Statistics;

/// <summary>
/// Statistics for location-type questions.
/// Contains all location data points and geographic bounds for map visualization.
/// </summary>
public class LocationStatisticsDto
{
    /// <summary>
    /// Total number of location responses.
    /// </summary>
    public int TotalLocations { get; set; }

    /// <summary>
    /// Minimum latitude across all locations (south bound).
    /// </summary>
    public double? MinLatitude { get; set; }

    /// <summary>
    /// Maximum latitude across all locations (north bound).
    /// </summary>
    public double? MaxLatitude { get; set; }

    /// <summary>
    /// Minimum longitude across all locations (west bound).
    /// </summary>
    public double? MinLongitude { get; set; }

    /// <summary>
    /// Maximum longitude across all locations (east bound).
    /// </summary>
    public double? MaxLongitude { get; set; }

    /// <summary>
    /// Geographic center latitude (average of all locations).
    /// </summary>
    public double? CenterLatitude { get; set; }

    /// <summary>
    /// Geographic center longitude (average of all locations).
    /// </summary>
    public double? CenterLongitude { get; set; }

    /// <summary>
    /// Individual location data points for map markers.
    /// </summary>
    public List<LocationDataPointDto> Locations { get; set; } = new();
}

/// <summary>
/// Individual location data point for a single response.
/// </summary>
public class LocationDataPointDto
{
    /// <summary>
    /// Latitude in decimal degrees (-90 to 90).
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude in decimal degrees (-180 to 180).
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Accuracy radius in meters (optional).
    /// </summary>
    public double? Accuracy { get; set; }

    /// <summary>
    /// Timestamp when location was captured (optional).
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Response ID for linking to response details.
    /// </summary>
    public int ResponseId { get; set; }
}
```

### B. CalculateLocationStatistics Method (Full Code)

```csharp
/// <summary>
/// Calculates statistics for location-type questions.
/// Extracts location data points and calculates geographic bounds.
/// </summary>
/// <param name="answers">Answers for the location question</param>
/// <returns>Location statistics with bounds and data points</returns>
private LocationStatisticsDto CalculateLocationStatistics(List<Answer> answers)
{
    var locations = new List<LocationDataPointDto>();

    foreach (var answer in answers)
    {
        switch (answer.Value)
        {
            case LocationAnswerValue locationValue:
                locations.Add(new LocationDataPointDto
                {
                    Latitude = locationValue.Latitude,
                    Longitude = locationValue.Longitude,
                    Accuracy = locationValue.Accuracy,
                    Timestamp = locationValue.Timestamp,
                    ResponseId = answer.ResponseId
                });
                break;

            case null:
                // Legacy fallback
                if (!string.IsNullOrWhiteSpace(answer.AnswerJson))
                {
                    try
                    {
                        var data = JsonSerializer.Deserialize<JsonElement>(answer.AnswerJson);
                        if (data.TryGetProperty("latitude", out var lat) &&
                            data.TryGetProperty("longitude", out var lon))
                        {
                            locations.Add(new LocationDataPointDto
                            {
                                Latitude = lat.GetDouble(),
                                Longitude = lon.GetDouble(),
                                Accuracy = data.TryGetProperty("accuracy", out var acc)
                                    ? acc.GetDouble()
                                    : null,
                                Timestamp = data.TryGetProperty("timestamp", out var ts)
                                    ? ts.GetDateTime()
                                    : null,
                                ResponseId = answer.ResponseId
                            });
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to parse location answer JSON for answer ID {AnswerId}",
                            answer.Id);
                    }
                }
                break;
        }
    }

    if (!locations.Any())
    {
        return new LocationStatisticsDto
        {
            TotalLocations = 0,
            Locations = new List<LocationDataPointDto>()
        };
    }

    return new LocationStatisticsDto
    {
        TotalLocations = locations.Count,
        MinLatitude = locations.Min(l => l.Latitude),
        MaxLatitude = locations.Max(l => l.Latitude),
        MinLongitude = locations.Min(l => l.Longitude),
        MaxLongitude = locations.Max(l => l.Longitude),
        CenterLatitude = locations.Average(l => l.Latitude),
        CenterLongitude = locations.Average(l => l.Longitude),
        Locations = locations
    };
}
```

### C. LeafletMapStats.tsx (Full Code)

```typescript
import { useMemo } from 'react';
import { MapContainer, TileLayer, Marker, Popup } from 'react-leaflet';
import { Box, Typography, Paper, Stack } from '@mui/material';
import L from 'leaflet';
import type { LocationStatistics } from '../../types';
import 'leaflet/dist/leaflet.css';

// Fix Leaflet default marker icon issue in React
import icon from 'leaflet/dist/images/marker-icon.png';
import iconShadow from 'leaflet/dist/images/marker-shadow.png';

const DefaultIcon = L.icon({
  iconUrl: icon,
  shadowUrl: iconShadow,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
});

L.Marker.prototype.options.icon = DefaultIcon;

interface LeafletMapStatsProps {
  data: LocationStatistics;
  questionText: string;
}

const LeafletMapStats: React.FC<LeafletMapStatsProps> = ({ data, questionText }) => {
  const center = useMemo<[number, number]>(() => {
    if (data.centerLatitude && data.centerLongitude) {
      return [data.centerLatitude, data.centerLongitude];
    }
    if (data.locations.length > 0) {
      return [data.locations[0].latitude, data.locations[0].longitude];
    }
    return [40.7128, -74.0060]; // Fallback to NYC
  }, [data]);

  const bounds = useMemo<[[number, number], [number, number]] | undefined>(() => {
    if (
      data.minLatitude &&
      data.maxLatitude &&
      data.minLongitude &&
      data.maxLongitude
    ) {
      return [
        [data.minLatitude, data.minLongitude],
        [data.maxLatitude, data.maxLongitude],
      ];
    }
    return undefined;
  }, [data]);

  if (data.totalLocations === 0) {
    return (
      <Paper sx={{ p: 3, textAlign: 'center' }}>
        <Typography variant="h6" gutterBottom>
          {questionText}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          No location responses recorded yet
        </Typography>
      </Paper>
    );
  }

  return (
    <Paper sx={{ p: 2 }}>
      <Stack spacing={2}>
        <Box>
          <Typography variant="h6" gutterBottom>
            {questionText}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {data.totalLocations} location{data.totalLocations !== 1 ? 's' : ''} recorded
          </Typography>
        </Box>

        <Box
          sx={{
            height: 400,
            width: '100%',
            borderRadius: 1,
            overflow: 'hidden',
            '& .leaflet-container': {
              height: '100%',
              width: '100%',
              borderRadius: 1,
            },
          }}
        >
          <MapContainer
            center={center}
            bounds={bounds}
            zoom={bounds ? undefined : 10}
            style={{ height: '100%', width: '100%' }}
            scrollWheelZoom={false}
          >
            <TileLayer
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
              attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            />

            {data.locations.map((location, index) => (
              <Marker
                key={`${location.responseId}-${index}`}
                position={[location.latitude, location.longitude]}
              >
                <Popup>
                  <Box sx={{ minWidth: 200 }}>
                    <Typography variant="subtitle2" fontWeight="bold" gutterBottom>
                      Response #{location.responseId}
                    </Typography>
                    <Typography variant="body2" component="div">
                      <strong>Latitude:</strong> {location.latitude.toFixed(6)}
                      <br />
                      <strong>Longitude:</strong> {location.longitude.toFixed(6)}
                      {location.accuracy && (
                        <>
                          <br />
                          <strong>Accuracy:</strong> ±{location.accuracy.toFixed(1)} meters
                        </>
                      )}
                      {location.timestamp && (
                        <>
                          <br />
                          <strong>Time:</strong>{' '}
                          {new Date(location.timestamp).toLocaleString()}
                        </>
                      )}
                    </Typography>
                  </Box>
                </Popup>
              </Marker>
            ))}
          </MapContainer>
        </Box>

        {data.minLatitude && data.maxLatitude && (
          <Typography variant="caption" color="text.secondary">
            Geographic range: {data.minLatitude.toFixed(4)}° to {data.maxLatitude.toFixed(4)}° latitude,{' '}
            {data.minLongitude?.toFixed(4)}° to {data.maxLongitude?.toFixed(4)}° longitude
          </Typography>
        )}
      </Stack>
    </Paper>
  );
};

export default LeafletMapStats;
```

### D. TypeScript Interface Additions

```typescript
// Add to frontend/src/types/index.ts

// Location Statistics
export interface LocationDataPoint {
  latitude: number;
  longitude: number;
  accuracy?: number | null;
  timestamp?: string | null;
  responseId: number;
}

export interface LocationStatistics {
  totalLocations: number;
  minLatitude?: number | null;
  maxLatitude?: number | null;
  minLongitude?: number | null;
  maxLongitude?: number | null;
  centerLatitude?: number | null;
  centerLongitude?: number | null;
  locations: LocationDataPoint[];
}

// Update QuestionStatistics interface
export interface QuestionStatistics {
  // ... existing properties ...
  locationStatistics?: LocationStatistics; // ADD THIS
}
```

---

## Approval Checklist

Before implementation begins, please confirm:

- [ ] Plan scope is acceptable
- [ ] Leaflet as map library is approved
- [ ] OpenStreetMap as tile provider is acceptable (free, no API key)
- [ ] Estimated timeline is acceptable (8-12 hours)
- [ ] No additional features required (clustering, heatmaps, etc.)

---

**Document Version**: 1.0
**Last Updated**: 2025-12-04
**Author**: Claude (Task Execution Agent)
**Awaiting Approval From**: User
