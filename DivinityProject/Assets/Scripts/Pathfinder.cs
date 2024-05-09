using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

/// <summary>
/// CAGD 2080 - 05/09/2024
/// Steven Bertolucci
/// Helper class implementing the main pathfinding algorithm. This class serves as both a graph implementation and an interface
/// to translate 2D graph points to 3D world space. Pathfinding is implemented via the A* algorithm, with some final additional
/// smoothing applied via (a somewhat bad implementation of) the Simple Stupid Funnel algorithm.
/// </summary>
public class Pathfinder
{

    /// <summary>
    /// An object representing a vertex in the graph. This holds the list of neighbors as well
    /// as the points bounding the cell in world space.
    /// </summary>
    public class Node
    {
        public HashSet<Vector2> points = new();
        public Dictionary<Node, Edge> neighbors = new();

        public Vector2 center;
    }

    /// <summary>
    /// An object representing an edge in the graph connecting two vertices. This holds the start
    /// and end point of the edge in world space.
    /// </summary>
    public class Edge
    {
        public HashSet<Vector2> vertices = new();
    }

    /// <summary>
    /// A container holding a node which is used in our pathfinding algorithm. The main purpose of this
    /// is to hold pathfinding weights and allow for sorting on the total cost in conjunction with a
    /// SortedSet.
    /// </summary>
    public class NodeContainer : IComparable
    {

        // Costs
        public float fCost = Mathf.Infinity;
        public float gCost = Mathf.Infinity;
        public float hCost = 0;

        public Node node;
        public Vector2 point;

        public bool isClosed = false;
        public NodeContainer parent;
        public Edge parentEdge;

        int IComparable.CompareTo(object obj)
        {
            return fCost.CompareTo(((NodeContainer) obj).fCost);
        }

    }

    /// <summary>
    /// The list of nodes in the graph.
    /// </summary>
    public Node[] nodeList;

    public bool GraphReady;

    private readonly Mesh mesh;
    private readonly Transform transform;

    public Pathfinder(Mesh targetMesh, Transform localToWorldTransform)
    {

        GraphReady = false;

        mesh = targetMesh;
        transform = localToWorldTransform;

        // Init lists. We divide by 3 since the triangle lists each vertex.
        int numNodes = targetMesh.triangles.Length / 3;
        nodeList = new Node[numNodes];

    }

    /// <summary>
    /// Helper to convert a Vector3 to a Vector2 by discarding the Y (height) information. Our nav graphs are represented in
    /// 2D so we often want to discard height info.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    private Vector2 Vector3To2(Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    /// <summary>
    /// Helper to expand a Vector2 into a Vector3 by adding Y (heigh) information. Mostly used for displaying graph data in
    /// world space.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private Vector3 Vector2To3(Vector2 v, float y = 0)
    {
        return new Vector3(v.x, y, v.y);
    }

    /// <summary>
    /// Construct the pathfinding data structures from the mesh. This works by taking each triangle in the
    /// mesh, iterating over the previously visited triangles and seeing if they share two vertices.
    /// </summary>
    public IEnumerator BuildGraph()
    {

        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        Vector2[] triPoints = new Vector2[3];

        // The triangle array is organized in runs of 3 points. We accumulate up until the 3rd point,
        // then process the triangle.
        for (int i = 0; i < tris.Length; i++)
        {

            int triIdx = i % 3;
            int nodeIdx = i / 3;

            triPoints[triIdx] = Vector3To2(transform.TransformPoint(verts[tris[i]]));
            if (triIdx != 2) { continue; }

            // Construct node and compute additional vertex data
            nodeList[nodeIdx] = new()
            {
                points = triPoints.ToHashSet(),
                center = triPoints.Aggregate((a, b) => a + b) / triPoints.Length
            };

            // Iterate over all previously added nodes and find shared edges
            int neighborsFound = 0;
            for (int j = nodeIdx - 1; j >= 0; j--)
            {

                IEnumerable<Vector2> commonPoints = nodeList[nodeIdx].points.Intersect(nodeList[j].points);

                // Adjacent triangles share two common points
                if (commonPoints.Count() == 2)
                {

                    Edge newEdge = new() { vertices = commonPoints.ToHashSet() };
                    nodeList[nodeIdx].neighbors[nodeList[j]] = newEdge;
                    nodeList[j].neighbors[nodeList[nodeIdx]] = newEdge;

                    neighborsFound += 1;

                }

                // A triangle can have at most 3 neighbors, so no point in continuing after that.
                if (neighborsFound == 3) { break; }

            }

            yield return null;

        }

        GraphReady = true;

    }

