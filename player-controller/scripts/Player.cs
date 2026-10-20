using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json; // Built into modern .NET

public partial class Player : CharacterBody3D
{
    public class JumpTelemetry
    {
        public float X {get; set;}
        public float Y {get; set;}
        public float Z {get; set;}
        public bool IsHanging {get;set;}
        //public string TimeStamp {get;set;}
    }

    private List<JumpTelemetry> telemetryLogs = new List<JumpTelemetry>();

	[Export]public float Speed = 10.0f;
	[Export]public float JumpVelocity = 9.5f;
	[Export]public float MouseSensitivity = 0.1f;
    [Export]public float CoyoteTime = 1.0f;

    private bool isCoyote = false;

    private bool isHanging = false;

    // neck is basically like a the stick that connects the camera to the player
    private Node3D _neck;
    private Camera3D _camera;

    public override void _Ready()
    {
        // the neck is used to rotate the camera up/down, while the player body rotates left/right, thus preventing gimbal lock
        _neck = GetNode<Node3D>("Neck");
        _camera = GetNode<Camera3D>("Neck/Camera3D");

        // Captures the mouse so it stays in the center and is invisible
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Check if the event is mouse movement
        if (@event is InputEventMouseMotion mouseMotion)
        {
            // Rotate the whole player left/right (Y-axis)
            RotateY(-mouseMotion.Relative.X * MouseSensitivity);

            // Rotate only the neck up/down (X-axis)
            _neck.RotateX(-mouseMotion.Relative.Y * MouseSensitivity);

            // Clamp the vertical rotation so you can't look past straight up/down
            Vector3 currentRotation = _neck.Rotation;
            currentRotation.X = Mathf.Clamp(currentRotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90));
            _neck.Rotation = currentRotation;
        }

        // Press 'Escape' to get your mouse back (useful for debugging!)
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Hnadle the coyote time
		if (!IsOnFloor() && !isCoyote)
		{
			velocity += GetGravity() * (float)delta;
		}
        else if(!IsOnFloor() && isCoyote)
        {
            isHanging = true;
            CoyoteTime -= (float)delta;
            if (CoyoteTime <= 0.0f){
                isCoyote = false;
                isHanging = false;
            }
        }
        else if(IsOnFloor()){
            CoyoteTime = 1.0f;
            isCoyote = true;
            isHanging = false;
        }

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && isCoyote)
		{
            CaptureJumpTelemetry(GlobalPosition, isHanging);
            isCoyote = false;
			velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");

        // Converts the 2D input direction to a 3D direction relative to the camera.
        // transform.basis is a 3x3 matrix representing the rotation and scale of the object. Its the key to moving relative to the camera.
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
            // apply horizontal movement
            velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
            // Decelerate to a stop when there is no input.
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

        // Apply the final velocity and move the character.
        Velocity = velocity;
		MoveAndSlide();
	}

    private void CaptureJumpTelemetry(Vector3 location, bool isHanging)
    {
        JumpTelemetry current = new JumpTelemetry{
            X = location.X,
            Y = location.Y,
            Z = location.Z,
            IsHanging = isHanging,
            //TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
        };

        telemetryLogs.Add(current);

        GD.Print(location.ToString());
    }
    
}
