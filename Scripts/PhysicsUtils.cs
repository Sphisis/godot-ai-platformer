using Godot;
using System;

public static class PhysicsUtils
{
    // Returns the raw IntersectRay result dictionary for the query from source->target.
    public static Godot.Collections.Dictionary Raycast(Node2D source, Node2D target, bool collideWithAreas = false, bool collideWithBodies = true)
    {
        if (source == null || target == null) return null;
        var q = PhysicsRayQueryParameters2D.Create(source.GlobalPosition, target.GlobalPosition);
        q.CollideWithAreas = collideWithAreas;
        q.CollideWithBodies = collideWithBodies;
        
        if (source is CollisionObject2D c) q.Exclude = new Godot.Collections.Array<Rid> { c.GetRid() };
        return source.GetWorld2D().DirectSpaceState.IntersectRay(q);
    }

    public static bool IsLineOfSightBetween(Node2D source, Node2D target, float tolerance = 8f, bool collideWithAreas = false, bool collideWithBodies = true)
    {
        var r = Raycast(source, target, collideWithAreas, collideWithBodies);
        if (r == null || r.Count == 0) return true;  // No obstacles

        // Check if we hit the target directly or its collision shape
        if (r.ContainsKey("collider") && r["collider"].Obj is GodotObject obj)
        {
            if (obj == target) return true;
            if (obj is Node n)
            {
                // Handle CollisionShape2D hits by checking their parent/owner
                if (n is CollisionShape2D cs)
                    n = cs.GetParent<Node>() ?? cs.GetOwner<Node>() ?? n;
                
                // Check if hit node or any of its parents is our target
                for (var p = n; p != null; p = p.GetParent())
                    if (p == target) return true;
            }
        }

        // Fallback: check if hit point is close enough to target
        return r.ContainsKey("position") && 
               ((Vector2)r["position"]).DistanceTo(target.GlobalPosition) <= tolerance;
    }
}
