# YallaCatch v3.0 Backend-Unity Sync Guide

## 📡 Data Mandates
The Unity Game Client requires root-level data flattening for high-performance parsing and strict Hub compatibility.

### 1. Coordinate Flattening
All Geolocation data must be flattened in JSON responses.
**Backend (Mongoose/Fastify):**
```ts
// model.ts
transform: (doc, ret) => {
  if (ret.location && ret.location.coordinates) {
    ret.lat = ret.location.coordinates[1];
    ret.lng = ret.location.coordinates[0];
  }
}
```
**Unity (Models.cs):**
```csharp
public float lat;
public float lng;
```

### 2. ID Stringification
Ensure `_id` and other `ObjectId` fields are returned as strings, not nested objects.

### 3. Level Logic
- `User.level` (String '1'-'5'): Dictates Game Logic/Physics.
- `User.levelName` (Enum 'bronze'...'diamond'): Dictates Branding/UI.

## 🛡️ Capture Pipeline
- Unity sends `POST /capture/attempt`.
- Backend MUST return success or failure with descriptive errors (e.g., "Out of range").
- Success responses MUST return the updated `pointsTotal` to trigger the HUD update.
