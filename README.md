# SIMPLIFIED OSKAR-STALBERG
## Overview
This project implements a simplified procedural terrain-grid generation approach inspired by the geometric techniques used by Oskar Stålberg, with the goal of producing a naturally structured, hexagon-based grid that can later be deformed into terrain.

The system is designed for Unity using C#.

The generation process starts from a single equilateral triangle and progressively builds concentric rings of triangles. These triangles are then converted into a set of faces, subdivided and quadified to produce a dense interior grid. Finally, a relaxation step distributes the interior vertices while preserving the outer hexagonal boundary.

The intended final topology resembles a large hexagon filled with interconnected quads, providing a suitable foundation for procedural terrain generation.

## Environment
- Engine: Unity

- Language: C#

## Disadvantages
Less complex patterns then the original algorithm.

## Basic Geometry

The system already contains two classes:

### Triangle

A triangle stores three points:
```
Vector2 a
Vector2 b
Vector2 c
```

### Face

A face stores four points:

```
Vector2 a
Vector2 b
Vector2 c
Vector2 d
```

These classes are intentionally simple and primarily represent the connectivity of the generated geometry.


## Explanation

### Step 1: Initial Generation of Triangles

We first create the initial "ring" of triangles. We begin by creating two points: `(0, 0)` and `(0, unit)`. `unit` is customizable and determines the size of the overall grid.

We then calculate a third point so that the three points form an equilateral triangle. The triangle will have:

```csharp
a = (0, 0)
b = (0, unit)
c = calculated point
```

This triangle is stored in:

```csharp
triangles[ring][0]
```

with `ring = 0` initially.

For each following triangle, we repeat the process by considering the `a-c` edge of the previous triangle as the initial edge of the new triangle. We repeat this process five more times until the ring is closed, forming the first hexagon.

For the second ring, we use the `b` and `c` points of each triangle in the first ring as the `a` and `b` points of the corresponding triangle in the second ring:

```csharp
triangles[1][0].a = triangles[0][0].b;
triangles[1][0].b = triangles[0][0].c;
```

The third ring is generated using the exact same process, using the second ring as its reference.

Finally, the fourth ring is used to close off a second hexagon. For example, to calculate `triangles[3][0]`:

```csharp
triangles[3][0].a = triangles[2][0].a;
triangles[3][0].b = triangles[2][0].c;
triangles[3][0].c = triangles[1][1].c;
```

---

### Step 2: Turning the Triangles into Quads

After the triangle generation phase is complete, we fill a new vector called `faces`.

The first ring of triangles is used to create three faces:

```text
triangles[0] + triangles[1]
triangles[2] + triangles[3]
triangles[4] + triangles[5]
```

The second ring works together with the third ring to create additional faces. For example, triangle `0` from the second ring is combined with triangle `0` from the third ring to create a face.

As a result, the triangles from the fourth ring are left untouched.

---

### Step 3: Subdividing Faces and Quadifying the Remaining Triangles

We then use two auxiliary methods: `Quadification` and `Subdivision`.

```csharp
public Face[] Quadification(Triangle triangle)
{
    // Returns 3 faces
}
```

`Quadification` takes a triangle and returns three faces by dividing each edge of the triangle and connecting the resulting points to the center of the triangle.

```csharp
public Face[] Subdivision(Face face)
{
    // Returns 4 faces
}
```

`Subdivision` takes a face and subdivides it into four smaller faces.

Finally, we fill the last vector, `facesFinal`. This vector contains all the faces resulting from the subdivision of every face in the initial `faces` vector, as well as the faces resulting from the quadification of every fourth-ring triangle.

---

### Step 4: Final Step — Relaxation Algorithm

The final step is to apply a relaxation algorithm to the generated faces.

The relaxation should only affect the interior points of the grid and must not move the outermost points of the hexagon. At this stage, the grid will look like a large hexagon containing many quads.

The goal is to relax the interior vertices while keeping the outer hexagonal boundary fixed, so that the overall hexagonal shape is preserved.


## End

In the end, we achieved our goal: we created a somewhat interesting and naturally shaped grid.

There are several directions this system could be taken further. One possibility would be to create a method for generating neighboring grids and connecting their shared vertices. This would allow multiple hexagonal grids to be seamlessly connected into a larger map.

Another possibility would be to store each face in a graph, keeping track of which faces are adjacent to one another. This would open the door to many more interesting developments, such as:

* **WFC (Wave Function Collapse)** to generate more natural tilemaps.
* **Tile-based movement**, allowing entities to move from one face to an adjacent face.
* **Biome generation**, where different regions of the graph can be assigned different biome types.
* **Pathfinding**, using the face adjacency graph as the basis for navigation.
* **Procedural map generation**, using the relationships between neighboring faces to create larger and more complex worlds.

## Practical example

In a Unity scene create an empty object attach to it the HexQuadGrid.cs component. Create a prefab with ....