    /// <summary>
    /// Draw debug lines showing connections between cells. Draws from the center of the cell.
    /// </summary>
    /// <param name="c"></param>
    public void DrawDebugGraph(Color c)
    {

        if (!GraphReady) { return; }

        foreach (Node n in nodeList)
        {
            foreach (KeyValuePair<Node, Edge> kv in n.neighbors)
            {
                Debug.DrawLine(Vector2To3(n.center), Vector2To3(kv.Key.center), c);
            }
        }

    }

    /// <summary>
    /// Draw debug lines marking the centers of each cell.
    /// </summary>
    /// <param name="c"></param>
    public void DrawDebugCenters(Color c)
    {

        if (!GraphReady) { return; }

        foreach (Node n in nodeList)
        {
            Debug.DrawRay(Vector2To3(n.center), Vector3.up * 2, c);
        }

    }

    /// <summary>
    /// Draw debug lines showing cell boundaries.
    /// </summary>
    /// <param name="c"></param>
    public void DrawDebugCells(Color c)
    {

        if (!GraphReady) { return; }

        foreach (Node n in nodeList)
        {

            List<Vector3> points = n.points.Select(p => Vector2To3(p)).ToList();
            DebugShapes.DrawTriangle(points[0], points[1], points[2], c, Time.deltaTime);

        }

    }

    /// <summary>
    /// Find the cell (i.e. triangle) that encloses the target point. The given point is expected to lie on one of the graph's
    /// triangles. We return the index of the triangle if found, or -1 if not.
    /// The algorithm to test the enclosing point was adapted from:
    /// https://blackpawn.com/texts/pointinpoly/
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private int GetEnclosingCell(Vector2 target2D, Vector2 endPosition)
    {

        // It's a bit inefficient, but if we're standing on exactly one of the corners of a cell, we could be considered
        // to be in multiple cells. In that case, we want to choose the one closest to the destination. We therefore loop
        // over all cells every time.
        List<int> candidates = new();

        for (int i = 0; i < nodeList.Length; i++)
        {

            Vector2[] points = nodeList[i].points.ToArray();

            // Edge case for when a point falls exactly on a corner of the cell.
            foreach (Vector2 p in points)
            {
                if (p == target2D)
                {
                    candidates.Add(i);
                    continue;
                }
            }

            // We use two sides of the triangle as our basis vectors, and compute the U and V coefficients of the target
            // position relative to them. If u or v is less than 0, we know the point falls outside of one of those sides.
            // If U+V is greater than 1, we know that the point falls outside of the third line. If all those tests succeed,
            // we know that the point is in the triangle.
            Vector3 v0 = points[2] - points[0];
            Vector3 v1 = points[1] - points[0];
            Vector3 v2 = target2D - points[0];

            // The basic gist of this calculation is to determine the U and V coefficients 
            float div = Vector3.SqrMagnitude(v0) * Vector3.SqrMagnitude(v1) - Vector3.Dot(v0, v1) * Vector3.Dot(v1, v0);
            float u = (Vector3.SqrMagnitude(v1) * Vector3.Dot(v2, v0) - Vector3.Dot(v1, v0) * Vector3.Dot(v2, v1)) / div;
            float v = (Vector3.SqrMagnitude(v0) * Vector3.Dot(v2, v1) - Vector3.Dot(v0, v1) * Vector3.Dot(v2, v0)) / div;

            // Check if point is in triangle
            if (u >= 0 && v >= 0 && (u + v) < 1) {
                candidates.Add(i);
            }

        }

        int idx = -1;
        float minDist = Mathf.Infinity;
        foreach (int c in candidates)
        {
            float dist = Vector2.SqrMagnitude(endPosition - nodeList[c].center);
            if (dist < minDist)
            {
                minDist = dist;
                idx = c;
            }
        }

        return idx;

    }

