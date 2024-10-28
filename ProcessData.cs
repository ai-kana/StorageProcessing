using UnityEngine;

namespace StorageProcessing;

public class Position
{
    public float X = 0;
    public float Y = 0;
    public float Z = 0;

    public Vector3 ToVector3()
    {
        return new(X, Y, Z);
    }
}

public class ProcessData
{
    public ushort ID = 0;
    public Position DropOffset = new();
    public List<StorageProcess> Processes = [];
}
