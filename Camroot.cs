using Godot;

public partial class Camroot : Node3D
{
    private float camroot_h;
    private float camroot_v;
    private Node3D h, v, cam;
    /// <summary>
    /// Called when the node enters the scene tree for the first time.
    /// </summary>
    public override void _Ready()
    {
        h = GetChild(0) as Node3D;
        v = h.GetChild(0) as Node3D;
        cam = v.GetChild(0) as Node3D;
    }

    /// <summary>
    /// Called every frame. 'delta' is the elapsed time since the previous frame.
    /// </summary>
    /// <param name="delta"></param>
    public override void _Process(double delta)
    {
    }
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event is InputEventMouseMotion motion && Input.IsMouseButtonPressed(MouseButton.Left))
        {
            camroot_h += motion.Relative.X;
            camroot_v += motion.Relative.Y;
        }
        if (@event is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == MouseButton.WheelUp)
            {
                cam.Translate(new Vector3(0, 0, 0.1f));
            }
            else if (mouse.ButtonIndex == MouseButton.WheelDown)
            {
                cam.Translate(new Vector3(0, 0, -0.1f));
            }
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        h.RotationDegrees = new Vector3(0, camroot_h, 0) + cam.RotationDegrees;
        v.RotationDegrees = new Vector3(camroot_v, 0, 0) + cam.RotationDegrees;
    }
}
