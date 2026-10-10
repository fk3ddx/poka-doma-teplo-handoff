// Floor plan of the flat (metres). Yard facade = z 0 (windows look toward -z).
// Flat is on the 5th (top) floor; its floor is y = 0, the yard ground is far below.
export const H = 2.5;             // ceiling height
export const FLOOR_H = 2.7;       // storey height
export const GROUND_Y = -4 * FLOOR_H - 0.9;
export const EXT_T = 0.35;        // exterior wall thickness

export const rooms = {
  bedroom: {x0: 0, z0: 0, x1: 2.8, z1: 4.4, wall: 'wallBedroom', floor: 'parquet'},
  living: {x0: 2.9, z0: 0, x1: 6.5, z1: 4.4, wall: 'wallLiving', floor: 'parquet'},
  kitchen: {x0: 6.6, z0: 0, x1: 8.8, z1: 2.9, wall: 'kitchenWall', floor: 'linoleum'},
  hallA: {x0: 6.6, z0: 3.0, x1: 8.8, z1: 6.0, wall: 'wallHall', floor: 'linoBrown', noWall: {w: [4.5, 6.0]}},
  hallB: {x0: 4.7, z0: 4.5, x1: 6.6, z1: 6.0, wall: 'wallHall', floor: 'linoBrown', noWall: {e: [4.5, 6.0]}},
  bath: {x0: 2.9, z0: 4.5, x1: 4.6, z1: 6.0, wall: 'tiles', floor: 'linoBrown'},
};

// Openings: axis 'x' = wall plane perpendicular to x (runs along z); c0..c1 = slab extents.
export const openings = [
  // interior doors
  {id: 'doorBedroom', axis: 'x', c0: 2.8, c1: 2.9, a0: 3.35, a1: 4.15, y0: 0, y1: 2.0, type: 'door'},
  {id: 'doorLiving', axis: 'z', c0: 4.4, c1: 4.5, a0: 5.25, a1: 6.05, y0: 0, y1: 2.0, type: 'door'},
  {id: 'doorKitchen', axis: 'z', c0: 2.9, c1: 3.0, a0: 6.9, a1: 7.6, y0: 0, y1: 2.0, type: 'door'},
  {id: 'doorBath', axis: 'x', c0: 4.6, c1: 4.7, a0: 4.9, a1: 5.6, y0: 0, y1: 2.0, type: 'door'},
  // entrance
  {id: 'doorFront', axis: 'z', c0: 6.0, c1: 6.3, a0: 7.6, a1: 8.5, y0: 0, y1: 2.05, type: 'door'},
  // facade windows (z = 0 inner face, -EXT_T outer face)
  {id: 'winBedroom', axis: 'z', c0: -EXT_T, c1: 0, a0: 0.75, a1: 2.05, y0: 0.85, y1: 2.3, type: 'window'},
  {id: 'winLiving', axis: 'z', c0: -EXT_T, c1: 0, a0: 3.25, a1: 4.65, y0: 0.85, y1: 2.3, type: 'window'},
  {id: 'doorBalcony', axis: 'z', c0: -EXT_T, c1: 0, a0: 4.8, a1: 5.55, y0: 0, y1: 2.3, type: 'balcony'},
  {id: 'winKitchen', axis: 'z', c0: -EXT_T, c1: 0, a0: 7.1, a1: 8.3, y0: 0.85, y1: 2.3, type: 'window'},
];

// Wall slabs for collision (x0,z0,x1,z1). Doors are added separately.
export const wallSlabs = [
  // exterior facade
  [-0.3, -EXT_T, 9.1, 0],
  // side walls
  [-0.3, -EXT_T, 0, 6.3], [8.8, -EXT_T, 9.1, 6.3],
  // stairwell side
  [-0.3, 6.0, 9.1, 6.3],
  // bedroom | living
  [2.8, 0, 2.9, 4.5],
  // living | kitchen/hall
  [6.5, 0, 6.6, 4.5],
  // kitchen | hall
  [6.6, 2.9, 8.8, 3.0],
  // living | hall/bath (z 4.4..4.5), bedroom back wall
  [-0.3, 4.4, 6.6, 4.5],
  // bath | hall
  [4.6, 4.5, 4.7, 6.0],
  // pantry behind bedroom (solid)
  [0, 4.5, 2.9, 6.0],
];

export const balcony = {x0: 2.95, x1: 6.45, z0: -1.3, z1: -EXT_T};

// Stairwell landing (outside the front door)
export const landing = {x0: 6.0, x1: 10.4, z0: 6.3, z1: 8.0};

// Key positions used by the story
export const spots = {
  landingStart: {x: 8.05, z: 7.7, yaw: 0},
  hallEntry: {x: 8.05, z: 5.5},
  phone: {x: 6.8, y: 0.85, z: 3.75},
  armchair: {x: 3.45, z: 0.85},
  tv: {x: 6.2, y: 1.05, z: 1.85},
  stove: {x: 8.5, z: 1.9},
  kitchenWindow: {x: 7.7, z: 0.4},
  balconyDoor: {x: 5.175, z: -0.17},
};