    /// <summary>
    /// Compute a path along the navmesh graph from startPoint to endPoint.
    /// This works by using the A* graph search algorithm. Basically, each node gets assigned a cost F, G, and H. G is accumulated
    /// during the search and represents the true cost to reach that node from the starting point along the current best path. H is
    /// an estimate of the distance from this node to the goal, and does not change. F is just the sum of these and is recalculated
    /// whenever G changes. Each iteration of the search, the node with the lowest F cost is considered next. The search then considers
    /// that node's neighbors and sees if our current path yields a better G cost than the node's current cost. If it does, the node
    /// is updated and the search moves on to the next iteration. The search ends when the goal is chosen as the next node.
    /// </summary>
    /// <param name="startCell">The starting cell</param>
    /// <param name="endCell">The ending cell</param>
    /// <param name="startPoint">The starting point within the starting cell</param>
    /// <param name="endPoint">The ending point within the ending cell</param>
    /// <returns>The computed path, or an empty list if no path was found.</returns>
    private LinkedList<NodeContainer> ComputeRoughPath(Node startCell, Node endCell, Vector2 startPoint2D, Vector2 endPoint2D)
    {

        // Construct starting node
        NodeContainer startingNode = new() { node = startCell, fCost = 0, gCost = 0, point = startPoint2D };

        // Unity's version of C# doesn't have a built-in PriorityQueue, so we're going to use a SortedSet and a
        // container class to emulate that instead. SortedSets don't like it when the sort value is changed, so
        // we'll just remove and re-insert when we need to do that.
        // The openSet essentially holds a list of nodes ordered by their approximate total cost.
        SortedSet<NodeContainer> openSet = new() { startingNode };

        // We also need an efficient way to get a node container given a node during neighbor searches.
        Dictionary<Node, NodeContainer> nodeContainerMappings = new() { [startingNode.node] = startingNode };

        // Run until there are no more reachable nodes
        while (openSet.Count != 0)
        {

            // Dequeue current best guess. This should perform in O(logn)
            NodeContainer cur = openSet.First();
            openSet.Remove(cur);
            cur.isClosed = true;

            // Ending condition - trace back the found path into a node list
            if (cur.node == endCell)
            {
                LinkedList<NodeContainer> path = new();
                UnwindPath(path, cur);
                return path;
            }

            // Visit each neighbor - there will be a maximum of 3 for any given node.
            foreach (KeyValuePair<Node, Edge> kv in cur.node.neighbors)
            {

                // Find the container containing this node if it exists
                Node rawNeighborNode = kv.Key;

                NodeContainer neighborNode = null;
                if (nodeContainerMappings.ContainsKey(rawNeighborNode))
                {
                    neighborNode = nodeContainerMappings[rawNeighborNode];
                }

                if (neighborNode != null && neighborNode.isClosed)
                {
                    continue;
                }

                // If the neighbor is not in the open set
                if (neighborNode == null)
                {

                    Vector2 neighborPoint = (rawNeighborNode == endCell) ? endPoint2D : rawNeighborNode.center;

                    neighborNode = new NodeContainer
                    {
                        node = rawNeighborNode,
                        parent = cur,
                        parentEdge = kv.Value,
                        point = neighborPoint,
                        hCost = (neighborPoint - endPoint2D).magnitude,
                        gCost = cur.gCost + (neighborPoint - cur.point).magnitude
                    };

                    neighborNode.fCost = neighborNode.hCost + neighborNode.gCost;
                    openSet.Add(neighborNode);
                    nodeContainerMappings.Add(rawNeighborNode, neighborNode);

                }

                else
                {
                    float tentativeG = cur.gCost + (neighborNode.point - cur.point).magnitude;
                    if (tentativeG < neighborNode.gCost)
                    {
                        openSet.Remove(neighborNode);
                        neighborNode.gCost = tentativeG;
                        neighborNode.fCost = neighborNode.hCost + neighborNode.gCost;
                        neighborNode.parent = cur;
                        neighborNode.parentEdge = kv.Value;
                        openSet.Add(neighborNode);
                    }
                }

            }

        }

        return new LinkedList<NodeContainer>();

    }

