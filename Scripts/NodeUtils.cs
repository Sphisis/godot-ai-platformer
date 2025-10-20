using Godot;
using System;

public static class NodeUtils
{
    // Recursive search for first node of type T under start
    public static T FindNodeByType<T>(Node start) where T : Node
    {
        if (start == null) return null;
        foreach (var childObj in start.GetChildren())
        {
            if (childObj is T t) return t;
            if (childObj is Node child)
            {
                var r = FindNodeByType<T>(child);
                if (r != null) return r;
            }
        }
        return null;
    }

    // Recursive search for a node with the given name
    public static Node FindNodeByName(Node start, string name)
    {
        if (start == null) return null;
        if (start.Name == name) return start;
        foreach (var childObj in start.GetChildren())
            if (childObj is Node child)
            {
                var r = FindNodeByName(child, name);
                if (r != null) return r;
            }
        return null;
    }
}
