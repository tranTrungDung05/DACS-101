# Dynamic Route ETA Design

## Goal

Make ETA updates depend on the vehicle's current position and the current optimal route to a configured destination instead of relying on a fixed trip history or a static sample trip.

ETA must continue to be produced by the existing `eta_serving` service. The C# app will be responsible for generating a new route-derived sample trip whenever needed and sending that sample trip to `eta_serving`.

## Current State

- GPS updates enter through `Controllers/GpsController.cs`.
- ETA destination is already configurable in `appsettings.json` under `EtaDestinations`.
- The current ETA flow appends raw GPS points to an in-memory buffer and sends that observed history to `IETAService`.
- This makes ETA dependent on the driven history of the trip and does not support dynamic re-routing around off-route movement.

## Desired Behavior

For each GPS update:

1. Resolve the configured destination for the vehicle.
2. Determine the current optimal route from the vehicle's current location to that destination.
3. If there is no active route for the trip, create one.
4. If the vehicle is still close to the current optimal route, keep using that route.
5. If the vehicle has deviated from the current route beyond a configured threshold, compute a new optimal route from the vehicle's new position to the same destination.
6. Convert the active optimal route into a route-derived sample trip.
7. Send that sample trip to `eta_serving` so ETA continues to come from the existing model service.
8. Broadcast the updated ETA to the frontend.

## Recommended Approach

Use OSRM's public routing API as the route source because it is the fastest path to implementation and does not require API keys.

Keep `eta_serving` in the ETA path. The new route layer does not replace ETA inference; it only replaces the fixed or history-bound trip input with a route-derived sample trip that can be refreshed whenever the vehicle goes off-route.

## Architecture Changes

### 1. Add a routing service in C#

Create a new service, tentatively `IRoutingService` and `RoutingService`, responsible for:

- Calling OSRM route API for `current_position -> configured_destination`
- Decoding the returned polyline geometry into latitude/longitude points
- Returning route metadata:
  - route points
  - route distance
  - route duration

The routing service should fail gracefully and allow the controller to fall back to the last known route state if OSRM is temporarily unavailable.

### 2. Add in-memory route state

Replace the current ETA raw-point buffer with a route-centric in-memory state keyed by journey ID.

Each route state should contain:

- current destination
- full decoded route points
- reduced sample-trip points for ETA
- last known ETA message or result
- last route refresh timestamp

This state is cleared when a trip is closed.

### 3. Route deviation detection

For each new GPS update, compare the current vehicle position against the active route geometry.

Deviation rule:

- Compute the minimum distance from the current point to the route points or route segments.
- If that distance exceeds a configured threshold, treat the vehicle as off-route and recompute the route.

Initial threshold:

- Start with a pragmatic threshold around 75 meters.

To reduce noisy reroutes:

- Do not reroute if the vehicle is already within the arrival threshold of the destination.
- Optionally skip immediate repeated reroutes if the route was just refreshed very recently.

### 4. Build route-derived sample trip for eta_serving

The route returned by OSRM becomes the input template for ETA prediction.

Transformation rules:

- Use the current vehicle position as the first point.
- Use the configured destination as the final point.
- Convert decoded route geometry into a reduced list of GPS-like points.
- Keep the list moderately sized instead of sending every geometry point.
- Generate synthetic timestamps increasing along the route based on OSRM route duration.

The reduced route-derived points should match the payload shape already expected by `IETAService`:

- `gps_points`: array of `{ lat, lon, timestamp }`
- `destination`: `{ lat, lon }`

This preserves the existing ETA service contract while changing the source of the trip points.

## Controller Flow Changes

`Controllers/GpsController.cs` will change as follows:

1. Continue receiving and storing raw GPS data as it does now.
2. Resolve destination from `EtaDestinations`.
3. Load or create route state for the current journey.
4. Check arrival threshold first.
5. If no route exists, fetch route from OSRM.
6. If route exists, test whether the vehicle has deviated from it.
7. If deviated, fetch a new route from OSRM using the vehicle's current position.
8. Convert the active route to a sample trip.
9. Send that route-derived sample trip to `IETAService`.
10. Broadcast ETA update as before.

The existing `ReceiveEtaUpdate` SignalR event should remain unchanged for compatibility.

An optional new event such as `ReceiveRouteUpdate` may be added later, but it is not required for the first implementation.

## Error Handling

- If destination configuration is missing, use the existing fallback destination fields.
- If OSRM fails and a previous route state exists, keep using the previous route-derived sample trip.
- If OSRM fails and no route state exists yet, skip ETA update for that GPS update.
- If `eta_serving` fails, continue processing GPS storage and location broadcasts without blocking the request.

## Testing Strategy

Add targeted automated tests for:

- destination resolution by vehicle ID with fallback
- route-derived sample trip generation
- deviation detection against an existing route
- reroute trigger when the vehicle exceeds the deviation threshold
- ETA request path using route-derived points instead of accumulated raw trip points

Manual verification should cover:

- initial route creation on first GPS update
- stable ETA updates while the vehicle stays on-route
- reroute and refreshed ETA after an off-route update
- arrival behavior near destination

## Scope Boundaries

Included:

- dynamic route lookup via OSRM
- off-route detection and reroute
- route-to-sample-trip conversion
- continued ETA inference through `eta_serving`

Excluded for this iteration:

- persistent route storage in database
- multi-destination workflows
- frontend route rendering redesign
- replacing `eta_serving` with direct OSRM ETA

## Implementation Notes

- Keep the implementation backward-compatible with the current frontend ETA event.
- Favor small focused helpers over large controller logic blocks.
- Avoid changing the public contract of `eta_serving` unless code inspection proves it is required.
- Preserve the current journey lifecycle and only swap the ETA input source from raw-history buffer to active optimal route sample trip.
