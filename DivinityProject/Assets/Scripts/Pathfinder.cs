using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class Pathfinder
{

    public class Node
    {
        public HashSet<Vector3> points = new();
        public Dictionary<Node, Edge> neighbors = new();

        public Vector3 center;
    }

    public class Edge
    {
        public HashSet<Vector3> vertices = new();
    }

    public Node[] nodeList;

    private readonly Mesh mesh;
    private readonly Transform transform;

    public Pathfinder(Mesh targetMesh, Transform localToWorldTransform)
    {

        mesh = targetMesh;
        transform = localToWorldTransform;

        // Init lists. We divide by 3 since the triangle lists each vertex.
        int numNodes = targetMesh.triangles.Length / 3;
        nodeList = new Node[numNodes];

        BuildGraph();

    }

    /// <summary>
    /// Construct the pathfinding data structures from the mesh. This works by taking each triangle in the
    /// mesh, iterating over the previously visited triangles and seeing if they share two vertices.
    /// </summary>
    private void BuildGraph()
    {

        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        Vector3[] triPoints = new Vector3[3];

        // The triangle array is organized in runs of 3 points. We accumulate up until the 3rd point,
        // then process the triangle.
        for (int i = 0; i < tris.Length; i++)
        {

            int triIdx = i % 3;
            int nodeIdx = i / 3;

            triPoints[triIdx] = verts[tris[i]];
            if (triIdx != 2) { continue; }

            // Construct node and compute additional vertex data
            nodeList[nodeIdx] = new()
            {
                points = triPoints.Select(p => transform.TransformPoint(p)).ToHashSet(),
                center = transform.TransformPoint(triPoints.Aggregate((a, b) => a + b) / triPoints.Length)
            };

            // Iterate over all previously added nodes and find shared edges
            int neighborsFound = 0;
            for (int j = nodeIdx - 1; j >= 0; j--)
            {

                IEnumerable<Vector3> commonPoints = nodeList[nodeIdx].points.Intersect<Vector3>(nodeList[j].points);

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

        }

    }

    /// <summary>
    /// Draw debug lines showing connections between cells. Draws from the center of the cell.
    /// </summary>
    /// <param name="c"></param>
    public void ShowDebugGraph(Color c)
    {

        foreach (Node n in nodeList)
        {
            foreach (KeyValuePair<Node, Edge> kv in n.neighbors)
            {
                Debug.DrawLine(n.center, kv.Key.center, c, Mathf.Infinity);
            }
        }

    }

    /// <summary>
    /// Draw debug lines marking the centers of each cell.
    /// </summary>
    /// <param name="c"></param>
    public void ShowDebugCenters(Color c)
    {

        foreach (Node n in nodeList)
        {
            Debug.DrawRay(n.center, Vector3.up * 2, c, Mathf.Infinity);
        }

    }

    /// <summary>
    /// Draw debug lines showing cell boundaries.
    /// </summary>
    /// <param name="c"></param>
    public void ShowDebugCells(Color c)
    {

        foreach (Node n in nodeList)
        {

            List<Vector3> points = n.points.ToList();

            Debug.DrawLine(points[0], points[1], c, Mathf.Infinity);
            Debug.DrawLine(points[1], points[2], c, Mathf.Infinity);
            Debug.DrawLine(points[2], points[0], c, Mathf.Infinity);

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
    public int GetEnclosingCell(Vector3 target)
    {

        int idx = -1;

        for (int i = 0; i < nodeList.Length; i++)
        {

            Vector3[] points = nodeList[i].points.ToArray();

            // We use two sides of the triangle as our basis vectors, and compute the U and V coefficients of the target
            // position relative to them. If u or v is less than 0, we know the point falls outside of one of those sides.
            // If U+V is greater than 1, we know that the point falls outside of the third line. If all those tests succeed,
            // we know that the point is in the triangle.
            Vector3 v0 = points[2] - points[0];
            Vector3 v1 = points[1] - points[0];
            Vector3 v2 = target - points[0];

            // The basic gist of this calculation is to determine the U and V coefficients 
            float div = Vector3.SqrMagnitude(v0) * Vector3.SqrMagnitude(v1) - Vector3.Dot(v0, v1) * Vector3.Dot(v1, v0);
            float u = (Vector3.SqrMagnitude(v1) * Vector3.Dot(v2, v0) - Vector3.Dot(v1, v0) * Vector3.Dot(v2, v1)) / div;
            float v = (Vector3.SqrMagnitude(v0) * Vector3.Dot(v2, v1) - Vector3.Dot(v0, v1) * Vector3.Dot(v2, v0)) / div;

            // Check if point is in triangle
            if (u >= 0 && v >= 0 && (u + v) < 1) { return i; }

        }

        return idx;

    }

    private class NodeContainer : IComparable
    {

        // Costs
        public float fCost = Mathf.Infinity;
        public float gCost = Mathf.Infinity;
        public float hCost = 0;

        public Node node;
        public Vector3 point;

        public bool isClosed = false;
        public NodeContainer parent;
        public Edge parentEdge;

        int IComparable.CompareTo(object obj)
        {
            return fCost.CompareTo(((NodeContainer) obj).fCost);
        }

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
    /// Compute a path along the navmesh graph from startPoint to endPoint.
    /// This works by using the A* graph search algorithm. Basically, each node gets assigned a cost F, G, and H. G is accumulated
    /// during the search and represents the true cost to reach that node from the starting point along the current best path. H is
    /// an estimate of the distance from this node to the goal, and does not change. F is just the sum of these and is recalculated
    /// whenever G changes. Each iteration of the search, the node with the lowest F cost is considered next. The search then considers
    /// that node's neighbors and sees if our current path yields a better G cost than the node's current cost. If it does, the node
    /// is updated and the search moves on to the next iteration. The search ends when the goal is chosen as the next node.
    /// </summary>
    /// <param name="startCellIdx"></param>
    /// <param name="endCellIdx"></param>
    /// <param name="startPoint"></param>
    /// <param name="endPoint"></param>
    /// <returns></returns>
    private LinkedList<NodeContainer> ComputeRoughPath(int startCellIdx, int endCellIdx, Vector3 startPoint, Vector3 endPoint)
    {

        Node startCell = nodeList[startCellIdx];
        Node endCell = nodeList[endCellIdx];

        // Construct starting node
        NodeContainer startingNode = new() { node = startCell, fCost = 0, gCost = 0, point = startPoint };

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

                    Vector3 neighborPoint = (rawNeighborNode == endCell) ? endPoint : rawNeighborNode.center;

                    neighborNode = new NodeContainer
                    {
                        node = rawNeighborNode,
                        parent = cur,
                        parentEdge = kv.Value,
                        point = neighborPoint,
                        hCost = Vector3.Magnitude(neighborPoint - endPoint),
                        gCost = cur.gCost + Vector3.Magnitude(neighborPoint - cur.point)
                    };

                    neighborNode.fCost = neighborNode.hCost + neighborNode.gCost;
                    openSet.Add(neighborNode);
                    nodeContainerMappings.Add(rawNeighborNode, neighborNode);

                }

                else
                {
                    float tentativeG = cur.gCost + Vector3.Magnitude(neighborNode.point - cur.point);
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

        Debug.Log("No path found");
        return new LinkedList<NodeContainer>();

    }

    /// <summary>
    /// Compute a path between the given start and end point using points on the navigation mesh. Note that this computes the
    /// path using Vector3s, but in practice we usually discard the Y coordinate afterwards, since that should equal the actual
    /// terrain height.
    /// 
    /// This works by using the A* graph search algorithm. Basically, each node gets assigned a cost F, G, and H. G is accumulated
    /// during the search and represents the true cost to reach that node from the starting point along the current best path. H is
    /// an estimate of the distance from this node to the goal, and does not change. F is just the sum of these and is recalculated
    /// whenever G changes. Each iteration of the search, the node with the lowest F cost is considered next. The search then considers
    /// that node's neighbors and sees if our current path yields a better G cost than the node's current cost. If it does, the node
    /// is updated and the search moves on to the next iteration. The search ends when the goal is chosen as the next node.
    /// 
    /// Inspiration for the basic algorithm was taken from:
    /// https://www.gamedev.net/tutorials/programming/artificial-intelligence/navigation-meshes-and-pathfinding-r4880/
    /// </summary>
    /// <param name="startPoint"></param>
    /// <param name="endPoint"></param>
    /// <returns></returns>
    public Queue<Vector3> ComputePath(Vector3 startPoint, Vector3 endPoint)
    {

        int startCellIdx = GetEnclosingCell(startPoint);
        int endCellIdx = GetEnclosingCell(endPoint);

        // This should never happen, but doesn't hurt to be safe.
        if (startCellIdx < 0 || endCellIdx < 0) { return new Queue<Vector3>(); }

        // Simple case for moving within the same cell - just move in straight line
        if (startCellIdx == endCellIdx)
        {
            Queue<Vector3> path = new();
            path.Enqueue(startPoint);
            path.Enqueue(endPoint);
            return path;
        }

        // For more complex cases with multiple cells, run the pathfinding algorithm
        LinkedList<NodeContainer> roughPath = ComputeRoughPath(startCellIdx, endCellIdx, startPoint, endPoint);

        // DEBUG
        // Vector3 prevPoint = startPoint;
        // foreach (NodeContainer nc in roughPath)
        // {
        //     Vector3 nextPoint = nc.point;
        //     Debug.DrawLine(prevPoint, nextPoint, Color.green, Mathf.Infinity);
        //     Debug.DrawLine(nc.parentEdge.vertices.First(), nc.parentEdge.vertices.Last(), Color.magenta, Mathf.Infinity);
        //     prevPoint = nextPoint;
        // }

        // Temp - no smoothing
        return new Queue<Vector3>(roughPath.Select(n => n.point));

    }
    
}