    /// <summary>
    /// Traverse the found path backwards and construct a list from it. We deliberately don't add the first
    /// node, since it'll just be the location where the player is currently.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="node"></param>
    private void UnwindPath(LinkedList<NodeContainer> path, NodeContainer node)
    {

        NodeContainer prevNode = node.parent;

        if (prevNode != null) {
            UnwindPath(path, prevNode);
            path.AddLast(node);
        }

    }

    /// <summary>
    /// Smooth out the given path, eliminating non-essential nodes. This works using a modified version of the "Simple Stupid
    /// Funnel" algorithm described at:
    /// http://digestingduck.blogspot.com/2010/03/simple-stupid-funnel-algorithm.html
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// TODO: This more or less works, still some bugs in edge cases.
    public LinkedList<Vector2> SmoothPath(Vector2 startPoint, Vector2 endPoint, LinkedList<NodeContainer> roughPath)
    {

        LinkedList<Vector2> pointsList = new();

        // Preprocess and sort edges
        Vector2 lastCell = startPoint;
        LinkedList<Vector2[]> sortedEdgeList = new();

        foreach (NodeContainer cell in roughPath)
        {

            Vector2 v0 = cell.parentEdge.vertices.First();
            Vector2 v1 = cell.parentEdge.vertices.Last();

            Vector2 left;
            Vector2 right;

            // The cross product points perpendicular to both input vectors, so we can use it to determine relative orientation
            Vector3 orientationDirection = Vector3.Cross(Vector2To3(cell.point - v0), Vector2To3(cell.point - lastCell));

            if (orientationDirection.y > 0)
            {
                right = v0;
                left = v1;
            }
            else
            {
                right = v1;
                left = v0;
            }

            sortedEdgeList.AddLast(new Vector2[] { left, right });
            lastCell = cell.point;

        }

        sortedEdgeList.AddLast(new Vector2[] { endPoint, endPoint });

        // Draw edges - left is red, right is blue
        // foreach (Vector2[] edge in sortedEdgeList)
        // {
        //     Debug.DrawRay(new Vector3(edge[0].x, 0, edge[0].y), Vector3.up * 5, Color.red, Mathf.Infinity);
        //     Debug.DrawRay(new Vector3(edge[1].x, 0, edge[1].y), Vector3.up * 5, Color.blue, Mathf.Infinity);
        // }

        Vector2 apex = startPoint;
        Vector2 funnelLeft = startPoint;
        Vector2 funnelRight = startPoint;
        float theta = Mathf.Infinity;

        pointsList.AddLast(apex);

        foreach (Vector2[] edge in sortedEdgeList)
        {

            Vector2 left = edge[0];
            Vector2 right = edge[1];

            // Left side first
            Vector2 leftEdge = apex - left;
            Vector2 rightEdge = apex - funnelRight;
            float newTheta = Vector2.SignedAngle(rightEdge, leftEdge);

            if (newTheta < 0)
            {
                apex = funnelRight;
                pointsList.AddLast(apex);
                funnelLeft = left;
                funnelRight = right;
                theta = Mathf.Infinity;
            }
            else if (newTheta == 0)
            {
                funnelLeft = left;
            }
            else if (newTheta <= theta)
            {
                funnelLeft = left;
                theta = newTheta;
            }

            // Debug.DrawLine(new Vector3(apex.x, 1.0f, apex.y), new Vector3(funnelLeft.x, 1.0f, funnelLeft.y), Color.black, 2.5f);
            // Debug.DrawLine(new Vector3(apex.x, 1.0f, apex.y), new Vector3(funnelRight.x, 1.0f, funnelRight.y), Color.black, 2.5f);

            // Now right side
            leftEdge = apex - funnelLeft;
            rightEdge = apex - right;
            newTheta = Vector2.SignedAngle(rightEdge, leftEdge);

            if (newTheta < 0)
            {
                apex = funnelLeft;
                pointsList.AddLast(apex);
                funnelLeft = left;
                funnelRight = right;
                theta = Mathf.Infinity;
            }
            else if (newTheta == 0)
            {
                funnelRight = right;
            }
            else if (newTheta <= theta)
            {
                funnelRight = right;
                theta = newTheta;
            }

            // Debug.DrawLine(new Vector3(apex.x, 1.0f, apex.y), new Vector3(funnelLeft.x, 1.0f, funnelLeft.y), Color.black, 2.5f);
            // Debug.DrawLine(new Vector3(apex.x, 1.0f, apex.y), new Vector3(funnelRight.x, 1.0f, funnelRight.y), Color.black, 2.5f);

        }

        pointsList.AddLast(endPoint);

        return pointsList;

    }


