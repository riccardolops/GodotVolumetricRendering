using Godot;
using System;

[GlobalClass]
[Tool]
public partial class MovablePanel : Panel
{
	private bool isPressed = false;
	private Vector2 originalPosition;
	private Vector2 mousePressPosition;
	private Vector2 minPosition;
	private Vector2 maxPosition;

	[Export]
	public bool lockVerticalMovement = false;

	[Export]
	public bool lockHorizontalMovement = false;

	[Export]
	public bool lockToParentRect = false;

	public Action onMove;

	public bool IsMoving()
	{
		return isPressed;
	}

	public override void _GuiInput(InputEvent @event)
	{
		InputEventMouseButton mouseButtonEvent = @event as InputEventMouseButton;
		if (mouseButtonEvent != null && mouseButtonEvent.ButtonIndex == MouseButton.Left)
		{
			if (mouseButtonEvent.Pressed)
			{
				isPressed = true;
				mousePressPosition = mouseButtonEvent.GlobalPosition;
				originalPosition = this.GlobalPosition;
				Rect2 rect = this.GetRect();
				Rect2 parentRect = this.GetParentControl().GetRect();
				minPosition = parentRect.Position;
				maxPosition = parentRect.Size - rect.Size;
			}
			else
			{
				isPressed = false;
				mousePressPosition = mouseButtonEvent.GlobalPosition;
				originalPosition = this.GlobalPosition;
			}
		}

		InputEventMouseMotion mouseMotionEvent = @event as InputEventMouseMotion;
		if (mouseMotionEvent != null && isPressed)
		{
			Vector2 offset = mouseMotionEvent.GlobalPosition - mousePressPosition;
			Vector2 newPosition = originalPosition + new Vector2(lockHorizontalMovement ? 0.0f : offset.X, lockVerticalMovement ? 0.0f : offset.Y);
			newPosition = newPosition.Clamp(minPosition, maxPosition);
			this.GlobalPosition = newPosition;
		}
	}
}
