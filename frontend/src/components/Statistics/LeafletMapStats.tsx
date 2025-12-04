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