    /// Inspiration for the basic algorithm was taken from:
    /// https://www.gamedev.net/tutorials/programming/artificial-intelligence/navigation-meshes-and-pathfinding-r4880/
    /// </summary>
    /// <param name="startPoint"></param>
    /// <param name="endPoint"></param>
    /// <returns></returns>
    public Queue<Vector2> ComputePath(Vector3 startPoint, Vector3 endPoint)
    {

        Vector2 startPoint2D = Vector3To2(startPoint);
        Vector2 endPoint2D = Vector3To2(endPoint);

        if (!GraphReady) {
            return new Queue<Vector2>();
        }

        int startCellIdx = GetEnclosingCell(startPoint2D, endPoint2D);
        int endCellIdx = GetEnclosingCell(endPoint2D, startPoint2D);

        // This should never happen, but doesn't hurt to be safe.
        if (startCellIdx < 0 || endCellIdx < 0) { return new Queue<Vector2>(); }

        // Simple case for moving within the same cell - just move in straight line
        if (startCellIdx == endCellIdx)
        {
            Queue<Vector2> path = new();
            path.Enqueue(startPoint2D);
            path.Enqueue(endPoint2D);
            return path;
        }

        Node startCell = nodeList[startCellIdx];
        Node endCell = nodeList[endCellIdx];
        
        Vector3[] sC = startCell.points.Select(p => Vector2To3(p)).ToArray();
        // DebugShapes.DrawShadedTriangle(sC[0], sC[1], sC[2], Color.red, Mathf.Infinity);
        Vector3[] eC = endCell.points.Select(p => Vector2To3(p)).ToArray();
        // DebugShapes.DrawShadedTriangle(eC[0], eC[1], eC[2], Color.blue, Mathf.Infinity);

        // For more complex cases with multiple cells, run the pathfinding algorithm
        LinkedList<NodeContainer> roughPath = ComputeRoughPath(startCell, endCell, startPoint2D, endPoint2D);
        LinkedList<Vector2> smoothPath = SmoothPath(startPoint2D, endPoint2D, roughPath);

        // Mark the computed cells
        // foreach (NodeContainer nc in roughPath)
        // {
        //     Vector3[] p = nc.node.points.Select(p => Vector2To3(p)).ToArray();
        //     DebugShapes.DrawShadedTriangle(p[0], p[1], p[2], Color.black, Mathf.Infinity);
        // }

        // // Mark portal edges
        // foreach (NodeContainer nc in roughPath)
        // {
        //     Vector3[] p = nc.parentEdge.vertices.Select(p => Vector2To3(p)).ToArray();
        //     DebugShapes.DrawWall(p[0], p[1], 0.5f, Color.green, Mathf.Infinity);
        // }

        return new Queue<Vector2>(smoothPath);

    }
    
}
